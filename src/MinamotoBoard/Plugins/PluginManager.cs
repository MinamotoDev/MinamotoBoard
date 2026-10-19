using MinamotoBoard.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MinamotoBoard.PluginApi;

namespace MinamotoBoard.Plugins
{
    public sealed class PluginRecord
    {
        public string RelativePath { get; internal set; }
        public string Name { get; internal set; }
        internal string StatusKey;
        internal string StatusDetail;
        public string Status { get { return Localizer.T(StatusKey ?? "") + StatusDetail; } }
        public bool Enabled { get; internal set; }
        internal IBoardPlugin Instance;
        internal PluginHost Host;
    }

    public sealed class PluginManager : IDisposable
    {
        public string PluginDirectory { get; private set; }
        private readonly string settingsFile;
        private readonly List<PluginRecord> records = new List<PluginRecord>();
        private bool disposed;
        public IList<PluginRecord> Records { get { return records.AsReadOnly(); } }
        public string DiscoveryError { get; private set; }

        public PluginManager(string pluginDirectory, string settingsFile)
        {
            PluginDirectory = Path.GetFullPath(pluginDirectory);
            this.settingsFile = settingsFile;
        }

        public static PluginManager CreateDefault()
        {
            return new PluginManager(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "MinamotoDev", "Board", "Plugins"), Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "MinamotoDev", "Board", "disabled-plugins.txt"));
        }

        // Disk-only discovery is safe to run on a worker thread before LoadOnUiThread.
        public void Discover()
        {
            records.Clear();
            DiscoveryError = null;
            try
            {
                Directory.CreateDirectory(PluginDirectory);
                HashSet<string> disabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                // If settings cannot be read, fail closed instead of enabling disabled code.
                if (File.Exists(settingsFile)) disabled.UnionWith(File.ReadAllLines(settingsFile, Encoding.UTF8));
                List<string> directories = new List<string> { PluginDirectory };
                directories.AddRange(Directory.GetDirectories(PluginDirectory).Where(
                    p => (File.GetAttributes(p) & FileAttributes.ReparsePoint) == 0));
                foreach (string directory in directories.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                {
                    foreach (string file in Directory.GetFiles(directory, "*.Plugin.dll").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                    {
                        string relative = file.Substring(PluginDirectory.TrimEnd(Path.DirectorySeparatorChar).Length + 1);
                        records.Add(new PluginRecord { RelativePath = relative, Name = Path.GetFileName(file),
                            Enabled = !disabled.Contains(relative), StatusKey = disabled.Contains(relative) ? "已禁用" : "待加载" });
                    }
                }
            }
            catch (Exception ex) { records.Clear(); DiscoveryError = ex.Message; }
        }

        public void LoadOnUiThread(Window window, InkCanvas canvas, Panel toolbar, Panel overlay, bool safeMode)
        {
            window.Dispatcher.VerifyAccess();
            HashSet<string> loadedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PluginRecord record in records)
            {
                if (!record.Enabled) continue;
                if (safeMode) { record.StatusKey = "安全模式：未加载"; continue; }
                try
                {
                    Assembly assembly = Assembly.LoadFrom(Path.Combine(PluginDirectory, record.RelativePath));
                    Type[] types = assembly.GetExportedTypes().Where(t => typeof(IBoardPlugin).IsAssignableFrom(t)
                        && t.IsClass && !t.IsAbstract).ToArray();
                    if (types.Length != 1) throw new InvalidOperationException(Localizer.T("每个 Plugin.dll 必须提供一个公开的 IBoardPlugin 实现。"));
                    IBoardPlugin plugin = (IBoardPlugin)Activator.CreateInstance(types[0]);
                    record.Instance = plugin;
                    if (plugin.ApiVersion != 1) throw new InvalidOperationException(Localizer.T("插件 API 版本不兼容（当前支持 1）。"));
                    if (string.IsNullOrWhiteSpace(plugin.Id)) throw new InvalidOperationException(Localizer.T("插件 Id 不能为空。"));
                    if (!loadedIds.Add(plugin.Id)) throw new InvalidOperationException(Localizer.T("插件 Id 重复：") + plugin.Id);
                    record.Name = plugin.Name;
                    record.Host = new PluginHost(window, canvas, toolbar, overlay,
                        ex => { record.StatusKey = "工具执行失败："; record.StatusDetail = ex.Message; });
                    plugin.Initialize(record.Host);
                    record.StatusKey = "已加载";
                }
                catch (Exception ex)
                {
                    Release(record);
                    record.StatusKey = "加载失败："; record.StatusDetail = (ex.InnerException ?? ex).Message;
                }
            }
        }

        // Called on a worker thread; changing settings does not unload in-process assemblies.
        public void SaveDisabled(IEnumerable<string> disabledPaths)
        {
            string directory = Path.GetDirectoryName(settingsFile);
            Directory.CreateDirectory(directory);
            string temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllLines(temporary, disabledPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), new UTF8Encoding(false));
                if (File.Exists(settingsFile)) File.Replace(temporary, settingsFile, null);
                else File.Move(temporary, settingsFile);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        private static void Release(PluginRecord record)
        {
            // Cleanup must continue even if a plugin's shutdown code fails.
            try { if (record.Instance != null) record.Instance.Shutdown(); } catch (Exception) { }
            try { if (record.Host != null) record.Host.Dispose(); } catch (Exception) { }
            record.Instance = null; record.Host = null;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            for (int i = records.Count - 1; i >= 0; i--) Release(records[i]);
        }
    }
}
