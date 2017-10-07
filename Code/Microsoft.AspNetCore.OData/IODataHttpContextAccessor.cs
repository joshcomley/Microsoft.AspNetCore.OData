using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OData
{
    public class ODataHttpContextAccessor
    {
        public ODataHttpContextAccessor()
        {
            HttpContextAccessor = new HttpContextAccessor();
        }

        public HttpContextAccessor HttpContextAccessor { get; set; }
    }
}