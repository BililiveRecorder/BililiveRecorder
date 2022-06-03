Set-PSDebug -Trace 1
Push-Location "$PSScriptRoot/source"
$env:BASE_URL = "/ui/"
npm ci
npx vite build
Copy-Item -Recurse dist ../../BililiveRecorder.Web/embeded/ui
Pop-Location
