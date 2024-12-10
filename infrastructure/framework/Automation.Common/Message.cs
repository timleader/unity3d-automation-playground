

using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Automation.Common
{

    public enum MessageType
    {
        Subscribe,
        Publish,

        Request,
        Response,
    }

    public enum StatusCode
    {
        Success         = 0,
        UnknownError    = -1,
    }

    public class Message        //  this is your envelope 
    {
        //---------------------------------------------------------------------
        [JsonProperty("msg_id")]
        public string MessageId { get; protected set; }
        [JsonProperty("msg_type"), JsonConverter(typeof(StringEnumConverter))]
        public MessageType MessageType { get; protected set; }
    }

    public class Request : Message
    {
        //---------------------------------------------------------------------
        [JsonProperty("function")]
        public string Function { get; private set; }

        //---------------------------------------------------------------------
        [JsonProperty("payload")]
        public JToken Payload { get; private set; }

        //--------------------------------------------------------------------
        public class Builder
        {
            private string mFunction;
            private JToken mPayload;

            public Builder(string function)
            {
                mFunction = function;
            }

            public Builder AttachPayload(object payload)
            {
                mPayload = JToken.FromObject(payload);
                return this;
            }

            public Request Build()
            {
                return new Request 
                {
                    MessageId   = Guid.NewGuid().ToString(),
                    MessageType = MessageType.Request,
                    
                    Function    = mFunction,
                    Payload     = mPayload
                };
            }
        }

    }

    public class Response : Message
    {
        //---------------------------------------------------------------------
        [JsonProperty("status")]
        public StatusCode Status { get; private set; }

        [JsonProperty("failure_reason")]
        public string FailureReason { get; private set; }

        [JsonProperty("payload")]
        public JToken Payload { get; private set; }


        /**
         *  Could return: 
         *      GameObject 
         *          -   Uuid
         *          -   Name
         *          -   Active 
         *      Component (MonoBehaviour)
         *          use [AutomationSerializable]
         *      Custom Serializers
         *          Text Component. etc.
         *          
         *          
         *      [AutomationMethod]
         */

        //--------------------------------------------------------------------
        public class Builder
        {
            private Request mRequest;

            private JToken mPayload;

            public Builder(Request request)
            {
                mRequest = request;
            }

            public Builder AttachPayload(object payload)
            {
                mPayload = JToken.FromObject(payload);
                return this;
            }

            public Response Build()
            {
                return new Response
                {
                    MessageId = mRequest.MessageId,
                    MessageType = MessageType.Response,
                    
                    Payload = mPayload
                };
            }
        }

    }

    public class Subscribe : Message
    {
        //---------------------------------------------------------------------
        [JsonProperty("subject")]
        public string SubjectPattern { get; private set; }
        
        //--------------------------------------------------------------------
        public class Builder
        {
            private string mSubjectPattern;

            public Builder(string subjectPattern)
            {
                mSubjectPattern = subjectPattern;
            }

            public Subscribe Build()
            {
                return new Subscribe 
                {
                    MessageId   = Guid.NewGuid().ToString(),
                    MessageType = MessageType.Subscribe,
                    
                    SubjectPattern    = mSubjectPattern,
                };
            }
        }
    }

    public class Publish : Message
    {
        //---------------------------------------------------------------------
        [JsonProperty("subject")]
        public string Subject { get; private set; }
        [JsonProperty("payload")]
        public JToken Payload { get; private set; }
        
        //--------------------------------------------------------------------
        public class Builder
        {
            private string mSubject;

            private JToken mPayload;

            public Builder(string subject)
            {
                mSubject = subject;
            }

            public Builder AttachPayload(object payload)
            {
                mPayload = JToken.FromObject(payload);
                return this;
            }

            public Publish Build()
            {
                return new Publish 
                {
                    MessageId   = Guid.NewGuid().ToString(),
                    MessageType = MessageType.Publish,
                    
                    Subject    = mSubject,
                    Payload     = mPayload
                };
            }
        }
    }

}