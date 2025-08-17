AiSnapperMac - Python (macOS)

Features parity with Windows version:
- Global hotkeys: Cmd+Shift+C (capture), Cmd+Shift+E (quick edit)
- Region selection overlay; right-click or press F for full screen
- Screenshot capture across multiple monitors
- Chat window with image preview and streaming responses
- Quick Edit floating window for selected text with AI transforms

Stack
- Python 3.10+
- PyQt6 (UI), PyQt6-Qt6 (packaged), qasync (optional)
- Pillow (image), mss (screenshots), pyautogui (keyboard/clipboard fallback), pynput (hotkeys)
- requests (OpenAI), websockets/sseclient-py (optional for streaming)

Run
1) python -m venv .venv; source .venv/bin/activate
2) pip install -r requirements.txt
3) export OPENAI_API_KEY=sk-...
4) python -m src.main

Packaged app (optional)
- Use pyinstaller or Briefcase later.

Notes
- Uses Cocoa-safe global hotkeys via pynput; for hardened/production, consider a small Swift helper with NSEvent.
- Region overlay uses a transparent fullscreen PyQt window across all screens.
- Clipboard and selected text read/paste use NSPasteboard via PyObjC if available; falls back to pyperclip + Cmd+C/V synth.
