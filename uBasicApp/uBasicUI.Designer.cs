using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace uBasicApp
{
    partial class uBasicUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(uBasicUI));
            consoleMenuStrip = new MenuStrip();
            fileMenuItem = new ToolStripMenuItem();
            openFileMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            recentFileToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            actionToolStripMenuItem = new ToolStripMenuItem();
            startToolStripMenuItem = new ToolStripMenuItem();
            pauseToolStripMenuItem = new ToolStripMenuItem();
            resumeToolStripMenuItem = new ToolStripMenuItem();
            stopToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            saveToolStripMenuItem = new ToolStripMenuItem();
            loadToolStripMenuItem = new ToolStripMenuItem();
            formatToolStripMenuItem = new ToolStripMenuItem();
            fontToolStripMenuItem = new ToolStripMenuItem();
            colorToolStripMenuItem = new ToolStripMenuItem();
            consolePictureBox = new PictureBox();
            consoleMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)consolePictureBox).BeginInit();
            SuspendLayout();
            // 
            // consoleMenuStrip
            // 
            consoleMenuStrip.ImageScalingSize = new Size(32, 32);
            consoleMenuStrip.Items.AddRange(new ToolStripItem[] { fileMenuItem, actionToolStripMenuItem, formatToolStripMenuItem });
            consoleMenuStrip.Location = new Point(0, 0);
            consoleMenuStrip.Name = "consoleMenuStrip";
            consoleMenuStrip.Padding = new Padding(7, 3, 0, 3);
            consoleMenuStrip.Size = new Size(866, 44);
            consoleMenuStrip.TabIndex = 1;
            consoleMenuStrip.Text = "menuStrip1";
            // 
            // fileMenuItem
            // 
            fileMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openFileMenuItem, toolStripSeparator1, recentFileToolStripMenuItem, toolStripSeparator2, exitToolStripMenuItem });
            fileMenuItem.Name = "fileMenuItem";
            fileMenuItem.Size = new Size(71, 38);
            fileMenuItem.Text = "File";
            // 
            // openFileMenuItem
            // 
            openFileMenuItem.Name = "openFileMenuItem";
            openFileMenuItem.Size = new Size(359, 44);
            openFileMenuItem.Text = "&Open";
            openFileMenuItem.Click += FileOpenMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(356, 6);
            // 
            // recentFileToolStripMenuItem
            // 
            recentFileToolStripMenuItem.Name = "recentFileToolStripMenuItem";
            recentFileToolStripMenuItem.Size = new Size(359, 44);
            recentFileToolStripMenuItem.Text = "Recent File";
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(356, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(359, 44);
            exitToolStripMenuItem.Text = "&Exit";
            exitToolStripMenuItem.Click += FileExitMenuItem_Click;
            // 
            // actionToolStripMenuItem
            // 
            actionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { startToolStripMenuItem, pauseToolStripMenuItem, resumeToolStripMenuItem, stopToolStripMenuItem, toolStripSeparator3, saveToolStripMenuItem, loadToolStripMenuItem });
            actionToolStripMenuItem.Name = "actionToolStripMenuItem";
            actionToolStripMenuItem.Size = new Size(102, 38);
            actionToolStripMenuItem.Text = "Action";
            // 
            // startToolStripMenuItem
            // 
            startToolStripMenuItem.Name = "startToolStripMenuItem";
            startToolStripMenuItem.Size = new Size(231, 44);
            startToolStripMenuItem.Text = "Start";
            startToolStripMenuItem.Click += ActionStartMenuItem_Click;
            // 
            // pauseToolStripMenuItem
            // 
            pauseToolStripMenuItem.Name = "pauseToolStripMenuItem";
            pauseToolStripMenuItem.Size = new Size(231, 44);
            pauseToolStripMenuItem.Text = "Pause";
            pauseToolStripMenuItem.Click += pauseToolStripMenuItem_Click;
            // 
            // resumeToolStripMenuItem
            // 
            resumeToolStripMenuItem.Name = "resumeToolStripMenuItem";
            resumeToolStripMenuItem.Size = new Size(231, 44);
            resumeToolStripMenuItem.Text = "Resume";
            resumeToolStripMenuItem.Click += resumeToolStripMenuItem_Click;
            // 
            // stopToolStripMenuItem
            // 
            stopToolStripMenuItem.Name = "stopToolStripMenuItem";
            stopToolStripMenuItem.Size = new Size(231, 44);
            stopToolStripMenuItem.Text = "Stop";
            stopToolStripMenuItem.Click += ActionStopMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(228, 6);
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(231, 44);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // loadToolStripMenuItem
            // 
            loadToolStripMenuItem.Name = "loadToolStripMenuItem";
            loadToolStripMenuItem.Size = new Size(231, 44);
            loadToolStripMenuItem.Text = "Load";
            loadToolStripMenuItem.Click += loadToolStripMenuItem_Click;
            // 
            // formatToolStripMenuItem
            // 
            formatToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { fontToolStripMenuItem, colorToolStripMenuItem });
            formatToolStripMenuItem.Name = "formatToolStripMenuItem";
            formatToolStripMenuItem.Size = new Size(109, 38);
            formatToolStripMenuItem.Text = "Format";
            // 
            // fontToolStripMenuItem
            // 
            fontToolStripMenuItem.Name = "fontToolStripMenuItem";
            fontToolStripMenuItem.Size = new Size(204, 44);
            fontToolStripMenuItem.Text = "Font";
            fontToolStripMenuItem.Click += FormatFontMenuItem_Click;
            // 
            // colorToolStripMenuItem
            // 
            colorToolStripMenuItem.Name = "colorToolStripMenuItem";
            colorToolStripMenuItem.Size = new Size(204, 44);
            colorToolStripMenuItem.Text = "Color";
            colorToolStripMenuItem.Click += FormatColorMenuItem_Click;
            // 
            // consolePictureBox
            // 
            consolePictureBox.BackgroundImageLayout = ImageLayout.None;
            consolePictureBox.Dock = DockStyle.Fill;
            consolePictureBox.Location = new Point(0, 44);
            consolePictureBox.Name = "consolePictureBox";
            consolePictureBox.Size = new Size(866, 683);
            consolePictureBox.TabIndex = 2;
            consolePictureBox.TabStop = false;
            consolePictureBox.Click += ConsolePiciureBox_Click;
            consolePictureBox.Paint += ConsolePictureBox_Paint;
            consolePictureBox.PreviewKeyDown += ConsolePictureBox_PreviewKeyDown;
            // 
            // uBasicUI
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(866, 727);
            Controls.Add(consolePictureBox);
            Controls.Add(consoleMenuStrip);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = consoleMenuStrip;
            Margin = new Padding(4);
            Name = "uBasicUI";
            Text = "uBasic";
            FormClosing += ConsoleForm_FormClosing;
            Load += ConsoleForm_Load;
            consoleMenuStrip.ResumeLayout(false);
            consoleMenuStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)consolePictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip consoleMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openFileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem formatToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fontToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem colorToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem recentFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private PictureBox consolePictureBox;
        private ToolStripMenuItem actionToolStripMenuItem;
        private ToolStripMenuItem startToolStripMenuItem;
        private ToolStripMenuItem stopToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem loadToolStripMenuItem;
        private ToolStripMenuItem pauseToolStripMenuItem;
        private ToolStripMenuItem resumeToolStripMenuItem;
    }
}

