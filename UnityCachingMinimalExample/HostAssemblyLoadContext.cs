using System;
using System.Reflection;
using System.Runtime.Loader;

namespace UnityCachingMinimalExample
{
    public class HostAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public HostAssemblyLoadContext(string pluginPath) : base("host_context", true)
        {
            Console.WriteLine($"Initializing Resolver at {pluginPath}");
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        // The Load method override causes all the dependencies present in the plugin's binary directory to get loaded
        // into the HostAssemblyLoadContext together with the plugin assembly itself.
        // NOTE: The Interface assembly must not be present in the plugin's binary directory, otherwise we would
        // end up with the assembly being loaded twice. Once in the default context and once in the HostAssemblyLoadContext.
        // The types present on the host and plugin side would then not match even though they would have the same names.
        protected override Assembly Load(AssemblyName name)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(name);
            if (assemblyPath != null)
            {
                Console.WriteLine($"Loading assembly {assemblyPath} into the HostAssemblyLoadContext");
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }
    }
}