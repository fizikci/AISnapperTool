from dataclasses import dataclass
from typing import Tuple
from PyQt6.QtGui import QGuiApplication

@dataclass
class Rect:
    x: float
    y: float
    w: float
    h: float

    def normalized(self) -> 'Rect':
        x2 = self.x + self.w
        y2 = self.y + self.h
        nx = min(self.x, x2)
        ny = min(self.y, y2)
        nw = abs(self.w)
        nh = abs(self.h)
        return Rect(nx, ny, nw, nh)


def virtual_geometry() -> Rect:
    vg = QGuiApplication.primaryScreen().virtualGeometry()
    return Rect(vg.left(), vg.top(), vg.width(), vg.height())


def estimate_device_pixel_ratio() -> float:
    # Use the primary screen scale as an estimate; works when all monitors share the same scale
    return float(QGuiApplication.primaryScreen().devicePixelRatio())


def logical_rect_to_pixels(logical: Rect, virtual_logical: Rect, dpr: float) -> Tuple[int, int, int, int]:
    logical = logical.normalized()
    # Map logical selection to pixel rect relative to the top-left of the virtual geometry
    px_left = round((logical.x - virtual_logical.x) * dpr)
    px_top = round((logical.y - virtual_logical.y) * dpr)
    px_w = max(1, round(logical.w * dpr))
    px_h = max(1, round(logical.h * dpr))
    return (px_left, px_top, px_w, px_h)
