using Elementwright.Desktop.Models;

namespace Elementwright.Desktop.Services;

public interface IApplicationDiscovery
{
    Task<IReadOnlyList<ApplicationInfo>> GetApplicationsAsync(
        CancellationToken cancellationToken = default);
}
