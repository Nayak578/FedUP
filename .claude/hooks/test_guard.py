#!/usr/bin/env python3
"""Fed Up test guard: a Claude Code hook (Python stdlib only).

Rules (see CLAUDE.md):
  * New test files may always be created (the "red" step of red -> green).
  * Existing tests may only GROW (new lines added, nothing removed or changed)
    unless game code under _Project/Scripts changed earlier in this session.
  * Shell and Unity-tool writes that target test files are blocked; tests are
    edited only through Write/Edit, where this guard can inspect the change.
  * At session end, if existing tests changed while no game code changed,
    stopping is blocked and the report is printed.

Modes:  session-start | pre | stop      (hook JSON arrives on stdin)
Exit 2 = block, with the reason on stderr.
"""
import hashlib
import json
import os
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
STATE_DIR = Path(__file__).resolve().parent / "state"
TEST_RE = re.compile(r"(^|/)fedup/assets/_project/tests/.+\.cs$")
SRC_RE = re.compile(r"(^|/)fedup/assets/_project/scripts/.+\.cs$")
TEST_HINT = re.compile(r"_project[\\/]+tests", re.I)
SHELL_WRITE = re.compile(
    r"(>>?|\btee\b|\bsed\s+-i|\bperl\s+-\w*i|\bawk\s+-i|\bcp\b|\bmv\b|\brm\b|\bdel\b|\btruncate\b|\bdd\b|"
    r"\bgit\s+(checkout|restore|rm|mv)\b|Set-Content|Add-Content|Out-File|Remove-Item|Move-Item|Copy-Item|"
    r"Clear-Content|New-Item|WriteAll|open\(|\.write\()",
    re.I,
)
UNITY_CODE_WRITE = re.compile(
    r"File\.(Write|Delete|Move|Copy|Replace|Append|Create)|WriteAll|StreamWriter|FileStream|Directory\.(Delete|Move)|"
    r"FileUtil\.|AssetDatabase\.(DeleteAsset|MoveAsset|CopyAsset|CreateAsset|RenameAsset|MoveAssetToTrash|DeleteAssets)",
    re.I,
)
UNITY_WRITE_TOOLS = ("create_script", "delete_script", "apply_text_edits", "script_apply_edits",
                     "manage_script", "manage_asset", "execute_code", "batch_execute")


def norm(p):
    if not p:
        return ""
    p = str(p).replace("\\", "/")
    try:
        p = str(Path(p).resolve().relative_to(ROOT)).replace("\\", "/")
    except Exception:
        pass
    return p.lower()


def is_test(p):
    return bool(TEST_RE.search(norm(p)))


def is_src(p):
    return bool(SRC_RE.search(norm(p)))


def tracked_files():
    out = {}
    base = ROOT / "FedUP" / "Assets" / "_Project"
    for sub in ("Tests", "Scripts"):
        d = base / sub
        if d.exists():
            for f in d.rglob("*.cs"):
                out[norm(f)] = hashlib.sha256(f.read_bytes()).hexdigest()
    return out


def state_path(sid):
    return STATE_DIR / f"{re.sub(r'[^A-Za-z0-9_-]', '_', sid or 'default')}.json"


def load_state(sid):
    p = state_path(sid)
    if p.exists():
        return json.loads(p.read_text(encoding="utf-8"))
    st = {"baseline": tracked_files(), "source_changed": False}
    save_state(sid, st)
    return st


def save_state(sid, st):
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    state_path(sid).write_text(json.dumps(st, indent=1), encoding="utf-8")


def block(msg):
    sys.stderr.write("TEST GUARD BLOCKED: " + msg + "\n")
    sys.exit(2)


def only_adds(old, new):
    """True if every non-blank line of `old` still appears in `new`, in order."""
    it = iter(l.strip() for l in new.splitlines())
    for line in (l.strip() for l in old.splitlines()):
        if not line:
            continue
        if not any(line == n for n in it):
            return False
    return True


