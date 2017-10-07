using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Common;

namespace Microsoft.AspNetCore.OData
{
    /// <summary>Represents an <see cref="T:System.Linq.IQueryable`1" /> containing zero or one entities. Use together with an [EnableQuery] from the System.Web.Http.OData or System.Web.OData namespace.</summary>
    /// <typeparam name="T">The type of the data in the data source.</typeparam>
    public sealed class SingleResult<T> : SingleResult
    {
        /// <summary>The <see cref="T:System.Linq.IQueryable`1" /> containing zero or one entities.</summary>
        public IQueryable<T> Queryable => base.Queryable as IQueryable<T>;

        /// <summary>Initializes a new instance of the <see cref="T:System.Web.Http.SingleResult`1" /> class.</summary>
        /// <param name="queryable">The <see cref="T:System.Linq.IQueryable`1" /> containing zero or one entities.</param>
        public SingleResult(IQueryable<T> queryable)
          : base((IQueryable)queryable)
        {
        }
    }

    /// <summary>Represents an <see cref="T:System.Linq.IQueryable" /> containing zero or one entities. Use together with an [EnableQuery] from the System.Web.Http.OData or System.Web.OData namespace.</summary>
    public abstract class SingleResult
    {
        /// <summary>The <see cref="T:System.Linq.IQueryable" /> containing zero or one entities.</summary>
        public IQueryable Queryable { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="T:System.Web.Http.SingleResult" /> class.</summary>
        /// <param name="queryable">The <see cref="T:System.Linq.IQueryable" /> containing zero or one entities.</param>
        protected SingleResult(IQueryable queryable)
        {
            if (queryable == null)
                throw Error.ArgumentNull("queryable");
            this.Queryable = queryable;
        }

        /// <summary>Creates a <see cref="T:System.Web.Http.SingleResult`1" /> from an <see cref="T:System.Linq.IQueryable`1" />. A helper method to instantiate a <see cref="T:System.Web.Http.SingleResult`1" /> object without having to explicitly specify the type <paramref name="T" />.</summary>
        /// <returns>The created <see cref="T:System.Web.Http.SingleResult`1" />.</returns>
        /// <param name="queryable">The <see cref="T:System.Linq.IQueryable`1" /> containing zero or one entities.</param>
        /// <typeparam name="T">The type of the data in the data source.</typeparam>
        public static SingleResult<T> Create<T>(IQueryable<T> queryable)
        {
            return new SingleResult<T>(queryable);
        }
    }
}