using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ISPG.Conversion.Helpers;

namespace ISPG.Conversion.Helpers
{
    /// <summary>
    /// Helper class for CSV export/import operations
    /// </summary>
    public static class CsvHelper
    {
        public static void WriteCsv(string filePath, List<Dictionary<string, string>> rows, List<string> columns)
        {
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Write header
                writer.WriteLine(string.Join(",", columns.Select(EscapeCsvField)));

                // Write rows
                foreach (var row in rows)
                {
                    List<string> values = new List<string>();
                    foreach (string col in columns)
                    {
                        string value = row.ContainsKey(col) ? row[col] ?? "" : "";
                        values.Add(EscapeCsvField(value));
                    }
                    writer.WriteLine(string.Join(",", values));
                }
            }
        }

        public static List<Dictionary<string, string>> ReadCsv(string filePath)
        {
            List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();

            using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null)
                    return rows;

                List<string> headers = ParseCsvLine(headerLine);

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    List<string> values = ParseCsvLine(line);
                    Dictionary<string, string> row = new Dictionary<string, string>();

                    for (int i = 0; i < headers.Count; i++)
                    {
                        string value = i < values.Count ? values[i] : "";
                        row[headers[i]] = value;
                    }

                    rows.Add(row);
                }
            }

            return rows;
        }

        private static string EscapeCsvField(string field)
        {
            if (field == null)
                return "\"\"";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

        private static List<string> ParseCsvLine(string line)
        {
            List<string> fields = new List<string>();
            bool inQuotes = false;
            StringBuilder currentField = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            fields.Add(currentField.ToString());
            return fields;
        }

        public static string GetParamValue(Element element, string[] paramNames)
        {
            foreach (string paramName in paramNames)
            {
                Parameter param = element.LookupParameter(paramName);
                if (param != null && param.HasValue)
                {
                    if (param.StorageType == StorageType.String)
                        return param.AsString();
                    else if (param.StorageType == StorageType.Double)
                        return param.AsDouble().ToString("F6");
                    else if (param.StorageType == StorageType.Integer)
                        return param.AsInteger().ToString();
                }
            }
            return "";
        }

        public static bool SetParamValue(Element element, string[] paramNames, string value)
        {
            foreach (string paramName in paramNames)
            {
                Parameter param = element.LookupParameter(paramName);
                if (param != null && !param.IsReadOnly)
                {
                    try
                    {
                        if (param.StorageType == StorageType.String)
                        {
                            param.Set(value);
                            return true;
                        }
                        else if (param.StorageType == StorageType.Double)
                        {
                            if (double.TryParse(value, out double d))
                            {
                                param.Set(d);
                                return true;
                            }
                        }
                        else if (param.StorageType == StorageType.Integer)
                        {
                            if (int.TryParse(value, out int i))
                            {
                                param.Set(i);
                                return true;
                            }
                        }
                    }
                    catch { }
                }
            }
            return false;
        }
    }
}
