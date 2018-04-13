# https://gallery.technet.microsoft.com/scriptcenter/EncryptDecrypt-files-use-65e7ae5d
# See FileCryptography.psm1.LICENSE.txt

function New-CryptographyKey()
{
<#
.SYNOPSIS 
Generates a random cryptography key.

.DESCRIPTION
Generates a random cryptography key based on the desired key size.

.PARAMETER Algorithm
Algorithm to generate key for.

.PARAMETER KeySize
Number of bits the generated key will have.

.PARAMETER AsPlainText
Returns a String instead of SecureString.

.OUTPUTS
System.Security.SecureString. New-CryptographyKey return the key as a SecureString by default.
System.String. New-CryptographyKey will return the key in plain text as a string if the -AsPlainText parameter is specified.

.EXAMPLE
$key = New-CryptographyKey
This example generates a random 256-bit AES key and stores it in the variable $key.

.NOTES
Author: Tyler Siegrist
Date: 9/22/2017
#>
[CmdletBinding()]
[OutputType([System.Security.SecureString])]
[OutputType([String], ParameterSetName='PlainText')]
Param(
    [Parameter(Mandatory=$false, Position=1)]
    [ValidateSet('AES','DES','RC2','Rijndael','TripleDES')]
    [String]$Algorithm='AES',
    [Parameter(Mandatory=$false, Position=2)]
    [Int]$KeySize,
    [Parameter(ParameterSetName='PlainText')]
    [Switch]$AsPlainText
)
    Process
    {
        try
        {
            $Crypto = [System.Security.Cryptography.SymmetricAlgorithm]::Create($Algorithm)
            if($PSBoundParameters.ContainsKey('KeySize')){
                $Crypto.KeySize = $KeySize
            }
            $Crypto.GenerateKey()
            if($AsPlainText)
            {
                return [System.Convert]::ToBase64String($Crypto.Key)
            }
            else
            {
                return [System.Convert]::ToBase64String($Crypto.Key) | ConvertTo-SecureString -AsPlainText -Force
            }
        }
        catch
        {
            Write-Error $_
        }
        
    }
}

