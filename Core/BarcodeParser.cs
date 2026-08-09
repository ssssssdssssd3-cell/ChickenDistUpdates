using System;
using ChickenDist.Core;

namespace ChickenDist.Core
{
    public class BarcodeParseResult
    {
        public bool IsScaleBarcode { get; set; }
        public string ItemCode { get; set; }
        public string TrimmedItemCode { get; set; }
        public decimal WeightOrPrice { get; set; }
        public string OriginalBarcode { get; set; }
        public string MatchedPrefix { get; set; }
    }

    public static class BarcodeParser
    {
        public static BarcodeParseResult Parse(string barcode)
        {
            var result = new BarcodeParseResult { OriginalBarcode = barcode, IsScaleBarcode = false };

            if (string.IsNullOrWhiteSpace(barcode))
                return result;

            barcode = barcode.Trim();

            string rawPrefixes = AppConfig.BarcodeScalePrefix;
            if (string.IsNullOrWhiteSpace(rawPrefixes))
            {
                rawPrefixes = "99,20,21,22,23,24,25,26,27,28,29,9";
            }
            else
            {
                if (!rawPrefixes.Contains("99")) rawPrefixes += ",99";
                if (!rawPrefixes.Contains("20")) rawPrefixes += ",20";
            }

            string[] prefixes = rawPrefixes.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int codeLen = AppConfig.BarcodeScaleItemCodeLength; // default 5
            int weightLen = AppConfig.BarcodeScaleWeightLength; // default 5
            decimal div = AppConfig.BarcodeScaleDivideBy; // default 1000

            foreach (string prefix in prefixes)
            {
                string p = prefix.Trim();
                if (string.IsNullOrEmpty(p)) continue;

                // EAN-13 (13 digits), EAN-12 (12 digits) or custom length
                int expectedLength = p.Length + codeLen + weightLen + 1; // e.g. 2 + 5 + 5 + 1 = 13 digits

                if (barcode.StartsWith(p) && (barcode.Length == expectedLength || barcode.Length == 13 || barcode.Length == 12))
                {
                    int actualCodeLen = (barcode.Length == 13 && p.Length == 2) ? 5 : codeLen;
                    int actualWeightLen = (barcode.Length == 13 && p.Length == 2) ? 5 : weightLen;

                    if (barcode.Length >= p.Length + actualCodeLen + actualWeightLen)
                    {
                        result.IsScaleBarcode = true;
                        result.MatchedPrefix = p;
                        result.ItemCode = barcode.Substring(p.Length, actualCodeLen);
                        result.TrimmedItemCode = result.ItemCode.TrimStart('0');
                        if (string.IsNullOrEmpty(result.TrimmedItemCode)) result.TrimmedItemCode = "0";

                        string weightStr = barcode.Substring(p.Length + actualCodeLen, actualWeightLen);
                        if (decimal.TryParse(weightStr, out decimal w))
                        {
                            result.WeightOrPrice = div > 0 ? w / div : w;
                        }
                        break;
                    }
                }
            }

            return result;
        }
    }
}
