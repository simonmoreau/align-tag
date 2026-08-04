function GetVersion($solutionDirectory) {

    $propsPath = Join-Path $solutionDirectory 'CommonAssemblyInfo.props'
    if (-not (Test-Path $propsPath)) {
        Write-Error "CommonAssemblyInfo.props not found at $propsPath"
        $appVersion = ''
    }
    else {
        [xml]$propsXml = Get-Content $propsPath -Raw
        $versionNode = $propsXml.Project.PropertyGroup | Where-Object { $_.Version -ne $null -and $_.Version.Trim() -ne '' } | Select-Object -First 1
        if ($versionNode -and $versionNode.Version) {
            [string]$appVersion = $versionNode.Version.Trim()
        }
        else {
            $appVersion = ''
        }
    }

    return $appVersion
}

function InitializeBundleStructure($solutionDirectory, $versions) {
    $bundleDirectory = Join-Path $solutionDirectory 'install\BIM 42 Align.bundle'
    $bundleContentsDirectory = Join-Path $bundleDirectory 'Contents'

    $packageContentsSource = Join-Path $solutionDirectory 'src\AlignTag\PackageContents.xml'
    $addinSource = Join-Path $solutionDirectory 'src\AlignTag\AlignTag.addin'

    if (-not (Test-Path $packageContentsSource)) {
        throw "PackageContents.xml not found at $packageContentsSource"
    }

    if (-not (Test-Path $addinSource)) {
        throw "AlignTag.addin not found at $addinSource"
    }

    New-Item -ItemType Directory -Path $bundleContentsDirectory -Force | Out-Null

    Copy-Item -Path $packageContentsSource -Destination (Join-Path $bundleDirectory 'PackageContents.xml') -Force

    foreach ($v in $versions) {
        $versionDirectory = Join-Path $bundleContentsDirectory $v
        $pluginOutputDirectory = Join-Path $versionDirectory 'AlignTag'

        New-Item -ItemType Directory -Path $pluginOutputDirectory -Force | Out-Null
        Copy-Item -Path $addinSource -Destination (Join-Path $versionDirectory 'AlignTag.addin') -Force
    }

    return $bundleDirectory
}

function SignFiles($TargetDir) {
    $filePaths = @()
    
    Get-ChildItem ($TargetDir) -Recurse | Where-Object { $_.extension -in ".exe", ".dll", ".msi" } |
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

function CreateZipPackage($solutionDirectory, $bundleDirectory, $version) {
    $installDirectory = Join-Path $solutionDirectory 'install'
    $outputDirectory = Join-Path $installDirectory 'OutputDirectory'
    $zipPath = Join-Path $outputDirectory "BIM42_Align_$version.zip"

    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

    if (Test-Path $zipPath) {
        Remove-Item -Path $zipPath -Force
    }

    $bundleName = Split-Path -Path $bundleDirectory -Leaf
    Push-Location $installDirectory
    try {
        Compress-Archive -Path $bundleName -DestinationPath $zipPath -Force
    }
    finally {
        Pop-Location
    }

    return $zipPath
}

$scriptDir = $PSScriptRoot
$solutionDirectory = (Resolve-Path (Join-Path $scriptDir '..')).Path

$navTargetDirectory = Join-Path $solutionDirectory 'src\AlignTag\'
$navPluginProject = Join-Path $navTargetDirectory 'AlignTag.csproj'

$versions = @('2022','2023','2024','2025','2026', '2027')

$version = GetVersion $solutionDirectory
$bundleDirectory = InitializeBundleStructure $solutionDirectory $versions

foreach ($v in $versions) {
    $pluginOutputDirectory = Join-Path $bundleDirectory "Contents\$v\AlignTag"
    dotnet build $navPluginProject --configuration $v --no-incremental --output $pluginOutputDirectory
}

SignFiles $bundleDirectory
$zipPath = CreateZipPackage $solutionDirectory $bundleDirectory $version
Write-Host "Package created: $zipPath"

