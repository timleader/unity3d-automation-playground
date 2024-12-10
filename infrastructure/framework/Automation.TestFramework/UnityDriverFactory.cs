
using System;
using System.Collections.Generic;

using Automation.Common;

namespace Automation.TestFramework
{

    public static class UnityDriverFactory
    {

        //---------------------------------------------------------------------
        private const string LogChannel = "UnityDriverFactory";
        
        //---------------------------------------------------------------------
        public delegate IUnityDriver DriverCreateFunc(DriverConfiguration configuration);

        //---------------------------------------------------------------------
        private static Dictionary<string, DriverCreateFunc> sUnityDriverCreationFuncs = new Dictionary<string, DriverCreateFunc> 
        {
            { "appium", (configuration) => new UnityAppiumDriver(configuration) }
        };

        //---------------------------------------------------------------------
        public static void RegisterUnityDriver(string @type, DriverCreateFunc creationFunc)
        {
            if (sUnityDriverCreationFuncs.ContainsKey(@type))
                throw new InvalidOperationException();

            sUnityDriverCreationFuncs.Add(@type, creationFunc);

            Logger.Trace(LogChannel, $"RegisterUnityDriver, type={@type}");
        }

        //---------------------------------------------------------------------
        public static IUnityDriver NewUnityDriver(string @type, DriverConfiguration configuration = null)
        {
            bool typeFound = sUnityDriverCreationFuncs.TryGetValue(@type, out DriverCreateFunc creationFunc);
            if (typeFound == false)
                throw new NotImplementedException();

            IUnityDriver unityDriver = creationFunc.Invoke(configuration);

            Logger.Trace(LogChannel, $"NewUnityDriver, type={@type}");

            return unityDriver;
        }

    }

}
