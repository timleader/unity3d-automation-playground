
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using UnityEngine;

using YamlDotNet.Serialization;

using Newtonsoft.Json;

using Automation.Common;


//  need `FileWithJarSupport` for reading files from .apk


namespace Automation.Runtime
{

    /// <summary>
    /// 
    /// </summary>
    public sealed class UnityLogAdapter : ILogAdapter
    {
        //---------------------------------------------------------------------
        public void OnLogMessage(LogLevel level, string channel, string message)
        {
            switch (level)
            {
                case LogLevel.Trace:
                case LogLevel.Info:
                    Debug.Log($"[{channel}] {message}");
                    break;
                case LogLevel.Warn:
                    Debug.LogWarning($"[{channel}] {message}");
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    Debug.LogError($"[{channel}] {message}");
                    break;
            }
        }
    }

}