def pre(data, st, sid):
    tool = data.get("tool_name", "")
    ti = data.get("tool_input", {}) or {}

    if tool in ("Write", "Edit", "MultiEdit", "NotebookEdit"):
        path = ti.get("file_path") or ti.get("notebook_path") or ""
        if is_src(path):
            st["source_changed"] = True
            save_state(sid, st)
            return
        if not is_test(path):
            return
        full = Path(path) if os.path.isabs(path) else ROOT / path
        if not full.exists():
            return  # brand-new test file: allowed (red step)
        if st.get("source_changed"):
            return
        if tool == "Write":
            if only_adds(full.read_text(encoding="utf-8", errors="replace"), ti.get("content", "")):
                return
            block(f"{path} would lose or change existing test lines, and no game code under "
                  "_Project/Scripts has changed this session. Change the code first, or only add tests.")
        edits = ti.get("edits") or [{"old_string": ti.get("old_string", ""), "new_string": ti.get("new_string", "")}]
        for e in edits:
            if e.get("old_string", "") not in e.get("new_string", ""):
                block(f"{path}: this edit removes or rewrites existing test text, and no game code under "
                      "_Project/Scripts has changed this session. Only additions are allowed until code changes.")
        return

    if tool in ("Bash", "PowerShell"):
        cmd = ti.get("command", "")
        if TEST_HINT.search(cmd) and SHELL_WRITE.search(cmd):
            if st.get("source_changed"):
                return  # same rule as Write/Edit: once game code changed, tests may change too
            block("shell command appears to write, move or delete files under _Project/Tests, and no game "
                  "code under _Project/Scripts has changed this session. Edit tests with Write/Edit (additions only) "
                  "or change the code first.")
        return

    if tool.startswith("mcp__unityMCP__") and tool.split("__")[-1] in UNITY_WRITE_TOOLS:
        blob = json.dumps(ti)
        if TEST_HINT.search(blob):
            name = tool.split("__")[-1]
            if name == "create_script":
                p = (ti.get("path") or "")
                cand = ROOT / "FedUP" / (p if p.lower().startswith("assets") else "Assets/" + p)
                if not cand.exists():
                    return
            if name in ("execute_code", "batch_execute") and not UNITY_CODE_WRITE.search(blob):
                return  # read-only C#: inspecting tests is fine
            if name == "manage_asset" and ti.get("action") in ("search", "get_info", "get_components"):
                return
            block(f"Unity tool '{name}' targets files under _Project/Tests. "
                  "Edit tests with the Write/Edit tools so the guard can check them.")


def stop(data, st):
    now = tracked_files()
    base = st.get("baseline", {})
    tests_changed = sorted(p for p, h in base.items() if TEST_RE.search(p) and now.get(p) != h)
    tests_added = sorted(p for p in now if TEST_RE.search(p) and p not in base)
    src_changed = sorted(p for p in set(base) | set(now) if SRC_RE.search(p) and base.get(p) != now.get(p))
    report = {"existing_tests_changed_or_deleted": tests_changed, "tests_added": tests_added,
              "game_code_changed": src_changed}
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    (STATE_DIR / "last_report.json").write_text(json.dumps(report, indent=1), encoding="utf-8")
    if tests_changed and not src_changed:
        if data.get("stop_hook_active"):
            sys.stderr.write("TEST GUARD WARNING (second stop, allowing): " + json.dumps(report) + "\n")
            return
        block("existing tests changed this session but no game code did: " + ", ".join(tests_changed)
              + ". Restore them or explain the change to the designer.")


def main():
    mode = sys.argv[1] if len(sys.argv) > 1 else "pre"
    raw = sys.stdin.read() if not sys.stdin.isatty() else ""
    data = json.loads(raw) if raw.strip() else {}
    sid = data.get("session_id", "default")
    if mode == "session-start":
        save_state(sid, {"baseline": tracked_files(), "source_changed": False})
        return
    st = load_state(sid)
    if mode == "pre":
        pre(data, st, sid)
    elif mode == "stop":
        stop(data, st)


if __name__ == "__main__":
    try:
        main()
    except SystemExit:
        raise
    except Exception as e:  # fail closed: a broken guard must not wave changes through
        block(f"guard error ({type(e).__name__}: {e}); fix .claude/hooks/test_guard.py before continuing")
