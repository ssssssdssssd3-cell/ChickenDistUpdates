using System.Configuration;

namespace ChickenDist.Core
{
    public static class AppConfig
    {
        public static string CompanyName
        {
            get
            {
                return ConfigurationManager.AppSettings["CompanyName"] ?? "شركة توزيع الكتاكيت";
            }
            set
            {
                try
                {
                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    if (config.AppSettings.Settings["CompanyName"] != null)
                    {
                        config.AppSettings.Settings["CompanyName"].Value = value;
                    }
                    else
                    {
                        config.AppSettings.Settings.Add("CompanyName", value);
                    }
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                }
                catch { }
            }
        }
    }
}
