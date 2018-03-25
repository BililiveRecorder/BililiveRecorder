if(-not (Test-Path env:APPVEYOR)) Throw New-Object System.NotSupportedException "Not Running on Appveyor!"
$env:old_script_path=(Get-Item -Path ".\" -Verbose).FullName
git clone --depth 1 https://github.com/Bililive/soft.danmuji.org.git C:\projects\site
Get-ChildItem -Path .\BililiveRecorder.WPF\bin\Release\app.publish | Copy-Item -Destination C:\projects\site\BililiveRecorder -Recurse -Container
git --git-dir=C:\projects\site\.git\ --work-tree=C:\projects\site\ add -A
git --git-dir=C:\projects\site\.git\ --work-tree=C:\projects\site\ commit -m 'BililiveRecorder $env:p_version'
git --git-dir=C:\projects\site\.git\ --work-tree=C:\projects\site\ push
