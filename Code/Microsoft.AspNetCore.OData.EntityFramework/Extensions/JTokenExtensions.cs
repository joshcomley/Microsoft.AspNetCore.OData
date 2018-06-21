using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.OData.EntityFramework.Extensions
{
    public static class JTokenExtensions
    {
        static JTokenExtensions()
        {
            ValueEqualsTypedMethod = typeof(JTokenExtensions).GetMethods()
                .Single(m => m.Name == nameof(ValueEquals) && m.GetGenericArguments().Count() == 1);
            GetValueTypedMethod = typeof(JTokenExtensions).GetMethods()
                .Single(m => m.Name == nameof(GetValue) && m.GetGenericArguments().Count() == 1);
        }


        public static MethodInfo GetValueTypedMethod { get; set; }

        public static MethodInfo ValueEqualsTypedMethod { get; set; }

        public static bool IsNull(this JToken token)
        {
            return token.Type == JTokenType.Null;
        }

        public static bool ValueEquals(this JToken token, object value, Type valueType)
        {
            var underlyingType = Nullable.GetUnderlyingType(valueType);
            if (valueType == typeof(Guid) || underlyingType == typeof(Guid))
            {
                var guidValue = token.Value<string>();
                if (!string.IsNullOrWhiteSpace(guidValue) && !Equals(null, value))
                {
                    return new Guid(guidValue) == (Guid)value;
                }
            }
            return (bool)ValueEqualsTypedMethod.MakeGenericMethod(valueType).Invoke(null, new object[] { token, value });
        }

        public static bool ValueEquals<TValue>(this JToken token, object value)
        {
            return Equals(value, token.Value<TValue>());
        }

        public static object GetValue(this JToken token, Type valueType)
        {
            var underlyingType = Nullable.GetUnderlyingType(valueType);
            if (valueType == typeof(Guid) || underlyingType == typeof(Guid))
            {
                var guidValue = token.Value<string>();
                if (!string.IsNullOrWhiteSpace(guidValue))
                {
                    return new Guid(guidValue);
                }

                return null;
            }
            return GetValueTypedMethod.MakeGenericMethod(valueType)
                .Invoke(null, new object[] { token });
        }

        public static TValue GetValue<TValue>(this JToken token)
        {
            return token.Value<TValue>();
        }
    }
}