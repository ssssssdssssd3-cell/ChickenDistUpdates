using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Text;

namespace ChickenDist.Core
{
    /// <summary>
    /// قارئ ملفات Excel (.xlsx) خفيف وسريع يعتمد فقط على OpenXML ومكتبة فك الضغط القياسية لدوت نت.
    /// لا يحتاج هذا القارئ لتثبيت مايكروسوفت أوفيس أو أي برمجيات خارجية.
    /// </summary>
    public static class XlsxParser
    {
        public static List<string[]> Parse(string filePath)
        {
            var rows = new List<string[]>();
            var sharedStrings = new List<string>();

            using (ZipArchive archive = ZipFile.OpenRead(filePath))
            {
                // 1. قراءة النصوص المشتركة (Shared Strings Table)
                ZipArchiveEntry sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
                if (sharedStringsEntry != null)
                {
                    using (Stream stream = sharedStringsEntry.Open())
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(stream);
                        XmlNodeList tNodes = doc.GetElementsByTagName("t");
                        foreach (XmlNode tNode in tNodes)
                        {
                            sharedStrings.Add(tNode.InnerText);
                        }
                    }
                }

                // 2. قراءة بيانات الورقة الأولى (Sheet 1)
                ZipArchiveEntry sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml") ?? archive.GetEntry("xl/worksheets/Sheet1.xml");
                if (sheetEntry == null)
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase))
                        {
                            sheetEntry = entry;
                            break;
                        }
                    }
                }

                if (sheetEntry != null)
                {
                    using (Stream stream = sheetEntry.Open())
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(stream);
                        
                        XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                        nsmgr.AddNamespace("x", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");

                        XmlNodeList rowNodes = doc.SelectNodes("//x:row", nsmgr);
                        if (rowNodes == null || rowNodes.Count == 0)
                        {
                            rowNodes = doc.GetElementsByTagName("row");
                        }

                        foreach (XmlNode rowNode in rowNodes)
                        {
                            XmlNodeList cNodes = rowNode.SelectNodes("x:c", nsmgr);
                            if (cNodes == null || cNodes.Count == 0)
                            {
                                cNodes = rowNode.SelectNodes("c");
                            }

                            var cellMap = new SortedDictionary<int, string>();
                            int maxCol = 0;

                            foreach (XmlNode cNode in cNodes)
                            {
                                if (cNode.Attributes["r"] == null) continue;
                                string cellRef = cNode.Attributes["r"].Value; // مثل "A1", "B2", "AA15"
                                int colIndex = GetColumnIndex(cellRef);
                                if (colIndex > maxCol) maxCol = colIndex;

                                string val = "";
                                XmlNode vNode = cNode.SelectSingleNode("x:v", nsmgr) ?? cNode.SelectSingleNode("v");
                                if (vNode != null)
                                {
                                    val = vNode.InnerText;
                                    var typeAttr = cNode.Attributes["t"];
                                    if (typeAttr != null && typeAttr.Value == "s")
                                    {
                                        int strIndex = int.Parse(val);
                                        if (strIndex >= 0 && strIndex < sharedStrings.Count)
                                        {
                                            val = sharedStrings[strIndex];
                                        }
                                    }
                                }
                                cellMap[colIndex] = val;
                            }

                            if (maxCol > 0)
                            {
                                string[] rowData = new string[maxCol];
                                for (int i = 1; i <= maxCol; i++)
                                {
                                    rowData[i - 1] = cellMap.ContainsKey(i) ? cellMap[i] : "";
                                }
                                rows.Add(rowData);
                            }
                        }
                    }
                }
            }

            return rows;
        }

        private static int GetColumnIndex(string cellRef)
        {
            string colName = "";
            foreach (char c in cellRef)
            {
                if (char.IsLetter(c)) colName += char.ToUpper(c);
                else break;
            }

            int index = 0;
            foreach (char c in colName)
            {
                index = index * 26 + (c - 'A' + 1);
            }
            return index;
        }
    }
}
