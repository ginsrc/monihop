[CmdletBinding()]
param(
    [ValidateSet("win-x64")]
    [string]$RuntimeIdentifier = "win-x64",
    [string]$InnoCompiler
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$projectPath = Join-Path $repositoryRoot "src\MoniHop.Desktop\MoniHop.Desktop.csproj"
$artifactsDirectory = Join-Path $repositoryRoot "artifacts"
$stagingDirectory = Join-Path $artifactsDirectory "staging"
$installedDirectory = Join-Path $stagingDirectory "installed"
$portableContainer = Join-Path $stagingDirectory "portable"
$portableDirectory = Join-Path $portableContainer "MoniHop"
$installerScript = Join-Path $PSScriptRoot "MoniHop.iss"
$portableMarker = Join-Path $PSScriptRoot "portable.flag"

function Reset-GeneratedDirectory([string]$Path) {
    $fullPath = [IO.Path]::GetFullPath($Path)
    $allowedRoot = [IO.Path]::GetFullPath($artifactsDirectory).TrimEnd('\') + '\'
    if (-not $fullPath.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to reset a directory outside the artifacts directory: $fullPath"
    }

    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $fullPath -Force | Out-Null
}

function Resolve-DotNet {
    $bundledPath = Join-Path $env:ProgramFiles "dotnet\dotnet.exe"
    if (Test-Path -LiteralPath $bundledPath) {
        return $bundledPath
    }

    $command = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    throw ".NET SDK was not found. Install the SDK version declared in global.json."
}

function Resolve-InnoCompiler([string]$RequestedPath) {
    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        $resolved = [IO.Path]::GetFullPath($RequestedPath)
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            throw "Inno Setup compiler was not found at: $resolved"
        }

        return $resolved
    }

    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    $uninstallRoots = @(
        "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*"
    )
    $installation = Get-ItemProperty $uninstallRoots -ErrorAction SilentlyContinue |
        Where-Object {
            $_.PSObject.Properties.Name -contains "DisplayName" -and
            $_.DisplayName -like "Inno Setup 7*"
        } |
        Select-Object -First 1
    if ($null -ne $installation -and
        $installation.PSObject.Properties.Name -contains "InstallLocation" -and
        -not [string]::IsNullOrWhiteSpace($installation.InstallLocation)) {
        $candidate = Join-Path $installation.InstallLocation "ISCC.exe"
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return $candidate
        }
    }

    throw "Inno Setup 7 compiler was not found. Pass -InnoCompiler with the full ISCC.exe path."
}

function Invoke-Native([string]$FilePath, [string[]]$Arguments) {
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

[xml]$project = Get-Content -LiteralPath $projectPath -Raw
$version = [string]($project.Project.PropertyGroup.Version | Select-Object -First 1)
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "The application version is missing from $projectPath."
}

$dotnet = Resolve-DotNet
$iscc = Resolve-InnoCompiler $InnoCompiler
New-Item -ItemType Directory -Path $artifactsDirectory -Force | Out-Null
Reset-GeneratedDirectory $stagingDirectory
New-Item -ItemType Directory -Path $installedDirectory -Force | Out-Null

Invoke-Native $dotnet @(
    "publish",
    $projectPath,
    "-c", "Release",
    "-r", $RuntimeIdentifier,
    "--self-contained", "true",
    "-p:PublishTrimmed=false",
    "-p:PublishSingleFile=false",
    "-p:DebugSymbols=false",
    "-p:DebugType=None",
    "-o", $installedDirectory,
    "--nologo"
)

if (Test-Path -LiteralPath (Join-Path $installedDirectory "portable.flag")) {
    throw "Installed publish output must not contain portable.flag."
}

New-Item -ItemType Directory -Path $portableContainer -Force | Out-Null
Copy-Item -LiteralPath $installedDirectory -Destination $portableDirectory -Recurse
Copy-Item -LiteralPath $portableMarker -Destination (Join-Path $portableDirectory "portable.flag")

$portableArchive = Join-Path $artifactsDirectory "MoniHop-$version-$RuntimeIdentifier-portable.zip"
if (Test-Path -LiteralPath $portableArchive) {
    Remove-Item -LiteralPath $portableArchive -Force
}
Compress-Archive -LiteralPath $portableDirectory -DestinationPath $portableArchive -CompressionLevel Optimal

$installerName = "MoniHop-$version-$RuntimeIdentifier-setup"
$installerPath = Join-Path $artifactsDirectory "$installerName.exe"
$compilerOutputDirectory = Join-Path (
    [IO.Path]::GetTempPath()) (
    "MoniHop-Inno-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $compilerOutputDirectory -Force | Out-Null
try {
    Invoke-Native $iscc @(
        "--no-ide-signtools",
        "--no-signing",
        "--define=AppVersion=$version",
        "--define=PublishDirectory=$installedDirectory",
        "--output-dir=$compilerOutputDirectory",
        "--output-filename=$installerName",
        $installerScript
    )

    if (Test-Path -LiteralPath $installerPath) {
        Remove-Item -LiteralPath $installerPath -Force
    }
    Copy-Item -LiteralPath (
        Join-Path $compilerOutputDirectory "$installerName.exe") -Destination $installerPath
}
finally {
    if (Test-Path -LiteralPath $compilerOutputDirectory) {
        Remove-Item -LiteralPath $compilerOutputDirectory -Recurse -Force
    }
}

$checksumPath = Join-Path $artifactsDirectory "SHA256SUMS.txt"
$checksumLines = @($installerPath, $portableArchive) | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_ -Algorithm SHA256
    "$($hash.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($_))"
}
Set-Content -LiteralPath $checksumPath -Value $checksumLines -Encoding ascii

Write-Output "Release artifacts:"
Write-Output "  $installerPath"
Write-Output "  $portableArchive"
Write-Output "  $checksumPath"
