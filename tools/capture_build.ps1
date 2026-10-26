# Launches a slice build with the hands-free autopilot, has the game save its own frames (FrameRecorder, "-record"),
# stitches them into the evidence mp4 with ffmpeg, and keeps the player log next to the video.
# The game records itself, so it can run behind other windows: nothing else on the desktop ends up in the video,
# and it never takes focus or the mouse.
# Usage: & tools/capture_build.ps1 -Slice 4      (-Demo "-autopilot-gate3" for an older gate's demo)
param([string]$Slice = "0", [string]$Demo = "-autopilot", [int]$MaxSeconds = 900)

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "builds\slice_$Slice\FedUp.exe"
$evidence = Join-Path $root "evidence"
$video = Join-Path $evidence "slice_$Slice.mp4"
$log = Join-Path $evidence "slice_${Slice}_build.log"
$frames = Join-Path $env:TEMP "fedup_frames_slice_$Slice"
New-Item -ItemType Directory -Force $evidence | Out-Null
Remove-Item $video, $log -ErrorAction SilentlyContinue
if (Test-Path $frames) { Remove-Item -Recurse -Force $frames }

$started = Get-Date
$game = Start-Process $exe -ArgumentList $Demo, "-record", "`"$frames`"", "-logFile", "`"$log`"", "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720" -PassThru
# the autopilot quits the game when it's done
if (-not $game.WaitForExit($MaxSeconds * 1000)) { Stop-Process -Id $game.Id; Write-Output "TIMEOUT: stopped the game after $MaxSeconds s" }
$secs = [int]((Get-Date) - $started).TotalSeconds

$count = (Get-ChildItem $frames -Filter "f_*.jpg" -ErrorAction SilentlyContinue).Count
if ($count -eq 0) { Write-Output "FAIL: the game saved no frames"; exit 1 }
& ffmpeg -hide_banner -loglevel error -y -framerate 30 -i (Join-Path $frames "f_%05d.jpg") -c:v libx264 -preset veryfast -crf 20 -pix_fmt yuv420p $video
Remove-Item -Recurse -Force $frames

Write-Output "ran $secs s, saved $count frames = $([math]::Round($count / 30, 1)) s of game -> $video ($([int]((Get-Item $video).Length / 1KB)) KB)"
Write-Output "--- autopilot / error lines from the player log ---"
Select-String -Path $log -Pattern "AUTOPILOT|RECORDING|Exception|Error" | ForEach-Object { $_.Line } | Select-Object -First 40
