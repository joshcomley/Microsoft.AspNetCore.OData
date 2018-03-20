using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using Iql.DotNet;
using Iql.Queryable;
using Iql.Queryable.Data.DataStores.InMemory.QueryApplicator;
using Iql.Queryable.Data.EntityConfiguration;
using Microsoft.Data.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Validation;
using Microsoft.OData.Edm.Vocabularies;
using EdmPrimitiveTypeKind = Microsoft.OData.Edm.EdmPrimitiveTypeKind;
using IEdmModel = Microsoft.OData.Edm.IEdmModel;
using IEdmTerm = Microsoft.OData.Edm.Vocabularies.IEdmTerm;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    internal abstract class AnnotationManagerBase
    {
        public static readonly IEdmModel Instance;
        public static readonly IEdmTerm ValidationTerm;
        public static readonly IEdmTerm DisplayTextFormatterTerm;
        public static readonly IEdmTerm IqlConfigurationTerm;
        public static readonly IEdmTerm ValidationRegexTerm;
        public static readonly IEdmValueTerm PermissionsTerm;
        public static readonly IEdmTerm ValidationMaxLengthTerm;
        public static readonly IEdmTerm ValidationMinLengthTerm;
        public static readonly IEdmTerm ValidationRequiredTerm;

        static AnnotationManagerBase()
        {
            IqlQueryableAdapter.ExpressionConverter = () => new DotNetExpressionConverter();
            IEnumerable<EdmError> errors;
            var assembly = typeof(ODataExtensionsEdmModelExtensions).GetTypeInfo().Assembly;
            var assemblyName = assembly.GetName().Name;
            using (var stream = assembly.GetManifestResourceStream(
                $"{assemblyName}.Vocabularies.MeasuresVocabularies.xml"))
            {
                if (stream == null)
                {
                    assemblyName = assemblyName.Replace("Brandless", "Microsoft");
                    using (var stream2 = assembly.GetManifestResourceStream(
                        $"{assemblyName}.Vocabularies.MeasuresVocabularies.xml"))
                    {
                        CsdlReader.TryParse(XmlReader.Create(stream2), out Instance, out errors);
                    }
                }
                else
                {
                    CsdlReader.TryParse(XmlReader.Create(stream), out Instance, out errors);
                }
            }
            //ISOCurrencyTerm = Instance.FindDeclaredTerm(MeasuresISOCurrency);
            var configurationNs = "Iql";
            var validationNs = $"{configurationNs}.Validation";
            DisplayTextFormatterTerm = StringEdmTerm("DisplayTextFormatter", configurationNs);
            IqlConfigurationTerm = StringEdmTerm("Configuration", configurationNs);
            ValidationTerm = StringEdmTerm("Expression", validationNs);
            ValidationRegexTerm = StringEdmTerm("RegularExpression", validationNs);
            ValidationMaxLengthTerm = NumberEdmTerm("MaximumLength", validationNs);
            ValidationMinLengthTerm = NumberEdmTerm("MinimumLength", validationNs);
            ValidationRequiredTerm = BooleanEdmTerm("Required", validationNs);
            //ISOCurrencyTerm = Instance.FindDeclaredValueTerm(MeasuresISOCurrency);

        }

        private static IEdmTerm StringEdmTerm(string name, string @namespace = null)
        {
            return EdmTerm(name, EdmPrimitiveTypeKind.String, @namespace);
        }

        private static IEdmTerm NumberEdmTerm(string name, string @namespace = null)
        {
            return EdmTerm(name, EdmPrimitiveTypeKind.Int32, @namespace);
        }

        private static IEdmTerm BooleanEdmTerm(string name, string @namespace = null)
        {
            return EdmTerm(name, EdmPrimitiveTypeKind.Boolean, @namespace);
        }

        private static IEdmTerm EdmTerm(string name, EdmPrimitiveTypeKind type, string @namespace = null)
        {
            return new EdmTerm(@namespace/* ?? typeof(ApiDbContext).Namespace*/, name, type, AppliesTo.Property);
        }

        private static class AppliesTo
        {
            public const string Property = "Property";
        }

        public abstract void SetMetadataAnnotation(
            IMetadata metadata = null,
            string property = null);
    }
}