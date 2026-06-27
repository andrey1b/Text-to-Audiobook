"""Генератор иконки для Text to Audiobook."""
from PIL import Image, ImageDraw
import math, os

ACCENT   = (255, 255, 255)   # белые наушники
BG_TOP   = ( 62, 142,  65)   # #3E8E41 — светлее зелёный
BG_BOT   = ( 44,  95,  45)   # #2C5F2D — садовый зелёный
WHITE    = (255, 255, 255)


def make_frame(size: int) -> Image.Image:
    s = size
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # ── Фон с градиентом ──────────────────────────────────────────────
    for y in range(s):
        t = y / s
        r = int(BG_TOP[0] + (BG_BOT[0] - BG_TOP[0]) * t)
        g = int(BG_TOP[1] + (BG_BOT[1] - BG_TOP[1]) * t)
        b = int(BG_TOP[2] + (BG_BOT[2] - BG_TOP[2]) * t)
        draw.line([(0, y), (s, y)], fill=(r, g, b, 255))

    # Маска скруглённых углов
    cr = max(3, int(s * 0.20))
    mask = Image.new("L", (s, s), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, s - 1, s - 1], radius=cr, fill=255)
    img.putalpha(mask)
    draw = ImageDraw.Draw(img)

    cx, cy = s / 2, s * 0.54
    arc_r  = s * 0.295
    arc_w  = max(2, int(s * 0.085))
    pad_w  = s * 0.125
    pad_h  = s * 0.175

    # ── Наушники: дуга оголовья ────────────────────────────────────────
    draw.arc(
        [cx - arc_r, cy - arc_r * 1.02, cx + arc_r, cy + arc_r * 0.15],
        start=200, end=340,
        fill=(*ACCENT, 255), width=arc_w,
    )

    # ── Левый и правый амбушюр ────────────────────────────────────────
    pad_y   = cy + s * 0.04
    left_x  = cx - arc_r * 0.88
    right_x = cx + arc_r * 0.88
    pr      = max(2, int(pad_w * 0.55))

    for px in (left_x, right_x):
        draw.rounded_rectangle(
            [px - pad_w, pad_y - pad_h, px + pad_w, pad_y + pad_h],
            radius=pr, fill=(*ACCENT, 255),
        )
        # Блик
        if size >= 32:
            hr = max(1, int(s * 0.025))
            hy = pad_y - pad_h * 0.32
            draw.ellipse([px - hr, hy - hr, px + hr, hy + hr],
                         fill=(*WHITE, 160))

    # ── Звуковые волны сверху (начиная с 48 px) ───────────────────────
    if size >= 48:
        wcy = cy - arc_r * 0.88
        for i, rm in enumerate([0.075, 0.125, 0.175]):
            r   = s * rm
            alp = 210 - i * 55
            lw  = max(1, int(s * 0.038 - i * s * 0.005))
            draw.arc(
                [cx - r, wcy - r * 0.75, cx + r, wcy + r * 0.75],
                start=225, end=315,
                fill=(*WHITE, alp), width=lw,
            )

    return img


def save_ico(path: str) -> None:
    sizes  = [16, 24, 32, 48, 64, 128, 256]
    frames = [make_frame(s) for s in sizes]
    frames[0].save(
        path, format="ICO",
        sizes=[(s, s) for s in sizes],
        append_images=frames[1:],
    )
    print(f"Сохранено: {path}")


if __name__ == "__main__":
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "app_icon.ico")
    save_ico(out)
