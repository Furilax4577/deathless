"""Plan du village et façades des bâtiments, dessinés À L'ÉCHELLE à partir des mesures de Docs/da/maisons-plans.md
et Docs/da/taverne-plan.md. Ces gabarits servent de base à Grok (grok_image.py --depuis) : il habille, il ne
choisit pas les proportions.

    python Docs/outils/plan_village.py        écrit Docs/da/gabarits/plan-village.png et facades.png, et vérifie
                                              les écarts (bâtiments entre eux, rivière, couloirs, axe nord)

Repère : Nyxessa à l'origine, x vers l'est, y vers le nord, en mètres. Pillow requis (déjà installé).
"""
import math
import os
import sys
from PIL import Image, ImageDraw, ImageFont

RACINE = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
SORTIE = os.path.join(RACINE, "Docs", "da", "gabarits")

# ---------------------------------------------------------------- mesures (m)
PERSONNAGE = 2.3
# nom : (centre x, y ; largeur de façade, profondeur ; hauteur de mur, annexe, teinte du toit)
BATIMENTS = {
    "Taverne":  dict(c=(-31, -16), l=16.6, p=11.6, mur=5.4, toit="#b4483a"),
    "Sorcier":  dict(c=(-27, 28), l=11.6, p=9.6, mur=4.9, toit="#2f4a7a"),
    "Druide":   dict(c=(21, 28), l=11.6, p=8.6, mur=4.9, toit="#7d8a4a"),
    "Forge":    dict(c=(32, 13), l=11.6, p=9.6, mur=4.9, toit="#8a5a3c"),
    "Mecano":   dict(c=(27, -17), l=12.6, p=9.6, mur=4.9, toit="#b4483a"),
    "Maison":   dict(c=(-37, 10), l=8.6, p=7.1, mur=4.4, toit="#c0553f"),
}
RIVIERE = [(0, 36), (6, 28), (12, 18), (13, 6), (12.5, 0), (12, -6), (6, -15), (-6, -20), (-20, -30), (-40, -48), (-64, -70)]
LARGEUR_RIVIERE = 4.5
PONTS = [((12.6, 0), 0), ((0, -17.8), 70)]          # centre, lacet du tablier (degrés)
# Passages en eau basse : un par maison de la rive est (druide, forge, mécano), pour ne pas dépendre du seul pont est.
GUES = [(9, 23.5), (12.8, 11), (9, -10.5)]
GUE = GUES[-1]
BASSIN, GROTTE = (0, 38), (-14, 40)
FALAISE_Y = 40
CLAIRIERES = [(70, 0), (0, -70), (-70, 0)]
PLATEAU, ANNEAU = 4.2, 8.0
# Sentiers d'attaque en terre battue (7 m de large), de chaque clairière à l'anneau pavé ; route de la grotte (4 m).
SENTIERS = [[(78, 0), (16, 0)], [(0, -78), (0, -20)], [(-78, 0), (-9, 0)]]
LARGEUR_SENTIER, LARGEUR_ROUTE_GROTTE = 7.0, 4.0


def coins(b, marge=0.0):
    """Coins du bâtiment, façade tournée vers Nyxessa."""
    (cx, cy), l, p = b["c"], b["l"] + 2 * marge, b["p"] + 2 * marge
    a = math.atan2(-cy, -cx)                      # direction vers le centre
    ux, uy = math.cos(a), math.sin(a)             # profondeur
    vx, vy = -uy, ux                              # façade
    return [(cx + sx * vx * l / 2 + sy * ux * p / 2, cy + sx * vy * l / 2 + sy * uy * p / 2)
            for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]


def riviere_dense(pas=0.5):
    pts = []
    for (x0, y0), (x1, y1) in zip(RIVIERE, RIVIERE[1:]):
        n = max(1, int(math.hypot(x1 - x0, y1 - y0) / pas))
        pts += [(x0 + (x1 - x0) * k / n, y0 + (y1 - y0) * k / n) for k in range(n)]
    return pts + [RIVIERE[-1]]


def dans(poly, x, y):
    ok = False
    for (x0, y0), (x1, y1) in zip(poly, poly[1:] + poly[:1]):
        if (y0 > y) != (y1 > y) and x < x0 + (y - y0) * (x1 - x0) / (y1 - y0):
            ok = not ok
    return ok


