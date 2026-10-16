param([string]$Path = 'artifacts\installer\MinamotoBoard.msi')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskMsi = if ([IO.Path]::IsPathRooted($Path)) { $Path } else { Join-Path $taskRoot $Path }
$taskInstaller = New-Object -ComObject WindowsInstaller.Installer
$taskDatabase = $taskInstaller.OpenDatabase($taskMsi, 0)
function Read-MsiRows([string]$query, [int]$columns) {
    $taskView = $taskDatabase.OpenView($query)
    [void]$taskView.Execute()
    try {
        while ($taskRecord = $taskView.Fetch()) {
            $taskValues = @()
            for ($taskIndex=1; $taskIndex -le $columns; $taskIndex++) { $taskValues += $taskRecord.StringData($taskIndex) }
            ,$taskValues
        }
    } finally { [void]$taskView.Close() }
}
function Assert-Msi([bool]$ok, [string]$message) { if (-not $ok) { throw $message } }
$taskProperties = @{}
Read-MsiRows 'SELECT `Property`, `Value` FROM `Property`' 2 | ForEach-Object { $taskProperties[$_[0]]=$_[1] }
Assert-Msi ($taskProperties['ProductName'] -eq 'MinamotoBoard') 'Incorrect product name'
Assert-Msi ($taskProperties['Manufacturer'] -eq 'MinamotoDev') 'Incorrect manufacturer'
Assert-Msi ($taskProperties['ALLUSERS'] -eq '1') 'Installer must be per-machine'
$taskDirectories = @{}
Read-MsiRows 'SELECT `Directory`, `Directory_Parent`, `DefaultDir` FROM `Directory`' 3 | ForEach-Object { $taskDirectories[$_[0]] = $_ }
Assert-Msi ($taskDirectories['INSTALLFOLDER'][1] -eq 'MinamotoDevFolder' -and $taskDirectories['INSTALLFOLDER'][2] -eq 'Board') 'Incorrect Board directory'
Assert-Msi ($taskDirectories['MinamotoDevFolder'][1] -eq 'ProgramFiles64Folder' -and $taskDirectories['MinamotoDevFolder'][2] -match '(^|\|)MinamotoDev$') 'Incorrect Program Files vendor directory'
$taskFiles = @(Read-MsiRows 'SELECT `FileName` FROM `File`' 1 | ForEach-Object { $_[0].Split('|')[-1] })
foreach ($taskExpected in @('MinamotoBoard.exe','MinamotoBoard.exe.config','MinamotoBoard.PluginApi.dll','README.txt','PLUGINS.md')) {
    Assert-Msi ($taskFiles -contains $taskExpected) "Missing packaged file: $taskExpected"
}
Assert-Msi ($taskFiles.Count -eq 5) 'Unexpected payload, e.g. test fixtures or sample plugin'
Assert-Msi ($taskDatabase.SummaryInformation(0).Property(7) -match '^x64;') 'MSI must target x64'
$taskSession = $taskInstaller.OpenPackage($taskMsi, 1)
foreach ($taskCase in @(@('#528039',0),@('#528040',1),@('#534296',1),@('',0))) {
    $taskSession.Property('NETFXRELEASE') = $taskCase[0]
    Assert-Msi ($taskSession.EvaluateCondition('NETFXRELEASE >= "#528040"') -eq $taskCase[1]) 'Incorrect .NET condition'
}
foreach ($taskCase in @(@('19044',0),@('19045',1),@('26300',1),@('',0))) {
    $taskSession.Property('OSBUILD') = $taskCase[0]
    Assert-Msi ($taskSession.EvaluateCondition('OSBUILD >= 19045') -eq $taskCase[1]) 'Incorrect OS build condition'
}
$taskShortcuts = @(Read-MsiRows 'SELECT `Name` FROM `Shortcut`' 1)
Assert-Msi ($taskShortcuts.Count -eq 1) 'Missing Start menu shortcut'
$taskTables = @(Read-MsiRows 'SELECT `Name` FROM `_Tables`' 1 | ForEach-Object { $_[0] })
if ($taskTables -contains 'CustomAction') {
    $taskActions = @(Read-MsiRows 'SELECT `Action` FROM `CustomAction`' 1)
    Assert-Msi ($taskActions.Count -eq 0) 'Custom actions are not expected'
}
Write-Output 'PASS: MSI metadata, x64, default directory, 5 payload files, Start menu shortcut, no custom actions, prerequisite boundary conditions.'
