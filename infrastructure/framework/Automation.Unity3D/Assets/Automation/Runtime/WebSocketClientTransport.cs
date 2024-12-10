
using System;

using WebSocketSharp;


namespace Automation.Runtime
{

    public class WebSocketClientTransport : ITransport
    {

        //---------------------------------------------------------------------
        private readonly WebSocket mSocket;

        //---------------------------------------------------------------------
        public event Action<string> OnReceivedMessage;

        //---------------------------------------------------------------------
        public WebSocketClientTransport(string url)
        {
            mSocket = new WebSocket(url);
            mSocket.OnMessage += OnMessage;
        }

        //---------------------------------------------------------------------
        private void OnMessage(object sender, MessageEventArgs e)
        {
            OnReceivedMessage?.Invoke(e.Data);
        }

        //---------------------------------------------------------------------
        public void Start()
        {
            mSocket.Connect();
        }

        //---------------------------------------------------------------------
        public void SendMessage(string rawMessage)
        {
            mSocket.Send(rawMessage);
        }
    }

}

