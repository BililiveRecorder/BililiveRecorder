[CmdletBinding()]
param (
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$BuildArguments
)

Write-Output "PowerShell $($PSVersionTable.PSVersion.ToString())"
Set-StrictMode -Version 2.0; $ErrorActionPreference = "Stop"; $ConfirmPreference = "None"; trap { Write-Error $_ -ErrorAction Continue; exit 1 }
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent

###########################################################################
# CONFIGURATION
###########################################################################

$BuildProjectFile = "$PSScriptRoot\build\_build.csproj"
$TempDirectory = "$PSScriptRoot\.nuke\temp"

$DotNetGlobalFile = "$PSScriptRoot\global.json"
$DotNetInstallUrl = "https://dot.net/v1/dotnet-install.ps1"
$DotNetChannel = "STS"

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1
$env:DOTNET_MULTILEVEL_LOOKUP = 0

###########################################################################
# EXECUTION
###########################################################################

function ExecSafe([scriptblock] $cmd) {
    & $cmd
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

# If dotnet CLI is installed globally and it's good enough, use it directly
if (Get-Command "dotnet" -ErrorAction SilentlyContinue) {
    $env:DOTNET_EXE = (Get-Command "dotnet").Path
}
else {
    $DotNetDirectory = "$TempDirectory\dotnet"
    $env:DOTNET_EXE = "$DotNetDirectory\dotnet.exe"

    if (-not (Test-Path $env:DOTNET_EXE -PathType Leaf)) {
        New-Item -ItemType Directory -Force -Path $DotNetDirectory | Out-Null

        try {
            $json = Get-Content $DotNetGlobalFile -ErrorAction SilentlyContinue | ConvertFrom-Json
            $DotNetVersion = $json.sdk.version
        } catch { }

        (New-Object System.Net.WebClient).DownloadFile($DotNetInstallUrl, "$TempDirectory\dotnet-install.ps1")

        if ($DotNetVersion) {
            ExecSafe { & "$TempDirectory\dotnet-install.ps1" -InstallDir $DotNetDirectory -Version $DotNetVersion -NoPath }
        } else {
            ExecSafe { & "$TempDirectory\dotnet-install.ps1" -InstallDir $DotNetDirectory -Channel $DotNetChannel -NoPath }
        }
    }
}

Write-Output "Microsoft (R) .NET Core SDK version $(& $env:DOTNET_EXE --version)"

ExecSafe { & $env:DOTNET_EXE build $BuildProjectFile /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet }
ExecSafe { & $env:DOTNET_EXE run --project $BuildProjectFile --no-build -- $BuildArguments }
