using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Text;

namespace FileFactory
{
	public partial class Form1 : Form
	{
		private ProcessingWindow processingWindow;
		private ProgressBar progressBar;
		private Label statusLabel;
		private TextBox consoleBox;

		public Form1()
		{
			InitializeComponent();
			InitializeUI();
			ShowProcessingWindow();
		}

		private void InitializeUI()
		{
			this.Text = "文件工厂";
			this.Size = new Size(600, 300);
			this.StartPosition = FormStartPosition.CenterScreen;

			Label warningLabel = new Label
			{
				Text = "文件加工时，请尽量不要启动别的程序！",
				Font = new Font("微软雅黑", 12, FontStyle.Bold),
				ForeColor = Color.Red,
				AutoSize = true,
				Dock = DockStyle.Top,
				Height = 60
			};

			consoleBox = new TextBox
			{
				Dock = DockStyle.Fill,
				Multiline = true,
				ScrollBars = ScrollBars.Vertical,
				Font = new Font("Consolas", 10),
				ReadOnly = true
			};

			progressBar = new ProgressBar
			{
				Dock = DockStyle.Bottom,
				Height = 20
			};

			statusLabel = new Label
			{
				Dock = DockStyle.Bottom,
				Height = 30,
				TextAlign = ContentAlignment.MiddleLeft
			};

			this.Controls.Add(warningLabel);
			this.Controls.Add(consoleBox);
			this.Controls.Add(progressBar);
			this.Controls.Add(statusLabel);
		}

		private void ShowProcessingWindow()
		{
			processingWindow = new ProcessingWindow();
			processingWindow.ProcessingStarted += OnProcessingStarted;
			processingWindow.FileProcessing += OnFileProcessing;
			processingWindow.ProgressChanged += OnProgressChanged;
			processingWindow.Show();
			processingWindow.Location = new Point(this.Right + 10, this.Top);
		}

		private void OnProcessingStarted(object sender, int totalFiles)
		{
			this.Invoke((MethodInvoker)delegate {
				progressBar.Maximum = totalFiles;
				progressBar.Value = 0;
			});
		}

		private void OnFileProcessing(object sender, (string file, int lines, int processedLines, int totalLines) info)
		{
			this.Invoke((MethodInvoker)delegate
			{
				consoleBox.AppendText($"正在处理：{info.file}" + Environment.NewLine);
				consoleBox.AppendText($"行数：{info.lines}" + Environment.NewLine);
				double progress = (double)info.processedLines / info.totalLines * 100;
				consoleBox.AppendText($"总进度：{progress:F2}%{Environment.NewLine}{Environment.NewLine}");
				consoleBox.SelectionStart = consoleBox.TextLength;
				consoleBox.ScrollToCaret();
			});
		}

		private void OnProgressChanged(object sender, int progress)
		{
			this.Invoke((MethodInvoker)delegate {
				progressBar.Value = progress;
			});
		}
	}
}