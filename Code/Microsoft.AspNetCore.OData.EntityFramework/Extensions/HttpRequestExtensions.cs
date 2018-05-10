using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.EntityFramework.Export;

namespace Microsoft.AspNetCore.OData.EntityFramework.Extensions
{
    public static class HttpRequestExtensions
    {
        public static bool IsKnownExportRequest(this HttpRequest request)
        {
            return request.IsKnownExportRequest(out var kind);
        }


        public static bool IsExportRequest(this HttpRequest request, out ExportKind kind, out string kindName)
        {
            kind = ExportKind.Unknown;
            if (request.Query.ContainsKey("export"))
            {
                var exportFormat = request.Query["export"].ToString().ToLower();
                if (!string.IsNullOrWhiteSpace(exportFormat))
                {
                    kindName = exportFormat.Trim();
                    switch (kindName)
                    {
                        case "excel":
                            kind = ExportKind.Excel;
                            break;
                    }
                    return true;
                }
            }

            kindName = null;
            return false;
        }

        public static bool IsKnownExportRequest(this HttpRequest request, out ExportKind kind)
        {
            if (request.IsExportRequest(out kind, out var name))
            {
                if (kind == ExportKind.Unknown)
                {
                    return false;
                }
            }
            return true;
        }
    }
}