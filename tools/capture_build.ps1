# Launches a slice build with the hands-free autopilot, records its window with ffmpeg,
# and copies the player log next to the video.
# Usage: powershell -ExecutionPolicy Bypass -File tools/capture_build.ps1 -Slice 0
param([int]$Slice = 0, [int]$MaxSeconds = 90)

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "builds\slice_$Slice\FedUp.exe"
$evidence = Join-Path $root "evidence"
$video = Join-Path $evidence "slice_$Slice.mp4"
$log = Join-Path $evidence "slice_${Slice}_build.log"
New-Item -ItemType Directory -Force $evidence | Out-Null
Remove-Item $video, $log -ErrorAction SilentlyContinue

$game = Start-Process $exe -ArgumentList "-autopilot", "-logFile", "`"$log`"", "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720" -PassThru

# wait for the game window
$deadline = (Get-Date).AddSeconds(30)
while ((Get-Date) -lt $deadline) {
    $game.Refresh()
    if ($game.MainWindowHandle -ne 0) { break }
    Start-Sleep -Milliseconds 250
}
if ($game.MainWindowHandle -eq 0) { Write-Output "FAIL: game window never appeared"; exit 1 }
$title = $game.MainWindowTitle

# GDI window capture can't see DirectX frames, so record the desktop region the window occupies, with the window in front.
Add-Type @"
using System; using System.Runtime.InteropServices;
public static class Win {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
}
"@
[Win]::SetProcessDPIAware() | Out-Null
Start-Sleep -Milliseconds 800
[Win]::SetForegroundWindow($game.MainWindowHandle) | Out-Null
$r = New-Object Win+RECT; [Win]::GetClientRect($game.MainWindowHandle, [ref]$r) | Out-Null
$p = New-Object Win+POINT; [Win]::ClientToScreen($game.MainWindowHandle, [ref]$p) | Out-Null
$w = ($r.R - $r.L) - (($r.R - $r.L) % 2); $h = ($r.B - $r.T) - (($r.B - $r.T) % 2)

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "ffmpeg"
$psi.Arguments = "-hide_banner -loglevel error -y -f gdigrab -framerate 30 -draw_mouse 0 -offset_x $($p.X) -offset_y $($p.Y) -video_size ${w}x${h} -i desktop -t $MaxSeconds -c:v libx264 -preset veryfast -crf 20 -pix_fmt yuv420p `"$video`""
$psi.RedirectStandardInput = $true
$psi.UseShellExecute = $false
$ff = [System.Diagnostics.Process]::Start($psi)
$started = Get-Date

# stop recording when the autopilot reports it is done (the game quits ~1 s later)
while (-not $game.HasExited -and ((Get-Date) - $started).TotalSeconds -lt $MaxSeconds) {
    if ((Test-Path $log) -and (Select-String -Path $log -Pattern "AUTOPILOT DONE" -Quiet)) { break }
    Start-Sleep -Milliseconds 250
}
if (-not $ff.HasExited) { $ff.StandardInput.Write("q"); $ff.StandardInput.Close() }
$ff.WaitForExit(15000) | Out-Null
$game.WaitForExit(10000) | Out-Null
if (-not $game.HasExited) { Stop-Process -Id $game.Id }

$secs = [int]((Get-Date) - $started).TotalSeconds
Write-Output "window title: $title"
Write-Output "recorded ~$secs s -> $video ($([int]((Get-Item $video).Length / 1KB)) KB)"
Write-Output "--- autopilot / error lines from the player log ---"
Select-String -Path $log -Pattern "AUTOPILOT|Exception|Error" | ForEach-Object { $_.Line } | Select-Object -First 40
