"""Vue 3D À L'ÉCHELLE du village (projection parallèle, sans perspective : un mètre vaut le même nombre de pixels
partout), tirée des mesures de plan_village.py. Sert de base à Grok (--depuis) pour un rendu low poly qui garde les
tailles : volumes des six bâtiments à leurs cotes, falaise, cascade, grotte, rivière, ponts, gué, quatre personnages
de 2,3 m près de Nyxessa.

    python Docs/outils/plan_village_3d.py      écrit Docs/da/gabarits/village-3d.png
"""
import math
import os
import random
import sys
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
    for a, b in pv.SENTIERS:
        d.line([P(*a), P(*b)], fill="#b08a5e", width=int(pv.LARGEUR_SENTIER * E * 0.7))
    # falaise (30 à 38 m de haut), cascade, grotte
    crete = [(-90, 34), (-60, 38), (-30, 33), (-8, 37), (8, 36), (34, 32), (60, 37), (90, 33)]
    d.polygon([P(-90, 40), P(90, 40)] + [P(x, 40, z) for x, z in reversed(crete)], fill="#8a8d94", outline="#5f6369")
    for x, z in crete[1:-1]:
        d.line([P(x, 40, z), P(x + 3, 40, 0)], fill="#74777e", width=2)
    d.polygon([P(-1.5, 40, 36), P(1.5, 40, 36), P(1.5, 40, 0), P(-1.5, 40, 0)], fill="#8fc3ea")
    gx = pv.GROTTE[0]
    d.ellipse([P(gx - 4, 40, 5)[0], P(gx, 40, 5)[1], P(gx + 4, 40)[0], P(gx, 40, -0.5)[1]], fill="#22262b")
    d.ellipse([P(gx - 2, 40, 3.6)[0], P(gx, 40, 3.6)[1], P(gx + 2, 40)[0], P(gx, 40, 0.2)[1]], fill="#3fd06a")
    # Variante serpentine (annotation de Quentin, 01/10/2026) : les trois maisons de la rive est ne sont plus reliées à
    # l'anneau par des bandes qui traversent la rivière (elles se lisaient comme un pont) ; leurs chemins longent la rive
    # est et convergent vers le pont principal, sur le sentier est. Les gués restent de simples passages dans l'eau.
    ROUTES_EST = {
        "Druide": [(17.4, 23.2), (21, 17), (21.5, 10), (19.5, 4), (17, 0.5)],
        "Forge": [(26.4, 10.7), (23, 7), (20, 3), (17, 0.5)],
        "Mecano": [(21.9, -13.8), (22, -9), (21, -4), (18.5, -1), (17, -0.5)],
    }

    def chemins_paves():
        pave = "#cfc7b2"
        cibles = [(n_, b_["c"]) for n_, b_ in pv.BATIMENTS.items()] + [("Grotte", pv.GROTTE), ("Pont", (12.6, 0)), ("Pont", (0, -17.8))]
        if pv.SERPENTE:
            cibles = [(n_, c_) for n_, c_ in cibles if n_ not in ROUTES_EST]
            for pts in ROUTES_EST.values():
                chemin = []
                for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
                    chemin += [(x0 + (x1 - x0) * k / 8, y0 + (y1 - y0) * k / 8) for k in range(8)]
                chemin.append(pts[-1])
                d.line([P(*q) for q in chemin], fill="#a39b88", width=int(3.0 * E * 0.8), joint="curve")
                d.line([P(*q) for q in chemin], fill=pave, width=int(2.2 * E * 0.8), joint="curve")
        for _, (cx, cy) in cibles:
            r = math.hypot(cx, cy)
            g = (cx, cy) == pv.GROTTE
            fin = 1 - (1 if g else 6) / r
            d.line([P(cx * pv.ANNEAU / r, cy * pv.ANNEAU / r), P(cx * fin, cy * fin)], fill=pave, width=int((pv.LARGEUR_ROUTE_GROTTE if g else 2.2) * E * 0.8))
        d.ellipse([P(-pv.ANNEAU, 0)[0], P(0, pv.ANNEAU)[1], P(pv.ANNEAU, 0)[0], P(0, -pv.ANNEAU)[1]], fill=pave)

    if pv.SERPENTE:                           # variante serpentine : les chemins passent SOUS l'eau, seules les pierres du gué traversent
        chemins_paves()
    # rivière, bassin, gué, pavés
    d.line([P(*p) for p in pv.RIVIERE_DENSE], fill="#3f7fb3", width=int(pv.LARGEUR_RIVIERE * E * 0.8), joint="curve")
    d.ellipse([P(-4, 38)[0], P(0, 41)[1], P(4, 38)[0], P(0, 35)[1]], fill="#3f7fb3")
    for gx, gy in pv.GUES:
        d.ellipse([P(gx - 3.2, 0)[0], P(0, gy + 3.2)[1], P(gx + 3.2, 0)[0], P(0, gy - 3.2)[1]], fill="#a9d4ee")
        if pv.SERPENTE:                       # gué explicite : cinq pierres plates en travers de l'eau peu profonde
            dense = pv.RIVIERE_DENSE
            i = min(range(len(dense)), key=lambda k: math.hypot(dense[k][0] - gx, dense[k][1] - gy))
            a, b = dense[max(0, i - 3)], dense[min(len(dense) - 1, i + 3)]
            n = math.hypot(b[0] - a[0], b[1] - a[1]) or 1.0
            nx, ny = -(b[1] - a[1]) / n, (b[0] - a[0]) / n
            for t in (-2.2, -1.1, 0.0, 1.1, 2.2):
                sx, sy = gx + nx * t, gy + ny * t
                d.ellipse([P(sx - 0.7, sy)[0], P(sx, sy + 0.7)[1], P(sx + 0.7, sy)[0], P(sx, sy - 0.7)[1]],
                          fill="#e4e7ea", outline="#6f757c")
    if not pv.SERPENTE:
        chemins_paves()
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
        if math.hypot(x, y) < 46 or x > 14 and y > -36 or abs(y) < 8 and x < 0 or abs(x) < 8 and y < 0 or (x < 0 and y > 0 and abs(x * 40 + y * 14) / 42.4 < 9):
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
                x, y0 = v[1]["c"][0] - 5.5, v[1]["c"][1] + 6
                d.rectangle([P(x - 2.25, y0, 7.5)[0], P(x, y0, 7.5)[1], P(x + 2.25, y0)[0], P(x, y0)[1]], fill="#e8d9a8", outline="#3a2a20")
                d.polygon([P(x - 2.8, y0, 7.5), P(x + 2.8, y0, 7.5), P(x, y0, 12)], fill="#24365c")
            if v[0] == "Forge":                                  # appentis
                x, y0 = v[1]["c"][0] + 4, v[1]["c"][1] + 5
                d.polygon([P(x - 2.5, y0 - 3, 3.4), P(x + 2.5, y0 - 3, 3.4), P(x + 2.5, y0 + 3, 4.4), P(x - 2.5, y0 + 3, 4.4)], fill="#6e4a30", outline="#3a2a20")
            if v[0] == "Maison":                                 # potager et puits
                x, y0 = v[1]["c"]
                d.polygon([P(x - 12, y0 + 4), P(x - 6, y0 + 4), P(x - 6, y0 - 1), P(x - 12, y0 - 1)], fill="#6b4a2c", outline="#3a2a20")
                d.rectangle([P(x - 10, 0)[0], P(0, y0 + 7, 1)[1], P(x - 8, 0)[0], P(0, y0 + 7)[1]], fill="#8a8d94")
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
    # Variante aérienne (01/10/2026) : python Docs/outils/plan_village_3d.py --k 0.9 --e 7.4 --nom village-aerien
    # k = écrasement de la profondeur (0,55 = vue de trois quarts ; 1 = vue presque verticale), e = pixels par mètre.
    nom = "village-3d"
    args = sys.argv[1:]
    if "--serpente" in args:                 # rivière serpentine (plan_village.serpente)
        pv.serpente()
    for opt, cible in (("--k", "K"), ("--e", "E"), ("--z", "Z")):
        if opt in args:
            globals()[cible] = float(args[args.index(opt) + 1])
    if "--nom" in args:
        nom = args[args.index("--nom") + 1]
    if "--cy" in args:
        CY = float(args[args.index("--cy") + 1])
    os.makedirs(pv.SORTIE, exist_ok=True)
    dessiner().save(os.path.join(pv.SORTIE, nom + ".png"))
    print("Docs/da/gabarits/" + nom + ".png")
