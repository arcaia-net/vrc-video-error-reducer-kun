using System;
using System.Windows;
using System.Windows.Media;

namespace VrcVideoErrorReducerKun
{
    public partial class MainWindow : Window
    {
        private readonly string targetPath;
        private readonly FirewallService firewallService = new FirewallService();
        private FirewallStatus currentStatus;

        public MainWindow(string targetPath)
        {
            this.targetPath = targetPath;
            InitializeComponent();
            Title = AppInfo.WindowTitle;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AddLog("アプリを起動しました。");
            await RefreshStatusAsync();
        }

        private async void EnableButton_Click(object sender, RoutedEventArgs e)
        {
            await RunOperationAsync("設定を有効にしています...", async () =>
            {
                await firewallService.EnableAsync(targetPath);
                AddLog("ルールを追加または更新しました。");
            });
        }

        private async void DisableButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "IPv6ブロック設定を削除しますか?",
                Title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            await RunOperationAsync("設定を削除しています...", async () =>
            {
                await firewallService.DisableAsync();
                AddLog("ルールを削除しました。");
            });
        }

        private async System.Threading.Tasks.Task RunOperationAsync(string message, Func<System.Threading.Tasks.Task> operation)
        {
            SetBusy(true);
            MessageText.Text = message;
            AddLog(message);

            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                AddLog("失敗: " + ex.Message);
                MessageBox.Show(
                    "処理に失敗しました。コマンドプロンプトなどから実行している場合は、標準出力を確認してください。",
                    Title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                await RefreshStatusAsync();
                SetBusy(false);
            }
        }

        private async System.Threading.Tasks.Task RefreshStatusAsync()
        {
            SetBusy(true);
            currentStatus = await firewallService.GetStatusAsync(targetPath);
            ApplyStatus(currentStatus);
            AddStatusLog(currentStatus);
            SetBusy(false);
        }

        private void ApplyStatus(FirewallStatus status)
        {
            StatusText.Text = status.DisplayText;
            MessageText.Text = status.Message;
            StatusText.Foreground = GetStatusBrush(status.Kind);
            EnableButton.IsEnabled = status.CanEnable;
            DisableButton.IsEnabled = status.CanDisable;

            if (!string.IsNullOrWhiteSpace(status.Details))
            {
                AddLog(status.Details);
            }
        }

        private void AddStatusLog(FirewallStatus status)
        {
            if (status.ComponentExists)
            {
                AddLog("yt-dlp.exe を検出しました。");
            }
        }

        private void SetBusy(bool isBusy)
        {
            if (currentStatus == null)
            {
                EnableButton.IsEnabled = false;
                DisableButton.IsEnabled = false;
                return;
            }

            EnableButton.IsEnabled = !isBusy && currentStatus.CanEnable;
            DisableButton.IsEnabled = !isBusy && currentStatus.CanDisable;
        }

        private void AddLog(string message)
        {
            ConsoleLogger.WriteLines(message);
        }

        private static Brush GetStatusBrush(FirewallStatusKind kind)
        {
            switch (kind)
            {
                case FirewallStatusKind.Configured:
                    return new SolidColorBrush(Color.FromRgb(26, 127, 55));
                case FirewallStatusKind.NotConfigured:
                    return new SolidColorBrush(Color.FromRgb(207, 34, 46));
                case FirewallStatusKind.ComponentMissing:
                case FirewallStatusKind.Incomplete:
                    return new SolidColorBrush(Color.FromRgb(154, 103, 0));
                default:
                    return new SolidColorBrush(Color.FromRgb(87, 96, 106));
            }
        }
    }
}
