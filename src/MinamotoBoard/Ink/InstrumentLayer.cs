using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;

namespace MinamotoBoard.Ink
{
    // Instances are session-only. Saved states are templates for the next Add action.
    public sealed class InstrumentLayer : Grid
    {
        private readonly Dictionary<BoardTool, InstrumentState> templates = new Dictionary<BoardTool, InstrumentState>();
        private BoardTool writingTool = BoardTool.Pen;
        private DrawingToolOptions writingOptions = new DrawingToolOptions();
        public event Action<StrokeCollection> InkCreated;
        public event Action<bool> PointerInput;
        public bool IsInteracting
        {
            get { foreach (InstrumentOverlay item in Children) if (item.IsInteracting) return true; return false; }
        }
        public static bool IsInstrument(BoardTool tool)
        { return tool == BoardTool.Ruler || tool == BoardTool.SetSquare || tool == BoardTool.SetSquare30 || tool == BoardTool.Compass; }

        public void Activate(BoardTool tool, DrawingToolOptions options)
        {
            writingTool = tool; writingOptions = options;
            foreach (InstrumentOverlay item in Children) item.SetWritingTool(tool, options);
        }

        public void AddInstrument(BoardTool kind)
        {
            if (!IsInstrument(kind) || IsInteracting) return;
            InstrumentState saved;
            if (!templates.TryGetValue(kind, out saved)) saved = new InstrumentState { Tool = kind, Triangle30 = kind == BoardTool.SetSquare30 };
            InstrumentState state = Copy(saved);
            // Keep repeated additions accessible instead of placing all handles on top of each other.
            foreach (InstrumentOverlay old in Children)
            {
                InstrumentState other = old.ExportStates()[0];
                if (state.Positioned && other.Positioned && Math.Abs(state.X-other.X) < 20 && Math.Abs(state.Y-other.Y) < 20)
                { state.X += 28; state.Y += 28; }
            }
            InstrumentOverlay item = new InstrumentOverlay();
            item.ImportStates(new List<InstrumentState> { state });
            item.InkCreated += delegate(StrokeCollection strokes) { if (InkCreated != null) InkCreated(strokes); };
            item.PointerInput += delegate(bool touch) { if (PointerInput != null) PointerInput(touch); };
            item.StateChanged += delegate { Remember(item); };
            item.DeleteRequested += delegate { Remember(item); Children.Remove(item); };
            Children.Add(item);
            item.Activate(kind, writingOptions); item.SetWritingTool(writingTool, writingOptions);
            item.Loaded += delegate { Remember(item); };
        }

        private void Remember(InstrumentOverlay item)
        { foreach (InstrumentState state in item.ExportStates()) templates[state.Tool] = Copy(state); }

        public List<InstrumentState> ExportStates()
        {
            foreach (InstrumentOverlay item in Children) if (item.IsInteracting) Remember(item);
            return new List<InstrumentState>(templates.Values);
        }
        public void ImportStates(List<InstrumentState> states)
        {
            if (states == null) return;
            foreach (InstrumentState state in states)
            {
                if (state == null || !IsInstrument(state.Tool) || !ToolPreferences.Finite(state.X) || !ToolPreferences.Finite(state.Y)
                    || !ToolPreferences.Finite(state.Angle) || !ToolPreferences.Finite(state.Length)
                    || !ToolPreferences.Finite(state.Scale) || !ToolPreferences.Finite(state.Radius)) continue;
                InstrumentState saved = Copy(state);
                if (saved.Tool == BoardTool.SetSquare && saved.Triangle30) saved.Tool = BoardTool.SetSquare30;
                saved.Length = Math.Max(640, saved.Length); saved.Scale = Math.Max(1, saved.Scale); saved.Radius = Math.Max(0, saved.Radius);
                templates[saved.Tool] = saved;
            }
        }
        public void CancelInteraction()
        { foreach (InstrumentOverlay item in Children) item.CancelInteraction(); }
        private static InstrumentState Copy(InstrumentState state)
        {
            return new InstrumentState { Tool = state.Tool, Positioned = state.Positioned, X = state.X, Y = state.Y,
                Angle = state.Angle, Length = state.Length, Scale = state.Scale, Radius = state.Radius, Triangle30 = state.Triangle30 };
        }
    }
}
