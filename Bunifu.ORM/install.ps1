#[System.Reflection.Assembly]::LoadFile("D:\coding\Bunifu.ORM\Bunifu.ORM\bin\Debug\Bunifu.ORM.dll")
#[Bunifu.ORM.VsSettingsImport]::TailorContractLibrary($web, $libraryName, $listTemplate)
$asm=[Reflection.Assembly]::LoadFile("D:\coding\Bunifu.ORM\Bunifu.ORM\bin\Debug\Bunifu.ORM.dll")
[Bunifu.ORM.VsSettingsImport]::ImportSettings()