using Haiku.App;
using Haiku.Interface;

namespace EmptyWindow;

public class App: BApplication
{
    private readonly BWindow _mainWin;

    public App()
        : base("application/x-vnd.demo-app")
    {
        // The only thing that happens when you start this application is
        // a window is created.
        _mainWin = new MainWindow();

        // Now that it has been created, show it!
        _mainWin.Show();
    }
}