namespace HIBPOfflineCheck
{
    partial class CreateBloomFilter
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
            this.textBoxOutput = new System.Windows.Forms.TextBox();
            this.buttonSelectOutput = new System.Windows.Forms.Button();
            this.buttonCreate = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.buttonSelectInput = new System.Windows.Forms.Button();
            this.textBoxInput = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.labelInfo = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBoxOutput
            // 
            this.textBoxOutput.Location = new System.Drawing.Point(9, 32);
            this.textBoxOutput.Name = "textBoxOutput";
            this.textBoxOutput.Size = new System.Drawing.Size(360, 20);
            this.textBoxOutput.TabIndex = 1;
            // 
            // buttonSelectOutput
            // 
            this.buttonSelectOutput.Location = new System.Drawing.Point(373, 30);
            this.buttonSelectOutput.Name = "buttonSelectOutput";
            this.buttonSelectOutput.Size = new System.Drawing.Size(70, 23);
            this.buttonSelectOutput.TabIndex = 2;
            this.buttonSelectOutput.Text = "Browse...";
            this.buttonSelectOutput.UseVisualStyleBackColor = true;
            this.buttonSelectOutput.Click += new System.EventHandler(this.buttonSelectOutput_Click);
            // 
            // buttonCreate
            // 
            this.buttonCreate.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.buttonCreate.Location = new System.Drawing.Point(152, 211);
            this.buttonCreate.Name = "buttonCreate";
            this.buttonCreate.Size = new System.Drawing.Size(75, 23);
            this.buttonCreate.TabIndex = 3;
            this.buttonCreate.Text = "Generate";
            this.buttonCreate.UseVisualStyleBackColor = true;
            this.buttonCreate.Click += new System.EventHandler(this.buttonCreate_Click);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 182);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(449, 23);
            this.progressBar.TabIndex = 4;
            this.progressBar.Visible = false;
            // 
            // buttonSelectInput
            // 
            this.buttonSelectInput.Location = new System.Drawing.Point(373, 30);
            this.buttonSelectInput.Name = "buttonSelectInput";
            this.buttonSelectInput.Size = new System.Drawing.Size(70, 23);
            this.buttonSelectInput.TabIndex = 5;
            this.buttonSelectInput.Text = "Browse...";
            this.buttonSelectInput.UseVisualStyleBackColor = true;
            this.buttonSelectInput.Click += new System.EventHandler(this.buttonSelectInput_Click);
            // 
            // textBoxInput
            // 
            this.textBoxInput.Location = new System.Drawing.Point(9, 32);
            this.textBoxInput.Name = "textBoxInput";
            this.textBoxInput.Size = new System.Drawing.Size(360, 20);
            this.textBoxInput.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(101, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "HIBP passwords file";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Bloom filter";
            // 
            // labelInfo
            // 
            this.labelInfo.AutoSize = true;
            this.labelInfo.Location = new System.Drawing.Point(9, 166);
            this.labelInfo.Name = "labelInfo";
            this.labelInfo.Size = new System.Drawing.Size(279, 13);
            this.labelInfo.TabIndex = 9;
            this.labelInfo.Text = "Generating the filter may take several minutes to complete, depending on your hardware.";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textBoxInput);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.buttonSelectInput);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(449, 68);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Input";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.buttonSelectOutput);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.textBoxOutput);
            this.groupBox2.Location = new System.Drawing.Point(12, 86);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(449, 67);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Output";
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.buttonCancel.Location = new System.Drawing.Point(233, 211);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 12;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // CreateBloomFilter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(473, 246);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.labelInfo);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.buttonCreate);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "CreateBloomFilter";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Create Bloom Filter";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.CreateBloomFilter_FormClosed);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox textBoxOutput;
        private System.Windows.Forms.Button buttonSelectOutput;
        private System.Windows.Forms.Button buttonCreate;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button buttonSelectInput;
        private System.Windows.Forms.TextBox textBoxInput;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label labelInfo;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button buttonCancel;
    }
}