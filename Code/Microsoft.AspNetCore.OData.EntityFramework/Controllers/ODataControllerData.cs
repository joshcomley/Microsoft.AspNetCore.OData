using Brandless.Data.Contracts;
using Brandless.Data.EntityFramework.Crud;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.OData.EntityFramework.Controllers
{
    public class ODataControllerData<TApiSecured, TApiUnsecured, TUser, TModel>
        where TModel : class 
        where TApiUnsecured : DbContext 
        where TApiSecured : DbContext
        where TUser : class
    {
        public IEdmModelAccessor ModelAccessor { get; }
        public UserManager<TUser> UserManager { get; }
        public TApiSecured Context { get; }
        public CrudBase<TApiSecured, TApiUnsecured, TModel> Crud { get; set; }
        public IRevisionKeyGenerator RevisionKeyGenerator { get; }

        public ODataControllerData(
            CrudBase<TApiSecured, TApiUnsecured, TModel> crud,
            IEdmModelAccessor modelAccessor,
            UserManager<TUser> userManager,
            TApiSecured context,
            IRevisionKeyGenerator revisionKeyGenerator)
        {
            Crud = crud;
            ModelAccessor = modelAccessor;
            UserManager = userManager;
            Context = context;
            RevisionKeyGenerator = revisionKeyGenerator;
        }
    }
}