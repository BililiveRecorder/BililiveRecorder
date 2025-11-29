Set-PSDebug -Trace 1
Push-Location "$PSScriptRoot/source"
$env:BASE_URL = "./"
$env:VITE_EMBEDDED_BUILD = "true"
(npm ci) -and (npx vite build)
Remove-Item -Recurse -ErrorAction SilentlyContinue ../../src/BililiveRecorder.Web/embeded/ui
Copy-Item -Recurse dist ../../src/BililiveRecorder.Web/embeded/ui
Pop-Location
