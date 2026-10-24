<#
.SYNOPSIS
Updates the federation configuration for a verified Microsoft Entra domain.

.DESCRIPTION
Uses the Microsoft Graph PowerShell SDK to verify a domain, resolve its internal
domain federation configuration, and update the supplied properties with
Update-MgDomainFederationConfiguration.

The script uses the current Microsoft Graph connection when one exists.
Otherwise, it connects with Domain.Read.All and
Domain-InternalFederation.ReadWrite.All delegated permissions.

.PARAMETER DomainId
The verified domain name in Microsoft Entra ID, such as contoso.com.

.PARAMETER Configuration
A hashtable containing one or more federation properties to update. Supported
properties include DisplayName, IssuerUri, PassiveSignInUri, ActiveSignInUri,
MetadataExchangeUri, SignOutUri, SigningCertificate, NextSigningCertificate,
FederatedIdpMfaBehavior, PreferredAuthenticationProtocol,
PromptLoginBehavior, IsSignedAuthenticationRequestRequired, PasswordResetUri,
SigningCertificateUpdateStatus, and SystemBrowserEnabledOn.

.PARAMETER FederationConfigurationId
The ID of the internal domain federation configuration to update. This parameter
is optional when the domain has exactly one federation configuration.

.PARAMETER Help
Prints detailed parameter descriptions and example invocations without
connecting to Microsoft Graph or updating the domain.

.EXAMPLE
.\Update-DomainFederationConfiguration.ps1 `
    -DomainId "contoso.com" `
    -Configuration @{
        DisplayName = "Contoso Federation"
        FederatedIdpMfaBehavior = "acceptIfMfaDoneByFederatedIdp"
    }

Updates the display name and federated IdP MFA behavior. The federation
configuration ID is resolved automatically.

.EXAMPLE
$certificate = [Convert]::ToBase64String(
    [IO.File]::ReadAllBytes("C:\certs\adfs-token-signing.cer")
)

.\Update-DomainFederationConfiguration.ps1 `
    -DomainId "contoso.com" `
    -Configuration @{ SigningCertificate = $certificate }

Updates the primary token-signing certificate for the domain federation.

.EXAMPLE
.\Update-DomainFederationConfiguration.ps1 `
    -DomainId "contoso.com" `
    -FederationConfigurationId "2a8ce608-bb34-473f-9e0f-f373ee4cbc5a" `
    -Configuration @{ PassiveSignInUri = "https://sts.contoso.com/adfs/ls/" } `
    -WhatIf

Shows the update that would be performed against a specific federation
configuration without making the change.

.EXAMPLE
.\Update-DomainFederationConfiguration.ps1 -Help

Prints this help information without connecting to Microsoft Graph.
#>
[CmdletBinding(
    DefaultParameterSetName = 'Update',
    SupportsShouldProcess,
    ConfirmImpact = 'High'
)]
param(
    [Parameter(Mandatory, ParameterSetName = 'Update')]
    [ValidateNotNullOrEmpty()]
    [string]$DomainId,

    [Parameter(Mandatory, ParameterSetName = 'Update')]
    [ValidateNotNull()]
    [hashtable]$Configuration,

    [Parameter(ParameterSetName = 'Update')]
    [ValidateNotNullOrEmpty()]
    [string]$FederationConfigurationId,

    [Parameter(Mandatory, ParameterSetName = 'Help')]
    [Alias('h')]
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Help) {
    Get-Help $PSCommandPath -Detailed
    return
}

if ($Configuration.Count -eq 0) {
    throw 'Configuration must contain at least one federation property to update.'
}

$supportedProperties = @(
    'ActiveSignInUri'
    'DisplayName'
    'FederatedIdpMfaBehavior'
    'IsSignedAuthenticationRequestRequired'
    'IssuerUri'
    'MetadataExchangeUri'
    'NextSigningCertificate'
    'PassiveSignInUri'
    'PasswordResetUri'
    'PreferredAuthenticationProtocol'
    'PromptLoginBehavior'
    'SigningCertificate'
    'SigningCertificateUpdateStatus'
    'SignOutUri'
    'SystemBrowserEnabledOn'
)

$unsupportedProperties = @(
    $Configuration.Keys |
        Where-Object { $_ -notin $supportedProperties }
)

if ($unsupportedProperties.Count -gt 0) {
    throw "Unsupported federation configuration properties: $($unsupportedProperties -join ', ')."
}

$requiredCommands = @(
    'Connect-MgGraph'
    'Get-MgContext'
    'Get-MgDomain'
    'Get-MgDomainFederationConfiguration'
    'Update-MgDomainFederationConfiguration'
)

$missingCommands = @(
    $requiredCommands |
        Where-Object { -not (Get-Command -Name $_ -ErrorAction SilentlyContinue) }
)

if ($missingCommands.Count -gt 0) {
    throw "Required Microsoft Graph commands are unavailable: $($missingCommands -join ', '). Install Microsoft.Graph.Authentication and Microsoft.Graph.Identity.DirectoryManagement."
}

if (-not (Get-MgContext)) {
    Connect-MgGraph `
        -Scopes 'Domain.Read.All', 'Domain-InternalFederation.ReadWrite.All' `
        -NoWelcome | Out-Null
}

$domain = Get-MgDomain -DomainId $DomainId -Property Id, IsVerified, AuthenticationType
if (-not $domain.IsVerified) {
    throw "Domain '$DomainId' is not verified in the connected Microsoft Entra tenant."
}

$federationConfigurations = @(
    Get-MgDomainFederationConfiguration -DomainId $DomainId -All
)

if ($FederationConfigurationId) {
    $federationConfiguration = $federationConfigurations |
        Where-Object Id -eq $FederationConfigurationId |
        Select-Object -First 1

    if (-not $federationConfiguration) {
        throw "Federation configuration '$FederationConfigurationId' was not found for domain '$DomainId'."
    }
}
else {
    if ($federationConfigurations.Count -eq 0) {
        throw "Verified domain '$DomainId' does not have a federation configuration."
    }

    if ($federationConfigurations.Count -gt 1) {
        $configurationIds = $federationConfigurations.Id -join ', '
        throw "Domain '$DomainId' has multiple federation configurations ($configurationIds). Specify FederationConfigurationId."
    }

    $federationConfiguration = $federationConfigurations[0]
}

$propertyNames = @($Configuration.Keys | Sort-Object)
$target = "$DomainId/$($federationConfiguration.Id)"
$operation = "Update federation properties: $($propertyNames -join ', ')"

if ($PSCmdlet.ShouldProcess($target, $operation)) {
    Update-MgDomainFederationConfiguration `
        -DomainId $DomainId `
        -InternalDomainFederationId $federationConfiguration.Id `
        -BodyParameter $Configuration

    Get-MgDomainFederationConfiguration `
        -DomainId $DomainId `
        -InternalDomainFederationId $federationConfiguration.Id
}
