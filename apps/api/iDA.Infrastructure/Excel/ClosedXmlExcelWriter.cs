using ClosedXML.Excel;
using Ida.Application.Common;

namespace Ida.Infrastructure.Excel;

public class ClosedXmlExcelWriter : IExcelWriter
{
    private const string AmountFormat = "#,##0.00;(#,##0.00)";
    private const string DateFormat = "dd/MM/yyyy";

    public byte[] Write<T>(IEnumerable<T> rows, IReadOnlyList<ExcelColumn<T>> columns,
        string sheetName)
    {
        using var workbook = new XLWorkbook();

        var sheet = workbook.Worksheets.Add(Sanitise(sheetName));

        for (var c = 0; c < columns.Count; c++)
        {
            var header = sheet.Cell(1, c + 1);
            header.Value = columns[c].Header;
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
        }

        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < columns.Count; c++)
            {
                var column = columns[c];
                var cell = sheet.Cell(r, c + 1);

                switch (column.Value(row))
                {
                    case null:
                        break;
                    case decimal amount:
                        cell.Value = amount;
                        cell.Style.NumberFormat.Format = column.Format ?? AmountFormat;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        break;
                    case DateOnly date:
                        cell.Value = date.ToDateTime(TimeOnly.MinValue);
                        cell.Style.NumberFormat.Format = column.Format ?? DateFormat;
                        break;
                    case DateTimeOffset moment:
                        cell.Value = moment.DateTime;
                        cell.Style.NumberFormat.Format = column.Format ?? DateFormat;
                        break;
                    case bool flag:
                        cell.Value = flag ? "ใช่" : "ไม่ใช่";
                        break;
                    case int or long or short or double:
                        cell.Value = System.Convert.ToDouble(column.Value(row));
                        break;
                    case Enum e:
                        cell.Value = e.ToString();
                        break;
                    case { } value:
                        cell.Value = value.ToString();
                        break;
                }
            }

            r++;
        }

        sheet.SheetView.FreezeRows(1);
        if (columns.Count > 0)
            sheet.Range(1, 1, Math.Max(r - 1, 1), columns.Count).SetAutoFilter();
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string Sanitise(string name)
    {
        var cleaned = new string(name.Where(c => !"[]:*?/\\".Contains(c)).ToArray()).Trim();
        if (cleaned.Length == 0) cleaned = "Export";
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }
}
