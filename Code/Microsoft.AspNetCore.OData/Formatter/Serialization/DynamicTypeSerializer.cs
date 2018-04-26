using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData;

namespace Microsoft.AspNetCore.OData.Formatter.Serialization
{
    public class DynamicTypeSerializer : ODataSerializer
    {
        public DynamicTypeSerializer() : base(ODataPayloadKind.Collection)
        {
        }

        public override Task WriteObjectAsync(object graph, Type type, ODataMessageWriter messageWriter, ODataSerializerContext writeContext)
        {
            var pageResult = graph as PageResult<object>;
            var results = pageResult.Items.ToList();
            foreach (var item in results)
            {
                var g = item as NoGroupByAggregationWrapper;
                var x = g.Container.Name;
                var y = g.Container.Other;
                foreach (var value in g.Values)
                {
                    var oDataProperty = new ODataProperty
                    {
                        Name = value.Key,
                        Value = value.Value
                    };
                    messageWriter.WriteProperty(oDataProperty);
                }
            }
            return Task.FromResult<object>(null);
        }
    }
}