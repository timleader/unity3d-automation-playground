
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using Automation.Common;

using TMPro;

using Attribute = Automation.Common.Attribute;

namespace Automation.Runtime
{

    //  traversal functions
    //      breakdown into path segments 
    //          

    public struct RoutingNode
    {
        public string Name;
        public string Path;
        public object Value;
    }

    public struct LeafNode
    {
        public Dictionary<string, object> Attributes;
    }
    
    public class Data
    {
        public static Dictionary<string, object> Root = new Dictionary<string, object>
        {
            { "scene", (Func<RoutingNode[]>)sceneHandler },
            { "application", (Func<RoutingNode[]>)applicationHandler },
            { "services", (Func<RoutingNode[]>)servicesHandler },
        };

        public static RoutingNode RootNode => new RoutingNode { Name = "root", Path = "", Value = Root };

        /*
            scene -> func() 
            application -> func()
            services -> func()
            unity -> 
         */

        public static RoutingNode[] sceneHandler()       //  should return `RoutingNode[]`
        {
            List<RoutingNode> results = new List<RoutingNode>(8);
            List<GameObject> tempGameObjects = new List<GameObject>(8);
            for (int sceneIdx = 0; sceneIdx < SceneManager.sceneCount; sceneIdx++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIdx);
                scene.GetRootGameObjects(tempGameObjects);

                foreach (GameObject gameObject in tempGameObjects)
                    results.Add(new RoutingNode { Name = gameObject.name, Value = gameObject.transform });
            }
            return results.ToArray();
        }

        public static RoutingNode[] applicationHandler()
        {
            return new[]
            {
                new RoutingNode
                {
                    Name = "info",
                    Value = new LeafNode
                    {
                        Attributes = new Dictionary<string, object>
                        {
                            { "unityVersion", Application.unityVersion }
                        }
                    }
                }
            };
        }

        public static RoutingNode[] servicesHandler()
        {
            return null;
        }
        
    }

    public static class RoutingInterface
    {
        //---------------------------------------------------------------------
        public static Attribute Attribute(object @object, string name)
        {
            switch (@object)
            {
                case Transform transform:
                    return null;
                case LeafNode leafNode:
                    Attribute attribute = new Attribute
                    {
                        Name = name,
                    };
                    attribute.SetValue(leafNode.Attributes[name]);
                    return attribute;
                default:
                    return null;
            }
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
        public static Attribute[] Attributes(object @object)
        {
            List<Attribute> attributes = new List<Attribute>();
            
            switch (@object)
            {
                case Transform nodeAsTransform:

                    Component[] components = nodeAsTransform.GetComponents<Component>();
                    for (int idx = 0; idx < components.Length; idx++)
                    {
                        Component component = components[idx];
                        Attribute attribute = null;
                        switch (component)
                        {
                            case RectTransform rectTransform:
                                Rectangle rect = RectTransformToScreenSpace(rectTransform);
                                attribute = new Attribute() { Name = "RectTransform:bounds" };
                                attribute.SetValue(rect);
                                attributes.Add(attribute);
                                break;
                            case TMP_Text text:
                                attribute = new Attribute() { Name = "TMP_Text:text" };
                                attribute.SetValue(text.text);
                                attributes.Add(attribute);
                                break;
                        }
                    }
                    break;
                case LeafNode leafNode:
                    foreach (KeyValuePair<string, object> entry in leafNode.Attributes)
                    {
                        Attribute attribute = new Attribute() { Name = entry.Key };
                        attribute.SetValue(entry.Value);
                        attributes.Add(attribute);
                    }
                    break;
                default:
                    break;
            }

            return attributes.ToArray();
        }

        //---------------------------------------------------------------------
        public static RoutingNode[] Children(RoutingNode parent)
        {
            RoutingNode[] children;
            
            object @object = parent.Value;
            switch (@object)
            {
                case Dictionary<string, object> dictionary:
                {
                    List<RoutingNode> nodes = new List<RoutingNode>(dictionary.Count);
                    foreach (KeyValuePair<string, object> pair in dictionary)
                        nodes.Add(new RoutingNode { Name = pair.Key, Value = pair.Value });
                    children = nodes.ToArray();
                    break;
                }
                case Func<RoutingNode[]> func:
                {
                    children = func();
                    break;
                }
                case Transform transform:
                {
                    children = new RoutingNode[transform.childCount];
                    for (int idx = 0; idx < transform.childCount; ++idx)
                    {
                        Transform childTransform = transform.GetChild(idx);

                        children[idx] = new RoutingNode
                        {
                            Name = childTransform.name,
                            Value = childTransform
                        };
                    }
                    break;
                }
                default: 
                {
                    children = new RoutingNode[] { };
                    break;
                }
            }

            for (int idx = 0; idx < children.Length; ++idx)
                children[idx].Path = $"{parent.Path}/{children[idx].Name}";

            return children;
        }
    }
    
}
