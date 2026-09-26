# Adopting the starter kit in an existing library

The `dotnet new` templates are great for a new library. But maybe you already have a library with users, and you want the build script, the analyzers, the API verification and the publishing pipeline without starting again. This guide explains how to add the starter kit to that existing repository.

There are two ways to do this:

* [Use the adoption script](#using-the-adoption-script). It does the mechanical work and tells you what is left.
* [Do it by hand](#doing-it-by-hand). Use this if you can't run PowerShell 7, or if you want full control.

Both ways start from the same idea: generate a solution from the template **using the name of your existing library**, and copy the parts you need. The template replaces `MyPackage` with that name in file names, folder names and file contents. So you never have to rename `MyPackage` yourself.

## Before you start

* Install the templates with `dotnet new install DotNetLibraryPackageTemplates`.
* Make sure your Git working tree is clean, so you can review every change with `git diff` and undo what you don't want.
* Find the exact name of your library project. If your project file is `Acme.Widgets.csproj`, the name is `Acme.Widgets`. The build script and the API verification tests use this name to find your project.
* Pick the template that matches your library:

  | Your library is...                         | Template                                   |
  |--------------------------------------------|--------------------------------------------|
  | Open-source, on GitHub                     | `oss-nuget-class-library-sln`              |
  | Open-source, source-only, on GitHub        | `oss-source-only-nuget-class-library-sln`  |
  | Internal, on GitHub                        | `nooss-nuget-class-library-sln`            |
  | Internal, source-only, on GitHub           | `nooss-source-only-nuget-class-library-sln`|
  | Internal, on Azure DevOps                  | `azdo-nuget-class-library-sln`             |
  | Internal, source-only, on Azure DevOps     | `azdo-source-only-nuget-class-library-sln` |

## Using the adoption script

The script [`Adopt-StarterKit.ps1`](Adopt-StarterKit.ps1) needs [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) and runs on Windows, Linux and macOS.

1. Download the script into the root of your repository:

   ```
   pwsh -Command "Invoke-WebRequest https://raw.githubusercontent.com/dennisdoomen/dotnet-library-starter-kit/main/Adopt-StarterKit.ps1 -OutFile Adopt-StarterKit.ps1"
   ```

1. See what the script would do, without changing anything:

   ```
   pwsh ./Adopt-StarterKit.ps1 -Template oss-nuget-class-library-sln -Name Acme.Widgets -WhatIf
   ```

   For the Azure DevOps templates, also pass `-Organization` and `-Project`.

1. Run it for real:

   ```
   pwsh ./Adopt-StarterKit.ps1 -Template oss-nuget-class-library-sln -Name Acme.Widgets
   ```

1. Follow the "Next steps" that the script prints, and read [After adopting](#after-adopting) below.
1. Delete `Adopt-StarterKit.ps1` from your repository. You don't need it anymore.

### What the script does

* It runs `dotnet new` with your template and library name into a temporary folder.
* It copies all files from that folder into your repository, **except**:
  * the library project folder and the `.Specs` test project folder, because you already have your own code and tests;
  * the approved API snapshots, because they describe the sample class of the template, not your library;
  * the solution file, unless your repository doesn't have one.
* It never overwrites an existing file, unless you pass `-Overwrite`. Files that are the same as the template version are skipped silently. Files that are different are listed, so you can compare them. If you prefer to merge using `git diff`, run it again with `-Overwrite`.
* If your repository has one solution file, it:
  * points `.nuke/parameters.json` and `Build/Build.cs` to that solution;
  * adds the API verification test project to it;
  * adds the build project to it (for `.slnx` files only), excluded from the solution build.
* It checks your repository and prints the steps you still need to do by hand, for example:
  * the MSBuild properties and package references in the template's `Directory.Build.props` and library project that are missing from yours;
  * a library project that isn't in the `<Name>/<Name>.csproj` folder the API verification tests expect;
  * a test project that isn't called `<Name>.Specs`, which is the name the build script uses.
* It keeps the generated template in the temporary folder, so you can compare it with your own files. The script prints where that folder is.

The script contains no hard-coded list of template files. Whatever the installed template produces is what gets copied. This means the script stays in sync with the templates. The build of this repository also runs the script against a small existing library on every change, to make sure it keeps working.

## Doing it by hand

1. Generate the template next to your repository, using your library name:

   ```
   dotnet new oss-nuget-class-library-sln --name Acme.Widgets --output ../starterkit
   ```

1. Copy these files and folders into the root of your repository. If a file already exists, compare the two versions and merge them.

   | What                     | Files                                                                                               |
   |--------------------------|-----------------------------------------------------------------------------------------------------|
   | Build script             | `Build/`, `build.ps1`, `build.sh`, `build.cmd`, `.nuke/`, `global.json`, `GitVersion.yml`           |
   | Code style and analyzers | `.editorconfig`, `<Name>.sln.DotSettings`, and the analyzer settings in `Directory.Build.props`     |
   | API verification         | `<Name>.ApiVerificationTests/` without the `ApprovedApi` folder, `AcceptApiChanges.ps1`, `AcceptApiChanges.sh` |
   | Pipelines                | `.github/` (GitHub) or `Build/azure-pipelines.yaml` (Azure DevOps)                                  |
   | Package scanning         | `.packageguard/`                                                                                    |
   | AI coding guidelines     | `.agents/`                                                                                          |
   | Packaging                | `PackageIcon.png`, `PackageReadme.md`                                                               |
   | Git settings             | `.gitattributes`, `.gitignore`                                                                      |
   | Documentation            | `README.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `LICENSE` (only if you don't have them yet)   |

   Don't copy the library project folder, the `.Specs` folder or the solution file. Instead, compare them with your own and take over what you need.

1. Merge the properties from the template's library project (for example `<Name>/<Name>.csproj`) into yours. The package metadata, such as `PackageIcon`, `PackageReadmeFile`, `PackageLicenseFile` and the `Package files` item group, is what makes `dotnet pack` produce a complete package.
1. If your solution file has a different name than `<Name>.slnx`, update `.nuke/parameters.json` and the `InspectCode` call in `Build/Build.cs`.
1. Add `<Name>.ApiVerificationTests` to your solution. Optionally, also add `Build/_build.csproj`, but exclude it from the solution build.
1. Read [After adopting](#after-adopting) below.

## After adopting

These steps apply to both ways.

* **Library location.** The API verification tests expect your library at `<Name>/<Name>.csproj`. If it lives somewhere else, for example in `src/`, update the paths in `<Name>.ApiVerificationTests/ApiApproval.cs`.
* **Test project.** The `RunTests` target in `Build/Build.cs` runs the tests in `<Name>.Specs`. If your test project has another name, change it there. If you have more than one test project, change the target to run all of them.
* **Target frameworks.** The analyzers only run for the newest target framework, `net10.0`, to keep the build fast. If your library doesn't target `net10.0`, change the conditions in `Directory.Build.props`.
* **Commit first.** GitVersion calculates the version number from your Git history. Commit the changes before you run the build script.
* **First build.** Run `build.ps1` or `build.sh`. The API verification tests fail the first time, because there is no approved snapshot of your public API yet. Check the `*.received.txt` files, run `AcceptApiChanges.ps1` or `AcceptApiChanges.sh` to accept them, and commit the `ApprovedApi` folder.
* **Analyzer findings.** An existing code base usually has many analyzer findings, and `TreatWarningsAsErrors` turns them into errors. You can fix them all at once, or lower the severity of specific rules in `.editorconfig` and fix them step by step.
* **Package versions.** If your library is already on NuGet, check that GitVersion continues from your latest version. Add a Git tag for your latest release if needed, or set `next-version` in `GitVersion.yml`.
* **Secrets.** The GitHub workflow uses a `NUGETAPIKEY` secret to publish to NuGet. Add it to your repository settings.
* **Everything else.** The generated `README.md` has a list of other things to review, such as the issue templates, the PackageGuard settings and the coverage service.
