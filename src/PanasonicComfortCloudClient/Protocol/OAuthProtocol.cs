// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace PanasonicComfortCloudClient.Protocol;

internal static class OAuthProtocol
	{
	internal const string CLIENT_ID = "Xmy6xIYIitMxngjB2rHvlm6HSDNnaMJx";
	internal const string AUTH0_CLIENT = "eyJuYW1lIjoiQXV0aDAuQW5kcm9pZCIsImVudiI6eyJhbmRyb2lkIjoiMzAifSwidmVyc2lvbiI6IjIuOS4zIn0=";
	internal const string REDIRECT_URI = "panasonic-iot-cfc://authglb.digital.panasonic.com/android/com.panasonic.ACCsmart/callback";
	internal const string SCOPE = "openid offline_access comfortcloud.control a2w.control";
	internal static string RandomToken ()
		{
		var bytes = new byte[32];
		using var random = RandomNumberGenerator.Create ();
		random.GetBytes (bytes);
		return Base64Url (bytes);
		}
	internal static string Challenge (string verifier) => Base64Url (SHA256.HashData (Encoding.ASCII.GetBytes (verifier)));
	private static string Base64Url (byte[] bytes) => Convert.ToBase64String (bytes).TrimEnd ('=').Replace ('+', '-').Replace ('/', '_');
	internal static Dictionary<string, string> Query (string query)
		{
		var values = new Dictionary<string, string> (StringComparer.Ordinal);
		foreach (var item in query.TrimStart ('?').Split ('&'))
			{
			if (item.Length == 0)
				{
				continue;
				}

			var parts = item.Split (['='], 2);
			var name = Uri.UnescapeDataString (parts[0].Replace ("+", " "));
			var value = parts.Length == 2 ? Uri.UnescapeDataString (parts[1].Replace ("+", " ")) : "";
			if (values.ContainsKey (name))
				{
				throw new ComfortCloudException ("Duplicate OAuth callback parameter.");
				}

			values.Add (name, value);
			}

		return values;
		}
	}