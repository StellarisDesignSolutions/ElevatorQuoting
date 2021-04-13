namespace ElevatorQuoting
{
    partial class LoadQuote
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
            this.listViewQuotes = new System.Windows.Forms.ListView();
            this.buttonLoad = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listViewQuotes
            // 
            this.listViewQuotes.BackColor = System.Drawing.Color.White;
            this.listViewQuotes.HideSelection = false;
            this.listViewQuotes.Location = new System.Drawing.Point(12, 12);
            this.listViewQuotes.Name = "listViewQuotes";
            this.listViewQuotes.ShowItemToolTips = true;
            this.listViewQuotes.Size = new System.Drawing.Size(650, 148);
            this.listViewQuotes.TabIndex = 1;
            this.listViewQuotes.UseCompatibleStateImageBehavior = false;
            this.listViewQuotes.View = System.Windows.Forms.View.Details;
            // 
            // buttonLoad
            // 
            this.buttonLoad.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.buttonLoad.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonLoad.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonLoad.ForeColor = System.Drawing.Color.White;
            this.buttonLoad.Location = new System.Drawing.Point(475, 166);
            this.buttonLoad.Name = "buttonLoad";
            this.buttonLoad.Size = new System.Drawing.Size(187, 30);
            this.buttonLoad.TabIndex = 2;
            this.buttonLoad.Text = "LOAD";
            this.buttonLoad.UseVisualStyleBackColor = false;
            this.buttonLoad.Click += new System.EventHandler(this.buttonLoad_Click);
            // 
            // LoadQuote
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(674, 202);
            this.Controls.Add(this.buttonLoad);
            this.Controls.Add(this.listViewQuotes);
            this.Name = "LoadQuote";
            this.Text = "LoadQuote";
            this.Load += new System.EventHandler(this.LoadQuote_Load);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ListView listViewQuotes;
        private System.Windows.Forms.Button buttonLoad;
    }
}