Function Protect-File
{
<#
.SYNOPSIS 
Encrypts a file using a symmetrical algorithm.

.DESCRIPTION
Encrypts a file using a symmetrical algorithm.

.PARAMETER FileName
File(s) to be encrypted.

.PARAMETER Key
Cryptography key as a SecureString to be used for encryption.

.PARAMETER KeyAsPlainText
Cryptography key as a String to be used for encryption.

.PARAMETER CipherMode
Specifies the block cipher mode to use for encryption.

.PARAMETER PaddingMode
Specifies the type of padding to apply when the message data block is shorter than the full number of bytes needed for a cryptographic operation.

.PARAMETER Suffix
Suffix of the encrypted file to be removed.

.PARAMETER RemoveSource
Removes the source (decrypted) file after encrypting.

.OUTPUTS
System.IO.FileInfo. Protect-File will return FileInfo with the SourceFile, Algorithm, Key, CipherMode, and PaddingMode as added NoteProperties

.EXAMPLE
Protect-File 'C:\secrets.txt' $key
This example encrypts C:\secrets.txt using the key stored in the variable $key. The encrypted file would have the default extension of '.AES' and the source (decrypted) file would not be removed.

.EXAMPLE
Protect-File 'C:\secrets.txt' -Algorithm DES -Suffix '.Encrypted' -RemoveSource
This example encrypts C:\secrets.txt with a randomly generated DES key. The encrypted file would have an extension of '.Encrypted' and the source (decrypted) file would be removed.

.EXAMPLE
Get-ChildItem 'C:\Files' -Recurse | Protect-File -Algorithm AES -Key $key -RemoveSource
This example encrypts all of the files under the C:\Files directory using the key stored in the variable $key. The encrypted files would have the default extension of '.AES' and the source (decrypted) files would be removed.

.NOTES
Author: Tyler Siegrist
Date: 9/22/2017
#>
[CmdletBinding(DefaultParameterSetName='SecureString')]
[OutputType([System.IO.FileInfo[]])]
Param(
    [Parameter(Mandatory=$true, Position=1, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true)]
    [Alias('PSPath','LiteralPath')]
    [string[]]$FileName,
    [Parameter(Mandatory=$false, Position=2)]
    [ValidateSet('AES','DES','RC2','Rijndael','TripleDES')]
    [String]$Algorithm = 'AES',
    [Parameter(Mandatory=$false, Position=3, ParameterSetName='SecureString')]
    [System.Security.SecureString]$Key = (New-CryptographyKey -Algorithm $Algorithm),
    [Parameter(Mandatory=$true, Position=3, ParameterSetName='PlainText')]
    [String]$KeyAsPlainText,
    [Parameter(Mandatory=$false, Position=4)]
    [System.Security.Cryptography.CipherMode]$CipherMode,
    [Parameter(Mandatory=$false, Position=5)]
    [System.Security.Cryptography.PaddingMode]$PaddingMode,
    [Parameter(Mandatory=$false, Position=6)]
    [String]$Suffix = ".$Algorithm",
    [Parameter()]
    [Switch]$RemoveSource
)
    Begin
    {
        #Configure cryptography
        try
        {
            if($PSCmdlet.ParameterSetName -eq 'PlainText')
            {
                $Key = $KeyAsPlainText | ConvertTo-SecureString -AsPlainText -Force
            }

            #Decrypt cryptography Key from SecureString
            $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Key)
            $EncryptionKey = [System.Convert]::FromBase64String([System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR))

            $Crypto = [System.Security.Cryptography.SymmetricAlgorithm]::Create($Algorithm)
            if($PSBoundParameters.ContainsKey('CipherMode')){
                $Crypto.Mode = $CipherMode
            }
            if($PSBoundParameters.ContainsKey('PaddingMode')){
                $Crypto.Padding = $PaddingMode
            }
            $Crypto.KeySize = $EncryptionKey.Length*8
            $Crypto.Key = $EncryptionKey
        }
        Catch
        {
            Write-Error $_ -ErrorAction Stop
        }
    }
    Process
    {
        $Files = Get-Item -LiteralPath $FileName
    
        ForEach($File in $Files)
        {
            $DestinationFile = $File.FullName + $Suffix

            Try
            {
                $FileStreamReader = New-Object System.IO.FileStream($File.FullName, [System.IO.FileMode]::Open)
                $FileStreamWriter = New-Object System.IO.FileStream($DestinationFile, [System.IO.FileMode]::Create)

                #Write IV (initialization-vector) length & IV to encrypted file
                $Crypto.GenerateIV()
                $FileStreamWriter.Write([System.BitConverter]::GetBytes($Crypto.IV.Length), 0, 4)
                $FileStreamWriter.Write($Crypto.IV, 0, $Crypto.IV.Length)

                #Perform encryption
                $Transform = $Crypto.CreateEncryptor()
                $CryptoStream = New-Object System.Security.Cryptography.CryptoStream($FileStreamWriter, $Transform, [System.Security.Cryptography.CryptoStreamMode]::Write)
                $FileStreamReader.CopyTo($CryptoStream)
    
                #Close open files
                $CryptoStream.FlushFinalBlock()
                $CryptoStream.Close()
                $FileStreamReader.Close()
                $FileStreamWriter.Close()

                #Delete unencrypted file
                if($RemoveSource){Remove-Item -LiteralPath $File.FullName}

                #Output ecrypted file
                $result = Get-Item $DestinationFile
                $result | Add-Member –MemberType NoteProperty –Name SourceFile –Value $File.FullName
                $result | Add-Member –MemberType NoteProperty –Name Algorithm –Value $Algorithm
                $result | Add-Member –MemberType NoteProperty –Name Key –Value $Key
                $result | Add-Member –MemberType NoteProperty –Name CipherMode –Value $Crypto.Mode
                $result | Add-Member –MemberType NoteProperty –Name PaddingMode –Value $Crypto.Padding
                $result
            }
            Catch
            {
                Write-Error $_
                If($FileStreamWriter)
                {
                    #Remove failed file
                    $FileStreamWriter.Close()
                    Remove-Item -LiteralPath $DestinationFile -Force
                }
                Continue
            }
            Finally
            {
                if($CryptoStream){$CryptoStream.Close()}
                if($FileStreamReader){$FileStreamReader.Close()}
                if($FileStreamWriter){$FileStreamWriter.Close()}
            }
        }
    }
}

