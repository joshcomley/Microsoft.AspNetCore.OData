using NuGet.Updater.Core;

namespace Microsoft.AspNetCore.NuGet.Updater
{
    class Program
    {
        static void Main(string[] args)
        {
            NuGetUpdater.UpdateAllFromFirstParentSolutionFolder(new[] { "https://www.myget.org/F/brandless/api/v2" });
        }
    }
}
