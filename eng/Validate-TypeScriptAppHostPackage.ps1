param(
    [string] $Configuration = "Release",
    [string] $PackageVersion = "9999.0.0",
    [string] $RailwayDependencyFeed = $env:RAILWAY_DEPENDENCY_FEED
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

if ($PackageVersion -notmatch '^(?:0|[1-9]\d*)\.(?:0|[1-9]\d*)\.(?:0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$') {
    throw "PackageVersion must be a canonical package version without build metadata or path separators."
}

function Remove-ContainedDirectory {
    param([string] $Path, [string] $Root)
    $fullRoot = [IO.Path]::GetFullPath($Root).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $fullPath = [IO.Path]::GetFullPath($Path)
    if (-not $fullPath.StartsWith($fullRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Cleanup target is outside the intended artifacts directory."
    }
    Remove-Item -LiteralPath $fullPath -Recurse -Force -ErrorAction SilentlyContinue
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "Aspire.Hosting.RabbitMQ.Railway.slnx"
$fixtureSource = Join-Path $repoRoot "tests/Aspire.Hosting.RabbitMQ.Railway/Fixtures/TypeScriptAppHost"
$artifactsRoot = Join-Path $repoRoot "artifacts/typescript-apphost-package"
$packageOutput = Join-Path $artifactsRoot "packages"
$fixtureWork = Join-Path $artifactsRoot "fixture"
$nugetPackages = Join-Path $artifactsRoot ".nuget-packages"
$packageId = "PinguApps.Aspire.Hosting.RabbitMQ.Railway"

if ([string]::IsNullOrWhiteSpace($RailwayDependencyFeed)) {
    $RailwayDependencyFeed = & (Join-Path $PSScriptRoot "Prepare-RailwayDependency.ps1") -Configuration $Configuration
}

Remove-ContainedDirectory -Path $artifactsRoot -Root (Join-Path $repoRoot "artifacts")
New-Item $packageOutput -ItemType Directory -Force | Out-Null
New-Item $nugetPackages -ItemType Directory -Force | Out-Null

$restoreArguments = @("restore", $solutionPath)
if (-not [string]::IsNullOrWhiteSpace($RailwayDependencyFeed)) {
    $restoreConfig = Join-Path $artifactsRoot "restore.NuGet.Config"
    $escapedFeed = [Security.SecurityElement]::Escape([IO.Path]::GetFullPath($RailwayDependencyFeed))
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear /><add key="railway-reviewed-source" value="$escapedFeed" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>
"@ | Set-Content -LiteralPath $restoreConfig -Encoding UTF8
    $restoreArguments += @("--configfile", $restoreConfig)
}
$previousBuildPackages = $env:NUGET_PACKAGES
try {
    $env:NUGET_PACKAGES = Join-Path $artifactsRoot (".build-packages-" + [guid]::NewGuid().ToString("N"))
    dotnet @restoreArguments
    if ($LASTEXITCODE -ne 0) { throw "Solution restore failed." }
    dotnet build $solutionPath -c $Configuration --no-restore -p:ContinuousIntegrationBuild=true
    if ($LASTEXITCODE -ne 0) { throw "Solution build failed." }
    dotnet pack $solutionPath -c $Configuration --no-build -p:Version=$PackageVersion -o $packageOutput
    if ($LASTEXITCODE -ne 0) { throw "Solution packaging failed." }
}
finally {
    if ($null -eq $previousBuildPackages) { Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue }
    else { $env:NUGET_PACKAGES = $previousBuildPackages }
}

$packageFile = Join-Path $packageOutput "$packageId.$PackageVersion.nupkg"
$packageCacheId = $packageId.ToLowerInvariant()
$packageCachePath = Join-Path $nugetPackages "$packageCacheId/$PackageVersion"

Remove-ContainedDirectory -Path $packageCachePath -Root $artifactsRoot
New-Item $packageCachePath -ItemType Directory -Force | Out-Null

Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($packageFile, $packageCachePath)
Copy-Item $packageFile (Join-Path $packageCachePath "$packageCacheId.$PackageVersion.nupkg")

$packageBytes = [IO.File]::ReadAllBytes($packageFile)
$packageHash = [Convert]::ToBase64String([System.Security.Cryptography.SHA512]::HashData($packageBytes))

Set-Content (Join-Path $packageCachePath "$packageCacheId.$PackageVersion.nupkg.sha512") $packageHash -Encoding ASCII

[ordered]@{
    version = 2
    contentHash = $packageHash
    source = (Resolve-Path $packageOutput).Path
} | ConvertTo-Json | Set-Content (Join-Path $packageCachePath ".nupkg.metadata") -Encoding UTF8

if (-not [string]::IsNullOrWhiteSpace($RailwayDependencyFeed)) {
    $dependencyId = "pinguapps.aspire.hosting.railway"
    $dependencyFile = Join-Path $RailwayDependencyFeed "PinguApps.Aspire.Hosting.Railway.1.0.0.nupkg"
    $dependencyCache = Join-Path $nugetPackages "$dependencyId/1.0.0"
    New-Item $dependencyCache -ItemType Directory -Force | Out-Null
    [IO.Compression.ZipFile]::ExtractToDirectory($dependencyFile, $dependencyCache)
    Copy-Item -LiteralPath $dependencyFile -Destination (Join-Path $dependencyCache "$dependencyId.1.0.0.nupkg")
    $dependencyHash = [Convert]::ToBase64String([Security.Cryptography.SHA512]::HashData([IO.File]::ReadAllBytes($dependencyFile)))
    Set-Content (Join-Path $dependencyCache "$dependencyId.1.0.0.nupkg.sha512") $dependencyHash -Encoding ASCII
    [ordered]@{version = 2; contentHash = $dependencyHash; source = $RailwayDependencyFeed} | ConvertTo-Json |
        Set-Content (Join-Path $dependencyCache ".nupkg.metadata") -Encoding UTF8
}

Copy-Item $fixtureSource $fixtureWork -Recurse
Remove-ContainedDirectory -Path (Join-Path $fixtureWork ".aspire") -Root $artifactsRoot
Remove-ContainedDirectory -Path (Join-Path $fixtureWork ".modules") -Root $artifactsRoot
Remove-ContainedDirectory -Path (Join-Path $fixtureWork "node_modules") -Root $artifactsRoot

$aspireConfigPath = Join-Path $fixtureWork "aspire.config.json"
$aspireConfig = Get-Content $aspireConfigPath -Raw | ConvertFrom-Json
$aspireConfig.packages.$packageId = $PackageVersion
$aspireConfig | ConvertTo-Json -Depth 10 | Set-Content $aspireConfigPath -Encoding UTF8

$packageOutputFullPath = (Resolve-Path $packageOutput).Path
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-package-gate" value="$packageOutputFullPath" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local-package-gate">
      <package pattern="PinguApps.Aspire.Hosting.RabbitMQ.Railway" />
      <package pattern="PinguApps.Aspire.Hosting.Railway" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content (Join-Path $fixtureWork "NuGet.Config") -Encoding UTF8

Push-Location $fixtureWork
try {
    $previousNuGetPackages = $env:NUGET_PACKAGES
    $env:NUGET_PACKAGES = $nugetPackages

    aspire restore --non-interactive
    if ($LASTEXITCODE -ne 0) { throw "Aspire restore failed." }
    npm ci --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw "npm ci failed." }
    npm run typecheck
    if ($LASTEXITCODE -ne 0) { throw "TypeScript typecheck failed." }
    aspire publish --non-interactive --list-steps
    if ($LASTEXITCODE -ne 0) { throw "Aspire publish pipeline inspection failed." }
    $deploymentSteps = aspire deploy --non-interactive --list-steps
    if ($LASTEXITCODE -ne 0) { throw "Aspire deployment pipeline inspection failed." }
    $deploymentSteps | Write-Output
    if (($deploymentSteps -join "`n") -notmatch 'railway-deploy-rabbitmq-management') {
        throw "The NuGet-backed fixture did not register the broker management deployment."
    }
}
finally {
    if ($null -eq $previousNuGetPackages) {
        Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
    }
    else {
        $env:NUGET_PACKAGES = $previousNuGetPackages
    }

    Pop-Location
}
