"""Élévation à l'échelle de la pièce héros de la montagne (face de falaise : grotte du portail, cascade, bassin), vue de face,
projection orthogonale, pour Grok (--depuis) ou comme gabarit de cotes.

    python Docs/outils/montagne_face.py      écrit Docs/da/gabarits/montagne-face.png (+ montagne-face-muet.png)

Cotes (plan du village, Docs/outils/plan_village.py) : la pièce fait 34 m de large (x de -24 à +10) sur 18 m de haut (premier
gradin de la falaise) ; la grotte est à x = -14 (8 m de large, 5 m de haut, 6 m de profondeur) ; la cascade à x = 0 (3 m de
large) tombe dans le bassin (8 m de diamètre) ; un personnage de 2,3 m donne l'échelle."""
import os
from PIL import Image, ImageDraw, ImageFont

RACINE = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
SORTIE = os.path.join(RACINE, "Docs", "da", "gabarits")
E = 40.0                               # pixels par mètre
L, H = 1600, 900
X0, SOL = 120, 820                     # pixel du x = -24 m et du sol


def px(x, y):                          # x en mètres (-24 .. +10), y en mètres au-dessus du sol
    return (X0 + (x + 24) * E, SOL - y * E)


def police(t):
    for n in ("arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(n, t)
        except OSError:
            pass
    return ImageFont.load_default()


def dessiner(legendes=True):
    img = Image.new("RGB", (L, H), "#dfe6ee")
    d = ImageDraw.Draw(img)
    # sol
    d.rectangle([0, SOL, L, H], fill="#7fa65b")
    # face de falaise : bord supérieur en blocs arrondis (profil irrégulier), 18 m de haut en moyenne
    haut = [(-24, 6), (-23, 12), (-21, 16), (-17, 18.5), (-12, 17), (-7, 19), (-3, 18), (0, 15.5), (3, 18.5), (7, 17), (10, 9), (10, 0)]
    poly = [px(-24, 0)] + [px(x, y) for x, y in haut]
    d.polygon(poly, fill="#8a8d94", outline="#4a4f55")
    # lits de blocs (strates horizontales indicatives)
    for y in (4, 8, 12, 16):
        d.line([px(-23, y), px(10, y)], fill="#7a7d84", width=2)
    # grotte : 8 m de large, 5 m de haut, arche, centre x = -14
    cave = [px(-18, 0), px(-18, 3.2)] + [px(-14 + 4 * __import__("math").cos(a / 10 * 3.14159), 3.2 + 1.8 * __import__("math").sin(a / 10 * 3.14159)) for a in range(10, -1, -1)] + [px(-10, 0)]
    d.polygon(cave, fill="#22262b", outline="#14171a")
    d.ellipse([px(-16.2, 3.6)[0], px(0, 3.6)[1], px(-11.8, 0)[0], px(0, 0.4)[1]], fill="#3fd06a")     # disque du portail
    # cascade : 3 m de large, du haut de la face jusqu'au bassin
    d.rectangle([px(-1.5, 15.5)[0], px(0, 15.5)[1], px(1.5, 0)[0], px(0, 0.5)[1]], fill="#8fc3ea", outline="#5b8db5")
    # bassin : 8 m de diamètre, devant la falaise
    d.ellipse([px(-4, 0)[0], px(0, 1.2)[1], px(4, 0)[0], px(0, -1.6)[1]], fill="#3f7fb3", outline="#2c5c82")
    # seuil pavé devant la grotte
    d.rectangle([px(-18.5, 0)[0], px(0, 0)[1], px(-9.5, 0)[0], px(0, -0.5)[1]], fill="#cfc7b2", outline="#8f877a")
    # personnage de 2,3 m (tête = 46 %) pour l'échelle
    gx = px(-21.5, 0)[0]
    d.rectangle([gx, SOL - 1.25 * E, gx + 1.0 * E, SOL], fill="#b33a3a")
    d.ellipse([gx - 0.1 * E, SOL - 2.3 * E, gx + 1.1 * E, SOL - 1.2 * E], fill="#aeb4bc")
    if legendes:
        f = police(22)
        d.text((px(-7, 0)[0], 40), "Pièce héros : 34 m × 18 m (x de -24 à +10 m)", fill="#22262b", font=f, anchor="mm")
        d.text(px(-14, 7.2), "grotte 8 × 5 m (profondeur 6 m)", fill="#f4f4f4", font=f, anchor="mm")
        d.text(px(-14, 6.3), "portail vert au fond", fill="#f4f4f4", font=f, anchor="mm")
        d.text(px(6.2, 9.0), "cascade 3 m", fill="#f4f4f4", font=f, anchor="mm")
        d.text(px(6.2, 8.1), "bassin Ø 8 m", fill="#f4f4f4", font=f, anchor="mm")
        d.text(px(-21.5, 3.0), "2,3 m", fill="#f4f4f4", font=f, anchor="mm")
        for k in range(0, 36, 4):                      # règle en mètres le long du sol
            d.line([px(-24 + k, -0.2), px(-24 + k, -0.9)], fill="#22262b", width=2)
            d.text(px(-24 + k, -1.5), str(k), fill="#22262b", font=police(16), anchor="mm")
    return img


if __name__ == "__main__":
    os.makedirs(SORTIE, exist_ok=True)
    dessiner().save(os.path.join(SORTIE, "montagne-face.png"))
    dessiner(legendes=False).save(os.path.join(SORTIE, "montagne-face-muet.png"))
    print("Docs/da/gabarits/montagne-face.png")
