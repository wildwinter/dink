namespace DinkCompiler;
using ClosedXML.Excel; 

public struct WritingStatusEntry
{
    public required string ID { get; set; }
    public WritingStatusDefinition WritingStatus { get; set; }
}

public class WritingStatuses
{
    private Dictionary<string, WritingStatusDefinition> _entries = new Dictionary<string, WritingStatusDefinition>();
    private List<string> _ids = new List<string>();

    public bool IsEmpty() {return _ids.Count==0;}

    public void Set(string id, WritingStatusDefinition statusDefinition)
    {
        if (!_ids.Contains(id))
        {
            _ids.Add(id);
        }

        _entries[id] = statusDefinition;
    }

    public WritingStatusDefinition GetDefinition(string id)
    {
        if (_entries.TryGetValue(id, out WritingStatusDefinition? def))
            return def;
        return new WritingStatusDefinition();
    }

    class WritingStatusEntryExport
    {
        public required string ID { get; set; }
        public required string Text { get; set; }
        public required string Status { get; set; }
    }

    public bool WriteToExcel(string rootName, LocStrings locStrings, List<WritingStatusDefinition> writingStatusDefinitions, string destStatusFile)
    {
        Console.WriteLine("Writing writing status file: " + destStatusFile);

        Dictionary<string, WritingStatusDefinition> keysByStatus = writingStatusDefinitions
            .ToDictionary(
                keySelector: def => def.Status, 
                elementSelector: def => def 
            );

        var recordsToExport = _ids.Select(id => new WritingStatusEntryExport
        {
            ID = id,
            Text = locStrings.GetText(id)??"",
            Status = _entries[id].Status
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