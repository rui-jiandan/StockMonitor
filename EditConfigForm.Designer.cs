namespace StockMonitor
{
    partial class EditConfigForm
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
            this.lblShowFormat = new System.Windows.Forms.Label();
            this.txtShowFormat = new System.Windows.Forms.TextBox();
            this.lblRefreshTime = new System.Windows.Forms.Label();
            this.txtRefreshTime = new System.Windows.Forms.TextBox();
            this.lblShowTodaySumFormat = new System.Windows.Forms.Label();
            this.txtShowTodaySumFormat = new System.Windows.Forms.TextBox();
            this.lblOrderBy = new System.Windows.Forms.Label();
            this.txtOrderBy = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // 
            // lblShowFormat
            // 
            this.lblShowFormat.AutoSize = true;
            this.lblShowFormat.Location = new System.Drawing.Point(20, 20);
            this.lblShowFormat.Name = "lblShowFormat";
            this.lblShowFormat.Size = new System.Drawing.Size(80, 15);
            this.lblShowFormat.TabIndex = 0;
            this.lblShowFormat.Text = "显示格式：";
            // 
            // txtShowFormat
            // 
            this.txtShowFormat.Location = new System.Drawing.Point(150, 20);
            this.txtShowFormat.Name = "txtShowFormat";
            this.txtShowFormat.Size = new System.Drawing.Size(400, 23);
            this.txtShowFormat.TabIndex = 1;
            // 
            // lblRefreshTime
            // 
            this.lblRefreshTime.AutoSize = true;
            this.lblRefreshTime.Location = new System.Drawing.Point(20, 60);
            this.lblRefreshTime.Name = "lblRefreshTime";
            this.lblRefreshTime.Size = new System.Drawing.Size(80, 15);
            this.lblRefreshTime.TabIndex = 2;
            this.lblRefreshTime.Text = "刷新时间(ms)：";
            // 
            // txtRefreshTime
            // 
            this.txtRefreshTime.Location = new System.Drawing.Point(150, 60);
            this.txtRefreshTime.Name = "txtRefreshTime";
            this.txtRefreshTime.Size = new System.Drawing.Size(400, 23);
            this.txtRefreshTime.TabIndex = 3;
            // 
            // lblShowTodaySumFormat
            // 
            this.lblShowTodaySumFormat.AutoSize = true;
            this.lblShowTodaySumFormat.Location = new System.Drawing.Point(20, 100);
            this.lblShowTodaySumFormat.Name = "lblShowTodaySumFormat";
            this.lblShowTodaySumFormat.Size = new System.Drawing.Size(120, 15);
            this.lblShowTodaySumFormat.TabIndex = 4;
            this.lblShowTodaySumFormat.Text = "今日汇总显示格式：";
            // 
            // txtShowTodaySumFormat
            // 
            this.txtShowTodaySumFormat.Location = new System.Drawing.Point(150, 100);
            this.txtShowTodaySumFormat.Name = "txtShowTodaySumFormat";
            this.txtShowTodaySumFormat.Size = new System.Drawing.Size(400, 23);
            this.txtShowTodaySumFormat.TabIndex = 5;
            // 
            // lblOrderBy
            // 
            this.lblOrderBy.AutoSize = true;
            this.lblOrderBy.Location = new System.Drawing.Point(20, 140);
            this.lblOrderBy.Name = "lblOrderBy";
            this.lblOrderBy.Size = new System.Drawing.Size(80, 15);
            this.lblOrderBy.TabIndex = 6;
            this.lblOrderBy.Text = "排序规则：";
            // 
            // txtOrderBy
            // 
            this.txtOrderBy.Location = new System.Drawing.Point(150, 140);
            this.txtOrderBy.Name = "txtOrderBy";
            this.txtOrderBy.Size = new System.Drawing.Size(400, 23);
            this.txtOrderBy.TabIndex = 7;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(150, 200);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 30);
            this.btnSave.TabIndex = 8;
            this.btnSave.Text = "保存";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(270, 200);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 30);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // EditConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 250);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.txtOrderBy);
            this.Controls.Add(this.lblOrderBy);
            this.Controls.Add(this.txtShowTodaySumFormat);
            this.Controls.Add(this.lblShowTodaySumFormat);
            this.Controls.Add(this.txtRefreshTime);
            this.Controls.Add(this.lblRefreshTime);
            this.Controls.Add(this.txtShowFormat);
            this.Controls.Add(this.lblShowFormat);
            this.Name = "EditConfigForm";
            this.Text = "编辑配置";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblShowFormat;
        private System.Windows.Forms.TextBox txtShowFormat;
        private System.Windows.Forms.Label lblRefreshTime;
        private System.Windows.Forms.TextBox txtRefreshTime;
        private System.Windows.Forms.Label lblShowTodaySumFormat;
        private System.Windows.Forms.TextBox txtShowTodaySumFormat;
        private System.Windows.Forms.Label lblOrderBy;
        private System.Windows.Forms.TextBox txtOrderBy;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        #endregion
    }
}