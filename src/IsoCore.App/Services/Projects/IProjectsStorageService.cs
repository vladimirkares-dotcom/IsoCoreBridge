using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IsoCore.Domain;

namespace IsoCore.App.Services.Projects;

public interface IProjectsStorageService
{
    Task<IReadOnlyList<ProjectInfo>> LoadProjectsAsync(CancellationToken cancellationToken = default);
    Task SaveProjectsAsync(IEnumerable<ProjectInfo> projects, CancellationToken cancellationToken = default);
}
