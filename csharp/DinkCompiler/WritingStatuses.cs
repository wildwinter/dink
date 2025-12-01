namespace DinkCompiler;
using Dink;

using ClosedXML.Excel; 

public class WritingStatuses
{
    // id, wsTag
    private Dictionary<string, string> _entries = new Dictionary<string, string>();
    private List<string> _ids = new List<string>();

    private CompilerEnvironment _env;

    public WritingStatuses(CompilerEnvironment env)
    {
        _env = env;
    }

    public bool IsEmpty() {return _ids.Count==0;}

    private void Set(string id, string wsTag)
    {
        if (!_ids.Contains(id))
        {
            _ids.Add(id);
        }
        _entries[id] = wsTag;
    }

    public WritingStatusDefinition GetStatus(string id)
    {
        if (_entries.TryGetValue(id, out string? wsTag))
            return GetDefinitionByTag(wsTag);
        return new WritingStatusDefinition();
    }

    public WritingStatusDefinition GetDefinitionByTag(string wsTag)
    {
        var result = _env.WritingStatusOptions.FirstOrDefault(x => x.WsTag == wsTag);
        if (result!=null)
            return result;
        return new WritingStatusDefinition();
    }

    public bool Build(List<DinkScene> dinkScenes, List<NonDinkLine> nonDinkLines, LocStrings locStrings)
    {
        if (_env.WritingStatusOptions.Count == 0)
            return true;

        Console.WriteLine("Extracting writing statuses...");

        foreach (var scene in dinkScenes)
        {
            foreach (var block in scene.Blocks)
            {
                foreach (var snippet in block.Snippets)
                { 
                    foreach (var beat in snippet.Beats)
                    {
                        if (beat is DinkLine line)
                        {
                            string statusTag = line.GetTagsFor(["ws"]).FirstOrDefault() ?? "";
                            if (statusTag.StartsWith("ws:"))
                                statusTag = statusTag.Substring(3);

                            Set(line.LineID, statusTag);
                        }
                    }
                }
            }
        }

        foreach(var ndLine in nonDinkLines)
        {
            string statusTag = ndLine.GetTags(["ws"]).FirstOrDefault() ?? "";
            if (statusTag.StartsWith("ws:"))
                statusTag = statusTag.Substring(3);

            Set(ndLine.ID, statusTag);
        }

        return true;
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
            Status = GetStatus(id).Status
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