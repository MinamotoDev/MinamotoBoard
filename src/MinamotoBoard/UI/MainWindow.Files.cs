using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;
using MinamotoBoard.Localization;
using MinamotoBoard.Models;

namespace MinamotoBoard.UI
{
    public partial class MainWindow
    {
        private bool documentDirty;
        private string documentPath;
        private readonly Queue<string> pendingFiles = new Queue<string>();
        private DispatcherTimer pendingFileTimer;

        internal void HandleExternalLaunch(string path)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            if (IsIconic(handle)) ShowWindow(handle, 9);
            IntPtr popup = GetLastActivePopup(handle);
            SetForegroundWindow(popup == IntPtr.Zero ? handle : popup);
            if (string.IsNullOrEmpty(path)) return;
            pendingFiles.Enqueue(path);
            if (pendingFileTimer == null)
            {
                pendingFileTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                pendingFileTimer.Tick += delegate
                {
                    if (!IsLoaded || !IsWindowEnabled(new WindowInteropHelper(this).Handle) || IsWriting() || fillWork != null) return;
                    if (pendingFiles.Count == 0) { pendingFileTimer.Stop(); return; }
                    // Stop before opening dialogs, which run a nested dispatcher loop.
                    pendingFileTimer.Stop();
                    OpenDocument(pendingFiles.Dequeue());
                    if (pendingFiles.Count != 0) pendingFileTimer.Start();
                };
                Closed += delegate { pendingFileTimer.Stop(); pendingFiles.Clear(); };
            }
            pendingFileTimer.Start();
        }

        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
        [DllImport("user32.dll")] private static extern IntPtr GetLastActivePopup(IntPtr window);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern bool IsWindowEnabled(IntPtr window);

        private void OnSaveDocument(object sender, RoutedEventArgs e) { SaveDocument(false, true); }

        internal bool SaveDocument(bool saveAs, bool showSuccess)
        {
            if (IsWriting()) return false;
            if (fillWork != null)
            {
                MessageBox.Show(this, Localizer.T("请等待填色完成后再保存。"), Localizer.T("保存"), MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            ToolOptionsPopup.IsOpen = false;
            try
            {
                CaptureCurrentPage();
                string directory = preferences.GetSaveDirectory();
                Directory.CreateDirectory(directory);
                string path = NextSavePath(directory);
                if (saveAs)
                {
                    SaveFileDialog dialog = new SaveFileDialog { Filter = "MinamotoBoard (*.mboard)|*.mboard",
                        DefaultExt = ".mboard", AddExtension = true, OverwritePrompt = true,
                        InitialDirectory = directory, FileName = Path.GetFileName(path) };
                    if (dialog.ShowDialog(this) != true) return false;
                    path = dialog.FileName;
                }
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    while (true)
                    {
                        try { BoardFile.Save(document, path, saveAs); break; }
                        catch (IOException)
                        {
                            if (saveAs || !File.Exists(path)) throw;
                            path = NextSavePath(directory);
                        }
                    }
                }
                finally { Mouse.OverrideCursor = null; }
                documentPath = path; documentDirty = false;
                Title = "MinamotoBoard · " + Path.GetFileName(path);
                if (showSuccess) MessageBox.Show(this, Localizer.T("已保存到：") + "\n" + path,
                    Localizer.T("保存"), MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Localizer.T("保存白板失败：") + "\n" + ex.Message,
                    Localizer.T("保存"), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        internal void OpenDocument(string filePath = null, bool append = true)
        {
            if (IsWriting()) return;
            ToolOptionsPopup.IsOpen = false;
            try
            {
                if (filePath == null)
                {
                    OpenFileDialog dialog = new OpenFileDialog { Filter = "MinamotoBoard (*.mboard)|*.mboard",
                        CheckFileExists = true, Multiselect = false };
                    string directory = preferences.GetSaveDirectory();
                    if (Directory.Exists(directory)) dialog.InitialDirectory = directory;
                    if (dialog.ShowDialog(this) != true) return;
                    filePath = dialog.FileName;
                }
                filePath = Path.GetFullPath(filePath);
                BoardDocument loaded;
                Mouse.OverrideCursor = Cursors.Wait;
                try { loaded = BoardFile.Load(filePath); }
                finally { Mouse.OverrideCursor = null; }
                if (append && loaded.Count > BoardDocument.MaximumPages - document.Count)
                {
                    MessageBox.Show(this, Localizer.T("追加后将超过 100 页上限，无法打开此文件。"), Localizer.T("打开白板"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                CancelAdaptiveErasing(); CancelToolWork(); Instruments.CancelInteraction();
                CaptureCurrentPage();
                CommitHistory();
                if (append)
                {
                    if (!document.AppendPages(loaded.Pages.ToList())) return;
                }
                else
                {
                    if (watchedInk != null) watchedInk.StrokesChanged -= OnInkChanged;
                    watchedInk = null; inkHistory.Clear();
                    document.ReplacePages(loaded.Pages.ToList(), loaded.CurrentIndex);
                }
                ShowCurrentPage();
                documentDirty = append;
                if (!append)
                {
                    documentPath = filePath;
                    Title = "MinamotoBoard · " + Path.GetFileName(documentPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, Localizer.T("打开白板失败：") + "\n" + ex.Message,
                    Localizer.T("打开白板"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string NextSavePath(string directory)
        {
            string prefix = "Board-" + DateTime.Now.ToString("yyyyMMdd-HHmm", System.Globalization.CultureInfo.InvariantCulture) + "-";
            long maximum = 0;
            foreach (string file in Directory.EnumerateFiles(directory, prefix + "*.mboard"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                long number;
                if (long.TryParse(name.Substring(prefix.Length), out number) && number > maximum) maximum = number;
            }
            return Path.Combine(directory, prefix + checked(maximum + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + ".mboard");
        }
    }
}
