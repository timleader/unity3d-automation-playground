
using System;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Scripting;

using Logger = Automation.Common.Logger;
using Object = UnityEngine.Object;

[assembly: AlwaysLinkAssembly]

namespace Automation.Runtime
{

    public static class UnityRuntimeEntrypoint
    {
        //---------------------------------------------------------------------
        private static bool sInitialized = false;
        private static WebSocketClientTransport sSocketClientTransport = null;
        private static Agent sAgent = null;
        
        //---------------------------------------------------------------------
        private static readonly Type[] sRequiredComponents =
        {
            typeof(Configuration),
            typeof(EventBus)
        };
        
        //---------------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            UnityEngine.Debug.LogError("Unity runtime initialized");
            
            if (sInitialized == true)
                return;
            
            sInitialized = true;

            _ = InitializeAsync();
        }
        
        //---------------------------------------------------------------------
        private static async Task InitializeAsync()
        {
            Logger.RegisterAdapter(new UnityLogAdapter());
            
            Logger.Info("entrypoint", "Initializing...");
            
            for (int idx = 0; idx < sRequiredComponents.Length; ++idx)
            {
                Type requiredComponent = sRequiredComponents[idx];
                if (Object.FindObjectOfType(requiredComponent) == null)
                {
                    GameObject go = new GameObject(requiredComponent.Name, requiredComponent);
                    Object.DontDestroyOnLoad(go);
                    
                    Logger.Info("entrypoint", "Creating " + requiredComponent.Name);
                }
                else
                {
                    Logger.Info("entrypoint", $"Component {requiredComponent.Name} found, already exists.");
                }
            }
            
            Logger.RegisterAdapter(new EventBusLogAdapter());

            Logger.Info("entrypoint", "Configuration Starting...");
            await Configuration.Instance.StartAsync();

            if (Configuration.Instance.Get("automation_connection", out string connectionString) == ConfigurationResult.Success)
            { 
                Logger.Info("entrypoint", "Connection string found successfully: " + connectionString);
                
                Configuration.Instance.Get("automation_access_key", out string accessKey, "secret");
                
                sSocketClientTransport = new WebSocketClientTransport(connectionString, accessKey);
                sSocketClientTransport.Start();

                sAgent = new Agent(sSocketClientTransport);
            }
        }
    }

}
