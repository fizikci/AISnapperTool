from PyQt6.QtWidgets import QWidget, QTextEdit, QPushButton, QLabel, QVBoxLayout, QHBoxLayout, QFileDialog, QApplication
from PyQt6.QtGui import QPixmap, QImage
from PyQt6.QtCore import Qt, QThread, pyqtSignal
from typing import Optional, List
from io import BytesIO
from PIL import Image
from .openai_client import OpenAIClient, image_message
from .selection_overlay import SelectionOverlay
from .screen import capture_full

class AskThread(QThread):
    delta = pyqtSignal(str)
    done = pyqtSignal()
    error = pyqtSignal(str)

    def __init__(self, client: OpenAIClient, messages: List[dict]):
        super().__init__()
        self.client = client
        self.messages = messages

    def run(self):
        try:
            self.client.ask_stream(self.messages, lambda d: self.delta.emit(d))
            self.done.emit()
        except Exception as e:
            self.error.emit(str(e))

class ChatWindow(QWidget):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("AiSnapper Chat")
        self.setMinimumSize(900, 700)

        self.preview = QLabel("No image. Click 'Capture'.")
        self.preview.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.preview.setStyleSheet("background:#23232A;color:#B8C1CC;padding:10px")
        self.prompt = QTextEdit()
        self.prompt.setPlaceholderText("Ask anything about the screenshot...")
        self.send_btn = QPushButton("Send")
        self.capture_btn = QPushButton("Capture")

        layout = QVBoxLayout()
        layout.addWidget(self.preview)
        btns = QHBoxLayout()
        btns.addWidget(self.capture_btn)
        btns.addStretch(1)
        layout.addLayout(btns)
        layout.addWidget(self.prompt)
        layout.addWidget(self.send_btn)
        self.setLayout(layout)

        self.send_btn.clicked.connect(self.on_send)
        self.capture_btn.clicked.connect(self.on_capture)

        self._png: Optional[bytes] = None
        self._client = OpenAIClient()

    def set_preview_image(self, pil: Image.Image):
        # Convert PIL image to QPixmap
        buf = BytesIO()
        pil.save(buf, format='PNG')
        data = buf.getvalue()
        self._png = data
        qim = QImage.fromData(data, 'PNG')
        pix = QPixmap.fromImage(qim)
        self.preview.setPixmap(pix.scaled(self.preview.width(), 360, Qt.AspectRatioMode.KeepAspectRatio, Qt.TransformationMode.SmoothTransformation))

    def resizeEvent(self, e):
        super().resizeEvent(e)
        if self.preview.pixmap() is not None:
            self.preview.setPixmap(self.preview.pixmap().scaled(self.preview.width(), 360, Qt.AspectRatioMode.KeepAspectRatio, Qt.TransformationMode.SmoothTransformation))

    def on_capture(self):
        # hide window briefly
        self.hide()
        QApplication.processEvents()
        overlay_res = SelectionOverlay.select_region(self)
        # show window again
        self.show()
        self.activateWindow()
        QApplication.processEvents()

        if overlay_res is None:
            return
        from .screen import capture_full
        full = capture_full()
        from .screen import crop
        l, t, w, h = overlay_res.pixel_rect
        cropped = crop(full, (l, t, w, h))
        self.set_preview_image(cropped)

    def on_send(self):
        if not self._png:
            self.preview.setText("Please capture an image first.")
            return
        prompt = self.prompt.toPlainText().strip()
        if not prompt:
            return
        msgs = image_message(prompt, self._png)
        self.prompt.setDisabled(True)
        self.send_btn.setDisabled(True)
        self._thread = AskThread(self._client, msgs)
        self._thread.delta.connect(self.append_delta)
        self._thread.done.connect(self.finish)
        self._thread.error.connect(self.show_error)
        self._thread.start()

    def append_delta(self, s: str):
        cur = self.preview.text() if self.preview.text() else ""
        # For demo, show response in the preview label below the image. In a full UI, use a chat list.
        self.preview.setText(cur + s)

    def finish(self):
        self.prompt.setDisabled(False)
        self.send_btn.setDisabled(False)

    def show_error(self, e: str):
        self.preview.setText(f"Error: {e}")
        self.prompt.setDisabled(False)
        self.send_btn.setDisabled(False)
