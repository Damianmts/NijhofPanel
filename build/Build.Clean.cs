using Nuke.Common.Tools.DotNet;
using Nuke.Common.ProjectModel;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

sealed partial class Build
{
    /// <summary>
    ///     Clean projects with dependencies.
    /// </summary>
    Target Clean => _ => _
        .OnlyWhenStatic(() => IsLocalBuild)
        .Executes(() =>
        {
            Project[] excludedProjects =
            [
                Solution.Automation.Build
            ];

            CleanDirectory(ArtifactsDirectory);
            foreach (var project in Solution.AllProjects)
            {
                if (excludedProjects.Contains(project)) continue;

                CleanDirectory(project.Directory / "bin");
                CleanDirectory(project.Directory / "obj");
            }

            foreach (var configuration in GlobBuildConfigurations())
            foreach (var project in Solution.AllProjects
                         .Where(p => !ExcludedProjects.Contains(p.Name)))
                DotNetClean(settings => settings
                    .SetProject(project)
                    .SetConfiguration(configuration)
                    .SetVerbosity(DotNetVerbosity.minimal)
                    .EnableNoLogo());
        });

    /// <summary>
    ///     Clean and log the specified directory.
    /// </summary>
    static void CleanDirectory(AbsolutePath path)
    {
        Log.Information("Cleaning directory: {Directory}", path);

        try
        {
            // Verwijder "ReadOnly" attribuut van directory en subbestanden
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    var attrs = File.GetAttributes(file);
                    if (attrs.HasFlag(FileAttributes.ReadOnly))
                        File.SetAttributes(file, attrs & ~FileAttributes.ReadOnly);
                }

                foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                {
                    var attrs = File.GetAttributes(dir);
                    if (attrs.HasFlag(FileAttributes.ReadOnly))
                        File.SetAttributes(dir, attrs & ~FileAttributes.ReadOnly);
                }
            }

            // Voer de standaard clean-operatie uit
            path.CreateOrCleanDirectory();
        }
        catch (IOException ex)
        {
            Log.Warning("Kon map niet verwijderen: {Path}. Fout: {Message}", path, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Warning("Geen toegang tot map: {Path}. Fout: {Message}", path, ex.Message);
        }
    }
}