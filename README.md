# Ping

A quiet desktop companion for Windows: a small floating bubble that turns your
to-dos into paper. Add a task with a time, and at that moment a clean thermal
receipt prints from your ESC/POS printer. No popups, no badges, no list staring
back at you — just paper, when it matters.

Built by devs who get overwhelmed by giant to-do lists — and who side-quest
instead of doing the thing. Ping shows one task at a time, on your desk, not on
your screen.

## What it does

- **The bubble** sits on your desktop. Drag it anywhere; it remembers where.
- **Add a task** asks for three things: what needs doing, which day, and a time
  (shown as big tappable blocks within your working hours). One tap and it's done.
- At the scheduled time, your thermal printer rolls out a receipt:

  ```
  ┌────────────────────────────────┐
  │              Ping              │
  │        Monday 4 Aug, 14:30     │
  │     CALL THE PLUMBER ABOUT     │
  │         THE KITCHEN TAP        │
  │  Scheduled: Today 14:30        │
  │  ────────────────────────────  │
  │      Done? Tick it off.        │
  └────────────────────────────────┘
  ```

- **Gentle catch-up**: if the PC was off when a task came due, Ping prints one
  grouped *"While you were away"* receipt when you're back — never a paper
  storm. Yesterday's tasks quietly retire.
- **Private by nature**: everything is local (SQLite in `%LocalAppData%\Ping`).
  No cloud, no account, works fully offline.

## Requirements

- Windows 10 or 11 (x64)
- An ESC/POS thermal receipt printer — tuned for the **CISSIYOG / POSSAF E**
  (58 mm / 80 mm, USB or Ethernet), compatible with most common models
  (Epson TM-T20, Star TSP100, Xprinter, Gprinter, Munbyn, Rongta, Hoin,
  Zjiang, Netum, Vretti, "Generic / Text Only", …)
- No .NET install needed — published builds are self-contained.

## Install

Download the latest `Ping-win-x64.zip` release (or the `PingSetup.exe`
installer) from the **Releases** page. The installer is per-user — no admin
needed — and installs to `%LocalAppData%\Programs\Ping`.

First run: Ping appears as a bubble at the bottom-right of your screen, plus a
small tray icon. Open **Settings**, pick your printer, and press **Test print** —
you should get a receipt within seconds.

See **[docs/PRINTER.md](docs/PRINTER.md)** for printer setup and troubleshooting.

## Building

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download)
(`windowsdesktop` workload is included).

```powershell
# Build
dotnet build -c Release

# Publish a single self-contained exe (no runtime required on target PCs)
dotnet publish src/Ping -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# Output:
#   src/Ping/bin/Release/net8.0-windows/win-x64/publish/Ping.exe

# Installer (requires Inno Setup 6, https://jrsoftware.org/isinfo.php)
ISCC installer\Ping.iss    # produces installer\output\PingSetup-1.0.0.exe
```

Or just let **CI** do it: push a `v1.0.x` tag and GitHub Actions builds the
self-contained exe and attaches it to a Release.

## Project layout

```
Ping.sln
assets/            icon source script + generated ping.ico / PNGs
installer/         Inno Setup script
src/Ping/
  App.xaml(.cs)    startup, single-instance, wiring
  Models/          PingTask, AppSettings
  Data/            SQLite: schema, task store, settings store
  Printing/        ESC/POS builder, receipt composer, transports,
                   discovery, print queue
  Scheduling/      due-task scheduler + catch-up policy
  Platform/        Windows autostart (per-user Run key)
  Resources/       CalmTheme.xaml — palette & control styles
  UI/              Bubble, Popup, AddTask, Upcoming, Settings windows,
                   tray icon, placement helper
```

## Docs

- **[docs/UX.md](docs/UX.md)** — design principles and wireframes
- **[docs/PRINTER.md](docs/PRINTER.md)** — printer setup, CISSIYOG/POSSAF E
  notes, ESC/POS details, troubleshooting

## License

[MIT](LICENSE)
