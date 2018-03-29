$isAppveyor = if ($env:APPVEYOR -eq $null) { "false" } else { $env:APPVEYOR }
$buildversion = if ($env:p_version -eq $null) { "本地编译" } else { $env:p_version }
$githash = git rev-parse --verify HEAD

(Get-Content .\BililiveRecorder.WPF\BuildInfo.txt).Replace('[APPVEYOR]', $isAppveyor.ToLower()).Replace('[VERSION]', $buildversion).Replace('[GIT_HASH]', $githash).Replace('[GIT_HASH_S]', $githash.Substring(0, 8)) | Set-Content .\BililiveRecorder.WPF\BuildInfo.cs
