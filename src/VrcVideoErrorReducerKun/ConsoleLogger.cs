using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace VrcVideoErrorReducerKun
{
    public static class ConsoleLogger
    {
        private const uint AttachParentProcess = 0xFFFFFFFF;
        private static bool initialized;
        private static bool attached;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint processId);

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            try
            {
                attached = AttachConsole(AttachParentProcess);
                if (!attached)
                {
                    return;
                }

                Console.OutputEncoding = new UTF8Encoding(false);
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true });
                Console.SetError(new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false)) { AutoFlush = true });
            }
            catch
            {
                attached = false;
            }
        }

        public static void WriteLines(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Initialize();
            if (!attached)
            {
                return;
            }

            string[] lines = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + line);
            }
        }
    }
}
