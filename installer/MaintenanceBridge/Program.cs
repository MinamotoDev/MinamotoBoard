using MinamotoBoard.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Windows.Forms;

[assembly: AssemblyTitle("MinamotoBoard MSI Maintenance Bridge")]
[assembly: AssemblyCompany("MinamotoDev")]
[assembly: AssemblyVersion("0.6.0.0")]
[assembly: AssemblyFileVersion("0.6.0.0")]

internal static class Program
{
    private const string UpgradeCode = "{8D1AF8D5-305C-4670-9E17-A0DB4FB394B3}";
    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    private static extern uint MsiEnumRelatedProducts(string upgradeCode, uint reserved, uint index, StringBuilder productCode);
    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    private static extern int MsiQueryProductState(string productCode);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr owner, string message, string title, uint flags);

    [DllImport("kernel32.dll")]
    private static extern ushort GetUserDefaultUILanguage();

    [STAThread]
    private static int Main(string[] args)
    {
        ushort detected = GetUserDefaultUILanguage();
        Localizer.Initialize((detected & 0x3ff) == 0x11 ? "ja-JP" :
            (detected & 0x3ff) == 0x04 ?
            (detected == 0x0404 || detected == 0x0c04 || detected == 0x1404 || detected == 0x7c04 ? "zh-TW" : "zh-CN") : "en-US");
        bool cleanup = Array.IndexOf(args, "--cleanup") >= 0;
        bool worker = Array.IndexOf(args, "--worker") >= 0;
        bool fromInstaller = Array.IndexOf(args, "--from-installer") >= 0;
        bool interactive = args.Length == 0 || Array.IndexOf(args, "--uninstall") >= 0;
        bool notify = interactive || Array.IndexOf(args, "--notify") >= 0;
        bool deleteConfig = GetValue(args, "--delete-config=") == "1";
        string userSid = GetValue(args, "--user-sid=");
        if (!cleanup && !worker && !fromInstaller && !interactive) return 1;
        try
        {
            if (cleanup) { DeleteUserData(userSid); return 0; }
            if (interactive)
            {
                if (!ConfirmUninstall(out deleteConfig)) return 1602;
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) userSid = identity.User.Value;
            }
            if (deleteConfig) CheckUser(userSid);
            List<string> products = FindProducts();
            if (products.Count == 0) return 0;
            if (!worker)
            {
                string temporary = Path.Combine(Path.GetTempPath(), "MinamotoBoard-Uninstall-" + Guid.NewGuid().ToString("N") + ".exe");
                File.Copy(Assembly.GetExecutingAssembly().Location, temporary);
                // Keep the coordinator in the initiating user's context. Only msiexec elevates.
                Process.Start(new ProcessStartInfo(temporary, "--worker --delete-config=" +
                    (deleteConfig ? "1" : "0") + " --user-sid=" + userSid + " --wait-pid=" + Process.GetCurrentProcess().Id +
                    (notify ? " --notify" : "")) { UseShellExecute = true });
                return 0;
            }
            int parentId;
            if (int.TryParse(GetValue(args, "--wait-pid="), out parentId))
            {
                try { using (Process parent = Process.GetProcessById(parentId)) parent.WaitForExit(30000); }
                catch (ArgumentException) { }
            }
            string msiexec = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "msiexec.exe");
            bool restart = false;
            bool elevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            foreach (string product in products)
            {
                int result = 1618;
                // The launching MSI exits before this separate uninstall can proceed.
                for (int attempt = 0; attempt < 60 && result == 1618; attempt++)
                {
                    using (Process process = Process.Start(new ProcessStartInfo(msiexec,
                        "/x " + product + " /passive /norestart")
                        { UseShellExecute = true, Verb = elevated ? "open" : "runas" }))
                    {
                        process.WaitForExit(); result = process.ExitCode;
                    }
                    if (result == 1618) Thread.Sleep(1000);
                }
                if (result == 3010) restart = true;
                else if (result == 1602) return 1602;
                else if (result != 0 && result != 1605)
                    throw new InvalidOperationException(Localizer.T("卸载失败，错误代码：") + result);
            }
            // Never remove user data following a failed/canceled uninstall or during an upgrade.
            if (deleteConfig)
            {
                cleanup = true;
                DeleteUserData(userSid);
            }
            if (notify) MessageBox(IntPtr.Zero, restart ? Localizer.T("卸载完成。重启后生效。") : Localizer.T("卸载完成。"), "MinamotoBoard", 0x40);
            return restart ? 3010 : 0;
        }
        catch (Win32Exception ex)
        {
            if (ex.NativeErrorCode == 1223) return 1602;
            MessageBox(IntPtr.Zero, ex.Message, cleanup ? Localizer.T("配置删除失败") : Localizer.T("卸载失败"), 0x10); return 1;
        }
        catch (Exception ex)
        {
            MessageBox(IntPtr.Zero, ex.Message, cleanup ? Localizer.T("配置删除失败") : Localizer.T("卸载失败"), 0x10); return 1;
        }
    }

    private static bool ConfirmUninstall(out bool deleteConfig)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using (Form dialog = new Form())
        {
            dialog.Text = Localizer.T("卸载 MinamotoBoard"); dialog.ClientSize = new Size(460, 168);
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog; dialog.MaximizeBox = false; dialog.MinimizeBox = false;
            dialog.StartPosition = FormStartPosition.CenterScreen;
            dialog.AutoScaleMode = AutoScaleMode.Dpi;
            dialog.Font = new Font("Segoe UI", 9);
            dialog.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            Label question = new Label { Text = Localizer.T("确定卸载 MinamotoBoard 吗？"), AutoSize = true, Location = new Point(20, 22) };
            CheckBox remove = new CheckBox { Text = Localizer.T("删除配置和插件"), AutoSize = true, Checked = false, Location = new Point(20, 60) };
            Button uninstall = new Button { Text = Localizer.T("卸载"), DialogResult = DialogResult.OK, Size = new Size(104, 28), Location = new Point(212, 122) };
            Button cancel = new Button { Text = Localizer.T("取消"), DialogResult = DialogResult.Cancel, Size = new Size(86, 28), Location = new Point(336, 122) };
            dialog.Controls.AddRange(new Control[] { question, remove, uninstall, cancel });
            dialog.AcceptButton = uninstall; dialog.CancelButton = cancel;
            bool confirmed = dialog.ShowDialog() == DialogResult.OK;
            deleteConfig = confirmed && remove.Checked;
            return confirmed;
        }
    }

    private static string GetValue(string[] args, string prefix)
    {
        foreach (string argument in args)
            if (argument.StartsWith(prefix, StringComparison.Ordinal)) return argument.Substring(prefix.Length);
        return "";
    }

    private static void CheckUser(string expectedSid)
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            if (identity.IsSystem || identity.User == null || string.IsNullOrEmpty(expectedSid) ||
                !string.Equals(identity.User.Value, expectedSid, StringComparison.Ordinal))
                throw new InvalidOperationException(Localizer.T("无法确认当前用户，未删除配置。"));
        }
    }

    private static string UserDataRoot(Environment.SpecialFolder folder)
    {
        string knownFolder = Environment.GetFolderPath(folder);
        if (string.IsNullOrWhiteSpace(knownFolder) || !Path.IsPathRooted(knownFolder))
            throw new InvalidOperationException(Localizer.T("无法定位配置目录。"));
        string parent = Path.GetFullPath(knownFolder);
        string target = Path.GetFullPath(Path.Combine(parent, "MinamotoDev", "Board"));
        string boundary = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!target.StartsWith(boundary, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(Localizer.T("配置目录无效。"));
        string vendorFolder = Path.GetDirectoryName(target);
        if (Directory.Exists(vendorFolder) && (File.GetAttributes(vendorFolder) & FileAttributes.ReparsePoint) != 0)
            throw new IOException(Localizer.T("配置目录包含链接，未删除配置。"));
        return target;
    }

    private static void CollectUserData(string root, string directory, List<string> files, List<string> directories)
    {
        RequireInsideRoot(root, directory);
        if (!Directory.Exists(directory)) return;
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new IOException(Localizer.T("配置目录包含链接，未删除配置。"));
        foreach (string entry in Directory.GetFileSystemEntries(directory))
        {
            RequireInsideRoot(root, entry);
            FileAttributes attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException(Localizer.T("配置目录包含链接，未删除配置。"));
            if ((attributes & FileAttributes.Directory) != 0) CollectUserData(root, entry, files, directories);
            else files.Add(entry);
        }
        directories.Add(directory);
    }

    private static void RequireInsideRoot(string root, string path)
    {
        string fullPath = Path.GetFullPath(path);
        if (!string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(Localizer.T("配置目录无效。"));
    }

    private static void DeleteUserData(string userSid)
    {
        CheckUser(userSid);
        // Fixed known-folder children only; no caller-supplied paths, profile enumeration or parent deletion.
        HashSet<string> roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            UserDataRoot(Environment.SpecialFolder.MyDocuments),
            UserDataRoot(Environment.SpecialFolder.LocalApplicationData) };
        List<string> files = new List<string>();
        List<string> directories = new List<string>();
        foreach (string root in roots) CollectUserData(root, root, files, directories);
        foreach (string file in files)
        {
            CheckCleanupParents(roots, file);
            FileAttributes attributes = File.GetAttributes(file);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException(Localizer.T("配置目录包含链接。"));
            if ((attributes & FileAttributes.ReadOnly) != 0) File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
            File.Delete(file);
        }
        // Post-order, non-recursive directory deletion cannot traverse a newly introduced junction.
        foreach (string directory in directories) Directory.Delete(directory, false);
    }

    private static void CheckCleanupParents(IEnumerable<string> roots, string file)
    {
        foreach (string root in roots)
        {
            if (!file.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
            string vendorFolder = Path.GetDirectoryName(root);
            string directory = Path.GetDirectoryName(file);
            // Recheck ancestors immediately before deleting each file; a plugin may have changed them.
            while (true)
            {
                if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException(Localizer.T("配置目录包含链接。"));
                if (string.Equals(directory, vendorFolder, StringComparison.OrdinalIgnoreCase)) return;
                directory = Path.GetDirectoryName(directory);
            }
        }
        throw new InvalidOperationException(Localizer.T("配置目录无效。"));
    }

    private static List<string> FindProducts()
    {
        List<string> products = new List<string>();
        for (uint index = 0; ; index++)
        {
            StringBuilder code = new StringBuilder(39);
            uint result = MsiEnumRelatedProducts(UpgradeCode, 0, index, code);
            if (result == 259) break;
            if (result != 0) throw new InvalidOperationException(Localizer.T("无法读取安装信息，错误代码：") + result);
            if (MsiQueryProductState(code.ToString()) == 5) products.Add(code.ToString());
        }
        return products;
    }
}
