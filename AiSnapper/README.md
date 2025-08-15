# AiSnapper (Windows, .NET 8, WPF)

A tiny desktop app to:
- Launch with **Ctrl+Alt+I**
- Select a **rectangular region** (or right‑click for full screen)
- **Preview** the capture, type a **prompt**
- Send both to **OpenAI Vision** (`gpt-4o-mini`)
- Show the **response** with a **Copy** button

## Prereqs
- Windows 10/11
- .NET 8 SDK
- OpenAI API key in env var: `OPENAI_API_KEY`

## Run
```powershell
setx OPENAI_API_KEY "sk-..."
dotnet build
dotnet run --project AiSnapper\AiSnapper.csproj
```
The hotkey is **Ctrl+Alt+I**. Change it in `MainWindow.xaml.cs` if you prefer.

## Notes
- Region selection: drag with left mouse; press **Esc** to cancel; **Right‑click** for full‑screen capture quickly.
- Multi-monitor supported via the OS virtual screen bounds.
- If the hotkey fails, try running as admin or change the combo (global hotkeys can collide).
- The OpenAI call uses simple HTTP; swap the model as you like.
