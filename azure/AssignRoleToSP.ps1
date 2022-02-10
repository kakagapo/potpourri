# following info from https://blogs.aaddevsup.xyz/2018/08/how-to-add-an-azure-ad-role-to-a-enterprise-application-service-principal/

Connect-AzureAD -AzureEnvironmentName <env name>

# to confirm tenant you are connected to
Get-AzureADTenantDetail

$spName = "xxx"

# identify the SP object ID
$mysp = Get-AzureADServicePrincipal -searchstring $spName
$mysp.ObjectId

# identify the directory role object ID
$myAADRole = Get-AzureADDirectoryRole | Where-Object {$_.displayName -eq 'Global administrator'}
$myAADRole.ObjectId

# assign role to SP
Add-AzureADDirectoryRoleMember -ObjectId $myAADRole.ObjectId -RefObjectId $mysp.ObjectId

# to confirm directory role assignment to the service principals
Get-AzureADDirectoryRoleMember -ObjectId $myAADRole.ObjectId | Where-Object {$_.ObjectType -eq 'ServicePrincipal'}

# to create test users using AzureAD powershell module
$PasswordProfile = New-Object -TypeName Microsoft.Open.AzureAD.Model.PasswordProfile
$PasswordProfile.Password = "<password>"
New-AzureADUser -DisplayName "<display name>" -PasswordProfile $PasswordProfile -UserPrincipalName "<upn>" -AccountEnabled $true -MailNickName "<mail nickname>"