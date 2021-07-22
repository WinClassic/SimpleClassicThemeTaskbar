﻿using SimpleClassicThemeTaskbar.Forms;
using SimpleClassicThemeTaskbar.Helpers;
using SimpleClassicThemeTaskbar.Helpers.NativeMethods;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SimpleClassicThemeTaskbar
{
    internal static class ApplicationEntryPoint
    {
        public static int ErrorCount = 0;
        public static UserControlEx ErrorSource;

        public static bool SCTCompatMode = false;

        public static SystemTrayNotificationService TrayNotificationService;
        private static UnmanagedCodeMigration.VirtualDesktopNotification VirtualDesktopNotification;
        private static uint virtualDesktopNotificationCookie;

        public static void VirtualDesktopNotifcation_CurrentDesktopChanged(object sender, EventArgs e)
        {
            Logger.Log(LoggerVerbosity.Detailed, "TaskbarManager", "Current virtual desktop changed, sending notification to all Taskbars.");
            var activeBars = HelperFunctions.GetOpenTaskbars();

            foreach (Taskbar bar in activeBars)
            {
                // Redo the enumeration because the open windows are completely different
                bar.Invoke(new Action(() => { bar.EnumerateWindows(); }));
            }
        }

        internal static void ExitSCTT()
        {
            Logger.Log(LoggerVerbosity.Detailed, "TaskbarManager", $"Exit requested");
            Config.Instance.SaveToRegistry();

            var activeBars = HelperFunctions.GetOpenTaskbars();

            foreach (Taskbar bar in activeBars)
            {
                foreach (Control d in bar.Controls)
                    d.Dispose();
                bar.selfClose = true;
                bar.Close();
                bar.Dispose();
            }
            // Taskbar randomBar = activeBars.FirstOrDefault();
            Logger.Log(LoggerVerbosity.Detailed, "TaskbarManager", $"Killed all taskbars");

            if (Environment.OSVersion.Version.Major >= 10 && virtualDesktopNotificationCookie != 0)
            {
                Logger.Log(LoggerVerbosity.Detailed, "TaskbarManager", "Unregistered virtual desktop notify serivce");
                UnmanagedCodeMigration.UnregisterVdmNotification(virtualDesktopNotificationCookie);
            }

            Logger.Log(LoggerVerbosity.Detailed, "TaskbarManager", "Unitializing SystemTrayNotificationService");
            TrayNotificationService.Dispose();

            Logger.Log(LoggerVerbosity.Detailed, "TaskbarManager", "Uninitialization sequence completed, exiting");
            Logger.Uninitialize();
            Environment.Exit(0);
        }

        internal static void NewTaskbars()
        {
            Logger.Log(LoggerVerbosity.Detailed, "TaskbarManager", "Generating new taskbars");

            var activeBars = HelperFunctions.GetOpenTaskbars();

            foreach (Taskbar bar in activeBars)
            {
                foreach (Control d in bar.Controls)
                    d.Dispose();
                bar.selfClose = true;
                bar.Close();
                bar.Dispose();
            }

            if (Config.Instance.EnablePassiveTray && TrayNotificationService == null)
                TrayNotificationService = new();
            else if (!Config.Instance.EnablePassiveTray && TrayNotificationService is not null)
            {
                TrayNotificationService.Dispose();
                TrayNotificationService = null;
            }
            
            int taskbars = 0;
            foreach (Screen screen in Screen.AllScreens)
            {
                taskbars++;
                Rectangle rect = screen.Bounds;
                Taskbar taskbar = new(screen.Primary);
                taskbar.ShowOnScreen(screen);
                Logger.Log(LoggerVerbosity.Detailed, "TaskbarManager", $"Created taskbar in working area: {screen.Bounds}");
                if (!Config.Instance.ShowTaskbarOnAllDesktops && !screen.Primary)
                    taskbar.NeverShow = true;
            }
            Logger.Log(LoggerVerbosity.Detailed, "TaskbarManager", $"Created {taskbars} taskbars in total");
        }

        private static void HandleException(Exception ex)
        {
            Logger.Log(LoggerVerbosity.Basic, "ExceptionHandler", $"An exception of type {ex.GetType().FullName} has occured.");
            Logger.Log(LoggerVerbosity.Detailed, "ExceptionHandler", $"Exception details:\nMessage: {ex.Message}\nException location: {ex.TargetSite}\nStack trace: {ex.StackTrace}");
            Logger.Uninitialize();

            using (var crashForm = new CrashForm(ex))
            {
                crashForm.ShowDialog();
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Logger.Initialize(LoggerVerbosity.Verbose);
            foreach (var arg in args)
            {
                if (arg.StartsWith("-v="))
                    Logger.SetVerbosity((LoggerVerbosity)Int32.Parse(arg[3..]));
            }

            Logger.Log(LoggerVerbosity.Detailed, "EntryPoint", "Parsing arguments");
            if (args.Contains("--exit"))
            {
                Logger.Log(LoggerVerbosity.Detailed, "EntryPoint", "Killing all SCTT instances");
                static List<IntPtr> EnumerateProcessWindowHandles(int processId, string name)
                {
                    List<IntPtr> handles = new();

                    foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
                    {
                        User32.EnumThreadWindows(thread.Id, (hWnd, lParam) =>
                        {
                            handles.Add(hWnd);
                            return true;
                        }, IntPtr.Zero);
                    }
                    return handles;
                }

                Process[] scttInstances = Process.GetProcessesByName("SimpleClassicThemeTaskbar");
                Array.ForEach(scttInstances, a =>
                {
                    List<IntPtr> handles = EnumerateProcessWindowHandles(a.Id, "SCTT_Shell_TrayWnd");
                    string s = "";
                    foreach (IntPtr handle in handles)
                    {
                        StringBuilder builder = new(1000);
                        User32.GetClassName(handle, builder, 1000);

                        if (builder.Length > 0)
                            s = s + builder.ToString() + "\n";

                        IntPtr returnValue = User32.SendMessage(handle, Constants.WM_SCT, new IntPtr(Constants.SCTWP_ISSCT), IntPtr.Zero);
                        if (returnValue != IntPtr.Zero)
                        {
                            User32.SendMessage(handle, Constants.WM_SCT, new IntPtr(Constants.SCTWP_EXIT), IntPtr.Zero);
                        }
                    }
                });
            }
            if (args.Contains("--gui-test"))
            {
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Forms.GraphicsTest());
                return;
            }
            if (args.Contains("--network-ui"))
            {
                Application.VisualStyleState = System.Windows.Forms.VisualStyles.VisualStyleState.NoneEnabled;
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Forms.NetworkUI());
                return;
            }
            if (args.Contains("--unmanaged"))
            {
                Logger.Log(LoggerVerbosity.Detailed, "EntryPoint", "Attempted to start unmanaged SCTT. This is not supported anymore.");
                Environment.Exit(/*d.UnmanagedSCTT()*/0);
            }
            if (args.Contains("--traydump"))
            {
                Logger.Log(LoggerVerbosity.Detailed, "EntryPoint", "Dumping system tray information to traydump.txt");
                FileStream fs = new FileStream("traydump.txt", FileMode.Create, FileAccess.ReadWrite);

                IntPtr hWndTray = User32.FindWindow("Shell_TrayWnd", null);
                if (hWndTray != IntPtr.Zero)
                {
                    hWndTray = User32.FindWindowEx(hWndTray, IntPtr.Zero, "TrayNotifyWnd", null);
                    if (hWndTray != IntPtr.Zero)
                    {
                        hWndTray = User32.FindWindowEx(hWndTray, IntPtr.Zero, "SysPager", null);
                        if (hWndTray != IntPtr.Zero)
                        {
                            hWndTray = User32.FindWindowEx(hWndTray, IntPtr.Zero, "ToolbarWindow32", null);
                            if (hWndTray != IntPtr.Zero)
                            {
                                UnmanagedCodeMigration.TBBUTTONINFO[] buttons = UnmanagedCodeMigration.GetTrayButtons(hWndTray);
                                foreach (var button in buttons)
                                {
                                    string str = "START OF TBBUTTONINFO\n";
                                    foreach (var d in button.GetType().GetProperties())
                                        str += d.Name + ": " + d.GetValue(button) + "\n";
                                    foreach (var d in button.GetType().GetFields())
                                        str += d.Name + ": " + d.GetValue(button) + "\n";
                                    str += "END OF TBBUTTONINFO\n";
                                    byte[] bytes = Encoding.UTF8.GetBytes(str);
                                    fs.Write(bytes, 0, bytes.Length);
                                    fs.Flush();
                                }
                            }
                        }
                    }
                }
                fs.Close();
                return;
            }
            if (args.Contains("--sct"))
            {
                Logger.Log(LoggerVerbosity.Detailed, "EntryPoint", "Current instance is an SCT managed instance");
                SCTCompatMode = true;
            }
#if DEBUG
#else
            else
            {
                MessageBox.Show("SCT Taskbar does not currently work without SCT. Please install SCTT via the Options menu in SCT 1.2.0 or higher", "SCT required");
                ExitSCTT();
            }
#endif

            //Setup crash reports
            Logger.Log(LoggerVerbosity.Detailed, "EntryPoint", "Release instance, enabling error handler");
            RegisterExceptionHandlers();

            //Check if system/program architecture matches
            if (Environment.Is64BitOperatingSystem != Environment.Is64BitProcess)
            {
                string sysArch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                string processArch = Environment.Is64BitProcess ? "x64" : "x86";
                MessageBox.Show($"You are trying to run a {processArch} version of SCT Taskbar on a {sysArch} version of Windows. This is not supported. Please download a {sysArch} version of SCT Taskbar", "Incorrect architecture");
#if DEBUG
#else
                return;
#endif
                Logger.Log(LoggerVerbosity.Detailed, "EntryPoint", "Debug instance, ignoring incorrect architecture");
            }

            if (Environment.OSVersion.Version.Major >= 10 && Process.GetProcessesByName("explorer").Length > 0)
            {
                Logger.Log(LoggerVerbosity.Detailed, "EntryPoint", "OSVersion.Major is equal or above 10. Initializing IVirtualDesktopManager");
                UnmanagedCodeMigration.InitializeVdmInterface();
                VirtualDesktopNotification = new();
                VirtualDesktopNotification.CurrentDesktopChanged += VirtualDesktopNotifcation_CurrentDesktopChanged;
                virtualDesktopNotificationCookie = UnmanagedCodeMigration.RegisterVdmNotification(VirtualDesktopNotification);
            }

            if (Config.Instance.EnablePassiveTray)
            {
                Logger.Log(LoggerVerbosity.Detailed, "EntryPoint", "Initializing new SystemTrayNotificationService");
                TrayNotificationService = new();
            }

            //TODO: Put TaskbarManager into a seperate class
            Logger.Log(LoggerVerbosity.Detailed, "EntryPoint", "Main initialization done, passing execution to TaskbarManager");

            Directory.CreateDirectory(Constants.VisualStyleDirectory);

            //Application.EnableVisualStyles();
            Application.VisualStyleState = System.Windows.Forms.VisualStyles.VisualStyleState.NoneEnabled;
            Application.SetCompatibleTextRenderingDefault(false);
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += delegate
            {
                NewTaskbars();
            };
            NewTaskbars();

            Application.Run();

            ExitSCTT();
        }

        private static void RegisterExceptionHandlers()
        {
            Application.ThreadException += (sender, e) => HandleException(e.Exception);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.IsTerminating && e.ExceptionObject is Exception ex)
                    HandleException(ex);
            };
            // AppDomain.CurrentDomain.FirstChanceException += (sender, e) => HandleException(e.Exception);
        }
    }
}