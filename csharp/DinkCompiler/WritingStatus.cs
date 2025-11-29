namespace DinkCompiler;

using ClosedXML.Excel; 

public struct WritingStatusEntry
{
    public required string ID { get; set; }
    public required string Text {get; set; }   
    public WritingStatusDefinition WritingStatus { get; set; }
}

public class WritingStatuses
{
    private Dictionary<string, WritingStatusEntry> _entries = new Dictionary<string, WritingStatusEntry>();
    private List<string> _ids = new List<string>();

    public bool IsEmpty() {return _ids.Count==0;}

    public IEnumerable<WritingStatusEntry> OrderedEntries => _ids.Select(id => _entries[id]);

    public void Set(WritingStatusEntry entry)
    {
        if (!_ids.Contains(entry.ID))
        {
            _ids.Add(entry.ID);
        }

        _entries[entry.ID] = entry;
    }

    public WritingStatusDefinition GetDefinition(string id)
    {
        WritingStatusDefinition def = new WritingStatusDefinition();
        if (_entries.TryGetValue(id, out WritingStatusEntry entry))
            def = entry.WritingStatus;
        return def;
    }

    class WritingStatusEntryExport
    {
        public required string ID { get; set; }
        public required string Text { get; set; }
        public required string Status { get; set; }
    }

    public bool WriteToExcel(string rootName, Dictionary<string, WritingStatusDefinition> writingStatusDefinitions, string destStatusFile)
    {
        Console.WriteLine("Writing writing status file: " + destStatusFile);

        Dictionary<string, WritingStatusDefinition> keysByStatus = writingStatusDefinitions.Values
            .ToDictionary(
                keySelector: def => def.Status, 
                elementSelector: def => def 
            );

        var recordsToExport = OrderedEntries.Select(v => new WritingStatusEntryExport
        {
            ID = v.ID,
            Text = v.Text,
            Status = v.WritingStatus.Status
        }).ToList();

        try
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Line Status - " + rootName);

                var table = worksheet.Cell("A1").InsertTable(recordsToExport);

                ExcelUtils.FormatCommonTable(worksheet, table);

                string statusHeading = ExcelUtils.FindColumnByHeading(worksheet, "Status") ?? "";

                XLColor lineColor = XLColor.AirForceBlue;
                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    var status = row.Cell(statusHeading).GetString(); // Status column

                    if (keysByStatus.TryGetValue(status, out WritingStatusDefinition? statusDef))
                    {
                        if (statusDef.Color!="")
                        {
                            row.Cell(statusHeading).Style.Fill.BackgroundColor = XLColor.FromHtml("#"+statusDef.Color);
                        }
                    }
                }   

                workbook.SaveAs(destStatusFile);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error writing out status Excel file {destStatusFile}: " + ex.Message);
            return false;
        }
        return true;
    }
}