param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$profileRoot=Join-Path $projectRoot 'artifacts/profile-build'
if(Test-Path -LiteralPath $profileRoot){Remove-Item -LiteralPath $profileRoot -Recurse -Force}
$profileId='6B724C69-DF78-48CE-923C-C635966C7215'
$pageId='AB2861D8-F102-4D7F-B3BC-76D8EB9E038D'
$bundle=Join-Path $profileRoot "$profileId.sdProfile"
$page=Join-Path $bundle "Profiles/$pageId"
New-Item -ItemType Directory -Path $page,$OutputDirectory -Force | Out-Null
$actions=[ordered]@{}
$slots=@(@('0,0','claude5','claude','fiveHour'),@('1,0','codex5','codex','fiveHour'),@('2,0','anti5','antigravity','fiveHour'),@('0,1','claude7','claude','sevenDay'),@('1,1','codex7','codex','sevenDay'),@('2,1','anti7','antigravity','sevenDay'))
foreach($slot in $slots){
 $actions[$slot[0]]=[ordered]@{ActionID=[guid]::NewGuid().ToString();LinkedTitle=$false;Name="AI Usage $($slot[2]) $($slot[3])";Plugin=@{Name='AI Usage Deck';UUID='com.local.aiusagedeck';Version='0.1.0.0'};Resources=$null;Settings=@{provider=$slot[2];window=$slot[3];group=$(if($slot[2] -eq 'antigravity'){'gemini-models'}else{''})};State=0;States=@(@{Title='';ShowTitle=$false});UUID="com.local.aiusagedeck.$($slot[1])"}
}
@{Device=@{Model='20GAI9901';UUID=''};Name='AI Usage Deck';Pages=@{Current=$pageId;Default=$pageId;Pages=@($pageId)};Version='3.0'} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $bundle 'manifest.json') -Encoding utf8
@{Controllers=@(@{Actions=$actions;Type='Keypad'});Icon='';Name='AI Usage Deck'} | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $page 'manifest.json') -Encoding utf8
Add-Type -AssemblyName System.IO.Compression.FileSystem
$outputFile=Join-Path $OutputDirectory 'AI Usage Deck.streamDeckProfile'
if(Test-Path -LiteralPath $outputFile){Remove-Item -LiteralPath $outputFile}
[IO.Compression.ZipFile]::CreateFromDirectory($profileRoot,$outputFile)
Write-Output $outputFile
