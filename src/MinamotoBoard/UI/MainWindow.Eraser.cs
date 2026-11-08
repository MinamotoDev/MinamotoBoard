using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MinamotoBoard.Ink;
using MinamotoBoard.Localization;

namespace MinamotoBoard.UI
{
    public partial class MainWindow
    {
        private sealed class EraserContact
        {
            public Point Last;
            public AdaptiveEraser Filter;
            public TouchDevice Touch;
            public StylusDevice Stylus;
            public bool Palm;
        }

        private readonly Dictionary<int, EraserContact> adaptiveContacts = new Dictionary<int, EraserContact>();
        private bool AdaptiveErasing { get { return activeTool == BoardTool.Eraser && (preferences.AdaptiveEraser || preferences.StandardEraser); } }
        private bool StandardErasing { get { return AdaptiveErasing && !preferences.AdaptiveEraser && preferences.StandardEraser; } }
        internal bool DebugMode { get; private set; }
        private readonly Dictionary<string, StandardEraserGroup> standardGroups = new Dictionary<string, StandardEraserGroup>();
        private BitmapSource eraserImage;

        private sealed class StandardEraserGroup
        {
            public Point Center;
            public Image Image;
        }

        internal void EnableDebugMode()
        {
            DebugMode = true;
            DebugIndicator.Visibility = Visibility.Visible;
        }

        private int DebugTouchCount()
        {
            if (!DebugMode || Mouse.LeftButton != MouseButtonState.Pressed ||
                Mouse.XButton2 != MouseButtonState.Pressed) return 1;
            return Mouse.XButton1 == MouseButtonState.Pressed ? 5 : 2;
        }

        private void UpdateDebugPalm(EraserContact contact)
        {
            int count = DebugTouchCount();
            // Supply a synthetic palm contact to the same detector used by real TouchPoint.Bounds.
            // Side-button release restores the saved manual size immediately.
            Rect bounds = count == 1 ? Rect.Empty : new Rect(contact.Last,
                count == 5 ? new Size(110, 72) : new Size(84, 48));
            UpdatePalmContact(contact, bounds);
            contact.Filter.Update(bounds, toolOptions[BoardTool.Eraser].Width);
        }

        private void OnEraserDebugMouseButton(object sender, MouseButtonEventArgs e)
        {
            if (!DebugMode || !AdaptiveErasing || e.StylusDevice != null ||
                (e.ChangedButton != MouseButton.XButton1 && e.ChangedButton != MouseButton.XButton2)) return;
            EraserContact contact;
            if (adaptiveContacts.TryGetValue(int.MinValue, out contact))
                MoveAdaptiveContact(int.MinValue, new[] { contact.Last });
            e.Handled = true;
        }

