if ($env:BILILIVERECORDER_RELEASE.ToLower() -eq "true")
{
    Import-Module ./CI/FileCryptography.psm1
    $secure_codesignpasswd = ConvertTo-SecureString "$env:codesignpasswd" -AsPlainText -Force
    Unprotect-File './CI/rixCloud2Genteure.pfx.AES' -KeyAsPlainText "$env:codesignaes"
    Import-PfxCertificate -FilePath './CI/rixCloud2Genteure.pfx' -CertStoreLocation Cert:\LocalMachine\My -Password $secure_codesignpasswd

    msbuild /t:Publish /verbosity:minimal /p:Configuration=Release /p:ApplicationVersion="$env:p_version" /logger:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"
    $host.SetShouldExit($LastExitCode)
}
else
{
    Write-Host "Not a new release, skipping msbuild /t:Publish"
}
