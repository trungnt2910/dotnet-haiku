using Haiku.App;
using Haiku.Interface;
using static Haiku.App.Symbols;
using static Haiku.Interface.Symbols;

namespace HaikuNamespace;

public class MainWindow: BWindow
{
    public MainWindow()
        : base(new BRect(), "Main Window", WindowType.TitledWindow, B_QUIT_ON_WINDOW_CLOSE)
    {
        MoveTo(100, 100);
        ResizeTo(200, 200);
    }

    public override bool QuitRequested()
    {
        be_app.PostMessage(B_QUIT_REQUESTED);
        return true;
    }
}
