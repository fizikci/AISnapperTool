from typing import Optional
try:
    from AppKit import NSWorkspace, NSRunningApplication
except Exception:
    NSWorkspace = None
    NSRunningApplication = None


def get_frontmost_pid() -> Optional[int]:
    try:
        if NSWorkspace is None:
            return None
        app = NSWorkspace.sharedWorkspace().frontmostApplication()
        return int(app.processIdentifier()) if app else None
    except Exception:
        return None


def activate_app(pid: int) -> bool:
    try:
        if NSRunningApplication is None:
            return False
        app = NSRunningApplication.runningApplicationWithProcessIdentifier_(pid)
        if app:
            return bool(app.activateWithOptions_(1 << 1))  # NSApplicationActivateIgnoringOtherApps
        return False
    except Exception:
        return False
