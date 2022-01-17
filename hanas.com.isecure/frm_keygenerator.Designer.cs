namespace hanas.com.isecure
{
    partial class frm_keygenerator
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
            this.btnClose = new System.Windows.Forms.Button();
            this.btnEncrypt = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txtDecryptedKey = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtEncryptedKey = new System.Windows.Forms.TextBox();
            this.lstMessage = new System.Windows.Forms.ListBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.label3 = new System.Windows.Forms.Label();
            this.txtData = new System.Windows.Forms.TextBox();
            this.btnDecrypt = new System.Windows.Forms.Button();
            this.btnRemoveDQuote = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.txtRemovedQuote = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(387, 217);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(116, 24);
            this.btnClose.TabIndex = 31;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnEncrypt
            // 
            this.btnEncrypt.Location = new System.Drawing.Point(387, 7);
            this.btnEncrypt.Name = "btnEncrypt";
            this.btnEncrypt.Size = new System.Drawing.Size(116, 24);
            this.btnEncrypt.TabIndex = 29;
            this.btnEncrypt.Text = "Encrypt!!";
            this.btnEncrypt.UseVisualStyleBackColor = true;
            this.btnEncrypt.Click += new System.EventHandler(this.btnEncrypt_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 69);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 13);
            this.label2.TabIndex = 28;
            this.label2.Text = "Decrypted Key";
            // 
            // txtDecryptedKey
            // 
            this.txtDecryptedKey.Location = new System.Drawing.Point(94, 65);
            this.txtDecryptedKey.Name = "txtDecryptedKey";
            this.txtDecryptedKey.Size = new System.Drawing.Size(283, 20);
            this.txtDecryptedKey.TabIndex = 27;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 13);
            this.label1.TabIndex = 26;
            this.label1.Text = "Encripted Key";
            // 
            // txtEncryptedKey
            // 
            this.txtEncryptedKey.Location = new System.Drawing.Point(94, 38);
            this.txtEncryptedKey.Name = "txtEncryptedKey";
            this.txtEncryptedKey.Size = new System.Drawing.Size(283, 20);
            this.txtEncryptedKey.TabIndex = 25;
            // 
            // lstMessage
            // 
            this.lstMessage.FormattingEnabled = true;
            this.lstMessage.Location = new System.Drawing.Point(11, 131);
            this.lstMessage.Name = "lstMessage";
            this.lstMessage.Size = new System.Drawing.Size(366, 108);
            this.lstMessage.TabIndex = 24;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(11, 117);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(366, 10);
            this.progressBar1.TabIndex = 23;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 13);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(30, 13);
            this.label3.TabIndex = 33;
            this.label3.Text = "Data";
            // 
            // txtData
            // 
            this.txtData.Location = new System.Drawing.Point(94, 9);
            this.txtData.Name = "txtData";
            this.txtData.Size = new System.Drawing.Size(283, 20);
            this.txtData.TabIndex = 32;
            // 
            // btnDecrypt
            // 
            this.btnDecrypt.Location = new System.Drawing.Point(387, 34);
            this.btnDecrypt.Name = "btnDecrypt";
            this.btnDecrypt.Size = new System.Drawing.Size(116, 24);
            this.btnDecrypt.TabIndex = 34;
            this.btnDecrypt.Text = "Decrypt!!";
            this.btnDecrypt.UseVisualStyleBackColor = true;
            this.btnDecrypt.Click += new System.EventHandler(this.btnDecrypt_Click);
            // 
            // btnRemoveDQuote
            // 
            this.btnRemoveDQuote.Location = new System.Drawing.Point(387, 62);
            this.btnRemoveDQuote.Name = "btnRemoveDQuote";
            this.btnRemoveDQuote.Size = new System.Drawing.Size(116, 24);
            this.btnRemoveDQuote.TabIndex = 35;
            this.btnRemoveDQuote.Text = "Remove D Quote";
            this.btnRemoveDQuote.UseVisualStyleBackColor = true;
            this.btnRemoveDQuote.Click += new System.EventHandler(this.btnRemoveDQuote_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 96);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(85, 13);
            this.label4.TabIndex = 37;
            this.label4.Text = "Quote Removed";
            // 
            // txtRemovedQuote
            // 
            this.txtRemovedQuote.Location = new System.Drawing.Point(94, 92);
            this.txtRemovedQuote.Name = "txtRemovedQuote";
            this.txtRemovedQuote.Size = new System.Drawing.Size(283, 20);
            this.txtRemovedQuote.TabIndex = 36;
            // 
            // frm_keygenerator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(509, 243);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtRemovedQuote);
            this.Controls.Add(this.btnRemoveDQuote);
            this.Controls.Add(this.btnDecrypt);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtData);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnEncrypt);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtDecryptedKey);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtEncryptedKey);
            this.Controls.Add(this.lstMessage);
            this.Controls.Add(this.progressBar1);
            this.Name = "frm_keygenerator";
            this.Text = "Key Generator";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnEncrypt;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtDecryptedKey;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtEncryptedKey;
        private System.Windows.Forms.ListBox lstMessage;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.ComponentModel.BackgroundWorker backgroundWorker;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtData;
        private System.Windows.Forms.Button btnDecrypt;
        private System.Windows.Forms.Button btnRemoveDQuote;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtRemovedQuote;
    }
}