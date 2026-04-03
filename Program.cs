using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;

namespace PAYETAXCalc;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            ComWrappersSupport.InitializeComWrappers();

            // Bootstrap the Windows App Runtime for unpackaged (MSI) deployment.
            // Skipped only when the process has a confirmed MSIX package identity
            // (i.e. running from VS with MSIX packaging), because the OS already
            // registers the COM servers via the package manifest in that case.
            if (!IsPackaged())
                BootstrapRuntime();

            Application.Start(p =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
        catch (Exception ex)
        {
            WriteStartupLog(ex);
            throw;
        }
    }

    private static void BootstrapRuntime()
    {
        // MddBootstrapInitialize loads the Windows App Runtime and registers its COM servers.
        // 0x00010008 = Windows App SDK major 1, minor 8.  minVersion 0 = no minimum patch.
        var hr = MddBootstrapInitialize(0x00010008, null, 0UL);
        if (hr != 0)
        {
            WriteStartupLog(new InvalidOperationException(
                $"MddBootstrapInitialize failed with HRESULT 0x{hr:X8}. " +
                $"Ensure Windows App Runtime 1.8 is installed on this machine."));
            Marshal.ThrowExceptionForHR(hr);
        }
    }

    /// <summary>
    /// Returns true only when the process has a confirmed MSIX package identity.
    /// GetCurrentPackageFullName returns ERROR_INSUFFICIENT_BUFFER (122) when a
    /// package name exists but the buffer is too small — the only reliable "is packaged"
    /// signal. APPMODEL_ERROR_NO_PACKAGE (15700) means unpackaged. Any other value
    /// (including unexpected errors) is treated as unpackaged so bootstrap always runs.
    /// </summary>
    private static bool IsPackaged()
    {
        int length = 0;
        int result = GetCurrentPackageFullName(ref length, null);
        // 122 = ERROR_INSUFFICIENT_BUFFER: a package name exists (buffer too small to hold it)
        // 0   = ERROR_SUCCESS: package name fits in zero-length buffer (degenerate, treat as packaged)
        // Anything else (including 15700 = APPMODEL_ERROR_NO_PACKAGE) = unpackaged
        return result is 0 or 122;
    }

    private static void WriteStartupLog(Exception ex)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PAYETAXCalc");
            Directory.CreateDirectory(logDir);
            File.AppendAllText(
                Path.Combine(logDir, "startup.log"),
                $"[{DateTime.Now:O}] {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}\n\n");
        }
        catch { }
    }

    [DllImport("Microsoft.WindowsAppRuntime.Bootstrap.dll", ExactSpelling = true)]
    private static extern int MddBootstrapInitialize(
        uint majorMinorVersion,
        [MarshalAs(UnmanagedType.LPWStr)] string? versionTag,
        ulong minVersion);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(
        ref int packageFullNameLength, char[]? packageFullName);
}
