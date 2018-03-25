if ($env:APPVEYOR) {
    msbuild "BililiveRecorder.sln" /t:Publish /verbosity:minimal /p:Configuration=Release /p:ApplicationVersion="$env:p_version" /logger:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"
}else {
    msbuild "BililiveRecorder.sln" /t:Publish /verbosity:minimal /p:Configuration=Release /p:ApplicationVersion="$env:p_version"
}
if ($LastExitCode -ne 0) { $host.SetShouldExit($LastExitCode)  }