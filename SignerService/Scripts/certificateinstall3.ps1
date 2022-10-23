$filelocale = "C:\LacunaSignerService\Pierre_de_Fermat.pfx"
$Pass = ConvertTo-SecureString -String '1234' -Force -AsPlainText
$User = "whatever"
$Cred = New-Object -TypeName "System.Management.Automation.PSCredential" -ArgumentList $User, $Pass
Import-PfxCertificate -FilePath $filelocale -CertStoreLocation Cert:\LocalMachine\My -Password $Cred.Password