"""Draws the app icon (Browser-Selector/app.ico) at every Windows size.

Run: python tools/make-icon.py   (needs Pillow)
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Browser-Selector" / "app.ico"
S = 1024  # draw big, then downsample


def lerp(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def draw():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))

    # Rounded square with a vertical blue gradient.
    grad = Image.new("RGBA", (S, S))
    gd = ImageDraw.Draw(grad)
    top, bottom = (56, 132, 255, 255), (28, 76, 196, 255)
    for y in range(S):
        gd.line([(0, y), (S, y)], fill=lerp(top, bottom, y / S))
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).rounded_rectangle([40, 40, S - 40, S - 40], radius=220, fill=255)
    img.paste(grad, (0, 0), mask)

    d = ImageDraw.Draw(img)
    white = (255, 255, 255, 255)
    w = 46

    # Globe on the left.
    cx, cy, r = 400, 512, 250
    d.ellipse([cx - r, cy - r, cx + r, cy + r], outline=white, width=w)
    d.ellipse([cx - r * 0.45, cy - r, cx + r * 0.45, cy + r], outline=white, width=w)
    d.line([cx - r, cy, cx + r, cy], fill=white, width=w)

    # The link forks to the right and ends in two choices.
    sx, sy = cx + r, cy
    for dy in (-1, 1):
        ex, ey = 800, cy + dy * 230
        d.line([sx, sy, ex, ey], fill=white, width=w)
        d.ellipse([ex - 62, ey - 62, ex + 62, ey + 62], fill=white)
    d.ellipse([sx - 34, sy - 34, sx + 34, sy + 34], fill=white)
    return img


def main():
    big = draw()
    sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]
    big.resize((256, 256), Image.LANCZOS).save(OUT, sizes=[(s, s) for s in sizes])
    big.resize((256, 256), Image.LANCZOS).save(ROOT / "docs" / "icon.png")
    print("wrote", OUT)


if __name__ == "__main__":
    main()
