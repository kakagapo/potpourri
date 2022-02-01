# https://stackoverflow.com/questions/30454771/how-does-azure-powershell-work-with-username-password-based-auth

# You don't need to create an application registration in Azure Active Directory for Azure Powershell. 
# To leverage username/password credentials of an Azure AD user, you can use the Add-AzureAccount cmdlet:
$username = "admin@your_account.onmicrosoft.com"
$password = "SuperSecretPassword" | ConvertTo-SecureString -AsPlainText -Force

$credential = New-Object -TypeName System.Management.Automation.PSCredential -ArgumentList $username, $password 
Add-AzureAccount -Credential $credential 

# Load Active Directory Authentication Library (ADAL) Assemblies
$adal = "${env:ProgramFiles(x86)}\Microsoft SDKs\Azure\PowerShell\ServiceManagement\Azure\Services\Microsoft.IdentityModel.Clients.ActiveDirectory.dll"
$adalforms = "${env:ProgramFiles(x86)}\Microsoft SDKs\Azure\PowerShell\ServiceManagement\Azure\Services\Microsoft.IdentityModel.Clients.ActiveDirectory.WindowsForms.dll"
[System.Reflection.Assembly]::LoadFrom($adal)
[System.Reflection.Assembly]::LoadFrom($adalforms)

# Set Azure AD Tenant name
$adTenant = "yourtenant.onmicrosoft.com" 

# Set well-known client ID for Azure PowerShell
$clientId = "1950a258-227b-4e31-a9cf-717495945fc2" 

# Set redirect URI for Azure PowerShell
$redirectUri = "urn:ietf:wg:oauth:2.0:oob"

# Set Resource URI to Azure Service Management API
$resourceAppIdURI = "https://management.core.windows.net/"

# Set Authority to Azure AD Tenant
$authority = "https://login.windows.net/$adTenant"

# Set user credentials (*** obviously you wouldn't have the password in clear text in a production script ***)
$userName = "admin@your_tenant.onmicrosoft.com"
$password = "SecretPassword"
$creds = New-Object "Microsoft.IdentityModel.Clients.ActiveDirectory.UserCredential" -ArgumentList $userName,$password

# Create AuthenticationContext tied to Azure AD Tenant
$authContext = New-Object "Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext" -ArgumentList $authority

# Acquire token
$authResult = $authContext.AcquireToken($resourceAppIdURI,$clientId,$creds)