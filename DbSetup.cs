using System;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

namespace ChickenDist
{
    class DbSetup
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("========================================");
            Console.WriteLine("🐣 معالج تهيئة قاعدة بيانات شركة توزيع الكتاكيت");
            Console.WriteLine("========================================");

            string masterConnStr = "Data Source=.;Initial Catalog=master;Integrated Security=True;Connect Timeout=15;";
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "Script.sql");
            
            // Fallback path in case of running from bin directory
            if (!File.Exists(scriptPath))
            {
                scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Database", "Script.sql");
            }
            if (!File.Exists(scriptPath))
            {
                scriptPath = "Database/Script.sql";
            }

            if (!File.Exists(scriptPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ لم يتم العثور على ملف السكربت في: " + Path.GetFullPath(scriptPath));
                Console.ResetColor();
                return;
            }

            Console.WriteLine("⚙️ قراءة ملف السكربت من: " + Path.GetFullPath(scriptPath));

            try
            {
                string script = File.ReadAllText(scriptPath);
                
                // Split script into commands on 'GO' lines
                string[] commands = Regex.Split(
                    script,
                    @"^\s*GO\s*$",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase
                );

                Console.WriteLine("🔌 الاتصال بـ SQL Server...");
                using (var conn = new SqlConnection(masterConnStr))
                {
                    conn.Open();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ تم الاتصال بخادم SQL Server بنجاح.");
                    Console.ResetColor();

                    int successCount = 0;
                    int errorCount = 0;

                    foreach (var cmdText in commands)
                    {
                        string trimmedCmd = cmdText.Trim();
                        if (string.IsNullOrWhiteSpace(trimmedCmd)) continue;

                        try
                        {
                            using (var cmd = new SqlCommand(trimmedCmd, conn))
                            {
                                cmd.ExecuteNonQuery();
                                successCount++;
                            }
                        }
                        catch (SqlException ex)
                        {
                            errorCount++;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("⚠️ تنبيه أثناء تنفيذ جزء من الاستعلام:\n" + ex.Message);
                            Console.ResetColor();
                        }
                    }

                    Console.WriteLine("========================================");
                    if (errorCount == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("🎉 تم إنشاء وتهيئة قاعدة البيانات بنجاح تام!");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("ℹ️ اكتمل التشغيل مع وجود " + errorCount + " تنبيهات/أخطاء بسيطة (غالباً بسبب إسقاط جداول غير موجودة سابقاً).");
                    }
                    Console.ResetColor();
                    Console.WriteLine("========================================");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ حدث خطأ فادح أثناء تهيئة قاعدة البيانات:\n" + ex.Message);
                Console.ResetColor();
            }

            Console.WriteLine("\nاضغط على أي مفتاح للخروج...");
            try { Console.ReadKey(); } catch { }
        }
    }
}