def verifier():
    """Écarts minimaux ; renvoie les lignes du rapport."""
    lignes, riv = [], riviere_dense()
    noms = list(BATIMENTS)
    for n in noms:
        b = BATIMENTS[n]
        r = math.hypot(*b["c"])
        az = (math.degrees(math.atan2(b["c"][0], b["c"][1])) + 360) % 360
        poly = coins(b)
        bord = [(x0 + (x1 - x0) * k / 20, y0 + (y1 - y0) * k / 20)
                for (x0, y0), (x1, y1) in zip(poly, poly[1:] + poly[:1]) for k in range(20)]
        d_riv = min(math.hypot(x - rx, y - ry) for x, y in bord for rx, ry in riv) - LARGEUR_RIVIERE / 2
        couloir = min(min(abs(y) for x, y in bord if abs(x) > 8) if any(abs(x) > 8 for x, y in bord) else 99,
                      min(abs(x) for x, y in bord if y < -8) if any(y < -8 for x, y in bord) else 99)
        axe = min((abs(x) for x, y in bord if y > 0), default=99)
        falaise = FALAISE_Y - max(y for x, y in bord)
        proche = min((min(math.hypot(x - u, y - v) for x, y in bord for u, v in coins(BATIMENTS[m])), m)
                     for m in noms if m != n)
        lignes.append("%-8s r=%4.1f m, azimut %3.0f° ; rivière %4.1f m ; couloir de vague %4.1f m ; axe nord %4.1f m ; "
                      "falaise %4.1f m ; voisin %s à %4.1f m" % (n, r, az, d_riv, couloir, axe, falaise, proche[1], proche[0]))
    return lignes


