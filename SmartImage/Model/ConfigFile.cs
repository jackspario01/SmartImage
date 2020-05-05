using System;
using System.Collections.Generic;
using Neocmd;
using SmartImage.Utilities;

namespace SmartImage.Model
{
	public sealed class ConfigFile
	{
		public string FileName { get; }

		public IDictionary<string, string> Config { get; }

		public ConfigFile(string fileName)
		{
			FileName = fileName;
			Config   = ExplorerSystem.ReadMap(fileName);
		}

		public void Write<T>(string name, T value)
		{
			var valStr = value.ToString();
			if (!Config.ContainsKey(name)) {
				Config.Add(name, valStr);
			}
			else {
				Config[name] = valStr;
			}

			Update();
		}


		public T Read<T>(string name, bool setDefaultIfNull = false, T defaultValue = default)
		{
			if (!Config.ContainsKey(name)) {
				Config.Add(name, string.Empty);
				Update();
			}

			var rawValue = Config[name];

			if (setDefaultIfNull && String.IsNullOrWhiteSpace((string) rawValue)) {
				Write(name, defaultValue.ToString());
				rawValue = Read<string>(name);
			}

			if (typeof(T).IsEnum) {
				Enum.TryParse(typeof(T), (string) rawValue, out var e);
				return (T) e;
			}

			return (T) (object) rawValue;
		}

		private void Update() => ExplorerSystem.WriteMap(Config, FileName);
	}
}