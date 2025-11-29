using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Npm;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Runtime identifier for CLI publish (e.g., linux-x64, win-x64, osx-arm64). Use 'any' for framework-dependent.")]
    readonly string RuntimeIdentifier = "any";

    [Solution] readonly Solution Solution = null!;

    AbsolutePath SourceDirectory => RootDirectory;
    AbsolutePath TestsDirectory => RootDirectory / "test";
    AbsolutePath WebUIDirectory => RootDirectory / "webui";
    AbsolutePath WebUISourceDirectory => WebUIDirectory / "source";
    AbsolutePath WebUIOutputDirectory => RootDirectory / "BililiveRecorder.Web" / "embeded" / "ui";
    AbsolutePath OutputDirectory => RootDirectory / "output";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj")
                .Where(x => !x.ToString().Contains("build"))
                .ForEach(x => x.DeleteDirectory());
            TestsDirectory.GlobDirectories("**/bin", "**/obj")
                .ForEach(x => x.DeleteDirectory());
            OutputDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target BuildWebUI => _ => _
        .Executes(() =>
        {
            var packageJsonPath = WebUISourceDirectory / "package.json";
            if (!packageJsonPath.FileExists())
            {
                Serilog.Log.Warning("WebUI source not found (package.json missing), skipping WebUI build. Make sure git submodules are initialized.");
                return;
            }

            // Install npm dependencies
            NpmTasks.NpmCi(s => s
                .SetProcessWorkingDirectory(WebUISourceDirectory)
                .SetProcessEnvironmentVariable("BASE_URL", "./")
                .SetProcessEnvironmentVariable("VITE_EMBEDDED_BUILD", "true"));

            // Build with Vite
            NpmTasks.NpmRun(s => s
                .SetProcessWorkingDirectory(WebUISourceDirectory)
                .SetCommand("build")
                .SetProcessEnvironmentVariable("BASE_URL", "./")
                .SetProcessEnvironmentVariable("VITE_EMBEDDED_BUILD", "true"));

            // Copy dist to output
            var distDirectory = WebUISourceDirectory / "dist";
            if (distDirectory.DirectoryExists())
            {
                WebUIOutputDirectory.DeleteDirectory();
                distDirectory.Copy(WebUIOutputDirectory);
            }
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .DependsOn(BuildWebUI)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild());
        });

    Target PublishCli => _ => _
        .DependsOn(BuildWebUI)
        .DependsOn(Restore)
        .Executes(() =>
        {
            var cliProject = Solution.GetProject("BililiveRecorder.Cli");
            var rid = RuntimeIdentifier;

            Serilog.Log.Information($"Publishing CLI for {rid}...");

            var publishSettings = new DotNetPublishSettings()
                .SetProject(cliProject)
                .SetConfiguration(Configuration);

            if (rid == "any")
            {
                // For "any" RID, don't specify a runtime identifier (framework-dependent)
                publishSettings = publishSettings
                    .SetOutput(OutputDirectory / "cli" / rid / Configuration);
            }
            else
            {
                publishSettings = publishSettings
                    .SetRuntime(rid)
                    .SetOutput(OutputDirectory / "cli" / rid / Configuration);
            }

            DotNetPublish(publishSettings);
        });

    Target PublishWpf => _ => _
        .DependsOn(Restore)
        .OnlyWhenStatic(() => EnvironmentInfo.IsWin)
        .Executes(() =>
        {
            var wpfProject = Solution.GetProject("BililiveRecorder.WPF");

            DotNetBuild(s => s
                .SetProjectFile(wpfProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetOutputDirectory(OutputDirectory / "wpf" / Configuration));
        });
}

class Configuration : Enumeration
{
    public static Configuration Debug = new() { Value = nameof(Debug) };
    public static Configuration Release = new() { Value = nameof(Release) };

    public static implicit operator string(Configuration configuration) => configuration.Value;
}
