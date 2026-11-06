param([switch]$WithExistingPlugins)
$ErrorActionPreference = 'Stop'
$gameRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$verification = Join-Path $PSScriptRoot 'Verification'
$runtime = Join-Path $verification ('Runtime-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runtime -Force | Out-Null
foreach ($name in @('KingdomInc.exe','UnityPlayer.dll','UnityCrashHandler64.exe','winhttp.dll','doorstop_config.ini','.doorstop_version')) {
    New-Item -ItemType HardLink -Path (Join-Path $runtime $name) -Value (Join-Path $gameRoot $name) | Out-Null
}
foreach ($name in @('KingdomInc_Data','MonoBleedingEdge','D3D12')) {
    New-Item -ItemType Junction -Path (Join-Path $runtime $name) -Value (Join-Path $gameRoot $name) | Out-Null
}
$bep = Join-Path $runtime 'BepInEx'
New-Item -ItemType Directory -Path (Join-Path $bep 'plugins\GlobalLedger'),(Join-Path $bep 'config') -Force | Out-Null
New-Item -ItemType Junction -Path (Join-Path $bep 'core') -Value (Join-Path $gameRoot 'BepInEx\core') | Out-Null
Copy-Item -LiteralPath (Join-Path $gameRoot 'BepInEx\config\BepInEx.cfg') -Destination (Join-Path $bep 'config\BepInEx.cfg')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\GlobalLedger.dll') -Destination (Join-Path $bep 'plugins\GlobalLedger\GlobalLedger.dll')
$builtFoundation = Join-Path $PSScriptRoot '..\Foundation\bin\Release\netstandard2.1\Foundation.dll'
$foundation = if (Test-Path -LiteralPath $builtFoundation) { $builtFoundation } else { Join-Path $gameRoot 'BepInEx\plugins\Foundation\Foundation.dll' }
if (-not (Test-Path -LiteralPath $foundation)) { throw 'Install Foundation 0.6.0 or later before verifying Global Ledger.' }
if ([Reflection.AssemblyName]::GetAssemblyName($foundation).Version -lt [Version]'0.6.0') { throw 'Global Ledger requires Foundation 0.6.0 or later.' }
New-Item -ItemType Directory -Path (Join-Path $bep 'plugins\Foundation') -Force | Out-Null
Copy-Item -LiteralPath $foundation -Destination (Join-Path $bep 'plugins\Foundation\Foundation.dll')
if ($WithExistingPlugins) {
    foreach ($plugin in Get-ChildItem -LiteralPath (Join-Path $gameRoot 'BepInEx\plugins') -Directory) {
        if ($plugin.Name -in @('GlobalLedger','Foundation')) { continue }
        Copy-Item -LiteralPath $plugin.FullName -Destination (Join-Path $bep 'plugins') -Recurse
    }
    # The normal game may already own port 17840. This private runtime uses 17849.
    [IO.File]::WriteAllText((Join-Path $bep 'config\urakka.kingdominc.factoryapi.cfg'), "[Server]`nPort = 17849`n")
}
$logPath = Join-Path $runtime 'diagnostic.log'
$resultFolder = Join-Path $runtime '_Mods\GlobalLedger\Verification'
$resultPath = Join-Path $resultFolder 'runtime-tests.json'
$exe = Join-Path $runtime 'KingdomInc.exe'
$owned = Start-Process -FilePath $exe -WorkingDirectory $runtime -WindowStyle Hidden -ArgumentList @('-global-ledger-smoke','-screen-width','1600','-screen-height','1000','-screen-fullscreen','0','-logFile',('"' + $logPath + '"')) -PassThru
try {
    $deadline = (Get-Date).AddSeconds(55)
    while ((Get-Date) -lt $deadline -and -not (Test-Path -LiteralPath $resultPath)) { Start-Sleep -Milliseconds 500 }
    if (-not (Test-Path -LiteralPath $resultPath)) {
        if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Tail 75 }
        throw "Isolated diagnostic did not complete. See $logPath"
    }
    $owned.WaitForExit(5000) | Out-Null
    $result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
    foreach ($file in Get-ChildItem -LiteralPath $resultFolder -File) {
        if ($WithExistingPlugins -and $file.Name -eq 'runtime-tests.json') { continue }
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $verification $file.Name) -Force
    }
    if ($WithExistingPlugins) { Copy-Item -LiteralPath $resultPath -Destination (Join-Path $verification 'compatibility-tests.json') -Force }
    $savedLog = if ($WithExistingPlugins) { 'unity-companion.log' } else { 'unity-isolated.log' }
    Copy-Item -LiteralPath $logPath -Destination (Join-Path $verification $savedLog) -Force
    $result | ConvertTo-Json -Depth 8
    if (-not $result.passed) { throw "Isolated diagnostic failed. See $logPath" }
    Start-Sleep -Seconds 2
} finally {
    $owned.Refresh()
    if (-not $owned.HasExited) { Stop-Process -Id $owned.Id }
    foreach ($replacement in Get-CimInstance Win32_Process -Filter "Name='KingdomInc.exe'") {
        if ($replacement.ExecutablePath -eq $exe -and $replacement.CommandLine.Contains($logPath)) { Stop-Process -Id $replacement.ProcessId }
    }
}
