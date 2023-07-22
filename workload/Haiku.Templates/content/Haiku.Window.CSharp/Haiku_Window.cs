using Haiku.App;
using Haiku.Interface;
using static Haiku.App.Symbols;
using static Haiku.Interface.Symbols;

namespace HaikuNamespace;

public class Haiku_Window: BWindow
{
    public Haiku_Window()
        : base(new BRect(), "Haiku_Window", WindowType.TitledWindow, 0)
    {
        MoveTo(100, 100);
        ResizeTo(200, 200);
    }
}
