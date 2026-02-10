using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace MP3te
{
	class Program
	{
		/* Importazione per collegare la console se il programma è lanciato da terminale */
		[DllImport("kernel32.dll")]
		static extern bool AttachConsole(int dwProcessId);
		private const int ATTACH_PARENT_PROCESS = -1;

		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length > 0)
			{
				/* Modalità CLI */
				RunCli(args);
			}
			else
			{
				/* Modalità GUI */
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new MainForm());
			}
		}

		static void RunCli(string[] args)
		{
			/* Collega l'output alla console esistente */
			AttachConsole(ATTACH_PARENT_PROCESS);
			
			Console.WriteLine("\n[MP3te CLI Mode]");
			
			if (args[0].ToLower() == "--help" || args[0].ToLower() == "-h")
			{
				Console.WriteLine("Usage: MP3te.exe [file_path] [title] [artist]");
				return;
			}

			if (args.Length >= 3)
			{
				try
				{
					string path = args[0];
					var meta = TagEngine.ReadTag(path);
					meta.Title = args[1];
					meta.Artist = args[2];
					
					TagEngine.WriteTag(path, meta);
					Console.WriteLine("Successfully updated: " + path);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error: " + ex.Message);
				}
			}
			else
			{
				Console.WriteLine("Invalid arguments. Use --help for info.");
			}
		}
	}
}