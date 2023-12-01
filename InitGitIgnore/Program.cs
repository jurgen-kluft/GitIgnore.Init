using System;
using System.IO;
using System.Collections.Generic;

namespace InitGitIgnore
{
	class Program
	{
		static void Main(string[] args)
		{
			MakeGitIgnore(@"F:\UE4.branches\4.21.1\");
		}

		class FileTypeStats
		{
			public Int64 MinSize { get; set; }
			public Int64 MaxSize { get; set; }
			public Int64 TotalSize { get; set; }
			public Int64 AverageSize { get; set; }
			public Int64 Count { get; set; }
			public int IsBinary { get; set; }
		}

		static List<string> defaultExcludedFolders = new List<string>
		{
			".vs",
			".svn", ".git", ".hg"
		};

		static List<string> defaultBinaryExtensions = new List<string>  {
			".jpg .jpeg .tga .gif .png .bmp .psd .ttf .ico .icns",
			".fbx .max .mb .exe .efi .msi .msm .dll .pdb .lib .a",
			".dylib .self .pyd .apk .vsix .bin .dat .data .zip .rar",
			".7z .gz .tgz .cab .bz2 .wixlib .doc .docx .pdf .chm",
			".mp4 .mp3 .avi .ogv .wav .uasset .umap .ddp .upk .nib",
			".so .mdb .mo .locres .bc"
		};

		static List<string> defaultTextExtensions = new List<string>  {
			".bat .cmd .command .sh .rsp", ".html .css .xsd .aspx", ".xlsm .xlsx",
			".rtf .smi", ".cpp .hpp .h .c .cc .inl .rc .cs .vb .py .js .java .sql .pas .asm .lua .go .rst",
			".ini .log .csv .txt .changes .xml .json .toml .yaml .properties .targets .build .config .m4",
			".sln .vcproj .vcxproj .csproj .vsprops .mak .mk .filters",
			".uproject .uplugin .uprojectdirs .usf .udn .archive",
			".ms .mel .glsl .hlsl .out",".vsmacros", ".pc .manifest", ".ucm", ".xib", ".vqs",
		};

		static HashSet<string> BuildExtensionSet(List<string> extensions)
		{
			HashSet<string> set = new HashSet<string>();
			foreach (string s in extensions)
			{
				string[] es = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				foreach (string e in es)
				{
					if (set.Contains(e) == false)
						set.Add(e);
				}
			}
			return set;
		}

		static void MakeGitIgnore(string folderPath)
		{
			Dictionary<string, FileTypeStats> fileStatsMap = new Dictionary<string, FileTypeStats>();

			HashSet<string> binaryExtensions = BuildExtensionSet(defaultBinaryExtensions);
			HashSet<string> textExtensions = BuildExtensionSet(defaultTextExtensions);

			List<string> output = new List<string>();
			List<string> allfiles = CollectAllFiles(folderPath, true);
			foreach (string filepath in allfiles)
			{
				int isbinary = 0;
				if (ShouldProcessFile(filepath, binaryExtensions, textExtensions))
				{
					isbinary = IsBinaryFile(filepath);
					if (isbinary == 1)
					{
						AddExtension(filepath, binaryExtensions);
					}
					else if (isbinary == -1)
					{
						AddExtension(filepath, textExtensions);
					}
				}
				else
				{
					if (ShouldProcessFile(filepath, binaryExtensions) == false)
					{
						isbinary = 1;
					}
					else if (ShouldProcessFile(filepath, textExtensions) == false)
					{
						isbinary = -1;
					}
				}

				string ext = Path.GetExtension(filepath);
				if (!String.IsNullOrEmpty(ext))
				{
					ext = ext.ToLower();
					FileTypeStats stat;
					if (!fileStatsMap.TryGetValue(ext, out stat))
					{
						stat = new FileTypeStats();
						stat.MinSize = Int64.MaxValue;
						stat.MaxSize = Int64.MinValue;
						fileStatsMap.Add(ext, stat);
					}
					FileInfo fileinfo = new FileInfo(filepath);
					if (fileinfo.Exists && fileinfo.Length > 0)
					{
						stat.TotalSize += fileinfo.Length;
						if (fileinfo.Length < stat.MinSize)
							stat.MinSize = fileinfo.Length;
						if (fileinfo.Length > stat.MaxSize)
							stat.MaxSize = fileinfo.Length;
						if (isbinary != 0 && stat.IsBinary == 0)
							stat.IsBinary = isbinary;
						stat.Count++;
					}
				}
			}

			Int64 textfiles_totalsize = 0;
			Int64 textfiles_totalcnt = 0;
			Int64 binaryfiles_totalsize = 0;
			Int64 binaryfiles_totalcnt = 0;
			foreach (var s in fileStatsMap)
			{
				FileTypeStats stat = s.Value;
				if (stat.IsBinary == -1)
				{
					textfiles_totalsize += stat.TotalSize;
					textfiles_totalcnt += stat.Count;
				}
				else
				{
					if (stat.MaxSize > (long)4 * 1024 * 1024 * 1024)
					{
						Console.WriteLine("{0} has a file larger than 4 GB", s.Key);
					}

					binaryfiles_totalsize += stat.TotalSize;
					binaryfiles_totalcnt += stat.Count;
				}
			}
			Console.WriteLine("Text Files - Total - Count: {0}, Size: {1} MB", textfiles_totalcnt, textfiles_totalsize / (1024 * 1024));
			Console.WriteLine("Binary Files - Total - Count {0}, Size: {1} MB", binaryfiles_totalcnt, binaryfiles_totalsize / (1024 * 1024));

			FileStream fs_gitignore = new FileStream("generated.gitignore", FileMode.Create, FileAccess.ReadWrite);
			StreamWriter sw_gitignore = new StreamWriter(fs_gitignore);

			//Console.WriteLine("---- BINARY EXTENSIONS -----");
			//foreach (string ext in binaryExtensions)
			//{
			//	Console.WriteLine("    " + ext);
			//}

			sw_gitignore.WriteLine("#ignore all files");
			sw_gitignore.WriteLine("*.* ");
			sw_gitignore.WriteLine(" ");

			sw_gitignore.WriteLine("#[FOLDERS,ignore]");
			sw_gitignore.WriteLine(".hg/");
			sw_gitignore.WriteLine(".svn/");
			sw_gitignore.WriteLine(" ");

			sw_gitignore.WriteLine("#[FILES,text]");
			foreach (string ext in textExtensions)
			{
				sw_gitignore.WriteLine("!*" + ext);
			}

			sw_gitignore.Close();
			fs_gitignore.Close();
		}

		public static bool ShouldProcessFile(string filepath, HashSet<string> binaryExtensions, HashSet<string> textExtensions)
		{
			return ShouldProcessFile(filepath, binaryExtensions) && ShouldProcessFile(filepath, textExtensions);
		}

		public static bool ShouldProcessFile(string filepath, HashSet<string> extensions)
		{
			string ext = Path.GetExtension(filepath);
			if (String.IsNullOrEmpty(ext))
				return false;
			ext = ext.ToLower();
			return extensions.Contains(ext) == false;
		}

		public static void AddExtension(string filepath, HashSet<string> extensions)
		{
			string ext = Path.GetExtension(filepath);
			if (!String.IsNullOrEmpty(ext))
			{
				ext = ext.ToLower();
				extensions.Add(ext);
			}
		}

		public static List<string> CollectAllFiles(string path, bool recursive = false)
		{
			List<string> files = new List<string>();
			if (Directory.Exists(path))
			{
				files.AddRange(Directory.GetFiles(path, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
			}
			return files;
		}

		public static int IsBinaryFile(string filepath)
		{
			FileInfo fileinfo = new FileInfo(filepath);
			if (!fileinfo.Exists || fileinfo.Length == 0)
				return 0;

			using (StreamReader stream = new StreamReader(filepath))
			{
				int ch;
				while ((ch = stream.Read()) != -1)
				{
					if (isControlChar(ch))
					{
						return 1;
					}
				}
			}
			return -1;
		}

		public static bool isControlChar(int ch)
		{
			return (ch > Chars.NUL && ch < Chars.BS)
				|| (ch > Chars.CR && ch < Chars.SUB);
		}

		public static class Chars
		{
			public static char NUL = (char)0; // Null char
			public static char BS = (char)8; // Back Space
			public static char CR = (char)13; // Carriage Return
			public static char SUB = (char)26; // Substitute
		}
	}
}