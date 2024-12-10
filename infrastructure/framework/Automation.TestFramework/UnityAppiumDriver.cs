
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Automation.Common;

using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;

using WebSocketSharp;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Newtonsoft.Json;

using Logger = Automation.Common.Logger;


namespace Automation.TestFramework
{

    public class UnityAppiumDriver : 
        IUnityDriver
    {

        //-------------------------------------------------------------------------
        private const string LogChannel = "driver.appium";

        //-------------------------------------------------------------------------
        private struct RequestResponse
        {
            // Timeout in milliseconds

            public string mRequestId;
            public TaskCompletionSource<Response> mTaskCompletionSource;
        }

        //-------------------------------------------------------------------------
        private readonly DriverConfiguration mDriverConfiguration;
        
        private readonly AndroidDriver mAppiumDriver;
        private ConnectionBridgeApi mConnectionBridgeApi;
        private WebSocket mWebsocketConnection;

        //-------------------------------------------------------------------------
        private readonly List<RequestResponse> mRequestResponse = new List<RequestResponse>(8);
        private List<object> mSubscriptions;

        //-------------------------------------------------------------------------
        public ApplicationState ApplicationState => throw new NotImplementedException();

        //-------------------------------------------------------------------------
        public UnityAppiumDriver(DriverConfiguration configuration)
        {
            mDriverConfiguration = configuration;
            
            AppiumConfiguration appiumConfiguration = configuration.Appium;
            
            Uri appiumServerUri = new UriBuilder()
            {
                Scheme = appiumConfiguration.Scheme,
                Host = appiumConfiguration.Host,
                Port = appiumConfiguration.Port,
                Path = "/wd/hub",
            }.Uri;
            
            AppiumOptions appiumOptions = new AppiumOptions
            {
                AutomationName = AutomationName.AndroidUIAutomator2,
                PlatformName = "Android",
                App = appiumConfiguration.Capabilities["app"]
            };
            // https://appium.github.io/appium.io/docs/en/writing-running-appium/caps/#uiautomator1
            appiumOptions.AddAdditionalAppiumOption("autoLaunch", "false");
            
            appiumOptions.AddAdditionalAppiumOption("autoGrantPermissions", "true");
            
            appiumOptions.AddAdditionalAppiumOption("unlockType", "pin");
            appiumOptions.AddAdditionalAppiumOption("unlockKey", "111111");
            
            
            mAppiumDriver = new AndroidDriver(appiumServerUri, appiumOptions, TimeSpan.FromSeconds(300.0));
            //mAppiumDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            
            
            mConnectionBridgeApi = new ConnectionBridgeApi(mDriverConfiguration.ConnectionBridge);
            
            //  Wait for Activity
        }

        #region WEBSOCKET_EVENTS

        //-------------------------------------------------------------------------
        private void OnOpen(object sender, EventArgs eventArgs)
        {
            Logger.Info("Websocket", "Opening Unity Appium Driver");
        }

        //-------------------------------------------------------------------------
        private void OnClose(object sender, CloseEventArgs eventArgs)
        {
            //  attempt to reconnect ?? 
        }

        //-------------------------------------------------------------------------
        private void OnError(object sender, WebSocketSharp.ErrorEventArgs eventArgs)
        {
            //  report / log
            Logger.Error("Websocket", "Error: " + eventArgs.Message);
        }

        //-------------------------------------------------------------------------
        private void OnMessage(object sender, MessageEventArgs eventArgs)
        {
            Logger.TraceFormat(LogChannel, "received message={0}", eventArgs.Data);
            
            string rawMessage = eventArgs.Data;

            Message message = JsonConvert.DeserializeObject<Message>(rawMessage);

            switch (message.MessageType)
            {
                case MessageType.Response:
                    bool responseConsumed = false;
                    for (int idx = 0; idx < mRequestResponse.Count; ++idx)
                    {
                        if (mRequestResponse[idx].mRequestId == message.MessageId)
                        {
                            Response response = JsonConvert.DeserializeObject<Response>(rawMessage);

                            if (response.Status == StatusCode.Success)
                            {
                                mRequestResponse[idx].mTaskCompletionSource.TrySetResult(response);
                            }
                            else
                            {
                                mRequestResponse[idx].mTaskCompletionSource.TrySetException(new Exception(response.FailureReason));
                            }

                            mRequestResponse.RemoveAt(idx);
                            responseConsumed = true;
                            
                            Logger.TraceFormat(LogChannel, "request-response pair completed msg_id={0}", message.MessageId);
                            
                            break;
                        }
                    }

                    if (!responseConsumed)
                    {
                        Logger.TraceFormat(LogChannel, "request-response pair failed msg_id={0}", message.MessageId);
                    }
                    break;
                case MessageType.Publish:
                    
                    Publish publish = JsonConvert.DeserializeObject<Publish>(rawMessage);

                    Logger.TraceFormat(LogChannel, "publish event received subject={0}, payload={1}", publish.Subject, publish.Payload);
                    
                    //  pass to the active subscriptions
                    break;
                default:
                    //  unexpected
                    break;
            }

        }

        #endregion

        //-------------------------------------------------------------------------
        public async Task LaunchAsync()
        {
            Logger.Info(LogChannel, "LaunchAsync");

            Guid peerUuid = Guid.NewGuid();
            string accessKey = "secret";
            Guid bridgeId = Guid.NewGuid();
            _ = await mConnectionBridgeApi.CreateBridgeAsync(bridgeId, accessKey);
            
            Uri connectionUri = mConnectionBridgeApi.GetBridgeUrl(bridgeId);
            
            mWebsocketConnection = new WebSocket(connectionUri.ToString());// configuration.WebsocketUrl);
            mWebsocketConnection.CustomHeaders = new Dictionary<string, string>
            {
                { "x-relay-accesskey", accessKey },
                { "x-relay-peeruuid", peerUuid.ToString() }
            };
            
            mWebsocketConnection.OnOpen += OnOpen;
            mWebsocketConnection.OnClose += OnClose;

            mWebsocketConnection.OnError += OnError;

            mWebsocketConnection.OnMessage += OnMessage;
            
            mWebsocketConnection.Connect();
            
            string escapeUriString = Uri.EscapeUriString(connectionUri.ToString());
            string deepLink = $"trash://game?automation_connection={escapeUriString}&automation_access_key={accessKey}";
            
            Logger.Info(LogChannel, $"Launching Activity with deeplink : {deepLink}");
            
            mAppiumDriver.StartActivityWithIntent(
                "com.timleader.endless_runner", 
                "com.unity3d.player.UnityPlayerActivity",
                "android.intent.action.VIEW",
                intentOptionalArgs: deepLink);
            
            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        //-------------------------------------------------------------------------
        private Task<Response> SendRequest(Request requestMessage)
        {
            RequestResponse requestResponse = new RequestResponse
            {
                mRequestId = requestMessage.MessageId,
                mTaskCompletionSource = new TaskCompletionSource<Response>()
            };

            string serializedRequestData = JsonConvert.SerializeObject(requestMessage);

            mRequestResponse.Add(requestResponse);
            mWebsocketConnection.Send(serializedRequestData);

            return requestResponse.mTaskCompletionSource.Task;
        }
        
        //-------------------------------------------------------------------------
        private void Subscribe(Message message, Action<object> callback)     //   
        {
            mWebsocketConnection.Send(JsonConvert.SerializeObject(message));


        }
        
        //-------------------------------------------------------------------------
        public Task ExecuteAsync(string command)
        {
            throw new NotImplementedException();
        }

        //-------------------------------------------------------------------------
        public Task InputTapAsync(int x, int y)
        {
            throw new NotImplementedException();
        }

        //-------------------------------------------------------------------------
        public async Task<QueryResult> QueryAsync(string query, TimeSpan timeout)
        {
            Logger.InfoFormat(LogChannel, "QueryAsync, query={0}", query);
            
            Request request = new Request.Builder("query")
                .AttachPayload(query)
                .Build();

            Response response = await SendRequest(request);

            //  What should be the returned type?
            QueryResult proxyObjects = response.Payload.ToObject<QueryResult>();

            return proxyObjects;
        }

        //-------------------------------------------------------------------------
        public Task QuitAsync()
        {
            mAppiumDriver.Quit();
            
            return null;
        }

        //-------------------------------------------------------------------------
        public Task RealtimeModuleLoadAsync(string modulePath)
        {
            throw new NotImplementedException();
        }

        //-------------------------------------------------------------------------
        public Task RealtimeModuleStartAsync(string moduleIdentifier, string parameter)
        {
            throw new NotImplementedException();
        }

        //-------------------------------------------------------------------------
        public Task RealtimeModuleStopAsync()
        {
            throw new NotImplementedException();
        }

        //-------------------------------------------------------------------------
        public async Task SubscribeAsync(string subjectPattern, Action<object> callback)
        {
            Logger.InfoFormat(LogChannel, "SubscribeAsync, subject={0}", subjectPattern);

            Subscribe subscribe = new Subscribe.Builder(subjectPattern)
                .Build();
            
            Subscribe(subscribe, null);
            
            await Task.Delay(10);
        }
    }

}