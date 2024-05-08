using System.Diagnostics;

namespace Cybertron;

public static partial class GenStatic
{
    public static class Platform
    {
        /// <summary>
        /// Gets the path of the executable with or without the .exe extension
        /// </summary>
        /// <param name="path">Path to the executable with or without the extension</param>
        public static void ExecutablePath(ref string path)
        {
            if (OperatingSystem.IsWindows())
            {
                if (!path.EndsWith(".exe"))
                {
                    path += ".exe";
                }
            }
            else
            {
                if (path.EndsWith(".exe"))
                {
                    path = path[..^4];
                }
            }
        }
        
        /// <inheritdoc cref="ExecutablePath(ref string)"/>
        public static string ExecutablePath(string path)
        {
            if (OperatingSystem.IsWindows())
            {
                if (!path.EndsWith(".exe"))
                {
                    path += ".exe";
                }
            }
            else
            {
                if (path.EndsWith(".exe"))
                {
                    path = path[..^4];
                }
            }
            return path;
        }
        
        public static Process TerminalProcess(ProcessStartInfo processStartInfo)
        {
            if (OperatingSystem.IsWindows())
                processStartInfo.FileName = "cmd.exe";
            else if (OperatingSystem.IsLinux())
                processStartInfo.FileName = "/bin/bash";
            else if (OperatingSystem.IsMacOS())
                processStartInfo.FileName = "/Applications/Utilities/Terminal.app";

            var terminalProcess = new Process
            {
                StartInfo = processStartInfo
            };
        
            return terminalProcess;
        }
    
        public static Process TerminalProcess()
        {
            var processStartInfo = new ProcessStartInfo();
            return TerminalProcess(processStartInfo);
        }
    }
}