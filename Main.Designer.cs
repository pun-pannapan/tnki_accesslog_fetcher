namespace tnki_accesslog_fetcher
{
    partial class Main
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            btn_pullnow = new Button();
            button1 = new Button();
            LogListBox = new ListBox();
            SuspendLayout();
            // 
            // btn_pullnow
            // 
            btn_pullnow.Location = new Point(53, 38);
            btn_pullnow.Name = "btn_pullnow";
            btn_pullnow.Size = new Size(133, 34);
            btn_pullnow.TabIndex = 0;
            btn_pullnow.Text = "FetchDataNow";
            btn_pullnow.UseVisualStyleBackColor = true;
            btn_pullnow.Click += btn_fetchnow_Click;
            // 
            // button1
            // 
            button1.Location = new Point(216, 38);
            button1.Name = "button1";
            button1.Size = new Size(133, 34);
            button1.TabIndex = 1;
            button1.Text = "SchedulePull";
            button1.UseVisualStyleBackColor = true;
            button1.Click += btn_schedulepull_Click;
            // 
            // LogListBox
            // 
            LogListBox.FormattingEnabled = true;
            LogListBox.ItemHeight = 25;
            LogListBox.Location = new Point(12, 90);
            LogListBox.Name = "LogListBox";
            LogListBox.Size = new Size(387, 154);
            LogListBox.TabIndex = 2;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(411, 253);
            Controls.Add(LogListBox);
            Controls.Add(button1);
            Controls.Add(btn_pullnow);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Main";
            Text = "HRRangerFetcher";
            ResumeLayout(false);
        }

        #endregion

        private Button btn_pullnow;
        private Button button1;
        private ListBox LogListBox;
    }
}
