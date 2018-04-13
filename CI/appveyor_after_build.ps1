if ($env:BILILIVERECORDER_RELEASE.ToLower() -eq "true")
{
    msbuild /t:Publish /verbosity:minimal /p:Configuration=Release /p:ApplicationVersion="$env:p_version" /logger:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"
    $host.SetShouldExit($LastExitCode)
}
else
{
    Write-Host "Not a new release, skipping msbuild /t:Publish"
}
