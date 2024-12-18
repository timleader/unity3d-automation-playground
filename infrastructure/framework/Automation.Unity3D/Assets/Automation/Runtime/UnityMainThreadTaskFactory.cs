
using System;
using System.Threading.Tasks;

using UnityEngine;


namespace Automation.Runtime
{
    
    public static class UnityMainThreadTaskFactory
    {
        //---------------------------------------------------------------------
        public static TaskScheduler Scheduler { get; private set; }
        public static TaskFactory Factory { get; private set; }

        //---------------------------------------------------------------------
        public static async void Run(Action action)
        {
            await RunAsync(action);
        }

        //---------------------------------------------------------------------
        public static Task RunAsync(Action action)
        {
            return Factory.StartNew(action);
        }

        //---------------------------------------------------------------------
        public static Task<T> RunAsync<T>(Func<T> func)
        {
            return Factory.StartNew(func);
        }

        //---------------------------------------------------------------------
        public static void InvokeOnMainThread(this Action action)
        {
            Run(action);
        }

        //---------------------------------------------------------------------
        public static Task InvokeOnMainThreadAsync(this Action action)
        {
            return RunAsync(action);
        }

        //---------------------------------------------------------------------
        public static Task<T> InvokeOnMainThreadAsync<T>(Func<T> func)
        {
            return RunAsync(func);
        }

        //---------------------------------------------------------------------
        [RuntimeInitializeOnLoadMethod]
        private static void InitializeFactory()
        {
            Scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Factory = new TaskFactory(Scheduler);
        }
    }

}
