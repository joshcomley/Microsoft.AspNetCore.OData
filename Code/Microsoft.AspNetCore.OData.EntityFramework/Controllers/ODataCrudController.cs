using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Brandless.Data;
using Brandless.Data.Contracts;
using Brandless.Data.EntityFramework.Crud;
using Brandless.Data.Models;
using Iql.Queryable.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.EntityFramework.Extensions;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.OData.EntityFramework.Controllers
{
    [EnableQuery]
    public abstract class ODataCrudController<TDbContextSecured,
        TDbContextUnsecured,
        TUser,
        T> : Controller, IODataCrudController, IODataCrudController<T>
        where T : class
        where TUser : class
        where TDbContextSecured : DbContext
        where TDbContextUnsecured : DbContext
    {
        private ODataControllerData<TDbContextSecured, TDbContextUnsecured, TUser, T> _data;
        protected const string Id = "{key}";
        protected const string IdSlash = Id + "/";
        protected const string SingleId = "(id=" + Id + ")";

        public IEdmModelAccessor ModelAccessor => Data.ModelAccessor;
        protected virtual CrudBase<TDbContextSecured, TDbContextUnsecured, T> Crud => Data.Crud;

        protected ODataControllerData<TDbContextSecured, TDbContextUnsecured, TUser, T>
            Data => _data ?? (_data = ActivatorUtilities
                        .CreateInstance<ODataControllerData<TDbContextSecured, TDbContextUnsecured, TUser, T>>(
                            Services,
                            new CrudBase<TDbContextSecured, TDbContextUnsecured, T>(Services)));

        public TDbContextSecured Context => Data.Context;
        public UserManager<TUser> UserManager => Data.UserManager;
        public IRevisionKeyGenerator RevisionKeyGenerator => Data.RevisionKeyGenerator;
        public IServiceProvider Services => HttpContext.RequestServices;


        #region Security
        public T FindEntityById(params object[] id)
        {
            return Crud.Secured.Find(id);
        }
        #endregion Security

        #region GET
        // GET: api/Products
        [HttpGet]
        public virtual Task<IQueryable<T>> Get()
        {
            return Task.FromResult(Crud.Secured.All());
        }

        [HttpGet]
        public virtual Task<IActionResult> Get([ModelBinder(typeof(KeyValueBinder))]KeyValue[] keys)
        {
            IActionResult result;
            var entityQuery = Crud.Secured.FindQuery(keys);
            if (entityQuery == null || entityQuery.Count() != 1)
            {
                result = NotFound();
            }
            else
            {
                result = Ok(new SingleResult<T>(entityQuery));
            }
            return Task.FromResult(result);
        }

        #endregion GET

        #region POST
        // POST api/[Entities]
        [HttpPost]
        [ValidateModel]
        public virtual async Task<IActionResult> Post()
        {
            var jobject = ResolveJObject();
            await OnValidate(PostedEntity, jobject);
            return await Post(PostedEntity, jobject);
        }

        protected virtual async Task<IActionResult> Post(T patchEntity, JObject value)
        {
            if (ModelState.IsValid)
            {
                var entity = Activator.CreateInstance<T>();
                await OnBeforePostAndPatchAsync(entity, patchEntity, value);
                await OnBeforePostAsync(entity, patchEntity, value);
                await PatchObjectWithLegalPropertiesAsync(entity, patchEntity, value);

                //var locationUri = $"{req.Protocol}://{req.Host}/{req.Path}/{Crud.EntityId(entity)}";
                var result = await Crud.Secured.AddAndSaveAsync(entity);
                if (!result.Success)
                {
                    return ResolveHttpResult(result);
                }
                await OnAfterPostAsync(entity, patchEntity, value);
                await OnAfterPostAndPatchAsync(entity, patchEntity, value);
                return Ok(entity);
            }
            return this.ODataModelStateError();
        }
        #endregion POST

        #region PATCH
        // PATCH api/[Entities]/5
        [HttpPatch("{key}")]
        [ValidateModel]
        public virtual async Task<IActionResult> Patch([ModelBinder(typeof(KeyValueBinder))]KeyValue[] keys)
        {
            var entity = Crud.Secured.Find(keys);
            var currentEntity = await FindAsync(entity);
            return await Patch(keys, ResolveJObject(), currentEntity);
        }

        private JObject ResolveJObject()
        {
            return PostedJson == null
                ? new JObject()
                : JObject.Parse(PostedJson);
        }

        protected virtual Task<T> FindAsync(T entity)
        {
            return Task.FromResult(entity);
        }

        protected async Task<IActionResult> Patch(KeyValue[] key, JObject value, T currentEntity)
        {
            await OnValidate(PostedEntity, value);
            return await Patch(key, value, currentEntity, PostedEntity);
        }

        public virtual async Task<IActionResult> Patch(KeyValue[] key, JObject value, T currentEntity, T patchEntity)
        {
            await OnBeforePostAndPatchAsync(currentEntity, patchEntity, value);
            await OnBeforePatchAsync(key, currentEntity, patchEntity, value);
            await PatchObjectWithLegalPropertiesAsync(currentEntity, patchEntity, value);
            var oDataActionResult = await UpdateAsync(currentEntity);
            var result = ResolveHttpResult(oDataActionResult);
            await OnAfterPatchAsync(key, currentEntity, patchEntity, value);
            await OnAfterPostAndPatchAsync(currentEntity, patchEntity, value);
            return result;
        }

        protected IActionResult ResolveHttpResult(ApiActionResult apiActionResult)
        {
            IActionResult result = Ok("");
            if (!apiActionResult.Success)
            {
                if (apiActionResult.Errors.Any())
                {
                    var errors = new ODataError();
                    var errorDetails = new List<ODataErrorDetail>();
                    foreach (var error in apiActionResult.Errors)
                    {
                        errorDetails.Add(new ODataErrorDetail
                        {
                            ErrorCode = error.Key,
                            Message = error.Message,
                            Target = error.Target
                        });
                    }
                    errors.Details = new ArraySegment<ODataErrorDetail>(errorDetails.ToArray());
                    var badResult = BadRequest(errors);
                    errors.ErrorCode = "" + badResult.StatusCode;
                    result = badResult;
                }
                else
                {
                    result = NotFound();
                }
            }
            return result;
        }

        protected virtual async Task<UpdateActionResult> UpdateAsync(T currentEntity)
        {
            var updateResult = await Crud.Secured.UpdateAndSaveAsync(currentEntity);
            return updateResult;
        }

        private async Task PatchObjectWithLegalPropertiesAsync(T currentEntity, T patchEntity, JObject value)
        {
            await OnBeforeFilterLegalPropertiesAsync(currentEntity, patchEntity, value);
            PatchEntityProperties(currentEntity, patchEntity, value);
            await OnAfterFilterLegalPropertiesAsync(currentEntity, patchEntity, value);
        }

        private void PatchEntityProperties(object currentEntity, object patchEntity, JObject value)
        {
            foreach (var property in value)
            {
                // If we don't allow get on this property in OData, don't allow set
                var entityType = currentEntity.GetType();
                if (!ModelAccessor.EdmModel.HasProperty(entityType, property.Key))
                    continue;
                var propertyInfo = entityType.GetProperty(property.Key);
                // Set the value to the value of the same property on the patch entity
                var patchedValue = propertyInfo.GetValue(patchEntity);
                if (typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType) &&
                    !typeof(string).IsAssignableFrom(propertyInfo.PropertyType)
                )
                {
                    var childEntityType = propertyInfo.PropertyType.GetGenericArguments().First();
                    var methodInfo = GetType().GetTypeInfo().GetRuntimeMethods()
                        .SingleOrDefault(m => m.Name == nameof(GetAndInclude))
                        .MakeGenericMethod(entityType);
                    var invoke = methodInfo
                        .Invoke(this, new object[] { currentEntity, propertyInfo.Name });
                    //var submittedList = (IList)currentEntity.GetPropertyValue(propertyInfo.Name);
                    var submittedList = (IList)patchedValue;
                    //var patchedList = (IList)patchedValue;
                    IList dbList = null;
                    if (invoke != null)
                    {
                        dbList = (IList)invoke
                            .GetPropertyValue(propertyInfo.Name);
                    }
                    var entityKey = Crud.Unsecured.Context.Model.FindEntityType(childEntityType).GetKeys()
                        .Single(k => k.IsPrimaryKey());
                    if (submittedList != null && dbList == null)
                    {
                        dbList = submittedList;
                    }
                    else if (submittedList != null && dbList != null)
                    {
                        var toRemove = new List<object>();
                        foreach (var dbChild in dbList)
                        {
                            var found = false;
                            var i = 0;
                            foreach (var submittedChild in submittedList)
                            {
                                var match = true;
                                foreach (var keyProperty in entityKey.Properties)
                                {
                                    var dbValue = dbChild.GetPropertyValue(keyProperty.Name);
                                    var submittedValue = submittedChild.GetPropertyValue(keyProperty.Name);
                                    if (!Equals(dbValue, submittedValue))
                                    {
                                        match = false;
                                        break;
                                    }
                                }
                                if (match)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                toRemove.Add(dbChild);
                            }
                        }
                        foreach (var itemToRemove in toRemove)
                        {
                            dbList.Remove(itemToRemove);
                        }
                        foreach (var submittedChild in submittedList)
                        {
                            foreach (var keyProperty in entityKey.Properties)
                            {
                                var submittedValue = submittedChild.GetPropertyValue(keyProperty.Name);
                                if (submittedValue.IsDefaultValue())
                                {
                                    dbList.Add(submittedChild);
                                    break;
                                }
                            }

                        }
                    }
                    //newList.Add()
                    if (dbList != null)
                    {
                        foreach (var child in dbList)
                        {
                            //var newChild = Activator.CreateInstance(childEntityType);
                            var isNew = false;
                            foreach (var keyProperty in entityKey.Properties)
                            {
                                var propertyValue = child.GetPropertyValue(keyProperty.Name);
                                if (propertyValue.IsDefaultValue())
                                {
                                    isNew = true;
                                }
                            }
                            var index = -1;
                            if (!isNew)
                            {
                                for (var i = 0; i < submittedList.Count; i++)
                                {
                                    var match = true;
                                    foreach (var keyProperty in entityKey.Properties)
                                    {
                                        var localValue = child.GetPropertyValue(keyProperty.Name);
                                        var submittedValue = submittedList[i].GetPropertyValue(keyProperty.Name);
                                        if (!Equals(localValue, submittedValue))
                                        {
                                            match = false;
                                            break;
                                        }
                                    }
                                    if (match)
                                    {
                                        index = i;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                index = submittedList.IndexOf(child);
                            }
                            PatchEntityProperties(child, submittedList[index], property.Value[index] as JObject);
                        }
                        propertyInfo.SetValue(currentEntity, dbList);
                    }
                    //var edmType = ModelAccessor.EdmModel.GetEdmType(childEntityType) as EdmEntityType;
                    //if (edmType != null)
                    //{

                    //}
                    //foreach (var child in property.Value)
                    //{
                    //    var jobject = child as JObject;
                    //}
                }
                else
                {
                    propertyInfo.SetValue(currentEntity, patchedValue);
                }
            }
        }

        public TEntity GetAndInclude<TEntity>(TEntity entity, string property) where TEntity : class
        {
            var keys = Crud.Secured.IdProperties.Select(p => new KeyValue(p.Name, entity.GetPropertyValue(p.Name)))
                .ToArray();
            var expression = CrudHelper.KeyEqualsExpression<TEntity>(keys);
            return Crud.Secured.Context.Set<TEntity>().Include(property).Where(
                expression).SingleOrDefault();
        }

        #endregion PATCH

        #region PUT
        // PUT api/[Entities]/5
        [HttpPut("{key}")]
        [ValidateModel]
        public virtual async Task<IActionResult> Put([ModelBinder(typeof(KeyValueBinder))]KeyValue[] keys)
        {
            return await Patch(keys);
        }
        #endregion PUT

        #region DELETE
        // DELETE api/Products/5
        [HttpDelete("{key}")]
        public virtual async Task<IActionResult> Delete([ModelBinder(typeof(KeyValueBinder))]KeyValue[] keys)
        {
            //var entity = Crud.Secured.Find(id);
            //var securityFilterResult = await ApplySecurityFiltersAsync(
            //    null,
            //    entity,
            //    (securityFilter, context) => securityFilter.OnDeleteAsync(context));
            //if (securityFilterResult != null)
            //{
            //    return securityFilterResult;
            //}
            var result = await DeleteEntity(keys);
            switch (result.Result)
            {
                case DeleteEntityResult.NotFound:
                    return NotFound();
                case DeleteEntityResult.Conflict:
                    return BadRequest("Conflict");
            }
            return Ok();
        }

        protected virtual async Task<DeleteActionResult> DeleteEntity(params KeyValue[] key)
        {
            var result = await Crud.Secured.DeleteAndSaveAsync(key);
            return result;
        }

        #endregion DELETE

        #region Intercepts
        public virtual Task OnBeforePostAndPatchAsync(T currentEntity, T patchEntity, JObject jObject)
        {
            ClearClassProperties(patchEntity);
            return Task.FromResult(true);
        }

        /// <summary>
        /// We might have a class property accidentally propogated to us,
        /// in which case Entity Framework will try to insert this class
        /// into the database. Clear all of these, for now.
        /// </summary>
        /// <param name="patchEntity"></param>
        private void ClearClassProperties(T patchEntity)
        {
            if (patchEntity == null)
            {
                return;
            }
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                // If single relationship 
                var crud = new CrudBase<TDbContextSecured, TDbContextUnsecured, T>(Services);
                if (property.PropertyType.GetTypeInfo().IsClass &&
                    !property.PropertyType.GetInterfaces().Any(i => i.GetTypeInfo().IsGenericType &&
                                                                                  i.GetGenericTypeDefinition() ==
                                                                                  typeof(IEnumerable<>)))
                {
                    property.SetValue(patchEntity, null);
                }
            }
        }

        public virtual Task OnAfterPostAndPatchAsync(T currentEntity, T patchEntity, JObject jObject)
        {
            return Task.FromResult(true);
        }

        public virtual Task OnBeforePostAsync(T currentEntity, T patchEntity, JObject jObject)
        {
            return Task.FromResult(true);
        }

        public virtual Task OnAfterPostAsync(T currentEntity, T patchEntity, JObject jObject)
        {
            return Task.FromResult(true);
        }

        public virtual Task OnBeforePatchAsync(KeyValue[] id, T currentEntity, T patchEntity, JObject jObject)
        {
            return Task.FromResult(true);
        }

        public virtual Task OnAfterPatchAsync(KeyValue[] id, T currentEntity, T patchEntity, JObject jObject)
        {
            return Task.FromResult(true);
        }

        public virtual Task OnBeforeFilterLegalPropertiesAsync(T currentEntity, T patchEntity, JObject jObject)
        {
            return Task.FromResult(true);
        }

        public virtual Task OnAfterFilterLegalPropertiesAsync(T currentEntity, T patchEntity, JObject jObject)
        {
            return Task.FromResult(true);
        }

        protected virtual Task OnValidate(T patchEntity, JObject value)
        {
            return Task.FromResult(true);
        }
        #endregion Intercepts

        object IODataCrudController.PostedEntity
        {
            get { return PostedEntity; }
            set { PostedEntity = (T)value; }
        }

        public T PostedEntity { get; set; }
        public string PostedJson { get; set; }

        public Type EntityType => typeof(T);
        object IODataCrudController.FindEntityById(params object[] id)
        {
            return FindEntityById(id);
        }

        protected virtual string GetCurrentUserId()
        {
            return UserManager.GetUserId(HttpContext.User);
        }
    }
}
