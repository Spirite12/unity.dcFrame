using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

internal static class TableCsvEditorUtil {
    internal sealed class HeaderData {
        public List<string> HeaderList { get; set; } = new();
        public Dictionary<string, string> DisplayNameDic { get; } = new();

        public string GetDisplayName(string fieldName) {
            if (DisplayNameDic.TryGetValue(fieldName, out var displayName) && !string.IsNullOrWhiteSpace(displayName)) {
                return displayName;
            }
            return fieldName;
        }
    }

    public static CsvReader CreateCsvReader(string filePath) {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture);
        var reader = new StreamReader(filePath, Encoding.UTF8);
        return new CsvReader(reader, config);
    }

    public static HeaderData ReadHeaderData(CsvReader csv) {
        var headerData = new HeaderData();
        if (!csv.Read()) {
            return headerData;
        }

        csv.ReadHeader();
        headerData.HeaderList = csv.HeaderRecord?.ToList() ?? new List<string>();
        if (!csv.Read()) {
            return headerData;
        }

        foreach (var header in headerData.HeaderList) {
            headerData.DisplayNameDic[header] = csv.GetField(header) ?? string.Empty;
        }
        return headerData;
    }
}
