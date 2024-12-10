
using System.Collections.Generic;

using NUnit.Framework;

using Automation.Runtime.QueryLanguage;


namespace Automation.Runtime.Tests
{

    public class QueryTests
    {

        //-------------------------------------------------------------------------
        private static readonly string[] TestQueries = new[]
        {
            "/application/info/@unityVersion",
            //"/scene/**/[@automation_id=MainButton]/@Button:Click()",
            //"/scene/\"Canvas\"/Label/@SetActive(true)",
            //"/scene/Canvas/Panel/AcceptBtn/@Button:enabled",
            //"/scene/Canvas/Label/@Text:text",
            
            //  expected token sequence 
            //  Slash, Identifier, Slash, Identifier, AttributeMarker, Identifier
        };

        /**
         * 
         *  /scene/Canvas/Panel/AcceptBtn
         *      attributes: 
         *          - name
         *          - active 
         *          - layer
         *          - tag
         *          - components 
         *          - children 
         *          - rect transform 
         *          - button:click()
         *          - image:color
         * 
         */

        /*
         * BatchQuery vs ReturnMultipleAttributes by default
         *  
         * 
         * 
         *  Node Output: 
         *      []struct { 
         *          Path string 
         *          Name string 
         *          ??Basic Info   --  include some useful stuff  
         *      }
         *      
         *  Attribute Output: 
         *      []struct { 
         *          Path string
         *          Name string 
         *          Value variant   --  Json.RawMessage
         *      }
         *      
         *  Function Call Output:
         *      []struct {
         *          ???
         *      }
         * 
         * 
         */

        //-------------------------------------------------------------------------
        [Test]
        public void QueryTest1()
        {
            Tokenizer tokenizer = new Tokenizer();
            Parser parser = new Parser();
            for (int idx = 0; idx < TestQueries.Length; ++idx)
            {
                string query = TestQueries[idx];
                List<Token> tokenSequence = tokenizer.Tokenize(query);
                parser.Parse(tokenSequence);
            }

        }
    }

}