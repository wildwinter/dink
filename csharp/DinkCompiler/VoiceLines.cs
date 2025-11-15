namespace DinkCompiler;

using ClosedXML.Excel; 
public struct VoiceEntry
{
    public required string ID { get; set; }
    public required string Character { get; set; }
    public required string Qualifier { get; set; }
    public required string Line { get; set; }
    public required string Direction { get; set; }
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
    
    struct VoiceEntryExport
    {
        public required string ID { get; set; }
        public required string Character { get; set; }
        public required string Qualifier { get; set; }
        public required string Actor { get; set; }
        public required string Line { get; set; }
        public required string Direction { get; set; }
        public required string Comments { get; set; }
        public required string Tags { get; set; }
    }
        
    public bool WriteToExcel(string rootName, Characters? characters, string destVoiceFile)
    {
        var recordsToExport = OrderedEntries.Select(v => new VoiceEntryExport
        {
            ID = v.ID,
            Character = v.Character,
            Qualifier = v.Qualifier,
            Actor = (characters != null) ? characters.Get(v.Character)?.Actor ?? "" : "", 
            Line = v.Line,
            Direction = v.Direction,
            Comments = string.Join(", ", v.Comments),
            Tags = string.Join(", ", v.Tags)
        }).ToList();

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

