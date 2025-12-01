namespace DinkCompiler;

using ClosedXML.Excel; 
public struct VoiceEntry
{
    public required string ID { get; set; }
    public required string BlockID { get; set; }
    public required string Character { get; set; }
    public required string Qualifier { get; set; }
    public required string Line { get; set; }
    public required string Direction { get; set; }
    public required string SnippetID { get; set; }
    public required List<string> SnippetComments { get; set; }
    public required List<string> BraceComments { get; set; }
    public required string GroupIndicator { get; set; }
    public required List<string> Comments { get; set; }
    public required List<string> Tags { get; set; }
}

public class VoiceLines
{
    private Dictionary<string, VoiceEntry> _entries = new Dictionary<string, VoiceEntry>();
    private List<string> _ids = new List<string>();

    public IEnumerable<VoiceEntry> OrderedEntries => _ids.Select(id => _entries[id]);
    public void Set(VoiceEntry entry)
    {
        if (!_ids.Contains(entry.ID))
        {
            _ids.Add(entry.ID);
        }

        _entries[entry.ID] = entry;
    }
        
    class VoiceEntryExport
    {
        public required string ID { get; set; }
        public required string BlockID { get; set; }
        public required string Character { get; set; }
        public required string Line { get; set; }
        public required string Direction { get; set; }
        public required string Comments { get; set; }
        public required string Actor { get; set; }
        public required string SectionID { get; set; }
        public required string Tags { get; set; }
        public required string AudioStatus {get; set;}
    }

    public bool WriteToExcel(string rootName, Characters? characters, 
                            WritingStatuses writingStatuses, bool ignoreWritingStatus,
                            AudioStatuses audioStatuses, 
                            string destVoiceFile)
    {
        bool useWritingStatus = !writingStatuses.IsEmpty()&&!ignoreWritingStatus;

        List<VoiceEntryExport> recordsToExport = OrderedEntries
            .Where(v => !useWritingStatus||writingStatuses.GetStatus(v.ID).Record)
            .Select(v => new VoiceEntryExport
            {
                ID = v.ID,
                BlockID = v.BlockID,
                SectionID = v.SnippetID,
                Character = v.Character,
                Actor = (characters != null) ? characters.Get(v.Character)?.Actor ?? "" : "", 
                Line = v.Line,
                Direction = v.Direction,
                Comments = (v.BraceComments.Count>0 ? string.Join("\n", v.BraceComments) + "\n" : "") + 
                        (v.SnippetComments.Count>0 ? string.Join("\n", v.SnippetComments) + "\n" : "") + 
                        (v.GroupIndicator != "" ? v.GroupIndicator + " " : "") +
                        string.Join("\n", v.Comments),
                Tags = string.Join(", ", v.Tags),
                AudioStatus = audioStatuses.GetStatus(v.ID).Status
            }).ToList();

        try
        {
            XLColor headerColor = XLColor.LightGreen;
            XLColor lineColor1 = XLColor.White;
            XLColor lineColor2 = XLColor.LightBlue;

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Voice Lines - " + rootName);

                var table = worksheet.Cell("A1").InsertTable(recordsToExport);

                ExcelUtils.FormatCommonTable(worksheet, table);

                string sectionHeading = ExcelUtils.FindColumnByHeading(worksheet, "SectionID") ?? "";

                string lastSection = "";
                XLColor sectionColor = lineColor2;
                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    var section = row.Cell(sectionHeading); // SectionID column
                    if (section.GetString() != lastSection)
                    {   
                        lastSection = section.GetString();
                        if (sectionColor == lineColor2)
                            sectionColor = lineColor1;
                        else
                            sectionColor = lineColor2;
                    }
                    row.Style.Fill.BackgroundColor = sectionColor;
                }   

                workbook.SaveAs(destVoiceFile);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error writing out voice lines Excel file {destVoiceFile}: " + ex.Message);
            return false;
        }
        return true;
    }

}

