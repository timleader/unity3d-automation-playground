
using System;
using System.Collections.Generic;


namespace Automation.Runtime.QueryLanguage
{

    public class Parser
    {

        public Query Parse(IEnumerable<Token> tokenStream)
        {
            IEnumerator<Token> tokenEnumerator = tokenStream.GetEnumerator();
            
            List<INodeFilter> nodeFilters = new List<INodeFilter>();
            string attributeSelector  = string.Empty;

            while (tokenEnumerator.MoveNext())
            {
                Token token = tokenEnumerator.Current;

                if (token.Type == TokenType.Slash)
                {
                    //  Node Filter 
                    tokenEnumerator.MoveNext();

                    Token identifierToken = tokenEnumerator.Current;
                    switch (identifierToken.Type)
                    {
                        case TokenType.Identifier:
                            nodeFilters.Add(new AbsoluteMatchFilter(identifierToken.Value));
                            break;
                        case TokenType.Wildcard:
                            nodeFilters.Add(new WildcardMatchFilter());
                            break;
                        case TokenType.RecursiveWildcard:
                            nodeFilters.Add(new RecursiveWildcardMatchFilter());
                            break;
                        case TokenType.OpenBracket:
                            nodeFilters.Add(ParseAttributeFilter(tokenEnumerator)); //  need to improve this 
                            break;
                        case TokenType.AttributeMarker:
                            attributeSelector = ParseAttributeMatch(tokenEnumerator);
                            break;
                    }
                }
                else
                {
                    //  implement this !!! 
                }
            }

            Query query = new Query
            {
                mNodeFilters = nodeFilters.ToArray(),
                mAttributeSelector = attributeSelector
            };

            return query;
        }

        private static string ParseAttributeMatch(IEnumerator<Token> tokenEnumerator)
        {
            tokenEnumerator.MoveNext();
            Token token = tokenEnumerator.Current;
            if (token.Type != TokenType.Identifier)
            {
                throw new Exception("Expected identifier");
            }

            string attributeName = token.Value;

            if (tokenEnumerator.MoveNext() == false)
            {
                return attributeName;
            }

            token = tokenEnumerator.Current;
            if (token.Type != TokenType.Namespacing)
            {
                throw new Exception("Expected equals");
            }

            tokenEnumerator.MoveNext();
            token = tokenEnumerator.Current;
            if (token.Type != TokenType.Identifier)
            {
                throw new Exception("Expected string");
            }

            string nestedName = token.Value;

            //  check for function call ... 

            return string.Empty;
        }

        private static INodeFilter ParseAttributeFilter(IEnumerator<Token> tokenEnumerator)
        {
            tokenEnumerator.MoveNext();
            Token token = tokenEnumerator.Current;
            if (token.Type != TokenType.AttributeMarker)
            {
                throw new Exception("Expected Attribute token");
            }

            tokenEnumerator.MoveNext();
            token = tokenEnumerator.Current;
            if (token.Type != TokenType.Identifier)
            {
                throw new Exception("Expected Identifier token");
            }

            string identifier = token.Value;

            tokenEnumerator.MoveNext();
            token = tokenEnumerator.Current;
            if (token.Type != TokenType.Equals)
            {
                throw new Exception("Expected Equal token");
            }

            tokenEnumerator.MoveNext();
            token = tokenEnumerator.Current;
            if (token.Type != TokenType.Identifier)
            {
                throw new Exception("Expected String token");
            }

            string value = token.Value;

            tokenEnumerator.MoveNext();
            token = tokenEnumerator.Current;
            if (token.Type != TokenType.CloseBracket)
            {
                throw new Exception("Expected CloseBracket token");
            }

            return new AttributeMatchFilter(identifier, value);
        }
    }

    public interface INodeFilter
    {
        RoutingNode[] Filter(RoutingNode[] nodes);
    }

    public abstract class SimpleMatchFilter : INodeFilter
    {
        public abstract bool Match(RoutingNode node);

        public RoutingNode[] Filter(RoutingNode[] nodes)
        {
            List<RoutingNode> filteredNodes = new List<RoutingNode>();
            foreach (RoutingNode node in nodes)
            {
                RoutingNode[] children = RoutingInterface.Children(node);
                for (int childIdx = 0; childIdx < children.Length; ++childIdx)
                {
                    RoutingNode child = children[childIdx];

                    if (Match(child) == true)
                    {
                        filteredNodes.Add(child);
                    }
                }
            }

            return filteredNodes.ToArray();
        }
    }

    public class AttributeMatchFilter : SimpleMatchFilter
    {
        public readonly string mAttributeName;
        public readonly object mAttributeMatchValue;

        public AttributeMatchFilter(string attributeName, object attributeMatchValue)
        {
            mAttributeName = attributeName;
            mAttributeMatchValue = attributeMatchValue;
        }

        public override bool Match(RoutingNode node)
        {
            object attribute = RoutingInterface.Attribute(node.Value, mAttributeName);
            return attribute == mAttributeMatchValue;
        }
    }

    public class AbsoluteMatchFilter : SimpleMatchFilter
    {
        private readonly string mMatchName;

        public AbsoluteMatchFilter(string matchName)
        {
            mMatchName = matchName;
        }

        public override bool Match(RoutingNode node) => string.Compare(node.Name, mMatchName, true) == 0;
    }

    public class WildcardMatchFilter : SimpleMatchFilter
    {
        public override bool Match(RoutingNode node) => true;
    }

    public class RecursiveWildcardMatchFilter : INodeFilter
    {
        public RoutingNode[] Filter(RoutingNode[] nodes)
        {
            List<RoutingNode> filteredNodes = new List<RoutingNode>();
            foreach (RoutingNode node in nodes)
            {
                RoutingNode[] children = RoutingInterface.Children(node);
                RoutingNode[] recursiveCollectedNodes = Filter(children);
                filteredNodes.AddRange(recursiveCollectedNodes);
            }

            return filteredNodes.ToArray();
        }
    }

}