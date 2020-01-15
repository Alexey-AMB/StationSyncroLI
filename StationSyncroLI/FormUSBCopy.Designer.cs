namespace StationSyncroCE
{
    partial class FormUSBCopy
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
            this.labelTop = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.labelFileName = new System.Windows.Forms.Label();
            this.progressBarAll = new System.Windows.Forms.ProgressBar();
            this.progressBarCurr = new System.Windows.Forms.ProgressBar();
            this.timer3 = new System.Windows.Forms.Timer();
            this.labelEnd = new System.Windows.Forms.Label();
            this.timer2 = new System.Windows.Forms.Timer();
            this.SuspendLayout();
            // 
            // labelTop
            // 
            this.labelTop.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.labelTop.Location = new System.Drawing.Point(13, 13);
            this.labelTop.Name = "labelTop";
            this.labelTop.Size = new System.Drawing.Size(283, 20);
            this.labelTop.Text = "Идет копирование файлов. Ждите.";
            this.labelTop.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(17, 57);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(105, 20);
            this.label2.Text = "Копирую файл:";
            // 
            // labelFileName
            // 
            this.labelFileName.Location = new System.Drawing.Point(128, 57);
            this.labelFileName.Name = "labelFileName";
            this.labelFileName.Size = new System.Drawing.Size(187, 20);
            this.labelFileName.Text = "......";
            // 
            // progressBarAll
            // 
            this.progressBarAll.Location = new System.Drawing.Point(17, 91);
            this.progressBarAll.Name = "progressBarAll";
            this.progressBarAll.Size = new System.Drawing.Size(283, 20);
            // 
            // progressBarCurr
            // 
            this.progressBarCurr.Location = new System.Drawing.Point(17, 130);
            this.progressBarCurr.Name = "progressBarCurr";
            this.progressBarCurr.Size = new System.Drawing.Size(283, 20);
            // 
            // timer3
            // 
            this.timer3.Interval = 5000;
            this.timer3.Tick += new System.EventHandler(this.timer3_Tick);
            // 
            // labelEnd
            // 
            this.labelEnd.Font = new System.Drawing.Font("Tahoma", 14F, System.Drawing.FontStyle.Bold);
            this.labelEnd.ForeColor = System.Drawing.Color.Lime;
            this.labelEnd.Location = new System.Drawing.Point(17, 176);
            this.labelEnd.Name = "labelEnd";
            this.labelEnd.Size = new System.Drawing.Size(283, 27);
            this.labelEnd.Text = "Копирование завершено";
            this.labelEnd.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.labelEnd.Visible = false;
            // 
            // timer2
            // 
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // FormUSBCopy
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(318, 215);
            this.ControlBox = false;
            this.Controls.Add(this.labelEnd);
            this.Controls.Add(this.progressBarCurr);
            this.Controls.Add(this.progressBarAll);
            this.Controls.Add(this.labelFileName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.labelTop);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormUSBCopy";
            this.Text = "FormUSBCopy";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.FormUSBCopy_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelTop;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelFileName;
        private System.Windows.Forms.ProgressBar progressBarAll;
        private System.Windows.Forms.ProgressBar progressBarCurr;
        private System.Windows.Forms.Timer timer3;
        private System.Windows.Forms.Label labelEnd;
        private System.Windows.Forms.Timer timer2;
    }
}