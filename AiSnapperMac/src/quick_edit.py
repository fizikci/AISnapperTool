from PyQt6.QtWidgets import QWidget, QTextEdit, QPushButton, QVBoxLayout, QHBoxLayout
from PyQt6.QtCore import Qt
from pynput.keyboard import Controller, Key
from .openai_client import OpenAIClient
import time

class QuickEdit(QWidget):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Quick Edit")
        self.setFixedSize(560, 360)
        self.editor = QTextEdit()
        self.btn_expand = QPushButton("Expand")
        self.btn_summarize = QPushButton("Summarize")
        self.btn_rephrase_pro = QPushButton("Rephrase (Professional)")
        self.btn_rephrase_casual = QPushButton("Rephrase (Casual)")
        self.btn_accept = QPushButton("Accept")
        self.btn_cancel = QPushButton("Cancel")

        row = QHBoxLayout()
        for b in [self.btn_expand, self.btn_summarize, self.btn_rephrase_pro, self.btn_rephrase_casual, self.btn_accept, self.btn_cancel]:
            row.addWidget(b)

        layout = QVBoxLayout()
        layout.addWidget(self.editor)
        layout.addLayout(row)
        self.setLayout(layout)

        self._client = OpenAIClient()
        self._kb = Controller()

        self.btn_expand.clicked.connect(lambda: self._transform("Expand and elaborate while keeping the original meaning."))
        self.btn_summarize.clicked.connect(lambda: self._transform("Summarize concisely."))
        self.btn_rephrase_pro.clicked.connect(lambda: self._transform("Rephrase to a professional tone."))
        self.btn_rephrase_casual.clicked.connect(lambda: self._transform("Rephrase to a casual, friendly tone."))
        self.btn_accept.clicked.connect(self._accept)
        self.btn_cancel.clicked.connect(self.close)

    def show_with_selection(self):
        # Synthesize Cmd+C to copy selection, then paste into editor
        self._kb.press(Key.cmd)
        self._kb.press('c')
        self._kb.release('c')
        self._kb.release(Key.cmd)
        time.sleep(0.2)
        # Paste into our editor
        self._kb.press(Key.cmd)
        self._kb.press('v')
        self._kb.release('v')
        self._kb.release(Key.cmd)
        self.show()
        self.activateWindow()

    def _transform(self, instruction: str):
        text = self.editor.toPlainText()
        msgs = [
            {"role": "system", "content": [ {"type": "text", "text": "You are a writing assistant. Only return the transformed text without quotes."} ]},
            {"role": "user", "content": [ {"type": "text", "text": f"Instruction: {instruction}\n\nText:\n{text}"} ]}
        ]
        try:
            result = self._client.ask(msgs)
            if result:
                self.editor.setPlainText(result.strip())
        except Exception as e:
            self.editor.setPlainText(f"Error: {e}")

    def _accept(self):
        text = self.editor.toPlainText()
        # Copy text
        from .clipboard_mac import set_clipboard_text
        if set_clipboard_text(text):
            # Paste into target app via Cmd+V
            self._kb.press(Key.cmd)
            self._kb.press('v')
            self._kb.release('v')
            self._kb.release(Key.cmd)
        self.close()
