using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using MinamotoBoard.Ink;
using MinamotoBoard.UI;

namespace MinamotoBoard.Tests
{
    internal static class Program
    {
        private static int passed;

        [STAThread]
        private static int Main()
        {
            try
            {
                Run("Default pen: white, 4 DIP, circular, pressure independent", Defaults);
                Run("New settings do not modify existing strokes", ExistingInk);
                Run("Tool transitions preserve ink and selected pen", ToolTransitions);
                Run("Invalid widths leave settings intact", InvalidWidths);
                Run("Native point erasure splits a stroke and preserves its style", PointErasure);
                Run("Window starts fullscreen with green board and valid controls", WindowDefaults);
                Run("Plugin changes UI and adds working feature; shutdown cleans up", PluginTests.LoadAndCleanup);
                Run("Disabled plugins and safe mode do not execute plugin code", PluginTests.DisabledAndSafeMode);
                Run("Broken plugin and invalid DLL do not prevent healthy loading", PluginTests.FailureIsolation);
                Run("Duplicate plugin IDs are rejected without duplicate UI", PluginTests.DuplicateIds);
                Run("Managed toolbar callback exceptions are contained", PluginTests.CallbackFailure);
                Console.WriteLine("PASS: {0}/11 scenarios", passed);
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }

        private static void Run(string name, Action test)
        {
            test(); passed++; Console.WriteLine("PASS: " + name);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static Stroke Line(DrawingAttributes attributes)
        {
            return new Stroke(new StylusPointCollection(new[]
            {
                new StylusPoint(0, 100), new StylusPoint(100, 100), new StylusPoint(200, 100)
            }), attributes.Clone());
        }

        private static void Defaults()
        {
            InkCanvas board = new InkCanvas(); new InkTools(board);
            DrawingAttributes pen = board.DefaultDrawingAttributes;
            Check(pen.Color == Colors.White && pen.Width == 4 && pen.Height == 4, "Wrong defaults");
            Check(pen.StylusTip == StylusTip.Ellipse && pen.IgnorePressure, "Wrong pen tip");
            Check(board.EditingMode == InkCanvasEditingMode.Ink, "Not in ink mode");
            Check(board.EraserShape.Width == 32, "Wrong eraser default");
        }

        private static void ExistingInk()
        {
            InkCanvas board = new InkCanvas(); InkTools tools = new InkTools(board);
            Stroke stroke = Line(board.DefaultDrawingAttributes); board.Strokes.Add(stroke);
            tools.SetColor(Colors.Red); tools.SetPenWidth(16);
            Check(stroke.DrawingAttributes.Color == Colors.White && stroke.DrawingAttributes.Width == 4,
                "Existing ink changed after selecting a new pen");
            Check(board.DefaultDrawingAttributes.Color == Colors.Red && board.DefaultDrawingAttributes.Width == 16,
                "New pen settings were not applied");
        }

        private static void ToolTransitions()
        {
            InkCanvas board = new InkCanvas(); InkTools tools = new InkTools(board);
            board.Strokes.Add(Line(board.DefaultDrawingAttributes));
            tools.SetColor(Colors.Yellow); tools.SetPenWidth(6); tools.UseEraser(); tools.SetEraserWidth(64);
            Check(board.EditingMode == InkCanvasEditingMode.EraseByPoint && board.EraserShape.Width == 64,
                "Eraser mode/size was not set");
            tools.UsePen();
            Check(board.Strokes.Count == 1 && board.EditingMode == InkCanvasEditingMode.Ink, "Switch lost ink");
            Check(board.DefaultDrawingAttributes.Color == Colors.Yellow && board.DefaultDrawingAttributes.Width == 6,
                "Switch lost selected pen");
        }

        private static void InvalidWidths()
        {
            InkCanvas board = new InkCanvas(); InkTools tools = new InkTools(board);
            bool penRejected = false, eraserRejected = false;
            try { tools.SetPenWidth(double.NaN); } catch (ArgumentOutOfRangeException) { penRejected = true; }
            try { tools.SetEraserWidth(-1); } catch (ArgumentOutOfRangeException) { eraserRejected = true; }
            Check(penRejected && eraserRejected, "Invalid size accepted");
            Check(board.DefaultDrawingAttributes.Width == 4 && board.EraserShape.Width == 32, "State damaged");
        }

        private static void PointErasure()
        {
            InkCanvas board = new InkCanvas(); InkTools tools = new InkTools(board);
            tools.SetColor(Colors.Blue); tools.SetPenWidth(6);
            board.Strokes.Add(Line(board.DefaultDrawingAttributes));
            board.Strokes.Erase(new[] { new Point(100, 90), new Point(100, 110) }, board.EraserShape);
            Check(board.Strokes.Count == 2, "Erasure did not split stroke");
            foreach (Stroke stroke in board.Strokes)
                Check(stroke.DrawingAttributes.Color == Colors.Blue && stroke.DrawingAttributes.Width == 6,
                    "Erasure changed remaining ink style");
        }

        private static void WindowDefaults()
        {
            MainWindow window = new MainWindow();
            InkCanvas board = (InkCanvas)window.FindName("Board");
            Check(window.WindowStyle == WindowStyle.None && window.WindowState == WindowState.Maximized,
                "Not fullscreen");
            Check(((SolidColorBrush)board.Background).Color == (Color)ColorConverter.ConvertFromString("#236B45"),
                "Wrong background");
            Check(((RadioButton)window.FindName("PenButton")).IsChecked == true, "Pen not selected");
            Check(((RadioButton)window.FindName("WhiteButton")).IsChecked == true, "White not selected");
            Check((double)((ComboBox)window.FindName("PenWidth")).SelectedItem == 4, "Wrong width selection");
            Check(window.Title.Contains("MinamotoBoard"), "Wrong app name");
            window.Close();
        }
    }
}
