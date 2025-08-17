from pynput import keyboard
from typing import Callable

class HotkeyManager:
    def __init__(self):
        self._capture_cb: Callable[[], None] | None = None
        self._edit_cb: Callable[[], None] | None = None
        self._listener: keyboard.GlobalHotKeys | None = None

    def start(self, on_capture: Callable[[], None], on_edit: Callable[[], None]):
        self._capture_cb = on_capture
        self._edit_cb = on_edit
        self._listener = keyboard.GlobalHotKeys({
            '<cmd>+<shift>+c': self._capture,
            '<cmd>+<shift>+e': self._edit,
        })
        self._listener.start()

    def stop(self):
        if self._listener:
            self._listener.stop()
            self._listener = None

    def _capture(self):
        if self._capture_cb:
            self._capture_cb()

    def _edit(self):
        if self._edit_cb:
            self._edit_cb()
