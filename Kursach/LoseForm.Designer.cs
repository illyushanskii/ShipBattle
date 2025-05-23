namespace Kursach
{
    partial class LoseForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoseForm));
            label1 = new Label();
            label2 = new Label();
            buttonGo = new Button();
            buttonExit = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Arial Rounded MT Bold", 30F, FontStyle.Bold);
            label1.ForeColor = Color.Red;
            label1.Location = new Point(13, 37);
            label1.Name = "label1";
            label1.Size = new Size(715, 58);
            label1.TabIndex = 0;
            label1.Text = "Нажаль, переміг комп'ютер!";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            label2.ForeColor = Color.MediumBlue;
            label2.Location = new Point(213, 139);
            label2.Name = "label2";
            label2.Size = new Size(312, 46);
            label2.TabIndex = 1;
            label2.Text = "Почати спочатку?";
            // 
            // buttonGo
            // 
            buttonGo.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            buttonGo.Location = new Point(177, 233);
            buttonGo.Name = "buttonGo";
            buttonGo.Size = new Size(135, 70);
            buttonGo.TabIndex = 2;
            buttonGo.Text = "Так";
            buttonGo.UseVisualStyleBackColor = true;
            buttonGo.Click += buttonGo_Click;
            buttonGo.MouseEnter += buttonGo_MouseEnter;
            buttonGo.MouseLeave += buttonGo_MouseLeave;
            // 
            // buttonExit
            // 
            buttonExit.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            buttonExit.Location = new Point(373, 233);
            buttonExit.Name = "buttonExit";
            buttonExit.Size = new Size(187, 70);
            buttonExit.TabIndex = 3;
            buttonExit.Text = "Вихід";
            buttonExit.UseVisualStyleBackColor = true;
            buttonExit.Click += buttonExit_Click;
            buttonExit.MouseEnter += buttonExit_MouseEnter;
            buttonExit.MouseLeave += buttonExit_MouseLeave;
            // 
            // LoseForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.GradientInactiveCaption;
            ClientSize = new Size(740, 353);
            Controls.Add(buttonExit);
            Controls.Add(buttonGo);
            Controls.Add(label2);
            Controls.Add(label1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "LoseForm";
            Text = "Поразка";
            FormClosing += LoseForm_FormClosing;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private Button buttonGo;
        private Button buttonExit;
    }
}