using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.VisualBasic;
using ModuleInterface;
using Unity;
using Unity.Registration;

namespace UnityCachingMinimalExample
{
    public delegate void ExecuteFunction(out WeakReference weakRef);

    public static class Program
    {
        /// <summary>
        /// Creates a new <see cref="IUnityContainer"/> with registrations.
        /// </summary>
        /// <returns>The container with registrations.</returns>
        private static IUnityContainer GetPreparedContainer()
        {
            return new UnityContainer()
                .RegisterType<ILogger, Logger>()
                .RegisterType<BadException>();
        }

        /// <summary>
        /// Creates a function which resolves the <see cref="IModule"/> using the prepared container.
        /// </summary>
        /// <returns>The resolving function.</returns>
        private static Func<Type, IModule> CreatePreparedContainerResolver()
        {
            return t =>
            {
                using var container = GetPreparedContainer();
                return (IModule) container.Resolve(t);
            };
        }

        /// <summary>
        /// Creates a function which resolves the <see cref="IModule"/> using the prepared container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The resolving function.</returns>
        private static Func<Type, IModule> CreateReflectionContainerResolver(IUnityContainer container)
        {
            return t =>
            {
                var parameterInfos = t.GetConstructors().First().GetParameters();
                var parameters = new object[parameterInfos.Length];
                for (var i = 0; i < parameterInfos.Length; ++i)
                    parameters[i] = container.Resolve(parameterInfos[i].ParameterType);
                return (IModule) Activator.CreateInstance(t, parameters);
            };
        }
        
        /// <summary>
        /// Creates a function which resolves the <see cref="IModule"/> using the prepared container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The resolving function.</returns>
        private static Func<Type, IModule> CreateProperReflectionContainerResolver(IUnityContainer container)
        {
            var containerAlc = AssemblyLoadContext.GetLoadContext(container.GetType().Assembly);
            
            object CreateObject(Type t)
            {
                var alc = AssemblyLoadContext.GetLoadContext(t.Assembly);
                if (alc == containerAlc)
                    return container.Resolve(t);
                
                var parameterInfos = t.GetConstructors().First().GetParameters();
                var parameters = new object[parameterInfos.Length];
                for (var i = 0; i < parameterInfos.Length; ++i)
                    parameters[i] = CreateObject(parameterInfos[i].ParameterType);
                return Activator.CreateInstance(t, parameters);
            };

            return t => (IModule) CreateObject(t);
        }

        /// <summary>
        /// Creates a function which resolves the <see cref="IModule"/> using the passed container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The resolving function.</returns>
        private static Func<Type, IModule> CreateContainerResolver(IUnityContainer container)
        {
            return t => (IModule) container.Resolve(t);
        }

        /// <summary>
        /// The resolving module template.
        /// </summary>
        /// <param name="resolveMethod">The resolve method.</param>
        /// <param name="assemblyPath">The path to the assembly.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ExecuteFunction CreateExecuteAndUnload(Func<Type, IModule> resolveMethod, string assemblyPath)
        {
            void TheExecutionFunction(out WeakReference alcWeakRef)
            {
                var alc = new HostAssemblyLoadContext(assemblyPath);
                alcWeakRef = new WeakReference(alc, true);
                var moduleAssembly = alc.LoadFromAssemblyName(AssemblyName.GetAssemblyName(assemblyPath));
                var moduleType = moduleAssembly.GetType("Module.Module") ?? throw new NullReferenceException();

                // Resolve the module.
                var theModule = resolveMethod(moduleType);
                theModule.WriteInformation();

                // Unload the AssemblyLoadContext
                alc.Unload();
            }

            return TheExecutionFunction;
        }

        /// <summary>
        /// Starts the num-th attempt to load and unload.
        /// </summary>
        /// <param name="num">The number of the attempt.</param>
        /// <param name="execute">The execution function.</param>
        private static void Attempt(int num, ExecuteFunction execute)
        {
            execute(out var alcWeakRef);
            for (var i = 0; alcWeakRef.IsAlive && i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Console.WriteLine($"Attempt #{num} successful: {!alcWeakRef.IsAlive}");
        }

        public static void Main(string[] args)
        {
            // Locate the module.dll
            var binDir = Path.GetFullPath("../../../../Module/bin");
            var moduleDll = Directory.GetFiles(binDir, "Module.dll", SearchOption.AllDirectories).First();

            // Do the attempts
            var i = 0;
            {
                var execFunc = CreatePreparedContainerResolver();
                var attemptFunc = CreateExecuteAndUnload(execFunc, moduleDll);
                Attempt(++i, attemptFunc);
            }

            using (var container = GetPreparedContainer())
            {
                var execFunc = CreateReflectionContainerResolver(container);
                var attemptFunc = CreateExecuteAndUnload(execFunc, moduleDll);
                Attempt(++i, attemptFunc);
            }

            {
                var container = GetPreparedContainer();
                var execFunc = CreateProperReflectionContainerResolver(container);
                var attemptFunc = CreateExecuteAndUnload(execFunc, moduleDll);
                Attempt(++i, attemptFunc);
            }

            using (var container = GetPreparedContainer())
            {
                var execFunc = CreateContainerResolver(container);
                var attemptFunc = CreateExecuteAndUnload(execFunc, moduleDll);
                Attempt(++i, attemptFunc);
            }
        }
    }
}