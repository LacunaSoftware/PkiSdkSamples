$ServiceName = "LacunaSignerService"
$UserName="WIN-HVE6V411JS0\SignerServiceUser"
$ExePath='C:\LacunaSignerService'
$ExeFilePath="C:\LacunaSignerService\Lacuna.SignerService.exe"
$Description='Lacuna Signer Service'
$DisplayName="Lacuna Signer Service"
$secpasswd = ConvertTo-SecureString "27JUho2zry1i" -AsPlainText -Force

if (Get-Service $ServiceName -ErrorAction SilentlyContinue)
{
    $serviceToRemove = Get-WmiObject -Class Win32_Service -Filter "name='$ServiceName'"
    $serviceToRemove.delete()
    sc.exe delete $ServiceName 
    "service removed"
}
else
{
    "service does not exists"
}

"installing service"

$acl = Get-Acl "$ExePath"
$aclRuleArgs = "$UserName", "Read,Write,ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($aclRuleArgs)
$acl.SetAccessRule($accessRule)
$acl | Set-Acl $ExePath


$mycreds = New-Object System.Management.Automation.PSCredential ($UserName, $secpasswd)
New-Service -name $ServiceName -binaryPathName $ExeFilePath -displayName $DisplayName -startupType Automatic -credential $mycreds

"installation completed"