using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace MP3te
{
    public static class Logger
    {
        private static RichTextBox _logBox;

        public static void RegisterControl(RichTextBox box)
        {
            _logBox = box;
        }

        public static void Clear()
        {
            if (_logBox != null && !_logBox.IsDisposed)
            {
                _logBox.Clear();
            }
        }

        public static void Info(string message) { Write("INFO", message, Color.Lime); }
        public static void Warn(string message) { Write("WARN", message, Color.Gold); }
        public static void Error(string message) { Write("ERR!", message, Color.Crimson); }

        private static void Write(string level, string message, Color color)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string fullMessage = string.Format("[{0}] {1}: {2}", timestamp, level, message);

            Console.WriteLine(fullMessage);

            if (_logBox != null && !_logBox.IsDisposed)
            {
                if (_logBox.InvokeRequired)
                    _logBox.Invoke(new Action(() => AppendToBox(fullMessage, color)));
                else
                    AppendToBox(fullMessage, color);
            }
        }

        private static void AppendToBox(string text, Color color)
        {
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.SelectionLength = 0;
            _logBox.SelectionColor = color;
            _logBox.AppendText(text + Environment.NewLine);
            _logBox.ScrollToCaret();
        }
    }
}