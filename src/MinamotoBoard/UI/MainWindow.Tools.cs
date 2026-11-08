using MinamotoBoard.Localization;
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
        private BoardTool lastWritingTool = BoardTool.Pen;
        private Button previousPageButton, nextPageButton, addPageButton, settingsButton, undoButton, redoButton;
        private StrokeCollection watchedInk;
        private long inkRevision;
        private readonly Dictionary<StrokeCollection, PageInk> inkHistory = new Dictionary<StrokeCollection, PageInk>();
        private bool historyApplying;
        private bool historyCommitPosted;
        private bool inkGesture;
        private CancellationTokenSource fillWork;
        private ToolPreferences preferences;
        private readonly Dictionary<int, Point> fillEraserPoints = new Dictionary<int, Point>();

        private void RestoreToolPreferences()
        {
            // Preferences and language are loaded before the visual tree is created.
            if (preferences.Tools == null) preferences.Tools = new List<ToolPreference>();
            foreach (ToolPreference saved in preferences.Tools ?? new List<ToolPreference>())
            {
                DrawingToolOptions value;
                if (saved == null || !toolOptions.TryGetValue(saved.Tool, out value)) continue;
                try { value.Color = (Color)ColorConverter.ConvertFromString(saved.Color); } catch (Exception) { }
                if (ToolPreferences.Finite(saved.Width)) value.Width = Math.Max(saved.Tool == BoardTool.Eraser ? 8 : 1, Math.Min(saved.Tool == BoardTool.Eraser ? 128 : 40, saved.Width));
                value.Style = saved.Style == 1 && (saved.Tool == BoardTool.Pen || saved.Tool == BoardTool.Highlighter || saved.Tool == BoardTool.Eraser) ? 1 : 0;
            }
            Instruments.ImportStates(preferences.Instruments);
            if (ToolPreferences.Finite(preferences.WindowWidth)) Width = Math.Max(MinWidth, Math.Min(SystemParameters.VirtualScreenWidth, preferences.WindowWidth));
            if (ToolPreferences.Finite(preferences.WindowHeight)) Height = Math.Max(MinHeight, Math.Min(SystemParameters.VirtualScreenHeight, preferences.WindowHeight));
            ApplyBetaToolVisibility();
            SelectTool(BoardTool.Pen);
            Loaded += delegate
            {
                BoardTool selected = preferences.Selected;
                if (!toolOptions.ContainsKey(selected) || InstrumentLayer.IsInstrument(selected) || !ToolEnabled(selected)) selected = BoardTool.Pen;
                SelectTool(selected);
            };
        }

        private bool ToolEnabled(BoardTool kind)
        {
            if (kind == BoardTool.Highlighter) return preferences.HighlighterEnabled;
            if (kind == BoardTool.Bucket) return preferences.BucketEnabled;
            return true;
        }

        private void ApplyBetaToolVisibility()
        {
            Button button;
            if (toolButtons.TryGetValue(BoardTool.Highlighter, out button)) button.Visibility = preferences.HighlighterEnabled ? Visibility.Visible : Visibility.Collapsed;
            if (toolButtons.TryGetValue(BoardTool.Bucket, out button)) button.Visibility = preferences.BucketEnabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnBetaToolsChanged()
        {
            CancelAdaptiveErasing();
            ApplyBetaToolVisibility();
            if (!ToolEnabled(activeTool)) SelectTool(BoardTool.Pen);
            ApplyToolOptions();
            SaveToolPreferences();
        }

        private void SaveToolPreferences()
        {
            preferences.Language = Localizer.CurrentLanguage;
            preferences.Selected = activeTool;
            preferences.Instruments = Instruments.ExportStates();
            preferences.Tools.Clear();
            foreach (KeyValuePair<BoardTool, DrawingToolOptions> pair in toolOptions)
                preferences.Tools.Add(new ToolPreference { Tool = pair.Key, Color = pair.Value.Color.ToString(), Width = pair.Value.Width, Style = pair.Value.Style });
            Rect bounds = RestoreBounds;
            if (!bounds.IsEmpty) { preferences.WindowWidth = bounds.Width; preferences.WindowHeight = bounds.Height; }
            preferences.Save();
        }

        private readonly Dictionary<Button, string> buttonLabels = new Dictionary<Button, string>();

        private Button IconButton(FrameworkElement icon, string label, RoutedEventHandler click)
        {
            Button button = new Button { Content = icon, ToolTip = Localizer.T(label) };
            buttonLabels.Add(button, label);
            AutomationProperties.SetName(button, Localizer.T(label)); button.Click += click;
            ToolsPanel.Children.Add(button); return button;
        }

        private void BuildToolbar()
        {
            foreach (BoardTool kind in new[] { BoardTool.Pen, BoardTool.Highlighter, BoardTool.Eraser, BoardTool.Ruler, BoardTool.SetSquare, BoardTool.SetSquare30, BoardTool.Compass, BoardTool.Bucket })
            {
                BoardTool selected = kind;
                toolOptions.Add(kind, new DrawingToolOptions { Width = kind == BoardTool.Eraser ? 32 : kind == BoardTool.Highlighter ? 20 : 4, Highlighter = kind == BoardTool.Highlighter, Color = kind == BoardTool.Highlighter ? Color.FromRgb(255, 213, 79) : Colors.White });
                Button button = IconButton(ToolbarIcons.Tool(kind), ToolbarIcons.Name(kind), delegate { SelectTool(selected); });
                toolButtons.Add(kind, button);
                if (!InstrumentLayer.IsInstrument(kind)) BindOptionsGesture(button, delegate { return selected; }, false);
            }
            ToolbarLeft.Content = ToolbarIcons.Glyph("\uE76B"); ToolbarRight.Content = ToolbarIcons.Glyph("\uE76C");
            previousPageButton = IconButton(ToolbarIcons.Glyph("\uE760"), "上一页", OnPreviousPage);
            nextPageButton = IconButton(ToolbarIcons.Glyph("\uE761"), "下一页", OnNextPage);
            addPageButton = IconButton(ToolbarIcons.Glyph("\uE710"), "加页", OnAddPage);
            IconButton(ToolbarIcons.Glyph("\uE74D"), "清屏", OnClearPage);
            undoButton = IconButton(ToolbarIcons.Glyph("\uE7A7"), "撤销", OnUndo);
            redoButton = IconButton(ToolbarIcons.Glyph("\uE7A6"), "重做", OnRedo);
            IconButton(ToolbarIcons.Glyph("\uE70D"), "收起工具栏", OnHideToolbar);
            settingsButton = IconButton(ToolbarIcons.Glyph("\uE713"), "设置", OnSettings);
            IconButton(ToolbarIcons.Glyph("\uE711"), "关闭", OnClose);
            BindOptionsGesture(ShowToolbarButton, delegate { return activeTool; }, true);
            BindAdaptiveEraser();
            Board.StrokeErasing += delegate(object sender, InkCanvasStrokeErasingEventArgs e)
            { if (e.Stroke is RegionFillStroke) e.Cancel = true; };
            Board.PreviewMouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.StylusDevice != null) return;
                BeginInkGesture();
                if (activeTool == BoardTool.Bucket) { e.Handled = true; FillAt(e.GetPosition(Board)); }
                else if (activeTool == BoardTool.Eraser) EraseFillAt(e.GetPosition(Board), int.MinValue, true);
            };
            Board.PreviewMouseMove += delegate(object sender, MouseEventArgs e)
            { if (e.StylusDevice == null && e.LeftButton == MouseButtonState.Pressed && activeTool == BoardTool.Eraser) EraseFillAt(e.GetPosition(Board), int.MinValue, false); };
            Board.PreviewMouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.StylusDevice != null) return;
                EndInkGesture();
                if (activeTool == BoardTool.Eraser && fillEraserPoints.ContainsKey(int.MinValue)) EraseFillAt(e.GetPosition(Board), int.MinValue, false);
                fillEraserPoints.Remove(int.MinValue);
            };
            Board.LostMouseCapture += delegate
            {
                fillEraserPoints.Remove(int.MinValue);
                EndInkGesture();
            };
            Board.PreviewStylusDown += delegate(object sender, StylusDownEventArgs e)
            {
                BeginInkGesture();
                if (activeTool == BoardTool.Bucket) { e.Handled = true; FillAt(e.GetPosition(Board)); }
                else if (e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus && (activeTool == BoardTool.Eraser || e.Inverted))
                    EraseFillAt(e.GetPosition(Board), -1 - e.StylusDevice.Id, true);
            };
            Board.PreviewStylusMove += delegate(object sender, StylusEventArgs e)
            {
                if (e.StylusDevice.TabletDevice.Type != TabletDeviceType.Stylus || e.InAir || (activeTool != BoardTool.Eraser && !e.Inverted)) return;
                foreach (StylusPoint point in e.GetStylusPoints(Board)) EraseFillAt((Point)point, -1 - e.StylusDevice.Id, false);
            };
            Board.PreviewStylusUp += delegate(object sender, StylusEventArgs e)
            {
                EndInkGesture();
                int pointer = -1 - e.StylusDevice.Id;
                if (e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus && fillEraserPoints.ContainsKey(pointer))
                    foreach (StylusPoint point in e.GetStylusPoints(Board)) EraseFillAt((Point)point, pointer, false);
                fillEraserPoints.Remove(pointer);
            };
            Board.LostStylusCapture += delegate(object sender, StylusEventArgs e)
            {
                fillEraserPoints.Remove(-1 - e.StylusDevice.Id);
                EndInkGesture();
            };
            Board.PreviewTouchDown += delegate(object sender, TouchEventArgs e)
            {
                BeginInkGesture();
                Point p = e.GetTouchPoint(Board).Position;
                if (activeTool == BoardTool.Bucket) { e.Handled = true; FillAt(p); }
                else if (activeTool == BoardTool.Eraser) EraseFillAt(p, e.TouchDevice.Id + 1, true);
            };
            Board.PreviewTouchMove += delegate(object sender, TouchEventArgs e)
            {
                if (activeTool != BoardTool.Eraser) return;
                foreach (TouchPoint point in e.GetIntermediateTouchPoints(Board)) EraseFillAt(point.Position, e.TouchDevice.Id + 1, false);
                EraseFillAt(e.GetTouchPoint(Board).Position, e.TouchDevice.Id + 1, false);
            };
            Board.PreviewTouchUp += delegate(object sender, TouchEventArgs e)
            {
                int pointer = e.TouchDevice.Id + 1;
                if (activeTool == BoardTool.Eraser && fillEraserPoints.ContainsKey(pointer))
                {
                    foreach (TouchPoint point in e.GetIntermediateTouchPoints(Board)) EraseFillAt(point.Position, pointer, false);
                    EraseFillAt(e.GetTouchPoint(Board).Position, pointer, false);
                }
                fillEraserPoints.Remove(pointer);
                EndInkGesture();
            };
            Board.LostTouchCapture += delegate(object sender, TouchEventArgs e)
            {
                fillEraserPoints.Remove(e.TouchDevice.Id + 1);
                EndInkGesture();
            };
            RestoreToolPreferences();
        }

        private void SelectTool(BoardTool kind)
        {
            if (IsWriting()) return;
            fillEraserPoints.Clear();
            if (InstrumentLayer.IsInstrument(kind))
            {
                if (activeTool != BoardTool.Pen && activeTool != BoardTool.Highlighter) SelectTool(lastWritingTool);
                ToolOptionsPopup.IsOpen = false; Instruments.AddInstrument(kind); return;
            }
            if (!ToolEnabled(kind)) return;
            if (kind == BoardTool.Pen || kind == BoardTool.Highlighter) lastWritingTool = kind;
            CancelAdaptiveErasing();
            CancelToolWork(); ToolOptionsPopup.IsOpen = false; activeTool = kind;
            ApplyToolOptions();
            foreach (KeyValuePair<BoardTool, Button> pair in toolButtons)
                pair.Value.Background = pair.Key == kind ? new SolidColorBrush(Color.FromRgb(216, 239, 222)) : Brushes.Transparent;
            ShowToolbarButton.Content = ToolbarIcons.Tool(kind);
            Label(ShowToolbarButton, Localizer.T("显示工具栏") + " · " + ToolbarIcons.Name(kind));
        }

        private void ApplyToolOptions()
        {
            DrawingToolOptions settings = toolOptions[activeTool];
            tools.SetEraserStyle(toolOptions[BoardTool.Eraser].Style == 1);
            tools.SetEraserWidth(toolOptions[BoardTool.Eraser].Width);
            Board.LiveHighlighter = activeTool == BoardTool.Highlighter;
            Board.DashedInk = activeTool == BoardTool.Pen && settings.Style == 1;
            Board.InkStyle = settings.Style;
            Board.EraserCursor = activeTool == BoardTool.Eraser;
            Board.EraserDiameter = toolOptions[BoardTool.Eraser].Width;
            if (activeTool == BoardTool.Eraser) tools.UseEraser();
            else if (activeTool == BoardTool.Bucket)
            { Board.EditingMode = InkCanvasEditingMode.None; Board.EditingModeInverted = InkCanvasEditingMode.None; }
            else { Board.DefaultDrawingAttributes = settings.Attributes(); tools.UsePen(); }
            if (Board.LiveHighlighter || Board.DashedInk) Board.EditingMode = InkCanvasEditingMode.None;
            if (AdaptiveErasing)
            {
                Board.EditingMode = InkCanvasEditingMode.None;
                Board.EditingModeInverted = InkCanvasEditingMode.None;
            }
            Board.UpdateHighlighterCursor();
            Instruments.Activate(activeTool, settings);
        }

        private void ShowOptions(Button source, BoardTool kind)
        {
            if (IsWriting() || !ToolEnabled(kind)) return;
            SelectTool(kind);
            ToolOptionsPanel.Children.Clear();
            ToolOptionsPanel.Children.Add(new TextBlock { Text = ToolbarIcons.Name(kind), FontSize = 16, FontWeight = FontWeights.SemiBold });
            DrawingToolOptions settings = toolOptions[kind];
            if (kind != BoardTool.Eraser)
            {
                WrapPanel palette = new WrapPanel { Margin = new Thickness(0, 12, 0, 4) };
                Color[] colors = { Colors.White, Color.FromRgb(255, 213, 79), Color.FromRgb(244, 67, 54),
                    Color.FromRgb(66, 165, 245), Color.FromRgb(76, 175, 80), Colors.Black };
                string[] labels = { Localizer.T("白色"), Localizer.T("黄色"), Localizer.T("红色"), Localizer.T("蓝色"), Localizer.T("绿色"), Localizer.T("黑色") };
                for (int i = 0; i < colors.Length; i++)
                {
                    Color color = colors[i];
                    RadioButton choice = new RadioButton { Width = 36, Height = 36,
                        Content = new Ellipse { Width = 24, Height = 24, Fill = new SolidColorBrush(color), Stroke = Brushes.Gray, StrokeThickness = 1 },
                        IsChecked = settings.Color == color, GroupName = "ToolPalette", Margin = new Thickness(0, 0, 6, 6), ToolTip = labels[i] };
                    FrameworkElementFactory ring = new FrameworkElementFactory(typeof(Border));
                    ring.Name = "SelectionRing";
                    ring.SetValue(Border.CornerRadiusProperty, new CornerRadius(18));
                    ring.SetValue(Border.BorderThicknessProperty, new Thickness(2));
                    ring.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
                    ring.SetValue(Border.BackgroundProperty, Brushes.Transparent);
                    FrameworkElementFactory swatch = new FrameworkElementFactory(typeof(ContentPresenter));
                    swatch.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                    swatch.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                    ring.AppendChild(swatch);
                    ControlTemplate template = new ControlTemplate(typeof(RadioButton)) { VisualTree = ring };
                    Trigger selected = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
                    selected.Setters.Add(new Setter(Border.BorderBrushProperty, Brushes.DodgerBlue, "SelectionRing"));
                    template.Triggers.Add(selected);
                    Trigger hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                    hover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(225, 235, 241)), "SelectionRing"));
                    template.Triggers.Add(hover);
                    Trigger focus = new Trigger { Property = UIElement.IsKeyboardFocusedProperty, Value = true };
                    focus.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(225, 235, 241)), "SelectionRing"));
                    template.Triggers.Add(focus); choice.Template = template;
                    AutomationProperties.SetName(choice, labels[i]);
                    choice.Checked += delegate { settings.Color = color; ApplyToolOptions(); };
                    palette.Children.Add(choice);
                }
                ToolOptionsPanel.Children.Add(palette);
            }
            if (kind != BoardTool.Bucket && !(kind == BoardTool.Eraser && preferences.AdaptiveEraser))
                AddSlider(Localizer.T("粗细"), kind == BoardTool.Eraser ? 8 : 1, kind == BoardTool.Eraser ? 128 : 40,
                    settings.Width, delegate(double value) { settings.Width = value; ApplyToolOptions(); });
            if (kind == BoardTool.Pen || kind == BoardTool.Highlighter || kind == BoardTool.Eraser)
            {
                ToolOptionsPanel.Children.Add(new TextBlock { Text = Localizer.T("样式"), Margin = new Thickness(0, 10, 0, 4) });
                ComboBox style = new ComboBox { ItemsSource = kind == BoardTool.Eraser ? new[] { Localizer.T("按像素"), Localizer.T("按笔画") } : new[] { Localizer.T("实线"), Localizer.T("虚线") },
                    SelectedIndex = settings.Style, MinHeight = 36, VerticalContentAlignment = VerticalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Left };
                AlignOptions(style);
                style.SelectionChanged += delegate { settings.Style = style.SelectedIndex; ApplyToolOptions(); };
                ToolOptionsPanel.Children.Add(style);
            }
            ToolOptionsPopup.PlacementTarget = source; ToolOptionsPopup.IsOpen = true;
        }

        private static void AlignOptions(ComboBox combo)
        {
            Style item = new Style(typeof(ComboBoxItem));
            item.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            item.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
            item.Setters.Add(new Setter(Control.MinHeightProperty, 32.0));
            combo.ItemContainerStyle = item;
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
            CommitHistory();
            fillEraserPoints.Clear();
            if (watchedInk != null) watchedInk.StrokesChanged -= OnInkChanged;
            watchedInk = Board.Strokes;
            PageInk page = PageFor(watchedInk);
            page.Mirror = CopyStrokes(watchedInk);
            page.Pending = null;
            watchedInk.StrokesChanged += OnInkChanged;
            inkRevision++;
            UpdateHistoryButtons();
        }
        private void OnInkChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            inkRevision++;
            if (historyApplying) return;
            StrokeCollection strokes = (StrokeCollection)sender;
            PageInk page = PageFor(strokes);
            if (page.Pending == null) page.Pending = page.Mirror;
            page.Mirror = CopyStrokes(strokes);
            ScheduleHistoryCommit();
        }

        private void BeginInkGesture()
        {
            if (inkGesture) return;
            CommitHistory();
            inkGesture = true;
        }

        private void EndInkGesture()
        {
            if (adaptiveContacts.Count != 0) return;
            if (!inkGesture) return;
            inkGesture = false;
            ScheduleHistoryCommit();
        }

        private void ScheduleHistoryCommit()
        {
            if (historyCommitPosted) return;
            historyCommitPosted = true;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate
            {
                historyCommitPosted = false;
                if (inkGesture) return;
                CommitHistory();
            }));
        }

        private void CommitHistory()
        {
            if (watchedInk == null || historyApplying) return;
            PageInk page = PageFor(watchedInk);
            if (page.Pending == null) return;
            if (!SameStrokes(page.Pending, page.Mirror))
            {
                page.Undo.Add(page.Pending);
                if (page.Undo.Count > 100) page.Undo.RemoveAt(0);
                page.Redo.Clear();
            }
            page.Pending = null;
            UpdateHistoryButtons();
        }

        private void OnUndo(object sender, RoutedEventArgs e) { TravelHistory(true); }
        private void OnRedo(object sender, RoutedEventArgs e) { TravelHistory(false); }

        private void TravelHistory(bool undo)
        {
            if (IsWriting() || historyApplying || watchedInk == null) return;
            CommitHistory();
            PageInk page = PageFor(watchedInk);
            List<Stroke[]> source = undo ? page.Undo : page.Redo;
            List<Stroke[]> destination = undo ? page.Redo : page.Undo;
            if (source.Count == 0) return;
            Stroke[] target = source[source.Count - 1];
            source.RemoveAt(source.Count - 1);
            destination.Add(page.Mirror);
            if (destination.Count > 100) destination.RemoveAt(0);
            ApplyStrokes(watchedInk, target);
            page.Mirror = CopyStrokes(watchedInk);
            page.Pending = null;
            UpdateHistoryButtons();
            Board.Focus();
        }

        private void ApplyStrokes(StrokeCollection strokes, Stroke[] desired)
        {
            historyApplying = true;
            try
            {
                strokes.Clear();
                if (desired.Length > 0) strokes.Add(new StrokeCollection(desired));
            }
            finally { historyApplying = false; }
        }

        private void UpdateHistoryButtons()
        {
            if (undoButton == null || watchedInk == null) return;
            PageInk page = PageFor(watchedInk);
            undoButton.IsEnabled = page.Undo.Count > 0 && page.Pending == null;
            redoButton.IsEnabled = page.Redo.Count > 0 && page.Pending == null;
        }

        private PageInk PageFor(StrokeCollection strokes)
        {
            PageInk page;
            if (!inkHistory.TryGetValue(strokes, out page))
            {
                page = new PageInk();
                page.Mirror = CopyStrokes(strokes);
                inkHistory.Add(strokes, page);
            }
            return page;
        }

        private static Stroke[] CopyStrokes(StrokeCollection strokes)
        {
            Stroke[] copy = new Stroke[strokes.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = strokes[i];
            return copy;
        }

        private static bool SameStrokes(Stroke[] left, Stroke[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++) if (!ReferenceEquals(left[i], right[i])) return false;
            return true;
        }

        private sealed class PageInk
        {
            public Stroke[] Mirror;
            public Stroke[] Pending;
            public readonly List<Stroke[]> Undo = new List<Stroke[]>();
            public readonly List<Stroke[]> Redo = new List<Stroke[]>();
        }
        private void CancelToolWork() { if (fillWork != null) fillWork.Cancel(); }
        private void EraseFillAt(Point point, int pointer, bool begin)
        {
            Point previous;
            if (begin || !fillEraserPoints.TryGetValue(pointer, out previous)) previous = point;
            fillEraserPoints[pointer] = point;
            DrawingToolOptions eraser = toolOptions[BoardTool.Eraser];
            double radius = eraser.Width / 2;
            Geometry area = (point - previous).LengthSquared < 0.0001
                ? (Geometry)new EllipseGeometry(point, radius, radius)
                : new LineGeometry(previous, point).GetWidenedPathGeometry(new Pen(Brushes.Black, eraser.Width)
                    { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round });
            List<RegionFillStroke> hits = new List<RegionFillStroke>();
            foreach (Stroke stroke in Board.Strokes)
            {
                RegionFillStroke fill = stroke as RegionFillStroke;
                if (fill != null && fill.Region.Bounds.IntersectsWith(area.Bounds)
                    && fill.Region.FillContainsWithDetail(area) != IntersectionDetail.Empty) hits.Add(fill);
            }
            foreach (RegionFillStroke fill in hits)
            {
                if (eraser.Style == 1) { Board.Strokes.Remove(fill); continue; }
                Geometry remainder = Geometry.Combine(fill.Region, area, GeometryCombineMode.Exclude, null, 0.05, ToleranceType.Absolute);
                remainder.Freeze();
                int index = Board.Strokes.IndexOf(fill);
                Board.Strokes.Remove(fill);
                foreach (Geometry part in RegionFill.SplitComponents(remainder))
                    Board.Strokes.Insert(index++, new RegionFillStroke(part, fill.DrawingAttributes.Color));
            }
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
                if (region == null) { ShowToolHint(Localizer.T("请选择封闭区域")); return; }
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
                    foreach (Geometry part in RegionFill.SplitComponents(remainder))
                        target.Insert(index++, new RegionFillStroke(part, fill.DrawingAttributes.Color));
                }
                target.Insert(0, new RegionFillStroke(region, color));
            }
            catch (OperationCanceledException) { }
            catch (Exception) { if (IsLoaded) ShowToolHint(Localizer.T("无法填充此区域")); }
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
