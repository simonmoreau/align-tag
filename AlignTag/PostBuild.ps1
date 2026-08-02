param ($RevitVersion, $TargetName, $ProjectDir, $TargetPath, $TargetDir, $Configuration)
write-host $RevitVersion
write-host $TargetName
write-host $ProjectDir
write-host $TargetPath
write-host $TargetDir

# sign the dll
# sign all dll and exe

$filePaths = @()

function CopyToFolder($revitVersion, $addinFolder) {

    if (Test-Path $addinFolder) {
        try {
            # Remove previous versions
            if (Test-Path ($addinFolder  + "\AlignTag.addin")) { Remove-Item ($addinFolder  + "\AlignTag.addin") }
            if (Test-Path ($addinFolder  + "\" + $TargetName)) { Remove-Item ($addinFolder  + "\" + $TargetName) -Recurse }
            
            # create the ConfigurateurDePlancher folder
            New-Item -ItemType Directory -Path ($addinFolder  + "\" + $TargetName)
            New-Item -ItemType Directory -Path ($addinFolder  + "\" + $TargetName + "\Resources")

            # Copy the addin file
            xcopy /Y ($ProjectDir + "AlignTag.addin") ($addinFolder)
            xcopy /Y ($TargetDir + "\*.dll*") ($addinFolder  + "\" + $TargetName)
            xcopy /Y ($TargetDir + "\Resources\*.*") ($addinFolder  + "\" + $TargetName + "\Resources")
            xcopy /Y ($TargetDir + "\*.json*") ($addinFolder  + "\" + $TargetName)
        }
        catch {
            Write-Host "Something went wrong"
        }
    }
}

function SignFiles($TargetDir) {
    Get-ChildItem ($TargetDir) -Recurse | Where-Object { $_.extension -in ".exe", ".dll" } |
    Foreach-Object {

        $signature = Get-AuthenticodeSignature -FilePath $_.FullName

        if ($signature.status -ne "Valid") {
            # $filePaths = $filePaths + " " + "`"" + $_.FullName +"`""
            $filePaths += $_.FullName
        }
    }

    Write-Host $filePaths

    azuresigntool sign -du "https://www.eai.fr" `
      -fd sha384 -kvu https://kv-eai-util-fr-signing.vault.azure.net/ `
      -kvm `
      -kvc EAICodeSigningCertificate `
      -tr http://timestamp.digicert.com `
      -td sha384 `
      -v $filePaths
}


if ($Configuration -eq "Debug") {

    SignFiles $TargetDir

    # Copy to Addin folder for debug
    $addinFolder = ($env:APPDATA + "\Autodesk\REVIT\Addins\" + $RevitVersion)
    CopyToFolder $RevitVersion $addinFolder
}



