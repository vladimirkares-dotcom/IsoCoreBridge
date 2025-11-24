using System.Collections.Generic;
using System.Threading.Tasks;
using IsoCore.Domain;

namespace IsoCore.App.Services;

public interface IProjectStorage
{
    Task SaveProjectsAsync(IEnumerable<ProjectInfo> projects);
    Task<List<ProjectInfo>> LoadProjectsAsync();
}
