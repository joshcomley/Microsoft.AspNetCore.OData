using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Brandless.Data;
using Brandless.Data.Contracts;
using Brandless.Data.Entities;
using Brandless.Data.EntityFramework.Crud;
using Brandless.Data.Models;
using Brandless.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Extensions.Configuration;
using Microsoft.AspNetCore.OData.Extensions.Validation;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
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
        private MethodInfo _validateEntityMethod;
        protected const string Id = "{key}";
        protected const string IdSlash = Id + "/";
        protected const string SingleId = "(id=" + Id + ")";

        public virtual IEdmModelAccessor ModelAccessor => Data.ModelAccessor;
        protected virtual CrudBase<TDbContextSecured, TDbContextUnsecured, T> Crud => Data.Crud;

        protected ODataControllerData<TDbContextSecured, TDbContextUnsecured, TUser, T>
            Data => _data ?? (_data = ActivatorUtilities
                        .CreateInstance<ODataControllerData<TDbContextSecured, TDbContextUnsecured, TUser, T>>(
                            Services,
                            new CrudBase<TDbContextSecured, TDbContextUnsecured, T>(Services)));

        public virtual TDbContextSecured Context => Data.Context;
        public virtual UserManager<TUser> UserManager => Data.UserManager;
        public virtual IRevisionKeyGenerator RevisionKeyGenerator => Data.RevisionKeyGenerator;
        public virtual IServiceProvider Services => HttpContext.RequestServices;

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
        public virtual Task<IActionResult> Get([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys)
        {
            IActionResult result;
            var entityQuery = Crud.Secured.FindQuery(keys.Cast<object>().ToArray());
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

        [HttpGet]
        public virtual Task<IActionResult> GetNavigationProperty([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys,
            string navigationProperty)
        {
            var propertyType = typeof(T).GetProperty(navigationProperty).PropertyType;
            if (typeof(IEnumerable).IsAssignableFrom(propertyType) && !typeof(string).IsAssignableFrom(propertyType))
            {
                propertyType = propertyType.GetGenericArguments()[0];
            }
            var typedResult = (GetNavigationPropertyTypedResult)GetType().GetMethod(nameof(GetNavigationPropertyTyped))
                .MakeGenericMethod(propertyType)
                .Invoke(this, new object[] { keys, navigationProperty });
            IActionResult result;
            if (typedResult.IsSingleResult && typedResult.SingleResult == null)
            {
                result = NotFound();
            }
            else
            {
                result = typedResult.IsSingleResult 
                    ? Ok(typedResult.SingleResult) 
                    : Ok(typedResult.Queryable);
            }
            return Task.FromResult(result);
        }

        public virtual GetNavigationPropertyTypedResult GetNavigationPropertyTyped<TEntity>([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys,
            string navigationProperty) where TEntity : class
        {
            var edmType = ModelAccessor.EdmModel.GetEdmType(typeof(T)) as EdmEntityType;
            var navProperty = edmType.NavigationProperties().Single(p => p.Name == navigationProperty);
            Expression<Func<TEntity, bool>> exp;
            var isSingleResult = false;
            if (navProperty.Partner.ReferentialConstraint != null)
            {
                var constraint = navProperty.Partner.ReferentialConstraint;
                var keyValuePairs = new List<WhereQuery>();
                foreach (var propertyPair in constraint.PropertyPairs)
                {
                    var keyValue = 
                        new WhereQuery(
                            propertyPair.DependentProperty.Name,
                            keys.Single(k => k.Key == propertyPair.PrincipalProperty.Name).Value,
                            typeof(TEntity).GetProperty(propertyPair.DependentProperty.Name).PropertyType);
                    keyValuePairs.Add(keyValue);
                }
                exp = Brandless.Data.EntityFramework.QueryBuilder.WhereExpression<TEntity>(keyValuePairs.ToArray());
            }
            else
            {
                isSingleResult = true;
                var keyEqualsExpression = Crud.Secured.KeyEqualsExpression(keys.Cast<object>().ToArray());
                var entity = Crud.Secured.Context.Set<T>()
                    .Single(keyEqualsExpression);
                var keyValuePairs = new List<WhereQuery>();
                foreach (var propertyPair in navProperty.ReferentialConstraint.PropertyPairs)
                {
                    var propertyValue = entity.GetPropertyValue(propertyPair.DependentProperty.Name);
                    var propertyName = propertyPair.PrincipalProperty.Name;
                    var principalType = typeof(TEntity).GetProperty(propertyPair.PrincipalProperty.Name).PropertyType;
                    var dependantType = typeof(T).GetProperty(propertyPair.DependentProperty.Name).PropertyType;
                    if (Nullable.GetUnderlyingType(dependantType) != null &&
                        Nullable.GetUnderlyingType(principalType) == null &&
                        Equals(null, propertyValue))
                    {
                        return new GetNavigationPropertyTypedResult(null, true, null);
                    }
                    var detail = new WhereQuery(
                        propertyName, 
                        propertyValue, 
                        principalType);
                    keyValuePairs.Add(detail);
                }
                exp = Brandless.Data.EntityFramework.QueryBuilder.WhereExpression<TEntity>(keyValuePairs.ToArray());
            }

            var entityQuery = Crud.Secured.Context.Set<TEntity>().Where(exp);
            return new GetNavigationPropertyTypedResult(entityQuery, isSingleResult,
                isSingleResult ? new SingleResult<TEntity>(entityQuery) : null);
        }

        #endregion GET

        #region POST
        // POST api/[Entities]
        [HttpPost]
        [LoadModel]
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
                InitializeEntity(patchEntity, new List<object>());
                await OnBeforePostAndPatchAsync(entity, patchEntity, value);
                await OnBeforePostAsync(entity, patchEntity, value);
                await PatchObjectWithLegalPropertiesAsync(entity, patchEntity, value);
                ModelState.Clear();
                if (!ValidateEntity(entity))
                {
                    return this.ODataModelStateError();
                }
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

        #region VALIDATION

        protected MethodInfo ValidateEntityMethod
        {
            get
            {
                if (_validateEntityMethod == null)
                {
                    _validateEntityMethod = GetType().GetMethod(nameof(ValidateEntity));
                }
                return _validateEntityMethod;
            }
        }

        protected bool InvokeValidateEntity(object entity, Dictionary<object, bool> validated, string path)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            return (bool)ValidateEntityMethod.MakeGenericMethod(entity.GetType())
                .Invoke(this, new[] { entity, validated, path });
        }

        public virtual bool ValidateEntity<TEntity>(TEntity entity, Dictionary<object, bool> validated = null, string path = "")
        {
            validated = validated ?? new Dictionary<object, bool>();
            if (validated.ContainsKey(entity))
            {
                return validated[entity];
            }
            var iqlValidation = ValidationMap.ForType<TEntity>();
            var accessor = string.IsNullOrWhiteSpace(path) ? "" : ".";
            var isValid = !string.IsNullOrWhiteSpace(path) || TryValidateModel(entity);
            validated.Add(entity, isValid);
            if (iqlValidation?.EntityValidations != null)
            {
                foreach (var entityValidation in iqlValidation.EntityValidations)
                {
                    var iqlValidationResult = entityValidation.ValidationFunction(entity);
                    isValid = isValid && iqlValidationResult;
                    if (!iqlValidationResult)
                    {
                        ModelState.AddModelError(path, entityValidation.Message);
                    }
                }
            }
            if (iqlValidation?.PropertyValidations != null)
            {
                foreach (var propertyValidationCollection in iqlValidation.PropertyValidations)
                {
                    foreach (var propertyValidation in propertyValidationCollection.Validations)
                    {
                        var iqlValidationResult = propertyValidation.ValidationFunction(entity);
                        isValid = isValid && iqlValidationResult;
                        if (!iqlValidationResult)
                        {
                            ModelState.AddModelError($"{path}{accessor}{propertyValidationCollection.PropertyName}", propertyValidation.Message);
                        }
                    }
                }
            }
            var entityType = entity.GetType();
            foreach (var property in entityType.GetRuntimeProperties())
            {
                var propertyType = property.PropertyType;
                var elementType = propertyType;
                var isSimpleEnumerable = false;
                if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType.IsGenericType)
                {
                    var genericArguments = propertyType.GetGenericArguments();
                    if (genericArguments.Length == 1)
                    {
                        elementType = genericArguments[0];
                        isSimpleEnumerable = true;
                    }
                }
                var relatedEntityType = Crud.Unsecured.Context.Model.FindEntityType(elementType);
                if (relatedEntityType != null)
                {
                    var value = entity.GetPropertyValue(property.Name);
                    if (value != null)
                    {
                        if (isSimpleEnumerable)
                        {
                            var i = 0;
                            foreach (var element in (IEnumerable)value)
                            {
                                var childPath = $"{path}{accessor}{property.Name}[{i}]";
                                isValid = InvokeValidateEntity(element, validated, childPath) && isValid;
                                i++;
                            }
                        }
                        else
                        {
                            var childPath = $"{path}{accessor}{property.Name}";
                            isValid = InvokeValidateEntity(value, validated, childPath) && isValid;
                        }
                    }
                }
            }
            return isValid;
        }
        #endregion VALIDATION

        #region PATCH
        // PATCH api/[Entities]/5
        [HttpPatch("{key}")]
        [LoadModel]
        public virtual async Task<IActionResult> Patch([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys)
        {
            var entity = Crud.Secured.Find(keys.Cast<object>().ToArray());
            var currentEntity = await FindAsync(entity);
            return await Patch(keys, ResolveJObject(), currentEntity);
        }

        protected virtual JObject ResolveJObject()
        {
            return PostedJson == null
                ? new JObject()
                : JObject.Parse(PostedJson);
        }

        protected virtual Task<T> FindAsync(T entity)
        {
            return Task.FromResult(entity);
        }

        protected virtual async Task<IActionResult> Patch(KeyValuePair<string, object>[] key, JObject value, T currentDatabaseEntity)
        {
            await OnValidate(PostedEntity, value);
            return await Patch(key, value, currentDatabaseEntity, PostedEntity);
        }

        public virtual async Task<IActionResult> Patch(KeyValuePair<string, object>[] key, JObject value, T currentDatabaseEntity, T patchEntity)
        {
            await OnBeforePostAndPatchAsync(currentDatabaseEntity, patchEntity, value);
            await OnBeforePatchAsync(key, currentDatabaseEntity, patchEntity, value);
            await PatchObjectWithLegalPropertiesAsync(currentDatabaseEntity, patchEntity, value);
            ModelState.Clear();
            if (!ValidateEntity(currentDatabaseEntity))
            {
                return this.ODataModelStateError();
            }
            var oDataActionResult = await UpdateAsync(currentDatabaseEntity);
            var result = ResolveHttpResult(oDataActionResult);
            await OnAfterPatchAsync(key, currentDatabaseEntity, patchEntity, value);
            await OnAfterPostAndPatchAsync(currentDatabaseEntity, patchEntity, value);
            return result;
        }

        protected virtual IActionResult ResolveHttpResult(ApiActionResult apiActionResult)
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

        protected virtual async Task PatchObjectWithLegalPropertiesAsync(T currentEntity, T patchEntity, JObject value)
        {
            await OnBeforeFilterLegalPropertiesAsync(currentEntity, patchEntity, value);
            await PatchEntityProperties(currentEntity, patchEntity, value);
            await OnAfterFilterLegalPropertiesAsync(currentEntity, patchEntity, value);
        }

        protected virtual async Task PatchEntityProperties(object currentEntity, object patchEntity, JObject value)
        {
            foreach (var property in value)
            {
                if (!ShouldPatchEntityProperty(currentEntity, patchEntity, property.Key))
                {
                    continue;
                }
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
                    //var submittedList = (IList)currentDatabaseEntity.GetPropertyValue(propertyInfo.Name);
                    var submittedList = (IList)patchedValue;
                    //var patchedList = (IList)patchedValue;
                    IList dbList = null;
                    if (invoke != null)
                    {
                        dbList = (IList)invoke.GetPropertyValue(propertyInfo.Name);
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
                            await PatchEntityProperties(child, submittedList[index], property.Value[index] as JObject);
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

        protected virtual bool ShouldPatchEntityProperty(object currentEntity, object patchEntity, string propertyKey)
        {
            return true;
        }

        public virtual TEntity GetAndInclude<TEntity>(TEntity entity, string property) where TEntity : class
        {
            var keys = Crud.Secured.IdProperties.Select(p => new KeyValuePair<string, object>(p.Name, entity.GetPropertyValue(p.Name)))
                .ToArray();
            var expression = CrudHelper.KeyEqualsExpression<TEntity>(keys);
            return Crud.Secured.Context.Set<TEntity>().Include(property).Where(
                expression).SingleOrDefault();
        }

        #endregion PATCH

        #region PUT
        // PUT api/[Entities]/5
        [HttpPut("{key}")]
        [LoadModel]
        public virtual async Task<IActionResult> Put([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys)
        {
            return await Patch(keys);
        }
        #endregion PUT

        #region DELETE
        // DELETE api/Products/5
        [HttpDelete("{key}")]
        public virtual async Task<IActionResult> Delete([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys)
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

        protected virtual async Task<DeleteActionResult> DeleteEntity(params KeyValuePair<string, object>[] key)
        {
            var result = await Crud.Secured.DeleteAndSaveAsync(key);
            return result;
        }

        #endregion DELETE

        #region Intercepts
        public virtual Task OnBeforePostAndPatchAsync(T currentEntity, T patchEntity, JObject jObject)
        {
            //ClearClassProperties(patchEntity);
            return Task.FromResult(true);
        }

        /// <summary>
        /// We might have a class property accidentally propogated to us,
        /// in which case Entity Framework will try to insert this class
        /// into the database. Clear all of these, for now.
        /// </summary>
        /// <param name="patchEntity"></param>
        protected virtual void ClearClassProperties(T patchEntity)
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

        private void InitializeEntity(object currentEntity, List<object> objects)
        {
            objects = objects ?? new List<object>();
            if (!objects.Contains(currentEntity))
            {
                objects.Add(currentEntity);
                var entityType = currentEntity.GetType();
                InitializeEntity(currentEntity);
                foreach (var property in entityType.GetProperties())
                {
                    var value = currentEntity.GetPropertyValue(property.Name);
                    if (value is IEnumerable && !(value is string))
                    {
                        var enumerable = value as IEnumerable;
                        foreach (var item in enumerable)
                        {
                            if (IsNewEntity(item))
                            {
                                InitializeEntity(item, objects);
                            }
                        }
                    }
                    if (IsNewEntity(value))
                    {
                        InitializeEntity(value, objects);
                    }
                }
            }
        }

        protected virtual void InitializeEntity(object currentEntity)
        {
            currentEntity.TryAs<ICreatedBy<TUser>>(e => { e.CreatedByUserId = GetCurrentUserId(); });
            currentEntity.TryAs<ICreatedDate>(e => { e.CreatedDate = DateTime.UtcNow; });
            currentEntity.TryAs<IHasGuid>(e => { e.Guid = Guid.NewGuid(); });
        }

        protected virtual bool IsNewEntity(object value)
        {
            if (value != null)
            {
                var type = value.GetType();
                if (type.IsClass && !(value is string))
                {
                    var childEntityConfiguration = Crud.Unsecured.Context.Model.FindEntityType(type);
                    if (childEntityConfiguration != null)
                    {
                        var entityKey = childEntityConfiguration.GetKeys()
                            .Single(k => k.IsPrimaryKey());
                        foreach (var key in entityKey.Properties)
                        {
                            if (value.GetPropertyValue(key.Name).IsDefaultValue())
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public virtual Task OnAfterPostAsync(T currentEntity, T patchEntity, JObject jObject)
        {
            return Task.FromResult(true);
        }

        public virtual Task OnBeforePatchAsync(KeyValuePair<string, object>[] id, T currentEntity, T patchEntity, JObject jObject)
        {
            return Task.FromResult(true);
        }

        public virtual Task OnAfterPatchAsync(KeyValuePair<string, object>[] id, T currentEntity, T patchEntity, JObject jObject)
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

        public virtual T PostedEntity { get; set; }
        public virtual string PostedJson { get; set; }

        public virtual Type EntityType => typeof(T);
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
