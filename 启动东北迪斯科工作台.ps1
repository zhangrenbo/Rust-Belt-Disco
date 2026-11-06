param(
    [switch]$CheckOnly,
    [switch]$CreateShortcut
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new()
$projectRoot = $PSScriptRoot
$workbenchRoot = 'E:\blender\tool\html-dual-ai-workbench'
$electron = Join-Path $workbenchRoot 'node_modules\electron\dist\electron.exe'
$appIcon = Join-Path $projectRoot 'public\assets\character-select\game-icon-dong-crop-v1.ico'
$projectHtml = Join-Path $projectRoot 'index.html'
$previewUrl = 'http://127.0.0.1:5173/'
$appTitle = '东北迪斯科工作台'
$configId = '东北迪斯科'
$appId = 'com.rishi.workbench.northeast-disco'
$env:NO_PROXY = 'localhost,127.0.0.1'

try {
    foreach ($required in @($electron, $appIcon, $projectHtml, (Join-Path $workbenchRoot 'src\main.js'), 'E:\codex-launcher\panes\pane-codex.ps1', 'E:\claude-launcher\panes\pane-ds.ps1')) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "缺少工作台文件：$required" }
    }
    $node = Get-Command node.exe -ErrorAction Stop
    $nodeVersion = (& $node.Source --version).TrimStart('v')
    if ([version]$nodeVersion -lt [version]'22.12.0') { throw '需要 Node.js 22.12 或更高版本。' }

    # Check ownership before letting the shared shell reuse the fixed game port.
    # Never stop another service or switch ports: saves belong to this origin.
    $listener = Get-NetTCPConnection -State Listen -LocalPort 5173 -ErrorAction SilentlyContinue
    $serviceState = 'will-start'
    if ($listener) {
        Add-Type -AssemblyName System.Net.Http
        $handler = [Net.Http.HttpClientHandler]::new()
        $handler.UseProxy = $false
        $client = [Net.Http.HttpClient]::new($handler)
        $client.Timeout = [TimeSpan]::FromSeconds(5)
        try {
            $response = $client.GetAsync($previewUrl).GetAwaiter().GetResult()
            $html = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            if (-not $response.IsSuccessStatusCode -or $html -notmatch '<title>东北迪斯科</title>' -or $html -notmatch 'src/station/station.css') {
                throw '5173 端口正在被其他页面占用，未停止任何进程。请先确认占用服务。'
            }
            $serviceState = 'reuse-existing'
        } finally { $client.Dispose(); $handler.Dispose() }
    }

    if ($CreateShortcut) {
        $shortcutPath = Join-Path ([Environment]::GetFolderPath('Desktop')) ($appTitle + '.lnk')
        $wsh = New-Object -ComObject WScript.Shell
        $shortcut = $wsh.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $shortcut.Arguments = '-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "' + $PSCommandPath + '"'
        $shortcut.WorkingDirectory = $projectRoot
        $shortcut.IconLocation = $appIcon + ',0'
        $shortcut.Description = '东北迪斯科：实时游戏预览、四个 AI 终端、截图与 UI 指认'
        $shortcut.Save()
    }

    if ($CheckOnly) {
        [pscustomobject]@{
            title = $appTitle; project = $projectRoot; html = $projectHtml
            preview = $previewUrl; service = $serviceState; runtime = $electron
            appId = $appId; icon = $appIcon; config = Join-Path $env:APPDATA ('小游戏AI工作台\' + $configId)
            dependenciesReady = Test-Path -LiteralPath (Join-Path $projectRoot 'node_modules\vite\bin\vite.js')
        } | ConvertTo-Json
        exit 0
    }

    if (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'node_modules\vite\bin\vite.js'))) {
        Push-Location -LiteralPath $projectRoot
        try {
            & npm.cmd ci
            if ($LASTEXITCODE -ne 0) { throw '游戏依赖安装失败，请查看 npm 输出。' }
        } finally { Pop-Location }
    }

    # Electron owns only the service it starts; existing game services survive exit.
    # The workbench is a desktop app. Webpage automation uses work Chrome + CDP 9223.
    $launchArguments = @(
        $workbenchRoot
        "--app-id=$appId"
        "--app-icon=$appIcon"
        "--workbench-name=$appTitle"
        "--config-id=$configId"
        "--project=$projectRoot"
        "--html=$projectHtml"
        "--preview-url=$previewUrl"
        "--ready-url=$previewUrl"
        '--dev-script=dev'
    )
    & $electron @launchArguments
    if ($LASTEXITCODE -ne 0) { throw "工作台退出，代码：$LASTEXITCODE" }
} catch {
    if ($CheckOnly) { throw }
    Add-Type -AssemblyName System.Windows.Forms
    [void][Windows.Forms.MessageBox]::Show($_.Exception.Message, '东北迪斯科工作台启动失败', 'OK', 'Error')
    exit 1
}
