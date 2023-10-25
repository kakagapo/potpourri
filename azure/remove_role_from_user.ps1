Connect-AzureAD 
Get-AzureADMSRoleAssignment -Filter "principalId eq '69584002-b4d1-4055-9c94-320542efd653'"
Remove-AzureADMSRoleAssignment -Id <String> 
Get-MgUserAppRoleAssignment -UserId <String>

Get-MgDirectoryRole 

$GlobalAdminRoleId = $DirectoryRoles | ? {$_.DisplayName -eq "Global administrator"} | select -ExpandProperty Id

$User = Get-MgUser -UserId Ken.Bowers@Office365itpros.com
$RoleMembers = Get-MgDirectoryRoleMember -DirectoryRoleId $TeamsAdminRoleId
If ($User.Id -notin $RoleMembers.Id) {
  Write-Host ("Adding user {0} to the Teams administrator role" -f $User.DisplayName)
  New-MgDirectoryRoleMemberByRef -DirectoryRoleId $TeamsAdminRoleId -BodyParameter @{"@odata.id" = "https://graph.microsoft.com/v1.0/directoryObjects/$($user.Id)"}
}


// Todo: create the worker 
Remove-MgDirectoryRoleMemberByRef -DirectoryRoleId $directoryRoleId -DirectoryObjectId $directoryObjectId