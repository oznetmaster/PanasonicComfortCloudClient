// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace PanasonicComfortCloudClient.Protocol;

internal static class RequestSigning
	{
	internal static string Timestamp (DateTimeOffset instant) => instant.UtcDateTime.ToString ("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
	internal static string ApiKey (string timestamp, string accessToken)
		{
		var instant = DateTimeOffset.ParseExact (timestamp, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
		var milliseconds = instant.ToUnixTimeMilliseconds ().ToString (CultureInfo.InvariantCulture);
		var data = Encoding.UTF8.GetBytes ("Comfort Cloud521325fb2dd486bf4831b47644317fca" + milliseconds + "Bearer " + accessToken);

		var hash = Convert.ToHexStringLower (SHA256.HashData (data));
		return hash.Insert (9, "cfc");
		}
	internal static string DevicePath (string guid) => Uri.EscapeDataString (guid.Replace ("/", "f"));
	}