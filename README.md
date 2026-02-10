**MP3te** is a lightweight CLI/GUI MP3 tag editor designed for speed and simplicity.  
Built with a modern dark interface, it allows for quick editing of metadata, cover art management, and change tracking.

⚠️ this tool is in Proto stage, provided as is it, without any warranty

## FEATURES

*   **Hybrid Interface:** Works as a standard GUI application or a Command Line Tool for batch automation.
*   **Dark Mode:** Native-feel dark theme optimized for Windows 10/11.
*   **Smart Layout:** 3-column unified grid design (Tree/Cover | Editor | Details/Log).
*   **History & Undo:** Tracks every change made during the session with single or multi-select Undo capability.
*   **Cover Art:** View and replace MP3 cover art instantly.
*   **Portable:** Single executable (plus TagLibSharp dependency), stores config in a local `.ini` file.

## MANDATORY

*   Windows 10/11
*   .NET Framework 4.7.2 or higher
*   TagLibSharp.dll (included in build)

## COMPILE (or just download the latest relase)

`& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /out:<PATH_OUTPUT>\MP3te.exe /optimize+ /win32icon:"<PATH_SOURCE>\MP3te.ico" /reference:TagLibSharp.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.dll /reference:System.Core.dll *.cs`

## USAGE

**CLI Mode:** `MP3te.exe "C:\Music\Song.mp3" "New Title" "New Artist"`
