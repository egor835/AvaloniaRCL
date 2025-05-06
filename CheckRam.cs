using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RCL;

public class CheckRam
{
    public static ulong GetTotalPhysicalMemory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWindowsMemory()/1024L/1024L;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetLinuxMemory()/1024L/1024L;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return GetMacMemory()/1024L/1024L;
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported OS");
        }
    }

    private static ulong GetWindowsMemory()
    {
        MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(memStatus))
        {
            return memStatus.ullTotalPhys;
        }
        else
        {
            throw new Exception("Unable to get memory status");
        }
    }

    private static ulong GetLinuxMemory()
    {
        foreach (var line in System.IO.File.ReadLines("/proc/meminfo"))
        {
            if (line.StartsWith("MemTotal:"))
            {
                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (ulong.TryParse(parts[1], out ulong kb))
                {
                    return kb * 1024;
                }
            }
        }
        throw new Exception("Could not read total memory from /proc/meminfo");
    }

    private static ulong GetMacMemory()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "sysctl",
            Arguments = "hw.memsize",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        string[] parts = output.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && ulong.TryParse(parts[1].Trim(), out ulong bytes))
        {
            return bytes;
        }

        throw new Exception("Could not get memory from sysctl");
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
}
