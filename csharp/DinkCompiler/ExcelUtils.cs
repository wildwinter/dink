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

    public static void FormatCommonTable(IXLWorksheet worksheet, IXLTable table)
    {
        worksheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        worksheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        worksheet.ColumnsUsed().AdjustToContents();
        worksheet.RowsUsed().AdjustToContents();
        worksheet.SheetView.FreezeRows(1);

        table.ShowAutoFilter = true;

        table.FirstRow().Style.Fill.BackgroundColor = XLColor.DarkGreen;
        table.FirstRow().Style.Font.Bold = true;
        table.FirstRow().Style.Font.FontColor = XLColor.White;
    }
}