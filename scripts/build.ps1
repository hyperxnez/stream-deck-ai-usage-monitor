param([string]$ReleaseDirectory)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
if(-not $ReleaseDirectory){$ReleaseDirectory=Join-Path $projectRoot 'artifacts/release'}
$ReleaseDirectory=[IO.Path]::GetFullPath($ReleaseDirectory)
function Step([string]$Command,[string[]]$StepArgs){& $Command @StepArgs;if($LASTEXITCODE -ne 0){throw "Build step failed: $Command"}}
Push-Location $projectRoot
try{
 Step 'dotnet' @('run','--project','tests/AIUsageDeck.Tests','-c','Release')
 Step 'dotnet' @('publish','src/AIUsageDeck.Collector','-c','Release','-r','win-x64','--self-contained','true','-p:PublishSingleFile=true','-p:IncludeNativeLibrariesForSelfExtract=true','-p:DebugType=none','-p:DebugSymbols=false','-p:Deterministic=true',"-p:PathMap=$projectRoot=/_",'-o','artifacts/collector')
 Push-Location 'plugin'
 try{
  Step 'npm.cmd' @('ci','--ignore-scripts','--no-fund','--no-audit')
  Step 'npm.cmd' @('run','build')
  Step 'node' @('--test','--test-reporter=dot','tests/frames.test.mjs','tests/render.test.mjs','tests/interaction.test.mjs')
 }finally{Pop-Location}
 $pluginName='com.local.aiusagedeck.sdPlugin'
 $pluginOut=Join-Path $ReleaseDirectory $pluginName
 New-Item -ItemType Directory -Path $ReleaseDirectory,$pluginOut -Force | Out-Null
 Copy-Item -Path (Join-Path $projectRoot "plugin/$pluginName/*") -Destination $pluginOut -Recurse -Force
 $collectorOut=Join-Path $pluginOut 'collector';New-Item -ItemType Directory -Path $collectorOut -Force | Out-Null
 Copy-Item -LiteralPath (Join-Path $projectRoot 'artifacts/collector/AIUsageDeck.Collector.exe') -Destination $collectorOut -Force
 & (Join-Path $PSScriptRoot 'create-profile.ps1') -OutputDirectory $ReleaseDirectory
 Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'install.ps1'),(Join-Path $PSScriptRoot 'uninstall.ps1') -Destination $ReleaseDirectory -Force
 $guideSource=if(Test-Path -LiteralPath (Join-Path $projectRoot 'README.vi.md')){Join-Path $projectRoot 'README.vi.md'}else{Join-Path $projectRoot 'README.md'}
 Copy-Item -LiteralPath $guideSource -Destination (Join-Path $ReleaseDirectory 'HUONG-DAN.md') -Force
 Push-Location 'plugin'
 try{Step 'npx.cmd' @('streamdeck','pack',$pluginOut,'--output',$ReleaseDirectory,'--force','--no-update-check','--no-file-list')}finally{Pop-Location}
 Get-ChildItem -LiteralPath $ReleaseDirectory -File | Where-Object {$_.Name -ne 'SHA256SUMS.txt'} | Get-FileHash -Algorithm SHA256 | ForEach-Object {"$($_.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($_.Path))"} | Set-Content -LiteralPath (Join-Path $ReleaseDirectory 'SHA256SUMS.txt') -Encoding utf8
 Write-Output "Release: $ReleaseDirectory"
}finally{Pop-Location}
