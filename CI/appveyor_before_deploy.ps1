if ($env:BILILIVERECORDER_RELEASE)
{
    # ./CI/do_codesign.ps1
    ./CI/push_clickonce.ps1
    ./CI/push_master.ps1
}
else
{
    Write-Host "Not a new release!"
}
