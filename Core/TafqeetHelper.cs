using System;

namespace ChickenDist.Core
{
    /// <summary>
    /// تحويل الأرقام إلى كلمات عربية (تفقيط المبالغ المالية)
    /// </summary>
    public static class TafqeetHelper
    {
        private static readonly string[] Ones = { "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة", "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر" };
        private static readonly string[] Tens = { "", "", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون" };
        private static readonly string[] Hundreds = { "", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة" };

        public static string ConvertToArabicWords(decimal amount, string currencyName = "جنيه", string piasterName = "قرش")
        {
            if (amount == 0) return "صفر " + currencyName;

            long integerPart = (long)Math.Truncate(amount);
            int decimalPart = (int)Math.Round((amount - integerPart) * 100);

            string result = "";

            if (integerPart > 0)
            {
                result = ConvertGroup(integerPart) + " " + currencyName;
            }

            if (decimalPart > 0)
            {
                if (!string.IsNullOrEmpty(result)) result += " و";
                result += ConvertGroup(decimalPart) + " " + piasterName;
            }

            return result + " فقط لا غير";
        }

        private static string ConvertGroup(long number)
        {
            if (number == 0) return "";

            if (number < 20) return Ones[number];
            if (number < 100)
            {
                long ten = number / 10;
                long one = number % 10;
                return one > 0 ? Ones[one] + " و" + Tens[ten] : Tens[ten];
            }
            if (number < 1000)
            {
                long hundred = number / 100;
                long remainder = number % 100;
                return remainder > 0 ? Hundreds[hundred] + " و" + ConvertGroup(remainder) : Hundreds[hundred];
            }
            if (number < 1000000)
            {
                long thousand = number / 1000;
                long remainder = number % 1000;
                string thousandText = thousand == 1 ? "ألف" : thousand == 2 ? "ألفان" : thousand <= 10 ? Ones[thousand] + " آلاف" : ConvertGroup(thousand) + " ألف";
                return remainder > 0 ? thousandText + " و" + ConvertGroup(remainder) : thousandText;
            }
            if (number < 1000000000)
            {
                long million = number / 1000000;
                long remainder = number % 1000000;
                string millionText = million == 1 ? "مليون" : million == 2 ? "مليونان" : million <= 10 ? Ones[million] + " ملايين" : ConvertGroup(million) + " مليون";
                return remainder > 0 ? millionText + " و" + ConvertGroup(remainder) : millionText;
            }

            return number.ToString("N0");
        }
    }
}
