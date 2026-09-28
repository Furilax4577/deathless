"""Vue 3D À L'ÉCHELLE du village (projection parallèle, sans perspective : un mètre vaut le même nombre de pixels
partout), tirée des mesures de plan_village.py. Sert de base à Grok (--depuis) pour un rendu low poly qui garde les
tailles : volumes des six bâtiments à leurs cotes, falaise, cascade, grotte, rivière, ponts, gué, quatre personnages
de 2,3 m près de Nyxessa.

    python Docs/outils/plan_village_3d.py      écrit Docs/da/gabarits/village-3d.png
"""
import math
import os
import random
from PIL import Image, ImageDraw

import plan_village as pv

L, H = 1280, 720
E = 9.4            # pixels par mètre
K = 0.55           # écrasement de la profondeur (vue plongeante depuis le sud)
Z = 0.80           # échelle des hauteurs
CX, CY = L / 2, 470


def P(x, y, z=0.0):
    return (CX + x * E, CY - (y * K + z * Z) * E)


def ombre(c, f):
    c = c.lstrip("#")
    return "#%02x%02x%02x" % tuple(max(0, min(255, int(int(c[i:i + 2], 16) * f))) for i in (0, 2, 4))


def batiment(d, b, nom):
    c = pv.coins(b)                       # 0,1 : façade (vers Nyxessa) ; 2,3 : arrière
    mur = b["mur"]
    pente = 50 if nom == "Druide" else 45
    faite = mur + b["p"] / 2 * math.tan(math.radians(pente))
    m0 = ((c[0][0] + c[3][0]) / 2, (c[0][1] + c[3][1]) / 2)     # milieux des pignons
    m1 = ((c[1][0] + c[2][0]) / 2, (c[1][1] + c[2][1]) / 2)
    faces = []
    for i in range(4):
        a, bb = c[i], c[(i + 1) % 4]
        poly = [P(*a), P(*bb), P(*bb, mur), P(*a, mur)]
        if i in (1, 3):                   # pignons : triangle jusqu'au faîtage
            m = m1 if i == 1 else m0
            poly = [P(*a), P(*bb), P(*bb, mur), P(*m, faite), P(*a, mur)]
        faces.append(((a[1] + bb[1]) / 2, poly, "#efe6cf" if i in (0, 2) else "#ddd2b6"))
    faces.append(((c[2][1] + c[3][1]) / 2 + 0.01, [P(*c[3], mur), P(*c[2], mur), P(*m1, faite), P(*m0, faite)], ombre(b["toit"], 0.8)))
    faces.append(((c[0][1] + c[1][1]) / 2 - 0.01, [P(*c[0], mur), P(*c[1], mur), P(*m1, faite), P(*m0, faite)], b["toit"]))
    for _, poly, teinte in sorted(faces, key=lambda f: -f[0]):
        d.polygon(poly, fill=teinte, outline="#3a2a20")
    # porte sur la façade (double pour la taverne), à l'échelle
    pl, ph = (3.2, 2.8) if nom == "Taverne" else (1.8, 2.6)
    fx, fy = c[1][0] - c[0][0], c[1][1] - c[0][1]
    n = math.hypot(fx, fy)
    ux, uy = fx / n, fy / n
    t = b["l"] * 0.35
    a = (c[0][0] + ux * (t - pl / 2), c[0][1] + uy * (t - pl / 2))
    bb = (c[0][0] + ux * (t + pl / 2), c[0][1] + uy * (t + pl / 2))
    if (c[0][1] + c[1][1]) / 2 < b["c"][1]:                     # façade visible (tournée vers le sud)
        d.polygon([P(*a), P(*bb), P(*bb, ph), P(*a, ph)], fill="#8b5a32", outline="#6f757c")


def personnage(d, x, y, teinte):
    d.rectangle([P(x - 0.5, y, 1.25)[0], P(x, y, 1.25)[1], P(x + 0.5, y)[0], P(x, y)[1]], fill=teinte)
    d.ellipse([P(x - 0.6, y, 2.3)[0], P(x, y, 2.3)[1], P(x + 0.6, y)[0], P(x, y, 1.2)[1]], fill="#aeb4bc")


