using StreamVideo_Client.WinForms;

namespace StreamVideo_Client
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new FormConnect());
        }
    }
}
