$asm = [System.Reflection.Assembly]::LoadFrom('c:\TCGCardShopModWork\TCG Card Shop Simulator\BepInEx\plugins\ArtExpander\ArtExpander.dll')
$plugin = $asm.GetType('ArtExpander.Plugin')
Write-Output 'Plugin fields:'
foreach ($f in $plugin.GetFields([System.Reflection.BindingFlags]'Public,NonPublic,Static,Instance')) {
    Write-Output ("  {0} : {1}" -f $f.Name, $f.FieldType.FullName)
}
$patch = $asm.GetType('ArtExpander.Patches.CardUISetCardPatch')
Write-Output 'Postfix methods:'
foreach ($m in $patch.GetMethods([System.Reflection.BindingFlags]'Public,NonPublic,Static,Instance')) {
    if ($m.Name -ne 'Postfix') { continue }
    $ps = @($m.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" })
    Write-Output ("  Postfix({0})" -f ($ps -join ', '))
}
