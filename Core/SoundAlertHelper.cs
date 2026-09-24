using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace ChickenDist.Core
{
    /// <summary>
    /// مساعد التنبيهات الصوتية المتقدمة لطلبات المتجر الإلكتروني والإشعارات الهامة
    /// ينتج نغمة تنبيه لطيفة وواضحة (Chime) تعمل على كافة أجهزة ويندوز دون الحاجة لملفات صوتية خارجية
    /// </summary>
    public static class SoundAlertHelper
    {
        private static byte[] _cachedChimeWav = null;
        private static readonly object _lock = new object();

        /// <summary>
        /// تشغيل صوت تنبيه فوري لوصول طلب أونلاين جديد عبر كارت الصوت والسماعات
        /// </summary>
        public static void PlayNewOrderAlert()
        {
            Task.Run(() =>
            {
                try
                {
                    // 1. محاولة تشغيل نغمة الكاشير اللطيفة (Dual-Tone Bell Chime) عبر كارت الصوت
                    byte[] wav = GetOrCreateChimeWav();
                    if (wav != null && wav.Length > 0)
                    {
                        using (var ms = new MemoryStream(wav))
                        using (var player = new SoundPlayer(ms))
                        {
                            player.PlaySync();
                            return;
                        }
                    }
                }
                catch
                {
                    // تجاهل والنزول للخيارات البديلة
                }

                // 2. خيار احتياطي عبر SystemSounds
                try
                {
                    SystemSounds.Exclamation.Play();
                }
                catch { }

                // 3. خيار احتياطي إضافي عبر بيزر اللوحة/سماعة الجهاز المباشرة
                try
                {
                    Console.Beep(988, 150);
                    System.Threading.Thread.Sleep(40);
                    Console.Beep(1319, 300);
                }
                catch { }
            });
        }

        private static byte[] GetOrCreateChimeWav()
        {
            if (_cachedChimeWav != null) return _cachedChimeWav;

            lock (_lock)
            {
                if (_cachedChimeWav != null) return _cachedChimeWav;

                try
                {
                    int sampleRate = 22050;
                    double duration = 0.55; // 550 مللي ثانية
                    int numSamples = (int)(sampleRate * duration);
                    short[] samples = new short[numSamples];

                    // نغمتان متتاليتان لطيفة جداً تشبه جرس الاستقبال/الكاشير: 880Hz (A5) ثم 1320Hz (E6)
                    for (int i = 0; i < numSamples; i++)
                    {
                        double t = (double)i / sampleRate;
                        double env1 = Math.Exp(-6.0 * t);
                        double tone1 = Math.Sin(2 * Math.PI * 880 * t) * env1;

                        double tone2 = 0;
                        if (t > 0.12)
                        {
                            double t2 = t - 0.12;
                            double env2 = Math.Exp(-5.0 * t2);
                            tone2 = Math.Sin(2 * Math.PI * 1320 * t2) * env2;
                        }

                        double mixed = (tone1 * 0.45) + (tone2 * 0.55);
                        samples[i] = (short)(mixed * 28000);
                    }

                    using (var ms = new MemoryStream())
                    using (var bw = new BinaryWriter(ms))
                    {
                        // RIFF header
                        bw.Write(new char[] { 'R', 'I', 'F', 'F' });
                        bw.Write(36 + numSamples * 2);
                        bw.Write(new char[] { 'W', 'A', 'V', 'E' });

                        // Subchunk 1 "fmt "
                        bw.Write(new char[] { 'f', 'm', 't', ' ' });
                        bw.Write(16);
                        bw.Write((short)1); // PCM
                        bw.Write((short)1); // Mono
                        bw.Write(sampleRate);
                        bw.Write(sampleRate * 2);
                        bw.Write((short)2);
                        bw.Write((short)16);

                        // Subchunk 2 "data"
                        bw.Write(new char[] { 'd', 'a', 't', 'a' });
                        bw.Write(numSamples * 2);
                        for (int i = 0; i < numSamples; i++)
                        {
                            bw.Write(samples[i]);
                        }

                        _cachedChimeWav = ms.ToArray();
                    }
                }
                catch
                {
                    _cachedChimeWav = null;
                }

                return _cachedChimeWav;
            }
        }
    }
}
