"""Générateur de plans de donjon « terrasses étagées » vus du ciel (02/10/2026), d'après le croquis de Quentin et l'exemple de Grok :
un grand volume fermé, un sol principal (Lvl 0), des terrasses à + 3 m (Lvl 1) et + 6 m (Lvl 2) adossées au fond, de gros murs de
soutènement, de larges escaliers (4 m), des arches vers de petites pièces cachées dans la masse, des piliers, la dalle d'arrivée et le portail.
Aucun balcon : chaque niveau est un sol plein. 2 ou 3 niveaux (rez-de-chaussée compris).

    python Docs/outils/donjon_plans.py            écrit Docs/da/gabarits/donjon-plans/plan-01.png ... plan-10.png et planche.png
    python Docs/outils/donjon_plans.py 7 12      un seul plan (graine 7), variante 12

Unités : mètres ; x vers l'est, y vers le SUD (y = 0 au fond du donjon, haut de l'image). Le portail est au sud, au milieu du mur avant."""
import math
import os
import random
import sys
from PIL import Image, ImageDraw, ImageFont

RACINE = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
SORTIE = os.path.join(RACINE, "Docs", "da", "gabarits", "donjon-plans")
PX = 17                                       # pixels par mètre
MARGE = 40
COULEURS = {0: "#262b35", 1: "#323a49", 2: "#414b5e"}
MUR = "#12151b"
ESCALIER = "#6e7890"


