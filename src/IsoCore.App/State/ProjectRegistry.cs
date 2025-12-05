using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using IsoCore.App.Services.Projects;
using IsoCore.Domain;

namespace IsoCore.App.State;

public class ProjectRegistry
{
    private readonly IProjectsStorageService _projectsStorage;
    private static DispatcherQueue? Dispatcher => App.MainDispatcherQueue;

    public ObservableCollection<ProjectInfo> Projects { get; }

    public ProjectRegistry()
    {
        _projectsStorage = IsoCore.App.App.ProjectsStorageService
            ?? throw new InvalidOperationException("ProjectsStorageService is not initialized.");
        Projects = new ObservableCollection<ProjectInfo>();
    }

    public async Task LoadFromStorageAsync()
    {
        var loaded = await _projectsStorage.LoadProjectsAsync().ConfigureAwait(false); // IO on background thread

        await RunOnDispatcherAsync(() =>
        {
            Projects.Clear();
            foreach (var project in loaded)
            {
                Projects.Add(project);
            }
        });
    }

    public async Task SaveToStorageAsync()
    {
        await _projectsStorage.SaveProjectsAsync(Projects).ConfigureAwait(false);
    }

    public async Task AddProjectAsync(ProjectInfo project)
    {
        await RunOnDispatcherAsync(() => Projects.Add(project));
        await SaveToStorageAsync();
    }

    public async Task RemoveProjectAsync(ProjectInfo project)
    {
        await RunOnDispatcherAsync(() => Projects.Remove(project));
        await SaveToStorageAsync();
    }

    public async Task DeleteProjectAsync(ProjectInfo project)
    {
        if (project == null)
        {
            return;
        }

        await RunOnDispatcherAsync(() => Projects.Remove(project));
        await SaveToStorageAsync();
    }

    public async Task UpdateProjectAsync(ProjectInfo project)
    {
        if (project == null)
        {
            return;
        }

        if (!Projects.Contains(project))
        {
            await RunOnDispatcherAsync(() =>
            {
                var existing = Projects.FirstOrDefault(p => string.Equals(p.Id, project.Id, StringComparison.OrdinalIgnoreCase)
                                                            || string.Equals(p.ProjectCode, project.ProjectCode, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    var index = Projects.IndexOf(existing);
                    if (index >= 0)
                    {
                        Projects[index] = project;
                    }
                }
                else
                {
                    Projects.Add(project);
                }
            });
        }
        else
        {
            await RunOnDispatcherAsync(() =>
            {
                var index = Projects.IndexOf(project);
                if (index >= 0)
                {
                    Projects[index] = project;
                }
            });
        }

        await SaveToStorageAsync();
    }

    public ProjectInfo? FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return Projects.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public ProjectInfo? FindByCode(string code)
    {
        return Projects.FirstOrDefault(p => p.ProjectCode == code);
    }

    public IReadOnlyList<ProjectInfo> Snapshot() => Projects.ToList();

    private async Task RunOnDispatcherAsync(Action action)
    {
        if (action == null)
        {
            return;
        }

        var dispatcher = Dispatcher;

        if (dispatcher == null || dispatcher.HasThreadAccess)
        {
            action();
            return;
        }

        var tcs = new TaskCompletionSource();
        if (!dispatcher.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
        {
            // If enqueuing fails, execute immediately to avoid deadlock; exception will bubble naturally.
            action();
            return;
        }

        await tcs.Task;
    }
}
