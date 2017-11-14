using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.OData.Routing.Conventions
{
    public class KeyValueBinder : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var key = bindingContext.ModelName;
            var value = bindingContext.ActionContext.RouteData.Values[key] as List<KeyValuePair<string, object>>;
            if (value != null)
            {
                bindingContext.Result = ModelBindingResult.Success(value.ToArray());
            }
        }
    }
}