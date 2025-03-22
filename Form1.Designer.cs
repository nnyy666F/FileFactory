namespace FileFactory
{
	partial class Form1 : Form
	{
		private System.ComponentModel.IContainer components = null;
		private System.Windows.Forms.ListBox mergeListBox;

		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			this.mergeListBox = new System.Windows.Forms.ListBox();
			this.SuspendLayout();
			this.mergeListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mergeListBox.DisplayMember = "DisplayName";
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Name = "Form1";
			this.Text = "Form1";
			this.ResumeLayout(false);
		}
	}
}