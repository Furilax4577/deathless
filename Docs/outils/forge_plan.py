"""Plan de circulation de la forge à l'échelle, vue de dessus : maison (2/3) + atelier extérieur ouvert (1/3) avec le foyer
contre le mur de la maison, l'enclume et le bac de trempe (postes séparés et repositionnables), et la boucle du forgeron.

    python Docs/outils/forge_plan.py      écrit Docs/da/gabarits/forge-plan.png

Repère : x vers l'est, y vers le nord, en mètres ; origine au coin sud-ouest de la maison. Façade au sud."""
import math
import os
from PIL import Image, ImageDraw, ImageFont

RACINE = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
SORTIE = os.path.join(RACINE, "Docs", "da", "gabarits")
E = 52.0                                   # pixels par mètre
L, H = 1220, 760
X0, Y0 = 60, 690                           # pixel de (0, 0) ; y vers le haut

MAISON = (0.0, 0.0, 11.6, 9.6)             # x0, y0, x1, y1
ATELIER = (11.6, 0.0, 17.4, 9.6)           # 5,8 m de large : le tiers de la forge
FOYER = (11.6, 6.4, 13.8, 8.8)             # contre le mur est de la maison (2,2 × 2,4 m), hotte et cheminée
ENCLUME = (15.2, 4.6)                      # centre ; billot Ø 0,8 m
BAC = (15.2, 1.6)                          # centre ; bac 1,6 × 0,8 m, long côté nord-sud
ESPACE = 1.5                               # passage libre mini autour de chaque poste


def px(x, y):
    return (X0 + x * E, Y0 - y * E)


