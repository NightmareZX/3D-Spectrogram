namespace WinForms_TestApp
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            GlControl_MainView = new OpenTK.GLControl.GLControl();
            Label_Frames = new Label();
            Timer_UpdateGL = new System.Windows.Forms.Timer(components);
            WebView_AudioProc = new Microsoft.Web.WebView2.WinForms.WebView2();
            MenuStrip_Main = new MenuStrip();
            reloadWebPageToolStripMenuItem = new ToolStripMenuItem();
            openInspectElementToolStripMenuItem = new ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)WebView_AudioProc).BeginInit();
            MenuStrip_Main.SuspendLayout();
            SuspendLayout();
            // 
            // GlControl_MainView
            // 
            GlControl_MainView.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            GlControl_MainView.APIVersion = new Version(3, 3, 0, 0);
            GlControl_MainView.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            GlControl_MainView.IsEventDriven = true;
            GlControl_MainView.Location = new Point(12, 51);
            GlControl_MainView.Name = "GlControl_MainView";
            GlControl_MainView.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            GlControl_MainView.SharedContext = null;
            GlControl_MainView.Size = new Size(1458, 619);
            GlControl_MainView.TabIndex = 0;
            // 
            // Label_Frames
            // 
            Label_Frames.AutoSize = true;
            Label_Frames.Location = new Point(12, 33);
            Label_Frames.Name = "Label_Frames";
            Label_Frames.Size = new Size(13, 15);
            Label_Frames.TabIndex = 1;
            Label_Frames.Text = "0";
            // 
            // Timer_UpdateGL
            // 
            Timer_UpdateGL.Interval = 1;
            Timer_UpdateGL.Tick += Timer_UpdateGL_Tick;
            // 
            // WebView_AudioProc
            // 
            WebView_AudioProc.AllowExternalDrop = true;
            WebView_AudioProc.CreationProperties = null;
            WebView_AudioProc.DefaultBackgroundColor = Color.White;
            WebView_AudioProc.Location = new Point(1368, 26);
            WebView_AudioProc.Name = "WebView_AudioProc";
            WebView_AudioProc.Size = new Size(102, 23);
            WebView_AudioProc.TabIndex = 2;
            WebView_AudioProc.ZoomFactor = 1D;
            // 
            // MenuStrip_Main
            // 
            MenuStrip_Main.Items.AddRange(new ToolStripItem[] { reloadWebPageToolStripMenuItem, openInspectElementToolStripMenuItem });
            MenuStrip_Main.Location = new Point(0, 0);
            MenuStrip_Main.Name = "MenuStrip_Main";
            MenuStrip_Main.Size = new Size(1482, 24);
            MenuStrip_Main.TabIndex = 3;
            MenuStrip_Main.Text = "menuStrip1";
            // 
            // reloadWebPageToolStripMenuItem
            // 
            reloadWebPageToolStripMenuItem.Name = "reloadWebPageToolStripMenuItem";
            reloadWebPageToolStripMenuItem.Size = new Size(108, 20);
            reloadWebPageToolStripMenuItem.Text = "Reload WebPage";
            reloadWebPageToolStripMenuItem.Click += reloadWebPageToolStripMenuItem_Click;
            // 
            // openInspectElementToolStripMenuItem
            // 
            openInspectElementToolStripMenuItem.Name = "openInspectElementToolStripMenuItem";
            openInspectElementToolStripMenuItem.Size = new Size(135, 20);
            openInspectElementToolStripMenuItem.Text = "Open Inspect Element";
            openInspectElementToolStripMenuItem.Click += openInspectElementToolStripMenuItem_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1482, 677);
            Controls.Add(WebView_AudioProc);
            Controls.Add(Label_Frames);
            Controls.Add(GlControl_MainView);
            Controls.Add(MenuStrip_Main);
            MainMenuStrip = MenuStrip_Main;
            Name = "MainForm";
            Text = "MainView";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            ((System.ComponentModel.ISupportInitialize)WebView_AudioProc).EndInit();
            MenuStrip_Main.ResumeLayout(false);
            MenuStrip_Main.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private OpenTK.GLControl.GLControl GlControl_MainView;
        private Label Label_Frames;
        private System.Windows.Forms.Timer Timer_UpdateGL;
        private Microsoft.Web.WebView2.WinForms.WebView2 WebView_AudioProc;
        private MenuStrip MenuStrip_Main;
        private ToolStripMenuItem reloadWebPageToolStripMenuItem;
        private ToolStripMenuItem openInspectElementToolStripMenuItem;
    }
}
