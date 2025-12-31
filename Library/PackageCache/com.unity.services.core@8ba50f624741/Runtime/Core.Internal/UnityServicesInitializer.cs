using UnityEngine;

namespace Unity.Services.Core.Internal
{
    static class UnityServicesInitializer
    {
        static bool s_CreatedServices;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void CreateStaticInstance()
        {
            if (s_CreatedServices)
            {
                // Reset flag for next domain reload
                s_CreatedServices = false;
                return;
            }

            UnityServices.ClearServices();
            UnityServicesBuilder.InstanceCreationDelegate = CreateInstance;

            var corePackageRegistry = new CorePackageRegistry();
            var coreRegistry = new CoreRegistry(corePackageRegistry.Registry);

            CorePackageRegistry.Instance = corePackageRegistry;
            CoreRegistry.Instance = coreRegistry;
            var coreMetrics = new CoreMetrics();
            var coreDiagnostics = new CoreDiagnostics();

            UnityServices.Instance = new UnityServicesInternal(coreRegistry, coreMetrics, coreDiagnostics);
            UnityServices.InstantiationCompletion?.TrySetResult(null);
            CoreMetrics.Instance = coreMetrics;
            CoreDiagnostics.Instance = coreDiagnostics;
        }

        #if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void AssemblyReloadSupport()
        {
            if (UnityServices.Instance == null)
            {
                CreateStaticInstance();
                s_CreatedServices = true;
            }
        }
        #endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static async void EnableServicesInitializationAsync()
        {
            var instance = (UnityServicesInternal)UnityServices.Instance;
            await instance.EnableInitializationAsync();
        }

        internal static IUnityServices CreateInstance(string servicesId)
        {
            var registry = new CoreRegistry(CorePackageRegistry.Instance.Registry, ServicesType.Instance, servicesId);
            var instance = new UnityServicesInternal(registry, CoreMetrics.Instance, CoreDiagnostics.Instance);
            instance.EnableInitialization();
            return instance;
        }
    }
}
