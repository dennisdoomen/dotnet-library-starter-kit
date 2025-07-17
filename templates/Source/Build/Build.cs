using System;
using System.Linq;
using Nuke.Common;
{{~ if azdo ~}}
using Nuke.Common.CI.AzurePipelines;
{{~ else ~}}
using Nuke.Common.CI.GitHubActions;
{{~ end ~}}
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
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

{{~ if azdo ~}}
    [Parameter("The Git specification of the branch or PR that is being build (e.g. ref/pull/123/merge or refs/heads/main)")]
    string BranchSpec = "";

    [Parameter("A unique number for the build, e.g. 1234")]
    string BuildNumber = "";

    [Parameter(
        "Set the URI of the NuGet artifacts API, which is used to publish packages generated during the build process.")]
    readonly string NugetArtifactsApiUri =
        "https://pkgs.dev.azure.com/MyOrganization/MyProject/_packaging/MyFeed/nuget/v3/index.json";
{{~ else ~}}
    GitHubActions GitHubActions => GitHubActions.Instance;

    string BranchSpec => GitHubActions?.Ref;

    string BuildNumber => GitHubActions?.RunNumber.ToString();

    [Parameter(
        "Set the URI specifying the location of the NuGet artifacts API, which is used to publish packages generated during the build process.")]
    readonly string NugetArtifactsApiUri = "https://api.nuget.org/v3/index.json";
{{~ end ~}}

    [Parameter("The API key used to authenticate and authorize access to the NuGet artifacts API.")]
    [Secret]
    readonly string NugetArtifactsApiKey;

    [Parameter("The key to use for scanning packages on GitHub")]
    [Secret]
    readonly string GitHubApiKey;

    [Solution(GenerateProjects = true)]
    readonly Solution Solution;

    [GitVersion(Framework = "net8.0", NoFetch = true, NoCache = true)]
    readonly GitVersion GitVersion;

    AbsolutePath ArtifactsDirectory => RootDirectory / "Artifacts";

    AbsolutePath TestResultsDirectory => ArtifactsDirectory / "TestResults";

    AbsolutePath CoverageResultsFile => TestResultsDirectory / "Cobertura.xml";

    [NuGetPackage("PackageGuard", "PackageGuard.dll")]
    Tool PackageGuard;

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

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution)
                .EnableNoCache());
        });

    Target Compile => _ => _
        .DependsOn(CalculateNugetVersion)
        .DependsOn(Restore)
        .Executes(() =>
        {
            ReportSummary(s => s
                .WhenNotNull(SemVer, (summary, semVer) => summary
                    .AddPair("Version", semVer)));

            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoLogo()
                .EnableNoRestore()
				.SetVersion(SemVer)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion) );
        });

    Target RunTests => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            TestResultsDirectory.CreateOrCleanDirectory();
            var project = Solution.GetProject("MyPackage.Specs");

            DotNetTest(s => s
                // We run tests in debug mode so that Fluent Assertions can show the names of variables
                .SetConfiguration(Configuration.Debug)
                // To prevent the machine language to affect tests sensitive to the current thread's culture
                .SetProcessEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en-US")
                .SetDataCollector("XPlat Code Coverage")
                .SetCollectCoverage(true)
                .SetCoverletOutputFormat(CoverletOutputFormat.cobertura)
                .SetResultsDirectory(TestResultsDirectory)
                .SetProjectFile(project)
                .CombineWith(project.GetTargetFrameworks(),
                    (ss, framework) => ss
                        .SetFramework(framework)
                        .AddLoggers($"trx;LogFileName={framework}.trx")
                ));
        });

    Target ApiChecks => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var project = Solution.GetProject("MyPackage.ApiVerificationTests");

            DotNetTest(s => s
                .SetConfiguration(Configuration)
                .SetProcessEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en-US")
                .SetResultsDirectory(TestResultsDirectory)
                .SetProjectFile(project)
                .AddLoggers($"trx;LogFileName={project!.Name}.trx"));
        });

    Target ScanPackages => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Environment.SetEnvironmentVariable("GitHubApiKey", GitHubApiKey);
            PackageGuard($"--config-path={RootDirectory / ".packageguard" / "config.json"} --use-caching {RootDirectory}");
        });

    Target GenerateCodeCoverageReport => _ => _
        .DependsOn(RunTests)
        .Executes(() =>
        {
{{~ if azdo ~}}
            ReportGenerator(s => s
				.AddReports(TestResultsDirectory / "**/coverage.cobertura.xml")
                .SetReportTypes(ReportTypes.HtmlInline_AzurePipelines_Light)
                .SetTargetDirectory(ArtifactsDirectory / "html")
                .AddFileFilters("-*.g.cs"));

            ReportGenerator(s => s
				.AddReports(TestResultsDirectory / "**/coverage.cobertura.xml")
                .SetReportTypes(ReportTypes.Cobertura)
                .SetTargetDirectory(TestResultsDirectory)
                .AddFileFilters("-*.g.cs"));
{{~ else ~}}
            ReportGenerator(s => s
				.AddReports(TestResultsDirectory / "**/coverage.cobertura.xml")
                .AddReportTypes(ReportTypes.lcov, ReportTypes.Html)
                .SetTargetDirectory(TestResultsDirectory / "reports")
                .AddFileFilters("-*.g.cs"));

            string link = TestResultsDirectory / "reports" / "index.html";
            Information($"Code coverage report: \x1b]8;;file://{link.Replace('\\', '/')}\x1b\\{link}\x1b]8;;\x1b\\");
{{~ end ~}}
        });

