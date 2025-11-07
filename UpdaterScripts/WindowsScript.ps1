$ErrorActionPreference = "Stop"
Start-Transcript -Path "|PathToScriptLogFile|"

Write-Host "ARG COUNT: $($args.Count)"
$args | ForEach-Object { Write-Host $_ }

$JSON = Get-Content -Raw -Path $args[0] | ConvertFrom-Json
Write-Host "JSON: $JSON"

$SCRIPT_PATH = $MyInvocation.MyCommand.Path
$UPDATER_PATH = $JSON.UpdaterPath
$UPDATER_DIR = Split-Path -Parent $UPDATER_PATH
$UPDATER_DIR_DIR = Split-Path -Parent $UPDATER_DIR
$APP_TO_LAUNCH = $JSON.AppToLaunch

Write-Host "SCRIPT_PATH: $SCRIPT_PATH"
Write-Host "UPDATER_PATH: $UPDATER_PATH"
Write-Host "UPDATER_DIR: $UPDATER_DIR"
Write-Host "UPDATER_DIR_DIR: $UPDATER_DIR_DIR"
Write-Host "APP_TO_LAUNCH: $APP_TO_LAUNCH"

$argPath = "`"`"$($args[0])`"`""

Write-Host "Launching updater application"
Start-Process -FilePath $UPDATER_PATH -Wait -Verb RunAs -ArgumentList $argPath

$code = @"
Remove-Item -Path `"`"`"$UPDATER_DIR`"`"`" -Recurse -Force
Move-Item -Path `"`"`"$UPDATER_DIR_DIR\updater_new`"`"`" -Destination `"`"`"$UPDATER_DIR_DIR\updater`"`"`" -Force
"@

Write-Host "Launching cleanup script"
Start-Process -FilePath "powershell.exe" -Verb RunAs -ArgumentList "-Command", "$code" -WindowStyle Hidden -Wait

Write-Host "Launching application"
& $APP_TO_LAUNCH

Write-Host "Removing json file"
Remove-Item -Path $args[0] -Force

Write-Host "Removing updater script"
Remove-Item -Path $SCRIPT_PATH -Force
