using System;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;

namespace ChickenDist.Core
{
    public class ScaleService : IDisposable
    {
        private static ScaleService _instance;
        public static ScaleService Instance => _instance ?? (_instance = new ScaleService());

        private SerialPort _serialPort;
        private Thread _readThread;
        private bool _isRunning;
        private string _buffer = "";

        public event Action<decimal, bool> WeightChanged; // event parameters: (weight, isStable)
        public event Action<string> ErrorOccurred;

        public decimal CurrentWeight { get; private set; }
        public bool IsStable { get; private set; }
        public bool IsConnected => _serialPort != null && _serialPort.IsOpen;

        private ScaleService() { }

        public bool Connect(string portName, int baudRate)
        {
            if (IsConnected)
                Disconnect();

            try
            {
                _serialPort = new SerialPort
                {
                    PortName = portName,
                    BaudRate = baudRate,
                    Parity = Parity.None,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                _serialPort.Open();
                _buffer = "";
                
                _isRunning = true;
                _readThread = new Thread(ReadDataLoop)
                {
                    IsBackground = true,
                    Name = "ScaleReadThread"
                };
                _readThread.Start();

                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"تعذر فتح منفذ الميزان {portName}: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            _isRunning = false;
            
            if (_readThread != null && _readThread.IsAlive)
            {
                try { _readThread.Join(500); } catch { }
            }

            if (_serialPort != null)
            {
                try
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                }
                catch { }
                finally
                {
                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }
        }

        private void ReadDataLoop()
        {
            byte[] readBuffer = new byte[1024];
            while (_isRunning && _serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    int bytesRead = _serialPort.Read(readBuffer, 0, readBuffer.Length);
                    if (bytesRead > 0)
                    {
                        string chunk = System.Text.Encoding.ASCII.GetString(readBuffer, 0, bytesRead);
                        _buffer += chunk;

                        // معالجة السطور الكاملة
                        int newlineIndex;
                        while ((newlineIndex = _buffer.IndexOf('\n')) >= 0)
                        {
                            string line = _buffer.Substring(0, newlineIndex).Trim();
                            _buffer = _buffer.Substring(newlineIndex + 1);

                            if (!string.IsNullOrEmpty(line))
                            {
                                ParseWeightLine(line);
                            }
                        }
                    }
                }
                catch (TimeoutException) { }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        ErrorOccurred?.Invoke($"حدث خطأ أثناء القراءة من الميزان: {ex.Message}");
                        // محاولة إعادة الاتصال التلقائي
                        Thread.Sleep(2000);
                    }
                }
                Thread.Sleep(50); // تقليل استهلاك المعالج
            }
        }

        private void ParseWeightLine(string line)
        {
            try
            {
                // بروتوكولات الموازين الشائعة ترسل الوزن برقم عشري
                // مثال: "ST,GS, +  2.500kg" أو "wn  2.500" أو "  2.500"
                // سنقوم بالبحث عن أول رقم عشري يحتوي على فاصلة عشرية أو رقم صحيح في السطر
                
                // البحث عن رقم عشري (قد يسبقه إشارة موجب أو سالب ومسافات)
                Match match = Regex.Match(line, @"[-+]?\s*\d+\.\d+");
                if (match.Success)
                {
                    string cleanVal = match.Value.Replace(" ", "");
                    if (decimal.TryParse(cleanVal, out decimal weight))
                    {
                        // موازين كثيرة ترسل إشارة الاستقرار ST أو US في بداية السطر
                        // ST = Stable (مستقر)، US/UT = Unstable (غير مستقر)
                        bool isStable = true;
                        if (line.Contains("US") || line.Contains("UT") || line.Contains("OL"))
                        {
                            isStable = false;
                        }

                        CurrentWeight = weight;
                        IsStable = isStable;
                        
                        WeightChanged?.Invoke(weight, isStable);
                    }
                }
                else
                {
                    // محاولة قراءة الأرقام العادية فقط في حال لم توجد نقطة عشرية (مثلاً: 2500 جرام)
                    Match matchInt = Regex.Match(line, @"\d+");
                    if (matchInt.Success && decimal.TryParse(matchInt.Value, out decimal grams))
                    {
                        // إذا كانت القيمة كبيرة جداً كأرقام صحيحة، فقد تكون بالجرام، نحولها لكيلوجرام (اختياري)
                        // ولكن عادة نعتبر الرقم المكتوب هو القيمة المباشرة.
                        bool isStable = !line.Contains("US");
                        CurrentWeight = grams;
                        IsStable = isStable;
                        WeightChanged?.Invoke(grams, isStable);
                    }
                }
            }
            catch { }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
