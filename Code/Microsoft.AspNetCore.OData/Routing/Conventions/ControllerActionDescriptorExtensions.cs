// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Paging;

namespace Microsoft.AspNetCore.OData.Routing.Conventions
{
    /// <summary>
    /// Provides helper methods for querying an action map.
    /// </summary>
    public static class ControllerActionDescriptorExtensions
    {
        /// <summary>
        /// Find the matching action descriptor.
        /// </summary>
        /// <param name="controllerActionDescriptors">The list of action descriptor.</param>
        /// <param name="targetActionNames">The target action name.</param>
        /// <returns></returns>
        public static ControllerActionDescriptor FindMatchingAction(
            this IEnumerable<ControllerActionDescriptor> controllerActionDescriptors, params string[] targetActionNames)
        {
            return controllerActionDescriptors.FindMatchingActions(targetActionNames).FirstOrDefault();
        }

        /// <summary>
        /// Find matching action descriptors.
        /// </summary>
        /// <param name="controllerActionDescriptors">The list of action descriptor.</param>
        /// <param name="targetActionNames">The target action name.</param>
        /// <returns></returns>
        public static IEnumerable<ControllerActionDescriptor> FindMatchingActions(
            this IEnumerable<ControllerActionDescriptor> controllerActionDescriptors, params string[] targetActionNames)
        {
            return targetActionNames.SelectMany(
                targetActionName => controllerActionDescriptors.Where(
                    c => String.Equals(c.ActionName, targetActionName, StringComparison.OrdinalIgnoreCase)))
                .Where(controllerActionDescriptor => controllerActionDescriptor != null);
        }

        /// <summary>
        /// Returns the page size attribute
        /// </summary>
        /// <param name="actionDescriptor"></param>
        /// <returns></returns>
        public static ActionPageSize PageSize(this ActionDescriptor actionDescriptor)
        {
            var controllerActionDescriptor = actionDescriptor as ControllerActionDescriptor;
            var pageSizeAttribute = controllerActionDescriptor?.MethodInfo.GetCustomAttribute<PageSizeAttribute>();
            var actionPageSize = new ActionPageSize();
            if (pageSizeAttribute != null)
            {
                actionPageSize.IsSet = true;
                actionPageSize.Size = pageSizeAttribute.Value;
            }
            return actionPageSize;
        }
    }
}
