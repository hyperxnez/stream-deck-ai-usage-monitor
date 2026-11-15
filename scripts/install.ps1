param([string]$PluginDirectory=(Join-Path $PSScriptRoot 'com.local.aiusagedeck.sdPlugin'))
$ErrorActionPreference='Stop'
$pluginName='com.local.aiusagedeck.sdPlugin'
$pluginDirectory=[IO.Path]::GetFullPath($PluginDirectory)
$manifest=Join-Path $pluginDirectory 'manifest.json'
if(-not(Test-Path -LiteralPath $manifest)){throw 'Plugin package not found.'}
if((Get-Content -LiteralPath $manifest -Raw|ConvertFrom-Json).UUID -ne 'com.local.aiusagedeck'){throw 'Unexpected plugin UUID.'}
$pluginsRoot=[IO.Path]::GetFullPath((Join-Path $env:APPDATA 'Elgato/StreamDeck/Plugins'))
$target=[IO.Path]::GetFullPath((Join-Path $pluginsRoot $pluginName))
if((Split-Path $target -Parent) -ne $pluginsRoot -or (Split-Path $target -Leaf) -ne $pluginName){throw 'Invalid install destination.'}
if(Test-Path -LiteralPath $target){
 $existingManifest=Join-Path $target 'manifest.json'
 if(-not(Test-Path -LiteralPath $existingManifest)-or (Get-Content -LiteralPath $existingManifest -Raw|ConvertFrom-Json).UUID -ne 'com.local.aiusagedeck'){throw 'Existing directory does not belong to com.local.aiusagedeck.'}
 foreach($collector in (Get-Process -Name 'AIUsageDeck.Collector' -ErrorAction SilentlyContinue)){
  if($collector.Path -and [IO.Path]::GetFullPath($collector.Path).StartsWith($target+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){Stop-Process -Id $collector.Id;Wait-Process -Id $collector.Id -Timeout 5 -ErrorAction SilentlyContinue}
 }
 $backupRoot=[IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'AIUsageDeck/Backups'))
 New-Item -ItemType Directory -Path $backupRoot -Force|Out-Null
 $backup=[IO.Path]::GetFullPath((Join-Path $backupRoot ('plugin-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))))
 if(-not $backup.StartsWith($backupRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid backup destination.'}
 try{Move-Item -LiteralPath $target -Destination $backup}catch{throw 'Close Stream Deck before updating, or install via com.local.aiusagedeck.streamDeckPlugin.'}
 Write-Output "Prior install backed up: $backup"
}
New-Item -ItemType Directory -Path $pluginsRoot -Force|Out-Null
Copy-Item -LiteralPath $pluginDirectory -Destination $target -Recurse
Write-Output "Installed: $target"
Write-Output 'Restart Stream Deck, then import AI Usage Deck.streamDeckProfile. Existing profiles are preserved.'
