
using System;


namespace Automation.Runtime
{

    /// <summary>
    /// 
    /// </summary>
    public sealed class ConfigurationEntry
    {
        public ConfigurationSource mSource;  
        
        public string mJsonEncodedValue;

        public int mAccessCount;
        public int mFailedCount;

        public Type mCachedDecodedType;
        public object mCachedDecodedData;

        public Action<string> mOnChangeEvent;

    }


}