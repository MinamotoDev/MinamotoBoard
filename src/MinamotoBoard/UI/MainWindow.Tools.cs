using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MinamotoBoard.Ink;

namespace MinamotoBoard.UI
{
    public partial class MainWindow
    {
        private readonly Dictionary<BoardTool, DrawingToolOptions> toolOptions = new Dictionary<BoardTool, DrawingToolOptions>();
        private readonly Dictionary<BoardTool, Button> toolButtons = new Dictionary<BoardTool, Button>();
        private BoardTool activeTool = BoardTool.Pen;
        private Button previousPageButton, nextPageButton, addPageButton, settingsButton;
        private StrokeCollection watchedInk;
        private long inkRevision;
        private CancellationTokenSource fillWork;

        private Button IconButton(FrameworkElement icon, string label, RoutedEventHandler click)
        {
            Button button = new Button { Content = icon, ToolTip = label };
            AutomationProperties.SetName(button, label); button.Click += click;
            ToolsPanel.Children.Add(button); return button;
        }

        private void BuildToolbar()
        {
            foreach (BoardTool kind in Enum.GetValues(typeof(BoardTool)))
            {
                BoardTool selected = kind;
                toolOptions.Add(kind, new DrawingToolOptions { Width = kind == BoardTool.Eraser ? 32 : 4 });
                Button button = IconButton(ToolbarIcons.Tool(kind), ToolbarIcons.Name(kind), delegate { SelectTool(selected); });
                toolButtons.Add(kind, button);
                BindOptionsGesture(button, delegate { return selected; }, false);
            }
            previousPageButton = IconButton(ToolbarIcons.Glyph("\uE760"), "上一页", OnPreviousPage);
            nextPageButton = IconButton(ToolbarIcons.Glyph("\uE761"), "下一页", OnNextPage);
            addPageButton = IconButton(ToolbarIcons.Glyph("\uE710"), "加页", OnAddPage);
            IconButton(ToolbarIcons.Glyph("\uE74D"), "清屏", OnClearPage);
            IconButton(ToolbarIcons.Glyph("\uE70D"), "收起工具栏", OnHideToolbar);
            settingsButton = IconButton(ToolbarIcons.Glyph("\uE713"), "设置", OnSettings);
            IconButton(ToolbarIcons.Glyph("\uE711"), "关闭", OnClose);
            BindOptionsGesture(ShowToolbarButton, delegate { return activeTool; }, true);
            Board.StrokeErasing += delegate(object sender, InkCanvasStrokeErasingEventArgs e)
            { if (e.Stroke is RegionFillStroke) e.Cancel = true; };
            Board.PreviewMouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.StylusDevice != null) return;
                if (activeTool == BoardTool.Bucket) { e.Handled = true; FillAt(e.GetPosition(Board)); }
                else if (activeTool == BoardTool.Eraser) EraseFillAt(e.GetPosition(Board));
            };
            Board.PreviewMouseMove += delegate(object sender, MouseEventArgs e)
            { if (e.StylusDevice == null && e.LeftButton == MouseButtonState.Pressed && activeTool == BoardTool.Eraser) EraseFillAt(e.GetPosition(Board)); };
            Board.PreviewStylusDown += delegate(object sender, StylusDownEventArgs e)
            {
                if (activeTool == BoardTool.Bucket) { e.Handled = true; FillAt(e.GetPosition(Board)); }
                else if (e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus && (activeTool == BoardTool.Eraser || e.Inverted))
                    EraseFillAt(e.GetPosition(Board));
            };
            Board.PreviewStylusMove += delegate(object sender, StylusEventArgs e)
            { if (!e.InAir && (activeTool == BoardTool.Eraser || e.Inverted)) EraseFillAt(e.GetPosition(Board)); };
            Board.PreviewTouchDown += delegate(object sender, TouchEventArgs e)
            {
                Point p = e.GetTouchPoint(Board).Position;
                if (activeTool == BoardTool.Bucket) { e.Handled = true; FillAt(p); }
                else if (activeTool == BoardTool.Eraser) EraseFillAt(p);
            };
            Board.PreviewTouchMove += delegate(object sender, TouchEventArgs e)
            { if (activeTool == BoardTool.Eraser) EraseFillAt(e.GetTouchPoint(Board).Position); };
            SelectTool(BoardTool.Pen);
        }

        private void SelectTool(BoardTool kind)
        {
            if (IsWriting()) return;
            CancelToolWork(); ToolOptionsPopup.IsOpen = false; activeTool = kind;
            ApplyToolOptions();
            foreach (KeyValuePair<BoardTool, Button> pair in toolButtons)
                pair.Value.Background = pair.Key == kind ? new SolidColorBrush(Color.FromRgb(216, 239, 222)) : Brushes.Transparent;
            ShowToolbarButton.Content = ToolbarIcons.Tool(kind);
            AutomationProperties.SetName(ShowToolbarButton, "显示工具栏 · " + ToolbarIcons.Name(kind));
        }

        private void ApplyToolOptions()
        {
            DrawingToolOptions settings = toolOptions[activeTool];
            if (activeTool == BoardTool.Eraser) { tools.SetEraserWidth(settings.Width); tools.UseEraser(); }
            else if (activeTool == BoardTool.Bucket)
            { Board.EditingMode = InkCanvasEditingMode.None; Board.EditingModeInverted = InkCanvasEditingMode.None; }
            else { Board.DefaultDrawingAttributes = settings.Attributes(); tools.UsePen(); }
            Instruments.Activate(activeTool, settings);
        }

        private void ShowOptions(Button source, BoardTool kind)
        {
            if (IsWriting()) return;
            SelectTool(kind);
            ToolOptionsPanel.Children.Clear();
            ToolOptionsPanel.Children.Add(new TextBlock { Text = ToolbarIcons.Name(kind), FontSize = 16, FontWeight = FontWeights.SemiBold });
            DrawingToolOptions settings = toolOptions[kind];
            if (kind != BoardTool.Eraser)
            {
                WrapPanel palette = new WrapPanel { Margin = new Thickness(0, 12, 0, 4) };
                Color[] colors = { Colors.White, Color.FromRgb(255, 213, 79), Color.FromRgb(244, 67, 54),
                    Color.FromRgb(66, 165, 245), Color.FromRgb(76, 175, 80), Colors.Black };
                string[] labels = { "白色", "黄色", "红色", "蓝色", "绿色", "黑色" };
                for (int i = 0; i < colors.Length; i++)
                {
                    Color color = colors[i];
                    RadioButton choice = new RadioButton { Content = new Ellipse { Width = 24, Height = 24,
                        Fill = new SolidColorBrush(color), Stroke = Brushes.Gray, StrokeThickness = 1 },
                        IsChecked = settings.Color == color, GroupName = "ToolPalette", Margin = new Thickness(0, 0, 10, 6), ToolTip = labels[i] };
                    AutomationProperties.SetName(choice, labels[i]);
                    choice.Checked += delegate { settings.Color = color; ApplyToolOptions(); };
                    palette.Children.Add(choice);
                }
                ToolOptionsPanel.Children.Add(palette);
            }
            if (kind != BoardTool.Bucket)
                AddSlider("粗细", kind == BoardTool.Eraser ? 8 : 1, kind == BoardTool.Eraser ? 128 : 40,
                    settings.Width, delegate(double value) { settings.Width = value; ApplyToolOptions(); });
            if (kind != BoardTool.Bucket && kind != BoardTool.Eraser)
            {
                ToolOptionsPanel.Children.Add(new TextBlock { Text = "样式", Margin = new Thickness(0, 10, 0, 4) });
                ComboBox style = new ComboBox { ItemsSource = kind == BoardTool.Pen ? new[] { "圆头", "方头" } : new[] { "实线", "虚线" },
                    SelectedIndex = settings.Style, MinHeight = 36 };
                style.SelectionChanged += delegate { settings.Style = style.SelectedIndex; ApplyToolOptions(); };
                ToolOptionsPanel.Children.Add(style);
            }
            if (kind == BoardTool.Ruler) AddSlider("长度", 160, 640, Instruments.Length, delegate(double value) { Instruments.Length = value; });
            if (kind == BoardTool.Compass) AddSlider("半径", 20, 400, Instruments.Radius, delegate(double value) { Instruments.Radius = value; });
            if (kind == BoardTool.Ruler || kind == BoardTool.SetSquare)
                AddSlider("角度", -180, 180, Instruments.Angle, delegate(double value) { Instruments.Angle = value; });
            if (kind == BoardTool.SetSquare)
            {
                ComboBox triangle = new ComboBox { ItemsSource = new[] { "45° 三角板", "30° / 60° 三角板" },
                    SelectedIndex = Instruments.Triangle30 ? 1 : 0, MinHeight = 36, Margin = new Thickness(0, 12, 0, 0) };
                triangle.SelectionChanged += delegate { Instruments.Triangle30 = triangle.SelectedIndex == 1; };
                ToolOptionsPanel.Children.Add(triangle);
            }
            ToolOptionsPopup.PlacementTarget = source; ToolOptionsPopup.IsOpen = true;
        }

        private void AddSlider(string name, double min, double max, double value, Action<double> change)
        {
            ToolOptionsPanel.Children.Add(new TextBlock { Text = name, Margin = new Thickness(0, 10, 0, 4) });
            Slider slider = new Slider { Minimum = min, Maximum = max, Value = Math.Max(min, Math.Min(max, value)),
                IsSnapToTickEnabled = false, IsMoveToPointEnabled = true, MinHeight = 36, SmallChange = 1, LargeChange = 5 };
            AutomationProperties.SetName(slider, name);
            slider.ValueChanged += delegate { change(slider.Value); };
            ToolOptionsPanel.Children.Add(slider);
        }

        // Capture an individual touch/pen gesture so vertical swipes work outside the button bounds.
        // Promoted mouse events are ignored, avoiding a second selection after a touch gesture.
        private void BindOptionsGesture(Button button, Func<BoardTool> tool, bool collapsed)
        {
            Point start = new Point(); int pointer = 0; bool swiped = false; TouchDevice touch = null;
            Action<Point> move = delegate(Point p)
            {
                Vector delta = p - start;
                if (!swiped && delta.Y < -28 && Math.Abs(delta.Y) > Math.Abs(delta.X) * 1.2)
                { swiped = true; ShowOptions(button, tool()); }
            };
            Action tap = delegate { if (collapsed) SetToolbarVisible(true); else SelectTool(tool()); };
            button.PreviewMouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            { if (e.StylusDevice == null) { start = e.GetPosition(RootLayout); pointer = 1; swiped = false; } };
            button.PreviewMouseMove += delegate(object sender, MouseEventArgs e)
            { if (pointer == 1 && e.LeftButton == MouseButtonState.Pressed) { move(e.GetPosition(RootLayout)); if (swiped) { button.ReleaseMouseCapture(); e.Handled = true; } } };
            button.PreviewMouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
            { if (pointer == 1) { if (swiped) e.Handled = true; pointer = 0; } };
            button.PreviewTouchDown += delegate(object sender, TouchEventArgs e)
            { if (pointer == 0 || pointer == 1) { pointer = 3; start = e.GetTouchPoint(RootLayout).Position; swiped = false; touch = e.TouchDevice; touch.Capture(button); e.Handled = true; } };
            button.PreviewTouchMove += delegate(object sender, TouchEventArgs e)
            { if (pointer == 3 && touch == e.TouchDevice) { move(e.GetTouchPoint(RootLayout).Position); e.Handled = true; } };
            button.PreviewTouchUp += delegate(object sender, TouchEventArgs e)
            {
                if (pointer != 3 || touch != e.TouchDevice) return;
                bool select = !swiped; pointer = 0; touch.Capture(null); touch = null; e.Handled = true;
                if (select) tap();
            };
            button.PreviewStylusDown += delegate(object sender, StylusDownEventArgs e)
            {
                if (e.StylusDevice.TabletDevice.Type != TabletDeviceType.Stylus || pointer > 1) return;
                pointer = 2; start = e.GetPosition(RootLayout); swiped = false; button.CaptureStylus(); e.Handled = true;
            };
            button.PreviewStylusMove += delegate(object sender, StylusEventArgs e)
            { if (pointer == 2) { move(e.GetPosition(RootLayout)); e.Handled = true; } };
            button.PreviewStylusUp += delegate(object sender, StylusEventArgs e)
            { if (pointer == 2) { bool select = !swiped; pointer = 0; button.ReleaseStylusCapture(); e.Handled = true; if (select) tap(); } };
            button.LostTouchCapture += delegate(object sender, TouchEventArgs e)
            { if (pointer == 3 && touch == e.TouchDevice) { pointer = 0; touch = null; } };
            button.LostStylusCapture += delegate { if (pointer == 2) pointer = 0; };
            button.LostMouseCapture += delegate { if (pointer == 1) pointer = 0; };
            button.MouseRightButtonUp += delegate(object sender, MouseButtonEventArgs e) { ShowOptions(button, tool()); e.Handled = true; };
            button.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
            { if (e.Key == Key.Up || e.Key == Key.Apps) { ShowOptions(button, tool()); e.Handled = true; } };
        }

        private void TrackInk()
        {
            if (watchedInk != null) watchedInk.StrokesChanged -= OnInkChanged;
            watchedInk = Board.Strokes; watchedInk.StrokesChanged += OnInkChanged; inkRevision++;
        }
        private void OnInkChanged(object sender, StrokeCollectionChangedEventArgs e) { inkRevision++; }
        private void CancelToolWork() { if (fillWork != null) fillWork.Cancel(); }
        private void EraseFillAt(Point point)
        {
            List<Stroke> remove = new List<Stroke>();
            foreach (Stroke stroke in Board.Strokes)
            {
                RegionFillStroke fill = stroke as RegionFillStroke;
                double radius = toolOptions[BoardTool.Eraser].Width / 2;
                if (fill != null && fill.Region.FillContainsWithDetail(new EllipseGeometry(point, radius, radius)) != IntersectionDetail.Empty)
                    remove.Add(fill);
            }
            foreach (Stroke stroke in remove) Board.Strokes.Remove(stroke);
        }

        private async void FillAt(Point point)
        {
            if (fillWork != null || Board.ActualWidth < 2 || Board.ActualHeight < 2) return;
            CancellationTokenSource work = new CancellationTokenSource(); fillWork = work;
            StrokeCollection target = Board.Strokes; long revision = inkRevision; Color color = toolOptions[BoardTool.Bucket].Color;
            try
            {
                RegionFillMask mask = RegionFill.Capture(target, Board.ActualWidth, Board.ActualHeight);
                Geometry region = await Task.Run(() => RegionFill.Find(mask, point, work.Token));
                if (work.IsCancellationRequested || !IsLoaded || target != Board.Strokes || revision != inkRevision) return;
                if (region == null) { ShowToolHint("请选择封闭区域"); return; }
                List<RegionFillStroke> old = new List<RegionFillStroke>();
                foreach (Stroke stroke in target)
                {
                    RegionFillStroke fill = stroke as RegionFillStroke;
                    if (fill != null && fill.Region.Bounds.IntersectsWith(region.Bounds)) old.Add(fill);
                }
                foreach (RegionFillStroke fill in old)
                {
                    // A newly drawn line may split a previously painted region. Preserve its other half.
                    CombinedGeometry remainder = new CombinedGeometry(GeometryCombineMode.Exclude, fill.Region, region);
                    remainder.Freeze();
                    int index = target.IndexOf(fill);
                    target.Remove(fill);
                    if (remainder.GetArea() > 0.1) target.Insert(index, new RegionFillStroke(remainder, fill.DrawingAttributes.Color));
                }
                target.Insert(0, new RegionFillStroke(region, color));
            }
            catch (OperationCanceledException) { }
            catch (Exception) { if (IsLoaded) ShowToolHint("无法填充此区域"); }
            finally { if (fillWork == work) fillWork = null; work.Dispose(); }
        }

        private void ShowToolHint(string text)
        {
            ToolTip tip = new ToolTip { Content = text, PlacementTarget = Toolbar.Visibility == Visibility.Visible
                ? toolButtons[activeTool] : ShowToolbarButton, Placement = PlacementMode.Top, IsOpen = true };
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += delegate { tip.IsOpen = false; timer.Stop(); }; timer.Start();
        }
    }
}
