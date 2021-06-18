﻿using SimpleClassicThemeTaskbar.Helpers.NativeMethods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;

namespace SimpleClassicThemeTaskbar.Helpers
{
    internal static class HelperFunctions
    {
        public static void Crawl(this Control control, Action<Control> action)
        {
            foreach (Control childControl in control.Controls)
            {
                Crawl(childControl, action);
            }

            action.Invoke(control);
        }

        public static IEnumerable<Taskbar> GetOpenTaskbars()
        {
            // We're using a for loop because apparently Application.OpenForm can be modified while iterating
            for (int i = 0; i < Application.OpenForms.Count; i++)
            {
                var form = Application.OpenForms[i];
                if (form is Taskbar taskbar)
                {
                    if (form.ParentForm is not Settings)
                    {
                        yield return taskbar;
                    }
                }
            }
        }

        delegate IntPtr MsgHookProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        static IntPtr proc(IntPtr hhk, uint nCode, IntPtr wParam, IntPtr lParam)
        {
            const uint WM_DESTROYWINDOW = 0xBFFF;

            Logger.Log(LoggerVerbosity.Verbose, "TrayHook", "HOOK IS WORK YES");
            if (nCode == WM_DESTROYWINDOW)
            {
                File.WriteAllText("C:\\fuck.txt", "fuck.txt");
                MessageBox.Show(Environment.CurrentDirectory);
                if (User32.DestroyWindow(wParam))
                    return new IntPtr(1);
                else
                    return new IntPtr(0);
            }
            return User32.CallNextHookEx(hhk, nCode, wParam, lParam); ;
        }

        public static void OpenQuickLaunchFolder()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var quickLaunchPath = Path.Combine(appDataPath, @"Microsoft\Internet Explorer\Quick Launch");

            _ = Process.Start(new ProcessStartInfo
            {
                FileName = quickLaunchPath,
                UseShellExecute = true,
            });
        }

        public static void SetFlatStyle(this Form form, FlatStyle style)
        {
            form.Crawl((control) =>
            {
                switch (control)
                {
                    case ButtonBase buttonBase:
                        buttonBase.FlatStyle = style;
                        break;

                    case Label label:
                        label.FlatStyle = style;
                        break;

                    case ComboBox comboBox:
                        comboBox.FlatStyle = style;
                        break;

                    case GroupBox groupBox:
                        groupBox.FlatStyle = style;
                        break;
                }
            });
        }

        public static Bitmap RemoveTransparencyKey(this Bitmap bitmap, Color? transparencyKey = null)
        {
            if (!transparencyKey.HasValue)
            {
                transparencyKey = Color.Magenta;
            }

            var rect = new Rectangle(Point.Empty, bitmap.Size);
            bitmap = bitmap.Clone(rect, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            var bitsPerPixel = Image.GetPixelFormatSize(bitmapData.PixelFormat);
            var size = bitmapData.Stride * bitmapData.Height;
            var data = new byte[size];

            Marshal.Copy(bitmapData.Scan0, data, 0, size);

            for (int i = 0; i < size; i += bitsPerPixel / 8)
            {
                // var magnitude = 1 / 3d * (data[i] + data[i + 1] + data[i + 2]);

                //data[i] is the first of 3 bytes of color
                if (transparencyKey.Value.B == data[i] &&
                    transparencyKey.Value.G == data[i + 1] &&
                    transparencyKey.Value.R == data[i + 2])
                {
                    data[i + 3] = 0;
                }
            }

            Marshal.Copy(data, 0, bitmapData.Scan0, data.Length);
            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }

        public static void DrawImage(this Graphics graphics, Image image, Rectangle destRect, Rectangle srcRect)
        {
            graphics.DrawImage(image, destRect, srcRect, GraphicsUnit.Pixel);
        }
    }
}