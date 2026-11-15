$ErrorActionPreference='Stop'
$pluginsRoot=[IO.Path]::GetFullPath((Join-Path $env:APPDATA 'Elgato/StreamDeck/Plugins'))
$target=[IO.Path]::GetFullPath((Join-Path $pluginsRoot 'com.local.aiusagedeck.sdPlugin'))
if((Split-Path $target -Parent) -ne $pluginsRoot -or (Split-Path $target -Leaf) -ne 'com.local.aiusagedeck.sdPlugin'){throw 'Invalid uninstall destination.'}
if(-not(Test-Path -LiteralPath $target)){Write-Output 'Plugin is not installed.';exit 0}
$manifest=Get-Content -LiteralPath (Join-Path $target 'manifest.json') -Raw|ConvertFrom-Json
if($manifest.UUID -ne 'com.local.aiusagedeck'){throw 'Unexpected plugin UUID.'}
foreach($collector in (Get-Process -Name 'AIUsageDeck.Collector' -ErrorAction SilentlyContinue)){
 if($collector.Path -and [IO.Path]::GetFullPath($collector.Path).StartsWith($target+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){Stop-Process -Id $collector.Id;Wait-Process -Id $collector.Id -Timeout 5 -ErrorAction SilentlyContinue}
}
# Move only this app's verified installation into a recoverable backup, never a user's profile.
$backupRoot=[IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'AIUsageDeck/Uninstalled'))
New-Item -ItemType Directory -Path $backupRoot -Force|Out-Null
$backup=[IO.Path]::GetFullPath((Join-Path $backupRoot ('plugin-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))))
if(-not $backup.StartsWith($backupRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid backup destination.'}
Move-Item -LiteralPath $target -Destination $backup
Write-Output "Plugin moved to recoverable backup: $backup"
Write-Output 'Restart Stream Deck. Profiles and local quota cache have been preserved.'
