"""Génère Ressources/deathless.ico : la gemme de Nyxessa du HUD (GemmeNyxessa dans Assets/Scripts/UI/FormesHud.cs),
losange vert #3fae5a avec sa facette claire #9fe870, liseré ardoise #161a24 pour rester lisible sur un fond clair.

Python pur (bibliothèque standard) : rastérisation suréchantillonnée, PNG écrits à la main, conteneur ICO à entrées PNG
(256, 64, 48, 32, 24 et 16 px). Relancer le script redonne exactement le même fichier.

Usage : python Launcher/make_icon.py
"""
import os
import struct
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "Ressources", "deathless.ico")

INK = (0x16, 0x1A, 0x24)
GREEN = (0x3F, 0xAE, 0x5A)
FACET = (0x9F, 0xE8, 0x70)


def inside(poly, x, y):
    """Point dans un polygone convexe (sommets dans l'ordre des aiguilles d'une montre, repère écran)."""
    n = len(poly)
    sign = 0
    for i in range(n):
        x1, y1 = poly[i]
        x2, y2 = poly[(i + 1) % n]
        cross = (x2 - x1) * (y - y1) - (y2 - y1) * (x - x1)
        if cross != 0:
            s = 1 if cross > 0 else -1
            if sign == 0:
                sign = s
            elif s != sign:
                return False
    return True


def render(size, ss=6):
    # Gemme au format de hud-nyx__gemme (33 × 42), centrée, avec une marge.
    h = size * 0.86
    w = h * 33.0 / 42.0
    cx, cy = size / 2.0, size / 2.0
    top, bottom = (cx, cy - h / 2), (cx, cy + h / 2)
    left, right = (cx - w / 2), (cx + w / 2)
    diamond = [top, (right, cy), bottom, (left, cy)]
    facet = [top, (right, cy), (cx, cy + h * 0.07)]
    edge = max(1.0, size * 0.045)
    # Liseré : on agrandit le losange en gardant son centre.
    k = 1.0 + edge / (h / 2)
    outer = [(cx + (x - cx) * k, cy + (y - cy) * k) for x, y in diamond]

    pixels = bytearray()
    for py in range(size):
        pixels.append(0)  # filtre PNG « aucun »
        for px in range(size):
            r = g = b = a = 0.0
            for sy in range(ss):
                for sx in range(ss):
                    x = px + (sx + 0.5) / ss
                    y = py + (sy + 0.5) / ss
                    if inside(facet, x, y):
                        c = FACET
                    elif inside(diamond, x, y):
                        c = GREEN
                    elif inside(outer, x, y):
                        c = INK
                    else:
                        continue
                    r += c[0]
                    g += c[1]
                    b += c[2]
                    a += 1
            n = ss * ss
            if a > 0:
                pixels += bytes((int(r / a + 0.5), int(g / a + 0.5), int(b / a + 0.5), int(255 * a / n + 0.5)))
            else:
                pixels += b"\0\0\0\0"
    return png(size, size, bytes(pixels))


def png(w, h, raw):
    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)

    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")


def main():
    sizes = [256, 64, 48, 32, 24, 16]
    images = [render(s) for s in sizes]
    header = struct.pack("<HHH", 0, 1, len(images))
    offset = 6 + 16 * len(images)
    entries = b""
    for s, data in zip(sizes, images):
        entries += struct.pack("<BBBBHHII", s % 256, s % 256, 0, 0, 1, 32, len(data), offset)
        offset += len(data)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "wb") as f:
        f.write(header + entries + b"".join(images))
    print("écrit", OUT, os.path.getsize(OUT), "octets")


if __name__ == "__main__":
    main()
