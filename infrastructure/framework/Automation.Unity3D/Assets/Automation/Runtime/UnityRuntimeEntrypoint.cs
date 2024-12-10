
using System;
using System.Threading.Tasks;

using UnityEngine;

using Logger = Automation.Common.Logger;
using Object = UnityEngine.Object;

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
            if (sInitialized == true)
                return;
            
            sInitialized = true;

            _ = InitializeAsync();
        }
        
        //---------------------------------------------------------------------
        private static async Task InitializeAsync()
        {
            for (int idx = 0; idx < sRequiredComponents.Length; ++idx)
            {
                Type requiredComponent = sRequiredComponents[idx];
                if (Object.FindObjectOfType(requiredComponent) == null)
                {
                    GameObject go = new GameObject(requiredComponent.Name, requiredComponent);
                    Object.DontDestroyOnLoad(go);
                }
            }
            
            Logger.RegisterAdapter(new UnityLogAdapter());
            Logger.RegisterAdapter(new EventBusLogAdapter());

            await Configuration.Instance.StartAsync();

            if (Configuration.Instance.Get("automation_connection", out string connectionString) ==
                ConfigurationResult.Success)
            { 
                sSocketClientTransport = new WebSocketClientTransport(connectionString);
                sSocketClientTransport.Start();

                sAgent = new Agent(sSocketClientTransport);
            }
        }
    }

}
