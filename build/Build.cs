using System.IO.Enumeration;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[PublicAPI]
sealed partial class Build : NukeBuild
{
    readonly string[] ExcludedProjects =
    {
        "NijhofPanel.Devhost",
        "NijhofPanel.Tests"
    };

    /// <summary>
    ///     Pipeline entry point.
    /// </summary>
    public static int Main() => Execute<Build>(b => b.CompileSolution);

    /// <summary>
    ///     Extract solution configuration names from the solution file.
    /// </summary>
    List<string> GlobBuildConfigurations()
    {
        var configurations = Solution.Configurations
            .Select(pair => pair.Key)
            .Select(config => config.Remove(config.LastIndexOf('|')))
            .Where(config => Configurations.Any(wildcard => FileSystemName.MatchesSimpleExpression(wildcard, config)))
            .Where(config => !ExcludedProjects.Any(excluded =>
                config.Contains(excluded, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.NotEmpty(configurations,
            $"No solution configurations have been found. Pattern: {string.Join(" | ", Configurations)}");
        return configurations;
    }

    Target CompilePerProject => _ => _
        .Executes(() =>
        {
            var configurations = GlobBuildConfigurations();

            foreach (var configuration in configurations)
            {
                var projects = Solution.AllProjects
                    .Where(project => !ExcludedProjects.Contains(project.Name))
                    .ToList();

                foreach (var project in projects)
                    DotNetBuild(s => s
                        .SetProjectFile(project)
                        .SetConfiguration(configuration));
            }
        });
}