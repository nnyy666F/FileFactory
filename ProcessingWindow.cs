using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace FileFactory
{
	public class ProcessingWindow : Form
	{
		private List<string> filePaths = new List<string>();
		private ListView listView;
		private ImageList imageList;
		private ToolStripMenuItem mergeMenuItem;
		private CheckBox appendCheckBox;
		public event EventHandler<int> ProcessingStarted;
		public event EventHandler<int> ProgressChanged;
		public event EventHandler<(string file, int lines, int processedLines, int totalLines)> FileProcessing;

		private Stack<List<string>> undoStack = new Stack<List<string>>();
		private Stack<List<string>> redoStack = new Stack<List<string>>();

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
			out SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

		private const uint SHGFI_ICON = 0x100;
		private const uint SHGFI_SMALLICON = 0x1;
		private const uint SHGFI_LARGEICON = 0x0;

		private const int CP_NOCLOSE_BUTTON = 0x200;

		[StructLayout(LayoutKind.Sequential)]
		private struct SHFILEINFO
		{
			public IntPtr hIcon;
			public int iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szTypeName;
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams myCp = base.CreateParams;
				myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
				return myCp;
			}
		}

		public ProcessingWindow()
		{
			InitializeUI();
			SetupDragDrop();
		}

		private void InitializeUI()
		{
			this.Text = "文件加工窗口";
			this.Size = new Size(800, 600);
			this.StartPosition = FormStartPosition.CenterScreen;

			imageList = new ImageList
			{
				ImageSize = new Size(16, 16),
				ColorDepth = ColorDepth.Depth32Bit
			};
			MenuStrip menuStrip = new MenuStrip();
			ToolStripMenuItem fileMenu = new ToolStripMenuItem("开始");
			mergeMenuItem = new ToolStripMenuItem("合并文件");
			mergeMenuItem.Enabled = false;
			mergeMenuItem.Click += StartProcessing;
			fileMenu.DropDownItems.Add(mergeMenuItem);
			menuStrip.Items.Add(fileMenu);

			appendCheckBox = new CheckBox
			{
				Text = "追加写入",
				Dock = DockStyle.Top,
				AutoSize = true
			};


			listView = new ListView
			{
				Dock = DockStyle.Fill,
				View = View.Details,
				SmallImageList = imageList,
				FullRowSelect = true
			};

			listView.Columns.Add("类型", 80);
			listView.Columns.Add("名称", 200);
			listView.Columns.Add("路径", 500);
			listView.KeyDown += ListView_KeyDown;

			this.Controls.Add(menuStrip);
			this.Controls.Add(appendCheckBox);
			this.Controls.Add(listView);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == (Keys.Control | Keys.Z))
			{
				Undo();
				return true;
			}
			else if (keyData == (Keys.Control | Keys.Y))
			{
				Redo();
				return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void Undo()
		{
			if (undoStack.Count > 0)
			{
				redoStack.Push(new List<string>(filePaths));
				filePaths = undoStack.Pop();
				UpdateListView();
				mergeMenuItem.Enabled = filePaths.Count > 0;
			}
		}

		private void Redo()
		{
			if (redoStack.Count > 0)
			{
				undoStack.Push(new List<string>(filePaths));
				filePaths = redoStack.Pop();
				UpdateListView();
				mergeMenuItem.Enabled = filePaths.Count > 0;
			}
		}

		private void UpdateListView()
		{
			listView.Items.Clear();
			foreach (string path in filePaths)
			{
				AddFileToList(path);
			}
		}

		private void ListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				undoStack.Push(new List<string>(filePaths));
				redoStack.Clear();
				foreach (ListViewItem item in listView.SelectedItems)
				{
					string path = item.Tag.ToString();
					filePaths.Remove(path);
					listView.Items.Remove(item);
				}
				mergeMenuItem.Enabled = filePaths.Count > 0;
			}
		}

		private Icon GetFileIcon(string path)
		{
			try
			{
				SHFILEINFO shinfo = new SHFILEINFO();
				uint flags = SHGFI_ICON | SHGFI_SMALLICON;

				if (Directory.Exists(path))
				{
					flags |= 0x1;
					SHGetFileInfo("", 0, out shinfo, (uint)Marshal.SizeOf(shinfo), flags);
				}
				else
				{
					SHGetFileInfo(path, 0, out shinfo, (uint)Marshal.SizeOf(shinfo), flags);
				}

				return Icon.FromHandle(shinfo.hIcon);
			}
			catch
			{
				return SystemIcons.WinLogo;
			}
		}

		private void AddFileToList(string path)
		{
			try
			{
				var icon = GetFileIcon(path);
				imageList.Images.Add(icon);

				var item = new ListViewItem
				{
					Text = Directory.Exists(path) ? "文件夹" : "文件",
					ImageIndex = imageList.Images.Count - 1
				};

				item.SubItems.Add(Path.GetFileName(path));
				item.SubItems.Add(path);
				item.Tag = path;

				listView.Items.Add(item);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"加载图标失败: {ex.Message}");
			}
		}

		private void SetupDragDrop()
		{
			this.AllowDrop = true;
			this.DragEnter += (s, e) =>
			{
				if (e.Data.GetDataPresent(DataFormats.FileDrop))
					e.Effect = DragDropEffects.Copy;
			};

			this.DragDrop += (s, e) =>
			{
				undoStack.Push(new List<string>(filePaths));
				redoStack.Clear();
				string[] items = (string[])e.Data.GetData(DataFormats.FileDrop);
				foreach (string path in items)
				{
					if ((File.Exists(path) || Directory.Exists(path)) && !filePaths.Contains(path))
					{
						filePaths.Add(path);
						AddFileToList(path);
					}
				}
				mergeMenuItem.Enabled = filePaths.Count > 0;
			};
		}

		private void StartProcessing(object sender, EventArgs e)
		{
			using (SaveFileDialog dialog = new SaveFileDialog())
			{
				dialog.Filter = "所有文件 (*.*)|*.*";
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					try
					{
						MergeFiles(dialog.FileName, appendCheckBox.Checked);
					}
					catch (Exception ex)
					{
						MessageBox.Show($"合并失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void MergeFiles(string outputPath, bool append)
		{
			int totalLines = CalculateTotalLines();
			int processedLines = 0;
			int reportInterval = Math.Max(totalLines / 6, 1);

			ProcessingStarted?.Invoke(this, totalLines);

			FileStream output;
			if (append && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0)
			{
				output = File.Open(outputPath, FileMode.Append);
			}
			else
			{
				output = File.Create(outputPath);
			}

			using (output)
			{
				foreach (string path in filePaths)
				{
					if (File.Exists(path))
					{
						int lines = CountLines(path);
						AppendFile(path, output);
						processedLines += lines;
						ProgressChanged?.Invoke(this, processedLines);
						while (processedLines >= reportInterval)
						{
							FileProcessing?.Invoke(this,
								(Path.GetFileName(path),
								lines,
								processedLines,
								totalLines));
							reportInterval += totalLines / 6;
						}
					}
					else if (Directory.Exists(path))
					{
						foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
						{
							int lines = CountLines(file);
							AppendFile(file, output);
							processedLines += lines;
							ProgressChanged?.Invoke(this, processedLines);
							while (processedLines >= reportInterval)
							{
								FileProcessing?.Invoke(this,
									(Path.GetFileName(file),
									lines,
									processedLines,
									totalLines));
								reportInterval += totalLines / 6;
							}
						}
					}
				}
			}
		}

		private int CalculateTotalLines()
		{
			int total = 0;
			foreach (string path in filePaths)
			{
				if (File.Exists(path))
				{
					total += CountLines(path);
				}
				else if (Directory.Exists(path))
				{
					foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
					{
						total += CountLines(file);
					}
				}
			}
			return total;
		}

		private void ProcessDirectory(string dirPath, FileStream output)
		{
			foreach (string file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
			{
				AppendFile(file, output);
			}
		}

		private void AppendFile(string filePath, FileStream output)
		{
			string comment = GenerateFileComment(filePath);
			byte[] commentBytes = Encoding.UTF8.GetBytes(comment);
			output.Write(commentBytes, 0, commentBytes.Length);
			using (FileStream input = File.OpenRead(filePath))
			{
				input.CopyTo(output);
			}
		}

		private string GenerateFileComment(string filePath)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("/*");
			sb.AppendLine($"时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
			sb.AppendLine($"原文件路径：{filePath}");

			try
			{
				sb.AppendLine($"MD5：{CalculateMD5(filePath)}");
				sb.AppendLine($"行数：{CountLines(filePath)}");
			}
			catch (Exception ex)
			{
				sb.AppendLine($"元数据获取失败：{ex.Message}");
			}

			sb.AppendLine("*/");
			sb.AppendLine();
			return sb.ToString();
		}

		private string CalculateMD5(string filePath)
		{
			using (var md5 = MD5.Create())
			using (var stream = File.OpenRead(filePath))
			{
				byte[] hashBytes = md5.ComputeHash(stream);
				return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
			}
		}

		private int CountLines(string filePath)
		{
			int count = 0;
			using (var reader = new StreamReader(filePath))
			{
				while (reader.ReadLine() != null)
				{
					count++;
				}
			}
			return count;
		}
	}
}