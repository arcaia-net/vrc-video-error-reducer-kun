using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Windows;

namespace VrcVideoErrorReducerKun
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ConsoleLogger.Initialize();

            string targetPath = GetTargetPathFromArguments(e.Args) ?? PathResolver.ResolveDefaultYtDlpPath();

            if (!IsAdministrator())
            {
                TryRestartAsAdministrator(targetPath);
                Shutdown();
                return;
            }

            var window = new MainWindow(targetPath);
            MainWindow = window;
            window.Show();
        }

        private static bool IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private static string GetTargetPathFromArguments(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "--target-base64", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        return Encoding.UTF8.GetString(Convert.FromBase64String(args[i + 1]));
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            return null;
        }

        private static void TryRestartAsAdministrator(string targetPath)
        {
            try
            {
                string encodedPath = Convert.ToBase64String(Encoding.UTF8.GetBytes(targetPath));
                var startInfo = new ProcessStartInfo
                {
                    FileName = Process.GetCurrentProcess().MainModule.FileName,
                    Arguments = "--target-base64 " + encodedPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(startInfo);
            }
            catch
            {
                MessageBox.Show(
                    "管理者権限が必要です。",
                    AppInfo.WindowTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
