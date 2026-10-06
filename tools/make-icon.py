"""Draws the app icon and the installer artwork.

  Browser-Selector/app.ico         app icon, every Windows size
  docs/icon.png                    256 px app icon for the README
  installer/setup.ico              app icon + a green "install" badge, so Setup doesn't look like the app
  installer/wizard-small.png       app icon for the top corner of the setup pages
  installer/wizard-large.png       tall blue panel for the setup's finish page

Run: python tools/make-icon.py   (needs Pillow)
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
S = 1024  # draw big, then downsample
TOP, BOTTOM = (56, 132, 255, 255), (28, 76, 196, 255)
WHITE = (255, 255, 255, 255)
ICO_SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]


def lerp(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def gradient(w, h):
    img = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(img)
    for y in range(h):
        d.line([(0, y), (w, y)], fill=lerp(TOP, BOTTOM, y / h))
    return img


def draw_glyph(d, ox, oy, k):
    """The globe with the link forking into two choices, in a 1024-unit box at (ox, oy), scaled by k."""
    w = 46 * k
    cx, cy, r = ox + 400 * k, oy + 512 * k, 250 * k
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=WHITE, width=round(w))
    d.ellipse([cx - r * 0.45, cy - r, cx + r * 0.45, cy + r], outline=WHITE, width=round(w))
    d.line([cx - r, cy, cx + r, cy], fill=WHITE, width=round(w))
    sx, sy = cx + r, cy
    for dy in (-1, 1):
        ex, ey = ox + 800 * k, cy + dy * 230 * k
        d.line([sx, sy, ex, ey], fill=WHITE, width=round(w))
        d.ellipse([ex - 62 * k, ey - 62 * k, ex + 62 * k, ey + 62 * k], fill=WHITE)
    d.ellipse([sx - 34 * k, sy - 34 * k, sx + 34 * k, sy + 34 * k], fill=WHITE)


def app_icon():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).rounded_rectangle([40, 40, S - 40, S - 40], radius=220, fill=255)
    img.paste(gradient(S, S), (0, 0), mask)
    draw_glyph(ImageDraw.Draw(img), 0, 0, 1)
    return img


def setup_icon(app):
    """The app icon with a green badge holding a down arrow into a tray: an installer."""
    c, r = 760, 250  # badge centre and radius (bottom-right, large enough to read at 16 px)
    img2 = app.copy()
    # A transparent ring around the badge separates it from the icon.
    cut = Image.new("L", (S, S), 255)
    ImageDraw.Draw(cut).ellipse([c - r - 34, c - r - 34, c + r + 34, c + r + 34], fill=0)
    img2.putalpha(Image.composite(img2.getchannel("A"), Image.new("L", (S, S), 0), cut))
    d = ImageDraw.Draw(img2)
    d.ellipse([c - r, c - r, c + r, c + r], fill=(22, 163, 74, 255))
    w = 52
    d.line([c, c - 150, c, c + 40], fill=WHITE, width=w)                       # arrow shaft
    d.polygon([(c - 105, c - 10), (c + 105, c - 10), (c, c + 100)], fill=WHITE)  # arrow head
    d.line([c - 130, c + 140, c + 130, c + 140], fill=WHITE, width=w)            # tray
    return img2


def wizard_large():
    """Tall panel (Inno's large image is 164x314 at 100%; drawn at 3x)."""
    w, h = 492, 942
    img = gradient(w, h)
    k = 0.42
    draw_glyph(ImageDraw.Draw(img), (w - 1024 * k) / 2 - 20 * k, h * 0.36 - 512 * k, k)
    return img.convert("RGB")


def main():
    app = app_icon()
    (ROOT / "installer").mkdir(exist_ok=True)
    app.resize((256, 256), Image.LANCZOS).save(ROOT / "Browser-Selector" / "app.ico", sizes=[(s, s) for s in ICO_SIZES])
    app.resize((256, 256), Image.LANCZOS).save(ROOT / "docs" / "icon.png")
    setup_icon(app).resize((256, 256), Image.LANCZOS).save(ROOT / "installer" / "setup.ico", sizes=[(s, s) for s in ICO_SIZES])
    app.resize((165, 165), Image.LANCZOS).save(ROOT / "installer" / "wizard-small.png")
    wizard_large().save(ROOT / "installer" / "wizard-large.png")
    print("wrote app.ico, icon.png, setup.ico, wizard-small.png, wizard-large.png")


if __name__ == "__main__":
    main()
