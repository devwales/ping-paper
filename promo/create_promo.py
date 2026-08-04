from __future__ import annotations

import math
import random
import shutil
import subprocess
import wave
from array import array
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
OUT = Path(__file__).resolve().parent
W, H, FPS, DURATION = 1280, 720, 24, 20

PAPER = "#F7F6F3"
CARD = "#FFFFFF"
INK = "#3E3A34"
MUTED = "#8A857C"
FAINT = "#BDB8AE"
ACCENT = "#7FBBB3"
ACCENT_DARK = "#568F88"
ACCENT_SOFT = "#E2F0ED"
DIVIDER = "#E7E3DB"
NOTE = "#A9803E"


def font(size: int, bold: bool = False):
    name = "seguisb.ttf" if bold else "segoeui.ttf"
    path = Path("C:/Windows/Fonts") / name
    return ImageFont.truetype(str(path), size)


F14, F16, F18, F22, F28, F36, F48, F64 = [font(x) for x in (14, 16, 18, 22, 28, 36, 48, 64)]
B14, B16, B18, B22, B28, B36, B48, B64 = [font(x, True) for x in (14, 16, 18, 22, 28, 36, 48, 64)]


def clamp(v, lo=0.0, hi=1.0):
    return max(lo, min(hi, v))


def ease(v):
    v = clamp(v)
    return 1 - (1 - v) ** 3


def smooth(v):
    v = clamp(v)
    return v * v * (3 - 2 * v)


def scene(t, start, end, fade=0.45):
    return smooth((t - start) / fade) * smooth((end - t) / fade)


def alpha_layer(base, layer, opacity=1.0):
    if opacity < 1:
        a = layer.getchannel("A").point(lambda x: int(x * opacity))
        layer.putalpha(a)
    base.alpha_composite(layer)


def rounded_shadow(base, box, radius=24, fill=CARD, shadow=35, offset=(0, 14), blur=24):
    x0, y0, x1, y1 = map(int, box)
    sh = Image.new("RGBA", base.size)
    sd = ImageDraw.Draw(sh)
    ox, oy = offset
    sd.rounded_rectangle((x0 + ox, y0 + oy, x1 + ox, y1 + oy), radius, fill=(62, 58, 52, shadow))
    sh = sh.filter(ImageFilter.GaussianBlur(blur))
    base.alpha_composite(sh)
    d = ImageDraw.Draw(base)
    d.rounded_rectangle((x0, y0, x1, y1), radius, fill=fill)


def centered(draw, xy, text, fnt, fill, anchor="mm"):
    draw.text(xy, text, font=fnt, fill=fill, anchor=anchor)


def wrap(draw, text, fnt, max_width):
    words, lines, line = text.split(), [], ""
    for word in words:
        test = (line + " " + word).strip()
        if draw.textbbox((0, 0), test, font=fnt)[2] <= max_width:
            line = test
        else:
            if line:
                lines.append(line)
            line = word
    if line:
        lines.append(line)
    return lines


def draw_icon(base, x, y, size, pulse=0.0):
    if size < 2:
        return
    glow = Image.new("RGBA", base.size)
    gd = ImageDraw.Draw(glow)
    r = int(size * (0.56 + pulse * 0.03))
    gd.ellipse((x-r, y-r, x+r, y+r), fill=(127, 187, 179, int(65 + 40*pulse)))
    glow = glow.filter(ImageFilter.GaussianBlur(int(size * 0.22)))
    base.alpha_composite(glow)
    icon_path = ROOT / "assets" / "ping-256.png"
    if icon_path.exists():
        icon = Image.open(icon_path).convert("RGBA").resize((size, size), Image.Resampling.LANCZOS)
        base.alpha_composite(icon, (int(x-size/2), int(y-size/2)))
    else:
        d = ImageDraw.Draw(base)
        d.ellipse((x-size/2, y-size/2, x+size/2, y+size/2), fill=ACCENT)


