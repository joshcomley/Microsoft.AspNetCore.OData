using System;

namespace Brandless.AspNetCore.OData.Extensions.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsPrimitiveType(this Type type)
        {
            if (type.IsPrimitive || type == typeof(decimal) || type == typeof(string))
            {
                return true;
            }

            var undderlyingType = Nullable.GetUnderlyingType(type);
            if (undderlyingType != null)
            {
                return undderlyingType.IsPrimitiveType();
            }
            return false;
        }
    }
}