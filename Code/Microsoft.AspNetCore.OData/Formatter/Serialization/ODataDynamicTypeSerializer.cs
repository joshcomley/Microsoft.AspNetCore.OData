using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData;

namespace Microsoft.AspNetCore.OData.Formatter.Serialization
{
    public class ODataDynamicTypeSerializer : ODataSerializer
    {
        public ODataDynamicTypeSerializer() : base(ODataPayloadKind.Collection)
        {
        }

        public override Task WriteObjectAsync(object graph, Type type, ODataMessageWriter messageWriter, ODataSerializerContext writeContext)
        {
            var pageResult = graph as PageResult<object>;
            var results = pageResult.Items.ToList();
            foreach (var item in results)
            {   
                var groupByWrapper = item as GroupByWrapper;
                foreach (var value in groupByWrapper.Values)
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