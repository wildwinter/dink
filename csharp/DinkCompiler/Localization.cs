namespace DinkCompiler;

using System.Text.Json;
using ClosedXML.Excel;
using Dink;
using SimpleVCLib;

public struct LocEntry
{
    public required string ID { get; set; }
    public required string Text { get; set; }
    public required List<string> Comments { get; set; }
    public required string Speaker { get; set; }
    public required DinkOrigin Origin {get;set;}
    public required bool IsDink {get;set;}
}

public class LocStrings
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

    public int Count {get{return _ids.Count;}}

    public string? GetText(string id)
    {
        if (_entries.TryGetValue(id, out LocEntry entry))
            return entry.Text;
        return null;
    }

    public void SetNonDink(string id, DinkOrigin origin)
    {
        if (_entries.TryGetValue(id, out LocEntry entry))
        {
            entry.Origin = origin;
            entry.IsDink = false;
            _entries[id] = entry;
        }
    }

    public void Remove(string id)
    {
        _entries.Remove(id);
        _ids.Remove(id);
    }
    
    class LocEntryExport
    {
        public required string ID { get; set; }
        public required string Text { get; set; }
        public required string Speaker { get; set; }
        public required string Comments { get; set; }
    }

    public bool WriteToExcel(string rootName, WritingStatuses writingStatuses, bool ignoreWritingStatus, string destLocFile)
    {
        Console.WriteLine("Writing localisation file: " + destLocFile);
        
        bool useWritingStatus = writingStatuses.HasDefinitions()&&!ignoreWritingStatus;

        var recordsToExport = OrderedEntries
            .Where(v => (!useWritingStatus)||writingStatuses.GetStatus(v.ID).Loc)
            .Select(v => new LocEntryExport
            {
                ID = v.ID,
                Speaker = v.Speaker,
                Text = v.Text,
                Comments = string.Join(" ", v.Comments)
            }).ToList();

        try
        {
            var prepResult = VCLib.PrepareToWrite(destLocFile);
            if (!prepResult.Success)
            {
                Console.Error.WriteLine($"Error writing out localisation Excel file {destLocFile}: {prepResult.Message}");
                return false;
            }

            XLColor headerColor = XLColor.LightGreen;

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Text Lines - " + rootName);

                var table = worksheet.Cell("A1").InsertTable(recordsToExport);

                ExcelUtils.FormatTableSheet(worksheet, table);
                ExcelUtils.AdjustSheet(worksheet);

                workbook.SaveAs(destLocFile);
            }

            var finishResult = VCLib.FinishedWrite(destLocFile);
            if (!finishResult.Success)
                Console.Error.WriteLine($"Warning: VC notification failed for '{destLocFile}': {finishResult.Message}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error writing out localisation Excel file {destLocFile}: " + ex.Message);
            return false;
        }
        return true;
    }

    private List<PoEntry> GetPoEntries(WritingStatuses writingStatuses, bool ignoreWritingStatus)
    {
        bool useWritingStatus = writingStatuses.HasDefinitions() && !ignoreWritingStatus;

        return OrderedEntries
            .Where(v => (!useWritingStatus) || writingStatuses.GetStatus(v.ID).Loc)
            .Select(v =>
            {
                var entry = new PoEntry
                {
                    Context = v.ID,
                    MsgId = v.Text,
                    MsgStr = ""
                };
                if (!string.IsNullOrEmpty(v.Speaker))
                    entry.ExtractedComments.Add("Speaker: " + v.Speaker);
                string comments = string.Join(" ", v.Comments);
                if (!string.IsNullOrEmpty(comments))
                    entry.ExtractedComments.Add(comments);
                return entry;
            }).ToList();
    }

    public bool WriteToPot(string rootName, WritingStatuses writingStatuses, bool ignoreWritingStatus, string destPotFile)
    {
        Console.WriteLine("Writing POT file: " + destPotFile);
        var entries = GetPoEntries(writingStatuses, ignoreWritingStatus);
        return PoUtils.WritePotFile(rootName, entries, destPotFile);
    }

    public bool WriteToPo(string rootName, WritingStatuses writingStatuses, bool ignoreWritingStatus, string lang, string destPoFile)
    {
        Console.WriteLine("Writing PO file: " + destPoFile);

        var newEntries = GetPoEntries(writingStatuses, ignoreWritingStatus);

        List<PoEntry> entriesToWrite;
        if (File.Exists(destPoFile))
        {
            string existingContent = File.ReadAllText(destPoFile);
            var (_, existingEntries) = PoUtils.ParsePoFile(existingContent);
            entriesToWrite = PoUtils.MergeEntries(newEntries, existingEntries);
        }
        else
        {
            entriesToWrite = newEntries;
        }

        return PoUtils.WritePoFile(rootName, lang, entriesToWrite, destPoFile);
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