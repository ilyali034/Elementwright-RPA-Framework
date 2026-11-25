using Elementwright.Core.Models;

namespace Elementwright.Core.Discovery;

public interface IApplicationDiscovery
{
    Task<IReadOnlyList<ApplicationInfo>> GetApplicationsAsync(
        CancellationToken cancellationToken = default);
}
