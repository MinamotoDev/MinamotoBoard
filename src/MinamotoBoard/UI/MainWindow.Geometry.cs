using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using MinamotoBoard.Ink;
using MinamotoBoard.Localization;

namespace MinamotoBoard.UI
{
    public partial class MainWindow
    {
        private static bool IsGeometryTool(BoardTool tool)
        { return tool == BoardTool.Shapes2D || tool == BoardTool.Shapes3D; }

        private static GeometryShape ValidShape(GeometryShape shape, bool solid)
        {
            // Map retired palette entries when restoring older preferences.
            if (shape == GeometryShape.Ellipse) shape = GeometryShape.Circle;
            if (shape == GeometryShape.Rectangle) shape = GeometryShape.Square;
            if (shape == GeometryShape.Cuboid) shape = GeometryShape.Cube;
            return Enum.IsDefined(typeof(GeometryShape), shape) && GeometryShapes.IsSolid(shape) == solid
                ? shape : solid ? GeometryShape.Cube : GeometryShape.Square;
        }

        private void AddShapeChoices(BoardTool kind)
        {
            ToolOptionsPanel.Children.Add(new TextBlock { Text = Localizer.T("图形"), Margin = new Thickness(0, 10, 0, 8) });
            WrapPanel grid = new WrapPanel();
            GeometryShape selected = kind == BoardTool.Shapes3D ? ValidShape(preferences.Shape3D, true) : ValidShape(preferences.Shape2D, false);
            foreach (GeometryShape shape in Enum.GetValues(typeof(GeometryShape)))
            {
                if (GeometryShapes.IsSolid(shape) != (kind == BoardTool.Shapes3D)) continue;
                if (ValidShape(shape, GeometryShapes.IsSolid(shape)) != shape) continue;
                GeometryShape choice = shape;
                string name = Localizer.T(GeometryShapes.Name(shape));
                DrawingGroup drawing = new DrawingGroup();
                DrawingGroup content = new DrawingGroup();
                double iconWidth = shape == GeometryShape.Arc ? 32 : 44;
                double iconHeight = shape == GeometryShape.Arc ? 16 : 40;
                Rect bounds = Rect.Empty;
                foreach (GeometryShapes.ShapePath path in GeometryShapes.Build(shape, new Point(), new Point(iconWidth, iconHeight)))
                {
                    foreach (Point point in path.Points) bounds.Union(point);
                    StreamGeometry geometry = new StreamGeometry();
                    using (StreamGeometryContext context = geometry.Open())
                    {
                        context.BeginFigure(path.Points[0], false, false);
                        for (int i = 1; i < path.Points.Count; i++) context.LineTo(path.Points[i], true, false);
                    }
                    Pen pen = new Pen(new SolidColorBrush(Color.FromRgb(23, 63, 43)), 1.5);
                    if (path.Dashed) pen.DashStyle = DashStyles.Dash;
                    content.Children.Add(new GeometryDrawing(null, pen, geometry));
                }
                content.Transform = new TranslateTransform((52 - bounds.Width) / 2 - bounds.X, (48 - bounds.Height) / 2 - bounds.Y);
                drawing.Children.Add(content);
                // Stable viewport keeps small shapes and lines equally sized.
                drawing.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 52, 48))));
                drawing.Freeze();
                Button button = new Button { Content = new Image { Source = new DrawingImage(drawing), Width = 44, Height = 40 },
                    Width = 58, Height = 56, MinWidth = 0, MinHeight = 0, Margin = new Thickness(3), Padding = new Thickness(5),
                    ToolTip = name, Background = selected == shape ? new SolidColorBrush(Color.FromRgb(216, 239, 222)) : Brushes.Transparent };
                AutomationProperties.SetName(button, name);
                button.Click += delegate
                {
                    if (kind == BoardTool.Shapes3D) preferences.Shape3D = choice; else preferences.Shape2D = choice;
                    ApplyToolOptions();
                    foreach (Button item in grid.Children) item.Background = Brushes.Transparent;
                    button.Background = new SolidColorBrush(Color.FromRgb(216, 239, 222));
                };
                grid.Children.Add(button);
            }
            ToolOptionsPanel.Children.Add(grid);
        }
        private void SetShapeGuides(BoardTool kind, bool enabled)
        {
            if (kind == BoardTool.Shapes3D) preferences.ShapeGuides3D = enabled; else preferences.ShapeGuides2D = enabled;
            ApplyToolOptions();
        }
        private void SetStandardShapes(BoardTool kind, bool enabled)
        {
            if (kind == BoardTool.Shapes3D) preferences.StandardShapes3D = enabled; else preferences.StandardShapes2D = enabled;
            ApplyToolOptions();
        }
    }
}
