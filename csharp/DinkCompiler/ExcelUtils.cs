namespace DinkCompiler;

using ClosedXML.Excel;

public class ExcelUtils 
{
    public static string? FindColumnByHeading(IXLWorksheet worksheet, string headingText)
    {
        var firstRow = worksheet.Row(1);
        int lastColumn = worksheet.LastCellUsed()?.Address.ColumnNumber ?? 1;
        for (int col = 1; col <= lastColumn; col++)
        {
            var cell = firstRow.Cell(col);
            
            if (cell.TryGetValue<string>(out string cellValue) && 
                !string.IsNullOrWhiteSpace(cellValue) &&
                cellValue==headingText)
            {
                return cell.Address.ColumnLetter;
            }
        }
        return null; 
    }

    public static void FormatSheet(IXLWorksheet worksheet, bool text=true)
    {
        worksheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        if (text)
            worksheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        else
            worksheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    public static void AdjustSheet(IXLWorksheet worksheet)
    {
        worksheet.ColumnsUsed().AdjustToContents();
        worksheet.RowsUsed().AdjustToContents();
    }

    public static void FormatTableSheet(IXLWorksheet worksheet, IXLTable table, int freeze=1, bool text=true)
    {
        FormatSheet(worksheet, text);
        worksheet.SheetView.FreezeRows(freeze);
        table.ShowAutoFilter = true;
        FormatHeaderLine(table.FirstRow().AsRange());
    }

    public static void FormatStatBlock(IXLRange range)
    {
        FormatStatLine(range.FirstColumn().AsRange());
        FormatHeaderLine(range.FirstRow().AsRange());
        range.LastRow().Style.Font.Bold = true;
    }

    public static void FormatStatLine(IXLRange range)
    {
        range.Style.Fill.BackgroundColor = XLColor.LightGreen;
        range.Style.Font.Bold = false;
        range.Style.Font.FontColor = XLColor.Black;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    public static void FormatHeaderLine(IXLRange range)
    {
        range.Style.Fill.BackgroundColor = XLColor.DarkGreen;
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }
}