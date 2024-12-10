
using System;

namespace Automation.Runtime
{
    
    public interface ITransport
    {
        
        //---------------------------------------------------------------------
        event Action<string> OnReceivedMessage;
        
        //---------------------------------------------------------------------
        void SendMessage(string rawMessage);

    }
    
}