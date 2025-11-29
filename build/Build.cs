using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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

    [Parameter("Container registry to push to (e.g., ghcr.io/bililiverecorder/bililiverecorder)")]
    readonly string ContainerRegistry = "";

    [Parameter("Container image tag")]
    readonly string ContainerTag = "latest";

    [Parameter("Whether to push the container image")]
    readonly bool ContainerPush = false;

    [Solution] readonly Solution Solution = null!;

    AbsolutePath SourceDirectory => RootDirectory;
    AbsolutePath TestsDirectory => RootDirectory / "test";
    AbsolutePath WebUIDirectory => RootDirectory / "webui";
    AbsolutePath WebUISourceDirectory => WebUIDirectory / "source";
    AbsolutePath WebUIOutputDirectory => RootDirectory / "BililiveRecorder.Web" / "embeded" / "ui";
    AbsolutePath ConfigGenDirectory => RootDirectory / "config_gen";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath ArtifactsDirectory => OutputDirectory / "artifacts";

    // All supported CLI runtime identifiers
    static readonly string[] CliRuntimeIdentifiers = new[]
    {
        "any",
        "linux-arm",
        "linux-arm64",
        "linux-musl-arm64",
        "linux-x64",
        "linux-musl-x64",
        "osx-x64",
        "osx-arm64",
        "win-x64"
    };

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

    Target GenerateConfig => _ => _
        .Executes(() =>
        {
            var packageJsonPath = ConfigGenDirectory / "package.json";
            if (!packageJsonPath.FileExists())
            {
                Serilog.Log.Warning("config_gen directory not found, skipping config generation.");
                return;
            }

            // Install npm dependencies
            NpmTasks.NpmInstall(s => s
                .SetProcessWorkingDirectory(ConfigGenDirectory));

            // Run config generation
            NpmTasks.NpmRun(s => s
                .SetProcessWorkingDirectory(ConfigGenDirectory)
                .SetCommand("build"));
        });

    Target PublishAllCli => _ => _
        .DependsOn(BuildWebUI)
        .DependsOn(Restore)
        .Produces(ArtifactsDirectory / "*.zip")
        .Executes(() =>
        {
            var cliProject = Solution.GetProject("BililiveRecorder.Cli");
            ArtifactsDirectory.CreateOrCleanDirectory();

            foreach (var rid in CliRuntimeIdentifiers)
            {
                Serilog.Log.Information($"Publishing CLI for {rid}...");

                var outputDir = OutputDirectory / "cli" / rid / Configuration;

                var publishSettings = new DotNetPublishSettings()
                    .SetProject(cliProject)
                    .SetConfiguration(Configuration)
                    .SetOutput(outputDir);

                if (rid != "any")
                {
                    publishSettings = publishSettings.SetRuntime(rid);
                }

                DotNetPublish(publishSettings);

                // Create zip archive
                var zipPath = ArtifactsDirectory / $"BililiveRecorder-CLI-{rid}.zip";
                Serilog.Log.Information($"Creating archive {zipPath}...");

                if (zipPath.FileExists())
                    zipPath.DeleteFile();

                ZipFile.CreateFromDirectory(outputDir, zipPath);
            }
        });

    Target BuildContainer => _ => _
        .DependsOn(BuildWebUI)
        .DependsOn(Restore)
        .Executes(() =>
        {
            // Build CLI for container (framework-dependent for smaller size)
            var cliProject = Solution.GetProject("BililiveRecorder.Cli");
            var containerBuildOutput = RootDirectory / "BililiveRecorder.Cli" / "bin" / "docker_out";

            DotNetBuild(s => s
                .SetProjectFile(cliProject)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(containerBuildOutput));

            // Build multi-arch container using buildah
            var containerImage = string.IsNullOrEmpty(ContainerRegistry)
                ? "bililive/recorder"
                : ContainerRegistry;
            var tag = string.IsNullOrEmpty(ContainerTag) ? "latest" : ContainerTag;
            var fullTag = $"{containerImage}:{tag}";

            Serilog.Log.Information($"Building container image {fullTag}...");

            // Build for each architecture
            var architectures = new[] { "amd64", "arm64", "arm/v7" };
            var manifestImages = new List<string>();

            foreach (var arch in architectures)
            {
                var archTag = $"{fullTag}-{arch.Replace("/", "-")}";
                Serilog.Log.Information($"Building for {arch}...");

                ProcessTasks.StartProcess(
                    "buildah",
                    $"build --arch {arch} -t {archTag} -f Dockerfile.GitHubActions .",
                    RootDirectory)
                    .AssertZeroExitCode();

                manifestImages.Add(archTag);
            }

            // Create manifest
            Serilog.Log.Information($"Creating manifest {fullTag}...");
            ProcessTasks.StartProcess(
                "buildah",
                $"manifest create {fullTag}",
                RootDirectory)
                .AssertZeroExitCode();

            foreach (var img in manifestImages)
            {
                ProcessTasks.StartProcess(
                    "buildah",
                    $"manifest add {fullTag} {img}",
                    RootDirectory)
                    .AssertZeroExitCode();
            }

            if (ContainerPush && !string.IsNullOrEmpty(ContainerRegistry))
            {
                Serilog.Log.Information($"Pushing manifest {fullTag}...");
                ProcessTasks.StartProcess(
                    "buildah",
                    $"manifest push --all {fullTag} docker://{fullTag}",
                    RootDirectory)
                    .AssertZeroExitCode();
            }
        });
}

class Configuration : Enumeration
{
    public static Configuration Debug = new() { Value = nameof(Debug) };
    public static Configuration Release = new() { Value = nameof(Release) };

    public static implicit operator string(Configuration configuration) => configuration.Value;
}
