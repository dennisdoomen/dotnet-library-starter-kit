#Requires -Version 7.0

<#
.SYNOPSIS
    Adds the .NET Library Starter Kit to an existing library repository.

.DESCRIPTION
    Generates a solution from one of the starter kit templates using the name of your existing library,
    copies the infrastructure files (build script, analyzers, pipelines, API verification, etc.) into
    your repository and reports what you still need to merge by hand.

    Because the files come from running the installed template, the file names, folder names and
    file contents already use your library name. And because nothing is hard-coded in this script,
    it stays in sync with whatever version of the templates you have installed.

    By default, existing files are never overwritten. Run it on a clean Git working tree with
    -Overwrite if you prefer to review the changes using `git diff`.

.PARAMETER Template
    The short name of the template to use, e.g. oss-nuget-class-library-sln. Run
    `dotnet new list class-library-sln` to see all options.

.PARAMETER Name
    The name of your existing library. This must match the name of the library project
    (e.g. Acme.Widgets for Acme.Widgets.csproj), because the template uses it for project names,
    namespaces and paths.

.PARAMETER Path
    The root directory of your existing repository. Defaults to the current directory.

.PARAMETER Organization
    The Azure DevOps organization. Only needed for the azdo-* templates.

.PARAMETER Project
    The Azure DevOps project. Only needed for the azdo-* templates.

.PARAMETER Overwrite
    Overwrite files that already exist in your repository.

.EXAMPLE
    ./Adopt-StarterKit.ps1 -Template oss-nuget-class-library-sln -Name Acme.Widgets -Path ~/src/acme-widgets

.EXAMPLE
    ./Adopt-StarterKit.ps1 -Template oss-nuget-class-library-sln -Name Acme.Widgets -WhatIf
