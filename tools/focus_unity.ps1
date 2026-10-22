# Brings the Unity Editor window to the front. Unity only imports and compiles changed files while focused.
Add-Type @"
using System; using System.Runtime.InteropServices;
public static class UFocus {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
  [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
}
"@
$p = Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $p) { "Unity editor not running"; exit 1 }
$h = $p.MainWindowHandle
if ([UFocus]::IsIconic($h)) { [UFocus]::ShowWindow($h, 9) | Out-Null }
# a harmless ALT tap lets this process take the foreground (Windows foreground-lock rule)
[UFocus]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [UFocus]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
"focused=" + [UFocus]::SetForegroundWindow($h)
