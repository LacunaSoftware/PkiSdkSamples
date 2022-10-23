$resource = [SxPfxImport]::new()
$resource.Credential = Get-Credential -Message "enter password" -UserName "WIN-HVE6V411JS0\SignerServiceUser"
$resource.FilePath = "C:\LacunaSignerService\Pierre_de_Fermat.pfx"
$resource.Location = "CurrentUser"
$resource.Store = "My"

$resource.Test()