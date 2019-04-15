$cert = New-SelfSignedCertificate -DnsName "atomix.me" -Type CodeSigning -CertStoreLocation cert:\LocalMachine\My
$rootStore = Get-Item cert:\LocalMachine\Root
$rootStore.Open("ReadWrite")
$rootStore.Add($cert)
$rootStore.Close()