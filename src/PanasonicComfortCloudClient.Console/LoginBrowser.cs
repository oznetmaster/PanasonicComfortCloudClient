// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PanasonicComfortCloudClient.ConsoleApp;

internal static class LoginBrowser
	{
	internal static Task<Uri> OpenAsync (Uri authorizationUri, CancellationToken token)
		{
		var completion = new TaskCompletionSource<Uri> (TaskCreationOptions.RunContinuationsAsynchronously);
		var thread = new Thread (() =>
		{
			try
				{
				Application.EnableVisualStyles ();
				using var form = new Form
					{
					Text = "Panasonic ID — Comfort Cloud test client",
					Width = 580,
					Height = 780,
					StartPosition = FormStartPosition.CenterScreen
					};
				using var view = new WebView2
					{
					Dock = DockStyle.Fill
					};
				form.Controls.Add (view);
				form.FormClosed += (_, _) => completion.TrySetCanceled (token);
				using CancellationTokenRegistration cancellation = token.Register (() =>
				{
					if (form.IsHandleCreated)
						{
						_ = form.BeginInvoke (() => form.Close ());
						}
				});
				form.Shown += async (_, _) =>
				  {
					  try
						  {
						  var profile = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "PanasonicComfortCloudClient", "BrowserProfile");
						  CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync (null, profile);
						  await view.EnsureCoreWebView2Async (environment);
						  view.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
						  view.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
						  view.NavigationStarting += (_, args) =>
							 {
								 if (!args.Uri.StartsWith ("panasonic-iot-cfc:", StringComparison.OrdinalIgnoreCase))
									 {
									 return;
									 }

								 args.Cancel = true;
								 _ = Uri.TryCreate (args.Uri, UriKind.Absolute, out Uri? callback)
									 ? completion.TrySetResult (callback)
									 : completion.TrySetException (new InvalidDataException ("Panasonic returned an invalid callback URI."));

								 form.Close ();
							 };
						  view.Source = authorizationUri;
						  }
					  catch (Exception ex)
						  {
						  _ = completion.TrySetException (ex);
						  form.Close ();
						  }
				  };
				Application.Run (form);
				}
			catch (Exception ex)
				{
				_ = completion.TrySetException (ex);
				}
		})
			{
			IsBackground = true,
			Name = "Panasonic login browser"
			};
		thread.SetApartmentState (ApartmentState.STA);
		thread.Start ();
		return completion.Task;
		}
	}