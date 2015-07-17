using System.Configuration;

namespace YAWIP
{
	public class DefaultConfigs
	{
		public readonly string FilePath = "yawip.txt";
		public readonly uint RefreshRate = 3000;
		public readonly string VlcHostname = "127.0.0.1";
		public readonly uint VlcPort = 4212;

		internal DefaultConfigs()
		{
			// do nothing
		}
	}
	
	public static class Configs
	{
		public static string FilePath { get; set; }
		public static uint RefreshRate { get; set; }
		public static string VlcHostname { get; set; }
		public static uint VlcPort { get; set; }
		public static string VlcPassword { get; set; }
		public static DefaultConfigs Defaults = new DefaultConfigs();

		public static void Load()
		{
			var appSettings = ConfigurationManager.AppSettings;
			uint tmp;
			FilePath = !string.IsNullOrWhiteSpace (appSettings ["FilePath"]) ? appSettings ["FilePath"] : Defaults.FilePath;
			RefreshRate = uint.TryParse (appSettings ["RefreshRate"], out tmp) && isValidRefreshRate (tmp) ? tmp : Defaults.RefreshRate;
			VlcHostname = !string.IsNullOrWhiteSpace (appSettings ["VlcHostname"]) ? appSettings["VlcHostname"] : Defaults.VlcHostname;
			VlcPort = uint.TryParse(appSettings ["VlcPort"], out tmp) && isValidVlcPort(tmp) ? tmp : Defaults.VlcPort;
			VlcPassword = appSettings ["VlcPassword"];
		}

		public static void Save()
		{
			var configFile = ConfigurationManager.OpenExeConfiguration (ConfigurationUserLevel.None);
			var appSettings = configFile.AppSettings.Settings;

			if (appSettings ["FilePath"] == null)
				appSettings.Add ("FilePath", FilePath);
			
			if (appSettings ["RefreshRate"] == null)
				appSettings.Add ("RefreshRate", RefreshRate.ToString());
			
			if (appSettings ["VlcHostname"] == null)
				appSettings.Add ("VlcHostname", VlcHostname);
			
			if (appSettings ["VlcPort"] == null)
				appSettings.Add ("VlcPort", VlcPort.ToString());

			if (appSettings ["VlcPassword"] == null)
				appSettings.Add ("VlcPassword", VlcPassword);

			appSettings ["FilePath"].Value = FilePath;
			appSettings ["RefreshRate"].Value = RefreshRate.ToString();
			appSettings ["VlcHostname"].Value = VlcHostname;
			appSettings ["VlcPort"].Value = VlcPort.ToString();
			appSettings ["VlcPassword"].Value = VlcPassword;

			configFile.Save (ConfigurationSaveMode.Full);
		}

		public static void Reset()
		{
			FilePath = Defaults.FilePath;
			RefreshRate = Defaults.RefreshRate;
			VlcHostname = Defaults.VlcHostname;
			VlcPort = Defaults.VlcPort;
			VlcPassword = string.Empty;
		}

		static bool isValidRefreshRate(uint refreshrate)
		{
			return refreshrate != 0;
		}

		static bool isValidVlcPort(uint vlcport)
		{
			return vlcport < 65537;
		}
	}

}