def police(t):
    for n in ("arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(n, t)
        except OSError:
            pass
    return ImageFont.load_default()


class Plan:
    def __init__(self, graine, variante):
        self.rng = random.Random(graine)
        self.graine = graine
        self.variante = variante
        self.L = self.rng.choice([32, 34, 36, 38, 40])        # largeur
        self.P = self.rng.choice([30, 32, 34, 36])            # profondeur
        self.B = self.rng.choice([12, 13, 14, 15])            # profondeur de la bande du fond
        self.terrasses = []                                   # (x0, y0, x1, y1, niveau)
        self.escaliers = []                                   # (x0, y0, x1, y1, montée (dx, dy), bas, haut)
        self.salles = []                                      # petites pièces cachées (x0, y0, x1, y1, porte (x, y, dx, dy))
        self.murs = []                                        # murs épais supplémentaires (x0, y0, x1, y1)
        self.nom = ""
        getattr(self, "v%d" % variante)()
        self.niveaux = 1 + max(t[4] for t in self.terrasses)

    # --- variantes ------------------------------------------------------------------------------------------------------
    def _cote(self):
        return self.rng.choice([1, -1])

    def v1(self):
        """Deux terrasses côte à côte (Lvl 1 et Lvl 2), un couloir central sans issue, escaliers sur les murs latéraux (croquis de Quentin)."""
        L, B = self.L, self.B
        wl, wr = self.rng.choice([12, 13, 14]), self.rng.choice([12, 13, 14])
        haut = self._cote()
        nl, nr = (2, 1) if haut == 1 else (1, 2)
        self.terrasses = [(0, 0, wl, B, nl), (L - wr, 0, L, B, nr)]
        self.escaliers = [(0, B, 4, B + 7, (0, -1), 0, nl), (L - 4, B, L, B + 7, (0, -1), 0, nr)]
        self.salles = [(2, 2, wl - 2, 6, (wl / 2, B, 0, 1))] if self.rng.random() < .7 else []
        self.salles += [(L - wr + 2, 2, L - 2, 6, (L - wr / 2, B, 0, 1))] if self.rng.random() < .7 else []
        self.murs = [(wl, 0, L - wr, B - 3)]
        self.nom = "Deux terrasses, couloir central"

    def v2(self):
        """Chaîne : Lvl 1 à droite, Lvl 2 à gauche accessible depuis le Lvl 1 (trois niveaux)."""
        L, B = self.L, self.B
        wr, wl = self.rng.choice([14, 15, 16]), self.rng.choice([10, 11, 12])
        s = self._cote()
        a, b = (L - wr, L), (0, wl)
        if s == -1:
            a, b = (0, wr), (L - wl, L)
        self.terrasses = [(a[0], 0, a[1], B, 1), (b[0], 0, b[1], B, 2)]
        bord = a[0] if s == 1 else a[1]
        self.escaliers = [(a[0] + 0 if s == 1 else a[1] - 4, B, a[0] + 4 if s == 1 else a[1], B + 7, (0, -1), 0, 1),
                          ((b[1] if s == 1 else b[0] - 3), 3, (b[1] + 3 if s == 1 else b[0]), 7, (1 if s == 1 else -1, 0), 0, 2)]
        # l'escalier du Lvl 1 au Lvl 2 se trouve dans le mur mitoyen
        self.escaliers[1] = (min(b[1], a[0]) if s == 1 else b[0] - 3, 3, (min(b[1], a[0]) + 3 if s == 1 else b[0]), 7, (1 if s == 1 else -1, 0), 1, 2) if s == 1 else (b[0] - 3, 3, b[0], 7, (-1, 0), 1, 2)
        if L - wr - wl > 4:                                  # espace entre les deux : sol Lvl 0 (couloir)
            self.murs = [(wl if s == 1 else wr, 0, L - wr if s == 1 else L - wl, 3)]
        self.salles = [(a[0] + 2, 2, a[1] - 2, 6, ((a[0] + a[1]) / 2, B, 0, 1))]
        self.nom = "Chaîne Lvl 1 puis Lvl 2"

    def v3(self):
        """Trois terrasses : deux Lvl 1 aux extrémités, un Lvl 2 au centre atteint depuis les deux Lvl 1 (trois niveaux)."""
        L, B = self.L, self.B
        e = self.rng.choice([10, 11, 12])
        c = L - 2 * e - 4
        self.terrasses = [(0, 0, e, B, 1), (e + 2, 0, L - e - 2, B, 2), (L - e, 0, L, B, 1)]
        self.escaliers = [(0, B, 4, B + 7, (0, -1), 0, 1), (L - 4, B, L, B + 7, (0, -1), 0, 1),
                          (e - 1, 3, e + 2, 7, (1, 0), 1, 2), (L - e - 2, 3, L - e + 1, 7, (-1, 0), 1, 2)]
        self.salles = [(e + 4, 2, L - e - 4, 6, (L / 2, B, 0, 1))]
        self.nom = "Trois terrasses, estrade centrale"

    def v4(self):
        """Terrasse en L : Lvl 1 le long du mur gauche et du fond, Lvl 2 dans l'angle du fond droit."""
        L, B = self.L, self.B
        lg = self.rng.choice([9, 10, 11])
        self.terrasses = [(0, 0, L, B, 1), (L - 12, 0, L, B - 3, 2)] if False else [(0, 0, L, B, 1), (L - 13, 0, L, 8, 2)]
        self.escaliers = [(L // 2 - 3, B, L // 2 + 3, B + 6, (0, -1), 0, 1), (L - 16, 2, L - 13, 6, (1, 0), 1, 2)]
        self.salles = [(2, 2, 8, 7, (5, B, 0, 1))] if self.rng.random() < .8 else []
        self.nom = "Grande terrasse et estrade haute"

    def v5(self):
        """Deux niveaux : deux terrasses Lvl 1 de tailles différentes, deux escaliers."""
        L, B = self.L, self.B
        wl, wr = self.rng.choice([10, 12]), self.rng.choice([14, 16])
        self.terrasses = [(0, 0, wl, B, 1), (L - wr, 0, L, B + 2, 1)]
        self.escaliers = [(0, B, 4, B + 6, (0, -1), 0, 1), (L - 4, B + 2, L, B + 8, (0, -1), 0, 1)]
        self.salles = [(2, 2, wl - 2, 6, (wl / 2, B, 0, 1)), (L - wr + 2, 2, L - 2, 6, (L - wr / 2, B + 2, 0, 1))]
        self.murs = [(wl, 0, L - wr, B - 2)]
        self.nom = "Deux niveaux, terrasses inégales"

    def v6(self):
        """Terrasses sur les côtés (le long des murs est et ouest), Lvl 1 et Lvl 2 en vis-à-vis."""
        L, B, P = self.L, self.B, self.P
        d = self.rng.choice([10, 11, 12])
        n1, n2 = (1, 2) if self.rng.random() < .5 else (2, 1)
        h = self.rng.choice([14, 16])
        self.terrasses = [(0, 0, d, h, n1), (L - d, 0, L, h, n2)]
        self.escaliers = [(0, h, d - 4 if d > 8 else 4, h + 6, (0, -1), 0, n1) if False else (0, h, 4, h + 7, (0, -1), 0, n1), (L - 4, h, L, h + 7, (0, -1), 0, n2)]
        self.salles = [(2, 2, d - 2, 6, (d / 2, h, 0, 1)), (L - d + 2, 2, L - 2, 6, (L - d / 2, h, 0, 1))]
        self.nom = "Terrasses latérales en vis-à-vis"

    def v7(self):
        """Une seule grande terrasse Lvl 1 au fond + un Lvl 2 au centre, large escalier central (trois niveaux)."""
        L, B = self.L, self.B
        c = self.rng.choice([12, 14])
        self.terrasses = [(0, 0, L, B, 1), ((L - c) // 2, 0, (L + c) // 2, 6, 2)]
        self.escaliers = [((L - 6) // 2, B, (L + 6) // 2, B + 6, (0, -1), 0, 1), ((L - 4) // 2, 6, (L + 4) // 2, 10, (0, -1), 1, 2)]
        self.salles = [(2, 2, 9, 7, (5, B, 0, 1)), (L - 9, 2, L - 2, 7, (L - 5, B, 0, 1))]
        self.nom = "Grande terrasse, estrade centrale haute"

    # --- dessin ---------------------------------------------------------------------------------------------------------
    def dessiner(self):
        L, P, B = self.L, self.P, self.B
        w, h = int(L * PX + 2 * MARGE), int(P * PX + 2 * MARGE + 60)
        img = Image.new("RGB", (w, h), "#0c0e13")
        d = ImageDraw.Draw(img)

        def px(x, y):
            return (MARGE + x * PX, MARGE + 34 + y * PX)

        def rect(x0, y0, x1, y1, fill, outline=None, width=1):
            d.rectangle([px(x0, y0), px(x1, y1)], fill=fill, outline=outline, width=width)

        rect(0, 0, L, P, COULEURS[0])
        # dalles du sol principal (quadrillage de 2 m)
        for x in range(0, L + 1, 2):
            d.line([px(x, 0), px(x, P)], fill="#2c323d", width=1)
        for y in range(0, P + 1, 2):
            d.line([px(0, y), px(L, y)], fill="#2c323d", width=1)
        # terrasses
        for x0, y0, x1, y1, n in self.terrasses:
            rect(x0, y0, x1, y1, COULEURS[n])
            for x in range(int(x0), int(x1) + 1, 2):
                d.line([px(x, y0), px(x, y1)], fill="#4b5568" if n == 2 else "#3c4556", width=1)
            for y in range(int(y0), int(y1) + 1, 2):
                d.line([px(x0, y), px(x1, y)], fill="#4b5568" if n == 2 else "#3c4556", width=1)
        # petites pièces cachées
        for x0, y0, x1, y1, (px_, py_, dx, dy) in self.salles:
            rect(x0, y0, x1, y1, "#171a21", "#0a0c10", 2)
            d.arc([px(px_ - 1, py_ - 2), px(px_ + 1, py_)], 180, 360, fill="#c7a56a", width=3)
        # murs épais supplémentaires
        for m in self.murs:
            rect(*m, MUR)
        # murs de soutènement : contour de chaque terrasse côté sol
        for x0, y0, x1, y1, n in self.terrasses:
            d.rectangle([px(x0, y0), px(x1, y1)], outline=MUR, width=7)
            # arche (ouverture) dans le mur de soutènement, repérée par la porte de chaque pièce cachée
        # escaliers
        for x0, y0, x1, y1, (ax, ay), bas, haut in self.escaliers:
            rect(x0, y0, x1, y1, ESCALIER, "#2a3040", 2)
            n = int(max(y1 - y0, x1 - x0))
            for i in range(1, n):
                if ay != 0:
                    y = y0 + i * (y1 - y0) / n
                    d.line([px(x0, y), px(x1, y)], fill="#3b4254", width=2)
                else:
                    x = x0 + i * (x1 - x0) / n
                    d.line([px(x, y0), px(x, y1)], fill="#3b4254", width=2)
            cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
            d.polygon([px(cx + ax * 1.2 - ay * 0.0, cy + ay * 1.2), px(cx - ay * 0.9 - ax * 0.9, cy + ax * 0.9 - ay * 0.9),
                       px(cx + ay * 0.9 - ax * 0.9, cy - ax * 0.9 - ay * 0.9)], fill="#e8e2c8")
        # enceinte
        d.rectangle([px(0, 0), px(L, P)], outline=MUR, width=10)
        # arrivée : dalle de bois, portail vert au milieu du mur sud
        rect(L / 2 - 3, P - 3.2, L / 2 + 3, P - 1.2, "#5a4126", "#2b1e10", 2)
        d.rectangle([px(L / 2 - 1.6, P - 0.4), px(L / 2 + 1.6, P + 0.5)], fill="#3fd06a", outline="#1b6a35", width=2)
        # piliers (sol principal), en grille à travers la zone libre
        zone = [(5, B + 8, L - 5, P - 6)]
        # piliers : grille régulière dans la zone libre du sol principal, espacés d'au moins 8 m
        y0z, y1z = B + 9, P - 7
        prof = y1z - y0z
        ny = 2 if prof >= 9 else 1
        nx = self.rng.choice([2, 3]) if L >= 36 else 2
        pil = []
        for i in range(nx):
            for j in range(ny):
                x = 6 + (L - 12) * (i + 0.5) / nx
                y = y0z + prof * (j + 0.5) / ny
                if not any(e[0] - 2 < x < e[2] + 2 and e[1] - 2 < y < e[3] + 2 for e in self.escaliers):
                    pil.append((x, y))
        for x, y in pil:
            d.ellipse([px(x - 1.0, y - 1.0), px(x + 1.0, y + 1.0)], fill="#59647a", outline=MUR, width=3)
        # points d'apparition (rouge) : sol principal et terrasses
        pts = []
        for _ in range(400):
            if len(pts) >= 9:
                break
            x, y = self.rng.uniform(4, L - 4), self.rng.uniform(3, P - 6)
            if any(math.hypot(x - a, y - b) < 6 for a, b in pts):
                continue
            if any(math.hypot(x - a, y - b) < 2.5 for a, b in pil):
                continue
            if any(e[0] - 1 < x < e[2] + 1 and e[1] - 1 < y < e[3] + 1 for e in self.escaliers):
                continue
            if y > P - 10 and abs(x - L / 2) < 8:
                continue
            if any(m[0] - 1 < x < m[2] + 1 and m[1] - 1 < y < m[3] + 1 for m in self.murs):
                continue
            pts.append((x, y))
        for x, y in pts:
            d.ellipse([px(x - 0.6, y - 0.6), px(x + 0.6, y + 0.6)], fill="#d04a3a", outline="#fff1d0")
        # coffres sur les terrasses (marron)
        for x0, y0, x1, y1, n in self.terrasses:
            for k in range(2 + n):
                x, y = self.rng.uniform(x0 + 1.5, x1 - 1.5), y0 + 1.2
                d.rectangle([px(x - 0.8, y - 0.5), px(x + 0.8, y + 0.5)], fill="#7a5530", outline="#2b1e10")
        # torches (points orange le long de l'enceinte et des murs de soutènement)
        for k in range(4, int(L) - 3, 6):
            for y in (1.0, P - 1.0):
                d.ellipse([px(k - 0.35, y - 0.35), px(k + 0.35, y + 0.35)], fill="#ffa13a")
        for k in range(5, int(P) - 3, 7):
            for x in (1.0, L - 1.0):
                d.ellipse([px(x - 0.35, k - 0.35), px(x + 0.35, k + 0.35)], fill="#ffa13a")
        # étiquettes de niveaux
        f = police(15)
        d.text(px(L / 2, P - 7), "Lvl 0", fill="#9aa4ba", font=police(22), anchor="mm")
        for x0, y0, x1, y1, n in self.terrasses:
            d.text(px((x0 + x1) / 2, y0 + (y1 - y0) / 2 + 1), "Lvl %d (+%d m)" % (n, 3 * n), fill="#c9d1e3", font=f, anchor="mm")
        # titre
        d.text((MARGE, 12), "Plan %02d · graine %d · %d niveaux · %d × %d m · %s" % (self.variante, self.graine, self.niveaux, self.L, self.P, self.nom),
               fill="#e8e2c8", font=police(16))
        d.text((MARGE, h - 28), "terrasses, escaliers de 4 m, arches vers petites pièces cachées, piliers ; rouge : apparitions ; vert : portail ; marron : coffres",
               fill="#8a93a8", font=police(12))
        return img


def planche(images):
    w = max(i.width for i in images)
    h = max(i.height for i in images)
    cols = 2
    rows = (len(images) + cols - 1) // cols
    img = Image.new("RGB", (cols * w, rows * h), "#0c0e13")
    for k, i in enumerate(images):
        img.paste(i, ((k % cols) * w, (k // cols) * h))
    return img


if __name__ == "__main__":
    os.makedirs(SORTIE, exist_ok=True)
    if len(sys.argv) >= 3:
        g, v = int(sys.argv[1]), int(sys.argv[2])
        Plan(g, v).dessiner().save(os.path.join(SORTIE, "plan-un.png"))
        print("plan-un.png")
        sys.exit(0)
    # dix plans : sept variantes, certaines répétées avec d'autres graines ; mélange de 2 et 3 niveaux
    choix = [(11, 1), (12, 1), (21, 2), (22, 2), (31, 3), (32, 3), (41, 4), (51, 5), (61, 6), (71, 7)]
    imgs = []
    for k, (g, v) in enumerate(choix, 1):
        p = Plan(g, v)
        im = p.dessiner()
        im.save(os.path.join(SORTIE, "plan-%02d.png" % k))
        imgs.append(im)
        print("plan-%02d : variante %d, graine %d, %d niveaux, %d x %d m, %s" % (k, v, g, p.niveaux, p.L, p.P, p.nom))
    planche(imgs[:4]).resize((int(planche(imgs[:4]).width * .55), int(planche(imgs[:4]).height * .55))).save(os.path.join(SORTIE, "planche-1.png"))
    planche(imgs[4:8]).resize((int(planche(imgs[4:8]).width * .55), int(planche(imgs[4:8]).height * .55))).save(os.path.join(SORTIE, "planche-2.png"))
    planche(imgs[8:]).save(os.path.join(SORTIE, "planche-3.png"))
