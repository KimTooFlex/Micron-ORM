param($installPath, $toolsPath, $package)

#$asm=[Reflection.Assembly]::LoadFile("$(Get-Location)\Micron.dll")
#[Micron.VsSettingsImport]::ImportSettings()

Import-Module (Join-Path $toolsPath Micron.psm1)