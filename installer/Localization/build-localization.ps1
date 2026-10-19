param(
    [Parameter(Mandatory=$true)][string]$WixDll,
    [Parameter(Mandatory=$true)][string]$WixUI,
    [Parameter(Mandatory=$true)][string]$AppSource,
    [Parameter(Mandatory=$true)][string]$MaintenanceSource,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$taskInstallerRoot = Join-Path $taskRoot 'installer'
$taskTables = @('Dialog', 'Control', 'UIText', 'LaunchCondition')
$taskCultures = @('en-US', 'ja-JP', 'zh-CN', 'zh-TW')
$taskIds = @('1033', '1041', '2052', '1028')
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$taskSources = @('MinamotoBoard.wxs', 'InstallerUI.wxs', 'StandardUI\VerifyReadyDlg.wxs', 'StandardUI\ExitDialog.wxs') | ForEach-Object { Join-Path $taskInstallerRoot $_ }
$taskWixArguments = @('--roll-forward', 'Major', $WixDll, 'build') + $taskSources + @('-ext', $WixUI, '-arch', 'x64', '-d', "AppSource=$AppSource", '-d', "DocSource=$taskRoot", '-d', "MaintenanceSource=$MaintenanceSource")

function ConvertTo-CppString([string]$text) {
    if ([string]::IsNullOrEmpty($text)) { return '0' }
    return 'L"' + $text.Replace('\', '\\').Replace('"', '\"').Replace("`r", '\r').Replace("`n", '\n').Replace("`t", '\t') + '"'
}

$taskHeader = New-Object Text.StringBuilder
[void]$taskHeader.AppendLine('// Generated from WiX 5.0.2 native UI localization. Copyright .NET Foundation and contributors; MS-RL.')
$taskCom = New-Object -ComObject WindowsInstaller.Installer
try {
    for ($taskLanguage = 0; $taskLanguage -lt $taskCultures.Count; $taskLanguage++) {
        $taskCulture = $taskCultures[$taskLanguage]
        $taskDraft = Join-Path $OutputDirectory "$taskCulture.msi"
        & dotnet @taskWixArguments -culture $taskCulture -loc (Join-Path $PSScriptRoot "$taskCulture.wxl") -intermediateFolder (Join-Path $OutputDirectory "obj-$taskCulture") -o $taskDraft
        if ($LASTEXITCODE -ne 0) { throw "Localized MSI compilation failed: $taskCulture" }
        $taskDatabase = $taskCom.OpenDatabase($taskDraft, 0)
        $taskTableDefinitions = New-Object 'Collections.Generic.List[string]'
        try {
            foreach ($taskTable in $taskTables) {
                $taskQuery = 'SELECT * FROM `' + $taskTable + '`'
                $taskView = $taskDatabase.OpenView($taskQuery)
                [void]$taskView.Execute()
                $taskTypes = $taskView.ColumnInfo(1)
                $taskColumns = $taskTypes.GetType().InvokeMember('FieldCount', [Reflection.BindingFlags]::GetProperty, $null, $taskTypes, $null)
                $taskIntegerMask = 0
                for ($taskColumn=1; $taskColumn -le $taskColumns; $taskColumn++) {
                    if ($taskTypes.StringData($taskColumn) -match '^[iI]') { $taskIntegerMask = $taskIntegerMask -bor (1 -shl ($taskColumn - 1)) }
                }
                [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskTypes)
                $taskCount = 0
                [void]$taskHeader.AppendLine("static const LocalizedRow Rows_${taskLanguage}_${taskTable}[] = {")
                while ($taskRecord = $taskView.Fetch()) {
                    $taskCells = for ($taskColumn=1; $taskColumn -le $taskColumns; $taskColumn++) { ConvertTo-CppString $taskRecord.StringData($taskColumn) }
                    [void]$taskHeader.AppendLine('    {{' + ($taskCells -join ', ') + '}},')
                    $taskCount++
                    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskRecord)
                }
                [void]$taskView.Close()
                [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskView)
                [void]$taskHeader.AppendLine('};')
                $taskTableDefinitions.Add('    {' + (ConvertTo-CppString $taskQuery) + ", $taskColumns, $taskIntegerMask, Rows_${taskLanguage}_${taskTable}, $taskCount},")
            }
        } finally { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskDatabase) }
        [void]$taskHeader.AppendLine("static const LocalizedTable Tables_${taskLanguage}[] = {")
        foreach ($taskDefinition in $taskTableDefinitions) { [void]$taskHeader.AppendLine($taskDefinition) }
        [void]$taskHeader.AppendLine('};')
    }
} finally { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskCom) }
[void]$taskHeader.AppendLine('static const LocalizedLanguage Languages[] = {')
for ($taskLanguage=0; $taskLanguage -lt $taskCultures.Count; $taskLanguage++) {
    [void]$taskHeader.AppendLine('    {L"' + $taskIds[$taskLanguage] + '", Tables_' + $taskLanguage + ', ' + $taskTables.Count + '},')
}
[void]$taskHeader.AppendLine('};')
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'LanguageData.h'), $taskHeader.ToString(), [Text.UTF8Encoding]::new($false))

# No SDK headers/CRT are needed: the source declares the small Windows API surface it imports.
$taskVswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$taskCompiler = & $taskVswhere -latest -products '*' -find 'VC\Tools\MSVC\**\bin\Hostx64\x64\cl.exe' | Select-Object -First 1
if (-not $taskCompiler) { throw 'Visual Studio x64 C++ compiler is required for native MSI language selection.' }
$taskLinker = Join-Path (Split-Path -Parent $taskCompiler) 'link.exe'
foreach ($taskLibrary in @('kernel32', 'msi')) {
    & $taskLinker /lib /nologo /machine:x64 "/def:$(Join-Path $PSScriptRoot "$taskLibrary.def")" "/out:$(Join-Path $OutputDirectory "$taskLibrary.lib")"
    if ($LASTEXITCODE -ne 0) { throw "Cannot generate Windows import library: $taskLibrary" }
}
$taskObject = Join-Path $OutputDirectory 'SelectLanguage.obj'
$taskDll = Join-Path $OutputDirectory 'BoardLocalization.dll'
& $taskCompiler /nologo /c /utf-8 /W4 /WX /O1 /GS- /Zl "/I$OutputDirectory" "/Fo$taskObject" (Join-Path $PSScriptRoot 'SelectLanguage.cpp')
if ($LASTEXITCODE -ne 0) { throw 'Native MSI localization compilation failed.' }
& $taskLinker /nologo /dll /noentry /nodefaultlib /machine:x64 /dynamicbase /nxcompat "/out:$taskDll" $taskObject (Join-Path $OutputDirectory 'kernel32.lib') (Join-Path $OutputDirectory 'msi.lib')
if ($LASTEXITCODE -ne 0) { throw 'Native MSI localization link failed.' }

& dotnet @taskWixArguments -culture en-US -loc (Join-Path $PSScriptRoot 'en-US.wxl') -d "LocalizationSource=$taskDll" -intermediateFolder (Join-Path $OutputDirectory 'obj-final') -o (Join-Path (Split-Path -Parent $OutputDirectory) 'MinamotoBoard.msi')
if ($LASTEXITCODE -ne 0) { throw 'Multilingual MSI compilation failed.' }

# UI rows are inserted temporarily by SelectBoardLanguage before the first dialog/launch condition.
# Keep the standard table schemas, navigation and payload; never persist a user's language choice in the MSI.
$taskCom = New-Object -ComObject WindowsInstaller.Installer
$taskDatabase = $taskCom.OpenDatabase((Join-Path (Split-Path -Parent $OutputDirectory) 'MinamotoBoard.msi'), 1)
try {
    foreach ($taskTable in $taskTables) {
        $taskView = $taskDatabase.OpenView('DELETE FROM `' + $taskTable + '`')
        [void]$taskView.Execute(); [void]$taskView.Close()
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskView)
    }
    $taskSummary = $taskDatabase.SummaryInformation(1)
    $taskSummary.Property(7) = 'x64;0'
    [void]$taskSummary.Persist()
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskSummary)
    [void]$taskDatabase.Commit()
} finally {
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskDatabase)
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($taskCom)
}
