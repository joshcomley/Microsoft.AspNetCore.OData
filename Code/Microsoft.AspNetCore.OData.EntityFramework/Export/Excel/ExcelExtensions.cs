using System.Drawing;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Microsoft.AspNetCore.OData.EntityFramework.Export.Excel
{
    public static class ExcelExtensions
    {
        public const string HyperlinkStyle = "Hyperlink";
        public static string AddHyperLinkStyle(this ExcelWorkbook wb)
        {
            if (wb.Styles.NamedStyles.Any(x => x.Name == HyperlinkStyle))
            {
                return HyperlinkStyle;
            }

            var s = wb.Styles.CreateNamedStyle(HyperlinkStyle);
            s.Style.Font.UnderLine = true;
            s.Style.Font.Color.SetColor(Color.DarkBlue);
            return HyperlinkStyle;
        }

        public static ExcelRichText Add(this ExcelRichTextCollection richTextCollection,
            string text, 
            bool bold = false,
            bool italic = false, 
            Color? color = null, 
            float size = 11,
            bool underline = false, 
            bool strike = false,
            string fontName = null)
        {
            var richText = richTextCollection.Add(text);

            richText.Color = color ?? Color.Black;
            richText.Bold = bold;
            richText.Strike = strike;
            richText.Italic = italic;
            richText.Size = size;
            if (fontName != null)
            {
                richText.FontName = fontName;
            }
            richText.UnderLine = underline;

            return richText;
        }
    }
}