
using System;
using System.Threading.Tasks;

using NUnit.Framework;

using Automation.Common;
using Automation.TestFramework;


namespace Tests
{

    public class BasicTest
    {
        //---------------------------------------------------------------------
        private IUnityDriver mUnityDriver;

        //---------------------------------------------------------------------
        [OneTimeSetUp]
        public void Setup()
        {
            mUnityDriver = UnityDriverFactory.NewUnityDriver("editor");
        }

        //---------------------------------------------------------------------
        [OneTimeTearDown]
        public void TearDown()
        {
        }

        //---------------------------------------------------------------------
        [Test]
        public async Task Test1()
        {
            QueryResult result;
            Node node;
            
            await mUnityDriver.LaunchAsync();    

            await mUnityDriver.SubscribeAsync("logs.agent", (message) => { Console.WriteLine(message); });
            //await mUnityDriver.SubscribeAsync("telemetry.*", (message) => { Console.WriteLine(message); });

            result = await mUnityDriver.QueryAsync("/application/info/@unityVersion", TimeSpan.FromSeconds(30));
            node = result.Nodes[0];
            bool hasUnityVersionAttribute = node.TryGetValue<string>("unityVersion", out string unityVersion);
            Assert.IsTrue(hasUnityVersionAttribute);
            Assert.AreEqual("2022.3.20f1", unityVersion);
            
            result = await mUnityDriver.QueryAsync("/scene/Canvas/TestScreen/AcceptBtn", TimeSpan.FromSeconds(10));
            node = result.Nodes[0];
            bool hasBoundsAttribute = node.TryGetValue<Rectangle>("RectTransform:bounds", out Rectangle acceptBtnRect);
            Assert.IsTrue(hasBoundsAttribute);

            Vector2 clickPoint = acceptBtnRect.Center();
            await mUnityDriver.InputTapAsync((int)clickPoint.X, (int)clickPoint.Y);
            
            result = await mUnityDriver.QueryAsync("/scene/Canvas/TestScreen/AcceptBtn/*", TimeSpan.FromSeconds(10));
            node = result.Nodes[0];
            bool hasTextAttribute = node.TryGetValue<string>("TMP_Text:text", out string acceptBtnText);
            Assert.IsTrue(hasTextAttribute);
            
            //  input gestures - eg. drag, pinch, etc..

            //  wait for game state x  !!! 
            //await mUnityDriver.QueryAsync("/services/GameState", TimeSpan.FromSeconds(180f));

            /*

            //  need a wait for game_state


            //  assert that it is the expected version, etc..
            //proxyObjects = await mUnityDriver.QueryAsync("services/GameStateManager", TimeSpan.FromSeconds(10));
            //  responses : not_found, unknown_error, success


            //  ability to call a static method too 



            //await mUnityDriver.RealtimeModuleLoadAsync("path/to/module");

            //await mUnityDriver.RealtimeModuleStartAsync("module_identifier", "parameters");

            //  should be able to communicate with the `RealtimeModule`
            */

            //await mUnityDriver.QuitAsync();       // maybe this is async 

        }

    }

}