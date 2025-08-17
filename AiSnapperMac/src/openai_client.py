import os
import json
import base64
import requests
from typing import List, Callable, Optional

API_URL = "https://api.openai.com/v1/chat/completions"
DEFAULT_MODEL = os.getenv("OPENAI_MODEL", "gpt-4o")

class OpenAIClient:
    def __init__(self, api_key: Optional[str] = None, model: Optional[str] = None):
        self.api_key = api_key or os.getenv("OPENAI_API_KEY")
        if not self.api_key:
            raise RuntimeError("OPENAI_API_KEY is not set")
        self.model = model or DEFAULT_MODEL
        self.session = requests.Session()
        self.session.headers.update({
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json"
        })

    def ask(self, messages: List[dict]) -> str:
        payload = {
            "model": self.model,
            "messages": messages,
            "temperature": 0.2
        }
        r = self.session.post(API_URL, data=json.dumps(payload))
        if r.status_code != 200:
            raise RuntimeError(f"OpenAI error {r.status_code}: {r.text}")
        data = r.json()
        return data["choices"][0]["message"]["content"]

    def ask_stream(self, messages: List[dict], on_delta: Callable[[str], None]):
        payload = {
            "model": self.model,
            "messages": messages,
            "temperature": 0.2,
            "stream": True
        }
        with self.session.post(API_URL, data=json.dumps(payload), stream=True) as r:
            if r.status_code != 200:
                raise RuntimeError(f"OpenAI error {r.status_code}: {r.text}")
            for line in r.iter_lines(decode_unicode=True):
                if not line:
                    continue
                if line.startswith("data: "):
                    data = line[6:].strip()
                    if data == "[DONE]":
                        break
                    try:
                        obj = json.loads(data)
                        delta = obj["choices"][0]["delta"].get("content")
                        if isinstance(delta, str):
                            on_delta(delta)
                    except Exception:
                        pass


def image_message(prompt: str, png_bytes: bytes) -> List[dict]:
    b64 = base64.b64encode(png_bytes).decode("ascii")
    return [
        {"role": "system", "content": [ {"type": "text", "text": "You are a helpful assistant. Be concise and reference the image when relevant."} ]},
        {
            "role": "user",
            "content": [
                {"type": "text", "text": prompt},
                {"type": "image_url", "image_url": {"url": f"data:image/png;base64,{b64}", "detail": "high"}}
            ]
        }
    ]
