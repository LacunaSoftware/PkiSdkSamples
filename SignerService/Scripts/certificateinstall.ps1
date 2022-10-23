[DscResource()]
class SxPfxImport{

	[DscProperty(Key)]
	[string]$FilePath

	[DscProperty(Mandatory)]
	[string]$Location

	[DscProperty(Mandatory)]
	[string]$Store
	
	[DscProperty()]
	[pscredential]$Credential

	SxPfxImport(){
	}
	[void]Set(){
		$command = {
			param($storeLocation, $certPath)
            $Pass = ConvertTo-SecureString -String '1234' -Force -AsPlainText
            $User = "whatever"
            $Cred = New-Object -TypeName "System.Management.Automation.PSCredential" -ArgumentList $User, $Pass
			Import-PfxCertificate -FilePath $certPath -CertStoreLocation $storeLocation -Password $Cred.Password
		}

		$job = Start-Job -ScriptBlock $command -Credential $this.Credential -ArgumentList $this._getStoreAddress(), $this.FilePath
		$job | Wait-Job | Out-Null
		$job | Receive-Job
	}
	[bool]Test(){
        $command = {
			param($storeLocation, $certPath)
        
		    $targetThumbPrint = (Get-PfxCertificate -FilePath $certPath).Thumbprint
		
		    $storeItems = Get-ChildItem -Path $storeLocation | Where-object {$_.Thumbprint -eq $targetThumbPrint}

		    $result = ($storeItems.Count -gt 0)

		    return $result
        }

		$job = Start-Job -ScriptBlock $command -Credential $this.Credential -ArgumentList $this._getStoreAddress(), $this.FilePath
		$job | Wait-Job | Out-Null
		return $job | Receive-Job
	}
	[SxPfxImport]Get(){
		return $this
	}
	[string]_getStoreAddress(){
		$certPath = 'Cert:' | Join-Path -ChildPath $this.Location | Join-Path -ChildPath $this.Store
		return $certPath
	}
}