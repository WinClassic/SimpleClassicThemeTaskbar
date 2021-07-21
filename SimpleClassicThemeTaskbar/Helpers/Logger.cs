﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleClassicThemeTaskbar.Helpers
{
    public enum LoggerVerbosity : int
    {
        None = 0,

        /// <summary>
        /// Log messages about basic information
        /// </summary>
        Basic = 1,

        /// <summary>
        /// Log messages about program flow
        /// </summary>
        Detailed = 2,

        /// <summary>
        /// Log messages for debugging or about received values
        /// </summary>
        Verbose = 3
    }

    public static class Logger
    {
        private static FileStream fs;
        private static bool loggerOff = false;
        private static LoggerVerbosity verb;

        public static LoggerVerbosity GetVerbosity() => verb;

        public static void Initialize(LoggerVerbosity verbosity)
        {
            SetVerbosity(verbosity);
            if (loggerOff)
                return;

            fs = new FileStream("latest.log", FileMode.Create, FileAccess.Write, FileShare.Read);
            Log(LoggerVerbosity.Basic, "Logger", "Succesfully initialized logger");

            Log(LoggerVerbosity.Detailed, "SystemDump", "Performing quick system dump");
            Log(LoggerVerbosity.Detailed, "SystemDump", $"OS: {RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}");
            if (ApplicationEntryPoint.SCTCompatMode) Log(LoggerVerbosity.Detailed, "SystemDump", $"SCT version: {Assembly.LoadFrom("C:\\SCT\\SCT.exe").GetName().Version}");
            Log(LoggerVerbosity.Detailed, "SystemDump", $"SCT Taskbar version: {Assembly.GetExecutingAssembly().GetName().Version}");
        }

        public static void Log(LoggerVerbosity verbosity, string source, string text)
        {
            if (Config.Instance.EnableDebugging)
                Debug.WriteLine(text, source);

            text.Replace("\n", "".PadLeft(38));
            if (loggerOff) return;
            if (verbosity <= verb)
            {
                string toWrite = $"[{verbosity,-8}][{source,-24}]: {text}\n";
                byte[] bytes = Encoding.UTF8.GetBytes(toWrite);
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush();
            }
        }

        public static void OpenLog()
        {
            if (fs != null)
            {
                fs.Flush();
                fs.Close();
                Process.Start("C:\\Windows\\system32\\notepad.exe", fs.Name);
                fs = new FileStream("latest.log", FileMode.Append, FileAccess.Write, FileShare.Read);
            }
        }

        public static void SetVerbosity(LoggerVerbosity verbosity)
        {
            verb = verbosity;
            loggerOff = verb == LoggerVerbosity.None;
        }

        public static void Uninitialize()
        {
            Log(LoggerVerbosity.Basic, "Logger", "Shutting down logger");
            fs.Close();
        }
    }
}