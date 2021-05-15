﻿using System;
using System.Runtime.InteropServices;

namespace SimpleClassicThemeTaskbar.Helpers.NativeMethods
{
    internal static class Shell32
    {
        internal const int SHGFI_LARGEICON = 0x0;

        internal const int SHGFI_SMALLICON = 0x1;

        internal const int SHIL_EXTRALARGE = 0x2;

        internal const int SHIL_JUMBO = 0x4;

        [DllImport("user32.dll")]
        internal static extern int DrawFrameControl(IntPtr hdc, ref RECT lpRect, uint un1, uint un2);

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern int SHGetFileInfo(string pszPath, int dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, uint uFlags);

        [DllImport("shell32.dll")]
        internal static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);
    }
}