using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.PowerShell;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Scriban;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Serilog.Log;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Default);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("The key to push to Nuget")]
    [Secret]
    readonly string NuGetApiKey;

    [Parameter("The key to use for scanning packages on GitHub")]
    [Secret]
    readonly string GitHubApiKey;

    [Solution(GenerateProjects = true)]
    readonly Solution Solution;

    [GitVersion(Framework = "net6.0", NoFetch = true)]
    readonly GitVersion GitVersion;

    GitHubActions GitHubActions => GitHubActions.Instance;

    string BranchSpec => GitHubActions?.Ref;

    string BuildNumber => GitHubActions?.RunNumber.ToString();

    AbsolutePath ArtifactsDirectory => RootDirectory / "Artifacts";

    string SemVer;

    Target CalculateNugetVersion => _ => _
        .Executes(() =>
        {
            SemVer = GitVersion.SemVer;
            if (IsPullRequest)
            {
                Information(
                    "Branch spec {branchspec} is a pull request. Adding build number {buildnumber}",
                    BranchSpec, BuildNumber);

                SemVer = string.Join('.', GitVersion.SemVer.Split('.').Take(3).Union(new[]
                {
                    BuildNumber
                }));
            }

            Information("SemVer = {semver}", SemVer);
        });

    Target PrepareTemplates => _ => _
        .Executes(() =>
        {
            AbsolutePath templateSource = RootDirectory / "templates" / "Source";

            foreach (AbsolutePath file in templateSource.GlobFiles("**/*"))
            {
                RelativePath relativePathTo = templateSource.GetRelativePathTo(file);
                string content = file.ReadAllText();

                Information("Processing {File}", file);

                var template = Template.Parse(content, file);

                AbsolutePath templateDirectory = ArtifactsDirectory / "templates";
                template.RenderToFileIfNotEmpty(templateDirectory / "Normal" / relativePathTo, new
                {
                    SourceOnly = false,
                    OpenSource = false,
                });

                template.RenderToFileIfNotEmpty(templateDirectory / "NormalOss" / relativePathTo, new
                {
                    SourceOnly = false,
                    OpenSource = true
                });

                template.RenderToFileIfNotEmpty(templateDirectory / "SourceOnly" / relativePathTo, new
                {
                    SourceOnly = true
                });

                template.RenderToFileIfNotEmpty(templateDirectory / "SourceOnlyOss" / relativePathTo, new
                {
                    SourceOnly = true,
                    OpenSource = true
                });

                template.RenderToFileIfNotEmpty(templateDirectory / "NormalAzdo" / relativePathTo, new
                {
                    Azdo = true
                }, [".github"]);

                template.RenderToFileIfNotEmpty(templateDirectory / "SourceOnlyAzdo" / relativePathTo, new
                {
                    SourceOnly = true,
                    Azdo = true
                }, [".github"]);
            }
        });

    Target PrepareTemplateReadmes => _ => _
        .DependsOn(PrepareTemplates)
        .Executes(() =>
        {

            string readmeTemplate = (RootDirectory / "templates" / "Source" / "README.md").ReadAllText();
            var template = Template.Parse(readmeTemplate);
            var readmeContents = template.Render(new
            {
                PackageReadme = true,
            });

            string[] names = ["Normal", "SourceOnly", "NormalOss", "SourceOnlyOss", "NormalAzdo", "SourceOnlyAzdo"];
            foreach (string name in names)
            {
                (ArtifactsDirectory / "templates" / name / "PackageReadme.md").WriteAllText(readmeContents);
            }
        });

    Target Compile => _ => _
        .DependsOn(CalculateNugetVersion)
        .DependsOn(PrepareTemplateReadmes)
        .Executes(() =>
        {
            ReportSummary(s => s
                .WhenNotNull(SemVer, (summary, semVer) => summary
                    .AddPair("Version", semVer)));

            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoLogo()
                .EnableNoCache()
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion));
        });

    Target PreparePackageReadme => _ => _
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();

            var content = (RootDirectory / "README.md").ReadAllText();
            var sections = content.Split(["\n## "], StringSplitOptions.RemoveEmptyEntries);

            string[] headersToInclude =
            [
                "About",
                "How do I use it",
                "Additional things",
                "Versioning",
                "Credits",
                "Support",
                "You may also like"
            ];

            var readmeContent = "## " + string.Join("\n## ", sections
                .Where(section => headersToInclude.Any(header => section.StartsWith(header, StringComparison.OrdinalIgnoreCase))));

            (ArtifactsDirectory / "Readme.md").WriteAllText(readmeContent);
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .DependsOn(PreparePackageReadme)
        .Executes(() =>
        {
            ReportSummary(s => s
                .WhenNotNull(SemVer, (c, semVer) => c
                    .AddPair("Packed version", semVer)));

            DotNetPack(s => s
                .SetProject(Solution)
                .SetOutputDirectory(ArtifactsDirectory)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .EnableNoLogo()
                .EnableNoRestore()
                .EnableContinuousIntegrationBuild() // Necessary for deterministic builds
                .SetVersion(SemVer));
        });

    Target TestPackageInstallation => _ => _
        .DependsOn(Pack)
        .Executes(() =>
        {
            var packageFile = ArtifactsDirectory.GlobFiles("*.nupkg").OrderByDescending(x => x.ToString()).First();
            var testDirectory = RootDirectory / "temp" / "test-installation";

            try
            {
                // Clean up any previous test directory
                testDirectory.DeleteDirectory();
                testDirectory.CreateDirectory();

                Information("Installing package: {Package}", packageFile);

                // First, try to uninstall any existing package with the same name
                try
                {
                    DotNet($"new uninstall DotNetLibraryPackageTemplates", workingDirectory: testDirectory, logOutput: false);
                }
                catch
                {
                    // Ignore errors if the package isn't installed
                }

                // Install the locally built package with force flag
                DotNet($"new install {packageFile} --force", workingDirectory: testDirectory);

                // Test each template variant by creating and building a test project
                string[] templateShortNames = [
                    "nooss-nuget-class-library-sln",
                    "nooss-source-only-nuget-class-library-sln",
                    "oss-nuget-class-library-sln",
                    "oss-source-only-nuget-class-library-sln"
                ];

                foreach (string templateName in templateShortNames)
                {
                    var projectTestDirectory = testDirectory / $"test-{templateName}";
                    projectTestDirectory.CreateDirectory();

                    Information("Testing template: {Template}", templateName);

                    // Create project from template
                    DotNet($"new {templateName} --name TestLibrary --force", workingDirectory: projectTestDirectory);

                    // Build the generated project to ensure it compiles without errors
                    // Note: template uses preferNameDirectory=true, so project is created in TestLibrary subdirectory
                    var actualProjectDirectory = projectTestDirectory / "TestLibrary";

                    DotNet("build", workingDirectory: actualProjectDirectory);
                    Information("Successfully built project from template: {Template}", templateName);

                    // Check if the basic project structure was created correctly
                    if ((actualProjectDirectory / $"TestLibrary.sln").FileExists() ||
                        (actualProjectDirectory / $"TestLibrary").DirectoryExists())
                    {
                        Information("Project structure was created successfully for template: {Template}", templateName);
                    }
                    else
                    {
                        Error($"Template {templateName} failed to create proper project structure");
                    }
                }

                Information("All template installations and builds completed successfully");
            }
            finally
            {
                // Clean up: uninstall the package and remove test directory
                try
                {
                    DotNet($"new uninstall DotNetLibraryPackageTemplates");
                    Information("Uninstalled package: DotNetLibraryPackageTemplates");
                }
                catch (Exception e)
                {
                    Warning("Failed to uninstall package DotNetLibraryPackageTemplates: {Error}", e.Message);
                }

                testDirectory.DeleteDirectory();
            }
        });

    Target TestTemplateBuild => _ => _
        .DependsOn(Pack)
        .DependsOn(PrepareTemplateReadmes)
        .Executes(() =>
        {
            string[] names = ["Normal", "SourceOnly", "NormalOss", "SourceOnlyOss", "NormalAzdo", "SourceOnlyAzdo"];
            foreach (string name in names)
            {
                var templateDirectory = ArtifactsDirectory / "templates" / name;

                // Only include the ScanPackages step for the Normal template to speed up the build and avoid rate limiting issues
                string additionalOption = name == "Normal" ? "" : " -skip ScanPackages";

                Environment.SetEnvironmentVariable("GitHubApiKey", GitHubApiKey);
                PowerShellTasks.PowerShell($"./build.ps1 Pack {additionalOption}", workingDirectory: templateDirectory);

                Assert.NotEmpty((templateDirectory / "Artifacts").GlobFiles("*.nupkg"));
            }
        });

    Target Push => _ => _
        .DependsOn(Pack)
        .DependsOn(TestPackageInstallation)
        .DependsOn(TestTemplateBuild)
        .OnlyWhenDynamic(() => IsTag)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            var packages = ArtifactsDirectory.GlobFiles("*.nupkg");

            Assert.NotEmpty(packages);

            DotNetNuGetPush(s => s
                .SetApiKey(NuGetApiKey)
                .EnableSkipDuplicate()
                .SetSource("https://api.nuget.org/v3/index.json")
                .EnableNoSymbols()
                .CombineWith(packages,
                    (v, path) => v.SetTargetPath(path)));
        });

    Target Default => _ => _
        .DependsOn(Push);

    bool IsPullRequest => GitHubActions?.IsPullRequest ?? false;

    bool IsTag => BranchSpec != null && BranchSpec.Contains("refs/tags", StringComparison.OrdinalIgnoreCase);
}
