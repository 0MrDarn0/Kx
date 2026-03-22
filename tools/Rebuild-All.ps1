[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Root,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$SkipClean
)

function Resolve-RepositoryRoot {
    param(
        [string[]]$CandidatePaths
    )

    foreach ($candidatePath in $CandidatePaths) {
        if ([string]::IsNullOrWhiteSpace($candidatePath) -or -not (Test-Path -LiteralPath $candidatePath)) {
            continue
        }

        $current = (Resolve-Path -LiteralPath $candidatePath).Path
        while ($true) {
            $hasGit = Test-Path -LiteralPath (Join-Path $current '.git')
            $hasRepoFolders =
                (Test-Path -LiteralPath (Join-Path $current 'src')) -or
                (Test-Path -LiteralPath (Join-Path $current 'apps')) -or
                (Test-Path -LiteralPath (Join-Path $current 'tests')) -or
                (Test-Path -LiteralPath (Join-Path $current 'examples'))

            if ($hasGit -or $hasRepoFolders) {
                return $current
            }

            $parent = Split-Path -Parent $current
            if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $current) {
                break
            }

            $current = $parent
        }
    }

    throw "Could not determine the repository root. Pass -Root explicitly."
}

$rootPath = if ([string]::IsNullOrWhiteSpace($Root)) {
    Resolve-RepositoryRoot @($PSScriptRoot, (Get-Location).Path)
}
else {
    (Resolve-Path -LiteralPath $Root).Path
}

$cleanScript = Join-Path $PSScriptRoot 'Clean-Artifacts.ps1'
if (-not $SkipClean) {
    try {
        & $cleanScript -Root $rootPath -Confirm:$false -WhatIf:$WhatIfPreference
    }
    catch {
        throw "Cleanup failed before rebuild. $($_.Exception.Message)"
    }
}

$projectFiles = Get-ChildItem -LiteralPath $rootPath -Recurse -Filter *.csproj -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notlike '*\bin\*' -and $_.FullName -notlike '*\obj\*' }

if (-not $projectFiles) {
    throw "No project files found under '$rootPath'."
}

$buildFailures = @()

foreach ($projectFile in $projectFiles) {
    $projectPath = $projectFile.FullName
    if (-not $PSCmdlet.ShouldProcess($projectPath, "Build project ($Configuration)")) {
        continue
    }

    Write-Host "Building $projectPath"
    & dotnet build $projectPath -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        $buildFailures += $projectPath
    }
}

if ($buildFailures.Count -gt 0) {
    throw "Build failed for $($buildFailures.Count) project(s):`n$($buildFailures -join "`n")"
}

Write-Host "Done. Built $($projectFiles.Count) project(s) in $Configuration mode under '$rootPath'."
