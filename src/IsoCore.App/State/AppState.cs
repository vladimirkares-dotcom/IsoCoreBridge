using IsoCore.Domain;
using System;

namespace IsoCore.App.State;

public class AppState
{
    public ProjectInfo? CurrentProject { get; private set; }

    public event Action<ProjectInfo?>? ProjectChanged;

    public void SetCurrentProject(ProjectInfo? project)
    {
        CurrentProject = project;
        ProjectChanged?.Invoke(project);
    }
}
