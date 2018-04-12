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

        public static bool ValueEquals(this JToken token, object value, Type valueType)
        {
            return (bool)ValueEqualsTypedMethod.MakeGenericMethod(valueType).Invoke(null, new object[] { token, value });
        }

        public static bool ValueEquals<TValue>(this JToken token, object value)
        {
            return Equals(value, token.Value<TValue>());
        }

        public static object GetValue(this JToken token, Type valueType)
        {
            return GetValueTypedMethod.MakeGenericMethod(valueType)
                .Invoke(null, new object[] { token });
        }

        public static TValue GetValue<TValue>(this JToken token)
        {
            return token.Value<TValue>();
        }
    }
}