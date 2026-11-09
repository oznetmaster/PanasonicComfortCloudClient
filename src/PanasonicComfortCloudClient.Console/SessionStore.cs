// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using PanasonicComfortCloudClient.Authentication;

namespace PanasonicComfortCloudClient.ConsoleApp;

internal sealed class SessionStore (string path)
	{
	private static readonly byte[] _entropy = Encoding.UTF8.GetBytes ("PanasonicComfortCloudClient.Console.Session.v1");
	private readonly string _path = Path.GetFullPath (path);
	internal ComfortCloudSession? Load ()
		{
		if (!File.Exists (_path))
			{
			return null;
			}

		var data = ProtectedData.Unprotect (File.ReadAllBytes (_path), _entropy, DataProtectionScope.CurrentUser);
		try
			{
			return JsonSerializer.Deserialize<ComfortCloudSession> (data) ?? throw new InvalidDataException ("Session file is empty.");
			}
		finally
			{
			CryptographicOperations.ZeroMemory (data);
			}
		}
	internal void Save (ComfortCloudSession session)
		{
		var plain = JsonSerializer.SerializeToUtf8Bytes (session);
		byte[] protectedBytes;
		try
			{
			protectedBytes = ProtectedData.Protect (plain, _entropy, DataProtectionScope.CurrentUser);
			}
		finally
			{
			CryptographicOperations.ZeroMemory (plain);
			}

		_ = Directory.CreateDirectory (Path.GetDirectoryName (_path)!);
		string temporary = _path + "." + Guid.NewGuid ().ToString ("N") + ".tmp";
		try
			{
			using (var file = new FileStream (temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
				{
				file.Write (protectedBytes);
				file.Flush (true);
				}

			if (File.Exists (_path))
				{
				File.Replace (temporary, _path, null);
				}
			else
				{
				File.Move (temporary, _path);
				}
			}
		finally
			{
			if (File.Exists (temporary))
				{
				File.Delete (temporary);
				}
			}
		}
	internal void Delete ()
		{
		if (File.Exists (_path))
			{
			File.Delete (_path);
			}
		}
	}