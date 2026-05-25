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
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show("فشل حفظ إعدادات التطبيق:\n" + ex.Message, "خطأ", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                }
            }
        }

        public static string ReceiptPrintMode
        {
            get
            {
                return ConfigurationManager.AppSettings["ReceiptPrintMode"] ?? "Detailed";
            }
            set
            {
                try
                {
                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    if (config.AppSettings.Settings["ReceiptPrintMode"] != null)
                    {
                        config.AppSettings.Settings["ReceiptPrintMode"].Value = value;
                    }
                    else
                    {
                        config.AppSettings.Settings.Add("ReceiptPrintMode", value);
                    }
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show("فشل حفظ إعدادات التطبيق:\n" + ex.Message, "خطأ", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                }
            }
        }
    }
}
