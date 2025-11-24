using System;
using System.IO;
using IsoCore.Domain;

namespace IsoCore.App.Services;

public static class ProjectPathService
{
    public static string GetRootProjectsFolder()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(baseDir, "IsoCoreBridge", "Projects");
    }

    public static string GetProjectRootFolder(ProjectInfo project)
    {
        if (project == null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        return Path.Combine(GetRootProjectsFolder(), project.Id.ToString());
    }

    public static string GetProjectCalcFolder(ProjectInfo project)
    {
        var calc = Path.Combine(GetProjectRootFolder(project), "Calc");
        Directory.CreateDirectory(calc);
        return calc;
    }

    public static string GetProjectOffersFolder(ProjectInfo project)
    {
        var offers = Path.Combine(GetProjectRootFolder(project), "Offers");
        Directory.CreateDirectory(offers);
        return offers;
    }

    public static string GetProjectLogsFolder(ProjectInfo project) =>
        Path.Combine(GetProjectRootFolder(project), "Logs");

    public static string GetProjectAttachmentsFolder(ProjectInfo project) =>
        Path.Combine(GetProjectRootFolder(project), "Attachments");

    public static void EnsureProjectFolders(ProjectInfo project)
    {
        Directory.CreateDirectory(GetProjectRootFolder(project));
        Directory.CreateDirectory(GetProjectCalcFolder(project));
        Directory.CreateDirectory(GetProjectOffersFolder(project));
        Directory.CreateDirectory(GetProjectLogsFolder(project));
        Directory.CreateDirectory(GetProjectAttachmentsFolder(project));
    }
}
