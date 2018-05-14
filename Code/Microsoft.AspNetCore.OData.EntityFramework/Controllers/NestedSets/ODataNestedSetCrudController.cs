using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.OData.EntityFramework.Controllers.NestedSets
{
    //public interface INestedSetsProvider<in TEntity>
    //{
    //    Task<IActionResult> SlideDelete(TEntity node);

    //    Task<IActionResult> Delete(TEntity node);

    //    Task<IActionResult> MoveTo(TEntity node, TEntity newParent, NestedSetsNodeMoveKind kind);

    //    Task<IActionResult> InsertAsChildOf(TEntity node, TEntity parent = default(TEntity));

    //    Task<IActionResult> InsertToLeftOf(TEntity node, TEntity sibling);

    //    Task<IActionResult> InsertToRightOf(TEntity node, TEntity sibling);
    //}
    public class ODataNestedSetCrudController<TDbContextSecured,
        TDbContextUnsecured,
        TUser,
        T> : ODataCrudController<TDbContextSecured,
        TDbContextUnsecured,
        TUser,
        T> where TDbContextSecured : DbContext where TDbContextUnsecured : DbContext where TUser : class where T : class
    {
        public Task<IActionResult> SlideDelete([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys)
        {
            throw new System.NotImplementedException();
        }

        public override Task<IActionResult> Delete([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys)
        {
            throw new System.NotImplementedException();
        }

        public Task<IActionResult> MoveToLeftOf([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys, [FromBody]T newParent)
        {
            throw new System.NotImplementedException();
        }

        public Task<IActionResult> MoveToRightOf([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys, [FromBody]T newParent)
        {
            throw new System.NotImplementedException();
        }

        public Task<IActionResult> MoveToUnderneathOf([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys, [FromBody]T newParent)
        {
            throw new System.NotImplementedException();
        }

        public Task<IActionResult> InsertAsChildOf([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys, [FromBody]T parent = default(T))
        {
            throw new System.NotImplementedException();
        }

        public Task<IActionResult> InsertToLeftOf([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys, [FromBody]T sibling)
        {
            throw new System.NotImplementedException();
        }

        public Task<IActionResult> InsertToRightOf([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys, [FromBody]T sibling)
        {
            throw new System.NotImplementedException();
        }
    }
}