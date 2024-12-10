
using System;

using UnityEngine;

using WebSocketSharp;
using WebSocketSharp.Server;


namespace Automation.Runtime
{
    
    public class WebSocketServerTransport : ITransport
    {

        //---------------------------------------------------------------------
        private readonly WebSocketServer mSocketServer;
        private WebSocketHandler mPrimaryHandler;

        //---------------------------------------------------------------------
        public event Action<string> OnReceivedMessage;
        
        //---------------------------------------------------------------------
        public WebSocketServerTransport()
        {
            mSocketServer = new WebSocketServer(9999, false);
        }
        
        //---------------------------------------------------------------------
        private void HandlerInitializer(WebSocketHandler handler)
        {
            if (mPrimaryHandler != null)
                Debug.Assert(mPrimaryHandler == handler);
            
            mPrimaryHandler = handler;
            mPrimaryHandler.OnReceivedMessage += OnReceivedMessage;
        }

        //---------------------------------------------------------------------
        public void Start()
        {
            mSocketServer.AddWebSocketService<WebSocketHandler>("/ws", HandlerInitializer);
            mSocketServer.Start();

            Debug.Log("Websocket server started on ws://localhost:8888/ws");
        }

        //---------------------------------------------------------------------
        public void SendMessage(string rawMessage)
        {
            if (mPrimaryHandler == null)
                return;
            
            mPrimaryHandler.SendMessage(rawMessage);
        }
    }

    
    public class WebSocketHandler : WebSocketBehavior    
    {
        //---------------------------------------------------------------------
        private const string LogChannel = "driver.target";
        
        //---------------------------------------------------------------------
        public event Action<string> OnReceivedMessage;
        
        //---------------------------------------------------------------------
        protected override void OnMessage(MessageEventArgs eventArgs)
        {
            OnReceivedMessage?.Invoke(eventArgs.Data);
        }

        //---------------------------------------------------------------------
        public void SendMessage(string rawMessage)
        {
            Send(rawMessage);
        }
    }
    
}