        private void BindAdaptiveEraser()
        {
            Board.PreviewMouseDown += OnEraserDebugMouseButton;
            Board.PreviewMouseUp += OnEraserDebugMouseButton;
            // Registered before the traditional fill handlers. Handled input never erases twice.
            Board.PreviewTouchDown += OnAdaptiveTouchDown;
            Board.PreviewTouchMove += OnAdaptiveTouchMove;
            Board.PreviewTouchUp += OnAdaptiveTouchUp;
            Board.AddHandler(LostTouchCaptureEvent, new EventHandler<TouchEventArgs>(delegate(object sender, TouchEventArgs e)
            { FinishAdaptiveContact(e.TouchDevice.Id + 1); }), true);
            Board.PreviewMouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (!AdaptiveErasing || e.StylusDevice != null) return;
                e.Handled = true;
                if (!Board.CaptureMouse()) return;
                Board.Focus();
                StartAdaptiveContact(int.MinValue, e.GetPosition(Board), Rect.Empty, null, null);
            };
            Board.PreviewMouseMove += delegate(object sender, MouseEventArgs e)
            {
                if (!AdaptiveErasing || e.StylusDevice != null) return;
                e.Handled = true;
                if (e.LeftButton != MouseButtonState.Pressed) { FinishAdaptiveContact(int.MinValue); return; }
                MoveAdaptiveContact(int.MinValue, new[] { e.GetPosition(Board) });
            };
            Board.PreviewMouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
            {
                if (!AdaptiveErasing || e.StylusDevice != null) return;
                e.Handled = true;
                MoveAdaptiveContact(int.MinValue, new[] { e.GetPosition(Board) });
                FinishAdaptiveContact(int.MinValue);
            };
            Board.AddHandler(LostMouseCaptureEvent, new MouseEventHandler(delegate
            { FinishAdaptiveContact(int.MinValue); }), true);
            Board.PreviewStylusDown += delegate(object sender, StylusDownEventArgs e)
            {
                if (!AdaptiveErasing || e.StylusDevice.TabletDevice.Type != TabletDeviceType.Stylus) return;
                e.Handled = true;
                if (!e.StylusDevice.Capture(Board)) return;
                StartAdaptiveContact(-1 - e.StylusDevice.Id, e.GetPosition(Board), Rect.Empty, null, e.StylusDevice);
            };
            Board.PreviewStylusMove += delegate(object sender, StylusEventArgs e)
            {
                if (!AdaptiveErasing || e.StylusDevice.TabletDevice.Type != TabletDeviceType.Stylus) return;
                e.Handled = true;
                if (!e.InAir) MoveAdaptiveContact(-1 - e.StylusDevice.Id, StylusPositions(e));
            };
            Board.PreviewStylusUp += delegate(object sender, StylusEventArgs e)
            {
                if (!AdaptiveErasing || e.StylusDevice.TabletDevice.Type != TabletDeviceType.Stylus) return;
                e.Handled = true;
                int pointer = -1 - e.StylusDevice.Id;
                MoveAdaptiveContact(pointer, StylusPositions(e));
                FinishAdaptiveContact(pointer);
            };
            Board.AddHandler(LostStylusCaptureEvent, new StylusEventHandler(delegate(object sender, StylusEventArgs e)
            { FinishAdaptiveContact(-1 - e.StylusDevice.Id); }), true);
            Deactivated += delegate { CancelAdaptiveErasing(); };
            Closed += delegate { CancelAdaptiveErasing(); };
            Board.StrokesReplaced += delegate { CancelAdaptiveErasing(); };
        }

        private void OnAdaptiveTouchDown(object sender, TouchEventArgs e)
        {
            if (!AdaptiveErasing) return;
            e.Handled = true;
            if (!e.TouchDevice.Capture(Board)) return;
            TouchPoint point = e.GetTouchPoint(Board);
            StartAdaptiveContact(e.TouchDevice.Id + 1, point.Position, point.Bounds, e.TouchDevice, null);
        }

        private void OnAdaptiveTouchMove(object sender, TouchEventArgs e)
        {
            if (!AdaptiveErasing) return;
            e.Handled = true;
            EraserContact contact;
            int pointer = e.TouchDevice.Id + 1;
            if (!adaptiveContacts.TryGetValue(pointer, out contact)) return;
            Rect bounds = e.GetTouchPoint(Board).Bounds;
            contact.Filter.Update(bounds, toolOptions[BoardTool.Eraser].Width);
            UpdatePalmContact(contact, bounds);
            MoveAdaptiveContact(pointer, TouchPositions(e));
        }

        private void OnAdaptiveTouchUp(object sender, TouchEventArgs e)
        {
            if (!AdaptiveErasing) return;
            e.Handled = true;
            int pointer = e.TouchDevice.Id + 1;
            // Up packets often have zero contact size. Finish with the last in-contact size.
            MoveAdaptiveContact(pointer, TouchPositions(e));
            FinishAdaptiveContact(pointer);
        }

        private IEnumerable<Point> TouchPositions(TouchEventArgs e)
        {
            foreach (TouchPoint point in e.GetIntermediateTouchPoints(Board)) yield return point.Position;
            yield return e.GetTouchPoint(Board).Position;
        }

        private IEnumerable<Point> StylusPositions(StylusEventArgs e)
        {
            foreach (StylusPoint point in e.GetStylusPoints(Board)) yield return (Point)point;
            yield return e.GetPosition(Board);
        }

        private void StartAdaptiveContact(int pointer, Point point, Rect bounds, TouchDevice touch, StylusDevice stylus)
        {
            if (adaptiveContacts.ContainsKey(pointer)) return;
            BeginInkGesture();
            double manualWidth = toolOptions[BoardTool.Eraser].Width;
            EraserContact contact = new EraserContact { Last = point, Filter = new AdaptiveEraser(manualWidth), Touch = touch, Stylus = stylus };
            if (touch != null)
            {
                contact.Filter.Update(bounds, manualWidth);
                UpdatePalmContact(contact, bounds);
            }
            adaptiveContacts.Add(pointer, contact);
            MoveAdaptiveContact(pointer, new[] { point });
        }

