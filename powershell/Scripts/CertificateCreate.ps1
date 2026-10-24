<#
.SYNOPSIS
Creates and exports a self-signed code-signing certificate.

.DESCRIPTION
Creates a self-signed code-signing certificate in the current user's personal
certificate store, exports its public certificate to a .cer file, and outputs
the Base64 encoded certificate either to standard output or to the clipboard.
Standard output also includes details about the generated certificate.

.PARAMETER CertificateName
The certificate name. The script uses this value for the certificate's common
name (CN), friendly name, and default output file name.

.PARAMETER OutputDirectory
The directory in which to create the exported .cer file. The directory is
created when it does not exist. The default is the current directory.

.PARAMETER ValidityInDays
The number of days for which the certificate is valid, starting when the script
runs. The default is 365.

.PARAMETER CertificateFileName
The name of the exported public certificate file. The default is
<CertificateName>.cer.

.PARAMETER Base64Output
Where to send the Base64 encoded certificate. Use StandardOutput to write it to
stdout after a summary of the certificate details, or Clipboard to copy only
the Base64 value to the clipboard. The default is StandardOutput.

.PARAMETER KeyLength
The RSA key length in bits. Supported values are 2048, 3072, and 4096. The
default is 2048.

.PARAMETER Help
Prints detailed parameter descriptions and example invocations without creating
a certificate.

.EXAMPLE
.\CertificateCreate.ps1

Creates MySelfSignedCert.cer in the current directory, valid for 365 days, and
writes its details and Base64 representation to standard output.

.EXAMPLE
.\CertificateCreate.ps1 -CertificateName "ApiClient" -OutputDirectory "C:\certs" -ValidityInDays 730 -Base64Output Clipboard

Creates C:\certs\ApiClient.cer, valid for 730 days, and copies its Base64
representation to the clipboard.

.EXAMPLE
.\CertificateCreate.ps1 -CertificateName "SigningCert" -CertificateFileName "signing-public.cer" -KeyLength 4096 -Base64Output StandardOutput

Creates a certificate with a 4096-bit key and writes its Base64 representation
and certificate details to standard output.

.EXAMPLE
.\CertificateCreate.ps1 -Help

Prints this help information without creating a certificate.
#>
[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$CertificateName = 'MySelfSignedCert',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$OutputDirectory = (Get-Location).Path,

    [Parameter()]
    [ValidateRange(1, 36500)]
    [int]$ValidityInDays = 365,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$CertificateFileName,

    [Parameter()]
    [ValidateSet('StandardOutput', 'Clipboard')]
    [string]$Base64Output = 'StandardOutput',

    [Parameter()]
    [ValidateSet(2048, 3072, 4096)]
    [int]$KeyLength = 2048,

    [Parameter()]
    [Alias('h')]
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Help) {
    Get-Help $PSCommandPath -Detailed
    return
}

if (-not $CertificateFileName) {
    $CertificateFileName = "$CertificateName.cer"
}

if ([System.IO.Path]::GetExtension($CertificateFileName) -ne '.cer') {
    throw "CertificateFileName must have a .cer extension."
}

$invalidFileNameCharacters = [System.IO.Path]::GetInvalidFileNameChars()
if ($CertificateFileName.IndexOfAny($invalidFileNameCharacters) -ge 0) {
    throw "CertificateFileName must be a file name, not a path. Use OutputDirectory to specify its location."
}

$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
if (-not [System.IO.Directory]::Exists($resolvedOutputDirectory)) {
    [System.IO.Directory]::CreateDirectory($resolvedOutputDirectory) | Out-Null
}

$certificatePath = Join-Path -Path $resolvedOutputDirectory -ChildPath $CertificateFileName
$certificate = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=$CertificateName" `
    -FriendlyName $CertificateName `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength $KeyLength `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddDays($ValidityInDays)

Export-Certificate `
    -Cert $certificate `
    -FilePath $certificatePath `
    -Force | Out-Null

$certificateBytes = [System.IO.File]::ReadAllBytes($certificatePath)
$base64Certificate = [System.Convert]::ToBase64String($certificateBytes)

if ($Base64Output -eq 'Clipboard') {
    Set-Clipboard -Value $base64Certificate
}
else {
    Write-Host 'Certificate details:' -ForegroundColor Cyan
    Write-Output "  Subject:             $($certificate.Subject)"
    Write-Output "  Issuer:              $($certificate.Issuer)"
    Write-Output "  Valid from:          $($certificate.NotBefore.ToString('u'))"
    Write-Output "  Expires:             $($certificate.NotAfter.ToString('u'))"
    Write-Output "  Signature algorithm: $($certificate.SignatureAlgorithm.FriendlyName)"
    Write-Output "  Public key algorithm: $($certificate.PublicKey.Oid.FriendlyName)"
    Write-Output '  Intended purpose:    Code Signing'
    Write-Output "  Key length:          $KeyLength bits"
    Write-Output "  Serial number:       $($certificate.SerialNumber)"
    Write-Output "  Thumbprint:          $($certificate.Thumbprint)"
    Write-Output "  Certificate file:    $certificatePath"
    Write-Output ''
    Write-Host 'Base64 certificate:' -ForegroundColor Green
    Write-Output $base64Certificate
}
