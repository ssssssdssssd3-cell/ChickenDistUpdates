using System;
using System.Net;
using System.IO;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = "https://raw.githubusercontent.com/ssssssdssssd3-cell/ChickenDistUpdates/main/ChickenDist.bin";
            string output = "e:\\New folder\\حسان\\حسان\\ChickenDist\\ChickenDist_New.exe";
            
            Console.WriteLine("Starting download from: " + url);
            Console.WriteLine("Saving to: " + output);
            
            try
            {
                // Simulate exact UpdateManager settings
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | (SecurityProtocolType)12288;
                
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");
                    client.DownloadFile(url, output);
                }
                
                Console.WriteLine("SUCCESS! Download completed successfully.");
                if (File.Exists(output))
                {
                    File.Delete(output);
                    Console.WriteLine("Cleaned up test file.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR!");
                Console.WriteLine("Exception Type: " + ex.GetType().FullName);
                Console.WriteLine("Exception Message: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("Inner Exception Type: " + ex.InnerException.GetType().FullName);
                    Console.WriteLine("Inner Exception Message: " + ex.InnerException.Message);
                }
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
