using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;

namespace MonkeyCompiler
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        // Controles de la interfaz
        private TextBox txtSource;
        private TextBox txtOutput;
        private Button btnCompileRun;

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
            this.components = new Container();
            this.txtSource = new TextBox();
            this.btnCompileRun = new Button();
            this.txtOutput = new TextBox();
            this.SuspendLayout();
            // 
            // txtSource
            // 
            this.txtSource.AcceptsTab = true;
            this.txtSource.Multiline = true;
            this.txtSource.ScrollBars = ScrollBars.Vertical;
            this.txtSource.Font = new Font("Consolas", 10F);
            this.txtSource.Location = new Point(12, 12);
            this.txtSource.Name = "txtSource";
            this.txtSource.Size = new Size(760, 320);
            this.txtSource.TabIndex = 0;
            // 
            // btnCompileRun
            // 
            this.btnCompileRun.Location = new Point(12, 338);
            this.btnCompileRun.Name = "btnCompileRun";
            this.btnCompileRun.Size = new Size(120, 30);
            this.btnCompileRun.TabIndex = 1;
            this.btnCompileRun.Text = "Compilar / Correr";
            this.btnCompileRun.UseVisualStyleBackColor = true;
            this.btnCompileRun.Click += new System.EventHandler(this.btnCompileRun_Click);
            // 
            // txtOutput
            // 
            this.txtOutput.Multiline = true;
            this.txtOutput.ScrollBars = ScrollBars.Vertical;
            this.txtOutput.Font = new Font("Consolas", 9F);
            this.txtOutput.Location = new Point(12, 374);
            this.txtOutput.Name = "txtOutput";
            this.txtOutput.ReadOnly = true;
            this.txtOutput.Size = new Size(760, 150);
            this.txtOutput.TabIndex = 2;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(784, 541);
            this.Controls.Add(this.txtOutput);
            this.Controls.Add(this.btnCompileRun);
            this.Controls.Add(this.txtSource);
            this.Name = "Form1";
            this.Text = "Monkey Compiler";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
