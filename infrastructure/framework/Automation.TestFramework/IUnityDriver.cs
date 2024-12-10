

/*
 * 
 *  Need better naming:
 *      TestSide 
 *          TestConnection
 *              Sends commands to game
 *      
 *      GameSide
 *          GameConnection 
 *              Receives commands from test and responds
 *  
 *  
 *      
 *      Realtime Gameplay Runner 
 *          - Embed Realtime Module in Application ... 
 *          
 *      Load Realtime Module ... 
 * 
 */

using System;
using System.Threading.Tasks;

using Automation.Common;


namespace Automation.TestFramework
{


    /*
     *  IUnityDriver Factory(string name)...
     * 
     */


    public enum ApplicationState
    {
        NotInstalled,
        NotRunning,
        RunningInForeground,
        RunningInBackground
    }

    public class Property
    {
        public string Name;
        public Type Type;
        public object Value;
    }

    public class ProxyObject
    {
        public int InstanceId;
        public string Path;

        public Property[] Properties;
    }

    public interface IUnityDriver
    {

        //-------------------------------------------------------------------------
        //  Status
        //-------------------------------------------------------------------------

        ApplicationState ApplicationState { get; }


        //-------------------------------------------------------------------------
        //  Game Query Language
        //-------------------------------------------------------------------------

        Task LaunchAsync();
        Task QuitAsync();


        //-------------------------------------------------------------------------
        //  Game Query Language
        //-------------------------------------------------------------------------

        Task SubscribeAsync(string subject, Action<object> callback);
        //  return Subscription object


        //-------------------------------------------------------------------------
        //  Game Query Language
        //-------------------------------------------------------------------------

        Task<QueryResult> QueryAsync(string query, TimeSpan waitTime);
        Task ExecuteAsync(string command);      //  response...


        //-------------------------------------------------------------------------
        //  Input
        //-------------------------------------------------------------------------

        Task InputTapAsync(int x, int y);


        //-------------------------------------------------------------------------
        //  Realtime Module
        //-------------------------------------------------------------------------

        Task RealtimeModuleLoadAsync(string modulePath);
        Task RealtimeModuleStartAsync(string moduleIdentifier, string parameter);
        Task RealtimeModuleStopAsync();

    }

}