// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#if NETFRAMEWORK
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;

using NUnit.Framework;

using PanasonicComfortCloudClient.Protocol;

namespace PanasonicComfortCloudClient.Tests;

[TestFixture]
public sealed class FrameworkGetHandlerTests
	{
	[Test]
	public async Task BodylessGetRetainsProtocolHeadersAndErrorResponse ()
		{
		using var server = new LoopbackServer ("400 Bad Request", "{\"code\":4008}");
		using var client = new HttpClient (new BodylessGetHandler (TimeSpan.FromSeconds (5)));
		using HttpRequestMessage request = Get (server.Uri);
		using HttpResponseMessage response = await client.SendAsync (request);
		var headers = await server.Request;
		Assert.That (headers, Does.Contain ("GET /read HTTP/1.1"));
		Assert.That (headers.ToLowerInvariant (), Does.Contain ("content-type: application/json"));
		Assert.That (headers, Does.Contain ("x-app-version: fixture"));
		Assert.That (response.StatusCode, Is.EqualTo (HttpStatusCode.BadRequest));
		Assert.That (await response.Content.ReadAsStringAsync (), Is.EqualTo ("{\"code\":4008}"));
		}

	[Test]
	public async Task RedirectIsReturnedWithoutFollowingIt ()
		{
		using var server = new LoopbackServer ("302 Found", "", "Location: http://127.0.0.1:1/not-followed\r\n");
		using var client = new HttpClient (new BodylessGetHandler (TimeSpan.FromSeconds (5)));
		using HttpRequestMessage request = Get (server.Uri);
		using HttpResponseMessage response = await client.SendAsync (request);
		Assert.That (response.StatusCode, Is.EqualTo (HttpStatusCode.Redirect));
		Assert.That (response.Headers.Location!.AbsolutePath, Is.EqualTo ("/not-followed"));
		}

	[Test]
	public async Task CancellationAbortsPendingResponseHeaders ()
		{
		using var server = new LoopbackServer (null, "");
		using var client = new HttpClient (new BodylessGetHandler (TimeSpan.FromSeconds (5)));
		using HttpRequestMessage request = Get (server.Uri);
		using var cancellation = new CancellationTokenSource ();
		Task<HttpResponseMessage> pending = client.SendAsync (request, cancellation.Token);
		Assert.That (await Task.WhenAny (server.Request, Task.Delay (5000)), Is.SameAs (server.Request));
		cancellation.Cancel ();
		_ = await Assert.CatchAsync<OperationCanceledException> (async () => await pending);
		}

	private static HttpRequestMessage Get (Uri uri)
		{
		var request = new HttpRequestMessage (HttpMethod.Get, uri)
			{
			Content = new ByteArrayContent ([])
			};
		request.Content.Headers.ContentType = new ("application/json");
		_ = request.Headers.TryAddWithoutValidation ("x-app-version", "fixture");
		return request;
		}

	private sealed class LoopbackServer : IDisposable
		{
		private readonly TcpListener _listener = new (IPAddress.Loopback, 0);
		private readonly CancellationTokenSource _stop = new ();
		private readonly Task _worker;
		private readonly TaskCompletionSource<string> _request = new (TaskCreationOptions.RunContinuationsAsynchronously);
		internal Task<string> Request => _request.Task;
		internal Uri Uri
			{
			get;
			}
		internal LoopbackServer (string? status, string body, string extraHeaders = "")
			{
			_listener.Start ();
			Uri = new ("http://127.0.0.1:" + ((IPEndPoint)_listener.LocalEndpoint).Port + "/read");
			_worker = Serve (status, body, extraHeaders);
			}
		private async Task Serve (string? status, string body, string extraHeaders)
			{
			try
				{
				using TcpClient connection = await _listener.AcceptTcpClientAsync ();
				using NetworkStream stream = connection.GetStream ();
				using var reader = new StreamReader (stream, Encoding.ASCII, false, 1024, true);
				var headers = new StringBuilder ();
				string? line;
				while (!string.IsNullOrEmpty (line = await reader.ReadLineAsync ()))
					{
					_ = headers.AppendLine (line);
					}

				_ = _request.TrySetResult (headers.ToString ());
				if (status is null)
					{
					try
						{
						await Task.Delay (Timeout.Infinite, _stop.Token);
						}
					catch (OperationCanceledException)
						{
						}

					return;
					}

				var response = Encoding.ASCII.GetBytes ("HTTP/1.1 " + status + "\r\nContent-Type: application/json\r\nContent-Length: " + Encoding.ASCII.GetByteCount (body) + "\r\nConnection: close\r\n" + extraHeaders + "\r\n" + body);
				await stream.WriteAsync (response, 0, response.Length);
				}
			catch (Exception ex)
				{
				_ = _request.TrySetException (ex);
				}
			}
		public void Dispose ()
			{
			_listener.Stop ();
			_stop.Cancel ();
			if (!_worker.Wait (TimeSpan.FromSeconds (5)))
				{
				throw new TimeoutException ("Loopback server did not stop.");
				}

			_stop.Dispose ();
			}
		}
	}
#endif