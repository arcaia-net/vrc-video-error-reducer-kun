using System;
using System.Reflection;

namespace VrcVideoErrorReducerKun
{
    public static class AppInfo
    {
        public const string DisplayName = "VRCの動画再生エラーを軽減してくれるクン";

        public static string VersionText
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version == null)
                {
                    return "v0.0.0";
                }

                return "v" + version.Major + "." + version.Minor + "." + version.Build;
            }
        }

        public static string WindowTitle
        {
            get { return DisplayName + " " + VersionText; }
        }
    }
}
