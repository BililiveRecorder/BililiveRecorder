if ($env:APPVEYOR -eq "True" -and $env:CONFIGURATION -eq "Release") {
    git config --global credential.helper store
    Add-Content "$env:USERPROFILE\.git-credentials" "https://$($env:github_access_token):x-oauth-basic@github.com`n"
    git config --global user.email "appveyor@genteure.com"
    git config --global user.name "Appveyor(Genteure)"
    git config --global core.autocrlf false
    
    Update-AppveyorBuild -Version "$(Get-Content VERSION -Raw)"

    $env:DEPLOY_SITE_GIT="C:\projects\site"
    git clone --quiet --depth 1 "https://github.com/Bililive/soft.danmuji.org.git" $env:DEPLOY_SITE_GIT
    $env:DEPLOY_SITE_BRANCH="rec$env:APPVEYOR_BUILD_VERSION"
    git checkout -b $env:DEPLOY_SITE_BRANCH
}

Write-Host "Current build version is $env:APPVEYOR_BUILD_VERSION"
