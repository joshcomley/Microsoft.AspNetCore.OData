using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Brandless.AspNetCore.OData.Extensions;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration;
using Brandless.AspNetCore.OData.Extensions.Extensions;
using Brandless.Data;
using Brandless.Data.Contracts;
using Brandless.Data.Entities;
using Brandless.Data.EntityFramework.Crud;
using Brandless.Data.Models;
using Brandless.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.EntityFramework.Export;
using Microsoft.AspNetCore.OData.EntityFramework.Export.Excel;
using Microsoft.AspNetCore.OData.EntityFramework.Extensions;
using Microsoft.AspNetCore.OData.Extensions;
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
        public T FindEntityById(object id)
        {
            return Crud.Secured.Find(id);
        }
        #endregion Security

        #region GET
        // GET: api/Products
        [HttpGet]
        public virtual async Task<IActionResult> Get()
        {
            if (Request.IsExportRequest(out var kind, out var kindName))
            {
                if (CanHandleExportRequest(kind, kindName))
                {
                    return await ExportAsync(kind, kindName);
                }
            }
            IActionResult result = Ok(await GetEntitySetQuery());
            return result;
        }

        public virtual async Task<IActionResult> ExportAsync(ExportKind kind, string kindName)
        {
            var modelConfiguration = Request.HttpContext.RequestServices.GetService<IEdmModelAccessor>().EdmModel.ModelConfiguration();
            var queryable = await GetEntitySetQuery();
            return await ExportAsync(kind, kindName, queryable, modelConfiguration);
        }

        public virtual async Task<IActionResult> ExportAsync(ExportKind kind, string kindName, IQueryable<T> queryable,
            ModelConfiguration modelConfiguration)
        {
            switch (kind)
            {
                case ExportKind.Excel:
                    return ExcelFile(await GetExcelReportAsync(queryable, modelConfiguration));
            }
            return null;
        }

        public virtual async Task<byte[]> GetExcelReportAsync(IQueryable<T> queryable, ModelConfiguration modelConfiguration)
        {
            return await new QueryToExcel<T>().GetReportAsync(
                Request,
                queryable,
                modelConfiguration);
        }

        protected IActionResult ExcelFile(byte[] report)
        {
            return File(report, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        public virtual bool CanHandleExportRequest(ExportKind kind, string kindName)
        {
            switch (kind)
            {
                case ExportKind.Excel:
                    return true;
            }

            return false;
        }

        public virtual Task<IQueryable<T>> GetEntitySetQuery()
        {
            return Task.FromResult(Crud.Secured.All());
        }

        [HttpGet]
        public virtual async Task<IActionResult> Get([ModelBinder(typeof(KeyValueBinder))]KeyValuePair<string, object>[] keys)
        {
            IActionResult result;
            var entityQuery = await GetEntityQuery(keys);
            if (entityQuery == null || entityQuery.Count() != 1)
            {
                result = NotFound();
            }
            else
            {
                result = Ok(new SingleResult<T>(entityQuery));
            }
            return result;
        }

        public virtual Task<IQueryable<T>> GetEntityQuery(KeyValuePair<string, object>[] keys)
        {
            return Task.FromResult(Crud.Secured.FindQuery(keys.Cast<object>().ToArray()));
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
                ModelState.Clear();
                await PatchObjectWithLegalPropertiesAsync(entity, patchEntity, value);
                if (!ModelState.IsValid)
                {
                    return this.ODataModelStateError();
                }

                ModelState.Clear();
                if (!await ValidateEntityAsync(entity, isNew: true))
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
                    _validateEntityMethod = GetType().GetMethod(nameof(ValidateEntityAsync));
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

        protected virtual void AddModelError<TEntity>(string path, string message, string errorCode)
        {
            var exception = new Exception(message);
            exception.Source = errorCode;
            ModelState.AddModelError(path, exception, MetadataProvider.GetMetadataForType(typeof(TEntity)));
        }

        public virtual async Task<bool> ValidateEntityAsync<TEntity>(TEntity entity, Dictionary<object, bool> validated = null, string path = "", bool isNew = false)
        {
            validated = validated ?? new Dictionary<object, bool>();
            if (validated.ContainsKey(entity))
            {
                return validated[entity];
            }
            var modelConfiguration = ModelConfiguration.ForType<TEntity>();
            var accessor = string.IsNullOrWhiteSpace(path) ? "" : ".";
            var isValid = !string.IsNullOrWhiteSpace(path) || TryValidateModel(entity);
            validated.Add(entity, isValid);
            if (isNew && Crud.Unsecured.Find(entity) != null)
            {
                isValid = false;
                AddModelError<TEntity>("", "An entity with this key already exists", "EntityWithKeyAlreadyExists");
            }
            if (modelConfiguration != null)
            {
                if (modelConfiguration.ValidationMap?.EntityValidations?.Any() == true)
                {
                    foreach (var entityValidation in modelConfiguration.ValidationMap.EntityValidations)
                    {
                        var iqlValidationResult = entityValidation.Run(entity);
                        isValid = isValid && iqlValidationResult;
                        if (!iqlValidationResult)
                        {
                            AddModelError<TEntity>(path, entityValidation.Message, entityValidation.Key);
                        }
                    }
                }
                if (modelConfiguration.ValidationMap?.PropertyValidations?.Any() == true)
                {
                    foreach (var propertyValidationCollection in modelConfiguration.ValidationMap.PropertyValidations)
                    {
                        foreach (var propertyValidation in propertyValidationCollection.Rules)
                        {
                            var iqlValidationResult = propertyValidation.Run(entity);
                            isValid = isValid && iqlValidationResult;
                            if (!iqlValidationResult)
                            {
                                AddModelError<TEntity>($"{path}{accessor}{propertyValidationCollection.PropertyName}", propertyValidation.Message, propertyValidation.Key);
                            }
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
                var relatedEntityType = Crud.Unsecured.Context.Model.FindEntityType(elementType.Name);
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
            return await Patch(keys, ResolveJObject());
        }

        protected virtual async Task<IActionResult> Patch(KeyValuePair<string, object>[] keys, JObject value)
        {
            keys = await CheckKeyChangeAsync(keys, value);
            var entity = Crud.Secured.Find(keys.Cast<object>().ToArray());
            if (entity == null)
            {
                return NotFound();
            }
            return await Patch(keys, ResolveJObject(), entity);
        }

        protected virtual async Task<KeyValuePair<string, object>[]> CheckKeyChangeAsync(KeyValuePair<string, object>[] keys, JObject value)
        {
            if (!value.HasValues)
            {
                return keys;
            }
            List<KeyValuePair<PropertyInfo, object>> newKeyValues = null;
            foreach (var pairing in keys)
            {
                if (!value[pairing.Key].ValueEquals(pairing.Value, pairing.Value.GetType()))
                {
                    newKeyValues = newKeyValues ?? new List<KeyValuePair<PropertyInfo, object>>();
                    var property = typeof(T).GetProperty(pairing.Key);
                    newKeyValues.Add(new KeyValuePair<PropertyInfo, object>(property,
                        value[property.Name].GetValue(property.PropertyType)));
                }
            }

            if (newKeyValues != null)
            {
                var oldKeyValues = new List<KeyValuePair<PropertyInfo, object>>();
                foreach (var keyProperty in keys)
                {
                    var property = typeof(T).GetProperty(keyProperty.Key);
                    oldKeyValues.Add(new KeyValuePair<PropertyInfo, object>(property, keyProperty.Value));
                }
                await PatchKeyAsync(oldKeyValues, newKeyValues);

                var newKeys = new Dictionary<string, KeyValuePair<string, object>>();
                foreach (var keyProperty in newKeyValues)
                {
                    newKeys.Add(keyProperty.Key.Name, new KeyValuePair<string, object>(keyProperty.Key.Name, keyProperty.Value));
                }

                foreach (var keyProperty in oldKeyValues)
                {
                    if (!newKeys.ContainsKey(keyProperty.Key.Name))
                    {
                        newKeys.Add(keyProperty.Key.Name, new KeyValuePair<string, object>(keyProperty.Key.Name, keyProperty.Value));
                    }
                }
                return newKeys.Values.ToArray();
            }

            return keys;
        }

        public virtual async Task PatchKeyAsync(List<KeyValuePair<PropertyInfo, object>> oldKeyValues, List<KeyValuePair<PropertyInfo, object>> newKeyValues)
        {
            var sqlParameters = new List<SqlParameter>();
            var setters = new List<string>();
            string tableName = GetSqlTableName();
            for (var i = 0; i < newKeyValues.Count; i++)
            {
                var property = newKeyValues[i].Key;
                var newValueParameterName = $"@NewValue{property.Name}";
                setters.Add($"[{property.Name}] = {newValueParameterName}");
                sqlParameters.Add(new SqlParameter(newValueParameterName, newKeyValues[i].Value));
                //await Crud.Secured.DeleteAndSaveAsync(oldEntity);
            }

            var filters = new List<string>();
            foreach (var keyProperty in oldKeyValues)
            {
                var property = keyProperty.Key;
                var parameterName = $"@OldValue{property.Name}";
                filters.Add($"[{property.Name}] = {parameterName}");
                sqlParameters.Add(new SqlParameter(parameterName, keyProperty.Value));
            }

            await Crud.Secured.Context.Database.ExecuteSqlCommandAsync(
                $"Update [{tableName}] SET {string.Join(", ", setters)} WHERE {string.Join(" AND ", filters)}",
                sqlParameters);
        }

        public virtual string GetSqlTableName()
        {
            return Crud.Secured.Context.Model.FindEntityType(typeof(T)).SqlServer().TableName;
        }

        protected virtual JObject ResolveJObject()
        {
            return PostedJson == null
                ? new JObject()
                : JObject.Parse(PostedJson);
        }

        protected virtual async Task<IActionResult> Patch(KeyValuePair<string, object>[] keys, JObject value, T currentDatabaseEntity)
        {
            await OnValidate(PostedEntity, value);
            return await Patch(keys, value, currentDatabaseEntity, PostedEntity);
        }

        public virtual async Task<IActionResult> Patch(KeyValuePair<string, object>[] keys, JObject value, T currentDatabaseEntity, T patchEntity)
        {
            await OnBeforePostAndPatchAsync(currentDatabaseEntity, patchEntity, value);
            await OnBeforePatchAsync(keys, currentDatabaseEntity, patchEntity, value);
            ModelState.Clear();
            await PatchObjectWithLegalPropertiesAsync(currentDatabaseEntity, patchEntity, value);
            if (!ModelState.IsValid)
            {
                return this.ODataModelStateError();
            }
            ModelState.Clear();
            if (!await ValidateEntityAsync(currentDatabaseEntity))
            {
                return this.ODataModelStateError();
            }

            for (var i = 0; i < keys.Length; i++)
            {
                if (!Equals(keys[i].Value, value[keys[i].Key]))
                {
                    // We have a key change
                }
            }
            var oDataActionResult = await UpdateAsync(currentDatabaseEntity);
            var result = ResolveHttpResult(oDataActionResult);
            await OnAfterPatchAsync(keys, currentDatabaseEntity, patchEntity, value);
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
            await PatchEntityPropertiesAsync(currentEntity, patchEntity, value, typeof(T));
            await OnAfterFilterLegalPropertiesAsync(currentEntity, patchEntity, value);
        }

        protected virtual async Task PatchEntityPropertiesAsync(object currentEntity, object patchEntity, JObject value, Type entityType, string path = "")
        {
            if (PatchEntityPropertiesAsyncMethod == null)
            {
                PatchEntityPropertiesAsyncMethod = GetType().GetRuntimeMethods().SingleOrDefault(m => m.Name == nameof(PatchEntityPropertiesAsync) && m.GetGenericArguments().Length == 1);
            }
            await (Task)PatchEntityPropertiesAsyncMethod.MakeGenericMethod(entityType)
                .Invoke(this, new object[] { currentEntity, patchEntity, value, path });
        }

        public MethodInfo PatchEntityPropertiesAsyncMethod { get; set; }

        protected virtual async Task PatchEntityPropertiesAsync<TEntity>(object currentEntity, object patchEntity,
            JObject value, string path)
        {
            var originalPath = path;
            foreach (var property in value)
            {
                if (!ShouldPatchEntityProperty(currentEntity, patchEntity, property.Key))
                {
                    continue;
                }

                var entityType = typeof(TEntity);
                // If we don't allow get on this property in OData, don't allow set
                if (!ModelAccessor.EdmModel.HasProperty(entityType, property.Key))
                {
                    continue;
                }
                var propertyInfo = entityType.GetProperty(property.Key);
                path = $"{originalPath}.{propertyInfo.Name}".TrimStart('.');
                // Set the value to the value of the same property on the patch entity
                var propertyType = propertyInfo.PropertyType;
                var canBeNull = !propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) != null;
                if (!canBeNull && value[propertyInfo.Name].IsNull())
                {
                    AddModelError<TEntity>(path, "Value cannot be empty", "DisallowedNull");
                }
                if (patchEntity != null)
                {
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
                        var entityKey = Crud.Unsecured.Context.Model.FindEntityType(childEntityType.FullName).GetKeys()
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
                                await PatchEntityPropertiesAsync(child, submittedList[index], property.Value[index] as JObject, childEntityType, $"{path}.{propertyInfo.Name}[{index}]".TrimStart('.'));
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

        protected virtual async Task<DeleteActionResult> DeleteEntity(KeyValuePair<string, object>[] key)
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
        object IODataCrudController.FindEntityById(object id)
        {
            return FindEntityById(id);
        }

        protected virtual string GetCurrentUserId()
        {
            return UserManager.GetUserId(HttpContext.User);
        }
    }
}
