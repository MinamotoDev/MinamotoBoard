MinamotoBoard 0.6.0

安装目录：C:\Program Files\MinamotoDev\Board
安装与卸载语言：自动识别 Windows 显示语言，支持English、日本語、简体中文和正體中文，其他语言使用英语。
运行环境：Windows 10 22H2 x64 或更高版本，.NET Framework 4.8 或更高版本。
从开始菜单打开 MinamotoBoard。

加页、上一页、下一页：管理白板页面，最多 100 页。
工具图标上滑：打开颜色、样式和粗细滑杆；鼠标右键也可打开。
尺规图标：新增尺规，画笔或荧光笔沿边绘制；红色按钮删除。
直尺和三角板：拖动尺身移动，蓝色手柄缩放和旋转。
圆规：拖动圆心移动，蓝色手柄调整半径，沿虚线圆周画弧。
油漆桶：只填封闭区域。
工具栏收起后，点击左下角圆形工具按钮展开。
清屏：清空当前页面的全部墨迹，无法撤销。
F11：切换全屏；Esc：退出全屏；Ctrl+Tab：显示或隐藏工具栏。
当前没有保存功能，退出后墨迹将丢失。

Plugins：
在“设置 → 插件”中管理插件，修改在重启后生效。
插件目录：Windows 文档\MinamotoDev\Board\Plugins。
插件启用设置：%LOCALAPPDATA%\MinamotoDev\Board。
仅使用可信插件。开发说明见 PLUGINS.md。
安全模式：MinamotoBoard.exe --safe-mode
软件渲染：MinamotoBoard.exe --software-rendering

卸载：
在 Windows 设置或控制面板中选择 MinamotoBoard，或运行安装目录的 Uninstall.exe。
确认页的“删除配置和插件”默认不勾选。
勾选后，卸载成功时删除当前用户的上述两个目录。

许可证：GPL-3.0-only，见 LICENSE。
第三方声明：THIRD-PARTY-NOTICES.md、WixUI-LICENSE.txt。
