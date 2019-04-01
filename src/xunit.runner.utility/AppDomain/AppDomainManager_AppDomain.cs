#if NETFRAMEWORK

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Web.Hosting;

namespace Xunit
{
    class AppDomainManager_AppDomain : IAppDomainManager
    {
        public AppDomainManager_AppDomain(string assemblyFileName, string configFileName, bool shadowCopy, string shadowCopyFolder)
        {
            Guard.ArgumentNotNullOrEmpty("assemblyFileName", assemblyFileName);

            assemblyFileName = Path.GetFullPath(assemblyFileName);
            Guard.FileExists("assemblyFileName", assemblyFileName);

            if (configFileName == null)
                configFileName = GetDefaultConfigFile(assemblyFileName);

            if (configFileName != null)
                configFileName = Path.GetFullPath(configFileName);

            AssemblyFileName = assemblyFileName;
            ConfigFileName = configFileName;
            AppDomain = CreateAppDomain(assemblyFileName, configFileName, shadowCopy, shadowCopyFolder);
        }

        public AppDomain AppDomain { get; private set; }

        public string AssemblyFileName { get; private set; }

        public string ConfigFileName { get; private set; }

        public bool HasAppDomain => true;

        static AppDomain CreateAppDomain(string assemblyFilename, string configFilename, bool shadowCopy, string shadowCopyFolder)
        {
            Trace.WriteLine(">>> STARTIN");

            var baseDir = Path.GetDirectoryName(assemblyFilename);
            Trace.WriteLine(">>> BaseDir=" + baseDir);

            var host = (AppDomainShim.AppDomainShim)ApplicationHost.CreateApplicationHost(typeof(AppDomainShim.AppDomainShim), "/app", baseDir);
            Trace.WriteLine(">>> HOSTED");

            if (true)
            {
                var dom = host.GetAppDomain();
                return dom;
            }
            else {
                var setup = new AppDomainSetup();
                setup.ApplicationBase = Path.GetDirectoryName(assemblyFilename);
                setup.ApplicationName = Guid.NewGuid().ToString();

                //setup.AppDomainInitializer = new AppDomainInitializer(InitAppDomain);

                //if (shadowCopy)
                //{
                //    setup.ShadowCopyFiles = "true";
                //    setup.ShadowCopyDirectories = setup.ApplicationBase;
                //    setup.CachePath = shadowCopyFolder ?? Path.Combine(Path.GetTempPath(), setup.ApplicationName);
                //}

                setup.ConfigurationFile = configFilename;

                return AppDomain.CreateDomain("WIBBBLYWOO" /*jPath.GetFileNameWithoutExtension(assemblyFilename)*/, System.AppDomain.CurrentDomain.Evidence, setup); //, new PermissionSet(PermissionState.Unrestricted));
            }
        }

        private static Assembly Dom_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Trace.WriteLine($">>> RESOLVIN {args.Name}");
            return Assembly.Load(args.Name);
        }

        public class AppDomainUnveiler : MarshalByRefObject
        {
            public AppDomain GetAppDomain()
            {
                Trace.WriteLine(">>> GETTING APPDOMAIN");
                return AppDomain.CurrentDomain;
            }
        }

        static void InitAppDomain(string[] args)
        {
            AppDomain.CurrentDomain.SetData(".appDomain", "*");
            AppDomain.CurrentDomain.SetData(".appVPath", "/appbase");
        }

        public TObject CreateObjectFrom<TObject>(string assemblyLocation, string typeName, params object[] args)
        {
            try
            {
#pragma warning disable CS0618
                var unwrappedObject = AppDomain.CreateInstanceFromAndUnwrap(assemblyLocation, typeName, false, 0, null, args, null, null, null);
#pragma warning restore CS0618
                return (TObject)unwrappedObject;
            }
            catch (TargetInvocationException ex)
            {
                ex.InnerException.RethrowWithNoStackTraceLoss();
                return default(TObject);
            }
        }

        public TObject CreateObject<TObject>(AssemblyName assemblyName, string typeName, params object[] args)
        {
            try
            {
#pragma warning disable CS0618
                var unwrappedObject = AppDomain.CreateInstanceAndUnwrap(assemblyName.FullName, typeName, false, 0, null, args, null, null, null);
#pragma warning restore CS0618
                return (TObject)unwrappedObject;
            }
            catch (TargetInvocationException ex)
            {
                ex.InnerException.RethrowWithNoStackTraceLoss();
                return default(TObject);
            }
        }

        public virtual void Dispose()
        {
            if (AppDomain != null)
            {
                string cachePath = AppDomain.SetupInformation.CachePath;

                try
                {
                    System.AppDomain.Unload(AppDomain);

                    if (cachePath != null)
                        Directory.Delete(cachePath, true);
                }
                catch { }
            }
        }

        static string GetDefaultConfigFile(string assemblyFile)
        {
            string configFilename = assemblyFile + ".config";

            if (File.Exists(configFilename))
                return configFilename;

            return null;
        }
    }
}

#endif
