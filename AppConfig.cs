using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MP3te
{
	public static class AppConfig
	{
		private static string ConfigFile = "mp3te.ini";
		private static Dictionary<string, string> _settings = new Dictionary<string, string>();

		public static void Load()
		{
			_settings.Clear();
			if (!File.Exists(ConfigFile)) return;

			try
			{
				foreach (var line in File.ReadAllLines(ConfigFile))
				{
					if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
					var parts = line.Split(new[] { '=' }, 2);
					if (parts.Length == 2)
					{
						_settings[parts[0].Trim()] = parts[1].Trim();
					}
				}
			}
			catch (Exception ex) { Logger.Error("Config load failed: " + ex.Message); }
		}

		public static void Save()
		{
			try
			{
				var lines = _settings.Select(kvp => kvp.Key + "=" + kvp.Value).ToArray();
				File.WriteAllLines(ConfigFile, lines);
			}
			catch (Exception ex) { Logger.Error("Config save failed: " + ex.Message); }
		}

		public static string Get(string key, string defaultValue = "")
		{
			return _settings.ContainsKey(key) ? _settings[key] : defaultValue;
		}

		public static void Set(string key, string value)
		{
			_settings[key] = value;
		}

		public static int GetInt(string key, int defaultValue)
		{
			int val;
			return int.TryParse(Get(key), out val) ? val : defaultValue;
		}

		public static bool GetBool(string key, bool defaultValue)
		{
			bool val;
			return bool.TryParse(Get(key), out val) ? val : defaultValue;
		}
	}
}