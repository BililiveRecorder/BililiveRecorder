if ($env:APPVEYOR)
{
    Rename-Item .\BililiveRecorder.WPF\bin\Release\app.publish\setup.exe BililiveRecorder.exe

    git clone --quiet --depth 1 https://github.com/Bililive/soft.danmuji.org.git C:\projects\site
    Get-ChildItem -Path .\BililiveRecorder.WPF\bin\Release\app.publish | Copy-Item -Destination C:\projects\site\BililiveRecorder -Recurse -Container -Force
    git --git-dir=C:\projects\site\.git\ --work-tree=C:\projects\site\ add -A
    git --git-dir=C:\projects\site\.git\ --work-tree=C:\projects\site\ commit --quiet -m "BililiveRecorder $env:p_version"
    git --git-dir=C:\projects\site\.git\ --work-tree=C:\projects\site\ push --quiet
}