{{~ if azdo ~}}
    Target PublishTestResults => _ => _
        .DependsOn(GenerateCodeCoverageReport)
        .OnlyWhenStatic(() => Host is AzurePipelines)
        .Executes(() =>
        {
            var testResults = TestResultsDirectory
                    .GlobDirectories("**/*.trx")
                    .Select(t => t.ToString())
                    .ToArray();

            Information($"Publishing .trx files to Azure Pipelines");

            AzurePipelines.Instance.PublishTestResults(
                ".NET tests",
                AzurePipelinesTestResultsType.VSTest, testResults);

            Information($"Publishing Cobertura {CoverageResultsFile!.Name} to Azure Pipelines");

            AzurePipelines.Instance.PublishCodeCoverage(
                AzurePipelinesCodeCoverageToolType.Cobertura,
                CoverageResultsFile.ToString(),
                ArtifactsDirectory / "html");
        });
{{~ end ~}}

    Target Pack => _ => _
        .DependsOn(ScanPackages)
        .DependsOn(CalculateNugetVersion)
        .DependsOn(ApiChecks)
        .DependsOn(GenerateCodeCoverageReport)
        {{~ if azdo }}.DependsOn(PublishTestResults){{~ end ~}}
        .Executes(() =>
        {
            ReportSummary(s => s
                .WhenNotNull(SemVer, (c, semVer) => c
                    .AddPair("Packed version", semVer)));

            DotNetPack(s => s
                .SetProject(Solution.GetProject("MyPackage"))
                .SetOutputDirectory(ArtifactsDirectory)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .EnableNoLogo()
                .EnableNoRestore()
                .EnableContinuousIntegrationBuild() // Necessary for deterministic builds
                .SetVersion(SemVer));
        });

{{~ if azdo ~}}
    Target UploadPackageAsPipelineArtifact => _ => _
        .OnlyWhenStatic(() => Host is AzurePipelines)
        .OnlyWhenStatic(() => IsServerBuild)
        .DependsOn(Pack)
        .Executes(() =>
        {
            AbsolutePath packageFile = ArtifactsDirectory.GlobFiles("*.nupkg")
                .SingleOrError("Expected exactly one file to be found, found none or multiple files");

            Information($"Uploading artifact ${packageFile!.Name} to Azure Pipelines");

            AzurePipelines.Instance.UploadArtifacts("drop", "package", packageFile!);
        });
{{~ end ~}}

    Target Push => _ => _
{{~ if azdo ~}}
        .OnlyWhenStatic(() => IsServerBuild)
        .OnlyWhenStatic(() => !IsPullRequest)
        .DependsOn(UploadPackageAsPipelineArtifact)
{{~ end ~}}
        .DependsOn(Pack)
        .OnlyWhenDynamic(() => IsTag)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            var packages = ArtifactsDirectory.GlobFiles("*.nupkg");

            Assert.NotEmpty(packages);

            DotNetNuGetPush(s => s
                .SetApiKey(NugetArtifactsApiKey)
                .EnableSkipDuplicate()
                .SetSource(NugetArtifactsApiUri)
                .EnableNoSymbols()
                .CombineWith(packages,
                    (v, path) => v.SetTargetPath(path)));
        });

    Target Default => _ => _
		.DependsOn(Pack)
{{~ if azdo ~}}
		.DependsOn(UploadPackageAsPipelineArtifact)
{{~ end ~}}
        .DependsOn(Push);

{{~ if azdo ~}}
	bool IsPullRequest => BranchSpec.Contains("/pull/");
{{~ else ~}}
    bool IsPullRequest => GitHubActions?.IsPullRequest ?? false;
{{~ end ~}}

    bool IsTag => BranchSpec != null && BranchSpec.Contains("refs/tags", StringComparison.OrdinalIgnoreCase);
}
