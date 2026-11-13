param([switch]$Install)
$ErrorActionPreference = 'Stop'
$modRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$gameRoot = [IO.Path]::GetFullPath((Join-Path $modRoot '..\..'))
$metadataPath = Join-Path $modRoot 'mod.json'
$meta = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
if ($meta.id -ne 'GlobalLedgerV2') { throw 'Expected Global Ledger V2 metadata.' }
& (Join-Path $gameRoot '_Mods\_AI\_Workflows\Tools\SourceLayout\Check-SourceLayout.ps1') -ModFolder $modRoot
& dotnet build (Join-Path $modRoot 'GlobalLedgerV2.csproj') -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Global Ledger V2 build failed.' }
$dll = Join-Path $modRoot 'bin\Release\netstandard2.1\GlobalLedgerV2.dll'
if ([Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString(3) -ne $meta.version) { throw 'Global Ledger V2 version mismatch.' }
$release = Join-Path $modRoot 'Release'
New-Item -ItemType Directory -Path $release -Force | Out-Null
Copy-Item -LiteralPath $dll,$metadataPath -Destination $release -Force
$work = Join-Path $modRoot ('_AI\Scratch\LocalPackage-' + [Guid]::NewGuid().ToString('N'))
$plugin = Join-Path $work 'Stage\BepInEx\plugins\GlobalLedgerV2'
New-Item -ItemType Directory -Path $plugin -Force | Out-Null
Copy-Item -LiteralPath $dll,$metadataPath -Destination $plugin
Add-Type -AssemblyName System.IO.Compression,System.IO.Compression.FileSystem
$candidate = Join-Path $work 'GlobalLedgerV2.zip'
$stage = Join-Path $work 'Stage'
$writer = [IO.Compression.ZipFile]::Open($candidate, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath $stage -Recurse -File) {
        $entryName = $file.FullName.Substring($stage.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($writer, $file.FullName, $entryName, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $writer.Dispose() }
$archive = Join-Path $release ('GlobalLedgerV2-v' + $meta.version + '-KingdomInc-1.0.14-Local.zip')
if (Test-Path -LiteralPath $archive) {
    $backup = Join-Path $modRoot ('_AI\Notes\Backups\Package-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    Copy-Item -LiteralPath $archive -Destination $backup
}
Copy-Item -LiteralPath $candidate -Destination $archive -Force
$hash = (Get-FileHash -LiteralPath $archive).Hash.ToLowerInvariant()
[IO.File]::WriteAllText(($archive + '.sha256'), $hash + '  ' + [IO.Path]::GetFileName($archive) + "`n")
if ($Install) {
    $target = Join-Path $gameRoot 'BepInEx\plugins\GlobalLedgerV2'
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    Copy-Item -LiteralPath $dll,$metadataPath -Destination $target -Force
    if ((Get-FileHash -LiteralPath (Join-Path $target 'GlobalLedgerV2.dll')).Hash -ne (Get-FileHash -LiteralPath $dll).Hash) { throw 'Installed Global Ledger V2 DLL differs from the build.' }
}
[pscustomobject]@{ Mod='Global Ledger V2'; Version=$meta.version; Archive=$archive; SHA256=$hash; Installed=[bool]$Install; Publication='Local only' } | ConvertTo-Json
