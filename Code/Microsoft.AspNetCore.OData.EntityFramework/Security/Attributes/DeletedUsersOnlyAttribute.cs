namespace Microsoft.AspNetCore.OData.EntityFramework.Security.Attributes
{
    public class DeletedUsersOnlyAttribute : UserDeletedStatusFilterAttribute
    {
        public DeletedUsersOnlyAttribute() : base(DeletedStatus.Deleted)
        {
        }
    }
}
