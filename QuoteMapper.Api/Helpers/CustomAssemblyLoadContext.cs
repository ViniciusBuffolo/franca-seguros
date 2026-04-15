using System.Reflection;
using System.Runtime.Loader;

namespace QuoteMapper.Api.Helpers
{
    public class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        // Loads the unmanaged native DLL from an absolute path.
        public IntPtr LoadUnmanagedLibrary(string absolutePath)
        {
            return LoadUnmanagedDllFromPath(absolutePath);
        }

        // Not used for managed assemblies in this case.
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}