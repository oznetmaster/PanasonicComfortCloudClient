// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK
using System.Net;
using System.Net.Http;

namespace PanasonicComfortCloudClient.Protocol;

// Framework HttpClientHandler rejects HttpContent on GET, even when empty.
// Panasonic nevertheless requires Content-Type. HttpWebRequest permits that
// header without opening a request-body stream and is available on Mono too.
internal sealed class BodylessGetHandler (TimeSpan timeout) : DelegatingHandler (new HttpClientHandler
	{
	AllowAutoRedirect = false,
	UseCookies = false
	})
	{
	protected override async Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken token)
		{
		if (request.Method != HttpMethod.Get || request.Content?.Headers.ContentLength != 0)
			{
			return await base.SendAsync (request, token).ConfigureAwait (false);
			}

		HttpWebRequest web = WebRequest.CreateHttp (request.RequestUri!);
		web.Method = "GET";
		web.AllowAutoRedirect = false;
		web.ReadWriteTimeout = (int)timeout.TotalMilliseconds;
		web.ContentType = request.Content.Headers.ContentType?.ToString ();
		web.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
		foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
			{
			var value = string.Join (", ", header.Value);
			if (header.Key.Equals ("Accept", StringComparison.OrdinalIgnoreCase))
				{
				web.Accept = value;
				}
			else if (header.Key.Equals ("User-Agent", StringComparison.OrdinalIgnoreCase))
				{
				web.UserAgent = value;
				}
			else
				{
				web.Headers[header.Key] = value;
				}
			}

		CancellationTokenRegistration cancellation = token.Register (web.Abort);
		bool transferred = false;
		HttpWebResponse? received = null;
		try
			{
			try
				{
				received = (HttpWebResponse)await web.GetResponseAsync ().ConfigureAwait (false);
				}
			catch (WebException ex) when (ex.Response is HttpWebResponse response)
				{
				received = response;
				}
			catch (WebException) when (token.IsCancellationRequested)
				{
				throw new OperationCanceledException (token);
				}
			catch (WebException ex)
				{
				throw new HttpRequestException ("The cloud GET request failed.", ex);
				}

			token.ThrowIfCancellationRequested ();
			var result = new HttpResponseMessage (received.StatusCode)
				{
				RequestMessage = request,
				Version = received.ProtocolVersion,
				Content = new OwnedResponseContent (received, cancellation)
				};
			foreach (var name in received.Headers.AllKeys)
				{
				if (!result.Headers.TryAddWithoutValidation (name, received.Headers.GetValues (name)))
					{
					_ = result.Content.Headers.TryAddWithoutValidation (name, received.Headers.GetValues (name));
					}
				}

			transferred = true;
			return result;
			}
		finally
			{
			if (!transferred)
				{
				cancellation.Dispose ();
				received?.Dispose ();
				}
			}
		}

	private sealed class OwnedResponseContent (HttpWebResponse response, CancellationTokenRegistration cancellation)
		  : StreamContent (response.GetResponseStream () ?? new MemoryStream ())
		{
		protected override void Dispose (bool disposing)
			{
			try
				{
				base.Dispose (disposing);
				}
			finally
				{
				if (disposing)
					{
					cancellation.Dispose ();
					response.Dispose ();
					}
				}
			}
		}
	}
#endif