
using Automation.Common;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Automation.Runtime
{

    public struct LogEvent
    {
        //---------------------------------------------------------------------
        [JsonProperty("level"), JsonConverter(typeof(StringEnumConverter))]
        public LogLevel Level { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
    }
    
    public class EventBusLogAdapter : ILogAdapter
    {
        //---------------------------------------------------------------------
        private const string EventBusSubjectFormat = "logs.{0}";
        
        //---------------------------------------------------------------------
        public void OnLogMessage(LogLevel level, string channel, string message)
        {
            string eventBusSubject = string.Format(EventBusSubjectFormat, channel);
            
            LogEvent logEvent = new LogEvent
            {
                Level = level,
                Message = message
            };
            
            EventBus.Instance.Publish(eventBusSubject, logEvent);
        }
    }
    
}