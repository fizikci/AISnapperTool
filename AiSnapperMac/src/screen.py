from typing import Tuple
from PIL import Image
import mss

# Returns a PIL Image of the entire virtual screen (spanning all monitors)
def capture_full() -> Image.Image:
    with mss.mss() as sct:
        monitor = sct.monitors[0]  # virtual screen
        img = sct.grab(monitor)
        return Image.frombytes('RGB', img.size, img.rgb)

# Crop a rectangle (left, top, width, height) from given PIL image
def crop(img: Image.Image, rect: Tuple[int, int, int, int]) -> Image.Image:
    l, t, w, h = rect
    return img.crop((l, t, l + w, t + h))

# Helper to get all monitors bounds
def get_monitors() -> list:
    with mss.mss() as sct:
        return sct.monitors
