$UserName="WIN-HVE6V411JS0\SignerServiceUser"
$ExePath='C:\LacunaSignerService'
$ExeFilePath='C:\LacunaSignerService\Lacuna.SignerService.exe'
$Description='Lacuna Signer Service'
$DisplayName='Lacuna Signer Service'

$acl = Get-Acl $ExePath
$aclRuleArgs = $UserName, "Read,Write,ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($aclRuleArgs)
$acl.SetAccessRule($accessRule)
$acl | Set-Acl $ExePath

New-Service -Name LacunaSignerService -BinaryPathName "$ExeFilePath --contentRoot $ExePath" -Credential $UserName -Description $Description -DisplayName $DisplayName -StartupType Automatic
