import sys
import signal
from PyQt6.QtWidgets import QApplication
from .chat_window import ChatWindow
from .quick_edit import QuickEdit
from .hotkeys import HotkeyManager


def main():
    app = QApplication(sys.argv)
    win = ChatWindow()
    win.show()

    qe = QuickEdit()

    hk = HotkeyManager()
    hk.start(on_capture=win.on_capture, on_edit=qe.show_with_selection)

    signal.signal(signal.SIGINT, signal.SIG_DFL)
    sys.exit(app.exec())


if __name__ == '__main__':
    main()
