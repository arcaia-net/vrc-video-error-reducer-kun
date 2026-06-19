using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace VrcVideoErrorReducerKun
{
    public sealed class FirewallService
    {
        private const string RuleName = "VRCVideoErrorReducerKun-yt-dlp-IPv6-Block";
        private const string DisplayName = "VRChat yt-dlp IPv6 Block";
        private const string GroupName = "VRC Video Error Reducer Kun";

        public async Task<FirewallStatus> GetStatusAsync(string targetPath)
        {
            string firewallProgramPath = PathResolver.ResolveFirewallProgramPath(targetPath);
            PowerShellResult result = await PowerShellRunner.RunAsync(BuildStatusScript(targetPath, firewallProgramPath));
            if (result.ExitCode != 0)
            {
                return new FirewallStatus
                {
                    Kind = FirewallStatusKind.Error,
                    DisplayText = "エラー",
                    Message = "ファイアウォール設定の確認に失敗しました。",
                    TargetPath = targetPath,
                    Details = CombineOutput(result)
                };
            }

            try
            {
                return ParseStatus(result.StandardOutput, targetPath);
            }
            catch (Exception ex)
            {
                return new FirewallStatus
                {
                    Kind = FirewallStatusKind.Error,
                    DisplayText = "エラー",
                    Message = "ファイアウォール設定の確認結果を読み取れませんでした。",
                    TargetPath = targetPath,
                    Details = ex.Message + Environment.NewLine + result.StandardOutput
                };
            }
        }

        public async Task EnableAsync(string targetPath)
        {
            if (!File.Exists(targetPath))
            {
                throw new FileNotFoundException("VRChatの動画再生に必要なコンポーネントが見つかりません。", targetPath);
            }

            string firewallProgramPath = PathResolver.ResolveFirewallProgramPath(targetPath);
            PowerShellResult result = await PowerShellRunner.RunAsync(BuildEnableScript(targetPath, firewallProgramPath));
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(CombineOutput(result));
            }
        }

        public async Task DisableAsync()
        {
            PowerShellResult result = await PowerShellRunner.RunAsync(BuildDisableScript());
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(CombineOutput(result));
            }
        }

        private static FirewallStatus ParseStatus(string output, string targetPath)
        {
            string json = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Last();
            var serializer = new JavaScriptSerializer();
            var values = serializer.Deserialize<Dictionary<string, object>>(json);

            return new FirewallStatus
            {
                Kind = ParseKind(GetString(values, "Kind")),
                DisplayText = GetString(values, "DisplayText"),
                Message = GetString(values, "Message"),
                TargetPath = targetPath,
                ComponentExists = GetBool(values, "ComponentExists"),
                ManagedRuleExists = GetBool(values, "ManagedRuleExists"),
                CanEnable = GetBool(values, "CanEnable"),
                CanDisable = GetBool(values, "CanDisable"),
                Details = GetString(values, "Details")
            };
        }

        private static FirewallStatusKind ParseKind(string value)
        {
            FirewallStatusKind kind;
            return Enum.TryParse(value, out kind) ? kind : FirewallStatusKind.Unknown;
        }

        private static string GetString(Dictionary<string, object> values, string key)
        {
            object value;
            return values.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : string.Empty;
        }

        private static bool GetBool(Dictionary<string, object> values, string key)
        {
            object value;
            return values.TryGetValue(key, out value) && value != null && Convert.ToBoolean(value);
        }

        private static string BuildStatusScript(string targetPath, string firewallProgramPath)
        {
            var script = new StringBuilder();
            AppendPreamble(script);
            AppendConstants(script, targetPath, firewallProgramPath);
            AppendFunctions(script);
            script.AppendLine("$componentExists = Test-Path -LiteralPath $TargetPath -PathType Leaf");
            script.AppendLine("$managedRules = @(Get-ManagedRules)");
            script.AppendLine("$kind = 'NotConfigured'");
            script.AppendLine("$displayText = '未設定'");
            script.AppendLine("$message = 'ファイアウォール設定はまだ追加されていません。'");
            script.AppendLine("$details = @()");
            script.AppendLine("$canDisable = $managedRules.Count -gt 0");
            script.AppendLine("if (-not $componentExists) {");
            script.AppendLine("  $kind = 'ComponentMissing'");
            script.AppendLine("  $displayText = 'コンポーネント未検出'");
            script.AppendLine("  $message = 'VRChatの動画再生に必要なコンポーネントが見つかりません。VRChatを一度起動してから、もう一度このアプリを実行してください。'");
            script.AppendLine("  $details += ('yt-dlp.exe が見つかりませんでした（検索パス: ' + $TargetPath + '）')");
            script.AppendLine("} elseif ($managedRules.Count -gt 0) {");
            script.AppendLine("  $rule = $managedRules[0]");
            script.AppendLine("  $appFilter = $rule | Get-NetFirewallApplicationFilter");
            script.AppendLine("  $addressFilter = $rule | Get-NetFirewallAddressFilter");
            script.AppendLine("  $portFilter = $rule | Get-NetFirewallPortFilter");
            script.AppendLine("  $remoteAddresses = @($addressFilter.RemoteAddress)");
            script.AppendLine("  $expectedDescription = Get-ExpectedDescription");
            script.AppendLine("  $checks = [ordered]@{");
            script.AppendLine("    Name = ($rule.Name -eq $RuleName)");
            script.AppendLine("    DisplayName = ($rule.DisplayName -eq $DisplayName)");
            script.AppendLine("    Group = ($rule.Group -eq $GroupName)");
            script.AppendLine("    Enabled = ($rule.Enabled -eq 'True')");
            script.AppendLine("    Direction = ($rule.Direction -eq 'Outbound')");
            script.AppendLine("    Action = ($rule.Action -eq 'Block')");
            script.AppendLine("    Program = ($appFilter.Program -eq $FirewallProgramPath)");
            script.AppendLine("    RemoteAddress = (Test-RemoteAddress $remoteAddresses)");
            script.AppendLine("    Protocol = ($portFilter.Protocol -eq 'Any')");
            script.AppendLine("    Description = ($rule.Description -eq $expectedDescription)");
            script.AppendLine("  }");
            script.AppendLine("  foreach ($key in $checks.Keys) { if (-not $checks[$key]) { $details += ($key + ' が期待値と一致しません。') } }");
            script.AppendLine("  if (@($checks.GetEnumerator() | Where-Object { -not $_.Value }).Count -eq 0) {");
            script.AppendLine("    $kind = 'Configured'");
            script.AppendLine("    $displayText = '設定済み'");
            script.AppendLine("    $message = 'IPv6ブロック設定が有効です。'");
            script.AppendLine("  } else {");
            script.AppendLine("    $kind = 'Incomplete'");
            script.AppendLine("    $displayText = '不完全な設定'");
            script.AppendLine("    $message = 'ファイアウォール設定の一部が期待値と異なります。'");
            script.AppendLine("  }");
            script.AppendLine("}");
            script.AppendLine("[pscustomobject]@{");
            script.AppendLine("  Kind = $kind");
            script.AppendLine("  DisplayText = $displayText");
            script.AppendLine("  Message = $message");
            script.AppendLine("  ComponentExists = [bool]$componentExists");
            script.AppendLine("  ManagedRuleExists = [bool]($managedRules.Count -gt 0)");
            script.AppendLine("  CanEnable = [bool]$componentExists");
            script.AppendLine("  CanDisable = [bool]$canDisable");
            script.AppendLine("  Details = [string]::Join([Environment]::NewLine, $details)");
            script.AppendLine("} | ConvertTo-Json -Compress");
            return script.ToString();
        }

        private static string BuildEnableScript(string targetPath, string firewallProgramPath)
        {
            var script = new StringBuilder();
            AppendPreamble(script);
            AppendConstants(script, targetPath, firewallProgramPath);
            AppendFunctions(script);
            script.AppendLine("if (-not (Test-Path -LiteralPath $TargetPath -PathType Leaf)) { throw ('対象ファイルが見つかりません: ' + $TargetPath) }");
            script.AppendLine("$rules = @(Get-ManagedRules)");
            script.AppendLine("foreach ($rule in $rules) { $rule | Remove-NetFirewallRule -ErrorAction Stop }");
            script.AppendLine("New-NetFirewallRule -Name $RuleName -DisplayName $DisplayName -Group $GroupName -Description (Get-ExpectedDescription) -Direction Outbound -Program $FirewallProgramPath -RemoteAddress @('::/1','8000::/1') -Action Block -Enabled True -Profile Domain,Private,Public -Protocol Any -ErrorAction Stop | Out-Null");
            return script.ToString();
        }

        private static string BuildDisableScript()
        {
            var script = new StringBuilder();
            AppendPreamble(script);
            AppendConstants(script, string.Empty, string.Empty);
            AppendFunctions(script);
            script.AppendLine("$rules = @(Get-ManagedRules)");
            script.AppendLine("foreach ($rule in $rules) { $rule | Remove-NetFirewallRule -ErrorAction Stop }");
            return script.ToString();
        }

        private static void AppendPreamble(StringBuilder script)
        {
            script.AppendLine("$ErrorActionPreference = 'Stop'");
            script.AppendLine("[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding -ArgumentList $false");
            script.AppendLine("$OutputEncoding = [Console]::OutputEncoding");
        }

        private static void AppendConstants(StringBuilder script, string targetPath, string firewallProgramPath)
        {
            script.AppendLine("$RuleName = " + PsQuote(RuleName));
            script.AppendLine("$DisplayName = " + PsQuote(DisplayName));
            script.AppendLine("$GroupName = " + PsQuote(GroupName));
            script.AppendLine("$TargetPath = " + PsQuote(targetPath));
            script.AppendLine("$FirewallProgramPath = " + PsQuote(firewallProgramPath));
        }

        private static void AppendFunctions(StringBuilder script)
        {
            script.AppendLine("function Get-ExpectedDescription {");
            script.AppendLine("  \"Blocks outbound IPv6 traffic for the yt-dlp.exe bundled with VRChat.`nTarget: $FirewallProgramPath`nManaged by VRC Video Error Reducer Kun.\"");
            script.AppendLine("}");
            script.AppendLine("function Get-ManagedRules {");
            script.AppendLine("  @(Get-NetFirewallRule -Name $RuleName -ErrorAction SilentlyContinue)");
            script.AppendLine("}");
            script.AppendLine("function Test-RemoteAddress($actual) {");
            script.AppendLine("  $expected = @('::/1','8000::/1')");
            script.AppendLine("  $actualValues = @($actual | ForEach-Object { [string]$_ })");
            script.AppendLine("  if ($actualValues.Count -ne $expected.Count) { return $false }");
            script.AppendLine("  return @(Compare-Object -ReferenceObject $expected -DifferenceObject $actualValues).Count -eq 0");
            script.AppendLine("}");
        }

        private static string PsQuote(string value)
        {
            return "'" + (value ?? string.Empty).Replace("'", "''") + "'";
        }

        private static string CombineOutput(PowerShellResult result)
        {
            return (result.StandardOutput + Environment.NewLine + result.StandardError).Trim();
        }
    }
}
