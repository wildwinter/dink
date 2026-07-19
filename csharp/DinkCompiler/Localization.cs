namespace DinkCompiler;

using System.Text.Encodings.Web;
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
        // Property declaration order determines column order in the sheet.
        public required string ID { get; set; }
        public required string Text { get; set; }
        public required string Speaker { get; set; }
        public required string Gender { get; set; }
        public required string Comments { get; set; }
    }

    public bool WriteToExcel(string rootName, Characters? characters, WritingStatuses writingStatuses, bool ignoreWritingStatus, string destLocFile)
    {
        Console.WriteLine("Writing localisation file: " + destLocFile);

        bool useWritingStatus = writingStatuses.HasDefinitions()&&!ignoreWritingStatus;

        var recordsToExport = OrderedEntries
            .Where(v => (!useWritingStatus)||writingStatuses.GetStatus(v.ID).Loc)
            .Select(v => new LocEntryExport
            {
                ID = v.ID,
                Speaker = v.Speaker,
                // Grammatical gender of the speaker as M/F/N, blank when
                // unspecified or the speaker isn't a known character.
                Gender = (characters != null) ? characters.GenderCodeFor(v.Speaker) : "",
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

                // Keep the Gender column narrow for the usual M/F/N codes, but
                // don't clip it: grammatical gender is free text, so a project
                // using a longer value ("Common", "Inanimate") still needs to be
                // readable. AdjustSheet has already sized it to its contents, so
                // only enforce a minimum.
                string? genderCol = ExcelUtils.FindColumnByHeading(worksheet, "Gender");
                if (genderCol != null)
                {
                    var column = worksheet.Column(genderCol);
                    if (column.Width < 8) column.Width = 8;
                    column.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

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

    private List<PoEntry> GetPoEntries(Characters? characters, WritingStatuses writingStatuses, bool ignoreWritingStatus)
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

                // Spelled out rather than the M/F/N used in the spreadsheet -
                // PO comments have no width constraint, and "Grammatical" is
                // worth stating so translators don't read it as the character's
                // gender identity. Omitted entirely when non-specified.
                string gender = (characters != null) ? characters.GenderNameFor(v.Speaker) : "";
                if (!string.IsNullOrEmpty(gender))
                    entry.ExtractedComments.Add("Grammatical gender: " + gender);

                string comments = string.Join(" ", v.Comments);
                if (!string.IsNullOrEmpty(comments))
                    entry.ExtractedComments.Add(comments);
                return entry;
            }).ToList();
    }

    public bool WriteToPot(string rootName, Characters? characters, WritingStatuses writingStatuses, bool ignoreWritingStatus, string destPotFile)
    {
        Console.WriteLine("Writing POT file: " + destPotFile);
        var entries = GetPoEntries(characters, writingStatuses, ignoreWritingStatus);
        return PoUtils.WritePotFile(rootName, entries, destPotFile);
    }

    public bool WriteToPo(string rootName, Characters? characters, WritingStatuses writingStatuses, bool ignoreWritingStatus, string lang, string destPoFile)
    {
        Console.WriteLine("Writing PO file: " + destPoFile);

        var newEntries = GetPoEntries(characters, writingStatuses, ignoreWritingStatus);

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
        // Previously this method built JSON via string interpolation, which
        // left any '"' in entry.Text unescaped - producing broken JSON for
        // any line of dialogue containing a literal double-quote. Hand it
        // off to JsonSerializer so escapes are correct.
        //
        // UnsafeRelaxedJsonEscaping keeps the output readable (no & for
        // '&', < for '<' etc.) - safe here because this file is a game
        // runtime asset, never embedded in an HTML context.
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // Dictionary<string,string> preserves insertion order on enumeration
        // in modern .NET, and JsonSerializer enumerates it for output - so
        // the resulting JSON keys come out in OrderedEntries order.
        var entries = new Dictionary<string, string>(_ids.Count);
        foreach (var entry in OrderedEntries)
        {
            entries[entry.ID] = entry.Text;
        }
        return JsonSerializer.Serialize(entries, options);
    }

}