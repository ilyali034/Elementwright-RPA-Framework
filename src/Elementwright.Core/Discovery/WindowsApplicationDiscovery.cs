using System.Diagnostics;
using Elementwright.Core.Models;

namespace Elementwright.Core.Discovery;

public sealed class WindowsApplicationDiscovery : IApplicationDiscovery
{
    public Task<IReadOnlyList<ApplicationInfo>> GetApplicationsAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<ApplicationInfo>>(() =>
        {
            var applications = new List<ApplicationInfo>();

            foreach (var process in Process.GetProcesses())
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (process.MainWindowHandle == nint.Zero)
                        continue;

                    var title = process.MainWindowTitle;
                    if (string.IsNullOrWhiteSpace(title))
                        continue;

                    string executablePath;
                    try
                    {
                        executablePath = process.MainModule?.FileName ?? string.Empty;
                    }
                    catch
                    {
                        executablePath = string.Empty;
                    }

                    applications.Add(new ApplicationInfo(
                        process.Id,
                        process.ProcessName,
                        title,
                        executablePath,
                        process.MainWindowHandle));
                }
                catch (InvalidOperationException)
                {
                    // The process may exit while the list is being collected.
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Some processes do not expose their details to this user.
                }
                finally
                {
                    process.Dispose();
                }
            }

            return applications
                .OrderBy(x => x.WindowTitle, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        }, cancellationToken);
    }
}
