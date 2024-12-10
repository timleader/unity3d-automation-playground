
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Automation.TestFramework
{

    public class AppiumConfiguration
    {
        //---------------------------------------------------------------------
        [YamlMember(Alias = "scheme")]
        public string Scheme { get; set; }
        [YamlMember(Alias = "host")]
        public string Host { get; set; }
        [YamlMember(Alias = "port")]
        public int Port { get; set; }
        
        [YamlMember(Alias = "capabilities")]
        public Dictionary<string, string> Capabilities { get; set; }
    }

    public class ConnectionBridgeConfiguration
    {
        //---------------------------------------------------------------------
        [YamlMember(Alias = "scheme")]
        public string Scheme { get; set; }
        [YamlMember(Alias = "host")]
        public string Host { get; set; }
        [YamlMember(Alias = "port")]
        public int Port { get; set; }
    }
    
    public class DriverConfiguration
    {
        //---------------------------------------------------------------------
        [YamlMember(Alias = "appium")]
        public AppiumConfiguration Appium { get; set; }
        [YamlMember(Alias = "connection_bridge")]
        public ConnectionBridgeConfiguration ConnectionBridge { get; set; }
    }

}
