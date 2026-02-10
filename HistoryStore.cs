using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MP3te
{
    public class HistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public string FilePath { get; set; }
        public string TagName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}] {1} | {2}: {3} -> {4}", 
                Timestamp.ToString("HH:mm"), Path.GetFileName(FilePath), TagName, OldValue, NewValue);
        }
    }

    public static class HistoryStore
    {
        private static readonly string HistoryFile = "history.log";
        private static List<HistoryEntry> _entries = new List<HistoryEntry>();

        public static List<HistoryEntry> Load()
        {
            _entries.Clear();
            if (!File.Exists(HistoryFile)) return _entries;

            try
            {
                foreach (var line in File.ReadAllLines(HistoryFile))
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 5)
                    {
                        _entries.Add(new HistoryEntry
                        {
                            Timestamp = DateTime.Parse(parts[0]),
                            FilePath = parts[1],
                            TagName = parts[2],
                            OldValue = parts[3],
                            NewValue = parts[4]
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("History Load Error: " + ex.Message);
            }
            return _entries;
        }

        public static void Clear()
        {
            _entries.Clear();
            // Nota: Non eliminiamo il file fisico se non richiesto esplicitamente, 
            // ma il pulsante "Default" implica un reset visivo della sessione.
            // Se vuoi pulire anche il file:
            // if (File.Exists(HistoryFile)) File.Delete(HistoryFile);
        }

        public static void RecordChange(string path, string tag, string oldVal, string newVal)
        {
            var entry = new HistoryEntry
            {
                Timestamp = DateTime.Now,
                FilePath = path,
                TagName = tag,
                OldValue = oldVal,
                NewValue = newVal
            };

            _entries.Add(entry);
            
            try
            {
                string line = string.Format("{0}|{1}|{2}|{3}|{4}", 
                    entry.Timestamp, entry.FilePath, entry.TagName, entry.OldValue, entry.NewValue);
                File.AppendAllLines(HistoryFile, new[] { line });
            }
            catch (Exception ex)
            {
                Logger.Error("History Save Error: " + ex.Message);
            }
        }

        public static void UndoEntries(List<HistoryEntry> entriesToUndo)
        {
            foreach (var entry in entriesToUndo)
            {
                try
                {
                    TagEngine.WriteSingleTag(entry.FilePath, entry.TagName, entry.OldValue);
                    Logger.Info("Undo: " + Path.GetFileName(entry.FilePath) + " reverted " + entry.TagName);
                    _entries.Remove(entry);
                }
                catch (Exception ex)
                {
                    Logger.Error("Undo failed for " + entry.FilePath + ": " + ex.Message);
                }
            }
            RebuildFile();
        }

        private static void RebuildFile()
        {
            var lines = _entries.Select(e => string.Format("{0}|{1}|{2}|{3}|{4}", 
                e.Timestamp, e.FilePath, e.TagName, e.OldValue, e.NewValue));
            File.WriteAllLines(HistoryFile, lines);
        }
        
        // Mantieni RollbackToIndex se serve per compatibilità futura
        public static void RollbackToIndex(int startIndex)
        {
             // ... codice esistente ...
        }
    }
}