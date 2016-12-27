namespace ProcessKiller
{
    partial class SettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.SettingsGroupBox = new System.Windows.Forms.GroupBox();
            this.SettingsTable = new System.Windows.Forms.TableLayoutPanel();
            this.CountDownKeyTextBox = new System.Windows.Forms.TextBox();
            this.ProcessKillerButtonLabel = new System.Windows.Forms.Label();
            this.ProcessKillerKeyTextbox = new System.Windows.Forms.TextBox();
            this.CountDownButtonLabel = new System.Windows.Forms.Label();
            this.ApplyButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            this.SettingsGroupBox.SuspendLayout();
            this.SettingsTable.SuspendLayout();
            this.SuspendLayout();
            // 
            // SettingsGroupBox
            // 
            this.SettingsGroupBox.Controls.Add(this.SettingsTable);
            this.SettingsGroupBox.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.SettingsGroupBox.Location = new System.Drawing.Point(12, 12);
            this.SettingsGroupBox.Name = "SettingsGroupBox";
            this.SettingsGroupBox.Size = new System.Drawing.Size(258, 141);
            this.SettingsGroupBox.TabIndex = 0;
            this.SettingsGroupBox.TabStop = false;
            this.SettingsGroupBox.Text = "按键设置";
            // 
            // SettingsTable
            // 
            this.SettingsTable.ColumnCount = 2;
            this.SettingsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55.10204F));
            this.SettingsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 44.89796F));
            this.SettingsTable.Controls.Add(this.CountDownKeyTextBox, 1, 1);
            this.SettingsTable.Controls.Add(this.ProcessKillerButtonLabel, 0, 0);
            this.SettingsTable.Controls.Add(this.ProcessKillerKeyTextbox, 1, 0);
            this.SettingsTable.Controls.Add(this.CountDownButtonLabel, 0, 1);
            this.SettingsTable.Location = new System.Drawing.Point(7, 22);
            this.SettingsTable.Name = "SettingsTable";
            this.SettingsTable.RowCount = 2;
            this.SettingsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.SettingsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.SettingsTable.Size = new System.Drawing.Size(245, 108);
            this.SettingsTable.TabIndex = 0;
            // 
            // CountDownKeyTextBox
            // 
            this.CountDownKeyTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.CountDownKeyTextBox.Font = new System.Drawing.Font("Microsoft YaHei", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CountDownKeyTextBox.Location = new System.Drawing.Point(137, 68);
            this.CountDownKeyTextBox.Name = "CountDownKeyTextBox";
            this.CountDownKeyTextBox.ReadOnly = true;
            this.CountDownKeyTextBox.Size = new System.Drawing.Size(80, 25);
            this.CountDownKeyTextBox.TabIndex = 3;
            this.CountDownKeyTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // ProcessKillerButtonLabel
            // 
            this.ProcessKillerButtonLabel.AutoSize = true;
            this.ProcessKillerButtonLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ProcessKillerButtonLabel.Location = new System.Drawing.Point(3, 0);
            this.ProcessKillerButtonLabel.Name = "ProcessKillerButtonLabel";
            this.ProcessKillerButtonLabel.Size = new System.Drawing.Size(128, 54);
            this.ProcessKillerButtonLabel.TabIndex = 0;
            this.ProcessKillerButtonLabel.Text = "掉线快捷键:";
            this.ProcessKillerButtonLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ProcessKillerKeyTextbox
            // 
            this.ProcessKillerKeyTextbox.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.ProcessKillerKeyTextbox.Font = new System.Drawing.Font("Microsoft YaHei", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ProcessKillerKeyTextbox.Location = new System.Drawing.Point(137, 14);
            this.ProcessKillerKeyTextbox.Name = "ProcessKillerKeyTextbox";
            this.ProcessKillerKeyTextbox.ReadOnly = true;
            this.ProcessKillerKeyTextbox.Size = new System.Drawing.Size(80, 25);
            this.ProcessKillerKeyTextbox.TabIndex = 1;
            this.ProcessKillerKeyTextbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // CountDownButtonLabel
            // 
            this.CountDownButtonLabel.AutoSize = true;
            this.CountDownButtonLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CountDownButtonLabel.Location = new System.Drawing.Point(3, 54);
            this.CountDownButtonLabel.Name = "CountDownButtonLabel";
            this.CountDownButtonLabel.Size = new System.Drawing.Size(128, 54);
            this.CountDownButtonLabel.TabIndex = 2;
            this.CountDownButtonLabel.Text = "倒计时快捷键:";
            this.CountDownButtonLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ApplyButton
            // 
            this.ApplyButton.Location = new System.Drawing.Point(26, 170);
            this.ApplyButton.Name = "ApplyButton";
            this.ApplyButton.Size = new System.Drawing.Size(100, 35);
            this.ApplyButton.TabIndex = 1;
            this.ApplyButton.Text = "应用";
            this.ApplyButton.UseVisualStyleBackColor = true;
            this.ApplyButton.Click += new System.EventHandler(this.ApplyButton_Click);
            // 
            // CancelButton
            // 
            this.CancelButton.Location = new System.Drawing.Point(158, 170);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(100, 35);
            this.CancelButton.TabIndex = 2;
            this.CancelButton.Text = "取消";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(282, 217);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.ApplyButton);
            this.Controls.Add(this.SettingsGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "设置";
            this.SettingsGroupBox.ResumeLayout(false);
            this.SettingsTable.ResumeLayout(false);
            this.SettingsTable.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox SettingsGroupBox;
        private System.Windows.Forms.TableLayoutPanel SettingsTable;
        private System.Windows.Forms.Label ProcessKillerButtonLabel;
        private System.Windows.Forms.TextBox ProcessKillerKeyTextbox;
        private System.Windows.Forms.Button ApplyButton;
        private System.Windows.Forms.Button CancelButton;
        private System.Windows.Forms.Label CountDownButtonLabel;
        private System.Windows.Forms.TextBox CountDownKeyTextBox;
    }
}