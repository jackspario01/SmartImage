﻿#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SmartImage.Engines.SauceNao;
using SmartImage.Model;
using SmartImage.Searching;
using CommandLine;
using SimpleCore;
using SimpleCore.Utilities;

#endregion

namespace SmartImage
{
	/**
	 * Single file executable build dir
	 * 
	 * C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Release\netcoreapp3.1\win10-x64
	 * C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Release\netcoreapp3.1\win10-x64\publish
	 *
	 * Single file publish command
	 *
	 * dotnet publish -c Release -r win10-x64
	 * dotnet publish -c Release -r win10-x64 --self-contained
	 *
	 * Legacy registry keys
	 *
	 * Computer\HKEY_CLASSES_ROOT\*\shell\SmartImage
	 * Computer\HKEY_CURRENT_USER\Software\SmartImage
	 * "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"
	 *
	 * Copy build
	 *
	 * copy SmartImage.exe C:\Library /Y
	 * copy SmartImage.exe C:\Users\Deci\Desktop /Y
	 * copy C:\Users\Deci\RiderProjects\SmartImage\SmartImage\bin\Release\netcoreapp3.1\win10-x64\publish\SmartImage.exe C:\Users\Deci\Desktop /Y
	 * 
	 * Bundle extract folder
	 * 
	 * C:\Users\Deci\AppData\Local\Temp\.net\SmartImage
	 * DOTNET_BUNDLE_EXTRACT_BASE_DIR
	 *
	 *
	 * nuget pack -Prop Configuration=Release
	 *
	 * C:\Library\Nuget
	 * dotnet pack -c Release -o %cd%
	 * dotnet nuget push "*.nupkg"
	 * del *.nupkg & dotnet pack -c Release -o %cd%
	 */
	public static class Program
	{
		//  ____                       _   ___
		// / ___| _ __ ___   __ _ _ __| |_|_ _|_ __ ___   __ _  __ _  ___
		// \___ \| '_ ` _ \ / _` | '__| __|| || '_ ` _ \ / _` |/ _` |/ _ \
		//  ___) | | | | | | (_| | |  | |_ | || | | | | | (_| | (_| |  __/
		// |____/|_| |_| |_|\__,_|_|   \__|___|_| |_| |_|\__,_|\__, |\___|
		//                                                     |___/


		// todo: make the console interaction and key reading a function instead of copy-pasting

		private readonly struct Option
		{
			/// <summary>
			/// If this function returns <c>null</c>, <see cref="Program.AlternateMenu"/> will not return
			/// </summary>
			public Func<object> Function { get; }

			/// <summary>
			/// Name
			/// </summary>
			public string Name { get; }

			public Option(string name, Func<object> func)
			{
				this.Name = name;
				this.Function = func;
			}
		}


		/// <summary>
		/// Runs when no arguments are given (and when the executable is double-clicked)
		/// </summary>
		/// <returns></returns>
		private static object AlternateMenu()
		{

			ConsoleKeyInfo cki;

			var options = new[]
			{
				new Option("* Select image", () =>
				{
					Console.WriteLine("Drag and drop the image here.");
					Console.Write("Image: ");

					var img = Console.ReadLine();
					img = RuntimeInfo.CleanString(img);

					return img;
				}),
				
				new Option("Add/remove context menu integration", () =>
				{
					bool ctx = RuntimeInfo.IsContextMenuAdded;

					if (!ctx) {
						CliParse.ContextMenuCommand.Add();
						CliOutput.WriteSuccess("Added to context menu");
					}
					else {
						CliParse.ContextMenuCommand.Remove();
						CliOutput.WriteSuccess("Removed from context menu");
					}

					Thread.Sleep(TimeSpan.FromSeconds(1));
					return null;
				}),
			};

			do {
				Console.Clear();
				Console.WriteLine(RuntimeInfo.NAME_BANNER);

				for (int i = 0; i < options.Length; i++) {
					var opt = options[i];
					Console.WriteLine("{0}: {1}", i, opt.Name);
				}

				Console.WriteLine();

				CliOutput.WriteSuccess("Enter the option number or escape to quit.");

				while (!Console.KeyAvailable) {
					// Block until input is entered.
				}


				// Key was read

				cki = Console.ReadKey(true);
				char keyChar = cki.KeyChar;

				if (Char.IsNumber(keyChar)) {
					int idx = (int) Char.GetNumericValue(cki.KeyChar);

					if (idx < options.Length && idx >= 0) {
						var option = options[idx];
						var fn = option.Function();

						if (fn != null) {
							return fn;
						}
					}
				}
			} while (cki.Key != ConsoleKey.Escape);


			return null;
		}


		/**
		 * Entry point
		 */
		private static void Main(string[] args)
		{
			Console.Title = RuntimeInfo.NAME;

			if (args == null || args.Length == 0) {
				var obj = AlternateMenu();
				Console.Clear();

				if (obj == null) {
					Console.WriteLine("Exiting");
					return;
				}

				// Image
				args = new[] {(string) obj};
			}

			Console.WriteLine(RuntimeInfo.NAME_BANNER);

			RuntimeInfo.EnsurePathIntegration();

			CliParse.ReadArguments(args);

			var img = RuntimeInfo.Config.Image;

			//CliOutput.WriteInfo("Image: {0}", img);

			bool run = img != null;

			if (run) {
				var sr = new SearchResults();
				var ok = Search.RunSearch(img, ref sr);

				if (!ok) {
					CliOutput.WriteError("Search failed");
					return;
				}

				var results = sr.Results;

				// Console.WriteLine("Elapsed: {0:F} sec", result.Duration.TotalSeconds);

				ConsoleKeyInfo cki;

				do {
					Console.Clear();

					for (int i = 0; i < sr.Results.Length; i++) {
						var r = sr.Results[i];

						var tag = (i).ToString();

						if (r != null) {
							string str = r.Format(tag);

							Console.Write(str);
						}
						else {
							Console.WriteLine("{0} - ...", tag);
						}
					}

					Console.WriteLine();

					// Exit
					if (RuntimeInfo.Config.AutoExit) {
						SearchConfig.Cleanup();
						return;
					}

					CliOutput.WriteSuccess("Enter the result number to open or escape to quit.");


					while (!Console.KeyAvailable) {
						// Block until input is entered.
					}


					// Key was read

					cki = Console.ReadKey(true);
					char keyChar = cki.KeyChar;

					if (Char.IsNumber(keyChar)) {
						int idx = (int) Char.GetNumericValue(cki.KeyChar);

						if (idx < results.Length && idx >= 0) {
							var res = results[idx];
							WebAgent.OpenUrl(res.Url);
						}
					}
				} while (cki.Key != ConsoleKey.Escape);

				// Exit
				SearchConfig.Cleanup();
			}
			else {
				//CliOutput.WriteInfo("Exited");
			}
		}
	}
}