param ($Configuration, $TargetName, $ProjectDir, $TargetPath, $TargetDir)
write-host $Configuration
write-host $TargetName
write-host $ProjectDir
write-host $TargetPath
write-host $TargetDir

# sign the dll
$cert=Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert

Set-AuthenticodeSignature -FilePath $TargetPath -Certificate $cert -IncludeChain All -TimestampServer "http://timestamp.comodoca.com/authenticode"

function CopyToAddinFolder($revitVersion) {
	
    $addinFolder = ($env:APPDATA + "\Autodesk\REVIT\Addins\" + $revitVersion)

    if (Test-Path $addinFolder) {
        try {
            # Remove previous versions
            if (Test-Path ($addinFolder  + "\" + $TargetName + ".addin")) { Remove-Item ($addinFolder  + "\" + $TargetName + ".addin") }
            if (Test-Path ($addinFolder  + "\" + $TargetName)) { Remove-Item ($addinFolder  + "\" + $TargetName) -Recurse }
            
            # create the AlignTag folder
            New-Item -ItemType Directory -Path ($addinFolder  + "\" + $TargetName)

            # Copy the addin file
            xcopy /Y ($ProjectDir + $TargetName + ".addin") ($addinFolder)
            xcopy /Y ($TargetDir + "\*.dll*") ($addinFolder  + "\" + $TargetName)
            xcopy /Y ($ProjectDir + "Resources\*.chm*") ($addinFolder  + "\" + $TargetName)
        }
        catch {
            Write-Host "Something went wrong"
        }
    }
}

$revitVersions = "2018","2019","2020","2021","2022"

Foreach ($revitVersion in $revitVersions) {
    CopyToAddinFolder $revitVersion
}

## Zip the package
$ReleasePath="G:\My Drive\05 - Travail\Revit Dev\AlignTag\Releases\Current"
$addinFolder = ($env:APPDATA + "\Autodesk\REVIT\Addins\" + $revitVersions[0])

$ReleaseZip = ($ReleasePath + "\" + $TargetName + ".zip")
if (Test-Path $ReleaseZip) { Remove-Item $ReleaseZip }

if ( Test-Path -Path $ReleasePath ) {
  7z a -tzip $ReleaseZip ($ProjectDir + $TargetName + ".addin") ($addinFolder  + "\" + $TargetName)
}


