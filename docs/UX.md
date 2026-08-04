# Ping — UX notes

## Principles

1. **Paper is the notification.** The app is almost invisible until the moment
   a task is due; then the receipt *is* the interface. Nothing flashes, dings,
   or stacks up.
2. **One thing at a time.** Adding a task is three decisions — what, which day,
   which time block — on a single small card. No projects, tags, priorities,
   or recurring-rule machinery.
3. **The list is shy.** "Upcoming" exists so you can check, but it is
   de-emphasized, capped at the next few items, and closes when you click
   elsewhere. Ping never shows an overwhelming backlog.
4. **Calm by default.** Muted palette, generous spacing, rounded corners, soft
   shadows, friendly microcopy ("Nothing left for today.", "Enjoy the quiet.").
   Error messages tell you what to do, not what went wrong internally:
   "Printer not found — check the USB cable."
5. **Forgiving physics.** A drag never opens the popup; a click never moves the
   bubble (4 px threshold). Windows near the bubble place themselves
   intelligently and stay on screen. Missed tasks come back as one gentle
   receipt, never a cascade.
6. **Keyboard-friendly.** Add-task opens with the caret in the text field;
   Enter saves; Esc closes secondary windows; every action is also in the
   bubble's right-click menu and the tray menu.

## Palette

| Token | Value | Use |
|---|---|---|
| Paper | `#F7F6F3` | window background |
| Card | `#FFFFFF` | raised cards |
| Ink | `#3E3A34` | primary text |
| Muted | `#8A857C` | secondary text |
| Faint | `#BDB8AE` | hints, placeholders |
| Accent | `#7FBBB3` | primary buttons, checked chips |
| Accent soft | `#E2F0ED` | chip fill, hover wash |
| Divider | `#E7E3DB` | hairlines |
| Note | `#A9803E` | sparse warm highlights |

## Wireframes

### The bubble (actual size ≈ 48–72 px, scalable in Settings)

```
        ╭──────╮
       ╱  ╭──╮  ╲        soft radial disc, gentle shadow,
      │  ╰──╯   │       highlight top-left; draggable anywhere,
       ╲        ╱        remembers its spot
        ╰──────╯
```

### Click → popup (232 px card, appears above/beside the bubble)

```
        ╭──────╮
        ╰──────╯
   ┌───────────────────────────┐
   │ ┌───────────────────────┐ │
   │ │ ＋  Add task          │ │   accent button, full width
   │ └───────────────────────┘ │
   │    Upcoming               │   ghost button, muted
   │ ───────────────────────── │
   │ Settings  Print today     │   11 px footer row:
   │ Hide      Quit            │   quiet secondary actions
   └───────────────────────────┘
        closes when you click away
```

### Add task (348 px)

```
   ┌──────────────────────────────────────┐
   │ Add a task                           │
   │                                      │
   │ ┌──────────────────────────────────┐ │
   │ │ What needs doing?                │ │  ← focused, Enter = save
   │ └──────────────────────────────────┘ │
   │                                      │
   │ WHEN                                 │
   │ ( Today ) ( Tomorrow ) ( Wednesday ) │  day chips — day-after
   │ ( Pick day… )                        │  shows real weekday name
   │                                      │
   │ TIME                          09–19  │
   │ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐  │
   │ │ 09:00│ │ 09:30│ │ 10:00│ │ 10:30│  │  large tappable blocks,
   │ └──────┘ └──────┘ └──────┘ └──────┘  │  past times greyed out
   │ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐  │  (today only)
   │ │ 11:00│ │ 11:30│ │ 12:00│ │ 12:30│  │
   │ └──────┘ └──────┘ └──────┘ └──────┘  │
   │                 …                    │
   │                                      │
   │        ┌────────────────────┐        │
   │        │  Save & schedule   │        │
   │        └────────────────────┘        │
   │   gentle inline note if something's  │
   │   missing — never a dialog           │
   └──────────────────────────────────────┘
```

### Upcoming (304 px, capped at five)

```
   ┌────────────────────────────┐
   │ Upcoming                   │
   │                            │
   │  Today 14:30               │
   │  Call the plumber          │
   │ ────────────────────────── │
   │  Tomorrow 09:00            │
   │  Water the plants          │
   │ ────────────────────────── │
   │         …                  │
   │ ────────────────────────── │
   │  [ Print all for today ]   │
   └────────────────────────────┘
     empty state:
     "Nothing scheduled. Enjoy the quiet."
```

### Settings (470 px, scrolls)

```
   ┌──────────────────────────────────────────────┐
   │ Settings                                     │
   │                                              │
   │ PRINTER                                      │
   │ (•) Windows printer (USB)  [ combo ▾ ][⟳]    │
   │ ( ) Ethernet               host […] port[…]  │
   │ ( ) Files (testing)                          │
   │ Paper:  (•) 80 mm   ( ) 58 mm                │
   │ [ Test print ]   status appears here calmly  │
   │                                              │
   │ YOUR DAY                                     │
   │ Working hours  [09:00 ▾] → [19:00 ▾]         │
   │                                              │
   │ BUBBLE                                       │
   │ Size    ├──────●──────┤      Opacity ├──●──┤ │
   │ [✓] Always on top   [✓] Show on start        │
   │                                              │
   │ GENERAL                                      │
   │ [ ] Start Ping with Windows                  │
   │ [✓] Print missed tasks when Ping starts      │
   │                                              │
   │              [ Cancel ]  [ Save ]            │
   └──────────────────────────────────────────────┘
```

### The receipt (80 mm, 48 columns; 58 mm = 32 columns)

```
┌──────────────────────────────────────────────┐
│                    Ping                      │  double size, centred
│              Monday 4 Aug, 14:30             │
│                                              │
│           CALL THE PLUMBER ABOUT             │  double size, bold,
│               THE KITCHEN TAP                │  word-wrapped
│                                              │
│  Scheduled: Today 14:30                      │
│ ──────────────────────────────────────────── │
│              Done? Tick it off.              │
└──────────────────────────────────────────────┘
            ~ feed ×3, then cut ~

Missed-task variant adds "( missed earlier )" under the date.
Catch-up batches several tasks on one "While you were away" receipt.
"Print all for today" uses a "Today, on paper" header.
```

## Interaction details

- **Bubble**: left-drag moves (position saved on release, clamped on restore);
  quiet left-click toggles the popup; right-click opens the full menu.
- **Popup / Upcoming**: `OnDeactivated` closes them — no explicit dismiss
  button, no window chrome.
- **Day chips**: Today / Tomorrow / *next weekday by name* (e.g. "Wednesday") /
  Pick day… (mini calendar, can't pick the past).
- **Time chips**: generated from working hours at the configured step
  (≥ 15 min); choosing *Today* disables times that have already passed and
  preselects the first sensible slot.
- **Tray**: left click toggles the bubble; menu mirrors everything; status
  messages (print problems) arrive as quiet balloons titled "Ping".
- **Single instance**: a second launch just exits — the running Ping keeps
  your place.