# ---------------------------------------------------------------- dessin
def police(t):
    for nom in ("arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(nom, t)
        except OSError:
            pass
    return ImageFont.load_default()


def plan(taille=1024, demi=80.0, legendes=True):
    e = taille / (2 * demi)
    P = lambda x, y: (taille / 2 + x * e, taille / 2 - y * e)
    img = Image.new("RGB", (taille, taille), "#7fa65b")
    d = ImageDraw.Draw(img)
    # lande à l'est, forêt au sud et à l'ouest (taches), falaise au nord
    d.polygon([P(16, 40), P(80, 40), P(80, -30), P(40, -34), P(18, -8), P(18, 8)], fill="#c8b882")
    d.rectangle([P(-80, 80), P(80, FALAISE_Y)], fill="#8a8d94")
    d.polygon([P(-80, 40), P(-60, 36), P(-30, 39), P(-6, 35), P(6, 35), P(34, 38), P(60, 35), P(80, 40)], fill="#9a9ea6")
    import random
    rnd = random.Random(7)
    for _ in range(420):
        x, y = rnd.uniform(-80, 80), rnd.uniform(-80, 38)
        if math.hypot(x, y) < 46 or x > 14 and y > -36 or abs(y) < 8 and x < 0 or abs(x) < 8 and y < 0 or (x < 0 and y > 0 and abs(x * 40 + y * 14) / 42.4 < 9):
            continue
        if min(math.hypot(x - rx, y - ry) for rx, ry in RIVIERE_DENSE[::6]) < 6:
            continue
        r = rnd.uniform(2.0, 3.4)
        d.ellipse([P(x - r, y + r), P(x + r, y - r)], fill=rnd.choice(["#3f7a3a", "#4f8f44", "#2f6a4a", "#5d9a3c"]))
    for cx, cy in CLAIRIERES:
        d.ellipse([P(cx - 7, cy + 7), P(cx + 7, cy - 7)], fill="#a9825a")
    for a, b in SENTIERS:
        d.line([P(*a), P(*b)], fill="#b08a5e", width=int(LARGEUR_SENTIER * e))
    # rivière, bassin, gué
    d.line([P(*p) for p in RIVIERE_DENSE], fill="#3f7fb3", width=int(LARGEUR_RIVIERE * e), joint="curve")
    d.ellipse([P(BASSIN[0] - 4, BASSIN[1] + 4), P(BASSIN[0] + 4, BASSIN[1] - 4)], fill="#3f7fb3")
    d.rectangle([P(-1.5, 60), P(1.5, BASSIN[1])], fill="#8fc3ea")
    for gx, gy in GUES:
        d.ellipse([P(gx - 3.2, gy + 3.2), P(gx + 3.2, gy - 3.2)], fill="#a9d4ee")
    # grotte
    d.ellipse([P(GROTTE[0] - 4.5, GROTTE[1] + 5), P(GROTTE[0] + 4.5, GROTTE[1] - 3)], fill="#22262b")
    d.ellipse([P(GROTTE[0] - 2.2, GROTTE[1] + 3), P(GROTTE[0] + 2.2, GROTTE[1] - 1.4)], fill="#3fd06a")
    # allées, anneau pavé, plateau
    pave = "#cfc7b2"
    cibles = [b["c"] for b in BATIMENTS.values()] + [GROTTE, (12.6, 0), (0, -17.8)]
    for cx, cy in cibles:
        r = math.hypot(cx, cy)
        fin = 1 - (2 if (cx, cy) == GROTTE else 6) / r
        d.line([P(cx * ANNEAU / r, cy * ANNEAU / r), P(cx * fin, cy * fin)], fill=pave,
               width=int((LARGEUR_ROUTE_GROTTE if (cx, cy) == GROTTE else 2.2) * e))
    d.ellipse([P(-ANNEAU, ANNEAU), P(ANNEAU, -ANNEAU)], fill=pave)
    oct8 = [P(PLATEAU * math.cos(math.radians(22.5 + 45 * k)), PLATEAU * math.sin(math.radians(22.5 + 45 * k))) for k in range(8)]
    d.polygon(oct8, fill="#6f757c")
    d.ellipse([P(-1, 1), P(1, -1)], fill="#3fd06a")
    # ponts
    for (cx, cy), lacet in PONTS:
        a = math.radians(lacet)
        ux, uy, vx, vy = math.cos(a), math.sin(a), -math.sin(a), math.cos(a)
        d.polygon([P(cx + sx * ux * 4 + sy * vx * 1.5, cy + sx * uy * 4 + sy * vy * 1.5)
                   for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))], fill="#9a6a3c")
    # bâtiments (toit : deux pans, faîtage parallèle à la façade)
    for nom, b in BATIMENTS.items():
        c = coins(b)
        d.polygon([P(*p) for p in c], fill=b["toit"], outline="#3a2a20")
        m0 = ((c[0][0] + c[3][0]) / 2, (c[0][1] + c[3][1]) / 2)
        m1 = ((c[1][0] + c[2][0]) / 2, (c[1][1] + c[2][1]) / 2)
        d.line([P(*m0), P(*m1)], fill="#3a2a20", width=2)
        if legendes:
            d.text(P(b["c"][0], b["c"][1]), nom, fill="white", font=police(15), anchor="mm", stroke_width=2, stroke_fill="#22262b")
    # annexes : appentis de la forge, tour du sorcier, jardin et puits de la maison
    fx, fy = BATIMENTS["Forge"]["c"]
    d.polygon([P(fx + 3, fy + 9), P(fx + 8, fy + 6.5), P(fx + 5, fy + 0.5), P(fx + 0, fy + 3)], fill="#6e4a30", outline="#3a2a20")
    sx, sy = BATIMENTS["Sorcier"]["c"]
    d.ellipse([P(sx - 7.7, sy + 8.2), P(sx - 3.2, sy + 3.7)], fill="#24365c", outline="#3a2a20")
    mx, my = BATIMENTS["Maison"]["c"]
    d.rectangle([P(mx - 12, my + 4), P(mx - 6, my - 1)], fill="#6b4a2c", outline="#3a2a20")
    d.ellipse([P(mx - 10, my + 8), P(mx - 8, my + 6)], fill="#8a8d94", outline="#3a2a20")
    if legendes:
        f = police(14)
        d.text(P(0, 46), "Cascade", fill="white", font=f, anchor="mm", stroke_width=2, stroke_fill="#22262b")
        d.text(P(-14, 47), "Grotte du portail", fill="white", font=f, anchor="mm", stroke_width=2, stroke_fill="#22262b")
        d.text(P(0, -3), "Nyxessa", fill="white", font=f, anchor="mm", stroke_width=2, stroke_fill="#22262b")
        for gx, gy in GUES:
            d.text(P(gx + 7, gy), "Gué", fill="white", font=f, anchor="mm", stroke_width=2, stroke_fill="#22262b")
        d.line([P(-76, -76), P(-56, -76)], fill="white", width=3)
        d.text(P(-66, -73), "20 m", fill="white", font=f, anchor="mm", stroke_width=2, stroke_fill="#22262b")
        d.text(P(74, 74), "N", fill="white", font=police(22), anchor="mm", stroke_width=2, stroke_fill="#22262b")
    return img


