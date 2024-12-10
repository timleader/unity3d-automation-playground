
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

using WebSocketSharp;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Newtonsoft.Json;

using Automation.Common;
using Automation.Runtime;
using Automation.TestFramework;

using Logger = Automation.Common.Logger;

namespace Automation.Editor
{

    public class UnityEditorDriver :
        IUnityDriver,
        IDisposable
    {
        
        //-------------------------------------------------------------------------
        private const string LogChannel = "driver.editor";

        //-------------------------------------------------------------------------
        private struct RequestResponse
        {
            // Timeout in milliseconds

            public string mRequestId;
            public TaskCompletionSource<Response> mTaskCompletionSource;
        }

        //-------------------------------------------------------------------------
        private WebSocket mWebsocketConnection;

        //-------------------------------------------------------------------------
        private readonly List<RequestResponse> mRequestResponse = new List<RequestResponse>(8);
        private List<object> mSubscriptions;

        public ApplicationState ApplicationState => throw new NotImplementedException();

        //-------------------------------------------------------------------------
        public UnityEditorDriver()
        {
            IDeserializer yamlDeserializer = new DeserializerBuilder()
               .WithNamingConvention(CamelCaseNamingConvention.Instance)
               .Build();

            mWebsocketConnection = new WebSocket("ws://127.0.0.1:9999/ws");// configuration.WebsocketUrl);

            mWebsocketConnection.OnOpen += OnOpen;
            mWebsocketConnection.OnClose += OnClose;

            mWebsocketConnection.OnError += OnError;

            mWebsocketConnection.OnMessage += OnMessage;
        }

        #region WEBSOCKET_EVENTS

        //-------------------------------------------------------------------------
        private void OnOpen(object sender, EventArgs eventArgs)
        {
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

        #region INTERNAL_API

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

        #endregion

        #region PUBLIC_API

        //-------------------------------------------------------------------------
        public async Task LaunchAsync()
        {
            Logger.Info(LogChannel, "LaunchAsync");
            
            mWebsocketConnection.Connect();

            //  shouldn't be here 
            AsyncOperation loadSceneHandle = EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters { loadSceneMode = LoadSceneMode.Single });
            while (loadSceneHandle.isDone == false)
                await Task.Delay(100);

            await Task.Delay(10);
        }

        //-------------------------------------------------------------------------
        public async Task QuitAsync()
        {
            await Task.Delay(10);
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

        //-------------------------------------------------------------------------
        public async Task<QueryResult> QueryAsync(string query, TimeSpan waitTime)
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
        public async Task ExecuteAsync(string command)
        {
            Logger.InfoFormat(LogChannel, "ExecuteAsync, command={0}", command);

            Request request = new Request.Builder("execute")
                .AttachPayload(command)
                .Build();

            Response response = await SendRequest(request);

            return;
        }

        //-------------------------------------------------------------------------
        public Task InputTapAsync(int x, int y)
        {
            Logger.InfoFormat(LogChannel, "InputTapAsync, x={0}, y={1}", x, y);

            StandaloneExtendedInputModule inputModule = GameObject.FindObjectOfType<StandaloneExtendedInputModule>();
            inputModule.Tap(new Vector2Int(x, y));
            
            //  do click straight away- no need to network commsn, appium will just talk over appium
            
            return Task.CompletedTask;
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

        #endregion

        //-------------------------------------------------------------------------
        public void Dispose()
        {
            if (mWebsocketConnection != null)
            {
                mWebsocketConnection.Close();
                mWebsocketConnection = null;
            }
        }

    }

}