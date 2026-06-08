using System.Configuration;

namespace ChickenDist.Core
{
    public static class AppConfig
    {
        private static string Get(string key, string def)
        {
            return ConfigurationManager.AppSettings[key] ?? def;
        }

        private static void Set(string key, string value)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings[key] != null)
                {
                    config.AppSettings.Settings[key].Value = value;
                }
                else
                {
                    config.AppSettings.Settings.Add(key, value);
                }
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch { }
        }

        public static string CompanyName
        {
            get => Get("CompanyName", "شركة توزيع الكتاكيت");
            set => Set("CompanyName", value);
        }

        public static bool ScaleEnabled
        {
            get => bool.TryParse(Get("ScaleEnabled", "false"), out bool res) && res;
            set => Set("ScaleEnabled", value.ToString().ToLower());
        }

        public static string ScaleComPort
        {
            get => Get("ScaleComPort", "COM3");
            set => Set("ScaleComPort", value);
        }

        public static int ScaleBaudRate
        {
            get => int.TryParse(Get("ScaleBaudRate", "9600"), out int res) ? res : 9600;
            set => Set("ScaleBaudRate", value.ToString());
        }

        public static bool ThermalPrinterEnabled
        {
            get => bool.TryParse(Get("ThermalPrinterEnabled", "false"), out bool res) && res;
            set => Set("ThermalPrinterEnabled", value.ToString().ToLower());
        }

        public static string ThermalPrinterName
        {
            get => Get("ThermalPrinterName", "");
            set => Set("ThermalPrinterName", value);
        }

        public static int ThermalPaperWidth
        {
            get => int.TryParse(Get("ThermalPaperWidth", "80"), out int res) ? res : 80;
            set => Set("ThermalPaperWidth", value.ToString());
        }
    }
}
