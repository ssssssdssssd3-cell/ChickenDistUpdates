using System;
using ChickenDist.Core;

namespace ChickenDist.Core
{
    public class BarcodeParseResult
    {
        public bool IsScaleBarcode { get; set; }
        public string ItemCode { get; set; }
        public decimal WeightOrPrice { get; set; }
        public string OriginalBarcode { get; set; }
    }

    public static class BarcodeParser
    {
        public static BarcodeParseResult Parse(string barcode)
        {
            var result = new BarcodeParseResult { OriginalBarcode = barcode, IsScaleBarcode = false };

            if (string.IsNullOrWhiteSpace(barcode))
                return result;

            string prefix = AppConfig.BarcodeScalePrefix;
            int codeLen = AppConfig.BarcodeScaleItemCodeLength;
            int weightLen = AppConfig.BarcodeScaleWeightLength;
            decimal div = AppConfig.BarcodeScaleDivideBy;

            // عادة باركود الميزان يتكون من: Prefix + Code + Weight + Checksum (1 رقم)
            int expectedLength = prefix.Length + codeLen + weightLen + 1;

            if (barcode.StartsWith(prefix) && barcode.Length == expectedLength)
            {
                result.IsScaleBarcode = true;
                result.ItemCode = barcode.Substring(prefix.Length, codeLen);
                
                string weightStr = barcode.Substring(prefix.Length + codeLen, weightLen);
                if (decimal.TryParse(weightStr, out decimal w))
                {
                    result.WeightOrPrice = div > 0 ? w / div : w;
                }
            }

            return result;
        }
    }
}
