using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Display;
using Brandless.AspNetCore.OData.Extensions.EntityConfiguration.Validation;

namespace Brandless.AspNetCore.OData.Extensions.EntityConfiguration
{
    public interface IEntityTypeConfiguration
    {
        IEntityValidationMap ValidationMap { get; set; }
        IEntityDisplayTextFormatterMap DisplayTextFormatterMap { get; set; }
    }
}