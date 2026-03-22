[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Root
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

$targetNames = @('bin', 'obj')

$directories = Get-ChildItem -LiteralPath $rootPath -Directory -Recurse -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in $targetNames -and $_.FullName -notlike '*\.git\*' } |
    Sort-Object FullName -Descending

if (-not $directories) {
    Write-Host "No bin/obj directories found under '$rootPath'."
    return
}

$removedCount = 0

foreach ($directory in $directories) {
    if ($PSCmdlet.ShouldProcess($directory.FullName, 'Remove directory')) {
        Remove-Item -LiteralPath $directory.FullName -Recurse -Force -ErrorAction Stop
        $removedCount++
        Write-Host "Removed $($directory.FullName)"
    }
}

Write-Host "Done. Removed $removedCount build artifact director$(if ($removedCount -eq 1) { 'y' } else { 'ies' })."
