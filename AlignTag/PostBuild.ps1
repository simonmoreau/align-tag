param ($Configuration, $TargetName, $ProjectDir, $TargetPath, $TargetDir)
write-host $Configuration
write-host $TargetName
write-host $ProjectDir
write-host $TargetPath
write-host $TargetDir

# sign the dll
$thumbPrint = "e729567d4e9be8ffca04179e3375b7669bccf272"
$cert=Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert | Where { $_.Thumbprint -eq $thumbPrint}

Set-AuthenticodeSignature -FilePath $TargetPath -Certificate $cert -IncludeChain All -TimestampServer "http://timestamp.comodoca.com/authenticode"

function CopyToFolder($revitVersion, $addinFolder) {

    if (Test-Path $addinFolder) {
        try {
            # Remove previous versions
            if (Test-Path ($addinFolder  + "\" + $TargetName + ".addin")) { Remove-Item ($addinFolder  + "\" + $TargetName + ".addin") }
            if (Test-Path ($addinFolder  + "\" + $TargetName)) { Remove-Item ($addinFolder  + "\" + $TargetName) -Recurse }
            
            # create the bimsync folder
            New-Item -ItemType Directory -Path ($addinFolder  + "\" + $TargetName)

            # Copy the addin file
            xcopy /Y ($ProjectDir + $TargetName + ".addin") ($addinFolder)
            xcopy /Y ($TargetDir + "\*.dll*") ($addinFolder  + "\" + $TargetName)
        }
        catch {
            Write-Host "Something went wrong"
        }
    }
}

$revitVersion = $Configuration.replace('Debug','').replace('Release','')

# Copy to Addin folder for debug
$addinFolder = ($env:APPDATA + "\Autodesk\REVIT\Addins\" + $revitVersion)
CopyToFolder $revitVersion $addinFolder

# Copy to release folder for building the package
$ReleasePath="G:\My Drive\05 - Travail\Revit Dev\AlignTag\Releases"
$releaseFolder = ($ReleasePath + "\BIM 42 Align.bundle\Contents\" + $revitVersion + "\")
CopyToFolder $revitVersion $releaseFolder


## Zip the package

$BundleFolder = ($ReleasePath + "\BIM 42 Align.bundle")

$ReleaseZip = ($ReleasePath + "\" + $TargetName + ".zip")
if (Test-Path $ReleaseZip) { Remove-Item $ReleaseZip }

if ( Test-Path -Path $ReleasePath ) {
  7z a -tzip $ReleaseZip ($BundleFolder)
}