import os
import subprocess
import tempfile

CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
HTML_DIR = os.path.join(tempfile.gettempdir(), "grass-ui-sprites")
OUT_DIR = os.path.join(REPO, "Assets", "GrassSimulation", "Textures", "UI")
SCALE = 3

os.makedirs(HTML_DIR, exist_ok=True)
os.makedirs(OUT_DIR, exist_ok=True)

PAGE = """<!doctype html><html><head><meta charset="utf-8"><style>
html,body{{margin:0;padding:0;background:transparent;overflow:hidden}}
.wrap{{width:{w}px;height:{h}px;position:relative;box-sizing:border-box;padding:{pad}px}}
</style></head><body><div class="wrap">{body}</div></body></html>"""

ICON_STROKE = {
    "clock": '<circle cx="12" cy="13" r="8"/><path d="M12 9v4l2.5 2.5M9 2h6"/>',
    "lock": '<rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/>',
    "unlock": '<rect x="5" y="11" width="14" height="10" rx="2"/><path d="M8 11V8a4 4 0 0 1 7.8-1.3"/>',
    "flower": '<circle cx="12" cy="10" r="3"/><path d="M12 7a3 3 0 1 1 3 3M12 7a3 3 0 1 0-3 3M12 13v8M9 18l3-2 3 2"/>',
    "bush": '<path d="M5 18c-1.5-3 0-7 3-7 0-3 3-5 5.5-4C15 5 19 6 19 10c2 1 2 5 0 8z"/>',
    "grass": '<path d="M4 20c1-5 2-9 4-12M10 20c0-6 1-10 2-14M16 20c-1-5-1-8 1-12M20 20c-1-3-2-6-4-8"/>',
    "shield": '<path d="M12 3l8 3v6c0 4.5-3.4 8-8 9-4.6-1-8-4.5-8-9V6z"/>',
    "shield_x": '<path d="M12 3l8 3v6c0 4.5-3.4 8-8 9-4.6-1-8-4.5-8-9V6z"/><path d="M9 10l6 6M15 10l-6 6"/>',
    "check": '<path d="M5 12l5 5 9-10"/>',
    "bolt": '<path d="M13 2L4 14h7l-1 8 9-12h-7z"/>',
    "back": '<path d="M15 18l-6-6 6-6"/>',
    "zen": '<path d="M12 21c-5 0-8-3.5-8-8 4 0 6 1.5 8 4 2-2.5 4-4 8-4 0 4.5-3 8-8 8zM12 17V3M12 8c-2-2-4-2-5-1M12 6c2-2 4-2 5-1"/>',
    "levels": '<rect x="3" y="4" width="18" height="16" rx="3"/><path d="M3 9h18M8 4v5"/>',
    "gear": '<circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z"/>',
    "sound": '<path d="M4 9h4l5-4v14l-5-4H4z"/><path d="M16 9a4 4 0 0 1 0 6M18.5 6.5a8 8 0 0 1 0 11"/>',
    "haptics": '<rect x="8" y="3" width="8" height="18" rx="2"/><path d="M4 8v8M20 8v8"/>',
    "retry": '<path d="M4 12a8 8 0 1 0 2.3-5.7M4 4v4h4"/>',
    "home": '<path d="M4 11l8-7 8 7v9H4z"/><path d="M10 20v-6h4v6"/>',
}

ICON_FILL = {
    "pause": '<rect x="6" y="5" width="4" height="14" rx="1.5"/><rect x="14" y="5" width="4" height="14" rx="1.5"/>',
    "play": '<path d="M7 4.5v15a1 1 0 0 0 1.5.9l12-7.5a1 1 0 0 0 0-1.8l-12-7.5A1 1 0 0 0 7 4.5z"/>',
}

STAR_PATH = "M12 3l2.8 5.7 6.2.9-4.5 4.4 1 6.2L12 17.3 6.5 20.2l1-6.2L3 9.6l6.2-.9z"


def svg(inner, size, extra=""):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="0 0 24 24" {extra}>{inner}</svg>'


sprites = []


def add(name, w, h, body, pad=0, border=None):
    sprites.append({"name": name, "w": w, "h": h, "body": body, "pad": pad, "border": border})


def box(style):
    return f'<div style="width:100%;height:100%;box-sizing:border-box;{style}"></div>'