Function Unprotect-File
{
<#
.SYNOPSIS 
Decrypts a file encrypted with Protect-File.

.DESCRIPTION
Decrypts a file using a provided cryptography key.

.PARAMETER FileName
File(s) to be decrypted.

.PARAMETER Key
Cryptography key as a SecureString be used for decryption.

.PARAMETER KeyAsPlainText
Cryptography key as a String to be used for decryption.

.PARAMETER CipherMode
Specifies the block cipher mode that was used for encryption.

.PARAMETER PaddingMode
Specifies the type of padding that was applied when the message data block was shorter than the full number of bytes needed for a cryptographic operation.

.PARAMETER Suffix
Suffix of the encrypted file to be removed.

.PARAMETER RemoveSource
Removes the source (encrypted) file after decrypting.

.OUTPUTS
System.IO.FileInfo. Unprotect-File will return FileInfo with the SourceFile as an added NoteProperty

.EXAMPLE
Unprotect-File 'C:\secrets.txt.AES' $key
This example decrypts C:\secrets.txt.AES using the key stored in the variable $key. The decrypted file would remove the default extension of '.AES' and the source (encrypted) file would not be removed.

.EXAMPLE
Unprotect-File 'C:\secrets.txt.Encrypted' -Algorithm DES -Key $key -Suffix '.Encrypted' -RemoveSource
This example decrypts C:\secrets.txt.Encrypted using DES and the key stored in the variable $key. The decrypted file would remove the extension of '.Encrypted' and the source (encrypted) file would be removed.

.EXAMPLE
Get-ChildItem 'C:\Files' -Recurse | Unprotect-File -Algorithm AES -Key $key -RemoveSource
This example decrypts all of the files under the C:\Files directory using the key stored in the variable $key. The decrypted files would remove the default extension of '.AES' and the source (encrypted) files would be removed.

.NOTES
Author: Tyler Siegrist
Date: 9/22/2017
#>
[CmdletBinding(DefaultParameterSetName='SecureString')]
[OutputType([System.IO.FileInfo[]])]
Param(
    [Parameter(Mandatory=$true, Position=1, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true)]
    [Alias('PSPath','LiteralPath')]
    [string[]]$FileName,
    [Parameter(Mandatory=$false, Position=2, ValueFromPipelineByPropertyName=$true)]
    [ValidateSet('AES','DES','RC2','Rijndael','TripleDES')]
    [String]$Algorithm = 'AES',
    [Parameter(Mandatory=$true, Position=3, ValueFromPipelineByPropertyName=$true, ParameterSetName='SecureString')]
    [System.Security.SecureString]$Key,
    [Parameter(Mandatory=$true, Position=3, ParameterSetName='PlainText')]
    [String]$KeyAsPlainText,
    [Parameter(Mandatory=$false, Position=4, ValueFromPipelineByPropertyName=$true)]
    [System.Security.Cryptography.CipherMode]$CipherMode = 'CBC',
    [Parameter(Mandatory=$false, Position=5, ValueFromPipelineByPropertyName=$true)]
    [System.Security.Cryptography.PaddingMode]$PaddingMode = 'PKCS7',
    [Parameter(Mandatory=$false, Position=6)]
    [String]$Suffix, #Assigning default value in code due to it not processing ".$Algorithm" properly when Algorithm is ValueFromPipelineByPropertyName
    [Parameter()]
    [Switch]$RemoveSource
)
    Process
    {
        #Configure cryptography
        try
        {
            if($PSCmdlet.ParameterSetName -eq 'PlainText')
            {
                $Key = $KeyAsPlainText | ConvertTo-SecureString -AsPlainText -Force
            }

            #Decrypt cryptography Key from SecureString
            $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Key)
            $EncryptionKey = [System.Convert]::FromBase64String([System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR))

            $Crypto = [System.Security.Cryptography.SymmetricAlgorithm]::Create($Algorithm)
            $Crypto.Mode = $CipherMode
            $Crypto.Padding = $PaddingMode
            $Crypto.KeySize = $EncryptionKey.Length*8
            $Crypto.Key = $EncryptionKey
        }
        Catch
        {
            Write-Error $_ -ErrorAction Stop
        }

        if(-not $PSBoundParameters.ContainsKey('Suffix'))
        {
            $Suffix = ".$Algorithm"
        }

        #Used to store successfully decrypted file names.
        $Files = Get-Item -LiteralPath $FileName

        ForEach($File in $Files)
        {
            #Verify file ends with supplied suffix
            If(-not $File.Name.EndsWith($Suffix))
            {
                Write-Error "$($File.FullName) does not have an extension of '$Suffix'."
                Continue
            }

            $DestinationFile = $File.FullName -replace "$Suffix$"

            Try
            {
                $FileStreamReader = New-Object System.IO.FileStream($File.FullName, [System.IO.FileMode]::Open)
                $FileStreamWriter = New-Object System.IO.FileStream($DestinationFile, [System.IO.FileMode]::Create)

                #Get IV from file
                [Byte[]]$LenIV = New-Object Byte[] 4
                $FileStreamReader.Seek(0, [System.IO.SeekOrigin]::Begin) | Out-Null
                $FileStreamReader.Read($LenIV,  0, 3) | Out-Null
                [Int]$LIV = [System.BitConverter]::ToInt32($LenIV,  0)
                [Byte[]]$IV = New-Object Byte[] $LIV
                $FileStreamReader.Seek(4, [System.IO.SeekOrigin]::Begin) | Out-Null
                $FileStreamReader.Read($IV, 0, $LIV) | Out-Null
                $Crypto.IV = $IV

                #Peform Decryption
                $Transform = $Crypto.CreateDecryptor()
                $CryptoStream = New-Object System.Security.Cryptography.CryptoStream($FileStreamWriter, $Transform, [System.Security.Cryptography.CryptoStreamMode]::Write)
                $FileStreamReader.CopyTo($CryptoStream)

                #Close open files
                $CryptoStream.FlushFinalBlock()
                $CryptoStream.Close()
                $FileStreamReader.Close()
                $FileStreamWriter.Close()

                #Delete encrypted file
                if($RemoveSource){Remove-Item $File.FullName}

                #Output decrypted file
                Get-Item $DestinationFile | Add-Member –MemberType NoteProperty –Name SourceFile –Value $File.FullName -PassThru
            }
            Catch
            {
                Write-Error $_
                If($FileStreamWriter)
                {
                    #Remove failed file
                    $FileStreamWriter.Close()
                    Remove-Item -LiteralPath $DestinationFile -Force
                }
                Continue
            }
            Finally
            {
                if($CryptoStream){$CryptoStream.Close()}
                if($FileStreamReader){$FileStreamReader.Close()}
                if($FileStreamWriter){$FileStreamWriter.Close()}
            }
        }
    }
}

Export-ModuleMember -Function New-CryptographyKey
Export-ModuleMember -Function Protect-File
Export-ModuleMember -Function Unprotect-File