def facades(largeur=1280, hauteur=720, legendes=True):
    """Six façades à la même échelle, sur deux rangs, avec un personnage de 2,3 m devant chacune."""
    img = Image.new("RGB", (largeur, hauteur), "#dfe6ee")
    d = ImageDraw.Draw(img)
    e = 19.0                                      # pixels par mètre
    rangs = [["Taverne", "Forge", "Sorcier"], ["Mecano", "Druide", "Maison"]]
    annexes = {"Forge": 5.0, "Sorcier": 4.5, "Druide": 2.0}
    for i, rang in enumerate(rangs):
        sol = 340 + i * 350
        d.rectangle([0, sol, largeur, sol + 30], fill="#9bbf7a")
        total = sum(BATIMENTS[n]["l"] + annexes.get(n, 0) for n in rang) + 5 * (len(rang) - 1) + 3
        x = (largeur - total * e) / 2 + 3 * e
        for n in rang:
            b = BATIMENTS[n]
            l, mur = b["l"] * e, b["mur"] * e
            pente = 50 if n == "Druide" else 45
            toit = b["p"] / 2 * math.tan(math.radians(pente)) * e
            # dalle, mur, toit vu de face (faîtage parallèle à la façade : un bandeau, débord de 0,5 m)
            d.rectangle([x - 0.4 * e, sol - 0.38 * e, x + l + 0.4 * e, sol], fill="#8a9099")
            d.rectangle([x, sol - mur, x + l, sol - 0.38 * e], fill="#efe6cf", outline="#5a3a2a", width=3)
            d.polygon([(x - 0.5 * e, sol - mur), (x + l + 0.5 * e, sol - mur), (x + l + 0.5 * e, sol - mur - toit),
                       (x - 0.5 * e, sol - mur - toit)], fill=b["toit"], outline="#3a2a20")
            # porte (double pour la taverne) et fenêtre
            pl, ph = (3.2, 2.8) if n == "Taverne" else (1.8, 2.6)
            px = x + l * 0.32 - pl * e / 2
            d.rectangle([px, sol - 0.38 * e - ph * e, px + pl * e, sol - 0.38 * e], fill="#8b5a32", outline="#6f757c", width=4)
            fl = 3.5 if n == "Mecano" else 1.2
            d.rectangle([x + l * 0.68 - fl * e / 2, sol - 3.0 * e, x + l * 0.68 + fl * e / 2, sol - 1.5 * e], fill="#9cc4e6", outline="#5a3a2a", width=3)
            if n in ("Taverne", "Mecano"):         # étage en façade : fenêtres hautes
                for k in (0.25, 0.75):
                    d.rectangle([x + l * k - 0.6 * e, sol - mur + 0.5 * e, x + l * k + 0.6 * e, sol - mur + 1.7 * e], fill="#9cc4e6", outline="#5a3a2a", width=3)
            xa = x + l
            if n == "Forge":                       # appentis ouvert
                d.polygon([(xa, sol - 4.5 * e), (xa + 5 * e, sol - 3.4 * e), (xa + 5 * e, sol - 3.1 * e), (xa, sol - 4.2 * e)], fill="#6e4a30")
                d.rectangle([xa + 4.6 * e, sol - 3.1 * e, xa + 4.9 * e, sol], fill="#5a3a2a")
                d.rectangle([xa + 0.8 * e, sol - 2.2 * e, xa + 3 * e, sol], fill="#6f757c")
                d.ellipse([xa + 1.4 * e, sol - 1.5 * e, xa + 2.4 * e, sol - 0.5 * e], fill="#ff8a2a")
            if n == "Sorcier":                     # tour ronde et toit conique
                d.rectangle([xa - 0.5 * e, sol - 7.5 * e, xa + 4 * e, sol], fill="#e8d9a8", outline="#5a3a2a", width=3)
                d.polygon([(xa - 1 * e, sol - 7.5 * e), (xa + 4.5 * e, sol - 7.5 * e), (xa + 1.75 * e, sol - 12 * e)], fill="#24365c")
                d.ellipse([xa + 1.1 * e, sol - 6.4 * e, xa + 2.4 * e, sol - 5.1 * e], fill="#9cc4e6", outline="#5a3a2a", width=3)
            if n == "Druide":                      # auvent du séchoir
                d.polygon([(xa, sol - 3.4 * e), (xa + 2 * e, sol - 2.8 * e), (xa + 2 * e, sol - 2.6 * e), (xa, sol - 3.2 * e)], fill="#4a5a3a")
                d.rectangle([xa + 1.7 * e, sol - 2.6 * e, xa + 1.9 * e, sol], fill="#5a3a2a")
            if n == "Maison":                      # puits
                d.rectangle([xa + 2 * e, sol - 1 * e, xa + 4 * e, sol], fill="#8a8d94")
                d.polygon([(xa + 1.7 * e, sol - 2.6 * e), (xa + 4.3 * e, sol - 2.6 * e), (xa + 3 * e, sol - 3.5 * e)], fill="#6e4a30")
            # personnage de 2,3 m, tête = 46 %
            gx = x - 2.2 * e
            d.rectangle([gx, sol - PERSONNAGE * 0.54 * e, gx + 1.0 * e, sol], fill="#b33a3a")
            d.ellipse([gx - 0.1 * e, sol - PERSONNAGE * e, gx + 1.1 * e, sol - PERSONNAGE * 0.5 * e], fill="#aeb4bc")
            if legendes:
                d.text((x + l / 2, sol + 15), "%s  %.1f m × %.1f m, faîtage %.1f m" % (n, b["l"], b["p"], (mur + toit) / e),
                       fill="#22262b", font=police(15), anchor="mm")
            x += (b["l"] + annexes.get(n, 0) + 5) * e
    return img


