using System;

namespace Microsoft.AspNetCore.OData.EntityFramework.Controllers
{
    public interface IODataCrudController<T>
    {
        T PostedEntity { get; set; }
        T FindEntityById(object id);
    }

    public interface IODataCrudController
    {
        object PostedEntity { get; set; }
        string PostedJson { get; set; }
        Type EntityType { get; }
        object FindEntityById(object id);
    }
}
