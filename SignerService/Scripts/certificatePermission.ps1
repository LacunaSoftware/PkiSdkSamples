﻿$serviceUser = "Server2019\ServiceUser"
$certificate = Get-ChildItem Cert:\LocalMachine\My | Where-Object Thumbprint -eq "1b32bccc4844f623571bfc892a0860a080062392"

$privateKey = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($certificate)
$containerName = ""
if ($privateKey.GetType().Name -ieq "RSACng")
{
    $containerName = $privateKey.Key.UniqueName
}
else
{
    $containerName = $privateKey.CspKeyContainerInfo.UniqueKeyContainerName
}

$keyFullPath = Get-ChildItem -Path $env:AllUsersProfile\Microsoft\Crypto -Recurse -Filter $containerName | Select -Expand FullName
if (-Not (Test-Path -Path $keyFullPath -PathType Leaf))
{
    throw "Unable to get the private key container to set permissions."
}

# Get the current ACL of the private key
$acl = (Get-Item $keyFullPath).GetAccessControl()

# Add the new ACE to the ACL of the private key
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($serviceUser, "Read", "Allow")
$acl.AddAccessRule($accessRule);

# Write back the new ACL
Set-Acl -Path $keyFullPath -AclObject $acl;
