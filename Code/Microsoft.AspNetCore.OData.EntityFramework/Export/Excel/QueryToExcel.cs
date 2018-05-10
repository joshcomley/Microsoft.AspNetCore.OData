using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Brandless.AspNetCore.OData.Extensions;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Reports;
using Brandless.AspNetCore.OData.Extensions.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.EntityFramework.Extensions;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Microsoft.AspNetCore.OData.EntityFramework.Export.Excel
{
    public class QueryToExcel<T>
    {
        private byte[] ListToExcel(HttpRequest request, IReadOnlyCollection<T> query, ModelConfiguration config)
        {
            var modelAccessor = request.HttpContext.RequestServices.GetService<IEdmModelAccessor>();
            var entityConfiguration = modelAccessor.EdmModel.ModelConfiguration().ForEntityType<T>();

            //var formatterMap = entityConfiguration
            //    .DisplayTextFormatterMap;
            //IEntityDisplayTextFormatter formatter;
            //if (formatterMap.Has("Report"))
            //{
            //    formatter = formatterMap.Get("Report");
            //}
            //else
            //{
            //    formatter = formatterMap.Default;
            //}
            var report = entityConfiguration.ReportDefinitions.GetDefault();
            using (var excelPackage = new ExcelPackage())
            {
                //Create the worksheet
                var ws = excelPackage.Workbook.Worksheets.Add("Result");

                //var imageFile = new FileInfo(@"https://i.imgur.com/G9ySTcg.jpg");
                //ws.Drawings.AddPicture("test", imageFile);
                //ws.CodeModule.
                excelPackage.Workbook.CreateVBAProject();

                var hyperlinkStyle = excelPackage.Workbook.AddHyperLinkStyle();

                //get our column headings
                var destinationRange = ws.Cells["A2"];
                int columnCount;
                if (report == null)
                {
                    report = new DynamicReportDefinition<T>(config);
                }
                if (report != null)
                {
                    var fields = report.Fields.ToArray();
                    columnCount = fields.Length;
                    for (var i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        var headerCell = ws.Cells[1, i + 1];
                        if (!string.IsNullOrWhiteSpace(fields[i].Title))
                        {
                            headerCell.Value = fields[i].Title;
                        }
                        if (field.Kind != ReportFieldKind.PercentageBar)
                        {
                            headerCell.AutoFilter = true;
                        }
                    }

                    var cellDictionary = new Dictionary<ExcelRange, bool>();
                    void SetCellValue(ExcelRange cell, object value, bool append = false)
                    {
                        var hasExistingValue = cellDictionary.ContainsKey(cell);
                        if (!hasExistingValue)
                        {
                            cellDictionary.Add(cell, true);
                        }

                        if (!append || !hasExistingValue)
                        {
                            cell.Value = value;
                            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                        }
                        else
                        {
                            cell.Value = $"{cell.Value}\n{value}";
                            cell.Style.WrapText = true;
                        }
                    }

                    void ApplyField(IReportField field, ExcelRange cell, ExcelRange columnRange, object item, int column, int row, bool append = false)
                    {
                        object noValueValue = null;
                        if (string.IsNullOrWhiteSpace(cell.Text) && field.NoValueFormatter != null)
                        {
                            noValueValue = field.NoValueFormatter(item);
                        }
                        if (field.Link != null)
                        {
                            var link = field.Link(item);
                            cell.Hyperlink = new Uri(link);
                            switch (field.Style)
                            {
                                case ReportFieldStyle.Normal:
                                    cell.StyleName = ExcelExtensions.HyperlinkStyle;
                                    break;
                            }
                        }

                        var formatted = field.Formatter(item);
                        var fieldKind = field.Kind;
                        if (fieldKind == ReportFieldKind.Auto && formatted != null)
                        {
                            var type = formatted.GetType();
                            var underlyingType = Nullable.GetUnderlyingType(type);
                            if (underlyingType != null)
                            {
                                type = underlyingType;
                            }

                            if (type.IsNumericType())
                            {
                                fieldKind = ReportFieldKind.Number;
                            }
                            else if (type.IsCollection())
                            {
                                fieldKind = ReportFieldKind.Collection;
                            }
                            else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                            {
                                fieldKind = ReportFieldKind.Date;
                            }
                            else if (type == typeof(string))
                            {
                                fieldKind = ReportFieldKind.String;
                            }
                        }
                        switch (fieldKind)
                        {
                            case ReportFieldKind.Auto:
                                break;
                            case ReportFieldKind.Currency:
                                var decimalString = formatted + "";
                                if (decimal.TryParse(decimalString, out var dec))
                                {
                                    SetCellValue(cell, dec, append);
                                    cell.Style.Numberformat.Format = "£###,###,##0.00";
                                }
                                break;
                            case ReportFieldKind.Number:
                                SetCellValue(cell, formatted, append);
                                break;
                            case ReportFieldKind.String:
                                SetCellValue(cell, formatted, append);
                                break;
                            case ReportFieldKind.Collection:
                                var collectionField = field as IReportCollectionField;
                                var collection = collectionField.PropertyAccessor(item);
                                var nestedField = collectionField.CollectionField;
                                if (collection != null)
                                {
                                    foreach (var child in collection)
                                    {
                                        //sb.AppendLine(field)
                                        ApplyField(nestedField, cell, columnRange, child, column, row, true);
                                    }
                                }
                                break;
                            case ReportFieldKind.Date:
                                var dateString = formatted?.ToString();
                                if (DateTime.TryParse(dateString, out var date))
                                {
                                    //cell.Formula = $"=DATEVALUE({dateString})";
                                    if (!append)
                                    {
                                        cell.Formula = $"={date.ToOADate()}";
                                        cell.Style.Numberformat.Format = "ddd, MMMM d, yyyy";
                                        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                                    }
                                    else
                                    {
                                        SetCellValue(cell, date.ToString("R"), true);
                                    }
                                }
                                break;
                            case ReportFieldKind.ImageLink:
                                SetCellValue(cell, "___IMAGE___" + formatted, append);
                                break;
                            case ReportFieldKind.EmailAddress:
                                if (append)
                                {
                                    SetCellValue(cell, formatted, true);
                                }
                                else
                                {
                                    //cell.Formula = "HYPERLINK(\"mailto:" + field.Formatter(item) + "\",\"" + field.Formatter(item) + "\")";
                                    var uri = formatted.ToStringOrEmpty();
                                    if (!string.IsNullOrWhiteSpace(uri))
                                    {
                                        cell.Hyperlink = new Uri("mailto:" + uri);
                                    }

                                    cell.Value = uri;
                                    cell.StyleName = hyperlinkStyle;
                                    cell.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                                }
                                break;
                            case ReportFieldKind.Percentage:
                                if (formatted == null || Equals(formatted, 0))
                                {
                                    cell.Value = noValueValue;
                                }
                                else
                                {
                                    cell.Style.Numberformat.Format = "#0.00%";
                                    SetCellValue(cell, formatted, append);
                                }
                                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                                break;
                            case ReportFieldKind.PercentageBar:
                                //range.Style.Numberformat.Format = "#0.00%";
                                columnRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                                columnRange.IsRichText = true;

                                if (cell.RichText.Count > 0)
                                {
                                    cell.RichText.Add("\r\n");
                                }
                                var value = formatted;
                                var hasValue = !Equals(null, value);
                                if (hasValue)
                                {
                                    var numericValue = Convert.ToDouble(value);
                                    var c = Color.ForestGreen;
                                    if (numericValue < 0.8)
                                    {
                                        c = Color.YellowGreen;
                                    }
                                    if (numericValue < 0.6)
                                    {
                                        c = Color.DarkOrange;
                                    }
                                    if (numericValue < 0.3)
                                    {
                                        c = Color.Red;
                                    }

                                    const int maxRepetitions = 50;
                                    const float lineSize = 3f;
                                    var repetitions = (int)Math.Round(numericValue * maxRepetitions);
                                    const string fontName = "consolas";
                                    if (repetitions > 0)
                                    {
                                        cell.RichText.Add(new string('█', repetitions), color: c, bold: true, fontName: fontName, size: lineSize, underline: false);
                                    }
                                    var remainder = maxRepetitions - repetitions;
                                    if (remainder > 0)
                                    {
                                        cell.RichText.Add(new string('█', remainder), color: Color.LightGray, fontName: fontName, bold: true, size: lineSize, underline: false);
                                    }
                                    cell.RichText.Add("\u200B", color: Color.LightGray);
                                    if (field.CommentFormatter != null)
                                    {
                                        cell.RichText.Add("\r\n" + field.CommentFormatter(item), fontName: "Calibri", size: 10);
                                    }
                                }
                                cell.Style.WrapText = true;
                                break;
                        }
                    }

                    for (var i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        var column = i + 1;
                        var columnRange = ws.Cells[2, column, query.Count + 2, column];
                        var row = 2;
                        foreach (var item in query)
                        {
                            using (var cell = ws.Cells[row, column])
                            {
                                ApplyField(field, cell, columnRange, item, column, row);
                            }
                            row++;
                        }
                    }
                }
                else
                {
                    columnCount = typeof(T).GetProperties().Length;
                    var data = query.ToList();
                    destinationRange.LoadFromCollection(data, true);
                }

                //Format the header
                using (var headerRange = ws.Cells[1, 1, 1, columnCount])
                {
                    headerRange.AutoFilter = true;
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;                      //Set Pattern for the background to Solid
                    headerRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));  //Set color to dark blue
                    headerRange.Style.Font.Color.SetColor(Color.White);
                }

                using (var allRange = ws.Cells[1, 1, query.Count + 1, columnCount])
                {
                    allRange.AutoFitColumns();
                }

                void SetColumnWidth(IReportField field, int column)
                {
                    switch (field.Kind)
                    {
                        case ReportFieldKind.Collection:
                            SetColumnWidth((field as IReportCollectionField).CollectionField, column);
                            break;
                        case ReportFieldKind.PercentageBar:
                            ws.Column(column).Width = 30;
                            break;
                        case ReportFieldKind.ImageLink:
                            ws.Column(column).Width = 0;
                            break;
                        case ReportFieldKind.Date:
                            ws.Column(column).Width = 24;
                            break;
                    }
                }

                if (report != null)
                {
                    var fields = report.Fields.ToArray();
                    for (var i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        var column = i + 1;
                        SetColumnWidth(field, column);
                    }
                }

                for (var i = 2; i < query.Count + 2; i++)
                {
                    using (var row = ws.Cells[i, 1, i, columnCount])
                    {
                        row.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        if (i % 2 == 0)
                        {
                            row.Style.Fill.BackgroundColor.SetColor(Color.AliceBlue);
                        }
                        else
                        {
                            row.Style.Fill.BackgroundColor.SetColor(Color.White);
                        }
                        row.Style.Border.BorderAround(ExcelBorderStyle.None);
                    }
                }

                var borderColor = Color.LightSlateGray;
                var borderStyle = ExcelBorderStyle.Thin;
                using (var leftColumn = ws.Cells[1, 1, query.Count + 1, 1])
                {
                    leftColumn.Style.Border.Left.Style = borderStyle;
                    leftColumn.Style.Border.Left.Color.SetColor(borderColor);
                }
                using (var rightColumn = ws.Cells[1, columnCount, query.Count + 1, columnCount])
                {
                    rightColumn.Style.Border.Right.Style = borderStyle;
                    rightColumn.Style.Border.Right.Color.SetColor(borderColor);
                }
                using (var bottomRow = ws.Cells[query.Count + 1, 1, query.Count + 1, columnCount])
                {
                    bottomRow.Style.Border.Bottom.Style = borderStyle;
                    bottomRow.Style.Border.Bottom.Color.SetColor(borderColor);
                }
                using (var topRow = ws.Cells[1, 1, 1, columnCount])
                {
                    topRow.Style.Border.Top.Style = borderStyle;
                    topRow.Style.Border.Top.Color.SetColor(borderColor);
                }

                ws.View.FreezePanes(2, 1);

                var vbaCode = new StringBuilder();

                vbaCode.Append(@"Private Sub Workbook_Activate()
        Dim Result As Excel.Worksheet
        Dim ImageUrl As String
        Set Result = ThisWorkbook.Sheets(""Result"")
        Dim shp As Shape
        Dim imagesLoaded As Integer
        imagesLoaded = 0
        Dim Range As Range
        Dim Row As Integer
        Dim K As Long, r As Range, v As Variant
        K = 1
        Result.Activate
        For Each r In ActiveSheet.UsedRange
            v = r.Value
            If InStr(v, ""___IMAGE___"") > 0 Then
                K = K + 1
                imagesLoaded = imagesLoaded + 1
                Img r, 100
            End If
        Next r


        If imagesLoaded > 0 Then
            ThisWorkbook.Save
        End If
End Sub

Sub Img(cells As Range, size As Integer)
    Dim filePath As String
    filePath = Environ(""TEMP"") & ""\asfklasfnklfs.jpg""
    Dim ImageUrl As String
    ImageUrl = cells.Value
    ImageUrl = Split(ImageUrl, ""___IMAGE___"")(1)
    If Trim(ImageUrl & vbNullString) <> vbNullString Then
        'imagesLoaded = imagesLoaded + 1
        Download_File ImageUrl, filePath
        Set shp = ActiveSheet.Shapes.AddPicture(filePath, True, True, 100, 100, -1, -1)
        shp.Top = cells.Top
        shp.Left = cells.Left
        cells.RowHeight = size * 1.1
        cells.ColumnWidth = (size / 5) * 2
        shp.Height = size
    End If
    cells.Value = """"
End Sub

Function FileExists(ByVal FileToTest As String) As Boolean
   FileExists = (Dir(FileToTest) <> """")
End Function

Sub DeleteFile(ByVal FileToDelete As String)
   If FileExists(FileToDelete) Then 'See above
      ' First remove readonly attribute, if set
      SetAttr FileToDelete, vbNormal
      ' Then delete the file
      Kill FileToDelete
   End If
End Sub

Function Download_File(ByVal vWebFile As String, ByVal vLocalFile As String) As Boolean
Dim oXMLHTTP As Object, i As Long, vFF As Long, oResp() As Byte

'You can also set a ref. to Microsoft XML, and Dim oXMLHTTP as MSXML2.XMLHTTP
Set oXMLHTTP = CreateObject(""MSXML2.XMLHTTP"")
oXMLHTTP.Open ""GET"", vWebFile, False 'Open socket to get the website
oXMLHTTP.Send 'send request

'Wait for request to finish
Do While oXMLHTTP.readyState <> 4
DoEvents
Loop

oResp = oXMLHTTP.responseBody 'Returns the results as a byte array

'Create local file and save results to it
vFF = FreeFile
If Dir(vLocalFile) <> """" Then Kill vLocalFile
Open vLocalFile For Binary As #vFF
Put #vFF, , oResp
Close #vFF

'Clear memory
Set oXMLHTTP = Nothing
End Function");
                excelPackage.Workbook.CodeModule.Code = vbaCode.ToString();

                excelPackage.Save();

                //var heights = new List<double>();
                //for (var row = 2; row <= query.Count + 1; row++)
                //{
                //    var excelRow = ws.Row(row);
                //    heights.Add(excelRow.Height);
                //}
                //var maxHeight = heights.Max();
                //for (var row = 2; row <= query.Count + 1; row++)
                //{
                //    ws.Row(row).Height = 30;
                //}

                //excelPackage.Save();
                var bytes = StreamToByteArray(excelPackage.Stream);
                return bytes;
            }
        }

        public async Task<byte[]> GetReportAsync(HttpRequest request, IQueryable<T> queryable, ModelConfiguration model)
        {
            //var modelAccessor = request.HttpContext.RequestServices.GetService<IEdmModelAccessor>();
            //var person = new Person();
            //person.Title = "Josh";
            //person.Id = 7;
            //var formatterMap = modelAccessor.EdmModel.ModelConfiguration().ForEntityType<Person>()
            //    .DisplayTextFormatterMap;
            //IEntityDisplayTextFormatter formatter;
            //if (formatterMap.Has("Report"))
            //{
            //    formatter = formatterMap.Get("Report");
            //}
            //else
            //{
            //    formatter = formatterMap.Default;
            //}
            //var personNameFormatted = formatter.Format(person);
            var data = await queryable.ToListWithODataRequestAsync(request);
            return ListToExcel(request, data, model);
            //var dataSet = ListConvertor.ConvertToDataSet(data);
            //var memoryStream = new MemoryStream();
            //ExcelLibrary.DataSetHelper.CreateWorkbook(memoryStream, dataSet);
            //return StreamToByteArray(memoryStream);
            //var odataQueryApplicator = new ODataQueryApplicator();
            //var data = await odataQueryApplicator.ProcessQueryAsync(request, queryable, typeof(T));
            //using (var excelFile = new ExcelPackage())
            //{
            //    //Create the worksheet
            //    var ws = excelFile.Workbook.Worksheets.Add("Result");

            //    ////get our column headings
            //    //var t = typeof(T);
            //    //var headings = t.GetProperties();
            //    //for (int i = 0; i < headings.Length; i++)
            //    //{

            //    //    ws.Cells[1, i + 1].Value = headings[i].Name;
            //    //}

            //    ////populate our Data
            //    //if (data.Any())
            //    //{
            //    //    ws.Cells["A2"].LoadFromCollection(data, true, TableStyles.Dark1);
            //    //}

            //    ////Format the header
            //    //using (ExcelRange rng = ws.Cells["A1:BZ1"])
            //    //{
            //    //    rng.Style.Font.Bold = true;
            //    //    rng.Style.Fill.PatternType = ExcelFillStyle.Solid; //Set Pattern for the background to Solid
            //    //    rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189)); //Set color to dark blue
            //    //    rng.Style.Font.Color.SetColor(Color.White);
            //    //}
            //    //    var worksheet = excelFile.Workbook.Worksheets.Add("Sheet1");
            //    ws.Cells["A1"]
            //        //.LoadFromDataTable(dataSet.Tables[0], true)
            //        .LoadFromCollection(data, true, TableStyles.Dark1);
            //    excelFile.Save();
            //    var bytes = StreamToByteArray(excelFile.Stream);
            //    await File.WriteAllBytesAsync(@"D:\Code\ReportGen.xslx", bytes);
            //    return bytes;
            //}
        }

        public static byte[] StreamToByteArray(Stream stream)
        {
            if (stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }
            // Jon Skeet's accepted answer 
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}