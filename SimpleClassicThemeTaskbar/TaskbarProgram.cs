﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using HWND = System.IntPtr;
using BOOL = System.Boolean;
using DWORD = System.Int32;

namespace SimpleClassicThemeTaskbar
{
    public partial class TaskbarProgram : UserControl
    {
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public string Title
        {
            get { return label1.Text; }
            set { label1.Text = value;  }
        }

        internal Window Window;
        public Icon Icon
        {
            get { return Icon.FromHandle(((Bitmap) pictureBox1.Image).GetHicon()); }
            set { if (value != null) pictureBox1.Image = value.ToBitmap(); }
        }

        public HWND WindowHandle;

        public new MouseEventHandler MouseDown;
        public new MouseEventHandler MouseUp;
        public new MouseEventHandler MouseMove;

        private bool activeWindow = false;
        public BOOL ActiveWindow
        {
            get
            {
                return activeWindow;
            }
            set 
            { 
                activeWindow = value;
                panel1.style = activeWindow ? Border3DStyle.Sunken : Border3DStyle.Raised;
                panel1.BackColor = activeWindow ? SystemColors.ControlLightLight : SystemColors.Control;
                label1.Font = activeWindow ? new Font(label1.Font.FontFamily, label1.Font.Size, FontStyle.Bold, GraphicsUnit.Pixel) : new Font(label1.Font.FontFamily, label1.Font.Size, FontStyle.Regular, GraphicsUnit.Pixel);
                panel1.Invalidate();
            }
        }

        public TaskbarProgram()
        {
            InitializeComponent();

            base.MouseDown += delegate (object sender, MouseEventArgs e) { MouseDown?.DynamicInvoke(this, e); };
            panel1.MouseDown += delegate (object sender, MouseEventArgs e) { MouseDown?.DynamicInvoke(this, e); };
            pictureBox1.MouseDown += delegate (object sender, MouseEventArgs e) { MouseDown?.DynamicInvoke(this, e); };
            label1.MouseDown += delegate (object sender, MouseEventArgs e) { MouseDown?.DynamicInvoke(this, e); };
            base.MouseUp += delegate (object sender, MouseEventArgs e) { MouseUp?.DynamicInvoke(this, e); };
            panel1.MouseUp += delegate (object sender, MouseEventArgs e) { MouseUp?.DynamicInvoke(this, e); };
            pictureBox1.MouseUp += delegate (object sender, MouseEventArgs e) { MouseUp?.DynamicInvoke(this, e); };
            label1.MouseUp += delegate (object sender, MouseEventArgs e) { MouseUp?.DynamicInvoke(this, e); };
            base.MouseMove += delegate (object sender, MouseEventArgs e) { MouseMove?.DynamicInvoke(this, e); };
            panel1.MouseMove += delegate (object sender, MouseEventArgs e) { MouseMove?.DynamicInvoke(this, e); };
            pictureBox1.MouseMove += delegate (object sender, MouseEventArgs e) { MouseMove?.DynamicInvoke(this, e); };
            label1.MouseMove += delegate (object sender, MouseEventArgs e) { MouseMove?.DynamicInvoke(this, e); };


            SizeChanged += delegate { panel1.Width = this.Width; };

            panel1.Do3DBorder = false;
            panel1.Invalidate();

            Click += OnClick;
            pictureBox1.Click += OnClick;
            panel1.Click += OnClick;
            label1.Click += OnClick;
        }

        private void OnClick(object sender, EventArgs e)
        {
            if (ActiveWindow)
            {
                ShowWindow(WindowHandle, 6);
            }
            else
            {
                if ((Window.WindowInfo.dwStyle & 0x20000000) > 0)
                    ShowWindow(WindowHandle, 9);
                SetForegroundWindow(WindowHandle);

                foreach (Control d in Parent.Controls)
                {
                    if (d is TaskbarProgram b)
                    {
                        b.ActiveWindow = false;
                    }
                }

                ActiveWindow = true;
            }
        }

        private void TaskbarProgram_Load(object sender, EventArgs e)
        {
            line.Width = Width;
        }
    }
}
