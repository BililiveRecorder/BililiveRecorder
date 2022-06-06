Set-PSDebug -Trace 1
Push-Location "$PSScriptRoot/source"
$env:BASE_URL = "/ui/"
$env:EMBEDED_BUILD = "true"
npm ci
npx vite build
Copy-Item -Recurse dist ../../BililiveRecorder.Web/embeded/ui
Pop-Location
