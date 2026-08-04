# Ping — printer guide

Ping prints raw ESC/POS, the command language spoken by virtually every thermal
receipt printer. It was designed and tuned around the **CISSIYOG / POSSAF E**
(58 mm / 80 mm, USB + Ethernet) and works with most ESC/POS-compatible models.

## Quick start

1. Connect the printer (USB or Ethernet), power on, load paper.
2. Open Ping → **Settings**.
3. Leave **Windows printer (USB)** selected — Ping auto-detects common thermal
   printers and preselects the best guess. Pick yours from the list if needed
   (press **⟳** after plugging in a new one).
4. Choose paper width: **80 mm** (default) or **58 mm**.
5. Press **Test print**. A receipt should appear within seconds.

If the test print fails, Ping tells you why in plain words, right under the
button and (if the window is closed) as a tray message.

## Connection modes

### Windows printer (USB) — recommended

Ping prints through the Windows spooler in **RAW** mode: the exact ESC/POS
bytes go to the printer unmodified. Two driver choices work:

- **Vendor driver** (CISSIYOG/POSSAF, Xprinter, etc.) — install it normally,
  the printer shows up in Windows, Ping finds it. Best option.
- **Generic / Text Only** (built into Windows) — works with nearly every
  ESC/POS printer. In *Settings → Bluetooth & devices → Printers & scanners →
  Add device → Add manually → install a local printer → use an existing port
  (USB001…)*, choose **Generic → Generic / Text Only**.

No Zadig driver swaps, no admin rights, no special services.

### Ethernet

1. Give the printer a known IP (its self-test page prints the current address —
   usually hold the feed button while powering on).
2. In Ping Settings choose **Ethernet**, enter the IP, port **9100**.
3. Test print.

Ping connects per job with an 8-second timeout. If the printer is unreachable
you'll get "Couldn't reach the printer at … — check the network cable."

### Files (testing)

No printer? Choose **Files**. Receipts are written as plain text to
`%LocalAppData%\Ping\receipts\receipt_yyyyMMdd_HHmmss.txt` — handy for trying
Ping, or for seeing exactly what would print.

## CISSIYOG / POSSAF E notes

Tested-by-design against this printer family:

| Topic | Notes |
|---|---|
| USB | Install the vendor driver (CD or vendor site) or *Generic / Text Only*. The printer enumerates as a standard USB printing device. |
| Ethernet | Default print port is 9100 (raw). Print the self-test page to learn the IP. |
| 80 mm | Full 48-column layout (Font A, 12×24). This is the reference design. |
| 58 mm | Select 58 mm in Settings — Ping switches to a 32-column layout automatically. |
| Cutter | Ping ends every receipt with 3 line feeds and a full cut (`GS V`). Models without an auto-cutter simply feed past the tear bar. |
| Character set | Receipt text is ASCII; smart quotes/dashes are normalized before printing. |
| Darkness/speed | Controlled on the printer itself (DIP switches / vendor tool), not by Ping. |

## ESC/POS details (for the curious)

Receipts are composed in `src/Ping/Printing/EscPos.cs` and
`ReceiptComposer.cs`. The command subset is deliberately small and portable:

| Command | Bytes | Used for |
|---|---|---|
| Initialize | `ESC @` | start of every receipt |
| Align | `ESC a n` | centred header/footer, left body |
| Bold | `ESC E n` | task text |
| Size | `GS ! n` | double-width/height "Ping" header and task text |
| Feed | `ESC d n` | 3 blank lines before the cut |
| Cut | `GS V 66 3` | full cut (feed-and-cut variant) |

Columns per line: **48** at 80 mm, **32** at 58 mm (Font A). Text is
word-wrapped in code, so long task names flow cleanly on either width.

## Reliability behaviour

- Printing is queued and asynchronous — the UI never waits on the printer.
- On failure (offline, busy, unplugged): **5 attempts, 20 s apart**.
- After the last attempt: a calm tray message, e.g.
  - "No printer selected yet — pick one in Settings."
  - "Printer not found — check it's installed and the USB cable is connected."
  - "Couldn't reach the printer at 192.168.1.50:9100 — check the network cable."
- Every job (success or failure) is recorded in the `print_log` table of
  `ping.db`, so a missed receipt is explainable after the fact.
- Retry works because jobs are idempotent by design: a task is only marked
  *printed* after the printer confirms the job went through, so a power blink
  at worst reprints a receipt — it never silently loses one.

## Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| Test print: "Printer not found" | Wrong printer selected, or USB unplugged. Press **⟳** in Settings and re-pick. |
| Nothing prints, no error | Spooler stalled: *services.msc → Print Spooler → Restart*, then re-test. |
| Gibberish / stray characters | Printer isn't ESC/POS (rare label printers), or the driver is translating text. Use RAW: pick the vendor driver or *Generic / Text Only*. |
| Text too wide / wraps oddly | Wrong paper width — toggle 80 mm ↔ 58 mm in Settings. |
| Ethernet times out | Printer IP changed (DHCP). Print self-test page for current IP; consider a DHCP reservation. |
| Receipt prints but doesn't cut | Model without cutter, or cut disabled in printer config. Paper still feeds to the tear bar. |
| Prints delayed by ~20 s | A previous job is retrying — check the printer, or wait; the queue drains itself. |
| Doubt about what was sent | Switch to **Files** mode and inspect the text receipt — it's exactly what the printer receives, minus control bytes. |
