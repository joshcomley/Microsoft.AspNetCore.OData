using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.OData
{
    public class DefaultAssemblyProvider : IAssemblyProvider
    {
        private readonly Lazy<IEnumerable<Assembly>> _candidateAssemblies;

        public DefaultAssemblyProvider(IHostingEnvironment environment)
        {
            if (environment == null)
                throw new ArgumentNullException(nameof(environment));

            _candidateAssemblies = new Lazy<IEnumerable<Assembly>>(() => GetCandidateAssemblies(environment));
        }

        public IEnumerable<Assembly> CandidateAssemblies => _candidateAssemblies.Value;

        private static IEnumerable<Assembly> GetCandidateAssemblies(IHostingEnvironment environment)
        {
            var entryAssemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies().Select(Assembly.Load).ToList();
            var callingAssemblies = Assembly.GetCallingAssembly().GetReferencedAssemblies().Select(Assembly.Load).ToList();
            return entryAssemblies.Concat(callingAssemblies).Distinct();
        }
    }
}