RIVIERE_DENSE = riviere_dense()

# Variante serpentine (01/10/2026, à la demande de Quentin : « j'aimais bien l'idée de la rivière qui serpente, avec les
# points de passage dans l'eau »). Même logique que le plan décidé (elle longe le plateau côté est, trois gués devant les
# maisons de la rive est, deux ponts, sortie au sud-ouest), mais avec de vraies boucles. Le plan décidé reste le défaut :
# `serpente()` remplace le tracé, les gués et les ponts.
def _queue_serpentine(debut=(-4, -21), fin=(-64, -70), tours=2.5, amplitude=6.5, n=36):
    """Queue sud-ouest de la rivière : ondulations nettes dans la forêt (aucun bâtiment de ce côté)."""
    L = math.hypot(fin[0] - debut[0], fin[1] - debut[1])
    ux, uy = (fin[0] - debut[0]) / L, (fin[1] - debut[1]) / L
    vx, vy = -uy, ux
    pts = []
    for k in range(1, n + 1):
        t = k / n
        off = amplitude * math.sin(2 * math.pi * t * tours) * min(1.0, t * 4)
        pts.append((round(debut[0] + ux * L * t + vx * off, 1), round(debut[1] + uy * L * t + vy * off, 1)))
    return pts


RIVIERE_SERPENTE = [(0, 36), (4, 32), (8, 27), (10.5, 22), (15, 17.5), (18, 12), (15, 6.5), (13, 1), (14.5, -4.5), (17, -9),
                    (15, -14), (10, -18), (4, -21.5), (-4, -21)] + _queue_serpentine()


def serpente():
    """Bascule le module sur la rivière serpentine ; gués au point de la rivière le plus proche de chaque maison de la
    rive est (druide, forge, mécano), ponts là où la rivière coupe le sentier est (y = 0) et le sentier sud (x = 0)."""
    global RIVIERE, RIVIERE_DENSE, GUES, GUE, PONTS
    RIVIERE = RIVIERE_SERPENTE
    RIVIERE_DENSE = riviere_dense()
    GUES = []
    for nom in ("Druide", "Forge", "Mecano"):
        cx, cy = BATIMENTS[nom]["c"]
        GUES.append(min(RIVIERE_DENSE, key=lambda p: math.hypot(p[0] - cx, p[1] - cy)))
    GUE = GUES[-1]
    est = min((p for p in RIVIERE_DENSE if p[0] > 5), key=lambda p: abs(p[1]))
    sud = min((p for p in RIVIERE_DENSE if p[1] < -10), key=lambda p: abs(p[0]))
    i = RIVIERE_DENSE.index(sud)
    a, b = RIVIERE_DENSE[max(0, i - 3)], RIVIERE_DENSE[min(len(RIVIERE_DENSE) - 1, i + 3)]
    lacet = math.degrees(math.atan2(b[1] - a[1], b[0] - a[0])) + 90       # tablier en travers de la rivière
    PONTS = [(est, 0), (sud, lacet)]


if __name__ == "__main__":
    if "--serpente" in sys.argv:
        serpente()
    os.makedirs(SORTIE, exist_ok=True)
    suffixe = "-serpente" if "--serpente" in sys.argv else ""
    plan().save(os.path.join(SORTIE, "plan-village" + suffixe + ".png"))
    plan(legendes=False).save(os.path.join(SORTIE, "plan-village" + suffixe + "-muet.png"))
    if not suffixe:
        facades().save(os.path.join(SORTIE, "facades.png"))
        facades(legendes=False).save(os.path.join(SORTIE, "facades-muet.png"))
    for l in verifier():
        print(l)
