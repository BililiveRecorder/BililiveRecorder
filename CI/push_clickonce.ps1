if (-not (Test-Path env:APPVEYOR)) {
    Throw New-Object System.NotSupportedException "Not Running on Appveyor!"
}
git clone --depth 1 https://github.com/Bililive/soft.danmuji.org.git C:\projects\site
Get-ChildItem -Path .\BililiveRecorder.WPF\bin\Release\app.publish | Copy-Item -Destination C:\projects\site\BililiveRecorder -Recurse -Container
git --quiet --git-dir=C:\projects\site\.git\ --work-tree=C:\projects\site\ add -A
git --quiet --git-dir=C:\projects\site\.git\ --work-tree=C:\projects\site\ commit -m "BililiveRecorder $env:p_version"
git --quiet --git-dir=C:\projects\site\.git\ --work-tree=C:\projects\site\ push
