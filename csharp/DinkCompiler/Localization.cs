namespace DinkCompiler;

using System.Text.Json;
using ClosedXML.Excel; 
public struct LocEntry
{
    public required string ID { get; set; }
    public required string Text { get; set; }
    public required List<string> Comments { get; set; }
    public required string Speaker { get; set; }
}

class LocStrings
{
    private Dictionary<string, LocEntry> _entries = new Dictionary<string, LocEntry>();
    private List<string> _ids = new List<string>();

    public IEnumerable<LocEntry> OrderedEntries => _ids.Select(id => _entries[id]);

    public void Set(LocEntry entry)
    {
        if (!_ids.Contains(entry.ID))
        {
            _ids.Add(entry.ID);
        }

        _entries[entry.ID] = entry;
    }

    public void Remove(string id)
    {
        _entries.Remove(id);
        _ids.Remove(id);
    }
    
    struct LocEntryExport
    {
        public required string ID { get; set; }
        public required string Text { get; set; }
        public required string Speaker { get; set; }
        public required string Comments { get; set; }
    }

    public bool WriteToExcel(string rootName, string destLocFile)
    {
        var recordsToExport = OrderedEntries.Select(v => new LocEntryExport
        {
            ID = v.ID,
            Speaker = v.Speaker,
            Text = v.Text,
            Comments = string.Join(", ", v.Comments)
        }).ToList();

        try
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Text Lines - " + rootName);

                var table = worksheet.Cell("A1").InsertTable(recordsToExport);

                worksheet.ColumnsUsed().AdjustToContents();

                table.FirstRow().Style.Fill.BackgroundColor = XLColor.LightBlue;
                table.FirstRow().Style.Font.Bold = true;

                workbook.SaveAs(destLocFile);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error writing out localisation Excel file {destLocFile}: " + ex.Message);
            return false;
        }
        return true;
    }

    public string WriteMinimal()
    {
        var options = new JsonSerializerOptions { WriteIndented = false };
        var lines = new List<string>();

        foreach (var entry in OrderedEntries)
        {
            lines.Add($"\t\"{entry.ID}\": \"{entry.Text}\"");  
        }

        return "{\n"+string.Join(",\n", lines)+"\n}";
    }

}