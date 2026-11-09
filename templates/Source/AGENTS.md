# Instructions for AI coding agents

This file tells AI coding agents how to work in this repository. Read [CONTRIBUTING.md](CONTRIBUTING.md) for the contribution rules and [README.md](README.md) for what the library does. This file does not repeat them.

## Coding guidelines

* Follow the C# coding guidelines in the skills under [`.agents/skills`](.agents/skills). They are based on the [Coding Guidelines for C#](https://csharpcodingguidelines.com/).
* Follow the rules in `.editorconfig` and `MyPackage.sln.DotSettings`. Don't change these files to make a warning go away.
* `Directory.Build.props` treats all warnings as errors and enables StyleCop, Roslynator, Meziantou, the CSharpGuidelinesAnalyzer and the .NET analyzers. They only run for the `net10.0` target framework. Fix the code instead of suppressing an analyzer warning. If a suppression is really necessary, keep it as local as possible and explain why.
* Cover every change with a new or existing unit test in `MyPackage.Specs`. Write the tests in the Arrange-Act-Assert style.

## Building and testing

* The build script uses [Fallout](https://fallout.build/). It lives in `Build/Build.cs`. Run it with `./build.ps1`, `./build.sh` or `build.cmd`.
* Run `./build.ps1 --plan` to see the targets and how they depend on each other. Useful targets are:
  * `Compile` to build the solution
  * `RunTests` to run the unit tests in `MyPackage.Specs`
  * `ApiChecks` to compare the public API with the approved snapshots
  * `Pack` (the default) to run everything and create the NuGet package in the `Artifacts` folder
<!--#if (benchmarks) -->
  * `RunBenchmarks` to run the benchmarks in `MyPackage.Benchmarks`. It is not part of the normal build.
<!--#endif -->
* Add `--skip ScanPackages` if you don't need the PackageGuard scan, for example when you work offline.
* The build uses GitVersion to calculate the version number from the Git history. **The repository needs at least one commit before you run the build.** Without it, the build fails while it calculates the version.
{{~ if source_only ~}}
* Until the build runs for the first time, the `.nuspec` file of the library is called `nuspec`. The build renames it. Don't rename it yourself.
{{~ end ~}}
* `PackageReadme.md` is generated from `README.md` during the build. Edit `README.md` instead.

## Changing the public API

* `MyPackage.ApiVerificationTests` compares the public API of the library with the snapshots in `MyPackage.ApiVerificationTests/ApprovedApi`. There is one `.verified.txt` file per target framework.
* If you change the public API on purpose, run `./build.ps1 ApiChecks`. The tests fail and write `.received.txt` files next to the snapshots. Check that the differences are what you expect. Then run `./AcceptApiChanges.ps1` (or `./AcceptApiChanges.sh`) to accept them, and commit the updated `.verified.txt` files.
* Never edit the `.verified.txt` files by hand, and never accept API changes you did not intend.
* [CONTRIBUTING.md](CONTRIBUTING.md) explains that public API changes need approval in an issue first. Mention every API change in the pull request description.

## Pipelines and dependencies

{{~ if azdo ~}}
* The Azure DevOps pipeline is defined in `Build/azure-pipelines.yaml`. It calls the same build script, so make sure the build passes locally first.
{{~ else ~}}
* The GitHub workflows under `.github/workflows` call the same build script, so make sure the build passes locally first.
{{~ end ~}}
* `.packageguard/config.json` lists the allowed licenses and packages. If you add a NuGet package, check that its license is allowed. Don't add packages to the allow list without asking.
