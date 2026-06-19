using System;
using System.IO;

namespace VrcVideoErrorReducerKun
{
    public static class PathResolver
    {
        private const string UserProfileVariable = "%USERPROFILE%";

        public static string ResolveDefaultYtDlpPath()
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userProfile, "AppData", "LocalLow", "VRChat", "VRChat", "Tools", "yt-dlp.exe");
        }

        public static string ResolveFirewallProgramPath(string targetPath)
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(userProfile) || string.IsNullOrWhiteSpace(targetPath))
            {
                return targetPath;
            }

            string normalizedProfile = userProfile.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedTarget = Path.GetFullPath(targetPath);

            if (!normalizedTarget.StartsWith(normalizedProfile + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return targetPath;
            }

            return UserProfileVariable + normalizedTarget.Substring(normalizedProfile.Length);
        }
    }
}
