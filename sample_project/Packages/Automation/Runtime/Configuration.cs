
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Automation.Runtime.Core;
using UnityEngine;

using YamlDotNet.Serialization;

using Newtonsoft.Json;

using Automation.Runtime.Utils.Extensions;
using UnityEngine.Networking;


namespace Automation.Runtime
{

    using Logger = Automation.Common.Logger;

    public struct ConfigurationSource
    {
        //-------------------------------------------------------------------------
        public string Path;
        public int Priority;
        
        //-------------------------------------------------------------------------
        public static readonly ConfigurationSource BaseConfigurationFile = new ConfigurationSource { Path = $"{Application.streamingAssetsPath}/base_config.yml", Priority = 0 };
        public static readonly ConfigurationSource SharedConfigurationFile = new ConfigurationSource { Path = $"shared_config.yml", Priority = 1 };
        public static readonly ConfigurationSource UserConfigurationFile = new ConfigurationSource { Path = $"user_config.yml", Priority = 2 };
        public static readonly ConfigurationSource CommandlineParameters = new ConfigurationSource { Path = "commandline", Priority = 3 };
        public static readonly ConfigurationSource DeeplinkParameters = new ConfigurationSource { Path = "deeplink", Priority = 4 };
        public static readonly ConfigurationSource RemoteConfigurationFile = new ConfigurationSource { Path = "", Priority = 5};
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class Configuration : 
        SingletonMonoBehaviour<Configuration>
    {

        //-------------------------------------------------------------------------
        private const string LogChannel = "io.configuration";
        
        //-------------------------------------------------------------------------
        private readonly Dictionary<string, ConfigurationEntry> mConfigurationEntries = new Dictionary<string, ConfigurationEntry> { };

        //-------------------------------------------------------------------------
        private async Task<bool> DownloadConfigurationAsync(string url, ConfigurationSource source)
        {
            Logger.Trace(LogChannel, $"Download Configuration: {url}");

            bool result = false;

            UnityWebRequest request = UnityWebRequest.Get(url);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.DataProcessingError:
                    Logger.Error(LogChannel, request.error);
                    return false;
                case UnityWebRequest.Result.Success:
                    break;
            }
            
            byte[] configurationData = request.downloadHandler.data;
            var jsonData = ExtractJsonConfigurationFromBytes(configurationData);
            LoadConfigurationEntries(jsonData, source);

            result = true;
            return result;
        }

        //-------------------------------------------------------------------------
        private void LoadConfigurationFile(string filePath, ConfigurationSource source)
        {
            string absoluteFilepath = Path.GetFullPath(filePath);
            string fileExtension = Path.GetExtension(filePath);

            if (FileWithJarSupport.Exists(filePath) == false)
            {
                Logger.WarnFormat(LogChannel, "file not found: {0} (source: {1})", filePath, source);
                return;
            }

            if (fileExtension.Equals(".yml") ||
                fileExtension.Equals(".yaml"))
            {
                Logger.InfoFormat(LogChannel, "loading config file as yaml: {0} (source: {1})", filePath, source);

                byte[] fileData = FileWithJarSupport.ReadAllBytes(filePath);
                IEnumerable<KeyValuePair<object, object>> yamlData = ExtractYamlConfigurationFromBytes(fileData);
                LoadConfigurationEntries(yamlData, source);
            }
            else if (fileExtension.Equals(".json"))
            {
                Logger.InfoFormat(LogChannel, "loading config file as json: {0} (source: {1})", filePath, source);

                byte[] fileData = FileWithJarSupport.ReadAllBytes(filePath);
                IEnumerable<KeyValuePair<string, object>> jsonData = ExtractJsonConfigurationFromBytes(fileData);
                LoadConfigurationEntries(jsonData, source);
            }
            else
            {
                Logger.WarnFormat(LogChannel, "unkonwn config file type: {0} (source: {1})", filePath, source);
            }
        }

        //-------------------------------------------------------------------------
        private void LoadConfigurationEntries<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> enumerable, ConfigurationSource source)
        {
            foreach (KeyValuePair<TKey, TValue> entry in enumerable)
            {
                string key = entry.Key.ToString();
                string jsonEncodedValue = JsonConvert.SerializeObject(entry.Value);

                if (mConfigurationEntries.TryGetValue(key, out ConfigurationEntry tmpEntry))
                {
                    if (tmpEntry.mSource.Priority > source.Priority)
                        continue;
                }

                ConfigurationEntry configurationEntry = new ConfigurationEntry
                {
                    mSource = source,
                    mCachedDecodedData = null,
                    mCachedDecodedType = null,
                    mJsonEncodedValue = jsonEncodedValue,
                    mAccessCount = 0,
                    mFailedCount = 0,
                    mOnChangeEvent = null,
                };

                mConfigurationEntries[key] = configurationEntry;
            }
        }
        
