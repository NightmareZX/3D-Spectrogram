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
            SuspendLayout();
            // 
            // GlControl_MainView
            // 
            GlControl_MainView.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            GlControl_MainView.APIVersion = new Version(3, 3, 0, 0);
            GlControl_MainView.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            GlControl_MainView.IsEventDriven = true;
            GlControl_MainView.Location = new Point(12, 32);
            GlControl_MainView.Name = "GlControl_MainView";
            GlControl_MainView.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            GlControl_MainView.SharedContext = null;
            GlControl_MainView.Size = new Size(1458, 593);
            GlControl_MainView.TabIndex = 0;
            GlControl_MainView.Load += GlControl_MainView_Load;
            GlControl_MainView.Paint += GlControl_MainView_Paint;
            // 
            // Label_Frames
            // 
            Label_Frames.AutoSize = true;
            Label_Frames.Location = new Point(12, 9);
            Label_Frames.Name = "Label_Frames";
            Label_Frames.Size = new Size(13, 15);
            Label_Frames.TabIndex = 1;
            Label_Frames.Text = "0";
            // 
            // Timer_UpdateGL
            // 
            Timer_UpdateGL.Enabled = true;
            Timer_UpdateGL.Interval = 20;
            Timer_UpdateGL.Tick += Timer_UpdateGL_Tick;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1482, 648);
            Controls.Add(Label_Frames);
            Controls.Add(GlControl_MainView);
            Name = "MainForm";
            Text = "MainView";
            FormClosing += MainForm_FormClosing;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private OpenTK.GLControl.GLControl GlControl_MainView;
        private Label Label_Frames;
        private System.Windows.Forms.Timer Timer_UpdateGL;
    }
}
