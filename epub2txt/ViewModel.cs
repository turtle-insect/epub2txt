using Microsoft.Win32;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace epub2txt
{
	internal class ViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		public ICommand SelectFilenameCommand { get; init; }
		public ICommand SelectOutputPathCommand { get; init; }
		public ICommand ExecuteCommand { get; init; }


		public String Filename { get; set; } = String.Empty;
		public String OutputPath { get; set; } = String.Empty;

		public ViewModel()
		{
			SelectFilenameCommand = new ActionCommand(SelectFilename);
			SelectOutputPathCommand = new ActionCommand(SelectOutputPath);
			ExecuteCommand = new ActionCommand(Execute);
		}

		private void SelectFilename(Object? parameter)
		{
			var dlg = new OpenFileDialog();
			dlg.Filter = "epub|*epub";
			if (dlg.ShowDialog() == false) return;

			Filename = dlg.FileName;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Filename)));

			if (!String.IsNullOrEmpty(OutputPath)) return;

			OutputPath = System.IO.Path.GetDirectoryName(Filename) ?? string.Empty;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OutputPath)));
		}

		private void SelectOutputPath(Object? parameter)
		{
			var dlg = new OpenFolderDialog();
			if (dlg.ShowDialog() == false) return;

			OutputPath = dlg.FolderName;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OutputPath)));
		}

		private void Execute(Object? parameter)
		{
			if(String.IsNullOrEmpty(Filename)) return;
			if (String.IsNullOrEmpty(OutputPath)) return;

			// 条件確認
			if (!System.IO.File.Exists(Filename)) return;
			if (!System.IO.Directory.Exists(OutputPath))
			{
				System.IO.Directory.CreateDirectory(OutputPath);
			}
			if (!System.IO.Directory.Exists(OutputPath)) return;

			// work領域
			var workPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "work");
			// すでに存在していたら消す
			if(System.IO.Directory.Exists(workPath))
			{
				System.IO.Directory.Delete(workPath, true);
			}

			// epubを解凍
			// zipファイルのはずなので解凍してみる
			try
			{
				System.IO.Compression.ZipFile.ExtractToDirectory(Filename, workPath);
			}
			catch { return; }

			var epub = new Epub();
			var files = epub.GetXhtmlFiles(workPath);
			if (files.Count == 0) return;

			MessageBox.Show("処理を開始します");

			// 基本的にファイルは全て正しい文法で記述されている前提とする
			// 全部のファイルを読み取りながら、本文をtxtとして保存していく
			// 一冊を一つのtxtファイルとして出力する
			var sb = new StringBuilder();
			foreach (var xhtml in files)
			{
				var buffer = System.IO.File.ReadAllText(xhtml);

				// 実施順番に意味がある
				// (入れ替え不可)

				// ヘッダーを除去する
				buffer = Regex.Replace(buffer, @"<head>[\s\S]*?</head>", "");

				// ルビ(ruby)を除去する
				buffer = Regex.Replace(buffer, @"<rt>.*?</rt>", "");

				// 全てのタグを除去する
				buffer = Regex.Replace(buffer, @"<[^>]*>", "");

				// 改行コードのみの行を除去する
				buffer = Regex.Replace(buffer, @"^\s*$\r?\n", "", RegexOptions.Multiline);

				// tabを除去する
				buffer = buffer.Replace("\t", "");

				sb.Append(buffer);
			}

			String fn = System.IO.Path.GetFileName(Filename);
			fn = System.IO.Path.GetFileNameWithoutExtension(fn) + ".txt";
			fn = System.IO.Path.Combine(OutputPath, fn);
			System.IO.File.WriteAllText(fn, sb.ToString());

			// 処理が完了したら、出力ファイルの場所を開いておしまい
			//System.Diagnostics.Process.Start("explorer.exe", OutputPath);
		}
	}
}
