
using System;
using System.Collections.Generic;
using WebSocketSharp;

using Logger = Automation.Common.Logger;

namespace Automation.Runtime
{

    public class WebSocketClientTransport : ITransport
    {

        //---------------------------------------------------------------------
        private const string LogChannel = "transport.websocket";
        
        //---------------------------------------------------------------------
        private readonly Guid mPeerUuid;
        private readonly WebSocket mSocket;

        //---------------------------------------------------------------------
        public event Action<string> OnReceivedMessage;

        //---------------------------------------------------------------------
        public WebSocketClientTransport(string url, string accessKey = null)
        {
            mPeerUuid = Guid.NewGuid();
            
            mSocket = new WebSocket(url);
            mSocket.CustomHeaders = new Dictionary<string, string>
            {
                { "x-relay-accesskey", accessKey },
                { "x-relay-peeruuid", mPeerUuid.ToString() }
            };
            
            mSocket.OnOpen += OnOpen;
            mSocket.OnError += OnError;
            
            mSocket.OnMessage += OnMessage;
            
            Logger.Info(LogChannel, $"Constructed websocket url: {url}, accessKey: {accessKey}");
        }
        
        //---------------------------------------------------------------------
        private void OnOpen(object sender, EventArgs e)
        {
            Logger.Info(LogChannel, $"OnOpen");
        }
        
        //---------------------------------------------------------------------
        private void OnError(object sender, ErrorEventArgs e)
        {
            Logger.Error(LogChannel, $"OnError: {e}");
        }

        //---------------------------------------------------------------------
        private void OnMessage(object sender, MessageEventArgs e)
        {
            OnReceivedMessage?.Invoke(e.Data);
        }

        //---------------------------------------------------------------------
        public void Start()
        {
            Logger.Info(LogChannel, $"Connect");
            mSocket.Connect();
        }

        //---------------------------------------------------------------------
        public void SendMessage(string rawMessage)
        {
            mSocket.Send(rawMessage);
        }
    }

}

