using System.Windows;

namespace TypeOver;

static class Program
{
    [STAThread]
    static void Main() => new Application().Run(new MainWindow());
}