def arbre(d, x, y, rnd):
    h, r = rnd.uniform(7, 11), rnd.uniform(2.2, 3.4)
    d.rectangle([P(x - 0.25, y, 3)[0], P(x, y, 3)[1], P(x + 0.25, y)[0], P(x, y)[1]], fill="#7a4a2c")
    v = rnd.choice(["#3f7a3a", "#4f8f44", "#2f6a4a", "#5d9a3c"])
    if rnd.random() < 0.45:
        d.polygon([P(x - r, y, 2.8), P(x + r, y, 2.8), P(x, y, h)], fill=v)
    else:
        d.ellipse([P(x - r, y, h)[0], P(x, y, h)[1], P(x + r, y)[0], P(x, y, 2.8)[1]], fill=v)


def dessiner():
    img = Image.new("RGB", (L, H), "#bcd6ee")
    d = ImageDraw.Draw(img)
    d.polygon([P(-90, -60), P(90, -60), P(90, 40), P(-90, 40)], fill="#7fa65b")
    d.polygon([P(16, 40), P(90, 40), P(90, -30), P(40, -34), P(18, -8), P(18, 8)], fill="#c8b882")
    # falaise (30 à 38 m de haut), cascade, grotte
    crete = [(-90, 34), (-60, 38), (-30, 33), (-8, 37), (8, 36), (34, 32), (60, 37), (90, 33)]
    d.polygon([P(-90, 40), P(90, 40)] + [P(x, 40, z) for x, z in reversed(crete)], fill="#8a8d94", outline="#5f6369")
    for x, z in crete[1:-1]:
        d.line([P(x, 40, z), P(x + 3, 40, 0)], fill="#74777e", width=2)
    d.polygon([P(-1.5, 40, 36), P(1.5, 40, 36), P(1.5, 40, 0), P(-1.5, 40, 0)], fill="#8fc3ea")
    gx = pv.GROTTE[0]
    d.ellipse([P(gx - 4, 40, 5)[0], P(gx, 40, 5)[1], P(gx + 4, 40)[0], P(gx, 40, -0.5)[1]], fill="#22262b")
    d.ellipse([P(gx - 2, 40, 3.6)[0], P(gx, 40, 3.6)[1], P(gx + 2, 40)[0], P(gx, 40, 0.2)[1]], fill="#3fd06a")
    # rivière, bassin, gué, pavés
    d.line([P(*p) for p in pv.RIVIERE_DENSE], fill="#3f7fb3", width=int(pv.LARGEUR_RIVIERE * E * 0.8), joint="curve")
    d.ellipse([P(-4, 38)[0], P(0, 41)[1], P(4, 38)[0], P(0, 35)[1]], fill="#3f7fb3")
    d.ellipse([P(pv.GUE[0] - 3.2, 0)[0], P(0, pv.GUE[1] + 3.2)[1], P(pv.GUE[0] + 3.2, 0)[0], P(0, pv.GUE[1] - 3.2)[1]], fill="#a9d4ee")
    pave = "#cfc7b2"
    for cx, cy in [b["c"] for b in pv.BATIMENTS.values()] + [pv.GROTTE, (12.6, 0), (0, -17.8), pv.GUE]:
        r = math.hypot(cx, cy)
        d.line([P(cx * pv.ANNEAU / r, cy * pv.ANNEAU / r), P(cx * (1 - 6 / r), cy * (1 - 6 / r))], fill=pave, width=int(2.2 * E * 0.8))
    d.ellipse([P(-pv.ANNEAU, 0)[0], P(0, pv.ANNEAU)[1], P(pv.ANNEAU, 0)[0], P(0, -pv.ANNEAU)[1]], fill=pave)
    for (cx, cy), lacet in pv.PONTS:
        a = math.radians(lacet)
        ux, uy, vx, vy = math.cos(a), math.sin(a), -math.sin(a), math.cos(a)
        d.polygon([P(cx + sx * ux * 4 + sy * vx * 1.5, cy + sx * uy * 4 + sy * vy * 1.5, 0.6)
                   for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))], fill="#9a6a3c", outline="#3a2a20")
    for cx, cy in pv.CLAIRIERES:
        d.ellipse([P(cx - 7, 0)[0], P(0, cy + 7)[1], P(cx + 7, 0)[0], P(0, cy - 7)[1]], fill="#a9825a")
    # objets triés du nord au sud
    rnd = random.Random(7)
    objets = []
    for _ in range(420):
        x, y = rnd.uniform(-88, 88), rnd.uniform(-58, 38)
        if math.hypot(x, y) < 44 or x > 14 and y > -36 or abs(y) < 7 and x < 0 or abs(x) < 7 and y < 0:
            continue
        if min(math.hypot(x - rx, y - ry) for rx, ry in pv.RIVIERE_DENSE[::6]) < 6:
            continue
        objets.append((y, "arbre", (x, y)))
    for nom, b in pv.BATIMENTS.items():
        objets.append((b["c"][1], "batiment", (nom, b)))
    objets.append((0, "plateau", None))
    for k, (x, teinte) in enumerate([(-3.6, "#b33a3a"), (-1.2, "#5a6a8a"), (1.2, "#6a3a8a"), (3.6, "#3a6a3a")]):
        objets.append((-10.5, "perso", (x, -10.5, teinte)))
    for y, genre, v in sorted(objets, key=lambda o: -o[0]):
        if genre == "arbre":
            arbre(d, v[0], v[1], rnd)
        elif genre == "batiment":
            batiment(d, v[1], v[0])
            if v[0] == "Sorcier":                                # tour ronde, toit conique
                x, y0 = v[1]["c"][0] - 7.2, v[1]["c"][1] + 3
                d.rectangle([P(x - 2.25, y0, 7.5)[0], P(x, y0, 7.5)[1], P(x + 2.25, y0)[0], P(x, y0)[1]], fill="#e8d9a8", outline="#3a2a20")
                d.polygon([P(x - 2.8, y0, 7.5), P(x + 2.8, y0, 7.5), P(x, y0, 12)], fill="#24365c")
            if v[0] == "Forge":                                  # appentis
                x, y0 = v[1]["c"][0] + 4, v[1]["c"][1] + 5
                d.polygon([P(x - 2.5, y0 - 3, 3.4), P(x + 2.5, y0 - 3, 3.4), P(x + 2.5, y0 + 3, 4.4), P(x - 2.5, y0 + 3, 4.4)], fill="#6e4a30", outline="#3a2a20")
            if v[0] == "Maison":                                 # potager et puits
                x, y0 = v[1]["c"]
                d.polygon([P(x - 11, y0 - 3), P(x - 5, y0 - 3), P(x - 5, y0 - 8), P(x - 11, y0 - 8)], fill="#6b4a2c", outline="#3a2a20")
                d.rectangle([P(x - 5.5, 0)[0], P(0, y0 - 10, 1)[1], P(x - 3.5, 0)[0], P(0, y0 - 10)[1]], fill="#8a8d94")
        elif genre == "plateau":
            for r, z in ((4.2, 0.25), (3.6, 0.5), (3.0, 0.75)):
                d.polygon([P(r * math.cos(math.radians(22.5 + 45 * k)), r * math.sin(math.radians(22.5 + 45 * k)), z) for k in range(8)],
                          fill="#6f757c", outline="#4a4f55")
            d.rectangle([P(-0.7, 0, 3)[0], P(0, 0, 3)[1], P(0.7, 0)[0], P(0, 0, 0.75)[1]], fill="#3a3f45")
            d.polygon([P(-0.8, 0, 4.4), P(0, 0, 5.8), P(0.8, 0, 4.4), P(0, 0, 3.2)], fill="#3fd06a")
        else:
            personnage(d, *v)
    return img


if __name__ == "__main__":
    os.makedirs(pv.SORTIE, exist_ok=True)
    dessiner().save(os.path.join(pv.SORTIE, "village-3d.png"))
    print("Docs/da/gabarits/village-3d.png")
