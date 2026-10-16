using System.Collections.Generic;
using System.Windows.Ink;
using System.Windows.Media;

namespace MinamotoBoard.Models
{
    public sealed class BoardPage
    {
        public StrokeCollection Strokes { get; set; }
        public Brush Background { get; set; }

        public BoardPage(StrokeCollection strokes, Brush background)
        {
            Strokes = strokes;
            Background = background;
        }
    }

    // Pages retain native stroke collections, never screenshots or full-canvas copies.
    public sealed class BoardDocument
    {
        public const int MaximumPages = 100;
        private readonly List<BoardPage> pages = new List<BoardPage>();
        public int CurrentIndex { get; private set; }
        public int Count { get { return pages.Count; } }
        public BoardPage CurrentPage { get { return pages[CurrentIndex]; } }
        public bool CanMovePrevious { get { return CurrentIndex > 0; } }
        public bool CanMoveNext { get { return CurrentIndex + 1 < Count; } }
        public bool CanAddPage { get { return Count < MaximumPages; } }

        public bool HasInk
        {
            get
            {
                foreach (BoardPage page in pages)
                    if (page.Strokes.Count > 0) return true;
                return false;
            }
        }

        public BoardDocument(StrokeCollection initialStrokes, Brush initialBackground)
        {
            pages.Add(new BoardPage(initialStrokes, initialBackground));
        }

        public bool AddPage()
        {
            if (!CanAddPage) return false;
            // A separate background brush prevents edits to a mutable plugin brush
            // on the new page from changing the preceding page's background.
            Brush background = CurrentPage.Background == null ? null : CurrentPage.Background.CloneCurrentValue();
            if (background != null && background.CanFreeze) background.Freeze();
            pages.Insert(CurrentIndex + 1, new BoardPage(new StrokeCollection(), background));
            CurrentIndex++;
            return true;
        }

        public bool MovePrevious()
        {
            if (!CanMovePrevious) return false;
            CurrentIndex--;
            return true;
        }

        public bool MoveNext()
        {
            if (!CanMoveNext) return false;
            CurrentIndex++;
            return true;
        }
    }
}
