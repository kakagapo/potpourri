# got from Stack Overflow - https://securecloud.blog/2018/10/16/you-cant-hide-things-in-aad/


Add-Type -Path $env:USERPROFILE\AzureADToken\Microsoft.IdentityModel.Clients.ActiveDirectory.2.12.111071459\lib\net45\Microsoft.IdentityModel.Clients.ActiveDirectory.dll 
 
$authContext = New-Object "Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext" -ArgumentList "https://login.windows.net/common/"
$ThisPromptBehavior = [Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior]::Always
$authResult = $AuthContext.AcquireToken("https://graph.microsoft.com","1b730954-1685-4b74-9bfd-dac224a7b894", "urn:ietf:wg:oauth:2.0:oob", $ThisPromptBehavior)
 
#URI to use the access token
$InitialURI = "https://graph.microsoft.com/v1.0/users/delta"
$GroupsURI = "https://graph.microsoft.com/beta/groups/delta"
 
 $Headers = @{
 "Authorization" = ($authResult.AccessTokenType) +" "+ ($authResult.AccessToken)
 }
 
#Subscribe for changes you're interested user objects
$req = Invoke-RestMethod -Method Get -Uri $uri -UseBasicParsing -Headers $headers -Verbose
#Subscribe for changes you're interested in groups
$greq = Invoke-RestMethod -Method Get -Uri $GroupsURI -UseBasicParsing -Headers $headers -Verbose
#query for all changes, and populate results to array
$ChangeArray = @();$count=$null
 
do
{
    $count = $count + $req.value.Count;$count
    $req = Invoke-RestMethod -Method Get -Uri $req.'@odata.nextLink' -UseBasicParsing -Headers $headers -Verbose
 
    $ChangeArray += $req.value
    $req.value
 
    Write-Host "Next Link"
 
} while ($req.'@odata.nextLink' -ne $null)
 
do
{
    $count = $count + $greq.value.Count;$count
    $greq = Invoke-RestMethod -Method Get -Uri $greq.'@odata.nextLink' -UseBasicParsing -Headers $headers -Verbose
 
    $ChangeArray += $greq.value
    $greq.value
 
    Write-Host "Next Link"
} while ($greq.'@odata.nextLink' -ne $null)