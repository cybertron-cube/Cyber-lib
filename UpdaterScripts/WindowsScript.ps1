$ErrorActionPreference = "Stop"
Start-Transcript -Path "|PathToScriptLogFile|"

$SCRIPT_PATH = $MyInvocation.MyCommand.Path
$UPDATER_PATH = $args[0]
$UPDATER_DIR = Split-Path -Parent "$UPDATER_PATH"
$UPDATER_DIR_DIR = Split-Path -Parent "$UPDATER_DIR"
$APP_TO_LAUNCH = $args[4]

Write-Host $SCRIPT_PATH
Write-Host $UPDATER_PATH
Write-Host $UPDATER_DIR
Write-Host $UPDATER_DIR_DIR
Write-Host $APP_TO_LAUNCH

function QuoteArgument {
    param (
        [string]$arg
    )
    
    return "`"$arg`""
}

$quotedArgs = $args | ForEach-Object { QuoteArgument $_ }

Write-Host $quotedArgs

Start-Process -FilePath "$UPDATER_PATH" -Verb RunAs -ArgumentList $quotedArgs -Wait

$code = @"
Remove-Item -Path `"`"`"$UPDATER_DIR`"`"`" -Recurse -Force
Move-Item -Path `"`"`"$UPDATER_DIR_DIR\updater_new`"`"`" -Destination `"`"`"$UPDATER_DIR_DIR\updater`"`"`" -Force
"@

Start-Process -FilePath "powershell.exe" -Verb RunAs -ArgumentList "-Command", "$code" -WindowStyle Hidden -Wait

& "$APP_TO_LAUNCH"

Remove-Item -Path "$SCRIPT_PATH" -Force
