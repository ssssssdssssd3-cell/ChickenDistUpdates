using System;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ChickenDist.Core
{
    /// <summary>
    /// مسجّل أحداث التطبيق — يكتب سجلات الأخطاء والمراجعة في ملف نصي مع توضيح الخطأ وحله بالعربية تلقائياً.
    /// </summary>
    public static class AppLogger
    {
        private static readonly object _lock = new object();

        private static string LogPath
        {
            get
            {
                try
                {
                    string dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                    return Path.Combine(dir, "app.log");
                }
                catch
                {
                    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
                }
            }
        }

        /// <summary>تسجيل خطأ مع التشخيص والحل بالعربية وتفاصيل الـ Exception</summary>
        public static void Error(string message, Exception ex, string source = "")
        {
            var diagnosis = DiagnoseException(message, ex, source);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(new string('=', 78));
            sb.AppendLine($"⏰ التوقيت: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | 🏷️ المصدر: {(string.IsNullOrEmpty(source) ? "عام" : source)}");
            if (!string.IsNullOrEmpty(message))
            {
                sb.AppendLine($"📌 البيان: {message}");
            }
            sb.AppendLine($"🔴 وصف الخطأ بالعربية: {diagnosis.ArabicTitle}");
            sb.AppendLine($"💡 الحل وخطوات العلاج المقترحة:");
            foreach (var step in diagnosis.ArabicSolutionSteps)
            {
                sb.AppendLine($"   👉 {step}");
            }
            if (!string.IsNullOrEmpty(diagnosis.QuickActionSql))
            {
                sb.AppendLine($"🛠️ الاستعلام / الإجراء السريع المقترح:");
                sb.AppendLine($"   {diagnosis.QuickActionSql}");
            }
            sb.AppendLine(new string('-', 78));
            sb.AppendLine($"⚙️ التفاصيل الفنية للمطور (Technical Details):");
            sb.AppendLine($"   النوع: {ex?.GetType().FullName}");
            sb.AppendLine($"   الرسالة: {ex?.Message}");
            if (ex?.InnerException != null)
            {
                sb.AppendLine($"   الخطأ الداخلي: {ex.InnerException.Message}");
            }
            if (!string.IsNullOrEmpty(ex?.StackTrace))
            {
                sb.AppendLine($"   StackTrace:\n{ex.StackTrace}");
            }
            sb.AppendLine(new string('=', 78));

            WriteRaw(sb.ToString());
        }

        /// <summary>تسجيل حدث مراجعة (Audit) لتتبع العمليات الحساسة</summary>
        public static void Audit(string action, string details)
        {
            string user = Session.EmpID > 0 ? $"{Session.EmpName} ({Session.UserName})" : "غير معروف";
            Write("AUDIT", $"{action} | المستخدم: {user} | {details}");
        }

        /// <summary>تسجيل تحذير</summary>
        public static void Warn(string message, string source = "")
        {
            string text = string.IsNullOrEmpty(source) ? message : $"[{source}] {message}";
            Write("WARN", text);
        }

        /// <summary>تسجيل رسالة معلوماتية</summary>
        public static void Info(string message)
        {
            Write("INFO", message);
        }

        // ── تشخيص وتحليل الأخطاء بالعربية مع تقديم الحل ────────────────────────────
        private class ErrorDiagnosis
        {
            public string ArabicTitle { get; set; }
            public string[] ArabicSolutionSteps { get; set; }
            public string QuickActionSql { get; set; }
        }

        private static ErrorDiagnosis DiagnoseException(string message, Exception ex, string source)
        {
            string raw = ((message ?? "") + " " + (ex?.Message ?? "") + " " + (ex?.InnerException?.Message ?? "")).ToLowerInvariant();

            // 1. Invalid column name
            if (raw.Contains("invalid column name"))
            {
                var match = Regex.Match(ex?.Message ?? "", "Invalid column name '([^']+)'", RegexOptions.IgnoreCase);
                string col = match.Success ? match.Groups[1].Value : "الموضح بالرسالة";
                return new ErrorDiagnosis
                {
                    ArabicTitle = $"عمود مفقود في قاعدة البيانات: '{col}'",
                    ArabicSolutionSteps = new[]
                    {
                        "قاعدة البيانات تحتاج إلى تطبيق التحديثات لإنشاء هذا العمود الجديد.",
                        "افتح شاشة الإعدادات العامة بالبرنامج واضغط على زر تحديث وترقية قاعدة البيانات.",
                        "أو قم بتنفيذ استعلام إضافة العمود يدوياً في SQL Server Management Studio."
                    },
                    QuickActionSql = $"ALTER TABLE [اسم_الجدول] ADD [{col}] DECIMAL(18,4) NULL;"
                };
            }

            // 2. Invalid object name (جدول أو View مفقود)
            if (raw.Contains("invalid object name"))
            {
                var match = Regex.Match(ex?.Message ?? "", "Invalid object name '([^']+)'", RegexOptions.IgnoreCase);
                string obj = match.Success ? match.Groups[1].Value : "الموضح بالرسالة";
                return new ErrorDiagnosis
                {
                    ArabicTitle = $"جدول أو View غير موجود بقاعدة البيانات: '{obj}'",
                    ArabicSolutionSteps = new[]
                    {
                        $"الجدول أو العرض ({obj}) لم يتم إنشاؤه في قاعدة البيانات الحالية.",
                        "تأكد من فتح البرنامج بصلاحيات المسؤول ليقوم بعمل الميجريشن التلقائي.",
                        "أو تأكد أنك متصل بقاعدة البيانات الصحيحة (prosoftdb) وليس قاعدة بيانات تجريبية أخرى."
                    }
                };
            }

            // 3. الاتصال بالسيرفر SQL Server
            if (raw.Contains("network-related") || raw.Contains("cannot open database") || raw.Contains("server was not found") || raw.Contains("login failed"))
            {
                return new ErrorDiagnosis
                {
                    ArabicTitle = "فشل الاتصال بسيرفر قواعد البيانات SQL Server",
                    ArabicSolutionSteps = new[]
                    {
                        "تأكد من أن خدمة SQL Server (MSSQLSERVER أو SQLEXPRESS) قيد التشغيل في خدمات ويندوز (services.msc).",
                        "تأكد من صحة اسم السيرفر وبيانات الدخول المسجلة في ملف Settings.ini.",
                        "إذا كان السيرفر على جهاز آخر، تأكد من اتصال الشبكة وسماح جدار الحماية (Firewall) للمنفذ 1433."
                    }
                };
            }

            // 4. تكرار Primary Key أو Unique Constraint
            if (raw.Contains("violation of primary key") || raw.Contains("cannot insert duplicate key") || raw.Contains("unique index"))
            {
                return new ErrorDiagnosis
                {
                    ArabicTitle = "تعارض كود أو رقم مكرر مسجل مسبقاً في قاعدة البيانات",
                    ArabicSolutionSteps = new[]
                    {
                        "الكود أو الرقم الذي تحاول حفظه (كود صنف، رقم فاتورة، أو باركود) موجود بالفعل ومستخدم سابقاً.",
                        "قم بتغيير الكود المدخل أو اضغط على زر توليد كود تلقائي جديد بالبرنامج."
                    }
                };
            }

            // 5. Foreign Key Constraint (منع الحذف لوجود حركات)
            if (raw.Contains("foreign key constraint") || raw.Contains("conflicted with the reference constraint"))
            {
                return new ErrorDiagnosis
                {
                    ArabicTitle = "لا يمكن الحذف لارتباط هذا البند بسجلات وحركات وفواتير سابقة",
                    ArabicSolutionSteps = new[]
                    {
                        "هذا السجل (عميل / مورد / صنف / خزينة) مرتبط بحركات مالية وفواتير تم تسجيلها مسبقاً.",
                        "للحفاظ على توازن الحسابات، يمنع النظام حذفه نهائياً.",
                        "الحل الصحيح: قم بإلغاء علامة (نشط) عن البند لإخفائه دون حذف تاريخه المالي."
                    }
                };
            }

            // 6. القسمة على صفر
            if (raw.Contains("divide by zero") || raw.Contains("attempted to divide by zero"))
            {
                return new ErrorDiagnosis
                {
                    ArabicTitle = "خطأ رياضي: محاولة قسمة على صفر",
                    ArabicSolutionSteps = new[]
                    {
                        "أحد الحقول الحسابية يحتوي على قيمة صفر (مثل معامل التجزئة للوحدة، أو الكمية، أو النسبة).",
                        "يرجى فتح كارت الصنف والتأكد من أن معامِلات الوحدات الكبرى والصغرى لا تساوي صفراً.",
                        "تأكد من عدم ترك خانة الكمية أو السعر فارغة أو صفرية."
                    }
                };
            }

            // 7. الوردية مغلقة
            if (raw.Contains("وردية") || raw.Contains("shift") || raw.Contains("cashbox") || raw.Contains("خزينة"))
            {
                return new ErrorDiagnosis
                {
                    ArabicTitle = "الوردية مغلقة أو الخزينة غير محددة",
                    ArabicSolutionSteps = new[]
                    {
                        "لا توجد وردية نشطة ومفتوحة حالياً للمستخدم لإتمام الحركة المالية.",
                        "توجه إلى شاشة (فتح وردية) بالبرنامج وقم بفتح وردية جديدة لتسجيل الإيرادات والمصروفات عليها."
                    }
                };
            }

            // 8. الطابعة
            if (raw.Contains("printer") || raw.Contains("printdocument") || raw.Contains("printing"))
            {
                return new ErrorDiagnosis
                {
                    ArabicTitle = "تعذر الاتصال بالطابعة أو عدم وجود طابعة افتراضية",
                    ArabicSolutionSteps = new[]
                    {
                        "تأكد من تشغيل الطابعة وتوصيل كابل الـ USB أو اتصالها بالشبكة.",
                        "تأكد من اختيار اسم الطابعة الصحيح من شاشة (إعدادات الطابعات) بالبرنامج.",
                        "جرب طباعة صفحة تجريبية من لوحة تحكم ويندوز للتأكد من تعريفها بشكل سليم."
                    }
                };
            }

            // 9. الصلاحيات
            if (raw.Contains("permission") || raw.Contains("unauthorized") || raw.Contains("صلاحية") || raw.Contains("مرفوض"))
            {
                return new ErrorDiagnosis
                {
                    ArabicTitle = "صلاحية غير كافية لتنفيذ هذه العملية",
                    ArabicSolutionSteps = new[]
                    {
                        "المستخدم الحالي لا يملك الصلاحية اللازمة لتنفيذ هذا الإجراء.",
                        "قم بتسجيل الدخول بحساب المدير (Admin) ثم توجه إلى شاشة (الموظفين والصلاحيات) لتفعيل الإذن المطلوب."
                    }
                };
            }

            // 10. تشخيص افتراضي عام ذكي
            return new ErrorDiagnosis
            {
                ArabicTitle = !string.IsNullOrEmpty(message) ? message : "تنبيه أثناء معالجة العملية",
                ArabicSolutionSteps = new[]
                {
                    "تأكد من ملء جميع الحقول المطلوبة ومراجعة القيم المدخلة قبل الحفظ.",
                    "إذا استمرت المشكلة، يرجى مشاركة هذا الجزء من السجل مع الدعم الفني لحلها فوراً."
                }
            };
        }

        // ── الكتابة الفعلية في الملف ───────────────────────────────────────────
        private static void Write(string level, string text)
        {
            try
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {text}{Environment.NewLine}{new string('-', 60)}{Environment.NewLine}";
                WriteRaw(line);
            }
            catch { }
        }

        private static void WriteRaw(string rawText)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(LogPath, rawText, System.Text.Encoding.UTF8);
                }
                RotateIfNeeded();
            }
            catch { }
        }

        private static void RotateIfNeeded()
        {
            try
            {
                lock (_lock)
                {
                    string path = LogPath;
                    var fi = new FileInfo(path);
                    if (fi.Exists && fi.Length > 5 * 1024 * 1024) // 5 MB
                    {
                        string archive = Path.ChangeExtension(path,
                            $".{DateTime.Now:yyyyMMdd_HHmmss}.log");
                        File.Move(path, archive);

                        // Clean up old archives, keep only the most recent 3
                        string dir = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(dir))
                        {
                            var logFiles = new DirectoryInfo(dir).GetFiles("app.*.log");
                            if (logFiles.Length > 3)
                            {
                                Array.Sort(logFiles, (a, b) => b.CreationTime.CompareTo(a.CreationTime));
                                for (int i = 3; i < logFiles.Length; i++)
                                {
                                    try { logFiles[i].Delete(); } catch { }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}
