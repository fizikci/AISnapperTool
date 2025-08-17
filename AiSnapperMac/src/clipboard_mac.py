import platform
import time
from typing import Optional

try:
    import AppKit
except Exception:  # noqa: S110
    AppKit = None

try:
    import Quartz
except Exception:  # noqa: S110
    Quartz = None

import pyperclip


def get_selected_text(timeout: float = 0.2) -> Optional[str]:
    # Best effort: synth Cmd+C is responsibility of caller; here we just read clipboard safely
    prev = None
    try:
        prev = pyperclip.paste()
    except Exception:
        prev = None

    time.sleep(timeout)
    try:
        txt = pyperclip.paste()
    except Exception:
        txt = None

    # Do not try to restore clipboard to avoid permission prompts; return what's there
    if txt and isinstance(txt, str) and txt.strip():
        return txt
    return None


def set_clipboard_text(text: str) -> bool:
    try:
        pyperclip.copy(text)
        return True
    except Exception:
        return False
