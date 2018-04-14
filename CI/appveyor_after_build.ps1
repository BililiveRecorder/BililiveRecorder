if ($env:BILILIVERECORDER_RELEASE.ToLower() -eq "true")
{
    Import-Module ./CI/FileCryptography.psm1
    $secure_codesignpasswd = ConvertTo-SecureString "$env:codesignpasswd" -AsPlainText -Force
    $file = Unprotect-File './CI/rixCloud2Genteure.pfx.AES' -KeyAsPlainText "$env:codesignaes"
    $ccert = Import-PfxCertificate -FilePath "$($file.FullName)" -CertStoreLocation Cert:\CurrentUser\My -Password $secure_codesignpasswd

    msbuild /t:Publish /verbosity:minimal /p:Configuration=Release /p:CertificateThumbprint="$($ccert.Thumbprint)" /p:ApplicationVersion="$env:p_version" /logger:"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"
    $host.SetShouldExit($LastExitCode)
}
else
{
    Write-Host "Not a new release, skipping msbuild /t:Publish"
}