def police(t):
    for n in ("arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(n, t)
        except OSError:
            pass
    return ImageFont.load_default()


def dessiner():
    img = Image.new("RGB", (L, H), "#eef1ea")
    d = ImageDraw.Draw(img)
    f, g = police(17), police(14)
    # sol de l'atelier (dalles)
    d.rectangle([px(ATELIER[0], ATELIER[3]), px(ATELIER[2], ATELIER[1])], fill="#c9c4b4", outline="#7a756a", width=2)
    # maison
    d.rectangle([px(MAISON[0], MAISON[3]), px(MAISON[2], MAISON[1])], fill="#e9dcc2", outline="#5a3a2a", width=4)
    d.text(px(5.8, 7.6), "MAISON 11,6 × 9,6 m (2/3)", fill="#3a2a20", font=f, anchor="mm")
    d.text(px(5.8, 7.0), "intérieur : zone à part (fondu au noir)", fill="#6a5a4a", font=g, anchor="mm")
    d.rectangle([px(2.0, 0.05), px(3.8, -0.25)], fill="#8b5a32")
    d.text(px(2.9, -0.7), "porte 1,8 m", fill="#3a2a20", font=g, anchor="mm")
    # atelier : toit à pente unique sur quatre poteaux d'angle (aux extrémités, hors des postes)
    d.rectangle([px(ATELIER[0] + 0.1, ATELIER[3] - 0.1), px(ATELIER[2] - 0.1, ATELIER[1] + 0.1)], outline="#6e4a30", width=3)
    for x, y in ((ATELIER[2] - 0.35, 0.35), (ATELIER[2] - 0.35, ATELIER[3] - 0.35), (ATELIER[0] + 2.6, ATELIER[3] - 0.35), (ATELIER[0] + 2.6, 0.35)):
        d.ellipse([px(x - 0.25, y + 0.25), px(x + 0.25, y - 0.25)], fill="#5a3a2a")
    d.text(px(14.5, 10.0), "ATELIER OUVERT 5,8 × 9,6 m (1/3), sans murs, toit bas", fill="#3a2a20", font=g, anchor="mm")
    # foyer
    d.rectangle([px(FOYER[0], FOYER[3]), px(FOYER[2], FOYER[1])], fill="#8a8d94", outline="#4a4f55", width=3)
    d.rectangle([px(FOYER[0] + 0.3, FOYER[3] - 0.4), px(FOYER[2] - 0.3, FOYER[1] + 0.4)], fill="#2b2b2e")
    d.text(px(12.7, 7.6), "foyer\n(feu en effet)", fill="#f4f4f4", font=g, anchor="mm", align="center")
    # postes séparés : enclume et bac
    for (cx, cy), r, nom, col in ((ENCLUME, 0.5, "enclume\nsur billot", "#4a4f55"), (BAC, 0.0, "bac de\ntrempe", "#7a5a3a")):
        if nom.startswith("bac"):
            d.rectangle([px(cx - 0.4, cy + 0.8), px(cx + 0.4, cy - 0.8)], fill=col, outline="#3a2a20", width=3)
        else:
            d.ellipse([px(cx - 0.4, cy + 0.4), px(cx + 0.4, cy - 0.4)], fill="#7a5a3a", outline="#3a2a20", width=3)
            d.rectangle([px(cx - 0.55, cy + 0.2), px(cx + 0.55, cy - 0.2)], fill=col)
        d.text(px(cx, cy - 1.15), nom, fill="#22262b", font=g, anchor="mm", align="center")
        d.ellipse([px(cx - ESPACE, cy + ESPACE), px(cx + ESPACE, cy - ESPACE)], outline="#c06a3a", width=1)
    # postes du forgeron (où il se tient, face à son poste) : la rangée de gauche, dans la bande libre de 2,8 m
    postes = [("Chauffe", (14.7, 7.6), "#e2a02a"), ("Frappe", (14.0, 4.6), "#d05a2a"), ("Trempe", (14.0, 1.6), "#3a7fb3")]
    for nom, (x, y), c in postes:
        d.ellipse([px(x - 0.35, y + 0.35), px(x + 0.35, y - 0.35)], fill=c, outline="#22262b", width=2)
    d.text(px(13.6, 8.95), "poste CHAUFFE", fill="#7a5a10", font=g, anchor="mm")
    d.text(px(12.8, 4.6), "poste FRAPPE", fill="#8a3a10", font=g, anchor="mm")
    d.text(px(12.8, 1.6), "poste TREMPE", fill="#1f5a8a", font=g, anchor="mm")
    # boucle du forgeron : chauffe -> frappe -> trempe, puis retour par la bande ouest ; passage de 1,5 m mini
    boucle = [(14.7, 7.0), (14.0, 6.0), (14.0, 4.6), (14.0, 3.1), (14.0, 1.6)]
    d.line([px(*p) for p in boucle], fill="#d05a2a", width=int(1.0 * E * 0.35), joint="curve")
    d.text(px(12.7, 6.0), "bande libre\n2,8 m", fill="#8a3a10", font=g, anchor="mm", align="center")
    # distances entre postes
    def dist(a, b):
        return math.hypot(a[0] - b[0], a[1] - b[1])
    d.text(px(14.6, 6.0), "%.1f m" % dist(postes[0][1], postes[1][1]), fill="#22262b", font=g, anchor="mm")
    d.text(px(14.6, 3.1), "%.1f m" % dist(postes[1][1], postes[2][1]), fill="#22262b", font=g, anchor="mm")
    # joueurs : place devant, côté sud
    for i in range(4):
        x = 13.0 + i * 1.35
        d.ellipse([px(x - 0.5, -1.4 + 0.5), px(x + 0.5, -1.4 - 0.5)], fill="#aeb4bc", outline="#22262b")
    d.text(px(15.5, -2.4), "place des 4 joueurs (devant l'atelier)", fill="#22262b", font=g, anchor="mm")
    # personnage forgeron (2,3 m) pour l'échelle
    d.text(px(8.7, -0.8), "façade (sud)", fill="#6a5a4a", font=g, anchor="mm")
    d.text((20, 18), "Forge : plan de circulation (m) — enclume et bac séparés, repositionnables", fill="#22262b", font=police(20))
    # règle
    for k in range(0, 18, 2):
        d.line([px(k, -3.0), px(k, -3.25)], fill="#22262b", width=2)
        d.text(px(k, -3.7), str(k), fill="#22262b", font=g, anchor="mm")
    return img


if __name__ == "__main__":
    os.makedirs(SORTIE, exist_ok=True)
    dessiner().save(os.path.join(SORTIE, "forge-plan.png"))
    print("Docs/da/gabarits/forge-plan.png")