        private void MoveAdaptiveContact(int pointer, IEnumerable<Point> points)
        {
            EraserContact contact;
            if (!adaptiveContacts.TryGetValue(pointer, out contact)) return;
            if (pointer == int.MinValue && DebugMode) UpdateDebugPalm(contact);
            List<Point> path = new List<Point> { contact.Last };
            foreach (Point point in points)
            {
                if (!ToolPreferences.Finite(point.X) || !ToolPreferences.Finite(point.Y)) continue;
                if ((point - path[path.Count - 1]).LengthSquared > 0.0001) path.Add(point);
            }
            if (DebugMode)
                DebugIndicator.Text = "Debug Mode · " + (pointer == int.MinValue ? DebugTouchCount() : adaptiveContacts.Count)
                    + (contact.Palm ? " · " + Localizer.T("手掌") : "");
            if (StandardErasing)
            {
                foreach (Point point in path)
                {
                    contact.Last = point;
                    UpdateStandardGroups(pointer);
                }
            }
            else
            {
                contact.Last = path[path.Count - 1];
                EraseAdaptivePath(path, contact.Filter.Size);
            }
        }

        private void FinishAdaptiveContact(int pointer)
        {
            EraserContact contact;
            if (!adaptiveContacts.TryGetValue(pointer, out contact)) return;
            adaptiveContacts.Remove(pointer);
            // Remove state first: releasing capture raises another LostCapture event.
            if (contact.Touch != null) { if (contact.Touch.Captured == Board) contact.Touch.Capture(null); }
            else if (contact.Stylus != null) { if (contact.Stylus.Captured == Board) contact.Stylus.Capture(null); }
            else if (Board.IsMouseCaptured) Board.ReleaseMouseCapture();
            if (StandardErasing) UpdateStandardGroups(null);
            Board.EraserImageVisible = StandardErasing && adaptiveContacts.Count != 0;
            Board.UpdateHighlighterCursor();
            if (adaptiveContacts.Count == 0) DebugIndicator.Text = "Debug Mode";
            EndInkGesture();
        }

        private void CancelAdaptiveErasing()
        {
            foreach (int pointer in new List<int>(adaptiveContacts.Keys)) FinishAdaptiveContact(pointer);
            standardGroups.Clear();
            EraserOverlay.Children.Clear();
            Board.EraserImageVisible = false;
            Board.UpdateHighlighterCursor();
        }

        private void UpdateStandardGroups(int? movingPointer)
        {
            bool visible = adaptiveContacts.Count != 0;
            if (Board.EraserImageVisible != visible)
            {
                Board.EraserImageVisible = visible;
                Board.UpdateHighlighterCursor();
            }
            // Connected contacts within 80 DIP share one centered, double-size eraser.
            // Group membership is part of the key: a join/split never sweeps across a centroid jump.
            List<int> pending = adaptiveContacts.Keys.OrderBy(value => value).ToList();
            HashSet<string> current = new HashSet<string>();
            while (pending.Count != 0)
            {
                List<int> members = new List<int> { pending[0] };
                pending.RemoveAt(0);
                for (int i = 0; i < members.Count; i++)
                {
                    EraserContact seed = adaptiveContacts[members[i]];
                    for (int j = pending.Count - 1; j >= 0; j--)
                    {
                        EraserContact candidate = adaptiveContacts[pending[j]];
                        if (seed.Touch == null || candidate.Touch == null ||
                            (seed.Last - candidate.Last).LengthSquared > 80 * 80) continue;
                        members.Add(pending[j]);
                        pending.RemoveAt(j);
                    }
                }
                members.Sort();
                string key = string.Join(",", members);
                current.Add(key);
                Point center = new Point(members.Average(id => adaptiveContacts[id].Last.X),
                    members.Average(id => adaptiveContacts[id].Last.Y));
                bool palm = members.Count >= 2 || members.Any(id => adaptiveContacts[id].Palm);
                double width = toolOptions[BoardTool.Eraser].Width * (palm ? 2 : 1);
                Size size = new Size(width, width * 512 / 284);
                StandardEraserGroup group;
                bool existing = standardGroups.TryGetValue(key, out group);
                if (!existing)
                {
                    if (eraserImage == null)
                    {
                        eraserImage = new BitmapImage(new Uri("pack://application:,,,/Assets/Eraser.png", UriKind.Absolute));
                        eraserImage.Freeze();
                    }
                    group = new StandardEraserGroup { Center = center,
                        Image = new Image { Source = eraserImage, Stretch = Stretch.Fill, IsHitTestVisible = false } };
                    RenderOptions.SetBitmapScalingMode(group.Image, BitmapScalingMode.HighQuality);
                    standardGroups.Add(key, group);
                    EraserOverlay.Children.Add(group.Image);
                }
                group.Image.Width = size.Width;
                group.Image.Height = size.Height;
                Canvas.SetLeft(group.Image, center.X - size.Width / 2);
                Canvas.SetTop(group.Image, center.Y - size.Height / 2);
                if (movingPointer.HasValue && members.Contains(movingPointer.Value))
                    EraseAdaptivePath(existing ? new[] { group.Center, center } : new[] { center }, size);
                group.Center = center;
            }
            foreach (string key in standardGroups.Keys.Where(key => !current.Contains(key)).ToArray())
            {
                EraserOverlay.Children.Remove(standardGroups[key].Image);
                standardGroups.Remove(key);
            }
        }

