﻿using SimpleClassicThemeTaskbar.Helpers;
using SimpleClassicThemeTaskbar.Helpers.NativeMethods;

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace SimpleClassicThemeTaskbar
{
    public partial class Settings : Form
    {
        private Taskbar previewTaskbar;
        private System.ComponentModel.ComponentResourceManager Resources = new(typeof(Settings));

        public Settings()
        {
            InitializeComponent();

            labelCopyrightSCT.Location = new Point(tabAbout.Width - labelCopyrightSCT.Width, labelCopyrightSCT.Location.Y);

            this.SetFlatStyle(FlatStyle.System);
        }

        public void SaveSettings()
        {
            Config.EnableSystemTrayHover = enableSysTrayHover.Checked;
            Config.EnableSystemTrayColorChange = enableSysTrayColorChange.Checked;
            Config.ShowTaskbarOnAllDesktops = showTaskbarOnAllDesktops.Checked;
            Config.EnableQuickLaunch = enableQuickLaunchCheckBox.Checked;
            Config.TaskbarProgramWidth = (int)taskbarProgramWidth.Value;
            Config.SpaceBetweenTrayIcons = (int)spaceBetweenTrayIcons.Value;
            Config.SpaceBetweenTaskbarIcons = (int)spaceBetweenTaskbarIcons.Value;
            Config.SpaceBetweenQuickLaunchIcons = (int)quickLaunchSpacingNumBox.Value;
            Config.StartButtonImage = customButtonTextBox.Text;
            Config.StartButtonIconImage = customIconTextBox.Text;
            Config.StartButtonAppearance = GetCurrentStartButtonAppearance();
            Config.Language = (string)languageComboBox.SelectedItem;
            Config.ProgramGroupCheck = (ProgramGroupCheck)comboBoxGroupingMethod.SelectedIndex;
            Config.ExitMenuItemCondition = (ExitMenuItemCondition)exitItemComboBox.SelectedIndex;
            Config.EnableDebugging = enableDebuggingCheckBox.Checked;
            Config.EnablePassiveTaskbar = enablePassiveTaskbarCheckBox.Checked;

            // Save taskbar filter
            string taskbarFilter = "";
            foreach (object f in taskbarFilterListBox.Items)
            {
                string filter = f.ToString();
                taskbarFilter += filter + "*";
            }
            Config.TaskbarProgramFilter = taskbarFilter;

            // Save renderer path
            switch (themeComboBox.SelectedItem)
            {
                case "Classic":
                    Config.RendererPath = "Internal/Classic";
                    break;

                case "Luna":
                    Config.RendererPath = "Internal/Luna";
                    break;

                default:
                    Config.RendererPath = (string)themeComboBox.SelectedItem;
                    break;
            }

            Config.ConfigChanged = true;
            Config.SaveToRegistry();
            ApplicationEntryPoint.NewTaskbars();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SettingsAddProgramFilter dialog = new();
            _ = dialog.ShowDialog(this);
            _ = taskbarFilterListBox.Items.Add(dialog.result);
            dialog.Dispose();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (taskbarFilterListBox.SelectedIndex != -1)
            {
                taskbarFilterListBox.Items.RemoveAt(taskbarFilterListBox.SelectedIndex);
            }
        }

        private void ButtonApply_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            SaveSettings();
            Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((string)themeComboBox.SelectedItem == "Custom...")
            {
                FolderBrowserDialog fbd = new();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string[] files = { "settings.txt", "startbutton.png", "systemtrayborder.png", "systemtraytexture.png", "taskbartexture.png", "taskbuttongroupwindowborder.png", "taskbuttonnormal.png", "taskbuttonnormalhover.png", "taskbuttonpressed.png", "taskbuttonpressedhover.png" };
                    string[] filesInDirectory = Directory.GetFiles(fbd.SelectedPath);
                    string[] filesInDirectoryLower = new string[filesInDirectory.Length];
                    for (int i = 0; i < filesInDirectory.Length; i++)
                        filesInDirectoryLower[i] = Path.GetFileName(filesInDirectory[i]).ToLower();
                    if (files.SequenceEqual(filesInDirectoryLower))
                    {
                        themeComboBox.Items.Clear();
                        themeComboBox.Items.Add("Classic");
                        themeComboBox.Items.Add("Luna");
                        themeComboBox.Items.Add("Custom...");
                        themeComboBox.Items.Add(fbd.SelectedPath);
                        themeComboBox.SelectedItem = fbd.SelectedPath;
                        return;
                    }
                }
                // Detect renderer correctly
                switch (Config.RendererPath)
                {
                    case "Internal/Classic":
                        themeComboBox.SelectedItem = "Classic";
                        break;

                    case "Internal/Luna":
                        themeComboBox.SelectedItem = "Luna";
                        break;

                    default:
                        themeComboBox.Items.Add(Config.RendererPath);
                        themeComboBox.SelectedItem = Config.RendererPath;
                        break;
                }
            }
        }

        private void CustomButtonBrowseButton_Click(object sender, EventArgs e)
        {
            customButtonFileDialog.ShowDialog();
        }

        private void CustomButtonFileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Image temp;

            try
            {
                temp = Image.FromFile(customButtonFileDialog.FileName);
            }
            catch
            {
                _ = MessageBox.Show(this, "The image you selected is invalid.", "SimpleClassicThemeTaskbar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            if (temp.Height != 66)
            {
                _ = MessageBox.Show(this, $"The image must be 66 pixels in height. The current height is {temp.Height} pixels. (3 * 22px)", "SimpleClassicThemeTaskbar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            customButtonTextBox.Text = customButtonFileDialog.FileName;
            temp.Dispose();

            UpdateStartButton();
        }

        private void CustomIconBrowseButton_Click(object sender, EventArgs e)
        {
            customIconFileDialog.ShowDialog();
        }

        private void CustomIconFileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Image temp;
            try
            {
                temp = Image.FromFile(customIconFileDialog.FileName);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(this, "The image you selected is invalid.", "SimpleClassicThemeTaskbar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            if (temp.Width != 16 || temp.Height != 16)
            {
                _ = MessageBox.Show(this, $"The image you specified must be 16x16 pixels in size.", "SimpleClassicThemeTaskbar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            customIconTextBox.Text = customIconFileDialog.FileName;
            temp.Dispose();

            UpdateStartButton();
        }

        private void EnableQuickLaunchCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            quickLaunchOptionsPanel.Enabled = enableQuickLaunchCheckBox.Checked;
        }

        private StartButtonAppearance GetCurrentStartButtonAppearance()
        {
            if (radioStartDefault.Checked)
            {
                return StartButtonAppearance.Default;
            }
            else if (customIconRadioButton.Checked)
            {
                return StartButtonAppearance.CustomIcon;
            }
            else if (customButtonRadioButton.Checked)
            {
                return StartButtonAppearance.CustomButton;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private void LayoutTaskbarPreview(bool showRightSide = false)
        {
            if (previewTaskbar == null)
            {
                previewTaskbar = new Taskbar(true)
                {
                    TopLevel = false,
                    Width = 1024, // 1024x768 is a pretty normal resolution
                    Dummy = true,
                };

                panelPreview.Controls.Add(previewTaskbar);

                previewTaskbar.Show();
            }

            const int weirdOffset = -4;
            previewTaskbar.Top = panelPreview.Height - previewTaskbar.Height + weirdOffset;

            if (showRightSide)
            {
                previewTaskbar.Left = panelPreview.Width - previewTaskbar.Width;
            }
            else
            {
                previewTaskbar.Left = 0;
            }
        }

        private void LoadSettings()
        {
            comboBoxGroupingMethod.SelectedIndex = (int)Config.ProgramGroupCheck;
            exitItemComboBox.SelectedIndex = (int)Config.ExitMenuItemCondition;

            enablePassiveTaskbarCheckBox.Checked = Config.EnablePassiveTaskbar;
            enableDebuggingCheckBox.Checked = Config.EnableDebugging;
            enableSysTrayHover.Checked = Config.EnableSystemTrayHover;
            enableSysTrayColorChange.Checked = Config.EnableSystemTrayColorChange;
            showTaskbarOnAllDesktops.Checked = Config.ShowTaskbarOnAllDesktops;
            enableQuickLaunchCheckBox.Checked = Config.EnableQuickLaunch;

            // Start button
            customIconRadioButton.Checked = Config.StartButtonAppearance == StartButtonAppearance.CustomIcon;
            customButtonRadioButton.Checked = Config.StartButtonAppearance == StartButtonAppearance.CustomButton;
            radioStartDefault.Checked = Config.StartButtonAppearance == StartButtonAppearance.Default;
            customButtonTextBox.Text = Config.StartButtonImage;
            customIconTextBox.Text = Config.StartButtonIconImage;

            taskbarProgramWidth.Value = Math.Min(Config.TaskbarProgramWidth, taskbarProgramWidth.Maximum);
            spaceBetweenTrayIcons.Value = Math.Min(Config.SpaceBetweenTrayIcons, spaceBetweenTrayIcons.Maximum);
            spaceBetweenTaskbarIcons.Value = Math.Min(Config.SpaceBetweenTaskbarIcons, spaceBetweenTaskbarIcons.Maximum);
            quickLaunchSpacingNumBox.Value = Math.Min(Config.SpaceBetweenQuickLaunchIcons, quickLaunchSpacingNumBox.Maximum);

            taskbarProgramWidth.Maximum = Screen.PrimaryScreen.Bounds.Width;
            languageComboBox.SelectedItem = Config.Language;

            if (!Config.EnableDebugging)
            {
                tabControl.TabPages.Remove(tabDebug);
            }

            // Load taskbar filter
            string taskbarFilter = Config.TaskbarProgramFilter;
            foreach (string filter in taskbarFilter.Split('*'))
                if (filter != "")
                    _ = taskbarFilterListBox.Items.Add(filter);

            // Detect renderer correctly
            switch (Config.RendererPath)
            {
                case "Internal/Classic":
                    themeComboBox.SelectedItem = "Classic";
                    break;

                case "Internal/Luna":
                    themeComboBox.SelectedItem = "Luna";
                    break;

                default:
                    themeComboBox.Items.Add(Config.RendererPath);
                    themeComboBox.SelectedItem = Config.RendererPath;
                    break;
            }
        }

        private void QuickLaunchLinkLabel_Click(object sender, EventArgs e)
        {
            Helpers.Helpers.OpenQuickLaunchFolder();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            labelCopyrightSCTT.Text = labelCopyrightSCTT.Text.Replace("{sctt_ver}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            if (ApplicationEntryPoint.SCTCompatMode)
            {
                labelCopyrightSCT.Show();
                labelCopyrightSCT.Text = labelCopyrightSCT.Text.Replace("{sct_ver}", Assembly.LoadFrom("C:\\SCT\\SCT.exe").GetName().Version.ToString());
            }

            LoadSettings();

            if (ApplicationEntryPoint.SCTCompatMode)
            {
                bannerPictureBox.Image = Properties.Resources.logo_sct_t;
            }
            else
            {
                bannerPictureBox.Image = Properties.Resources.logo_sctt;
            }

            _ = UXTheme.SetWindowTheme(Handle, " ", " ");

            LayoutTaskbarPreview();
        }

        private void SettingsTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            var aboutIndex = tabControl.TabPages.IndexOf(tabAbout);
            var systemTrayIndex = tabControl.TabPages.IndexOf(tabSystemTray);

            var aboutSelected = tabControl.SelectedIndex == aboutIndex;
            var systemTraySelected = tabControl.SelectedIndex == systemTrayIndex;

            if (!aboutSelected)
            {
                LayoutTaskbarPreview(systemTraySelected);
            }

            panelPreview.Visible = !aboutSelected;
        }

        private void StartButtonRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            customButtonTextBox.Enabled = customButtonBrowseButton.Enabled = customButtonRadioButton.Checked;
            customIconTextBox.Enabled = customIconBrowseButton.Enabled = customIconRadioButton.Checked;

            UpdateStartButton();
        }

        private void UpdateStartButton()
        {
            // var appearance = GetCurrentStartButtonAppearance();
            // startButton.DummySettings(customButtonTextBox.Text, customIconTextBox.Text, appearance);
        }

		private void enableDebuggingCheckBox_CheckedChanged(object sender, EventArgs e)
		{
            if (enableDebuggingCheckBox.Checked)
			{
                if (!tabControl.TabPages.Contains(tabDebug))
                    tabControl.TabPages.Add(tabDebug);
			}
            else
			{
                if (tabControl.TabPages.Contains(tabDebug))
                    tabControl.TabPages.Remove(tabDebug);
			}
		}
	}
}