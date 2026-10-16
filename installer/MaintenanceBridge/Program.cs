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

[assembly: AssemblyTitle("MinamotoBoard MSI Maintenance Bridge")]
[assembly: AssemblyCompany("MinamotoDev")]
[assembly: AssemblyVersion("0.3.0.0")]
[assembly: AssemblyFileVersion("0.3.0.0")]

internal static class Program
{
    private const string UpgradeCode = "{8D1AF8D5-305C-4670-9E17-A0DB4FB394B3}";
    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    private static extern uint MsiEnumRelatedProducts(string upgradeCode, uint reserved, uint index, StringBuilder productCode);
    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    private static extern int MsiQueryProductState(string productCode);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr owner, string message, string title, uint flags);

    [STAThread]
    private static int Main(string[] args)
    {
        bool worker = Array.IndexOf(args, "--worker") >= 0;
        bool fromInstaller = Array.IndexOf(args, "--from-installer") >= 0;
        // Internal MSI payload only. There is no separately installed uninstaller.
        if (!worker && !fromInstaller) return 1;
        try
        {
            List<string> products = FindProducts();
            if (products.Count == 0)
            {
                MessageBox(IntPtr.Zero, "没有找到已安装的 MinamotoBoard。", "MinamotoBoard", 0x40);
                return 0;
            }
            if (!worker)
            {
                if (!fromInstaller && MessageBox(IntPtr.Zero,
                    "是否卸载 MinamotoBoard？\n请先关闭白板并确认不再需要当前笔迹。\n文档中的插件和用户设置不会删除。",
                    "卸载 MinamotoBoard", 0x124) != 6) return 1602;
                // Run outside INSTALLFOLDER so MSI can remove the installed launcher.
                string temporary = Path.Combine(Path.GetTempPath(), "MinamotoBoard-Uninstall-" + Guid.NewGuid().ToString("N") + ".exe");
                File.Copy(Assembly.GetExecutingAssembly().Location, temporary);
                bool elevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
                Process.Start(new ProcessStartInfo(temporary, "--worker")
                {
                    UseShellExecute = true, Verb = elevated ? "open" : "runas"
                });
                return 0;
            }
            string msiexec = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "msiexec.exe");
            bool restart = false;
            foreach (string product in products)
            {
                int result = 1618;
                // The MSI that launched us first closes its confirmation dialog.
                // Retry only the native 'another installation is running' result.
                for (int attempt = 0; attempt < 60 && result == 1618; attempt++)
                {
                    using (Process process = Process.Start(new ProcessStartInfo(msiexec,
                        "/x " + product + " /passive /norestart") { UseShellExecute = false, CreateNoWindow = true }))
                    {
                        process.WaitForExit(); result = process.ExitCode;
                    }
                    if (result == 1618) Thread.Sleep(1000);
                }
                if (result == 3010) restart = true;
                else if (result != 0 && result != 1605)
                    throw new InvalidOperationException("Windows Installer 返回代码 " + result + "，卸载未完成。");
            }
            MessageBox(IntPtr.Zero, restart ? "卸载完成，请稍后重新启动 Windows。" : "MinamotoBoard 已卸载。", "MinamotoBoard", 0x40);
            return restart ? 3010 : 0;
        }
        catch (Win32Exception ex)
        {
            if (ex.NativeErrorCode == 1223) return 1602;
            MessageBox(IntPtr.Zero, ex.Message, "无法卸载 MinamotoBoard", 0x10); return 1;
        }
        catch (Exception ex) { MessageBox(IntPtr.Zero, ex.Message, "无法卸载 MinamotoBoard", 0x10); return 1; }
    }

    private static List<string> FindProducts()
    {
        List<string> products = new List<string>();
        for (uint index = 0; ; index++)
        {
            StringBuilder code = new StringBuilder(39);
            uint result = MsiEnumRelatedProducts(UpgradeCode, 0, index, code);
            if (result == 259) break;
            if (result != 0) throw new InvalidOperationException("无法读取 Windows Installer 产品记录：" + result);
            if (MsiQueryProductState(code.ToString()) == 5) products.Add(code.ToString());
        }
        return products;
    }
}
