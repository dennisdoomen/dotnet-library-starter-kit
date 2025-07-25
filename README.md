<h1 align="center">
  <br>
  .NET Library Starter Kit
  <br>
</h1>

<h4 align="center">
A battle-tested starter kit for building open-source and internal NuGet libraries, born from half a billion downloads.
</h4>

<div align="center">

[![](https://img.shields.io/github/actions/workflow/status/dennisdoomen/dotnet-package-templates/build.yml?branch=main)](https://github.com/dennisdoomen/dotnet-package-templates/actions?query=branch%3amain)
[![](https://img.shields.io/github/release/dennisdoomen/dotnet-package-templates.svg?label=latest%20release&color=007edf)](https://github.com/dennisdoomen/dotnet-package-templates/releases/latest)
[![](https://img.shields.io/nuget/dt/DotNetLibraryPackageTemplates.svg?label=downloads&color=007edf&logo=nuget)](https://www.nuget.org/packages/DotNetLibraryPackageTemplates)
![GitHub Repo stars](https://img.shields.io/github/stars/dennisdoomen/dotnet-package-templates?style=flat)
[![GitHub contributors](https://img.shields.io/github/contributors/dennisdoomen/dotnet-package-templates)](https://github.com/dennisdoomen/dotnet-package-templates/graphs/contributors)
[![GitHub last commit](https://img.shields.io/github/last-commit/dennisdoomen/dotnet-package-templates)](https://github.com/dennisdoomen/dotnet-package-templates)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/dennisdoomen/dotnet-package-templates)](https://github.com/dennisdoomen/dotnet-package-templates/graphs/commit-activity)
[![open issues](https://img.shields.io/github/issues/dennisdoomen/dotnet-package-templates)](https://github.com/dennisdoomen/dotnet-package-templates/issues)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)](https://makeapullrequest.com)

<a href="#about">About</a> •
<a href="#download">Download</a> •
<a href="#how-to-use-it">How To Use</a> •
<a href="#building">Building</a> •
<a href="#contributors">Contributors</a> •
<a href="#versioning">Versioning</a> •
<a href="#credits">Credits</a> •
<a href="#related">Related</a> •
<a href="#license">License</a>

</div>

## About

### What's this?

A bunch of `dotnet new` templates to quickly get you started building high-quality binary or source-only open-source and non-open-source libraries including everything you need to publish it on NuGet or make it available as open-source.

It includes:
* Multi-targeting to cover as many .NET frameworks as possible
* Separate templates for internal (or "inner sourced") as well as open-source libraries
* Support for both GitHub and Azure DevOps Repositories/Pipelines
* Can create projects for binary or source-only packages
* Code coverage using [Coverlet](https://github.com/coverlet-coverage/coverlet) and [Coveralls.io](https://coveralls.io/)
* Static code analysis using Roslyn analyzers [StyleCopAnalyzers](https://github.com/DotNetAnalyzers/StyleCopAnalyzers), [Roslynator](https://github.com/dotnet/roslynator), [CSharpGuidelinesAnalyzer](https://github.com/bkoelman/CSharpGuidelinesAnalyzer) and [Meziantou](https://github.com/meziantou/Meziantou.Framework).
* Auto-formatting using (a slightly opinionated) `.editorconfig` and settings honored by [JetBrains Rider](https://www.jetbrains.com/rider/) and [ReSharper](https://www.jetbrains.com/resharper/)
* A [Nuke](https://nuke.build/) C# build script that you can run locally as well as in your CI/CD pipeline
* A GitHub Actions workflow that builds, tests, packages and publishes your library
* GitHub issue templates to streamline bug reporting and feature requests
* An extensive read-me
* Automatic versioning using [GitVersion](https://gitversion.net/) and tagging
* Contribution guidelines
* Customized release notes templates for GitHub connected to pull requests labels.
* A test project using [xUnit](https://xunit.net/) and [Fluent Assertions 7](https://fluentassertions.com/)
* Validation of the public API of the library against snapshots using [Verify](https://github.com/VerifyTests/Verify)
* NuGet auditing using .NET built-in auditing capabilities
* NuGet package and licensing scanning through [PackageGuard](https://github.com/dennisdoomen/packageguard?tab=readme-ov-file#readme)

### What's so special about that?

I like to build my software systems in a nicely broken down set of libraries that are easy to maintain, test and deploy based on the [Principles Of Successful Package Management](https://www.dennisdoomen.com/2016/10/principles-for-successful-package.html). However, every time I or the teams I work with need to start a new library or reusable component, we have to piece together so much from other projects that I felt this would fill the gap.

This is the result of years of experience in building in-house and open-source libraries that are used by thousands of developers around the world. I hope it's a great starting point for building your own libraries.

**Tip** You can also use this as a starting point for a [GitHub Template Repository](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-template-repository), fork and adapt the repository for your own company, or use it as an organization template in Azure DevOps.

### Who created this?
My name is Dennis Doomen and I'm a Microsoft MVP and Principal Consultant at [Aviva Solutions](https://avivasolutions.nl/) with 29 years of experience under my belt. As a coding architect, I specialize in designing full-stack enterprise solutions based on .NET as well as providing coaching on all aspects of designing, building, deploying and maintaining software systems. I'm the author of several open-source projects such as [Fluent Assertions](https://www.fluentassertions.com), [Reflectify](https://github.com/dennisdoomen/reflectify), [Liquid Projections](https://www.liquidprojections.net), and I've been maintaining [coding guidelines for C#](https://www.csharpcodingguidelines.com) since 2001.

Contact me through [Email](mailto:dennis.doomen@avivasolutions.nl), [Bluesky](https://bsky.app/profile/dennisdoomen.com), [Twitter/X](https://twitter.com/ddoomen) or [Mastadon](https://mastodon.social/@ddoomen)

## Download

This repository is available as [a NuGet package](https://www.nuget.org/packages/DotNetLibraryPackageTemplates) on https://nuget.org. To install it, use the following command-line:

`dotnet new install DotNetLibraryPackageTemplates`

You can update the templates using the following command.

`dotnet new update`

## How do I use it?

### Generating the library skeleton

1. Create a new directory for your library initialized with Git
1. Run the following command to start building an internal library for GitHub

    `dotnet new nooss-binary-nuget-class-library-sln --name TheNameOfYourAwesomeLibrary`

    Or, if you prefer to build a NuGet package that only adds source files to a project (and avoids binary dependencies)

    `dotnet new nooss-source-only-nuget-class-library-sln --name TheNameOfYourAwesomeLibrary`

    Or, if you want to build an open-source packages, use either of the following

    `dotnet new oss-binary-nuget-class-library-sln --name TheNameOfYourAwesomeLibrary`

    `dotnet new oss-source-only-nuget-class-library-sln --name TheNameOfYourAwesomeLibrary`

    If you are still using Azure DevOps, we got you covered, but you need the organization and project names as well. Again, choose between a binary package or a source-only package.

    `dotnet new azdo-binary-nuget-class-library-sln --name TheNameOfYourAwesomeLibrary --organization MyDevOpsOrganization --project MyDevOpsProject` 

    `dotnet new azdo-source-only-nuget-class-library-sln --name TheNameOfYourAwesomeLibrary --organization MyDevOpsOrganization --project MyDevOpsProject`

1. Make the necessary changes to the generated code (see next section)
1. Commit the changes to your repository into a new commit. Without it, the build script will crash on generating the version number.
1. Run `build.ps1` to build the code, run the tests, and package the library into a NuGet package in the `Artifacts` directory.

### What to do after that

The template makes a lot of assumptions, so after generating the project, there's a couple of things you can tweak.

* Update the `Readme.md` and `PackageReadme.md` with information about your library
* Review the guidelines in `CONTRIBUTING.md` to see if it aligns with how you want to handle contributions
* Review the issue templates under `.github/issue_template`
* Adjust the .NET frameworks this library should target
* Adjust the root namespace and assembly names
* For the source-only packages, update the `.nuspec` file so it represents your information.
* Set-up labels in GitHub matching those in the `release.yml` so you can label pull requests accordingly
* Alter the coverage service that is being used.
* Determine if you want to use API verification against snapshots
* Study the Nuke `build.cs` file or invoking it through `build.ps1 -plan` to see how it works
* See if all dependencies are up-to-date
* Configure NuGet auditing (see next paragraph)
* Fine-tune the allowed open-source licenses and packages in the `.\packageguard\config.json`
* Store the PackageGuard cache that appears under `.\packageguard` after a first build in source control to speed-up successive runs
* Adjust the `funding.yml` to allow people to sponsor your project

> [!NOTE]
> Before the first time the build script has run on your new solution, the `.nuspec` file is still called `nuspec`. This was needed because `dotnet pack` refuses to include the `.nuspec` file in the template package this repository produces. This file is automatically renamed after the first time the `build.ps1` script is run. 


## Additional things to be aware of

### About NuGet auditing
By default, a `dotnet restore` will also check the NuGet packages [for any vulnerabilities](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages). If you run into those, there are a couple of options you can take.
1. Update the dependencies to a version that resolve the vulnerability
1. Update the `WarningsNotAsErrors` element in the `Directory.Build.Props` file to include the relevant `NU190x` error codes as listed [here](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages#warning-codes).
1. Disable auditing entirely by setting the `NuGetAudit` element to `false` in that same `Directory.Build.Props` file.

### About API verification

The `ApiVerificationTests` will generate a `.txt` file containing a representation (per target framework) of the public API of your library. It's a nice technique to prevent accidentally introducing breaking changes. So, whenever the structure of your API changes compared to the snapshot stored in the `ApprovedApi` folder, the test will fail. You can then use `AcceptApiChanges.ps1` to update the snapshots and make the test succeed again. Alternatively, you can install the plug-in [Verify Support](https://plugins.jetbrains.com/plugin/17240-verify-support) by Matthias Koch to do this from inside [JetBrains Rider](https://www.jetbrains.com/rider/) or [ReSharper](https://www.jetbrains.com/resharper/). 

## Building

To build this repository locally, you need the following:
* The [.NET SDKs](https://dotnet.microsoft.com/en-us/download/visual-studio-sdks) for .NET 4.7, 6.0 and 8.0.
* Visual Studio, JetBrains Rider or Visual Studio Code with the C# DevKit

You can also build, run the unit tests and package the code using the following command-line:

`build.ps1`

Or, if you have, the [Nuke tool installed](https://nuke.build/docs/getting-started/installation/):

`nuke`

Also try using `--help` to see all the available options or `--plan` to see what the scripts does.

## Contributors

Your contributions are always welcome! Please have a look at the [contribution guidelines](CONTRIBUTING.md) first.

<a href="https://github.com/dennisdoomen/dotnet-package-templates/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=dennisdoomen/dotnet-package-templates" alt="contrib.rocks image" />
</a>

(Made with [contrib.rocks](https://contrib.rocks))

## Versioning
This library uses [Semantic Versioning](https://semver.org/) to give meaning to the version numbers. For the versions available, see the [tags](/releases) on this repository.

## Credits
This library wouldn't have been possible without the following tools, packages and companies:

* [ASP.NET Core Template](https://github.com/NikolayIT/ASP.NET-Core-Template) - Created by [Nikolay Kostov](https://github.com/NikolayIT) and the inspiration for this repo.
* [Scriban](https://github.com/scriban/scriban/) - A fast, powerful, safe and lightweight scripting language and engine by [Alexandre Mutel](https://github.com/xoofx)
* [Nuke](https://nuke.build/) - Smart automation for DevOps teams and CI/CD pipelines by [Matthias Koch](https://github.com/matkoch)
* [Rider](https://www.jetbrains.com/rider/) - The world's most loved .NET and game dev IDE by [JetBrains](https://www.jetbrains.com/)
* [xUnit](https://xunit.net/) - Community-focused unit testing tool for .NET by [Brad Wilson](https://github.com/bradwilson)
* [Coverlet](https://github.com/coverlet-coverage/coverlet) - Cross platform code coverage for .NET by [Toni Solarin-Sodara](https://github.com/tonerdo)
* [Polysharp](https://github.com/Sergio0694/PolySharp) - Generated, source-only polyfills for C# language features by [Sergio Pedri](https://github.com/Sergio0694)
* [GitVersion](https://gitversion.net/) - From git log to SemVer in no time
* [ReportGenerator](https://reportgenerator.io/) - Converts coverage reports by [Daniel Palme](https://github.com/danielpalme)
* [StyleCopyAnalyzer](https://github.com/DotNetAnalyzers/StyleCopAnalyzers) - StyleCop rules for .NET
* [Roslynator](https://github.com/dotnet/roslynator) - A set of code analysis tools for C# by [Josef Pihrt](https://github.com/josefpihrt)
* [CSharpCodingGuidelines](https://github.com/bkoelman/CSharpGuidelinesAnalyzer) - Roslyn analyzers by [Bart Koelman](https://github.com/bkoelman) to go with the [C# Coding Guidelines](https://csharpcodingguidelines.com/)
* [Meziantou](https://github.com/meziantou/Meziantou.Framework) - Another set of awesome Roslyn analyzers by [Gérald Barré](https://github.com/meziantou)
* [Verify](https://github.com/VerifyTests/Verify) - Snapshot testing by [Simon Cropp](https://github.com/SimonCropp)

## Support the project
* [Github Sponsors](https://github.com/sponsors/dennisdoomen)
* [Tip Me](https://paypal.me/fluentassertions)
* [Buy me a Coffee](https://ko-fi.com/dennisdoomen)
* [Sponsor Me](https://www.patreon.com/bePatron?u=9250052&redirect_uri=http%3A%2F%2Ffluentassertions.com%2F&utm_medium=widget)

## You may also like

* [My Blog](https://www.dennisdoomen.com)
* [FluentAssertions](https://github.com/fluentassertions/fluentassertions) - Extension methods to fluently assert the outcome of .NET tests
* [PackageGuard](https://github.com/dennisdoomen/packageguard?tab=readme-ov-file#readme) - Get a grip on your open-source packages
* [Reflectify](https://github.com/dennisdoomen/reflectify?tab=readme-ov-file#readme) - Reflection extensions without causing dependency pains
* [Pathy](https://github.com/dennisdoomen/pathy?tab=readme-ov-file#readme) - Fluently building and using file and directory paths without binary dependencies
* [C# Coding Guidelines](https://csharpcodingguidelines.com/) - Forkable coding guidelines for all C# versions

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