# 9-slice frames (design px; border = design px per side L,B,R,T at 1x)
add("panel_card", 96, 99, box("background:#FFFFFF;border-radius:24px;box-shadow:0 3px 0 #C3D6AE;height:96px"), border=[28, 31, 28, 28])
add("panel_modal", 104, 110, box("background:#FFFFFF;border-radius:28px;box-shadow:0 6px 0 #C3D6AE;height:104px"), border=[32, 38, 32, 32])
add("button_primary", 96, 77, box("background:#2E7D32;border-radius:22px;box-shadow:0 5px 0 #1D5420;height:72px"), border=[26, 31, 26, 26])
add("button_secondary", 64, 50, box("background:#E4EFD6;border-radius:16px"), border=[20, 20, 20, 20])
add("button_white", 64, 52, box("background:#FFFFFF;border-radius:16px;box-shadow:0 2px 0 #C3D6AE;height:50px"), border=[20, 22, 20, 20])
add("chip_white", 64, 48, box("background:#FFFFFF;border-radius:16px;box-shadow:0 2px 0 rgba(27,46,31,0.25);height:46px"), border=[20, 22, 20, 20])
add("pill_white", 48, 26, box("background:#FFFFFF;border-radius:13px;box-shadow:0 2px 0 #C3D6AE;height:24px"), border=[13, 14, 13, 12])

# Tintable white shapes
add("shape_round8", 32, 32, box("background:#FFFFFF;border-radius:8px"), border=[10, 10, 10, 10])
add("shape_round14", 40, 40, box("background:#FFFFFF;border-radius:14px"), border=[16, 16, 16, 16])
add("shape_pill", 32, 16, box("background:#FFFFFF;border-radius:8px"), border=[8, 8, 8, 8])
add("shape_circle", 64, 64, box("background:#FFFFFF;border-radius:50%"))
add("joystick_base", 132, 132, box("background:rgba(255,255,255,0.22);border:3px solid rgba(255,255,255,0.7);border-radius:50%"))
add("joystick_knob", 60, 63, box("background:rgba(255,255,255,0.92);border-radius:50%;box-shadow:0 3px 0 rgba(27,46,31,0.3);height:60px"))
add("upgrade_rings", 84, 84,
    '<div style="position:absolute;left:22px;top:22px;width:40px;height:40px;border-radius:50%;border:3px dashed #4A5D4E;box-sizing:border-box"></div>'
    '<div style="position:absolute;left:9px;top:9px;width:66px;height:66px;border-radius:50%;border:3px solid #2E7D32;box-sizing:border-box"></div>')

# White stroke icons (tint in Unity)
for name, inner in ICON_STROKE.items():
    add(f"icon_{name}", 48, 48, svg(inner, 48, 'fill="none" stroke="#FFFFFF" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"'))

for name, inner in ICON_FILL.items():
    add(f"icon_{name}", 48, 48, svg(inner, 48, 'fill="#FFFFFF"'))

# Multi-color icons
add("icon_coin", 48, 48, '<svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 28 28"><circle cx="14" cy="14" r="12" fill="#F4B740" stroke="#B9831C" stroke-width="2"/><circle cx="14" cy="14" r="6.5" fill="none" stroke="#B9831C" stroke-width="2"/></svg>')
add("icon_star_on", 64, 64, svg(f'<path d="{STAR_PATH}"/>', 64, 'fill="#F4B740" stroke="#B9831C" stroke-width="1.4" stroke-linejoin="round"'))
add("icon_star_off", 64, 64, svg(f'<path d="{STAR_PATH}"/>', 64, 'fill="#E4EFD6" stroke="#9DB28A" stroke-width="1.4" stroke-linejoin="round"'))
add("icon_star_bonus", 48, 48, svg(f'<path d="{STAR_PATH}"/>', 48, 'fill="#FFF3D6" stroke="#B9831C" stroke-width="1.8" stroke-linejoin="round"'))

manifest = []

for sprite in sprites:
    html_path = os.path.join(HTML_DIR, sprite["name"] + ".html")
    png_path = os.path.join(OUT_DIR, sprite["name"] + ".png")

    with open(html_path, "w", encoding="utf-8") as f:
        f.write(PAGE.format(w=sprite["w"], h=sprite["h"], pad=sprite["pad"], body=sprite["body"]))

    subprocess.run([
        CHROME,
        "--headless=new",
        "--disable-gpu",
        "--hide-scrollbars",
        "--default-background-color=00000000",
        f"--force-device-scale-factor={SCALE}",
        f"--window-size={sprite['w']},{sprite['h']}",
        f"--screenshot={png_path}",
        "file:///" + html_path.replace("\\", "/"),
    ], check=True, capture_output=True)

    border = sprite["border"]
    manifest.append({
        "name": sprite["name"],
        "border": [b * SCALE for b in border] if border else None,
    })

with open(os.path.join(HTML_DIR, "borders.txt"), "w", encoding="utf-8") as f:
    for item in manifest:
        border = ",".join(str(b) for b in item["border"]) if item["border"] else ""
        f.write(item["name"] + ";" + border + "\n")

print(len(manifest), "sprites written to", OUT_DIR)