#>
[CmdletBinding(SupportsShouldProcess = $true)]
Param(
    [Parameter(Mandatory = $true)]
    [string]$Template,

    [Parameter(Mandatory = $true)]
    [string]$Name,

    [string]$Path = (Get-Location).Path,

    [string]$Organization,

    [string]$Project,

    [switch]$Overwrite
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = "Stop"

$Path = (Resolve-Path $Path).Path

# These folders hold the library and its tests. You already have those, so we never copy them.
$UserCodeFolders = @($Name, "$Name.Specs")

$FollowUps = [System.Collections.Generic.List[string]]::new()

function Get-RelativePath([string]$BasePath, [string]$FullPath) {
    return [System.IO.Path]::GetRelativePath($BasePath, $FullPath).Replace('\', '/')
}

function Test-IsUserCode([string]$RelativePath) {
    $topFolder = $RelativePath.Split('/')[0]
    if ($UserCodeFolders -contains $topFolder) {
        return $true
    }

    # The approved API snapshots describe the sample class of the template, not your library
    return $RelativePath -like "$Name.ApiVerificationTests/ApprovedApi/*"
}

function Get-MissingMsBuildElements([string]$TemplateFile, [string]$ExistingFile) {
    [xml]$templateXml = Get-Content $TemplateFile -Raw
    [xml]$existingXml = Get-Content $ExistingFile -Raw

    $existingProperties = @($existingXml.SelectNodes("/Project/PropertyGroup/*") | ForEach-Object { $_.LocalName })
    $existingPackages = @($existingXml.SelectNodes("/Project/ItemGroup/PackageReference") | ForEach-Object { $_.Include })

    $missing = @()
    foreach ($property in $templateXml.SelectNodes("/Project/PropertyGroup/*")) {
        if ($existingProperties -notcontains $property.LocalName) {
            $missing += "<$($property.LocalName)>$($property.InnerText.Trim())</$($property.LocalName)>"
        }
    }

    foreach ($package in $templateXml.SelectNodes("/Project/ItemGroup/PackageReference")) {
        if ($existingPackages -notcontains $package.Include) {
            $missing += "<PackageReference Include=`"$($package.Include)`" Version=`"$($package.Version)`" />"
        }
    }

    return $missing | Select-Object -Unique
}

function Add-MergeFollowUp([string]$Description, [string]$TemplateFile, [string]$ExistingFile) {
    $missing = @(Get-MissingMsBuildElements $TemplateFile $ExistingFile)
    if ($missing.Count -gt 0) {
        $FollowUps.Add("$Description is missing these elements from the template (see $TemplateFile):`n      " + ($missing -join "`n      "))
    }
}

###########################################################################
# GENERATE THE TEMPLATE WITH YOUR LIBRARY NAME
###########################################################################

if (-not (dotnet new list $Template 2>$null | Select-String -SimpleMatch $Template)) {
    throw "Template '$Template' is not installed. Run 'dotnet new install DotNetLibraryPackageTemplates' first."
}

$generated = Join-Path ([System.IO.Path]::GetTempPath()) "starterkit-$Name-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"

$templateArguments = @("new", $Template, "--name", $Name, "--output", $generated)
if ($Organization) { $templateArguments += @("--organization", $Organization) }
if ($Project) { $templateArguments += @("--project", $Project) }

Write-Host "Generating '$Template' for '$Name' into $generated"
& dotnet @templateArguments | Out-Null
if ($LASTEXITCODE) {
    throw "dotnet new failed with exit code $LASTEXITCODE"
}

###########################################################################
# COPY THE INFRASTRUCTURE FILES
###########################################################################

$copied = @()
$kept = @()

foreach ($file in Get-ChildItem $generated -Recurse -File -Force) {
    $relativePath = Get-RelativePath $generated $file.FullName

    if ((Test-IsUserCode $relativePath) -or $relativePath -eq "$Name.slnx") {
        continue
    }

    $destination = Join-Path $Path $relativePath
    if (Test-Path $destination) {
        if ((Get-FileHash $destination).Hash -eq (Get-FileHash $file.FullName).Hash) {
            continue
        }

        if (-not $Overwrite) {
            $kept += $relativePath
            continue
        }
    }

    if ($PSCmdlet.ShouldProcess($relativePath, "Copy from template")) {
        New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
        Copy-Item $file.FullName $destination -Force
    }

    $copied += $relativePath
}

###########################################################################
# WIRE UP THE EXISTING SOLUTION
###########################################################################

$solutions = @(Get-ChildItem $Path -File | Where-Object { $_.Extension -in ".sln", ".slnx" })
$apiTestsProject = "$Name.ApiVerificationTests/$Name.ApiVerificationTests.csproj"

if ($solutions.Count -eq 0) {
    if ($PSCmdlet.ShouldProcess("$Name.slnx", "Copy from template")) {
        Copy-Item (Join-Path $generated "$Name.slnx") (Join-Path $Path "$Name.slnx")
    }

    $copied += "$Name.slnx"
    $FollowUps.Add("No solution was found, so $Name.slnx was copied from the template. Make sure its project paths match your repository.")
}
elseif ($solutions.Count -gt 1) {
    $FollowUps.Add("Found more than one solution file. Update .fallout/parameters.json and the InspectCode call in Build/Build.cs to point to the one the build should use.")
}
else {
    $solution = $solutions[0]

    if ($solution.Name -ne "$Name.slnx") {
        foreach ($file in @(".fallout/parameters.json", "Build/Build.cs") | Where-Object { $copied -contains $_ }) {
            $fullPath = Join-Path $Path $file
            if ($PSCmdlet.ShouldProcess($file, "Point to $($solution.Name)")) {
                (Get-Content $fullPath -Raw).Replace("`"$Name.slnx", "`"$($solution.Name)") |
                    Set-Content $fullPath -NoNewline
            }
        }
    }

    if ($copied -contains $apiTestsProject -and $PSCmdlet.ShouldProcess($solution.Name, "Add $apiTestsProject")) {
        & dotnet sln $solution.FullName add (Join-Path $Path $apiTestsProject) | Out-Null
    }

    if ($solution.Extension -eq ".slnx" -and $copied -contains "Build/_build.csproj") {
        [xml]$solutionXml = Get-Content $solution.FullName -Raw
        if (-not $solutionXml.SelectSingleNode("/Solution/Project[@Path='Build/_build.csproj']")) {
            if ($PSCmdlet.ShouldProcess($solution.Name, "Add Build/_build.csproj (excluded from build)")) {
                $buildProject = $solutionXml.CreateElement("Project")
                $buildProject.SetAttribute("Path", "Build/_build.csproj")
                $buildElement = $solutionXml.CreateElement("Build")
                $buildElement.SetAttribute("Project", "false")
                $buildProject.AppendChild($buildElement) | Out-Null
                $solutionXml.DocumentElement.PrependChild($buildProject) | Out-Null
                $solutionXml.Save($solution.FullName)
            }
        }
    }
    elseif ($solution.Extension -eq ".sln") {
        $FollowUps.Add("Optionally add Build/_build.csproj to $($solution.Name) so you can edit the build script from your IDE. Exclude it from all solution configurations so it isn't built with the rest of your code.")
    }
}

###########################################################################
# REPORT WHAT NEEDS TO BE DONE BY HAND
###########################################################################

$libraryProject = Get-ChildItem $Path -Recurse -File -Filter "$Name.csproj" |
    Where-Object { $_.FullName -notlike "$generated*" } |
    Select-Object -First 1

if (-not $libraryProject) {
    $FollowUps.Add("Could not find $Name.csproj. The build script and the API verification tests expect the library project to be called $Name.")
}
else {
    $expectedLocation = Join-Path $Path $Name "$Name.csproj"
    if ($libraryProject.FullName -ne $expectedLocation) {
        $FollowUps.Add("Your library project lives at $(Get-RelativePath $Path $libraryProject.FullName), but $Name.ApiVerificationTests/ApiApproval.cs expects it at $Name/$Name.csproj. Update the paths in that file.")
    }

    Add-MergeFollowUp "$Name.csproj" (Join-Path $generated $Name "$Name.csproj") $libraryProject.FullName
}

if (-not (Get-ChildItem $Path -Recurse -File -Filter "$Name.Specs.csproj" | Where-Object { $_.FullName -notlike "$generated*" })) {
    $FollowUps.Add("Build/Build.cs runs the tests in the project $Name.Specs. Change the name in the RunTests target to the name of your own test project.")
}

if ($kept -contains "Directory.Build.props") {
    Add-MergeFollowUp "Your Directory.Build.props" (Join-Path $generated "Directory.Build.props") (Join-Path $Path "Directory.Build.props")
}

foreach ($folder in $UserCodeFolders) {
    if (Test-Path (Join-Path $generated $folder)) {
        $FollowUps.Add("Compare your project with the template's $folder folder in $generated for anything else you want to adopt.")
    }
}

$FollowUps.Add("Commit the changes. GitVersion needs at least one commit, or the build script will fail.")
$FollowUps.Add("Run build.ps1 (or build.sh). The API verification tests fail the first time, because there are no approved snapshots yet. Run AcceptApiChanges.ps1 (or AcceptApiChanges.sh) to accept them.")

Write-Host ""
Write-Host "Copied $($copied.Count) file(s):" -ForegroundColor Green
$copied | ForEach-Object { Write-Host "  $_" }

if ($kept.Count -gt 0) {
    Write-Host ""
    Write-Host "Kept $($kept.Count) existing file(s). Compare them with the template versions in $generated, or run again with -Overwrite and use 'git diff':" -ForegroundColor Yellow
    $kept | ForEach-Object { Write-Host "  $_" }
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
$step = 1
foreach ($followUp in $FollowUps) {
    Write-Host "  $step. $followUp"
    $step++
}

Write-Host ""
Write-Host "The generated template is kept in $generated. Delete it when you're done."
