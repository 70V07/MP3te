using System;
using System.IO;
using System.Drawing;
using TagLib;

namespace MP3te
{
	public class Mp3Metadata
	{
		public string FilePath { get; set; }
		public string Title { get; set; }
		public string Artist { get; set; }
		public string Album { get; set; }
		public uint Year { get; set; }
		public string Genre { get; set; }
		public string Bitrate { get; set; }
		public string Duration { get; set; }
		public string SampleRate { get; set; }
		public string Mode { get; set; } 
	}

	public static class TagEngine
	{
		public static Mp3Metadata ReadTag(string filePath)
		{
			using (var file = TagLib.File.Create(filePath))
			{
				return new Mp3Metadata
				{
					FilePath = filePath,
					Title = file.Tag.Title ?? Path.GetFileNameWithoutExtension(filePath),
					Artist = file.Tag.FirstPerformer ?? "",
					Album = file.Tag.Album ?? "",
					Year = file.Tag.Year,
					Genre = file.Tag.FirstGenre ?? "",
					Bitrate = file.Properties.AudioBitrate.ToString() + " kbps",
					Duration = file.Properties.Duration.ToString(@"mm\:ss"),
					SampleRate = file.Properties.AudioSampleRate.ToString(),
					Mode = file.Properties.AudioChannels == 2 ? "Stereo" : "Mono"
				};
			}
		}

		public static void WriteTag(string filePath, Mp3Metadata data)
		{
			using (var file = TagLib.File.Create(filePath))
			{
				file.Tag.Title = data.Title;
				file.Tag.Performers = new[] { data.Artist };
				file.Tag.Album = data.Album;
				file.Tag.Year = data.Year;
				file.Tag.Genres = new[] { data.Genre };
				file.Save();
			}
		}

		public static void WriteSingleTag(string filePath, string tagName, string value)
		{
			using (var file = TagLib.File.Create(filePath))
			{
				switch (tagName)
				{
					case "Title": file.Tag.Title = value; break;
					case "Artist": file.Tag.Performers = new[] { value }; break;
					case "Album": file.Tag.Album = value; break;
					case "Year": 
						uint y; 
						if(uint.TryParse(value, out y)) file.Tag.Year = y; 
						break;
					case "Genre": file.Tag.Genres = new[] { value }; break;
				}
				file.Save();
			}
		}

		public static Image GetCover(string filePath)
		{
			try 
			{
				using (var file = TagLib.File.Create(filePath))
				{
					if (file.Tag.Pictures.Length > 0)
					{
						var bin = file.Tag.Pictures[0].Data.Data;
						using (var ms = new MemoryStream(bin))
						{
                            // Fix: Crea una Bitmap clonata per evitare problemi con lo stream chiuso
							return new Bitmap(ms);
						}
					}
				}
			}
			catch {}
			return null;
		}

		public static void SetCover(string filePath, string imagePath)
		{
			using (var file = TagLib.File.Create(filePath))
			{
				var picture = new TagLib.Picture(imagePath);
				file.Tag.Pictures = new TagLib.IPicture[] { picture };
				file.Save();
			}
		}
	}
}