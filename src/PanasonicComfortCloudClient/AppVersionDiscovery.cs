// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using HtmlAgilityPack;

using PanasonicComfortCloudClient.Concurrency;

namespace PanasonicComfortCloudClient;

/// <summary>Finds the Android app version using the Python reference's Play Store, AppBrain and optional gist sources.</summary>
/// <remarks>This explicitly created helper owns an in-memory two-day cache only. It never retries device commands or changes a cloud client's protocol version implicitly.</remarks>
public sealed class AppVersionDiscovery : IDisposable
	{
	private readonly HttpClient _http;
	private readonly bool _ownsHttp;
	private readonly AsyncOperationGate _operations;
	private AppVersionObservation? _current;
	/// <summary>Creates an explicit version-discovery helper.</summary>
	/// <param name="httpClient">An optional caller-owned HTTP client whose redirects are disabled.</param>
	/// <param name="initialObservation">An optional application-restored version observation; storage remains application-owned.</param>
	public AppVersionDiscovery (HttpClient? httpClient = null, AppVersionObservation? initialObservation = null)
		{
		_http = httpClient ?? new HttpClient (new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
		_ownsHttp = httpClient is null;
		_current = initialObservation;
		_operations = new (typeof (AppVersionDiscovery), () =>
			{
				if (_ownsHttp)
					_http.Dispose ();
			});
		}
	/// <summary>Gets the most recent successful version observation.</summary>
	/// <value>The application version, source and observation time, or null before discovery.</value>
	public AppVersionObservation? Current => Volatile.Read (ref _current);
	/// <summary>Returns the cached observation if younger than two days; otherwise refreshes it.</summary>
	/// <param name="cancellationToken">Cancels waiting or retrieval.</param>
	/// <returns>The current version observation.</returns>
	public Task<AppVersionObservation> GetAsync (CancellationToken cancellationToken = default) => DiscoverAsync (false, false, cancellationToken);
	/// <summary>Explicitly refreshes from Play Store, falling back to AppBrain.</summary>
	/// <param name="cancellationToken">Cancels waiting or retrieval.</param>
	/// <returns>The new observation; failure retains the old cache and is reported.</returns>
	public Task<AppVersionObservation> RefreshAsync (CancellationToken cancellationToken = default) => DiscoverAsync (true, false, cancellationToken);
	/// <summary>Explicitly reads the reference's community-maintained gist source.</summary>
	/// <param name="cancellationToken">Cancels waiting or retrieval.</param>
	/// <returns>The validated gist observation. This source is not selected implicitly.</returns>
	public Task<AppVersionObservation> ReadCommunityGistAsync (CancellationToken cancellationToken = default) => DiscoverAsync (true, true, cancellationToken);
	private async Task<AppVersionObservation> DiscoverAsync (bool refresh, bool gist, CancellationToken token)
		{
		using AsyncOperationGate.Operation operation = await _operations.EnterAsync (token).ConfigureAwait (false);
		token = operation.Token;
		AppVersionObservation? current = Current;
		if (!refresh && current is not null && current.ObservedAt > DateTimeOffset.UtcNow.AddDays (-2))
			return current;
		Uri[] sources = gist ? [new ("https://api.github.com/gists/e886d56531dbcde08aa11c096ab0a219")]
			: [new ("https://play.google.com/store/apps/details?id=com.panasonic.ACCsmart"), new ("https://www.appbrain.com/app/panasonic-comfort-cloud/com.panasonic.ACCsmart")];
		foreach (Uri source in sources)
			{
			token.ThrowIfCancellationRequested ();
			try
				{
				using var request = new HttpRequestMessage (HttpMethod.Get, source);
				request.Headers.UserAgent.ParseAdd ("PanasonicComfortCloudClient/0.1");
				using var deadline = CancellationTokenSource.CreateLinkedTokenSource (token);
				deadline.CancelAfter (TimeSpan.FromSeconds (30));
				using HttpResponseMessage response = await _http.SendAsync (request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait (false);
				deadline.Token.ThrowIfCancellationRequested ();
				if (!response.IsSuccessStatusCode)
					continue;
				using Stream stream = await response.Content.ReadAsStreamAsync (deadline.Token).ConfigureAwait (false);
				using var bytes = new MemoryStream ();
				var buffer = new byte[8192];
				int read;
				while ((read = await stream.ReadAsync (buffer, deadline.Token).ConfigureAwait (false)) != 0)
					{
					if (bytes.Length + read > 4 * 1024 * 1024)
						throw new ComfortCloudException ("The version page exceeded four MiB.");
					bytes.Write (buffer, 0, read);
					}

				string text = System.Text.Encoding.UTF8.GetString (bytes.ToArray ());
				string? version;
				if (gist)
					{
					Gist? document = JsonSerializer.Deserialize<Gist> (text);
					version = document?.Files is not null && document.Files.TryGetValue ("comfort-cloud-version", out GistFile? file) ? file.Content?.Trim () : null;
					}
				else if (source.Host == "play.google.com")
					{
					version = Regex.Match (text, "\\[\"(\\d+\\.\\d+\\.\\d+)\"\\]", RegexOptions.CultureInvariant, TimeSpan.FromSeconds (1)).Groups[1].Value;
					}
				else
					{
					var html = new HtmlDocument ();
					html.LoadHtml (text);
					version = html.DocumentNode.Descendants ("meta").FirstOrDefault (n => n.GetAttributeValue ("itemprop", "") == "softwareVersion")?.GetAttributeValue ("content", "");
					}

				if (version is null || !Regex.IsMatch (version, "^[0-9]+\\.[0-9]+\\.[0-9]+$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds (1)))
					continue;
				deadline.Token.ThrowIfCancellationRequested ();
				var observation = new AppVersionObservation
					{
					Version = version,
					Source = source,
					ObservedAt = DateTimeOffset.UtcNow
					};
				_operations.Commit (() => Volatile.Write (ref _current, observation));
				return observation;
				}
			catch (HttpRequestException) { /* Read-only source fallback; no cloud command is repeated. */ }
			catch (IOException) { }
			catch (JsonException) { }
			catch (OperationCanceledException) when (!token.IsCancellationRequested) { }
			}

		throw new ComfortCloudException ("No selected source supplied a valid current app version; the existing observation was retained.");
		}
	/// <summary>Disposes this helper, retaining ownership of an injected client with its caller.</summary>
	public void Dispose () => _operations.Dispose ();
	private sealed class Gist
		{
		[JsonPropertyName ("files")]
		public Dictionary<string, GistFile>? Files
			{
			get; init;
			}
		}
	private sealed class GistFile
		{
		[JsonPropertyName ("content")]
		public string? Content
			{
			get; init;
			}
		}
	}

/// <summary>Records where and when an explicit app-version lookup succeeded.</summary>
public sealed class AppVersionObservation
	{
	/// <summary>Gets the validated three-part Android version.</summary>
	/// <value>The advertised version, with no guessed fallback.</value>
	public required string Version
		{
		get; init;
		}
	/// <summary>Gets the source page or API.</summary>
	/// <value>The HTTPS source of this observation.</value>
	public required Uri Source
		{
		get; init;
		}
	/// <summary>Gets the receipt instant.</summary>
	/// <value>The UTC observation time, used for the two-day cache.</value>
	public DateTimeOffset ObservedAt
		{
		get; init;
		}
	}