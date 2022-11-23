param(
    [Parameter(Mandatory)]
    [System.String]$Version,

    [Parameter(Mandatory)]
    [ValidateSet('Debug','Release')]
    [System.String]$Target,
    
    [Parameter(Mandatory)]
    [System.String]$TargetPath,
    
    [Parameter(Mandatory)]
    [System.String]$TargetAssembly,
    
    [Parameter(Mandatory)]
    [System.String]$ProjectPath
)

# Make sure Get-Location is the script path
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Test some preliminaries
("$TargetPath",
 "$ProjectPath"
) | % {
    if (!(Test-Path "$_")) {Write-Error -ErrorAction Stop -Message "$_ folder is missing"}
}
# Plugin name without ".dll"
$name = "$TargetAssembly" -Replace('.dll')
$author = "otDan"
$PluginName = "TESTING-$name"
$FolderName = "$author-$PluginName"

# Set some directories
$ProfilePath = "C:\Users\Daniel\AppData\Roaming\r2modmanPlus-local\ROUNDS\profiles\ModdedTesting"
$PluginFolder = "$ProfilePath\BepInEx\plugins\$FolderName"

if(-Not (Test-Path -Path $PluginFolder)) {
    New-Item -Type Directory -Path $PluginFolder | Out-Null
}

$ModListPath = "$ProfilePath\mods.yml"
$RegisteredMod = Select-String -Path $ModListPath -Pattern "$FolderName"
if(!($RegisteredMod -ne $null)) {
    $ModInfo = @"
- manifestVersion: 1
  name: $FolderName
  authorName: $author
  websiteUrl: ""
  displayName: $PluginName
  description: ""
  gameVersion: ""
  networkMode: ""
  packageType: ""
  installMode: ""
  loaders: []
  dependencies: []
  incompatibilities: []
  optionalDependencies: []
  versionNumber:
    major: 9
    minor: 9
    patch: 9
  enabled: true
  icon: $ProfilePath\BepInEx\plugins\$FolderName\icon.png
"@

    Add-Content $ModListPath $ModInfo
}

$ReleasePath = "$ProjectPath\release"
$OutPath = "$ReleasePath\out"

# Go
Write-Host "---------------------------------------"
Write-Host ""
Write-Host "Making for $Target by $author from $ProjectPath"

# Release package for ThunderStore
if($Target.Equals("Release")) {
    Write-Host ""
    Write-Host "Packaging $name $Version for ThunderStore"
    $ThunderPath = "$OutPath\package"
    if(!(Test-Path -Path $ThunderPath)) {
        New-Item -Type Directory -Path $ThunderPath | Out-Null
    }

    $PluginsPath = "$ThunderPath\plugins"
    if(!(Test-Path -Path $PluginsPath)) {
        New-Item $PluginsPath -ItemType Directory | Out-Null
    }

    ForEach($Path in "$PluginFolder", "$ThunderPath\plugins\") {
        ForEach($NextPath in "$TargetPath\$name.dll", "$ReleasePath\icon.png") {
            Copy-Item -Path $NextPath -Destination $Path
        }
    }
    Copy-Item -Path "$ProjectPath\README.md" -Destination "$ThunderPath\README.md"
    Copy-Item -Path "$ReleasePath\manifest.json" -Destination "$ThunderPath\manifest.json"
    Copy-Item -Path "$ReleasePath\icon.png" -Destination "$ThunderPath\icon.png"

    $RequiredPath = "$ReleasePath\out\required"
    if(Test-Path -Path $RequiredPath) {
        $RequiredFiles = Get-ChildItem "$RequiredPath" -Filter *.dll
        ForEach($RequiredDll in $RequiredFiles) {
            $DependencyPath = "$ThunderPath\dependencies"
            if(!(Test-Path -Path $DependencyPath)) {
                New-Item $DependencyPath -ItemType Directory | Out-Null
            }

            $DllName = [System.IO.Path]::GetFileName("$RequiredDll")
            Write-Host "Dll: $DllName"
            Copy-Item -Path "$ReleasePath\out\required\$RequiredDll" -Destination "$DependencyPath\$DllName"
        }
    }

    $Description = Get-Content "$ReleasePath\description.txt" -First 1
    (Get-Content -path "$ThunderPath\manifest.json" -Raw).replace("#VERSION#", "$Version").replace("#DESCRIPTION#", "$Description") | Set-Content -Path "$ThunderPath\manifest.json"
    (Get-Content -path "$ThunderPath\README.md" -Raw).replace("#DESCRIPTION#", "$Description") | Set-Content -Path "$ThunderPath\README.md"

    $ZipPath = "$OutPath\$name.zip"
    Remove-Item -Path $ZipPath
    Compress-Archive -Path "$ThunderPath\*" -DestinationPath $ZipPath

    Remove-Item -Path $ThunderPath -Recurse  
    ii $OutPath
}

Copy-Item -Path "$TargetPath\$name.dll" -Destination "$OutPath\$name.dll"
ii $PluginFolder

Write-Host ""
Write-Host "---------------------------------------"
Pop-Location