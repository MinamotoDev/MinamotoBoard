MinamotoBoard 0.2.0

默认安装位置：C:\Program Files\MinamotoDev\Board
从开始菜单打开 MinamotoBoard，或双击此目录中的 MinamotoBoard.exe。
安装需要管理员授权，正常运行不要求管理员。
目标环境：Windows 10 22H2 x64，.NET Framework 4.8 或更高版本。

默认绿板和白笔，底部可以换色、改笔宽、切换橡皮。
F11 切换全屏，Esc 退出全屏，Ctrl+Tab 隐藏/显示工具栏。
当前仍为基础白板预览版，没有保存功能，退出后笔迹会丢失。

Plugins：
点击底部 Plugins 打开管理窗口，可打开插件目录与保存启用设置。
通常位于 %USERPROFILE%\Documents\MinamotoDev\Board\Plugins。
实际使用 Windows 文档位置；重定向的文档目录也受支持。
放入 *.Plugin.dll（以及旁边的依赖）后重启生效。
插件目录和配置属于当前用户，MSI 卸载不会删除它们。
仅安装可信插件：插件能修改界面、笔迹，并具有当前用户的文件和网络权限。
插件故障时可使用 MinamotoBoard.exe --safe-mode 启动，不执行插件代码。
开发接口和更多说明见此目录中的 PLUGINS.md。

软件渲染兼容模式：MinamotoBoard.exe --software-rendering
真实触屏、旧电脑性能和学校 Windows 10 环境仍需现场验证。