        //-------------------------------------------------------------------------
        private IEnumerable<KeyValuePair<object, object>> ExtractYamlConfigurationFromBytes(byte[] data)
        {
            IDeserializer yamlDeserializer = new DeserializerBuilder()
                .Build();
            
            using Stream inStream = new MemoryStream(data);
            using TextReader reader = new StreamReader(inStream);
            object ymlDocument = yamlDeserializer.Deserialize(reader);
            Dictionary<object, object> yamlData = (ymlDocument as Dictionary<object, object>);
            return yamlData;
        }
        
        //-------------------------------------------------------------------------
        private IEnumerable<KeyValuePair<string, object>> ExtractJsonConfigurationFromBytes(byte[] data)
        {
            using Stream inStream = new MemoryStream(data);
            using TextReader reader = new StreamReader(inStream);
            using JsonTextReader jsonReader = new JsonTextReader(reader);
            
            JsonSerializer jsonDeserializer = new JsonSerializer();
            Dictionary<string, object> jsonDocument = jsonDeserializer.Deserialize<Dictionary<string, object>>(jsonReader);
            return jsonDocument;
        }
        
        #region PUBLIC_RUNTIME_API

        //-------------------------------------------------------------------------
        public async Task StartAsync()
        {
            ConfigurationSource[] fileConfigurationSources = new[]
            {
                ConfigurationSource.BaseConfigurationFile,
                ConfigurationSource.SharedConfigurationFile,
                ConfigurationSource.UserConfigurationFile,
            };
            
            foreach (ConfigurationSource configurationSource in fileConfigurationSources)
                LoadConfigurationFile(configurationSource.Path, configurationSource);
                
            //  Load Deeplink
            if (string.IsNullOrEmpty(Application.absoluteURL) == false)
            {
                Logger.Trace(LogChannel, $"Processing deeplink: {Application.absoluteURL}");

                Uri uri = new Uri(Application.absoluteURL);
                IReadOnlyDictionary<string, string> parameters = uri.ParseQueryString();
                LoadConfigurationEntries(parameters, ConfigurationSource.DeeplinkParameters);
            }
            
            //  Load Remote 
            if (Get("remote_config", out string remoteConfig) == ConfigurationResult.Success)
                await DownloadConfigurationAsync(remoteConfig, ConfigurationSource.RemoteConfigurationFile);
        }

        //-------------------------------------------------------------------------
        public bool Has(string key)
        {
            return mConfigurationEntries.ContainsKey(key);
        }

        //-------------------------------------------------------------------------
        public ConfigurationResult Get<T>(string key, out T value, T defaultValue = default(T))
        {
            ConfigurationResult result = ConfigurationResult.Unknown;

            if (mConfigurationEntries.TryGetValue(key, out ConfigurationEntry entry))
            {
                entry.mAccessCount++;

                if (typeof(T) == entry.mCachedDecodedType)
                {
                    value = (T)entry.mCachedDecodedData;
                    result = ConfigurationResult.Success;
                }
                else
                {
                    try
                    {
                        value = JsonConvert.DeserializeObject<T>(entry.mJsonEncodedValue);

                        entry.mCachedDecodedData = value;
                        entry.mCachedDecodedType = typeof(T);
                        result = ConfigurationResult.Success;
                    }
                    catch (Exception exception)
                    {
                        entry.mFailedCount++;

                        value = defaultValue;
                        result = ConfigurationResult.DecodeError;

                        Logger.Error(LogChannel, $"DeserializeObject<{typeof(T).ToString()}>() failed for value: \n'{entry.mJsonEncodedValue}'");
                        Logger.Exception(LogChannel, exception);
                    }
                }
            }
            else
            {
                value = defaultValue;
                result = ConfigurationResult.NotFound;
            }

            return result;
        }

        //-------------------------------------------------------------------------
        public void AddListener(string lKey, Action<string> lOnChangeHandler)
        {
            ConfigurationEntry lConfigValue;
            if (mConfigurationEntries.TryGetValue(lKey, out lConfigValue) == true)
            {
                lConfigValue.mOnChangeEvent += lOnChangeHandler;
            }
        }

        //-------------------------------------------------------------------------
        public void RemoveListener(string lKey, Action<string> lOnChangeHandler)
        {
            ConfigurationEntry lConfigValue;
            if (mConfigurationEntries.TryGetValue(lKey, out lConfigValue) == true)
            {
                lConfigValue.mOnChangeEvent -= lOnChangeHandler;
            }
        }
        
        #endregion

    }


}