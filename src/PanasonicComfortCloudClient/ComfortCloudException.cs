// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;

namespace PanasonicComfortCloudClient;

/// <summary>Reports an HTTP, authentication or cloud-protocol failure without retaining sensitive bodies.</summary>
/// <remarks>Creates a cloud error with optional transport and protocol details.</remarks>
/// <param name="message">A description containing no credentials or response body.</param>
/// <param name="statusCode">The HTTP status, or null for local validation failures.</param>
/// <param name="errorCode">The cloud error identifier, or null when absent.</param>
/// <param name="retryAfter">The server-requested retry delay; writes are not retried automatically.</param>
public sealed class ComfortCloudException (string message, HttpStatusCode? statusCode = null, string? errorCode = null, TimeSpan? retryAfter = null) : Exception (message)
	{
	/// <summary>Gets the HTTP status.</summary>
	/// <value>The response status, or null when no HTTP response exists.</value>
	public HttpStatusCode? StatusCode
		{
		get;
		} = statusCode;
	/// <summary>Gets the cloud error identifier.</summary>
	/// <value>The protocol code, or null when absent.</value>
	public string? ErrorCode
		{
		get;
		} = errorCode;
	/// <summary>Gets the server-requested retry interval.</summary>
	/// <value>The Retry-After delay, or null when not supplied.</value>
	public TimeSpan? RetryAfter
		{
		get;
		} = retryAfter;
	}