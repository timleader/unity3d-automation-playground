
using System;
using System.Collections.Generic;


namespace Automation.Common
{

    public interface ILogAdapter
    {
        //---------------------------------------------------------------------
        void OnLogMessage(LogLevel level, string channel, string message);
    }

    public enum LogLevel
    {
        Trace,
        Info,
        Warn,
        Error,
        Fatal
    }

    public static class Logger     //  static 
    {

        //---------------------------------------------------------------------
        private static readonly List<ILogAdapter> sLogAdapters = new List<ILogAdapter> { };

        //---------------------------------------------------------------------
        public static void RegisterAdapter(ILogAdapter logAdapter)
        {
            sLogAdapters.Add(logAdapter);
        }

        //---------------------------------------------------------------------
        private static void EmitLogMessage(LogLevel level, string channel, string message)
        {
            for (int idx = 0; idx < sLogAdapters.Count; ++idx)
            {
                try
                {
                    sLogAdapters[idx].OnLogMessage(level, channel, message);
                }
                catch (Exception) { }
            }
        }

        //---------------------------------------------------------------------
        public static void Trace(string channel, string message) => EmitLogMessage(LogLevel.Trace, channel, message);

        //---------------------------------------------------------------------
        public static void TraceFormat(string channel, string format, params object[] args) => EmitLogMessage(LogLevel.Trace, channel, string.Format(format, args));

        //---------------------------------------------------------------------
        public static void Info(string channel, string message) => EmitLogMessage(LogLevel.Info, channel, message);

        //---------------------------------------------------------------------
        public static void InfoFormat(string channel, string format, params object[] args) => EmitLogMessage(LogLevel.Info, channel, string.Format(format, args));

        //---------------------------------------------------------------------
        public static void Warn(string channel, string message) => EmitLogMessage(LogLevel.Warn, channel, message);

        //---------------------------------------------------------------------
        public static void WarnFormat(string channel, string format, params object[] args) => EmitLogMessage(LogLevel.Warn, channel, string.Format(format, args));

        //---------------------------------------------------------------------
        public static void Error(string channel, string message) => EmitLogMessage(LogLevel.Error, channel, message);

        //---------------------------------------------------------------------
        public static void ErrorFormat(string channel, string format, params object[] args) => EmitLogMessage(LogLevel.Error, channel, string.Format(format, args));

        //---------------------------------------------------------------------
        public static void Exception(string channel, Exception ex)
        {
            //Debug.LogException(ex);
        }

    }
}
