git --git-dir="$env:DEPLOY_SITE_GIT\.git\" --work-tree="$env:DEPLOY_SITE_GIT" add -A
git --git-dir="$env:DEPLOY_SITE_GIT\.git\" --work-tree="$env:DEPLOY_SITE_GIT" commit --quiet -m "BililiveRecorder $env:APPVEYOR_BUILD_VERSION"
git --git-dir="$env:DEPLOY_SITE_GIT\.git\" --work-tree="$env:DEPLOY_SITE_GIT" push --quiet+

$headers = @{
    'Accept'        = 'application/vnd.github.v3+json'
    'User-Agent'    = 'genteure@github appveyor@genteure.com'
    'Authorization' = "token $env:github_access_token"
}
$body = @{
    'title'                 = "[CI] BililiveRecorder $env:APPVEYOR_BUILD_VERSION"
    'head'                  = "$env:DEPLOY_SITE_BRANCH"
    'body'                  = "Update file for BililiveRecorder $env:APPVEYOR_BUILD_VERSION"
    'base'                  = 'master'
    'maintainer_can_modify' = $true
}

Invoke-RestMethod -Method Post -Headers $headers -Body $body -Uri "https://api.github.com/repos/Bililive/rec.danmuji.org/pulls" -ErrorAction:SilentlyContinue | Out-Null
