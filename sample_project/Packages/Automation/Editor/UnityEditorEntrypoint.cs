

using UnityEditor;

using Automation.Runtime;
using Automation.TestFramework;

namespace Automation.Editor
{

    [InitializeOnLoad]
    public static class UnityEditorEntrypoint
    {
        //---------------------------------------------------------------------
        private static WebSocketServerTransport sSocketServerTransport;
        private static Agent sAgent;

        //---------------------------------------------------------------------
        static UnityEditorEntrypoint()
        {
            sSocketServerTransport = new WebSocketServerTransport();
            sSocketServerTransport.Start();

            sAgent = new Agent(sSocketServerTransport);

            UnityDriverFactory.RegisterUnityDriver("editor", () => new UnityEditorDriver());
        }
    }

}
