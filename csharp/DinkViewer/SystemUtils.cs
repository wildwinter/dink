namespace DinkViewer;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class SystemUtils 
{
    public static void OpenUsingDefaultApp(string pathName)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(pathName) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", pathName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", pathName);
            }
            else
            {
                throw new Exception("Operating System not supported for opening browsers.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening browser: {ex.Message}");
        }
    }
}