param(
    [Parameter(Mandatory)]
    [System.String]$Version,

    [Parameter(Mandatory)]
    [ValidateSet('Debug','Release','MultiplayerDebug')]
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

# Set some directories
$PluginFolder = "C:\Users\Daniel\AppData\Roaming\r2modmanPlus-local\ROUNDS\profiles\ModdedTesting\BepInEx\plugins\Unknown-$name"

# Go
Write-Host ""
Write-Host "Making for $Target from $TargetPath"

# Debug copies the dll to ROUNDS
if ($Target.Equals("Debug")) {
    Write-Host "Updating local installation in r2modman"
    
    # $plug = New-Item -Type Directory -Path "$RoundsPath\BepInEx\plugins\$name" -Force
    Write-Host "Copy $TargetAssembly to $PluginFolder"
    Copy-Item -Path "$TargetPath\$name.dll" -Destination $PluginFolder -Force
}

# Release package for ThunderStore
if($Target.Equals("Release")) {
    $package = "$ProjectPath\release"

    Write-Host "Packaging for ThunderStore $name $Version"
    New-Item -Type Directory -Path "$package\Thunderstore" -Force
    $thunder = New-Item -Type Directory -Path "$package\Thunderstore\package"
    $thunder.CreateSubdirectory('plugins')
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$PluginFolder"
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$thunder\plugins\"
    Copy-Item -Path "$ProjectPath\README.md" -Destination "$thunder\README.md"
    Copy-Item -Path "$ProjectPath\manifest.json" -Destination "$thunder\manifest.json"
    Copy-Item -Path "$ProjectPath\icon.png" -Destination "$thunder\icon.png"

    #((Get-Content -path "$thunder\manifest.json" -Raw) -replace "#VERSION#", "$Version") | Set-Content -Path "$thunder\manifest.json"
    #((Get-Content -path "$thunder\manifest.json" -Raw) -replace "#NAME#", "$name") | Set-Content -Path "$thunder\manifest.json"
    #$website = "https://github.com/otDan/" + "$name"
    #((Get-Content -path "$thunder\manifest.json" -Raw) -replace "#WEBSITE_URL#", "$website") | Set-Content -Path "$thunder\manifest.json"

    Remove-Item -Path "$package\Thunderstore\$name.zip" -Force

    Compress-Archive -Path "$thunder\*" -DestinationPath "$package\Thunderstore\$name.zip"
    $thunder.Delete($true)

    $package = "$ProjectPath\release"
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$package\$name.dll"
}

Pop-Location