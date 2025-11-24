using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using IsoCore.App.Services;
using IsoCore.Domain;

namespace IsoCore.App.State;

public class ProjectRegistry
{
    private readonly IProjectStorage _projectStorage;

    public ObservableCollection<ProjectInfo> Projects { get; }

    public ProjectRegistry()
    {
        _projectStorage = ProjectStorageManager.Storage
            ?? throw new InvalidOperationException("ProjectStorageManager.Storage is not initialized.");

        Projects = new ObservableCollection<ProjectInfo>();
    }

    public async Task LoadFromStorageAsync()
    {
        var loaded = await _projectStorage.LoadProjectsAsync();
        Projects.Clear();
        foreach (var project in loaded)
        {
            Projects.Add(project);
        }
    }

    public async Task SaveToStorageAsync()
    {
        await _projectStorage.SaveProjectsAsync(Projects);
    }

    public async Task AddProjectAsync(ProjectInfo project)
    {
        Projects.Add(project);
        await SaveToStorageAsync();
    }

    public async Task RemoveProjectAsync(ProjectInfo project)
    {
        Projects.Remove(project);
        await SaveToStorageAsync();
    }

    public async Task UpdateProjectAsync(ProjectInfo project)
    {
        if (Projects.Contains(project))
        {
            await SaveToStorageAsync();
        }
    }

    public ProjectInfo? FindByCode(string code)
    {
        return Projects.FirstOrDefault(p => p.ProjectCode == code);
    }

    public IReadOnlyList<ProjectInfo> Snapshot() => Projects.ToList();
}
