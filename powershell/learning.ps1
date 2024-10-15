# example showing how to create an object with properties mentioned
$person = New-Object PSObject | Select-Object Name, Title, Sex;
$person.Name = "John Doe";
$person.Title = "Sir"
$person.Sex = "Male";

# to import a module
Import-Module .\MyModule.psm1 -Global -Force

# list of all aliases
Get-Alias 
# specific info about ps alias 
Get-Alias ps 

# CMD commands
ipconfig
cls

# pipe
ipconfig | Out-File -FilePath C:\Users\kanavane\Desktop\ipconfig.txt; C:\Users\kanavane\Desktop\ipconfig.txt # the last part after ; opens the text file


Set-PSDebug -Strict
#$global:VerbosePreference = 'Continue'
#$global:DebugPreference = 'Continue'
#$global:ErrorActionPreference = 'Stop'


# reading and writing CSV files

# string formatting
$name = "man"
$msg = "Hi, {0}" -f $name
Write-Host $msg

# loops
for ($i = 1; $i -le 5; $i++)
{
    Write-Host $i
}

$alpha = @("a", "b", "c")
foreach($item in $alpha)
{
    Write-Host("Item: ", $item)
}


# getting system info
$os = Get-WMIObject win32_operatingsystem
Write-Host $os.OSArchitecture


# accessing env vars
$computerName = $env:COMPUTERNAME
Write-Host $computerName

# execute command, for example here I am running netsh
$NetshParams = "interface dump"
$netshCommand = ('netsh -c {0}' -f $NetshParams)
Write-Host ('Executing netsh: {0}' -f $netshCommand)
$netshResult = Invoke-Expression $netshCommand
Write-Host ('Netsh result: {0}' -f ($netshResult | Out-String))
if ($LASTEXITCODE -ne 0)
{
    throw ('Netsh failed. Exit code: {0}. Logs: {1}' -f $LASTEXITCODE, ($netshResult | Out-String))
}

# Files

# XML processing

# PowerShell Remoting

# Workflows
 
# PowerShell 4.0 
# Desired State Configuration(DSC) -> Deployment and configuration of Windows services and their settings

# PowerShell 5.0
# PowerShellGet -> to install and update PowerShell Modules
# NetworkSwitch cmdlets
# OneGet -> discover and install software packages from web
