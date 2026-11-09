// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Text;

using PanasonicComfortCloudClient.Authentication;

namespace PanasonicComfortCloudClient.Tests;

internal sealed class ScriptedHandler : HttpMessageHandler
	{
	internal readonly List<CapturedRequest> _requests = [];
	internal readonly Queue<Func<CapturedRequest, CancellationToken, Task<HttpResponseMessage>>> _replies = new ();
	internal int _active;
	internal int _maximumActive;
	internal void Reply (string json, HttpStatusCode status = HttpStatusCode.OK) => _replies.Enqueue ((_, _) => Task.FromResult (new HttpResponseMessage (status)
		{
		Content = new StringContent (json, Encoding.UTF8, "application/json")
		}));
	internal void Reply (Func<CapturedRequest, CancellationToken, Task<HttpResponseMessage>> action) => _replies.Enqueue (action);
	protected override async Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken token)
		{
		_ = Interlocked.Increment (ref _active);
		_maximumActive = Math.Max (_maximumActive, _active);
		try
			{
			var headers = request.Headers.ToDictionary (h => h.Key, h => string.Join (",", h.Value), StringComparer.OrdinalIgnoreCase);
			if (request.Content is not null)
				{
				foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
					{
					headers[header.Key] = string.Join (",", header.Value);
					}
				}

			var captured = new CapturedRequest (request.Method.Method, request.RequestUri!, request.Content is null ? null : await request.Content.ReadAsStringAsync (token), headers);
			_requests.Add (captured);
			return _replies.Count == 0
				? throw new InvalidOperationException ("Unexpected cloud request: " + captured.Uri.AbsolutePath)
				: await _replies.Dequeue () (captured, token);
			}
		finally
			{
			_ = Interlocked.Decrement (ref _active);
			}
		}
	}
internal sealed class CapturedRequest (string method, Uri uri, string? body, Dictionary<string, string> headers)
	{
	internal string Method
		{
		get;
		} = method;
	internal Uri Uri
		{
		get;
		} = uri;
	internal string? Body
		{
		get;
		} = body;
	internal Dictionary<string, string> Headers
		{
		get;
		} = headers;
	}
internal static class Fixture
	{
	internal static async Task<ComfortCloudClient> Client (ScriptedHandler handler, TimeSpan? interval = null)
		{
		var client = new ComfortCloudClient (new ComfortCloudOptions
			{
			AppVersion = "fixture-1",
			MinimumRequestInterval = interval ?? TimeSpan.Zero
			}, new HttpClient (handler));
		await client.RestoreSessionAsync (Session ());
		return client;
		}
	internal static ComfortCloudSession Session (bool expired = false) => new ()
		{
		AccessToken = "fake-access-never-live",
		RefreshToken = "fake-refresh-never-live",
		ClientId = "fixture-client",
		ExpiresAt = expired ? DateTimeOffset.UtcNow.AddMinutes (-1) : DateTimeOffset.UtcNow.AddHours (1)
		};
	internal const string AQUAREA_STATUS = """
        {"a2wName":"Fixture","status":{"operationStatus":1,"operationMode":1,"specialStatus":0,
        "zoneStatus":[{"zoneId":1,"operationStatus":1,"heatSet":22,"heatMin":10,"heatMax":30,"ecoHeat":-2,"comfortHeat":2},{"zoneId":2,"operationStatus":0,"heatSet":20,"heatMin":10,"heatMax":30}],
        "tankStatus":{"operationStatus":0,"heatSet":50,"heatMin":40,"heatMax":60}}}
        """;
	}