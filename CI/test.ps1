if ($env:BILILIVERECORDER_RELEASE.ToLower() -eq "true")
{
    Write-Host "test"
}
else
{
    Write-Host "Not a new release, skipping msbuild /t:Publish"
}