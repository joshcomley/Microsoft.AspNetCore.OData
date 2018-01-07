using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.OData.EntityFramework.Controllers
{
    public class KeyValuePairDetail
    {
        public KeyValuePair<string, object> KeyValuePair { get; }
        public Type ValueType { get; }

        public KeyValuePairDetail(KeyValuePair<string, object> keyValuePair, Type valueType)
        {
            KeyValuePair = keyValuePair;
            ValueType = valueType;
        }
    }
}