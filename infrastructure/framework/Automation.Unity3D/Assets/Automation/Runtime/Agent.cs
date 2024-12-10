
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

using Automation.Common;
using Automation.Runtime.QueryLanguage;

using Logger = Automation.Common.Logger;
using Attribute = Automation.Common.Attribute;

namespace Automation.Runtime
{
    public class Agent
    {
        //---------------------------------------------------------------------
        private const string LogChannel = "agent";

        //---------------------------------------------------------------------
        private readonly ITransport mTransport;

        //---------------------------------------------------------------------
        public Agent(ITransport transport)
        {
            mTransport = transport;
            
            mTransport.OnReceivedMessage += OnReceivedMessage; 
        }
        
        //---------------------------------------------------------------------
        private void OnReceivedMessage(string rawMessage)
        {
            Logger.TraceFormat(LogChannel, "received message={0}", rawMessage);

            Message message = JsonConvert.DeserializeObject<Message>(rawMessage);

            switch (message.MessageType)
            {
                case MessageType.Request:
                    
                    Request request = JsonConvert.DeserializeObject<Request>(rawMessage);
                    if (request.Function == "query")
                    {
                        UnityMainThreadTaskFactory.Run(async () => await WrapRequestHandler(ExampleRequest2, request));
                    }
                    //  pass through to command handlers 

                    break;
                case MessageType.Subscribe:

                    Subscribe subscribe = JsonConvert.DeserializeObject<Subscribe>(rawMessage);
                    
                    Logger.TraceFormat(LogChannel, "Subscribe, subject={0}", subscribe.SubjectPattern);

                    EventBus eventBus = EventBus.Instance;
                    eventBus.Subscribe(subscribe.SubjectPattern, (subject, @event) =>
                    {
                        Publish publish = new Publish.Builder(subject)
                            .AttachPayload(@event)
                            .Build();
                        
                        string publishString = JsonConvert.SerializeObject(publish);

                        mTransport.SendMessage(publishString);
                    });

                    break;
                default:
                    //  unexpected
                    break;
            }
        }
        
        
        //---------------------------------------------------------------------
        private async Task WrapRequestHandler(Func<Request, CancellationToken, Task<Response>> onRequest, Request request)
        {
            //  handle timeout's here 

            //  catch exceptions, and respond to requester 
            CancellationTokenSource timeoutCancellationTokenSource = new CancellationTokenSource();

            try
            {
                Task<Response> requestTask = onRequest(request, timeoutCancellationTokenSource.Token);

                await requestTask;

                string responseString = JsonConvert.SerializeObject(requestTask.Result);

                mTransport.SendMessage(responseString);
                
                Logger.TraceFormat(LogChannel, "sent_message, response={0}", responseString);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
        
        //---------------------------------------------------------------------
        private static QueryResult ExecuteQuery(string queryPath)
        {
            Logger.TraceFormat(LogChannel, "ExecuteQuery, query={0}", queryPath);
            
            RoutingNode[] nodes = new[] { Data.RootNode }; //  currentNode should actially be an array of nodes 

            Tokenizer tokenizer = new Tokenizer();
            Parser parser = new Parser();

            List<Token> tokenSequence = tokenizer.Tokenize(queryPath);
            Query query = parser.Parse(tokenSequence);

            for (int idx = 0; idx < query.mNodeFilters.Length; ++idx)
            {
                INodeFilter nodeFilter = query.mNodeFilters[idx];
                nodes = nodeFilter.Filter(nodes);
            }

            Logger.TraceFormat(LogChannel, "ExecuteQuery, query={0}, results_count={1}", queryPath, nodes.Length);

            //  generate the data 
            QueryResult payload = new QueryResult { };
            foreach (RoutingNode node in nodes)
            {
                Attribute[] attributes;
                if (string.IsNullOrEmpty(query.mAttributeSelector))
                {
                    attributes = RoutingInterface.Attributes(node.Value);
                }
                else
                {
                    Attribute attribute = RoutingInterface.Attribute(node.Value, query.mAttributeSelector);
                    attributes = new Attribute[] { attribute };
                }
            
                Node serializableNode = new Node
                {
                    Name = node.Name,
                    Path = node.Path,
                    Attributes = attributes 
                };

                payload.Nodes.Add(serializableNode);
            }

            return payload;
        }
        
        //---------------------------------------------------------------------
        public static Rectangle RectTransformToScreenSpace(RectTransform transform)
        {
            UnityEngine.Vector2 size = UnityEngine.Vector2.Scale(transform.rect.size, transform.lossyScale);
            Rectangle rect = new Rectangle
            {
                X = transform.position.x - (transform.pivot.x * size.x), 
                Y = transform.position.y - (transform.pivot.y * size.y), 
                Width = size.x, 
                Height = size.y
            };
            return rect;
        }
        
        //---------------------------------------------------------------------
        private static Rect GetScreenRectFromRectTransform(Canvas canvas, RectTransform rectTransform)
        {
            //  assert ( canvas.renderMode == RenderMode.ScreenSpaceOverlay )

            UnityEngine.Vector3[] corners = new UnityEngine.Vector3[4];
            rectTransform.GetWorldCorners(corners);

            UnityEngine.Vector2 screenPosition = canvas.worldCamera.WorldToScreenPoint(corners[0]);
            Rect screenRect = new Rect(screenPosition.x, screenPosition.y, 0f, 0f);

            for (int idx = 1; idx < corners.Length; ++idx)
            {
                screenPosition = canvas.worldCamera.WorldToScreenPoint(corners[idx]);
                screenRect.xMin = Mathf.Min(screenRect.xMin, screenPosition.x);
                screenRect.xMax = Mathf.Max(screenRect.xMax, screenPosition.x);
                screenRect.yMin = Mathf.Min(screenRect.yMin, screenPosition.y);
                screenRect.yMax = Mathf.Max(screenRect.yMax, screenPosition.y);
            }
            
            return screenRect;
        }

        //---------------------------------------------------------------------
        private static async Task<Response> ExampleRequest2(Request request, CancellationToken cancellationToken) //  move this external 
        {
            string queryPath = request.Payload.ToObject<string>();

            QueryResult payload;

            do
            {
                payload = ExecuteQuery(queryPath);

                await Task.Delay(100);

                break;

            } while (payload.Nodes.Count == 0);


            Response response = new Response.Builder(request)
                .AttachPayload(payload)
                .Build();

            return response;
        }
    }
}