        private static void UpdatePalmContact(EraserContact contact, Rect bounds)
        {
            // Some panels report one large contact, others report a cluster of fingertips.
            // Separate enter/leave thresholds prevent size flicker at the palm boundary.
            if (bounds.IsEmpty || !ToolPreferences.Finite(bounds.Width) || !ToolPreferences.Finite(bounds.Height) ||
                bounds.Width <= 1 || bounds.Height <= 1 || bounds.Width > 4096 || bounds.Height > 4096)
            { contact.Palm = false; return; }
            double longSide = Math.Max(bounds.Width, bounds.Height);
            double shortSide = Math.Min(bounds.Width, bounds.Height);
            contact.Palm = contact.Palm ? longSide >= 48 && shortSide >= 22 : longSide >= 64 && shortSide >= 28;
        }

        private void EraseAdaptivePath(IList<Point> path, Size size)
        {
            StrokeCollection strokes = Board.Strokes;
            if (strokes.Count == 0) return;
            bool byStroke = toolOptions[BoardTool.Eraser].Style == 1;
            StylusShape shape = StandardErasing ? (StylusShape)new RectangleStylusShape(size.Width, size.Height)
                : new EllipseStylusShape(size.Width, size.Height);
            Rect bounds = Rect.Empty;
            foreach (Point point in path) bounds.Union(point);
            bounds.Inflate(size.Width / 2, size.Height / 2);
            Geometry area = null;
            foreach (Stroke stroke in CopyStrokes(strokes))
            {
                RegionFillStroke fill = stroke as RegionFillStroke;
                if (fill == null)
                {
                    if (!stroke.GetBounds().IntersectsWith(bounds) || !stroke.HitTest(path, shape)) continue;
                    StrokeCollection result = byStroke ? new StrokeCollection() : stroke.GetEraseResult(path, shape);
                    Board.ApplyAdaptiveErase(stroke, result);
                    continue;
                }
                if (!fill.Region.Bounds.IntersectsWith(bounds)) continue;
                if (area == null) area = AdaptiveEraser.Sweep(path, size, StandardErasing);
                if (fill.Region.FillContainsWithDetail(area) == IntersectionDetail.Empty) continue;
                StrokeCollection remaining = new StrokeCollection();
                if (!byStroke)
                {
                    Geometry remainder = Geometry.Combine(fill.Region, area, GeometryCombineMode.Exclude, null, 0.05, ToleranceType.Absolute);
                    remainder.Freeze();
                    foreach (Geometry part in RegionFill.SplitComponents(remainder)) remaining.Add(new RegionFillStroke(part, fill.DrawingAttributes.Color));
                }
                if (remaining.Count == 0) strokes.Remove(fill);
                else strokes.Replace(fill, remaining);
            }
        }
    }
}
