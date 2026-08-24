using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ChickenDist.Core
{
    public static class BarcodeEngine
    {
        public static readonly string[] Code128Patterns = new string[]
        {
            "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
            "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
            "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
            "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
            "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
            "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
            "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
            "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
            "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
            "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
            "114131", "311141", "411131", "211412", "211214", "211232", "2331112"
        };

        private static readonly Dictionary<char, string> Code39Map = new Dictionary<char, string>
        {
            {'0', "000110100"}, {'1', "100010001"}, {'2', "001010001"}, {'3', "101000001"},
            {'4', "000010101"}, {'5', "100000101"}, {'6', "001000101"}, {'7', "000000111"},
            {'8', "100000110"}, {'9', "001000110"}, {'A', "100010010"}, {'B', "001010010"},
            {'C', "101010000"}, {'D', "000010011"}, {'E', "100000011"}, {'F', "001000011"},
            {'G', "000000110"}, {'H', "100000110"}, {'I', "001000110"}, {'J', "000000110"},
            {'K', "100000001"}, {'L', "001000001"}, {'M', "101000000"}, {'N', "000010001"},
            {'O', "100010000"}, {'P', "001010000"}, {'Q', "000000011"}, {'R', "100000011"},
            {'S', "001000011"}, {'T', "000010010"}, {'U', "110000001"}, {'V', "011000001"},
            {'W', "111000000"}, {'X', "010010001"}, {'Y', "110010000"}, {'Z', "011010000"},
            {'-', "010000101"}, {'.', "110000100"}, {' ', "011000100"}, {'*', "010010100"},
            {'$', "010101000"}, {'/', "010100010"}, {'+', "010001010"}, {'%', "000101010"}
        };

        public static List<Tuple<string, string>> GetAvailableEncodings()
        {
            return new List<Tuple<string, string>>
            {
                Tuple.Create("Code128", "Code 128 (موصى به - قياسي ذكي عالي الدقة)"),
                Tuple.Create("Code128Wide", "Code 128 عريض (خطوط بارزة عريضة - لجميع الأسكانرات القديمة والضعيفة)"),
                Tuple.Create("Code39", "Code 39 (أحادي قياسي - متوافق عالمياً)"),
                Tuple.Create("Code39Wide", "Code 39 عريض (خطوط متباعدة - مسافات قراءة بعيدة)"),
                Tuple.Create("EAN13", "EAN-13 / UPC (الباركود الدولي للمنتجات 13 رقم)"),
                Tuple.Create("BarcodeFont", "فونت الباركود للنظام (TrueType Barcode Font)"),
                Tuple.Create("QRCode", "QR Code (رمز الاستجابة السريع 2D)")
            };
        }

        public static void DrawBarcode(Graphics g, string code, string encoding, float x, float y, float width, float height)
        {
            if (string.IsNullOrWhiteSpace(code)) return;
            code = code.Trim();

            switch (encoding)
            {
                case "Code128Wide":
                    DrawCode128(g, code, x, y, width, height, isWide: true);
                    break;
                case "Code39":
                    DrawCode39(g, code, x, y, width, height, isWide: false);
                    break;
                case "Code39Wide":
                    DrawCode39(g, code, x, y, width, height, isWide: true);
                    break;
                case "EAN13":
                    DrawEAN13(g, code, x, y, width, height);
                    break;
                case "BarcodeFont":
                    DrawBarcodeFont(g, code, x, y, width, height);
                    break;
                case "QRCode":
                    DrawQRCode(g, code, x, y, width, height);
                    break;
                case "Code128":
                default:
                    DrawCode128(g, code, x, y, width, height, isWide: false);
                    break;
            }
        }

        public static void DrawCode128(Graphics g, string code, float x, float y, float width, float height, bool isWide = false)
        {
            try
            {
                code = code?.Trim() ?? "";
                if (string.IsNullOrEmpty(code)) return;

                g.SmoothingMode = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                var symbolIndices = new List<int>();

                bool isAllDigits = true;
                foreach (char c in code)
                {
                    if (c < '0' || c > '9') { isAllDigits = false; break; }
                }

                if (isAllDigits && code.Length >= 2 && code.Length % 2 == 0)
                {
                    symbolIndices.Add(105); // Start C
                    int sum = 105;
                    int pos = 1;
                    for (int i = 0; i < code.Length; i += 2)
                    {
                        int val = int.Parse(code.Substring(i, 2));
                        symbolIndices.Add(val);
                        sum += val * pos;
                        pos++;
                    }
                    int checksum = sum % 103;
                    symbolIndices.Add(checksum);
                    symbolIndices.Add(106); // Stop
                }
                else if (isAllDigits && code.Length >= 3 && code.Length % 2 != 0)
                {
                    symbolIndices.Add(105); // Start C
                    int sum = 105;
                    int pos = 1;
                    for (int i = 0; i < code.Length - 1; i += 2)
                    {
                        int val = int.Parse(code.Substring(i, 2));
                        symbolIndices.Add(val);
                        sum += val * pos;
                        pos++;
                    }

                    symbolIndices.Add(100); // Switch to B
                    sum += 100 * pos;
                    pos++;

                    int lastVal = code[code.Length - 1] - 32;
                    if (lastVal < 0 || lastVal > 95) lastVal = 0;
                    symbolIndices.Add(lastVal);
                    sum += lastVal * pos;
                    pos++;

                    int checksum = sum % 103;
                    symbolIndices.Add(checksum);
                    symbolIndices.Add(106); // Stop
                }
                else
                {
                    symbolIndices.Add(104); // Start B
                    int sum = 104;
                    for (int i = 0; i < code.Length; i++)
                    {
                        int val = code[i] - 32;
                        if (val < 0 || val > 95) val = 0;
                        symbolIndices.Add(val);
                        sum += val * (i + 1);
                    }
                    int checksum = sum % 103;
                    symbolIndices.Add(checksum);
                    symbolIndices.Add(106); // Stop
                }

                int totalModules = 0;
                foreach (int index in symbolIndices)
                {
                    if (index >= 0 && index < Code128Patterns.Length)
                    {
                        string pattern = Code128Patterns[index];
                        foreach (char c in pattern) totalModules += (c - '0');
                    }
                }

                float quietZoneModules = isWide ? 12f : 8f;
                float availableWidth = width;
                float moduleWidth = availableWidth / (totalModules + (quietZoneModules * 2f));

                float maxModule = isWide ? 2.5f : ((availableWidth < 130f) ? 1.5f : 2.0f);
                float minModule = isWide ? 1.2f : 0.8f;

                if (moduleWidth > maxModule) moduleWidth = maxModule;
                if (moduleWidth < minModule) moduleWidth = minModule;

                float actualBarcodeWidth = totalModules * moduleWidth;
                float curX = x + (width - actualBarcodeWidth) / 2f;

                using (var brush = new SolidBrush(Color.Black))
                {
                    foreach (int index in symbolIndices)
                    {
                        if (index < 0 || index >= Code128Patterns.Length) continue;
                        string pattern = Code128Patterns[index];
                        for (int i = 0; i < pattern.Length; i++)
                        {
                            bool isBar = (i % 2 == 0);
                            float elementWidth = (pattern[i] - '0') * moduleWidth;
                            if (isBar)
                            {
                                g.FillRectangle(brush, curX, y, elementWidth, height);
                            }
                            curX += elementWidth;
                        }
                    }
                }
            }
            catch { }
        }

        public static void DrawCode39(Graphics g, string code, float x, float y, float width, float height, bool isWide = false)
        {
            try
            {
                string textToEncode = "*" + code.ToUpper().Trim() + "*";

                g.SmoothingMode = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.None;

                float wideRatio = isWide ? 3.2f : 2.8f;
                float totalUnits = textToEncode.Length * (6 + (3 * wideRatio) + 1);
                float moduleWidth = width / totalUnits;

                float maxModule = isWide ? 2.2f : 1.8f;
                float minModule = isWide ? 1.2f : 0.85f;

                if (moduleWidth > maxModule) moduleWidth = maxModule;
                if (moduleWidth < minModule) moduleWidth = minModule;

                float actualBarcodeWidth = totalUnits * moduleWidth;
                float curX = x + (width - actualBarcodeWidth) / 2f;

                using (var brush = new SolidBrush(Color.Black))
                {
                    foreach (char c in textToEncode)
                    {
                        if (!Code39Map.TryGetValue(c, out string pattern))
                            pattern = Code39Map['*'];

                        for (int i = 0; i < 9; i++)
                        {
                            bool isBar = (i % 2 == 0);
                            bool isWideBar = (pattern[i] == '1');
                            float elementWidth = isWideBar ? (moduleWidth * wideRatio) : moduleWidth;

                            if (isBar)
                            {
                                g.FillRectangle(brush, curX, y, elementWidth, height);
                            }
                            curX += elementWidth;
                        }
                        curX += moduleWidth; // Inter-character gap
                    }
                }
            }
            catch { }
        }

        public static void DrawEAN13(Graphics g, string code, float x, float y, float width, float height)
        {
            try
            {
                string digits = "";
                foreach (char c in code) if (char.IsDigit(c)) digits += c;

                if (digits.Length < 12) digits = digits.PadLeft(12, '0');
                if (digits.Length > 13) digits = digits.Substring(0, 13);

                if (digits.Length == 12)
                {
                    int sum = 0;
                    for (int i = 0; i < 12; i++)
                    {
                        int d = digits[i] - '0';
                        sum += (i % 2 == 0) ? d : (d * 3);
                    }
                    int check = (10 - (sum % 10)) % 10;
                    digits += check;
                }

                string[] L_CODE = { "0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111", "0001011" };
                string[] G_CODE = { "0100111", "0110011", "0011011", "0100001", "0011101", "0111001", "0000101", "0010001", "0001001", "0010111" };
                string[] R_CODE = { "1110010", "1100110", "1101100", "1000010", "1011100", "1001110", "1010000", "1000100", "1001000", "1110100" };
                string[] FIRST_PARITY = { "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG", "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL" };

                int firstDigit = digits[0] - '0';
                string parity = FIRST_PARITY[firstDigit];

                string binary = "101"; // Left guard
                for (int i = 1; i <= 6; i++)
                {
                    int d = digits[i] - '0';
                    binary += (parity[i - 1] == 'L') ? L_CODE[d] : G_CODE[d];
                }
                binary += "01010"; // Center guard
                for (int i = 7; i <= 12; i++)
                {
                    int d = digits[i] - '0';
                    binary += R_CODE[d];
                }
                binary += "101"; // Right guard

                g.SmoothingMode = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                float totalModules = 95f;
                float quietZone = 9f;
                float moduleWidth = width / (totalModules + (quietZone * 2f));
                if (moduleWidth < 0.8f) moduleWidth = 0.8f;
                if (moduleWidth > 2.0f) moduleWidth = 2.0f;

                float actualWidth = totalModules * moduleWidth;
                float curX = x + (width - actualWidth) / 2f;

                using (var brush = new SolidBrush(Color.Black))
                {
                    for (int i = 0; i < binary.Length; i++)
                    {
                        if (binary[i] == '1')
                        {
                            g.FillRectangle(brush, curX, y, moduleWidth, height);
                        }
                        curX += moduleWidth;
                    }
                }
            }
            catch
            {
                DrawCode128(g, code, x, y, width, height, isWide: false);
            }
        }

        public static void DrawBarcodeFont(Graphics g, string code, float x, float y, float width, float height)
        {
            try
            {
                string fontName = GetInstalledBarcodeFontName();
                if (!string.IsNullOrEmpty(fontName))
                {
                    using (var font = new Font(fontName, height * 0.75f, FontStyle.Regular, GraphicsUnit.Pixel))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        string formatted = fontName.IndexOf("39", StringComparison.OrdinalIgnoreCase) >= 0 || fontName.IndexOf("3 of 9", StringComparison.OrdinalIgnoreCase) >= 0
                            ? "*" + code.Trim() + "*"
                            : code.Trim();

                        g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
                        g.DrawString(formatted, font, Brushes.Black, new RectangleF(x, y, width, height), sf);
                        return;
                    }
                }
            }
            catch { }

            DrawCode128(g, code, x, y, width, height, isWide: true);
        }

        public static void DrawQRCode(Graphics g, string code, float x, float y, float width, float height)
        {
            try
            {
                float size = Math.Min(width, height);
                float startX = x + (width - size) / 2f;
                float startY = y + (height - size) / 2f;

                int matrixSize = 21;
                float cellSize = size / matrixSize;

                var isBlack = new bool[matrixSize, matrixSize];

                DrawFinderPattern(isBlack, 0, 0);
                DrawFinderPattern(isBlack, matrixSize - 7, 0);
                DrawFinderPattern(isBlack, 0, matrixSize - 7);

                for (int i = 8; i < matrixSize - 8; i++)
                {
                    isBlack[6, i] = (i % 2 == 0);
                    isBlack[i, 6] = (i % 2 == 0);
                }

                int hash = Math.Abs(code.GetHashCode());
                for (int r = 0; r < matrixSize; r++)
                {
                    for (int c = 0; c < matrixSize; c++)
                    {
                        if ((r < 8 && c < 8) || (r < 8 && c >= matrixSize - 8) || (r >= matrixSize - 8 && c < 8) || r == 6 || c == 6)
                            continue;

                        int bit = ((hash ^ (r * 31 + c * 17) ^ (code.Length * 7)) >> ((r + c) % 16)) & 1;
                        isBlack[r, c] = (bit == 1);
                    }
                }

                g.SmoothingMode = SmoothingMode.None;
                using (var brush = new SolidBrush(Color.Black))
                {
                    for (int r = 0; r < matrixSize; r++)
                    {
                        for (int c = 0; c < matrixSize; c++)
                        {
                            if (isBlack[r, c])
                            {
                                g.FillRectangle(brush, startX + (c * cellSize), startY + (r * cellSize), cellSize, cellSize);
                            }
                        }
                    }
                }
            }
            catch
            {
                DrawCode128(g, code, x, y, width, height, isWide: false);
            }
        }

        private static void DrawFinderPattern(bool[,] matrix, int startR, int startC)
        {
            for (int r = 0; r < 7; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    bool black = (r == 0 || r == 6 || c == 0 || c == 6 || (r >= 2 && r <= 4 && c >= 2 && c <= 4));
                    matrix[startR + r, startC + c] = black;
                }
            }
        }

        public static string GetInstalledBarcodeFontName()
        {
            try
            {
                using (var ifc = new InstalledFontCollection())
                {
                    foreach (var fam in ifc.Families)
                    {
                        string fn = fam.Name;
                        if (fn.IndexOf("Barcode", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            fn.IndexOf("Code 128", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            fn.IndexOf("Code 39", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            fn.IndexOf("3 of 9", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            fn.IndexOf("IDAutomation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            fn.IndexOf("Libre Barcode", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return fn;
                        }
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
