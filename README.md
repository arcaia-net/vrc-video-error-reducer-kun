# VRCの動画再生エラーを軽減してくれるクン

VRChatに同梱されている `yt-dlp.exe` のIPv6アウトバウンド通信だけを、Windows Defender ファイアウォールのルールでブロックするGUIツールです。

常駐はせず、起動して「設定を有効にする」または「設定を削除する」を実行したら、ウィンドウを閉じて終了できます。

## 対象環境

- Windows 11
- .NET Framework 4.8
- 管理者でアプリを実行できるアカウント

## 対象ファイル

初期リリースでは、次のVRChat標準パスのみを対象にします。

```text
%USERPROFILE%\AppData\LocalLow\VRChat\VRChat\Tools\yt-dlp.exe
```

対象ファイルが見つからない場合、ファイアウォールルールは作成しません。

## 作成するファイアウォールルール

| 項目 | 値 |
| --- | --- |
| Name | `VRCVideoErrorReducerKun-yt-dlp-IPv6-Block` |
| DisplayName | `VRChat yt-dlp IPv6 Block` |
| Group | `VRC Video Error Reducer Kun` |
| Direction | Outbound |
| Program | `%USERPROFILE%\AppData\LocalLow\VRChat\VRChat\Tools\yt-dlp.exe` |
| RemoteAddress | `::/1`, `8000::/1` |
| Action | Block |
| Profile | Domain, Private, Public |
| Protocol | Any |

IPv4通信やVRChat本体の通信はブロックしません。

## アンインストール

1. アプリを起動します。
2. 「設定を削除する」を押して、作成済みのファイアウォールルールを削除します。
3. アプリの `.exe` を削除します。

`.exe` を削除しただけでは、作成済みのファイアウォールルールは残ります。

## 手動削除

アプリを使わずに削除する場合は、管理者権限のPowerShellで次を実行してください。

```powershell
Remove-NetFirewallRule -Name "VRCVideoErrorReducerKun-yt-dlp-IPv6-Block"
```

## 開発

このリポジトリの開発には、CodeX と Claude Code を利用しています。
セキュリティ的な問題がないか、PCに致命的な影響を与えないかといった確認は行っていますが、予期せぬバグが含まれている可能性があるため、自己責任でのご利用をお願いします。
ご協力していただける方は issue を投稿していただけると幸いです。
