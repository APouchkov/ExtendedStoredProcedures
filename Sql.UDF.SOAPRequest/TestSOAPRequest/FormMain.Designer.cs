namespace TestSOAPRequest
{
    partial class FormMain
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
            this.buttonSend = new System.Windows.Forms.Button();
            this.textBoxUri = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.richTextBoxBody = new System.Windows.Forms.RichTextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.richTextBoxResponseBody = new System.Windows.Forms.RichTextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.richTextBoxHeader = new System.Windows.Forms.RichTextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.richTextBoxResponseHeader = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // buttonSend
            // 
            this.buttonSend.Location = new System.Drawing.Point(281, 12);
            this.buttonSend.Name = "buttonSend";
            this.buttonSend.Size = new System.Drawing.Size(75, 23);
            this.buttonSend.TabIndex = 0;
            this.buttonSend.Text = "Send";
            this.buttonSend.UseVisualStyleBackColor = true;
            this.buttonSend.Click += new System.EventHandler(this.buttonSend_Click);
            // 
            // textBoxUri
            // 
            this.textBoxUri.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxUri.Location = new System.Drawing.Point(12, 41);
            this.textBoxUri.Name = "textBoxUri";
            this.textBoxUri.Size = new System.Drawing.Size(615, 20);
            this.textBoxUri.TabIndex = 1;
            this.textBoxUri.Text = "http://will/WS_SMS";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(26, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "URI";
            // 
            // richTextBoxBody
            // 
            this.richTextBoxBody.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxBody.Location = new System.Drawing.Point(12, 170);
            this.richTextBoxBody.Name = "richTextBoxBody";
            this.richTextBoxBody.Size = new System.Drawing.Size(615, 58);
            this.richTextBoxBody.TabIndex = 3;
            this.richTextBoxBody.Text = "<SendSMS><SenderCode>1</SenderCode><retries>2</retries><SMS><retries>2</retries><" +
                "phones><phone>79851962045</phone></phones><message>Привет Hello !</message><rus>" +
                "1</rus></SMS></SendSMS>";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 154);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(31, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Body";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 341);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(82, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Response Body";
            // 
            // richTextBoxResponseBody
            // 
            this.richTextBoxResponseBody.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxResponseBody.Location = new System.Drawing.Point(12, 357);
            this.richTextBoxResponseBody.Name = "richTextBoxResponseBody";
            this.richTextBoxResponseBody.Size = new System.Drawing.Size(615, 122);
            this.richTextBoxResponseBody.TabIndex = 5;
            this.richTextBoxResponseBody.Text = "";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 69);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(42, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Header";
            // 
            // richTextBoxHeader
            // 
            this.richTextBoxHeader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxHeader.Location = new System.Drawing.Point(12, 85);
            this.richTextBoxHeader.Name = "richTextBoxHeader";
            this.richTextBoxHeader.Size = new System.Drawing.Size(615, 58);
            this.richTextBoxHeader.TabIndex = 2;
            this.richTextBoxHeader.Text = "";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 249);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(93, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Response Header";
            // 
            // richTextBoxResponseHeader
            // 
            this.richTextBoxResponseHeader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxResponseHeader.Location = new System.Drawing.Point(12, 265);
            this.richTextBoxResponseHeader.Name = "richTextBoxResponseHeader";
            this.richTextBoxResponseHeader.Size = new System.Drawing.Size(615, 62);
            this.richTextBoxResponseHeader.TabIndex = 4;
            this.richTextBoxResponseHeader.Text = "";
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(646, 491);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.richTextBoxResponseHeader);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.richTextBoxHeader);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.richTextBoxResponseBody);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.richTextBoxBody);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxUri);
            this.Controls.Add(this.buttonSend);
            this.Name = "FormMain";
            this.Text = "Test SOAP Request";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonSend;
        private System.Windows.Forms.TextBox textBoxUri;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox richTextBoxBody;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RichTextBox richTextBoxResponseBody;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RichTextBox richTextBoxHeader;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.RichTextBox richTextBoxResponseHeader;
    }
}

