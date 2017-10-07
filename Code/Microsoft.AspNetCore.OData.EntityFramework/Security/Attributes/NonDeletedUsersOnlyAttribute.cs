namespace Microsoft.AspNetCore.OData.EntityFramework.Security.Attributes
{
    public class NonDeletedUsersOnlyAttribute : UserDeletedStatusFilterAttribute
    {
        public NonDeletedUsersOnlyAttribute() : base(DeletedStatus.NotDeleted)
        {
        }
    }
}
