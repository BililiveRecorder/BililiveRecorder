if($env:APPVEYOR){
    git config --global credential.helper store
    Add-Content "$env:USERPROFILE\.git-credentials" "https://$($env:github_access_token):x-oauth-basic@github.com`n"
    git config --global user.email "appveyor@genteure.com"
    git config --global user.name "Appveyor(Genteure)"
    git config --global core.autocrlf false
}

$commit_message_version_regex="^Release: (\d+\.\d+\.\d+)$"

if ($env:APPVEYOR_REPO_BRANCH -eq "dev" -and $env:APPVEYOR_REPO_COMMIT_MESSAGE -cmatch $commit_message_version_regex)
{
    git checkout dev -q
    $env:BILILIVERECORDER_RELEASE=$true
    $env:p_version="$($Matches[1]).0"
    Update-AppveyorBuild -Version "$env:p_version"
}
else
{
    $env:BILILIVERECORDER_RELEASE=$false
    $env:p_version="0.0.0.$env:APPVEYOR_BUILD_NUMBER"
    Update-AppveyorBuild -Version "dev-$($env:APPVEYOR_REPO_COMMIT.Substring(0, 7))-$env:APPVEYOR_BUILD_NUMBER"
}

Write-Host "Current build version is $env:p_version"