def desktop_card(base, t):
    # Calm fake desktop with the real Ping add-task layout.
    x0, y0, x1, y1 = 655, 105, 1175, 615
    rounded_shadow(base, (x0, y0, x1, y1), 28, "#EEF1EF", 30, (0, 18), 30)
    d = ImageDraw.Draw(base)
    d.rounded_rectangle((x0, y0, x1, y1), 28, fill="#E9EEEC")
    # desktop hint
    d.ellipse((1080, 150, 1130, 200), fill="#D5E6E2")
    d.ellipse((1100, 170, 1148, 218), fill="#C5DDD8")
    # Add task window enters
    p = ease((t - 5.2) / 0.8)
    card_y = int(145 + (1-p)*55)
    rounded_shadow(base, (720, card_y, 1082, card_y+430), 18, PAPER, 36, (0, 12), 20)
    d = ImageDraw.Draw(base)
    d.text((745, card_y+25), "Add task", font=B22, fill=INK)
    # input
    d.rounded_rectangle((745, card_y+67, 1057, card_y+117), 10, fill=CARD, outline=ACCENT, width=2)
    typed = "Call the plumber"[:max(0, int((t-6.0)*12))]
    d.text((760, card_y+81), typed or "What needs doing?", font=F16, fill=INK if typed else FAINT)
    if 6.0 < t < 7.5 and int(t*3) % 2:
        tx = d.textbbox((760, card_y+81), typed, font=F16)[2]
        d.line((tx+2, card_y+80, tx+2, card_y+103), fill=ACCENT_DARK, width=2)
    d.text((747, card_y+139), "DAY", font=B14, fill=MUTED)
    chips = [("Today", 745, 800), ("Tomorrow", 816, 905), ("Thursday", 921, 1028)]
    for label, xa, xb in chips:
        selected = label == "Tomorrow" and t >= 7.4
        d.rounded_rectangle((xa, card_y+165, xb, card_y+201), 18, fill=ACCENT_SOFT if selected else CARD,
                            outline=ACCENT if selected else DIVIDER, width=2 if selected else 1)
        centered(d, ((xa+xb)//2, card_y+183), label, F14, ACCENT_DARK if selected else MUTED)
    d.text((747, card_y+224), "TIME", font=B14, fill=MUTED)
    times = ["09:00", "09:30", "10:00", "10:30", "11:00", "11:30"]
    for i, label in enumerate(times):
        col, row = i % 3, i // 3
        xa, ya = 745 + col*105, card_y+250 + row*48
        selected = label == "09:00" and t >= 8.1
        d.rounded_rectangle((xa, ya, xa+92, ya+36), 8, fill=ACCENT if selected else CARD,
                            outline=ACCENT if selected else DIVIDER)
        centered(d, (xa+46, ya+18), label, F14, CARD if selected else INK)
    saved = ease((t-8.9)/0.45)
    d.rounded_rectangle((745, card_y+358, 1057, card_y+407), 11, fill=ACCENT)
    centered(d, (901, card_y+382), "Scheduled  ✓" if saved else "Save & schedule", B16, CARD)


def printer_scene(base, t):
    d = ImageDraw.Draw(base)
    # desk
    d.rectangle((0, 548, W, H), fill="#E7DED1")
    d.rectangle((0, 548, W, 558), fill="#D6C8B8")
    # soft plant silhouettes
    for pts in [((1000, 548),(970,390),(1040,470)), ((1040,548),(1100,410),(1060,490)), ((980,548),(920,435),(995,485))]:
        d.polygon(pts, fill="#C5D8D0")
    # printer body
    rounded_shadow(base, (700, 285, 1035, 565), 28, "#EFEDEA", 55, (0, 18), 22)
    d = ImageDraw.Draw(base)
    d.rounded_rectangle((725, 320, 1010, 535), 22, fill="#454642")
    d.rounded_rectangle((760, 355, 975, 455), 14, fill="#292B29")
    d.rounded_rectangle((790, 404, 945, 430), 8, fill="#111311")
    d.ellipse((955, 485, 968, 498), fill=ACCENT)
    # receipt comes out
    p = ease((t - 12.2) / 2.4)
    top, bottom = 420, int(435 + 310*p)
    if bottom > top:
        paper = Image.new("RGBA", (270, max(1, bottom-top)), (250,248,242,255))
        pd = ImageDraw.Draw(paper)
        pd.line((0, 4, 270, 4), fill="#D8D3C9", width=2)
        if p > .22:
            centered(pd, (135, 34), "Ping", B22, INK)
            centered(pd, (135, 62), "Tuesday 4 Aug, 14:30", F14, MUTED)
        if p > .48:
            pd.line((24, 88, 246, 88), fill=DIVIDER, width=2)
            centered(pd, (135, 122), "CALL THE PLUMBER", B18, INK)
            centered(pd, (135, 150), "ABOUT THE KITCHEN TAP", B16, INK)
        if p > .74:
            pd.line((24, 179, 246, 179), fill=DIVIDER, width=2)
            centered(pd, (135, 208), "Done? Tick it off.", F14, MUTED)
        base.alpha_composite(paper, (783, top))
    # redraw slot lip
    d = ImageDraw.Draw(base)
    d.rounded_rectangle((786, 411, 949, 435), 8, fill="#101210")


def render_frame(i):
    t = i / FPS
    base = Image.new("RGBA", (W, H), PAPER)
    # subtle warm radial field
    bg = Image.new("RGBA", (W, H), (0,0,0,0))
    bd = ImageDraw.Draw(bg)
    bd.ellipse((-220, -310, 760, 670), fill=(226,240,237,150))
    bd.ellipse((800, 320, 1500, 1000), fill=(239,225,205,100))
    bg = bg.filter(ImageFilter.GaussianBlur(100))
    base.alpha_composite(bg)

    # Scene 1: overwhelm
    a = scene(t, 0, 3.7)
    if a:
        layer = Image.new("RGBA", base.size)
        d = ImageDraw.Draw(layer)
        d.text((110, 105), "Your to-do list", font=F28, fill=MUTED)
        d.text((110, 145), "shouldn’t shout.", font=B64, fill=INK)
        tasks = ["Email everyone", "Book the thing", "Fix the tap", "Buy groceries", "Update project", "Call Mum"]
        for n, task in enumerate(tasks):
            rise = ease((t - .45 - n*.20)/.5)
            if rise <= 0: continue
            x = 120 + (n%3)*330
            y = 280 + (n//3)*105 + (1-rise)*32
            d.rounded_rectangle((x,y,x+290,y+72),16,fill=(255,255,255,int(245*rise)),outline=DIVIDER)
            d.ellipse((x+18,y+24,x+40,y+46),outline=FAINT,width=2)
            d.text((x+55,y+22),task,font=F18,fill=INK)
        alpha_layer(base, layer, a)

    # Scene 2: identity
    a = scene(t, 3.2, 5.7)
    if a:
        layer = Image.new("RGBA", base.size)
        p = ease((t-3.3)/.7)
        draw_icon(layer, 640, int(250+(1-p)*35), int(150*p), math.sin(t*4)*.5+.5)
        d = ImageDraw.Draw(layer)
        centered(d,(640,390),"Meet Ping.",B48,INK)
        centered(d,(640,444),"A quiet desktop companion.",F22,MUTED)
        alpha_layer(base, layer, a)

    # Scene 3: schedule
    a = scene(t, 5.0, 10.9)
    if a:
        layer = Image.new("RGBA", base.size)
        d = ImageDraw.Draw(layer)
        d.text((90,150),"Schedule one thing.",font=B48,fill=INK)
        d.text((94,218),"What. When. Done.",font=F22,fill=MUTED)
        d.rounded_rectangle((94,284,485,352),18,fill=ACCENT_SOFT)
        d.text((118,303),"No projects  •  No priorities  •  No noise",font=F16,fill=ACCENT_DARK)
        desktop_card(layer,t)
        alpha_layer(base, layer, a)

    # Scene 4: receipt
    a = scene(t, 10.35, 16.7)
    if a:
        layer = Image.new("RGBA", base.size)
        d = ImageDraw.Draw(layer)
        d.text((92,128),"Then, right on time…",font=F28,fill=MUTED)
        d.text((92,170),"Paper is the notification.",font=B48,fill=INK)
        d.text((96,245),"No popup. No badge.\nJust the task in your hands.",font=F22,fill=MUTED,spacing=10)
        printer_scene(layer,t)
        alpha_layer(base, layer, a)

    # Scene 5: final
    a = scene(t, 16.1, 20.1)
    if a:
        layer = Image.new("RGBA", base.size)
        draw_icon(layer,640,205,150,math.sin(t*3)*.5+.5)
        d = ImageDraw.Draw(layer)
        centered(d,(640,340),"One task. On paper.",B48,INK)
        centered(d,(640,398),"Right when it matters.",F28,MUTED)
        centered(d,(640,500),"Ping",B28,ACCENT_DARK)
        centered(d,(640,545),"Private  •  Local  •  Offline",F18,MUTED)
        alpha_layer(base, layer, a)

    # discreet brand bug
    d = ImageDraw.Draw(base)
    d.text((W-42,H-30),"PING",font=B14,fill="#B5B0A7",anchor="rs")
    return base.convert("RGB")


def make_audio(path):
    rate = 44100
    random.seed(4)
    data = array("h")
    chord = [174.61, 220.00, 261.63, 329.63]
    beats = [3.35, 5.25, 7.45, 8.15, 9.05, 12.25, 14.55, 16.35]
    for n in range(DURATION * rate):
        t = n / rate
        # Airy, original ambient chord.
        bed = 0.0
        fade = min(1, t/1.5, (DURATION-t)/1.5)
        for j, f in enumerate(chord):
            bed += math.sin(2*math.pi*f*t + j*.7) * (0.016/(j+1))
        bed *= fade * (0.75 + 0.25*math.sin(2*math.pi*.08*t))
        bell = 0.0
        for b in beats:
            dt = t-b
            if 0 <= dt < 1.2:
                env = math.exp(-5.2*dt)
                bell += env*(math.sin(2*math.pi*659.25*dt)+.45*math.sin(2*math.pi*987.77*dt))*0.035
        # Tiny receipt-printer texture.
        mech = 0.0
        if 12.2 < t < 14.6:
            mech = (random.random()*2-1)*0.012*(0.7+0.3*math.sin(2*math.pi*18*t))
        val = int(max(-1,min(1,bed+bell+mech))*32767)
        data.extend((val,val))
    with wave.open(str(path),"wb") as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(rate); w.writeframes(data.tobytes())


def main():
    OUT.mkdir(exist_ok=True)
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        raise SystemExit("ffmpeg not found")
    silent = OUT / "Ping-promo-silent.mp4"
    audio = OUT / "Ping-promo-audio.wav"
    final = OUT / "Ping-promo.mp4"
    cmd = [ffmpeg,"-y","-f","rawvideo","-vcodec","rawvideo","-pix_fmt","rgb24","-s",f"{W}x{H}",
           "-r",str(FPS),"-i","-","-an","-c:v","libx264","-preset","medium","-crf","18","-pix_fmt","yuv420p",
           "-movflags","+faststart",str(silent)]
    proc = subprocess.Popen(cmd,stdin=subprocess.PIPE)
    assert proc.stdin
    for i in range(FPS*DURATION):
        proc.stdin.write(render_frame(i).tobytes())
    proc.stdin.close()
    if proc.wait()!=0: raise SystemExit("Video render failed")
    make_audio(audio)
    subprocess.run([ffmpeg,"-y","-i",str(silent),"-i",str(audio),"-c:v","copy","-c:a","aac","-b:a","192k",
                    "-shortest","-movflags","+faststart",str(final)],check=True)
    silent.unlink(missing_ok=True); audio.unlink(missing_ok=True)
    print(final)


if __name__ == "__main__":
    main()
