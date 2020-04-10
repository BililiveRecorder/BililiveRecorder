# called by msbuild

$isAppveyor = if ($env:APPVEYOR -eq $null) { "false" } else { $env:APPVEYOR }
$buildversion = if ($env:APPVEYOR_BUILD_VERSION -eq $null) { "本地编译" } else { $env:APPVEYOR_BUILD_VERSION }
$githash = git rev-parse --verify HEAD

(Get-Content .\BuildInfo.txt).Replace('[PROJECT_NAME]', $args).Replace('[APPVEYOR]', $isAppveyor.ToLower()).Replace('[VERSION]', $buildversion).Replace('[GIT_HASH]', $githash).Replace('[GIT_HASH_S]', $githash.Substring(0, 8)) | Set-Content ".\TempBuildInfo\BuildInfo.$args.cs"

Write-Output "BuildInfo for $args patched"
