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
            if (string.IsNullOrWhiteSpace(rawPrefixes)) rawPrefixes = "20,99,21,22,27,9";

            string[] prefixes = rawPrefixes.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            int codeLen = AppConfig.BarcodeScaleItemCodeLength; // 5
            int weightLen = AppConfig.BarcodeScaleWeightLength; // 5
            decimal div = AppConfig.BarcodeScaleDivideBy; // 1000

            foreach (string prefix in prefixes)
            {
                string p = prefix.Trim();
                if (string.IsNullOrEmpty(p)) continue;

                int expectedLength = p.Length + codeLen + weightLen + 1; // e.g. 2 + 5 + 5 + 1 = 13 digits

                if (barcode.StartsWith(p) && (barcode.Length == expectedLength || (p.Length == 2 && barcode.Length == 13)))
                {
                    result.IsScaleBarcode = true;
                    result.MatchedPrefix = p;
                    result.ItemCode = barcode.Substring(p.Length, codeLen);
                    result.TrimmedItemCode = result.ItemCode.TrimStart('0');
                    if (string.IsNullOrEmpty(result.TrimmedItemCode)) result.TrimmedItemCode = "0";

                    string weightStr = barcode.Substring(p.Length + codeLen, weightLen);
                    if (decimal.TryParse(weightStr, out decimal w))
                    {
                        result.WeightOrPrice = div > 0 ? w / div : w;
                    }
                    break;
                }
            }

            return result;
        }
    }
}
