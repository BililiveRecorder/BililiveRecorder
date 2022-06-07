Set-PSDebug -Trace 1
Push-Location "$PSScriptRoot/source"
$env:BASE_URL = "/ui/"
$env:EMBEDED_BUILD = "true"
(npm ci) -and (npx vite build)
Remove-Item -Recurse -ErrorAction SilentlyContinue ../../BililiveRecorder.Web/embeded/ui
Copy-Item -Recurse dist ../../BililiveRecorder.Web/embeded/ui
Pop-Location
