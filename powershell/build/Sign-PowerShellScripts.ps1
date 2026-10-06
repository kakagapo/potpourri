[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$Path,

    [Parameter()]
    [AllowEmptyString()]
    [string]$CertificateThumbprint,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]$CertificateSubject = 'CN=MySelfSignedCert'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$now = Get-Date
$codeSigningEnhancedKeyUsage = '1.3.6.1.5.5.7.3.3'
$candidateCertificates = Get-ChildItem -Path Cert:\CurrentUser\My |
    Where-Object {
        $candidate = $_
        $enhancedKeyUsageObjectIds = @(
            $candidate.EnhancedKeyUsageList | ForEach-Object { $_.ObjectId }
        )

        $candidate.HasPrivateKey -and
        $candidate.NotBefore -le $now -and
        $candidate.NotAfter -gt $now -and
        $enhancedKeyUsageObjectIds -contains $codeSigningEnhancedKeyUsage
    }

if ($CertificateThumbprint) {
    $normalizedThumbprint = $CertificateThumbprint.Replace(' ', '').ToUpperInvariant()
    $certificate = $candidateCertificates |
        Where-Object Thumbprint -eq $normalizedThumbprint |
        Select-Object -First 1
}
else {
    $certificate = $candidateCertificates |
        Where-Object Subject -eq $CertificateSubject |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1
}

if (-not $certificate) {
    $selector = if ($CertificateThumbprint) {
        "thumbprint '$CertificateThumbprint'"
    }
    else {
        "subject '$CertificateSubject'"
    }

    throw "No valid code-signing certificate with a private key was found for $selector in Cert:\CurrentUser\My. Run Scripts\CertificateCreate.ps1 to create one."
}

$resolvedScriptPath = (Resolve-Path -LiteralPath $Path).Path
$signature = Set-AuthenticodeSignature `
    -FilePath $resolvedScriptPath `
    -Certificate $certificate `
    -HashAlgorithm SHA256

if (-not $signature.SignerCertificate -or
    $signature.SignerCertificate.Thumbprint -ne $certificate.Thumbprint) {
    throw "Signing failed for '$resolvedScriptPath': $($signature.StatusMessage)"
}

Write-Host "Signed $resolvedScriptPath with certificate $($certificate.Thumbprint)."
