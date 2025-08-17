{{ if !package_readme }}
<h1 align="center">
  <br>
  MyPackage
  <br>
</h1>

{{ if source_only }}
<h4 align="center">Nice tagline about your package, mentioning it can be used without the dependency hell</h4>
{{ else }}
<h4 align="center">Nice tagline about your package</h4>
{{ end }}

<div align="center">

{{~ if !azdo ~}}
[![](https://img.shields.io/github/actions/workflow/status/your-github-username/mypackage/build.yml?branch=main)](https://github.com/your-github-username/mypackage/actions?query=branch%3amain)
[![Coveralls branch](https://img.shields.io/coverallsCoverage/github/your-github-username/mypackage?branch=main)](https://coveralls.io/github/your-github-username/mypackage?branch=main)
[![](https://img.shields.io/github/release/your-github-username/mypackage.svg?label=latest%20release&color=007edf)](https://github.com/your-github-username/mypackage/releases/latest)
[![](https://img.shields.io/nuget/dt/mypackage.svg?label=downloads&color=007edf&logo=nuget)](https://www.nuget.org/packages/mypackage)
[![](https://img.shields.io/librariesio/dependents/nuget/mypackage.svg?label=dependent%20libraries)](https://libraries.io/nuget/mypackage)
![GitHub Repo stars](https://img.shields.io/github/stars/your-github-username/mypackage?style=flat)
[![GitHub contributors](https://img.shields.io/github/contributors/your-github-username/mypackage)](https://github.com/your-github-username/mypackage/graphs/contributors)
[![GitHub last commit](https://img.shields.io/github/last-commit/your-github-username/mypackage)](https://github.com/your-github-username/mypackage)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/your-github-username/mypackage)](https://github.com/your-github-username/mypackage/graphs/commit-activity)
[![open issues](https://img.shields.io/github/issues/your-github-username/mypackage)](https://github.com/your-github-username/mypackage/issues)
{{~ end ~}}
![Static Badge](https://img.shields.io/badge/4.7%2C_8.0%2C_netstandard2.0%2C_netstandard2.1-dummy?label=dotnet&color=%235027d5)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)](https://makeapullrequest.com)
![](https://img.shields.io/badge/release%20strategy-githubflow-orange.svg)

<a href="#about">About</a> •
<a href="#how-to-use-it">How To Use</a> •
<a href="#download">Download</a> •
<a href="#building">Building</a> •
<a href="#contributing">Contributing</a> •
<a href="#versioning">Versioning</a> •
<a href="#credits">Credits</a> •
<a href="#related">Related</a> •
{{~ if open_source ~}}
<a href="#license">License</a>
{{~ end ~}}

</div>

{{ end ~}}

> [!TIP]
> What to do next after generating the template

The template makes a lot of assumptions, so after generating the project, there's a couple of things you can tweak.

* Update the `Readme.md` and `PackageReadme.md` with information about your library
* Review the guidelines in `CONTRIBUTING.md` to see if it aligns with how you want to handle contributions
{{~ if !azdo ~}}
* Review the issue templates under `.github/issue_template`
* Set-up labels in GitHub matching those in the `release.yml` so you can label pull requests accordingly
{{~ end ~}}
* Adjust the .NET frameworks this library should target
* Adjust the root namespace and assembly names
{{~ if source_only ~}}
* For the source-only packages, update the `.nuspec` file so it represents your information.
{{~ end ~}}
* Alter the coverage service that is being used.
* Determine if you want to use API verification against snapshots
* Study the Nuke `build.cs` file or invoking it through `build.ps1 -plan` to see how it works
* See if all dependencies are up-to-date
* Configure NuGet auditing (see next paragraph)
* Fine-tune the allowed open-source licenses and packages in the `.\packageguard\config.json`
* Store the PackageGuard cache that appears under `.\packageguard` after a first build in source control to speed-up successive runs
{{~ if open_source ~}}
* Adjust the `funding.yml` to allow people to sponsor your project
{{~ end ~}}

> [!NOTE]
> Before the first time the build script has run on your new solution, the `.nuspec` file is still called `nuspec`. This was needed because `dotnet pack` refuses to include the `.nuspec` file in the template package this repository produces. This file is automatically renamed after the first time the `build.ps1` script is run. 

> [!TIP]
> Also check-out the [main repository](https://github.com/dennisdoomen/dotnet-library-starter-kit) for additional information on these generated solutions.

## About

### What's this?

Add stuff like:
* MyPackage offers
* what .NET, C# other versions of dependencies it supports

### What's so special about that?

* What makes it different from other libraries?
* Why did you create it.
* What problem does it solve?
{{ if source_only ~}}
* Mention that it is a source-only package, which just adds a C# file and doesn't create binary dependencies
{{ end ~}}

### Who created this?
* Something about you, your company, your team, etc.
{{ if open_source }}
* How to contact you like LinkedIn, Twitter, Bluesky, Mastodon, email, etc.
{{ end ~}}

## How do I use it?
* Code examples
* Where to find more examples

```csharp
Some example code showing your library
```

## Download
{{ if open_source }}
This library is available as [a NuGet package](https://www.nuget.org/packages/mypackage) on https://nuget.org. To install it, use the following command-line:

  `dotnet add package mypackage`

{{~ else if azdo ~}}
This library is available as [a NuGet package] on https://dev.azure.com/MyOrganization/MyPackage/_artifacts. To install it, use the following command-line:

`dotnet add package Fnv.IntegrationPlatform.Crm`

{{~ else ~}}
This library can be installed by adding the GitHub Packages feed to your package manager:

 {%{ `dotnet nuget add source --username USERNAME --password ${{ secrets.GITHUB_TOKEN }} --store-password-in-clear-text --name github "https://nuget.pkg.github.com/NAMESPACE/index.json"` }%}

Read more about [GitHub Packages](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry).

Then, install the package using the following command-line:

  `dotnet add package mypackage`
{{ end ~}}

## Building

To build this repository locally, you need the following:
* The [.NET SDKs](https://dotnet.microsoft.com/en-us/download/visual-studio-sdks) for .NET 4.7 and 8.0.
* Visual Studio, JetBrains Rider or Visual Studio Code with the C# DevKit

You can also build, run the unit tests and package the code using the following command-line:

`build.ps1`

Or, if you have, the [Nuke tool installed](https://nuke.build/docs/getting-started/installation/):

`nuke`

Also try using `--help` to see all the available options or `--plan` to see what the scripts does.

{{~ if !package_readme ~}}
## Contributing

Your contributions are always welcome! Please have a look at the [contribution guidelines](CONTRIBUTING.md) first.

{{~ if open_source ~}}
Previous contributors include:

<a href="https://github.com/your-github-username/mypackage/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=your-github-username/mypackage" alt="contrib.rocks image" />
</a>

(Made with [contrib.rocks](https://contrib.rocks))
{{~ end ~}}
{{~ end ~}}

## Versioning
This library uses [Semantic Versioning](https://semver.org/) to give meaning to the version numbers. For the versions available, see the [tags](/releases) on this repository.

## Credits
This library wouldn't have been possible without the following tools, packages and companies:

* [Nuke](https://nuke.build/) - Smart automation for DevOps teams and CI/CD pipelines by [Matthias Koch](https://github.com/matkoch)
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

{{~ if open_source ~}}
## Support the project
* [Github Sponsors](https://github.com/sponsors/your-github-username)
* [Tip Me](https://paypal.me/your-paypal-username)
* [Buy me a Coffee](https://ko-fi.com/your-github-username)
* [Patreon](https://patreon.com/your-patreon-username)

{{~ if !package_readme ~}}
## You may also like

* Your blog
* Your other projects
* Related projects you think are cool or interesting for the consumers of this project

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
{{ end -}}
{{ end -}}
