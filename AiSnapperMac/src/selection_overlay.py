from PyQt6.QtWidgets import QWidget, QApplication
from PyQt6.QtGui import QPainter, QColor, QPen, QGuiApplication
from PyQt6.QtCore import Qt, QRectF, QPointF
from dataclasses import dataclass
from typing import Optional, Tuple
from .util import Rect, virtual_geometry, estimate_device_pixel_ratio, logical_rect_to_pixels

@dataclass
class SelectionResult:
    logical_rect: Rect
    pixel_rect: Tuple[int, int, int, int]

class SelectionOverlay(QWidget):
    def __init__(self):
        super().__init__(None, Qt.WindowType.FramelessWindowHint | Qt.WindowType.Tool | Qt.WindowType.WindowStaysOnTopHint)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground, True)
        self.setAttribute(Qt.WidgetAttribute.WA_NoSystemBackground, True)
        self.setMouseTracking(True)
        self._dragging = False
        self._start = QPointF(0, 0)
        self._current = QPointF(0, 0)

        # Cover the virtual screen with a single window
        vg = QGuiApplication.primaryScreen().virtualGeometry()
        self.setGeometry(vg)

        self._result: Optional[SelectionResult] = None

    def paintEvent(self, event):
        p = QPainter(self)
        p.setRenderHint(QPainter.RenderHint.Antialiasing)

        # Dim background
        p.fillRect(self.rect(), QColor(0, 0, 0, 110))

        if self._dragging:
            rect = QRectF(self._start, self._current)
            norm = rect.normalized()
            # Cutout effect
            p.setCompositionMode(QPainter.CompositionMode.CompositionMode_Clear)
            p.fillRect(norm, QColor(0, 0, 0, 0))
            p.setCompositionMode(QPainter.CompositionMode.CompositionMode_SourceOver)
            # Border
            pen = QPen(QColor(255, 255, 255, 220))
            pen.setWidth(2)
            p.setPen(pen)
            p.drawRect(norm)

    def mousePressEvent(self, event):
        if event.button() == Qt.MouseButton.LeftButton:
            self._dragging = True
            self._start = event.position()
            self._current = event.position()
            self.update()
        elif event.button() == Qt.MouseButton.RightButton:
            # Full screen selection quickly
            vg = virtual_geometry()
            dpr = estimate_device_pixel_ratio()
            px = logical_rect_to_pixels(vg, vg, dpr)
            self._result = SelectionResult(vg, px)
            self.close()

    def mouseMoveEvent(self, event):
        if not self._dragging:
            return
        self._current = event.position()
        self.update()

    def mouseReleaseEvent(self, event):
        if self._dragging and event.button() == Qt.MouseButton.LeftButton:
            self._dragging = False
            vg = virtual_geometry()
            dpr = estimate_device_pixel_ratio()
            logical = Rect(self._start.x(), self._start.y(), self._current.x() - self._start.x(), self._current.y() - self._start.y())
            px = logical_rect_to_pixels(logical, vg, dpr)
            self._result = SelectionResult(logical.normalized(), px)
            self.close()

    def keyPressEvent(self, event):
        if event.key() == Qt.Key.Key_Escape:
            self._result = None
            self.close()

    @staticmethod
    def select_region(parent=None) -> Optional[SelectionResult]:
        overlay = SelectionOverlay()
        overlay.showFullScreen()
        # Run a nested event loop until closed
        app = QApplication.instance()
        while overlay.isVisible():
            app.processEvents()
        return overlay._result
