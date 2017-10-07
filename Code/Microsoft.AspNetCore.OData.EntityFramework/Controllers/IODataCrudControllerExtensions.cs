using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.OData.EntityFramework.Controllers
{
    // ReSharper disable once InconsistentNaming

    public static class IODataCrudControllerExtensions
    {
        public static void SetModel(this IODataCrudController controller, ActionExecutingContext context)
        {
            if (controller.PostedEntity != null) return;
            var valueString = ReadAsString(context.HttpContext.Request);
            if (string.IsNullOrWhiteSpace(valueString))
            {
                //controller.TrySetPostedEntityFromId(context);
                return;
            }
            var value = JObject.Parse(valueString);
            controller.PostedEntity = (controller as Controller).GetODataModel(controller.EntityType, value, false);
            controller.PostedJson = valueString;
            if (controller.PostedEntity == null)
            {
                //controller.TrySetPostedEntityFromId(context);
            }
        }

        private const string IdKey = "id";
        private static void TrySetPostedEntityFromId(this IODataCrudController controller, ActionExecutingContext context)
        {
            if (context.ActionArguments.ContainsKey(IdKey))
            {
                controller.PostedEntity = controller.FindEntityById(context.ActionArguments[IdKey]);
            }
        }

        internal static object TryGetModelFromId(this IODataCrudController controller, ActionExecutingContext context)
        {
            return context.ActionArguments.ContainsKey(IdKey) ? controller.FindEntityById(context.ActionArguments[IdKey]) : null;
        }

        internal static object TryGetModelFromId<TController>(
            this IODataCrudController<TController> controller, ActionExecutingContext context, out bool hasId)
        {
            return controller.TryGetModelFromId<TController, TController>(context, out hasId);
        }

        internal static object TryGetModelFromId<TController, TEntity>(this IODataCrudController<TController> controller, ActionExecutingContext context, out bool hasId)
        {
            hasId = context.ActionArguments.ContainsKey(IdKey);
            return hasId ? controller.FindEntityById(context.ActionArguments[IdKey]) : default(TController);
        }

        private static string ReadAsString(HttpRequest request)
        {
            if (request.Body.CanSeek)
            {
                var pos = request.Body.Position;
                request.Body.Seek(0, SeekOrigin.Begin);
                var str = ReadStreamAsString(request.Body);
                request.Body.Seek(pos, SeekOrigin.Begin);
                return str;
            }
            if (request.Body.CanRead)
            {
                return ReadStreamAsString(request.Body);
            }
            return null;
        }

        private static string ReadStreamAsString(Stream stream)
        {
            string str;
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true))
            {
                str = reader.ReadToEnd();
            }
            return str;
        }
    }
}
