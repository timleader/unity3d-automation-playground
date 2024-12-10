
using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Automation.Common
{

    public class Node
    {
        //---------------------------------------------------------------------
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("attributes")]
        public Attribute[] Attributes { get; set; }
        
        //---------------------------------------------------------------------
        public bool TryGetValue<T>(string attributeName, out T typedValue)
        {
            Attribute attribute = null;
            for (int idx = 0; idx < Attributes.Length; ++idx)
            {
                if (Attributes[idx].Name == attributeName)
                {
                    attribute = Attributes[idx];
                    break;
                }
            }

            if (attribute == null)
            {
                typedValue = default(T);
                return false;
            }

            return attribute.TryGetValue<T>(out typedValue);
        }
    }

    public enum AttributeType
    {
        Boolean,
        String,
        Integer,
        Float,
        Rectangle,
        Vector2,
        Vector3,
        GenericDictionary,
    }

    public class Rectangle
    {
        [JsonProperty("x")]
        public float X { get; set; }
        [JsonProperty("y")]
        public float Y { get; set; }
        [JsonProperty("width")]
        public float Width { get; set; }
        [JsonProperty("height")]
        public float Height { get; set; }

        public Vector2 Center()
        {
            return new Vector2
            {
                X = X + (Width * 0.5f),
                Y = Y + (Height * 0.5f)
            };
        }
    }

    public class Vector2
    {
        [JsonProperty("x")]
        public float X { get; set; }
        [JsonProperty("y")]
        public float Y { get; set; }
    }

    public class Vector3
    {
        [JsonProperty("x")]
        public float X { get; set; }
        [JsonProperty("y")]
        public float Y { get; set; }
        [JsonProperty("z")]
        public float Z { get; set; }
    }
    
    public class Attribute
    {
        
        //---------------------------------------------------------------------
        private const string LogChannel = "attribute";
        
        //---------------------------------------------------------------------
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("type"), JsonConverter(typeof(StringEnumConverter))]
        public AttributeType Type { get; private set; }

        [JsonProperty("value")] 
        private JToken mValue;
        
        //---------------------------------------------------------------------
        public void SetValue(object value)
        {
            mValue = JToken.FromObject(value);
            
            Type valueType = value.GetType();
            if (valueType == typeof(bool))
            {
                Type = AttributeType.Boolean;
            }
            else if (valueType == typeof(string))
            {
                Type = AttributeType.String;
            }
            else if (valueType == typeof(int))
            {
                Type = AttributeType.Integer;
            }
            else if (valueType == typeof(float))
            {
                Type = AttributeType.Float;
            }
            else if (valueType == typeof(Rectangle))
            {
                Type = AttributeType.Rectangle;
            }
            else if (valueType == typeof(Vector2))
            {
                Type = AttributeType.Vector2;
            }
            else if (valueType == typeof(Vector3))
            {
                Type = AttributeType.Vector3;
            }
            else
            {
                Type = AttributeType.GenericDictionary;
            }
        }
        
        //---------------------------------------------------------------------
        public bool TryGetValue<T>(out T typedValue) 
        {
            bool result = false;
            
            try
            {
                typedValue = mValue.ToObject<T>();
                result = true;
            }
            catch (Exception exception)
            {
                Logger.Error(LogChannel, exception.Message);
                typedValue = default(T);
                result = false;
            }
            
            return result;
        }
    }


    public class QueryResult
    {
        //---------------------------------------------------------------------
        public List<Node> Nodes { get; set; } = new List<Node>();

        //  Function Call Results 
    }

}
