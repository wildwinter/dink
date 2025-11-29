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

class VoiceLines
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

    public Dictionary<string, string?> GatherAudioFileStatuses(List<AudioFolder> audioFolders)
    {
        var idArray = OrderedEntries.Select(v => v.ID).ToArray();
        var result = idArray.ToDictionary(
            id => id,
            id => (string?)null,
            StringComparer.OrdinalIgnoreCase);

        foreach (var audioFolder in audioFolders)
        {
            string audioFolderRoot = audioFolder.Folder;

            if (string.IsNullOrWhiteSpace(audioFolderRoot) || !Directory.Exists(audioFolderRoot))
                continue;

            foreach (var filePath in Directory.EnumerateFiles(audioFolderRoot, "*", SearchOption.AllDirectories))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

                // Only compare IDs to the *filename*, never to folder names
                foreach (var id in idArray)
                {
                    if (result[id] == null &&
                        nameWithoutExt.StartsWith(id, StringComparison.OrdinalIgnoreCase))
                    {
                        result[id] = audioFolder.Status;
                    }
                }
            }
        }

        return result;
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
                            WritingStatuses writingStatuses,
                            Dictionary<string, string?> audioFileStatuses, 
                            string destVoiceFile)
    {
        bool useWritingStatus = !writingStatuses.IsEmpty();

        List<VoiceEntryExport> recordsToExport = OrderedEntries
            .Where(v => !useWritingStatus||writingStatuses.GetDefinition(v.ID).Record)
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
                AudioStatus = audioFileStatuses[v.ID]??"Unknown"
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

