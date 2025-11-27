namespace DinkCompiler;

using ClosedXML.Excel; 
public struct VoiceEntry
{
    public required string ID { get; set; }
    public required string Character { get; set; }
    public required string Qualifier { get; set; }
    public required string Line { get; set; }
    public required string Direction { get; set; }
    public required string SnippetID { get; set; }
    public required List<string> SnippetComments { get; set; }
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
                        result[id] = audioFolder.State;
                    }
                }
            }
        }

        return result;
    }
        
    class VoiceEntryExport
    {
        public required string ID { get; set; }
        public required string SectionID { get; set; }
        public required string SectionComments { get; set; }
        public required string Character { get; set; }
        public required string Qualifier { get; set; }
        public required string Actor { get; set; }
        public required string Line { get; set; }
        public required string Direction { get; set; }
        public required string Comments { get; set; }
        public required string Tags { get; set; }
        public required string AudioStatus {get; set;}
    }

    public bool WriteToExcel(string rootName, Characters? characters, 
                            Dictionary<string, string?> audioFileStatuses, 
                            string destVoiceFile)
    {
        List<VoiceEntryExport> recordsToExport = OrderedEntries.Select(v => new VoiceEntryExport
        {
            ID = v.ID,
            SectionID = v.SnippetID,
            SectionComments = string.Join(", ", v.SnippetComments),
            Character = v.Character,
            Qualifier = v.Qualifier,
            Actor = (characters != null) ? characters.Get(v.Character)?.Actor ?? "" : "", 
            Line = v.Line,
            Direction = v.Direction,
            Comments = string.Join(", ", v.Comments),
            Tags = string.Join(", ", v.Tags),
            AudioStatus = audioFileStatuses[v.ID]??"Unknown"
        }).ToList();

        if (recordsToExport.Count>0) {
            for (int i = 1; i < recordsToExport.Count; i++)
            {   
                if (recordsToExport[i].SectionID == recordsToExport[i-1].SectionID)
                {
                    recordsToExport[i].SectionComments = "";
                }
            }
        }

        try
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Voice Lines - " + rootName);

                var table = worksheet.Cell("A1").InsertTable(recordsToExport);

                worksheet.ColumnsUsed().AdjustToContents();

                table.FirstRow().Style.Fill.BackgroundColor = XLColor.LightBlue;
                table.FirstRow().Style.Font.Bold = true;

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

