#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Icônes de Deathless : classes (emblème dans un cadre hexagonal) et compétences (glyphe seul).

Relançable : `python generer_icones.py` (bibliothèque standard seulement).
Écrit :
  - Classes/*.svg et Competences/*.svg (à côté de ce script) ;
  - ../../Docs/icones/planche.html (planche de revue autonome, SVG intégrés).

Style : langage des gemmes low poly des effets. Uniquement des <polygon> remplis en couleur pleine, facettes
ombrées par une lumière unique venue du haut à gauche, 2 à 5 teintes par objet, prises telles quelles dans les
palettes de thème du jeu (Assets/VFX/_Palettes/*.asset, lues à chaque exécution ; table de secours ci-dessous).
"""
import base64
import codecs
import html
import math
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import raster  # noqa: E402  (rastérisation PNG / ICO en Python pur)

ICI = Path(__file__).resolve().parent
MAIN = ICI.parent.parent
DOSSIER_PALETTES = MAIN / "Assets" / "VFX" / "_Palettes"
SORTIE_CLASSES = ICI / "Classes"
SORTIE_COMPETENCES = ICI / "Competences"
PLANCHE = MAIN / "Docs" / "icones" / "planche.html"
SORTIE_NYXESSA = ICI / "Nyxessa"

# ---------------------------------------------------------------------------------------------- palettes

# Secours si les assets de palette sont absents (valeurs relevées le 25/09/2026, identiques aux assets).
SECOURS = {
    "Feu": {"Braise": "#4a1206", "Rouge": "#cc1f08", "Orange": "#ff610a", "Jaune": "#ffe666",
            "Blanc chaud": "#fff4d6", "Charbon": "#2e2a28", "Cendre": "#6a615a"},
    "Nyxessa": {"Émeraude profonde": "#062a17", "Émeraude": "#0b5226", "Vert Nyx": "#178a36",
                "Vert clair": "#3fb552", "Éclat": "#a4ec90"},
    "Terre": {"Terre profonde": "#3a281a", "Terre sombre": "#5b3f2a", "Terre claire": "#8a6a48",
              "Sable": "#b8966c", "Pierre": "#8c877f"},
    "Rage": {"Rouge noir": "#3a0a08", "Rouge sombre": "#6e1410", "Rouge vif": "#b3261e", "Rouge pâle": "#ff7359",
             "Ivoire": "#e8dcc0", "Ivoire clair": "#fff5e0", "Fer": "#5a5f66", "Fer sombre": "#3f444a",
             "Fer clair": "#7a8088"},
    "Sacre": {"Nuit": "#1e2a3a", "Or sombre": "#b8903a", "Or": "#e8c872", "Or clair": "#f4e2a8", "Acier": "#5a7aa0"},
    "Soin": {"Or sombre": "#b8903a", "Or": "#e8c872", "Or clair": "#f4e2a8", "Blanc chaud": "#fff3d1"},
    "Os": {"Os gris": "#999485", "Os": "#c7bfa8", "Os pâle": "#ebe6cc", "Magie": "#8cff73"},
    "Critique": {"Ambre": "#a8641a", "Or chaud": "#e8a53a", "Or clair": "#ffd166", "Blanc chaud": "#fff3d1",
                 "Meilleur": "#ff5a3c"},
    "Chasse": {"Sous-bois": "#23361f", "Forêt": "#3e5a2b", "Olive": "#7a8c3a", "Ocre clair": "#d9b45a",
               "Ocre": "#a8742f"},
    "Ombre": {"Nuit": "#140b1f", "Violet sombre": "#2b1840", "Violet": "#5b3a8a", "Lilas": "#a58ad6",
              "Fumée": "#6b6478"},
    "BouclierPlein": {"Bleu nuit": "#0d2e73", "Bleu": "#1a66d9", "Bleu vif": "#4ca6ff", "Bleu pâle": "#95bfff",
                      "Lueur": "#59a6ff"},
}

RE_TEINTE = re.compile(r"- nom: (.+?)\n\s+role: \d+\n\s+couleur: \{r: ([-\d.e]+), g: ([-\d.e]+), b: ([-\d.e]+)")


def _nom(brut):
    brut = brut.strip()
    if brut.startswith('"') and brut.endswith('"'):
        brut = codecs.decode(brut[1:-1], "unicode_escape")
    return brut


def _hex(r, g, b):
    return "#%02x%02x%02x" % tuple(max(0, min(255, round(float(c) * 255))) for c in (r, g, b))


def charger_palettes():
    palettes = {}
    for theme, secours in SECOURS.items():
        chemin = DOSSIER_PALETTES / (theme + ".asset")
        lues = {}
        if chemin.exists():
            for m in RE_TEINTE.finditer(chemin.read_text(encoding="utf-8")):
                lues[_nom(m.group(1))] = _hex(m.group(2), m.group(3), m.group(4))
        if not lues:
            print("  palette %s : asset introuvable, table de secours" % theme)
            lues = dict(secours)
        for nom, valeur in secours.items():
            if nom not in lues:
                print("  palette %s : teinte « %s » absente de l'asset, secours %s" % (theme, nom, valeur))
                lues[nom] = valeur
            elif lues[nom] != valeur:
                print("  palette %s : « %s » a changé (%s -> %s), l'asset fait foi" % (theme, nom, valeur, lues[nom]))
        palettes[theme] = lues
    return palettes


P = charger_palettes()


def c(theme, nom):
    return P[theme][nom]


# Rampes (de la plus sombre à la plus claire) utilisées par les icônes.
SACRE = [c("Sacre", "Or sombre"), c("Sacre", "Or"), c("Sacre", "Or clair")]
LAME_SACREE = [c("Sacre", "Acier"), c("Sacre", "Or clair")]           # lame : acier à l'ombre, or clair à la lumière
FEU_EXT = [c("Feu", "Rouge"), c("Feu", "Orange")]
FEU_MIL = [c("Feu", "Orange"), c("Feu", "Jaune")]
FEU_COEUR = [c("Feu", "Jaune"), c("Feu", "Blanc chaud")]
FEU_BOULE = [c("Feu", "Rouge"), c("Feu", "Orange"), c("Feu", "Jaune")]
BOIS = [c("Terre", "Terre sombre"), c("Terre", "Terre claire"), c("Terre", "Sable")]
ARC = [c("Terre", "Terre claire"), c("Chasse", "Ocre"), c("Chasse", "Ocre clair")]
OCRE = [c("Chasse", "Ocre"), c("Chasse", "Ocre clair")]
FER = [c("Rage", "Fer"), c("Rage", "Fer clair"), c("Terre", "Pierre")]
CORDE = c("Os", "Os pâle")
PLUMES = (c("Chasse", "Ocre clair"), c("Chasse", "Ocre"))
OMBRE = [c("Ombre", "Violet"), c("Ombre", "Lilas")]
OMBRE3 = [c("Ombre", "Violet sombre"), c("Ombre", "Violet"), c("Ombre", "Lilas")]
LAME_OMBRE = [c("Ombre", "Fumée"), c("Ombre", "Lilas")]
FUMEE = [c("Ombre", "Violet"), c("Ombre", "Fumée"), c("Ombre", "Lilas")]
RAGE = [c("Rage", "Rouge sombre"), c("Rage", "Rouge vif"), c("Rage", "Rouge pâle")]
FER_RAGE = [c("Rage", "Fer sombre"), c("Rage", "Fer"), c("Rage", "Fer clair")]
IVOIRE = [c("Rage", "Ivoire"), c("Rage", "Ivoire clair")]
TERRE = [c("Terre", "Terre sombre"), c("Terre", "Terre claire"), c("Terre", "Sable")]
# Soin (décision du 25/09/2026) : blanc chaud et or, lumière sacrée ; le vert reste à Nyxessa. Plus de menthe.
SOIN = [c("Soin", "Or sombre"), c("Soin", "Or"), c("Soin", "Or clair"), c("Soin", "Blanc chaud")]
SOIN_BLANC = c("Soin", "Blanc chaud")
# Ancienne palette Soin (menthe, jusqu'au 25/09/2026) : interdite dans les icônes.
MENTHE = {"#1b6a4c", "#2e9e72", "#4fcf9a", "#b8f5d8"}
CRITIQUE = [c("Critique", "Ambre"), c("Critique", "Or chaud"), c("Critique", "Or clair")]
OS = [c("Os", "Os gris"), c("Os", "Os"), c("Os", "Os pâle")]
MANA = [c("BouclierPlein", "Bleu nuit"), c("BouclierPlein", "Bleu"), c("BouclierPlein", "Bleu vif"),
        c("BouclierPlein", "Bleu pâle")]

# Druide (classe à venir, 25/09/2026) : pas de palette de thème dans le jeu. Sans vert : bois et terre (Terre),
# ambre (Critique), bois de cerf et lune (Os), ocres d'automne (Chasse). Cadre en os, fond terre profonde / charbon.
BOIS_CLAIR = [c("Terre", "Terre claire"), c("Terre", "Sable")]
BOIS_DRUIDE = [c("Terre", "Terre sombre"), c("Terre", "Terre claire"), c("Terre", "Sable")]
AMBRE = [c("Critique", "Ambre"), c("Critique", "Or chaud"), c("Critique", "Or clair")]
AUTOMNE = [c("Critique", "Ambre"), c("Chasse", "Ocre"), c("Critique", "Or chaud")]
BOIS_CERF = [c("Terre", "Sable"), c("Os", "Os"), c("Os", "Os pâle")]
LUNE = [c("Os", "Os gris"), c("Os", "Os"), c("Os", "Os pâle")]

# Mécanicien (classe à venir, 25/09/2026) : pas de palette de thème. Laiton (Critique : ambre, or chaud, or clair),
# acier clair (Fer clair de Rage, Os) sur fond d'acier (Fer / Fer sombre de Rage), comme l'ingénieur KayKit.
LAITON = [c("Critique", "Ambre"), c("Critique", "Or chaud"), c("Critique", "Or clair")]
ACIER_CLAIR = [c("Rage", "Fer clair"), c("Os", "Os"), c("Os", "Os pâle")]

# Barde, Bavaroise, Clochard (classes à venir, kits proposés le 25/09/2026). Pas encore de palette de thème dans
# Assets/VFX/_Palettes : teintes relevées sur les modèles (sandbox-rig, captures barde_* et bavaroise_*) et sur la
# description du clochard. Aucune teinte n'est verte : teinte (hue) toujours sous 50° ou au-dessus de 180°, vérifié
# par verifier(). À reporter en palettes du jeu quand les effets de ces classes seront faits.
BARDE = {"nuit": "#2a0e17", "bordeaux sombre": "#5a1a2a", "bordeaux": "#8c2a3e", "bordeaux clair": "#b8475a",
         "moutarde sombre": "#9a7414", "moutarde": "#d4a82a", "moutarde claire": "#efd070",
         "crème ombre": "#d9c8a0", "crème": "#f2e8d0", "rosace": "#3a2410", "rosace claire": "#5a3a1c"}
BAVAROISE = {"ambre sombre": "#7a3f0e", "ambre": "#c7741c", "ambre clair": "#f0a93a",
             "mousse ombre": "#d8c8a4", "mousse": "#f6ecd4", "mousse claire": "#fffaf0",
             "étain sombre": "#4e5864", "étain": "#8793a0", "étain clair": "#c3cbd3",
             "cuivre sombre": "#8a4a30", "cuivre": "#b0603f", "cuivre clair": "#cf8460",
             "bleu nuit": "#1c3358", "bleu": "#2a4a7a", "bretzel sombre": "#6e3c1a", "bretzel": "#a35f2a",
             "bretzel clair": "#cf8a45"}
CLOCHARD = {"kraft sombre": "#6e5230", "kraft": "#a07c4e", "kraft clair": "#c9a574",
            "verre sombre": "#4a2c12", "verre": "#7a5226", "verre clair": "#a87a44",
            "gaz nuit": "#2e220a", "gaz profond": "#4a3510", "gaz sombre": "#6a4a12", "gaz": "#a8782a",
            "gaz clair": "#d4a440", "gaz pâle": "#ecca78",
            "rouge usé sombre": "#6e2a22", "rouge usé": "#9a4232", "rouge usé clair": "#c06a52"}
BA = lambda *n: [BARDE[x] for x in n]          # noqa: E731
BV = lambda *n: [BAVAROISE[x] for x in n]      # noqa: E731
CL = lambda *n: [CLOCHARD[x] for x in n]       # noqa: E731
LUTH = [c("Terre", "Terre claire"), c("Terre", "Sable"), c("Chasse", "Ocre clair")]
MANCHE_LUTH = [c("Terre", "Terre sombre"), c("Terre", "Terre claire")]
MOUTARDE = BA("moutarde sombre", "moutarde", "moutarde claire")
CREME = BA("crème ombre", "crème")
BORDEAUX = BA("bordeaux sombre", "bordeaux", "bordeaux clair")
ETAIN = BV("étain sombre", "étain", "étain clair")
CUIVRE = BV("cuivre sombre", "cuivre", "cuivre clair")
MOUSSE = BV("mousse ombre", "mousse", "mousse claire")
BIERE = BV("ambre sombre", "ambre", "ambre clair")
KRAFT = CL("kraft sombre", "kraft", "kraft clair")
VERRE = CL("verre sombre", "verre", "verre clair")
GAZ = CL("gaz sombre", "gaz", "gaz clair", "gaz pâle")
ROUGE_USE = CL("rouge usé sombre", "rouge usé", "rouge usé clair")
OR_PIECE = [c("Critique", "Ambre"), c("Critique", "Or chaud"), c("Critique", "Or clair")]

# Cadres des classes : rampe de la bordure (sombre -> claire, 4 niveaux), puis les deux zones du fond de
# l'hexagone, coupé en diagonale (« / », d'un sommet à l'autre) : moitié haut gauche claire, bas droite sombre.
CADRES = {
    "paladin": ([c("Sacre", "Or sombre"), c("Sacre", "Or"), c("Sacre", "Or"), c("Sacre", "Or clair")],
                c("Sacre", "Acier"), c("Sacre", "Nuit")),
    "mage_feu": ([c("Feu", "Rouge"), c("Feu", "Orange"), c("Feu", "Orange"), c("Feu", "Jaune")],
                 c("Feu", "Braise"), c("Feu", "Charbon")),
    "rodeur": ([c("Terre", "Terre claire"), c("Chasse", "Ocre"), c("Chasse", "Ocre"), c("Chasse", "Ocre clair")],
               c("Terre", "Terre sombre"), c("Terre", "Terre profonde")),
    "assassin": ([c("Ombre", "Violet"), c("Ombre", "Violet"), c("Ombre", "Lilas"), c("Ombre", "Lilas")],
                 c("Ombre", "Violet sombre"), c("Ombre", "Nuit")),
    "viking": ([c("Rage", "Rouge sombre"), c("Rage", "Rouge vif"), c("Rage", "Rouge vif"), c("Rage", "Rouge pâle")],
               c("Rage", "Rouge sombre"), c("Rage", "Rouge noir")),
    "mecanicien": ([c("Critique", "Ambre"), c("Critique", "Or chaud"), c("Critique", "Or chaud"), c("Critique", "Or clair")],
                   c("Rage", "Fer"), c("Rage", "Fer sombre")),
    "barde": (BA("bordeaux sombre", "bordeaux", "bordeaux", "bordeaux clair"), BARDE["bordeaux sombre"], BARDE["nuit"]),
    "bavaroise": (BV("étain sombre", "étain", "étain", "étain clair"), BAVAROISE["bleu"], BAVAROISE["bleu nuit"]),
    "clochard": ([c("Rage", "Fer"), c("Rage", "Fer clair"), c("Os", "Os gris"), c("Os", "Os")],
                 CLOCHARD["gaz profond"], CLOCHARD["gaz nuit"]),
    "druide": ([c("Os", "Os gris"), c("Os", "Os"), c("Os", "Os"), c("Os", "Os pâle")],
               c("Terre", "Terre profonde"), c("Feu", "Charbon")),
}

# ---------------------------------------------------------------------------------------------- géométrie

LUMIERE = (-0.6, -0.8)  # direction VERS la lumière (haut gauche, y vers le bas)


def add(a, b):
    return (a[0] + b[0], a[1] + b[1])


def sub(a, b):
    return (a[0] - b[0], a[1] - b[1])


def mul(a, k):
    return (a[0] * k, a[1] * k)


def dot(a, b):
    return a[0] * b[0] + a[1] * b[1]


def norm(a):
    n = math.hypot(a[0], a[1])
    return (a[0] / n, a[1] / n) if n > 1e-9 else (0.0, 0.0)


def centroide(pts):
    return (sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts))


def polaire(centre, r, deg):
    a = math.radians(deg)
    return (centre[0] + r * math.cos(a), centre[1] + r * math.sin(a))


def regulier(centre, r, n, a0=-90.0):
    return [polaire(centre, r, a0 + 360.0 * i / n) for i in range(n)]


def miroir(pts, axe=64.0):
    return [(2 * axe - x, y) for (x, y) in pts]


def repere(origine, direction):
    """Repère local : s le long de `direction`, t perpendiculaire (vers la droite de la direction, y vers le bas)."""
    u = norm(direction)
    v = (-u[1], u[0])

    def f(s, t):
        return (origine[0] + u[0] * s + v[0] * t, origine[1] + u[1] * s + v[1] * t)

    return f


def teinte(rampe, normale):
    d = dot(norm(normale), LUMIERE)
    i = int(round((d + 1.0) / 2.0 * (len(rampe) - 1)))
    return rampe[max(0, min(len(rampe) - 1, i))]


class Icone:
    def __init__(self, nom, famille, titre, notes=""):
        self.nom, self.famille, self.titre, self.notes = nom, famille, titre, notes
        self.formes = []

    # -- primitives
    def poly(self, pts, couleur):
        self.formes.append((list(pts), couleur))

    def gemme(self, contour, rampe, table=0.0, centre=None, teinte_table=None, decalage=0.1, dessous=True):
        """Forme à facettes : éventail depuis `centre`, ou couronne de facettes autour d'une table (table > 0)."""
        cen = centre or centroide(contour)
        if dessous:
            self.poly(contour, rampe[0])
        n = len(contour)
        if table > 0:
            taille = max(math.hypot(*sub(p, cen)) for p in contour)
            cc = add(cen, mul(LUMIERE, taille * decalage))
            interieur = [add(cc, mul(sub(p, cen), table)) for p in contour]
        couleurs = []
        for i in range(n):
            a, b = contour[i], contour[(i + 1) % n]
            e = sub(b, a)
            nn = (-e[1], e[0])
            if dot(nn, sub(mul(add(a, b), 0.5), cen)) < 0:
                nn = (-nn[0], -nn[1])
            couleurs.append(teinte(rampe, nn))
        # Facettes voisines de même teinte fusionnées en un seul polygone (SVG plus compact, rendu identique).
        debut = 0
        if len(set(couleurs)) > 1:
            while couleurs[debut - 1] == couleurs[debut]:
                debut += 1
        i = 0
        while i < n:
            j = i
            while j + 1 < n and couleurs[(debut + j + 1) % n] == couleurs[(debut + i) % n]:
                j += 1
            idx = [(debut + k) % n for k in range(i, j + 2)]
            col = couleurs[(debut + i) % n]
            if table > 0:
                self.poly([contour[k] for k in idx] + [interieur[k] for k in reversed(idx)], col)
            else:
                self.poly([cen] + [contour[k] for k in idx], col)
            i = j + 1
        if table > 0:
            self.poly(interieur, teinte_table or rampe[len(rampe) // 2 if len(rampe) > 2 else -1])

    def bande(self, chemin, largeurs, rampe, dessous=True):
        """Objet long à arête centrale (lame, manche, corne, arc) : deux demi-facettes par segment."""
        n = len(chemin)
        if isinstance(largeurs, (int, float)):
            largeurs = [largeurs] * n
        gauches, droites = [], []
        for i in range(n):
            if i == 0:
                d = sub(chemin[1], chemin[0])
            elif i == n - 1:
                d = sub(chemin[-1], chemin[-2])
            else:
                d = add(norm(sub(chemin[i], chemin[i - 1])), norm(sub(chemin[i + 1], chemin[i])))
            d = norm(d)
            nr = (-d[1], d[0])
            w = largeurs[i] / 2.0
            gauches.append(add(chemin[i], mul(nr, w)))
            droites.append(sub(chemin[i], mul(nr, w)))
        if dessous:
            self.poly(gauches + droites[::-1], rampe[0])
        for i in range(n - 1):
            for bord, signe in ((gauches, 1), (droites, -1)):
                a, b = bord[i], bord[i + 1]
                e = sub(b, a)
                if math.hypot(*e) < 1e-6:
                    e = sub(chemin[i + 1], chemin[i])
                nn = (-e[1] * signe, e[0] * signe)
                self.poly([chemin[i], chemin[i + 1], b, a], teinte(rampe, nn))

    # -- sortie
    def svg(self, avec_entete=True):
        lignes = []
        if avec_entete:
            lignes.append('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" width="128" height="128">')
            lignes.append("<!-- Deathless : %s (%s). Généré par ArtSources/Icones/generer_icones.py -->" % (self.nom, self.titre))
        for pts, col in self.formes:
            lignes.append('<polygon fill="%s" points="%s"/>' % (col, " ".join("%s,%s" % (_n(x), _n(y)) for x, y in pts)))
        if avec_entete:
            lignes.append("</svg>")
        return "\n".join(lignes) + "\n"

    def polygones(self):
        return "".join('<polygon fill="%s" points="%s"/>' % (col, " ".join("%s,%s" % (_n(x), _n(y)) for x, y in pts))
                       for pts, col in self.formes)


def _n(v):
    v = round(v, 1)
    return ("%d" % v) if v == int(v) else ("%.1f" % v)


# ---------------------------------------------------------------------------------------------- pièces communes

def cadre_hex(ic, classe):
    rampe, fond_clair, fond_sombre = CADRES[classe]
    ext = regulier((64, 64), 62, 6, -90)
    inte = regulier((64, 64), 51, 6, -90)
    ic.poly(ext, rampe[0])
    for i in range(6):
        a, b = ext[i], ext[(i + 1) % 6]
        e = sub(b, a)
        nn = (-e[1], e[0])
        if dot(nn, sub(mul(add(a, b), 0.5), (64, 64))) < 0:
            nn = (-nn[0], -nn[1])
        ic.poly([a, b, inte[(i + 1) % 6], inte[i]], teinte(rampe, nn))
    # Sommets : 0 haut, 1 haut droite, 2 bas droite, 3 bas, 4 bas gauche, 5 haut gauche. Coupe de 1 à 4.
    ic.poly([inte[4], inte[5], inte[0], inte[1]], fond_clair)
    ic.poly([inte[1], inte[2], inte[3], inte[4]], fond_sombre)


def flamme(ic, cx, base, h, demi, couches=(FEU_EXT, FEU_MIL, FEU_COEUR)):
    """Flamme à trois langues, trois couches emboîtées (extérieur, milieu, cœur)."""
    forme = [(0, 0), (-0.6, 0.07), (-0.92, 0.3), (-0.95, 0.55), (-0.62, 0.74), (-0.47, 0.58), (-0.27, 0.8),
             (0.08, 1.0), (0.33, 0.72), (0.48, 0.56), (0.72, 0.64), (0.95, 0.4), (0.88, 0.17), (0.55, 0.05)]
    echelles = [(1.0, 1.0, 0.0), (0.66, 0.72, 0.03), (0.36, 0.44, 0.05)]
    for (ex, ey, dy), rampe in zip(echelles, couches):
        pts = [(cx + x * demi * ex, base - dy * h - y * h * ey) for x, y in forme]
        ic.gemme(pts, rampe, centre=(cx, base - dy * h - 0.3 * h * ey))


def epee(ic, pommeau, pointe, lame=14.0, garde_l=30.0, rampe_lame=LAME_SACREE, rampe_garde=SACRE,
         rampe_poignee=None, poignee=14.0, feuille=False):
    """Épée ou dague de `pommeau` à `pointe` : pommeau, poignée, garde, lame à arête centrale."""
    rampe_poignee = rampe_poignee or [c("Sacre", "Acier"), c("Sacre", "Or sombre")]
    f = repere(pommeau, sub(pointe, pommeau))
    longueur = math.hypot(*sub(pointe, pommeau))
    g = 6 + poignee  # position de la garde
    ic.bande([f(3, 0), f(g, 0)], 8, rampe_poignee)
    ic.gemme([f(-7, 0), f(0, -6.5), f(7, 0), f(0, 6.5)], rampe_garde)
    if feuille:
        ic.bande([f(g + 2, 0), f(g + (longueur - g) * 0.45, 0), f(longueur, 0)], [lame * 0.8, lame, 0], rampe_lame)
    else:
        ic.bande([f(g + 2, 0), f(longueur - lame * 1.1, 0), f(longueur, 0)], [lame, lame * 0.85, 0], rampe_lame)
    ic.bande([f(g, -garde_l / 2), f(g, 0), f(g, garde_l / 2)], [5, 8, 5], rampe_garde)


def fleche(ic, queue, pointe, epaisseur=5.0, tete=13.0, plumes=PLUMES, rampe_hampe=BOIS, rampe_tete=FER):
    """Flèche ou carreau non magique : hampe en bois, pointe en fer, empennage en plumes. Aucune lueur."""
    f = repere(queue, sub(pointe, queue))
    longueur = math.hypot(*sub(pointe, queue))
    base_tete = longueur - tete * 1.25
    if plumes:
        for signe, col in ((-1, plumes[0]), (1, plumes[1])):
            e = epaisseur / 2
            ic.poly([f(1, signe * e), f(15, signe * e), f(10, signe * (e + 6.5)), f(-2, signe * (e + 6.5))], col)
    ic.bande([f(0, 0), f(base_tete + 2, 0)], epaisseur, rampe_hampe)
    t = tete / 2
    ic.gemme([f(base_tete - 2, 0), f(base_tete, -t), f(longueur, 0), f(base_tete, t)], rampe_tete,
             centre=f(base_tete + 2, 0))


def tete_hache(ic, f, rampe=FER_RAGE, fil=IVOIRE, echelle=1.0, sens=1):
    """Fer de hache barbue dans le repère local f (s le long du manche, t vers le tranchant)."""
    k = echelle

    def g(s, t):
        return f(s * k, sens * t * k)

    corps = [g(-7, 2), g(8, 2), g(20, 28), g(12, 34), g(0, 36), g(-12, 35), g(-25, 30), g(-13, 14)]
    ic.gemme(corps, rampe, table=0.45, teinte_table=rampe[1])
    ic.bande([g(21, 30), g(12, 36.5), g(0, 38.5), g(-12, 37.5), g(-26, 32)], 4.5 * k, fil)


# ---------------------------------------------------------------------------------------------- classes

def classe_paladin():
    ic = Icone("classe_paladin", "classes", "Paladin", "Écu à croix et épée, cadre or (Sacré).")
    cadre_hex(ic, "paladin")
    epee(ic, (64, 20), (64, 110), lame=12, garde_l=34, poignee=10)
    ecu = [(40, 42), (88, 42), (88, 64), (84, 80), (76, 92), (64, 102), (52, 92), (44, 80), (40, 64)]
    ic.gemme(ecu, SACRE, table=0.72, teinte_table=c("Sacre", "Or"), decalage=0.06)
    nuit = c("Sacre", "Nuit")
    ic.poly([(60, 50), (68, 50), (68, 62), (80, 62), (80, 70), (68, 70), (68, 92), (64, 96), (60, 92), (60, 70),
             (48, 70), (48, 62), (60, 62)], nuit)
    return ic


def classe_mage_feu():
    ic = Icone("classe_mage_feu", "classes", "Mage de feu", "Grande flamme à trois langues, cadre feu.")
    cadre_hex(ic, "mage_feu")
    flamme(ic, 64, 106, 86, 34)
    return ic


def classe_rodeur():
    ic = Icone("classe_rodeur", "classes", "Rôdeur",
               "Arc bandé debout, flèche encochée, bois et ocres (Chasse, sans vert).")
    cadre_hex(ic, "rodeur")
    centre = (26, 64)
    arc = [polaire(centre, 60, a) for a in range(-52, 53, 8)]
    encoche = (24, 64)
    ic.bande([arc[0], encoche], 4.2, [CORDE, CORDE])
    ic.bande([encoche, arc[-1]], 4.2, [CORDE, CORDE])
    fleche(ic, (22, 64), (105, 64), epaisseur=7.5, tete=20, rampe_hampe=[c("Terre", "Terre claire"), c("Terre", "Sable")])
    n = len(arc)
    ic.bande(arc, [7 + 9 * math.sin(math.pi * i / (n - 1)) for i in range(n)], OCRE)
    ic.bande([polaire(centre, 60, -7), polaire(centre, 60, 7)], 20, [c("Terre", "Terre claire"), c("Chasse", "Ocre clair")])
    return ic


def classe_assassin():
    ic = Icone("classe_assassin", "classes", "Assassin", "Capuche et regard dans l'ombre, cadre violet (Ombre).")
    cadre_hex(ic, "assassin")
    capuche = [(64, 18), (80, 30), (92, 50), (96, 76), (92, 100), (36, 100), (32, 76), (36, 50), (48, 30)]
    ic.gemme(capuche, OMBRE3, centre=(58, 60))
    visage = [(64, 44), (78, 56), (82, 76), (74, 92), (54, 92), (46, 76), (50, 56)]
    ic.poly(visage, c("Ombre", "Nuit"))
    lilas = c("Ombre", "Lilas")
    ic.poly([(51, 68), (61, 71), (60, 75), (52, 73)], lilas)
    ic.poly(miroir([(51, 68), (61, 71), (60, 75), (52, 73)]), lilas)
    return ic


def classe_viking():
    ic = Icone("classe_viking", "classes", "Viking", "Hache à deux mains à double fer, cadre rouge (Rage).")
    cadre_hex(ic, "viking")
    ic.bande([(64, 112), (64, 22)], 9, [c("Rage", "Rouge vif"), c("Rage", "Rouge pâle")])
    for y in (84, 98):
        ic.bande([(64, y), (64, y + 5)], 10, IVOIRE)
    f = repere((64, 48), (0, -1))
    tete_hache(ic, f, sens=1, echelle=1.0)
    tete_hache(ic, f, sens=-1, echelle=1.0)
    ic.gemme([(64, 16), (68, 23), (64, 28), (60, 23)], IVOIRE)
    return ic


# ---------------------------------------------------------------------------------------------- paladin

def paladin_epee():
    ic = Icone("paladin_epee", "paladin", "Frappe à l'épée", "Attaque principale (RT).")
    epee(ic, (20, 108), (110, 18), lame=19, garde_l=40, poignee=14)
    return ic


def paladin_garde():
    ic = Icone("paladin_garde", "paladin", "Garde et parade", "Attaque secondaire (LT) : écu levé, éclats de parade.")
    ecu = [(28, 26), (92, 26), (92, 56), (86, 78), (76, 96), (60, 110), (44, 96), (34, 78), (28, 56)]
    ecu = [(x - 4, y + 2) for x, y in ecu]
    ic.gemme(ecu, SACRE, table=0.62, teinte_table=c("Sacre", "Or"), decalage=0.05)
    ic.gemme(regulier((56, 60), 11, 8, -67.5), [c("Sacre", "Acier"), c("Sacre", "Or clair")], table=0.5,
             teinte_table=c("Sacre", "Or clair"))
    for ang, lg in ((-85, 20), (-45, 26), (-5, 20)):
        a = polaire((90, 30), 7, ang)
        b = polaire((90, 30), 7 + lg, ang)
        ic.bande([a, b], [7, 0], [c("Sacre", "Or"), c("Sacre", "Or clair")])
    return ic


def paladin_charge_belier():
    ic = Icone("paladin_charge_belier", "paladin", "Charge bélier", "Compétence 1 (LB) : tête de bélier en gemmes dorées.")
    n = 16
    corne = []
    for i in range(n):
        t = i / (n - 1)
        corne.append(polaire((33, 52), 21 - 13 * t, -25 - 335 * t))
    larg = [16 - 11 * i / (n - 1) for i in range(n)]
    ic.bande(corne, larg, SACRE)
    ic.bande(miroir(corne), larg, SACRE)
    tete = [(50, 36), (78, 36), (88, 54), (82, 80), (74, 98), (64, 106), (54, 98), (46, 80), (40, 54)]
    ic.gemme(tete, SACRE, table=0.5, teinte_table=c("Sacre", "Or"))
    nuit = c("Sacre", "Nuit")
    oeil = [(46, 56), (59, 60), (50, 66)]
    ic.poly(oeil, nuit)
    ic.poly(miroir(oeil), nuit)
    ic.poly([(46, 52), (60, 55), (59, 58), (45, 55)], c("Sacre", "Acier"))
    ic.poly(miroir([(46, 52), (60, 55), (59, 58), (45, 55)]), c("Sacre", "Acier"))
    narine = [(56, 92), (61, 95), (57, 98)]
    ic.poly(narine, nuit)
    ic.poly(miroir(narine), nuit)
    return ic


def paladin_soin():
    ic = Icone("paladin_soin", "paladin", "Soin sur soi", "Compétence 2 (RB) : croix de lumière sacrée, blanc chaud et or.")
    b = 13
    cx, cy = 58, 70
    croix = [(cx - b, cy - 44), (cx + b, cy - 44), (cx + b, cy - b), (cx + 44, cy - b), (cx + 44, cy + b),
             (cx + b, cy + b), (cx + b, cy + 44), (cx - b, cy + 44), (cx - b, cy + b), (cx - 44, cy + b),
             (cx - 44, cy - b), (cx - b, cy - b)]
    ic.gemme(croix, SOIN[:3], table=0.62, centre=(cx, cy), teinte_table=SOIN_BLANC, decalage=0.04)
    for (x, y, r) in ((106, 40, 7), (96, 20, 5), (114, 20, 4)):
        ic.gemme([(x, y - r * 1.5), (x + r, y), (x, y + r * 1.5), (x - r, y)], [c("Sacre", "Or"), SOIN_BLANC])
    return ic


# ---------------------------------------------------------------------------------------------- mage de feu

def mage_boule_de_feu():
    ic = Icone("mage_boule_de_feu", "mage_feu", "Boule de feu", "Attaque principale (RT).")
    cen = (76, 50)
    for fin, milieu, large in (((12, 84), (40, 70), 34), ((44, 116), (58, 84), 34), ((14, 114), (42, 82), 46)):
        ic.bande([cen, milieu, fin], [large, large * 0.55, 0], FEU_EXT)
    ic.bande([cen, (46, 80), (24, 104)], [30, 14, 0], FEU_MIL)
    ic.gemme(regulier(cen, 32, 10, -72), FEU_BOULE, table=0.52, teinte_table=c("Feu", "Jaune"))
    ic.gemme(regulier(add(cen, (-6, -7)), 9, 5, -90), FEU_COEUR)
    return ic


def mage_cone_de_flammes():
    ic = Icone("mage_cone_de_flammes", "mage_feu", "Cône de flammes", "Attaque secondaire maintenue (LT).")
    o = (18, 110)
    dir0 = -45
    for demi, rayons, rampe in ((33, (108, 90), FEU_EXT), (22, (88, 72), FEU_MIL), (11, (60, 50), FEU_COEUR)):
        bord = []
        k = 9
        for i in range(k):
            a = dir0 - demi + 2 * demi * i / (k - 1)
            bord.append(polaire(o, rayons[i % 2], a))
        ic.gemme([o] + bord, rampe, centre=polaire(o, rayons[1] * 0.25, dir0))
    return ic


def mage_brulure():
    ic = Icone("mage_brulure", "mage_feu", "Brûlure", "État des ennemis touchés (pas un emplacement).")
    flamme(ic, 36, 112, 58, 22)
    flamme(ic, 94, 112, 62, 22)
    flamme(ic, 64, 114, 94, 30)
    return ic


def jauge_mana():
    ic = Icone("jauge_mana", "mage_feu", "Mana", "Jauge de classe du mage (bleu de la jauge du HUD).")
    goutte = [(64, 10), (78, 34), (92, 58), (96, 78), (88, 98), (74, 110), (54, 110), (40, 98), (32, 78), (36, 58),
              (50, 34)]
    ic.gemme(goutte, MANA, table=0.5, centre=(64, 76), teinte_table=c("BouclierPlein", "Bleu vif"), decalage=0.12)
    ic.gemme([(50, 62), (56, 52), (58, 66), (52, 78)], [c("BouclierPlein", "Bleu vif"), c("BouclierPlein", "Bleu pâle")])
    return ic


# ---------------------------------------------------------------------------------------------- rôdeur

def rodeur_tir():
    ic = Icone("rodeur_tir", "rodeur", "Bander et tirer", "Attaque principale (RT) : arc bandé.")
    centre = (34, 64)
    arc = [polaire(centre, 58, a) for a in range(-64, 65, 8)]
    haut, bas = arc[0], arc[-1]
    encoche = (16, 64)
    ic.bande([haut, encoche], 3.6, [CORDE, CORDE])
    ic.bande([encoche, bas], 3.6, [CORDE, CORDE])
    fleche(ic, (14, 64), (120, 64), epaisseur=6.5, tete=18)
    larg = [6, 7.5, 9, 10.5, 11.5, 12.5, 13, 13.5, 13.5, 13, 12.5, 11.5, 10.5, 9, 7.5, 6, 6][:len(arc)]
    ic.bande(arc, larg, ARC)
    ic.bande([polaire(centre, 58, -6), polaire(centre, 58, 6)], 14, OCRE)
    return ic


def rodeur_visee():
    ic = Icone("rodeur_visee", "rodeur", "Viser", "Attaque secondaire (LT) : cercle de charge à quatre crans. Ajoutée : absente de la liste.")
    cen = (64, 64)
    for q in range(4):
        a0 = q * 90 + 12
        pts = [polaire(cen, 40, a0 + i * 11) for i in range(7)]
        ic.bande(pts, 9, OCRE)
    for q in range(4):
        a = q * 90
        p1 = polaire(cen, 52, a)
        p2 = polaire(cen, 30, a)
        ic.bande([p1, p2], [9, 0], ARC)
    ic.gemme([(64, 56), (72, 64), (64, 72), (56, 64)], OCRE)
    return ic


def rodeur_nuee_de_fleches():
    ic = Icone("rodeur_nuee_de_fleches", "rodeur", "Nuée de flèches", "Compétence 1 (LB) : pluie de flèches sur la zone marquée.")
    cen = (64, 102)
    anneau_ext = [(cen[0] + 50 * math.cos(math.radians(a)), cen[1] + 15 * math.sin(math.radians(a)))
                  for a in range(0, 360, 20)]
    anneau_int = [(cen[0] + 40 * math.cos(math.radians(a)), cen[1] + 9 * math.sin(math.radians(a)))
                  for a in range(0, 360, 20)]
    n = len(anneau_ext)
    for i in range(n):
        a, b = anneau_ext[i], anneau_ext[(i + 1) % n]
        milieu = mul(add(a, b), 0.5)
        ic.poly([a, b, anneau_int[(i + 1) % n], anneau_int[i]], teinte(OCRE, sub(cen, milieu)))
    for (x0, y0, x1, y1) in ((26, 12, 38, 88), (58, 5, 66, 98), (90, 14, 98, 86)):
        fleche(ic, (x0, y0), (x1, y1), epaisseur=5, tete=14)
    return ic


def rodeur_roulade_salve():
    ic = Icone("rodeur_roulade_salve", "rodeur", "Roulade arrière et salve", "Compétence 2 (RB) : roulade en arrière, salve de flèches devant.")
    cen = (36, 68)
    pts = [polaire(cen, 27, a) for a in range(-20, -290, -18)]
    larg = [4 + 9 * i / (len(pts) - 1) for i in range(len(pts))]
    ic.bande(pts, larg, ARC)
    fin = pts[-1]
    d = norm(sub(pts[-1], pts[-2]))
    f = repere(fin, d)
    ic.gemme([f(-2, -13), f(15, 0), f(-2, 13)], OCRE)
    for ang in (-24, 0, 24):
        q = polaire((54, 66), 70, ang)
        fleche(ic, polaire((54, 66), 16, ang), q, epaisseur=5, tete=14)
    return ic


# ---------------------------------------------------------------------------------------------- assassin

def assassin_dague():
    ic = Icone("assassin_dague", "assassin", "Dague", "Attaque principale (RT).")
    epee(ic, (22, 106), (108, 20), lame=22, garde_l=36, poignee=16, rampe_lame=LAME_OMBRE, rampe_garde=OMBRE,
         rampe_poignee=[c("Ombre", "Violet sombre"), c("Ombre", "Violet")], feuille=True)
    return ic


def assassin_arbalete():
    ic = Icone("assassin_arbalete", "assassin", "Arbalète", "Attaque secondaire (LT) : en main et visée ; carreau non magique.")
    ic.bande([(64, 118), (64, 44)], 12, BOIS)
    ic.bande([(64, 96), (64, 104)], 14, OMBRE)
    gauche = [(64, 46), (46, 44), (28, 50), (14, 62)]
    ic.bande([gauche[-1], (64, 72)], 2.8, [CORDE, CORDE])
    ic.bande([(64, 72), miroir(gauche)[-1]], 2.8, [CORDE, CORDE])
    ic.bande(gauche, [9, 8, 6.5, 4.5], FER)
    ic.bande(miroir(gauche), [9, 8, 6.5, 4.5], FER)
    ic.gemme([(56, 40), (72, 40), (74, 52), (54, 52)], FER)
    fleche(ic, (64, 76), (64, 8), epaisseur=4.5, tete=14, plumes=(CORDE, c("Os", "Os")))
    return ic


def assassin_fumigene():
    ic = Icone("assassin_fumigene", "assassin", "Grenade fumigène", "Compétence 1 (LB) : bombe et nuage de fumée.")
    nuage = [c("Ombre", "Violet"), c("Ombre", "Fumée"), c("Ombre", "Fumée"), c("Ombre", "Lilas")]
    for (x, y, r, n) in ((100, 50, 17, 7), (66, 28, 16, 7), (96, 22, 15, 7), (82, 40, 20, 8)):
        ic.gemme(regulier((x, y), r, n, -80), nuage, table=0.55, teinte_table=c("Ombre", "Fumée"))
    ic.bande([(58, 58), (66, 50)], 10, [c("Ombre", "Fumée"), c("Ombre", "Lilas")])
    ic.gemme(regulier((44, 80), 30, 8, -67.5), OMBRE3, table=0.55, teinte_table=c("Ombre", "Violet"))
    ic.gemme([(66, 44), (70, 48), (66, 52), (62, 48)], [c("Critique", "Or chaud"), c("Critique", "Blanc chaud")])
    return ic


def assassin_furtif():
    ic = Icone("assassin_furtif", "assassin", "Mode furtif", "Indicateur (passif) : œil barré.")
    haut = [(14 + 100 * i / 10, 64 - 34 * math.sin(math.pi * i / 10)) for i in range(11)]
    bas = [(114 - 100 * i / 10, 64 + 30 * math.sin(math.pi * i / 10)) for i in range(1, 10)]
    oeil = haut + bas
    ic.gemme(oeil, [c("Ombre", "Violet"), c("Ombre", "Lilas"), c("Ombre", "Lilas")], centre=(64, 64))
    ic.gemme(regulier((64, 64), 21, 8, -67.5), OMBRE3, table=0.6, teinte_table=c("Ombre", "Violet"))
    ic.gemme(regulier((64, 64), 8, 6, 0), [c("Ombre", "Nuit"), c("Ombre", "Violet sombre")])
    ic.bande([(22, 110), (106, 18)], 22, [c("Ombre", "Nuit"), c("Ombre", "Nuit")])
    ic.bande([(24, 107), (104, 21)], 11, OMBRE)
    return ic


# ---------------------------------------------------------------------------------------------- viking

def viking_hache():
    ic = Icone("viking_hache", "viking", "Hache", "Attaque principale (RT) : hache à deux mains et taillade.")
    a, b = (16, 118), (78, 22)
    ic.bande([a, b], 9, [c("Rage", "Rouge sombre"), c("Rage", "Rouge vif")])
    for s in (0.08, 0.5, 0.6):
        p = add(a, mul(sub(b, a), s))
        q = add(a, mul(sub(b, a), s + 0.045))
        ic.bande([p, q], 11, IVOIRE)
    f = repere(add(a, mul(sub(b, a), 0.83)), sub(b, a))
    tete_hache(ic, f, echelle=1.45, sens=1)
    ic.gemme([b, add(b, (5, -2)), add(b, (2, -9)), add(b, (-4, -4))], IVOIRE)
    return ic


def viking_attaque_tournante():
    ic = Icone("viking_attaque_tournante", "viking", "Attaque tournante", "Attaque secondaire maintenue (LT).")
    cen = (58, 66)
    pts = [polaire(cen, 40, a) for a in range(-335, -34, 12)]
    ic.bande(pts, [2 + 16 * i / (len(pts) - 1) for i in range(len(pts))], RAGE)
    bout = polaire(cen, 40, -35)
    # Manche radial, tranchant vers l'avant du mouvement (sens des angles croissants).
    tete_hache(ic, repere(bout, norm(sub(bout, cen))), echelle=0.9, sens=1)
    return ic


def viking_rugissement():
    ic = Icone("viking_rugissement", "viking", "Rugissement", "Compétence 1 (LB) : crâne barbare casqué qui rugit.")
    corne = [(36, 46), (22, 40), (13, 28), (12, 12)]
    ic.bande(corne, [13, 11, 8, 0], IVOIRE)
    ic.bande(miroir(corne), [13, 11, 8, 0], IVOIRE)
    face = [(36, 52), (92, 52), (92, 70), (84, 82), (44, 82), (36, 70)]
    ic.gemme(face, RAGE, table=0.6, teinte_table=c("Rage", "Rouge vif"))
    casque = [(32, 54), (34, 36), (46, 22), (64, 16), (82, 22), (94, 36), (96, 54)]
    ic.gemme(casque, FER_RAGE, table=0.5, centre=(64, 40), teinte_table=c("Rage", "Fer"))
    ic.bande([(30, 52), (98, 52)], 9, [c("Rage", "Fer"), c("Rage", "Fer clair")])
    noir = c("Rage", "Rouge noir")
    orbite = [(42, 58), (58, 58), (57, 70), (47, 70)]
    ic.poly(orbite, noir)
    ic.poly(miroir(orbite), noir)
    lueur = [(49, 62), (53, 64), (50, 67)]
    ic.poly(lueur, c("Rage", "Rouge pâle"))
    ic.poly(miroir(lueur), c("Rage", "Rouge pâle"))
    ic.poly([(64, 68), (60, 77), (68, 77)], noir)
    bouche = [(44, 82), (84, 82), (80, 100), (48, 100)]
    ic.poly(bouche, noir)
    ivoire = c("Rage", "Ivoire clair")
    for i in range(5):
        x = 47 + i * 7.2
        ic.poly([(x, 82), (x + 6, 82), (x + 3, 89)], ivoire)
    machoire = [(42, 98), (86, 98), (82, 112), (64, 118), (46, 112)]
    ic.gemme(machoire, RAGE, table=0.0, centre=(64, 108))
    for i in range(4):
        x = 51 + i * 7
        ic.poly([(x, 98), (x + 6, 98), (x + 3, 91)], c("Rage", "Ivoire"))
    return ic


def viking_saut_percutant():
    ic = Icone("viking_saut_percutant", "viking", "Saut percutant", "Compétence 2 (RB) : bond, frappe au sol, onde de terre.")
    cen = (64, 100)
    ext = [(cen[0] + 54 * math.cos(math.radians(a)), cen[1] + 14 * math.sin(math.radians(a))) for a in range(0, 360, 20)]
    inte = [(cen[0] + 40 * math.cos(math.radians(a)), cen[1] + 8 * math.sin(math.radians(a))) for a in range(0, 360, 20)]
    n = len(ext)
    for i in range(n):
        a, b = ext[i], ext[(i + 1) % n]
        ic.poly([a, b, inte[(i + 1) % n], inte[i]], teinte(TERRE, sub(cen, mul(add(a, b), 0.5))))
    for ang, lg, lar in ((-160, 30, 9), (-135, 34, 10), (-112, 24, 8)):
        for sgn in (1, -1):
            a = polaire((64, 94), 8, ang if sgn > 0 else -180 - ang)
            b = polaire((64, 94), 8 + lg, ang if sgn > 0 else -180 - ang)
            ic.bande([a, b], [lar, 0], TERRE)
    for y in (12, 42):
        chev = [(34, y), (64, y + 22), (94, y), (94, y + 14), (64, y + 36), (34, y + 14)]
        ic.gemme(chev, RAGE, centre=(64, y + 18))
    return ic


def jauge_rage():
    ic = Icone("jauge_rage", "viking", "Rage", "Jauge de classe du viking : poing serré.")
    ic.gemme([(42, 92), (88, 92), (86, 118), (44, 118)], RAGE, table=0.5, teinte_table=c("Rage", "Rouge sombre"))
    ic.gemme([(30, 50), (98, 46), (100, 78), (90, 98), (40, 98), (30, 80)], RAGE, table=0.6,
             teinte_table=c("Rage", "Rouge vif"))
    for i, (x, h) in enumerate(((34, 30), (51, 24), (68, 26), (84, 32))):
        doigt = [(x, 50), (x + 1, h + 6), (x + 5, h + 1), (x + 11, h + 1), (x + 15, h + 6), (x + 16, 50), (x + 8, 56)]
        ic.gemme(doigt, RAGE, table=0.45, teinte_table=c("Rage", "Rouge vif"))
    ic.bande([(24, 58), (36, 76), (62, 72)], [16, 15, 12], RAGE)
    ic.gemme([(62, 64), (70, 70), (62, 79), (56, 71)], [c("Rage", "Rouge vif"), c("Rage", "Rouge pâle")])
    return ic


# ---------------------------------------------------------------------------------------------- communes

def commun_esquive():
    ic = Icone("commun_esquive", "communes", "Esquive, roulade", "Commune (B / Ctrl) : écart rapide.")
    for (x, y, lg) in ((10, 44, 24), (4, 64, 32), (10, 84, 24)):
        ic.bande([(x, y), (x + lg, y)], [0, 7], OS)
    for dx in (0, 30):
        chev = [(40 + dx, 26), (60 + dx, 26), (92 + dx, 64), (60 + dx, 102), (40 + dx, 102), (72 + dx, 64)]
        ic.gemme(chev, OS, centre=(66 + dx, 64))
    return ic


def commun_potion_soin():
    ic = Icone("commun_potion_soin", "communes", "Potion de soin",
               "Commune (croix haut / 1) : vendue par le druide ; liquide or, croix blanc chaud.")
    ic.gemme(regulier((64, 80), 36, 12, -75), SOIN[:3], table=0.6, teinte_table=c("Sacre", "Or sombre"), decalage=0.08)
    ic.bande([(64, 48), (64, 30)], 20, OS)
    ic.bande([(64, 32), (64, 26)], 28, OS)
    ic.bande([(64, 27), (64, 10)], 16, TERRE)
    ic.poly([(40, 66), (47, 58), (44, 76), (37, 80)], SOIN_BLANC)
    b = 6
    cx, cy = 66, 84
    ic.poly([(cx - b, cy - 17), (cx + b, cy - 17), (cx + b, cy - b), (cx + 17, cy - b), (cx + 17, cy + b),
             (cx + b, cy + b), (cx + b, cy + 17), (cx - b, cy + 17), (cx - b, cy + b), (cx - 17, cy + b),
             (cx - 17, cy - b), (cx - b, cy - b)], SOIN_BLANC)
    return ic


def commun_coup_critique():
    ic = Icone("commun_coup_critique", "communes", "Coup critique", "Retour visuel commun (tête, dos, furtif).")
    cen = (64, 64)
    for i in range(8):
        a = -90 + i * 45 + 8
        lg = 58 if i % 2 == 0 else 34
        ic.bande([polaire(cen, 8, a), polaire(cen, lg, a)], [16 if i % 2 == 0 else 11, 0], CRITIQUE)
    ic.gemme(regulier(cen, 16, 8, -90 + 8 + 22.5), CRITIQUE, table=0.5, teinte_table=c("Critique", "Blanc chaud"))
    return ic


# ---------------------------------------------------------------------------------------------- barde, bavaroise, clochard

def tr(cx, cy, k=1.0, ang=0.0, miroir_x=False):
    """Transformation locale -> icône : miroir horizontal éventuel, rotation (degrés), échelle, translation."""
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))

    def f(x, y):
        if miroir_x:
            x = -x
        return (cx + k * (x * ca - y * sa), cy + k * (x * sa + y * ca))

    return f


def _t(f, pts):
    return [f(x, y) for x, y in pts]


def luth(ic, f, k):
    """Luth du barde : caisse en poire, rosace, cordes, manche, chevillier renversé (repère local, caisse en bas)."""
    ic.bande(_t(f, [(0, -2), (0, -44)]), 9 * k, MANCHE_LUTH)
    ic.bande(_t(f, [(0, -44), (-7, -56)]), 8 * k, MANCHE_LUTH)
    corps = [(22 * math.cos(math.radians(a)), 20 + 24 * math.sin(math.radians(a))) for a in range(-20, 201, 20)]
    corps += [(-12, -2), (-6, -8), (6, -8), (12, -2)]
    ic.gemme(_t(f, corps), LUTH, table=0.62, teinte_table=LUTH[1], decalage=0.05)
    ic.gemme(regulier(f(0, 13), 7 * k, 8, 22.5), BA("rosace", "rosace claire"))
    ic.bande(_t(f, [(0, 34), (0, -42)]), 2.8 * k, [BARDE["crème"], BARDE["crème"]])
    ic.bande(_t(f, [(-8, 32), (8, 32)]), 3.6 * k, BA("rosace", "rosace claire"))


def chope(ic, f, k):
    """Chope de la bavaroise (KayKit mug_full_Large) : corps cuivré, cerclages et anse en étain, mousse crème."""
    ic.bande(_t(f, [(14, -12), (26, -12), (31, -2), (31, 8), (26, 16), (14, 16)]), 6.5 * k, ETAIN)
    ic.gemme(_t(f, [(-17, -20), (17, -20), (16, 22), (13, 26), (-13, 26), (-16, 22)]), CUIVRE, table=0.7,
             teinte_table=BAVAROISE["cuivre"], decalage=0.04)
    for y in (-11, 16):
        ic.bande(_t(f, [(-16.8, y), (16.6, y)]), 5.5 * k, ETAIN)
    ic.gemme(_t(f, [(-20, -18), (-22, -26), (-14, -34), (-4, -36), (6, -33), (14, -37), (22, -28), (20, -18)]),
             MOUSSE, centre=f(-2, -26))


def bouteille_kraft(ic, f, k):
    """Bouteille dans son sac en papier kraft : goulot en verre brun (jamais vert)."""
    ic.bande(_t(f, [(0, -6), (0, -30)]), 10 * k, VERRE)
    ic.bande(_t(f, [(-2.5, -8), (-2.5, -27)]), 2.6 * k, [CLOCHARD["kraft clair"], CLOCHARD["kraft clair"]])
    ic.bande(_t(f, [(0, -29), (0, -37)]), 13 * k, ROUGE_USE)
    sac = [(-16, 26), (16, 26), (19, -2), (15, -10), (9, -5), (3, -12), (-3, -6), (-10, -12), (-19, -3)]
    ic.gemme(_t(f, sac), KRAFT, table=0.55, teinte_table=CLOCHARD["kraft"], decalage=0.05)
    ic.bande(_t(f, [(-9, 2), (-7, 20)]), 2.4 * k, [CLOCHARD["kraft sombre"], CLOCHARD["kraft sombre"]])


def nuage_gaz(ic, bouffees, rampe=GAZ):
    for x, y, r in bouffees:
        ic.gemme(regulier((x, y), r, 8, -80), rampe, table=0.55, teinte_table=rampe[1])


def volute(ic, x, y0, y1, amp, larg, rampe):
    n = 9
    pts = [(x + amp * math.sin(math.pi * 2 * i / (n - 1)), y0 + (y1 - y0) * i / (n - 1)) for i in range(n)]
    ic.bande(pts, [larg * (1 - 0.8 * i / (n - 1)) for i in range(n)], rampe)


def chevron(ic, x, y, h, w, d, rampe):
    ic.gemme([(x, y - h), (x + w, y - h), (x + w + d, y), (x + w, y + h), (x, y + h), (x + d, y)], rampe,
             centre=(x + w * 0.5 + d * 0.5, y))


def note(ic, x, y, hampe=48, drapeau=True, rampe=MOUTARDE):
    """Croche : tête ovale inclinée, hampe, drapeau."""
    tete = [(x + 13 * math.cos(math.radians(a)) * math.cos(math.radians(-25)) - 9 * math.sin(math.radians(a)) * math.sin(math.radians(-25)),
             y + 13 * math.cos(math.radians(a)) * math.sin(math.radians(-25)) + 9 * math.sin(math.radians(a)) * math.cos(math.radians(-25)))
            for a in range(0, 360, 36)]
    ic.bande([(x + 10, y - 2), (x + 10, y - hampe)], 6, rampe)
    if drapeau:
        ic.bande([(x + 10, y - hampe), (x + 22, y - hampe + 12), (x + 26, y - hampe + 26), (x + 20, y - hampe + 36)],
                 [7, 7, 5, 0], rampe)
    ic.gemme(tete, rampe, table=0.5, teinte_table=BARDE["crème"])


def impact(ic, cx, cy, n, r0, r1, larg, rampe, a0=-90.0, ouverture=360.0):
    for i in range(n):
        a = a0 + (ouverture * i / (n - 1) if ouverture < 360 else 360.0 * i / n)
        ic.bande([polaire((cx, cy), r0, a), polaire((cx, cy), r1, a)], [larg, 0], rampe)


def piece(ic, x, y, rx, ry, epaisseur=5):
    bord = [(x + rx * math.cos(math.radians(a)), y + ry * math.sin(math.radians(a)) + epaisseur) for a in range(0, 181, 20)]
    bord += [(x - rx, y), (x + rx, y)]
    ic.poly(bord, c("Critique", "Ambre"))
    ic.gemme([(x + rx * math.cos(math.radians(a)), y + ry * math.sin(math.radians(a))) for a in range(0, 360, 30)],
             OR_PIECE, table=0.62, teinte_table=c("Critique", "Or chaud"), decalage=0.05)


# ---- barde

def classe_barde_a():
    ic = Icone("classe_barde_a", "barde_variantes", "Barde, variante A : luth",
               "Luth en diagonale, bois clair et rosace sombre. Cadre bordeaux, fond bordeaux sombre / nuit.")
    cadre_hex(ic, "barde")
    luth(ic, tr(68, 68, 1.02, 38), 1.02)
    return ic


def classe_barde_b():
    ic = Icone("classe_barde_b", "barde_variantes", "Barde, variante B : chapeau à plume",
               "Le grand chapeau bordeaux du barde, ruban moutarde et plume crème.")
    cadre_hex(ic, "barde")
    ic.bande([(80, 64), (92, 46), (98, 30), (98, 18)], [9, 13, 10, 0], CREME)
    ic.gemme([(38, 70), (40, 50), (50, 38), (70, 36), (84, 44), (90, 70)], BORDEAUX, table=0.55,
             teinte_table=BARDE["bordeaux"])
    ic.bande([(39, 63), (89, 63)], 9, MOUTARDE)
    ic.gemme([(18, 80), (30, 71), (50, 68), (78, 68), (98, 71), (110, 80), (100, 88), (64, 91), (28, 88)], BORDEAUX,
             table=0.5, teinte_table=BARDE["bordeaux"])
    return ic


def barde_coup_de_luth():
    ic = Icone("barde_coup_de_luth", "barde", "Coup de luth", "RT : le luth tenu par le manche, comme une massue ; le 3e coup étourdit.")
    luth(ic, tr(62, 66, 1.0, -135), 1.0)
    impact(ic, 96, 34, 5, 16, 30, 8, MOUTARDE, a0=-110, ouverture=150)
    return ic


def barde_jouer():
    ic = Icone("barde_jouer", "barde", "Jouer", "LT maintenu : mélodie qui soigne les alliés proches.")
    ic.bande([(46, 34), (98, 22)], 12, MOUTARDE)
    note(ic, 36, 96, hampe=62, drapeau=False)
    note(ic, 88, 84, hampe=62, drapeau=False)
    for x, y in ((112, 60), (18, 50)):
        ic.gemme([(x, y - 7), (x + 5, y), (x, y + 7), (x - 5, y)], CREME)
    return ic


def barde_ballade_entrainante():
    ic = Icone("barde_ballade_entrainante", "barde", "Ballade entraînante", "LB : alliés proches plus rapides pendant 6 s.")
    note(ic, 34, 94, hampe=64)
    for dx in (0, 24):
        chevron(ic, 70 + dx, 64, 26, 10, 16, BORDEAUX)
    return ic


def barde_accord_dissonant():
    ic = Icone("barde_accord_dissonant", "barde", "Accord dissonant", "RB : onde sonore en cône qui repousse et étourdit.")
    o = (16, 64)
    for i, (r, rampe) in enumerate(((34, MOUTARDE), (60, BORDEAUX), (86, MOUTARDE))):
        pts = [polaire(o, r + (2.5 if j % 2 else -2.5), a) for j, a in enumerate(range(-42, 43, 7))]
        ic.bande(pts, 12 - 2 * i, rampe)
    ic.gemme(regulier(o, 9, 6, 0), BORDEAUX)
    return ic


def barde_jauge_inspiration():
    ic = Icone("barde_jauge_inspiration", "barde", "Inspiration", "Jauge : plume d'écrivain ; monte en frappant et en jouant.")
    ic.bande([(20, 112), (36, 92), (58, 64), (80, 38), (100, 18), (110, 10)], [0, 10, 22, 24, 16, 0], CREME)
    ic.bande([(22, 110), (36, 92), (90, 26)], [4, 3.5, 1.5], MOUTARDE)
    ic.gemme([(14, 116), (22, 104), (28, 110)], BA("moutarde sombre", "moutarde"))
    for x, y, r in ((104, 64, 7), (88, 86, 5)):
        ic.gemme([(x, y - r * 1.5), (x + r, y), (x, y + r * 1.5), (x - r, y)], MOUTARDE)
    return ic


# ---- bavaroise

def classe_bavaroise_a():
    ic = Icone("classe_bavaroise_a", "bavaroise_variantes", "Bavaroise, variante A : chope",
               "Chope cuivrée cerclée d'étain, mousse crème. Cadre étain, fond bleu bavarois.")
    cadre_hex(ic, "bavaroise")
    chope(ic, tr(58, 70, 1.25), 1.25)
    return ic


def classe_bavaroise_b():
    ic = Icone("classe_bavaroise_b", "bavaroise_variantes", "Bavaroise, variante B : bretzel",
               "Bretzel doré et grains de sel. Cadre étain, fond bleu bavarois.")
    cadre_hex(ic, "bavaroise")
    rampe = BV("bretzel sombre", "bretzel", "bretzel clair")
    boucle = [(64 + 34 * math.cos(math.radians(a)), 58 + 28 * math.sin(math.radians(a))) for a in range(25, -206, -15)]
    ic.bande(boucle, 12, rampe)
    fin_g, fin_d = boucle[-1], boucle[0]
    ic.bande([fin_d, (70, 76), (54, 90), (42, 98)], [12, 12, 12, 10], rampe)
    ic.bande([fin_g, (58, 76), (74, 90), (86, 98)], [12, 12, 12, 10], rampe)
    for x, y in ((46, 40), (64, 32), (82, 40), (36, 60), (92, 60), (64, 82)):
        ic.gemme([(x, y - 3), (x + 3, y), (x, y + 3), (x - 3, y)], BV("mousse ombre", "mousse claire"))
    return ic


def bavaroise_coups_de_chopes():
    ic = Icone("bavaroise_coups_de_chopes", "bavaroise", "Coups de chopes", "RT : enchaînement gauche-droite ; le 4e coup repousse.")
    for r, larg in ((50, 7), (40, 5)):
        pts = [polaire((74, 78), r, a) for a in range(-200, -129, 10)]
        ic.bande(pts, [0] + [larg] * (len(pts) - 2) + [0], MOUSSE)
    chope(ic, tr(70, 68, 1.2, 22), 1.2)
    impact(ic, 104, 30, 4, 10, 22, 7, BIERE, a0=-80, ouverture=110)
    return ic


def bavaroise_trinquer():
    ic = Icone("bavaroise_trinquer", "bavaroise", "Trinquer", "LT : une gorgée qui soigne et remplit l'Ivresse.")
    chope(ic, tr(38, 72, 0.95, 18, miroir_x=True), 0.95)
    chope(ic, tr(90, 72, 0.95, -18), 0.95)
    for x, y, r in ((64, 22, 7), (52, 14, 5), (78, 14, 5)):
        ic.gemme(regulier((x, y), r, 7, -90), MOUSSE)
    return ic


def bavaroise_tournee_generale():
    ic = Icone("bavaroise_tournee_generale", "bavaroise", "Tournée générale", "LB : chope lancée qui éclate, mousse glissante en zone.")
    cen = (64, 104)
    ext = [(cen[0] + 54 * math.cos(math.radians(a)), cen[1] + 14 * math.sin(math.radians(a))) for a in range(0, 360, 30)]
    ic.gemme(ext, MOUSSE, table=0.6, teinte_table=BAVAROISE["mousse"])
    for ang, lg in ((-150, 30), (-120, 40), (-90, 44), (-60, 40), (-30, 30)):
        a = polaire((64, 70), 12, ang)
        b = polaire((64, 70), 12 + lg, ang)
        ic.bande([a, b], [14, 0], MOUSSE)
    for x, y, r in ((30, 40, 5), (98, 36, 6), (84, 18, 4), (44, 20, 4)):
        ic.gemme([(x, y - r * 1.4), (x + r, y), (x, y + r * 1.4), (x - r, y)], BIERE)
    chope(ic, tr(64, 76, 0.85, 160), 0.85)
    return ic


def bavaroise_charge_du_tonneau():
    ic = Icone("bavaroise_charge_du_tonneau", "bavaroise", "Charge du tonneau", "RB : elle fonce épaule en avant et renverse tout.")
    for y, lg in ((40, 26), (64, 34), (88, 26)):
        ic.bande([(4, y), (4 + lg, y)], [0, 7], ETAIN)
    f = tr(76, 64, 1.0, 12)
    ic.gemme(_t(f, [(-24, -40), (24, -40), (32, 0), (24, 40), (-24, 40), (-32, 0)]), TERRE, table=0.7,
             teinte_table=c("Terre", "Terre claire"), decalage=0.05)
    for y in (-26, 26):
        ic.bande(_t(f, [(-29 + abs(y) * 0.1, y), (29 - abs(y) * 0.1, y)]), 7, ETAIN)
    for x in (-10, 10):
        ic.bande(_t(f, [(x, -38), (x * 1.3, 0), (x, 38)]), 2.4, [c("Terre", "Terre sombre"), c("Terre", "Terre sombre")])
    return ic


def bavaroise_jauge_ivresse():
    ic = Icone("bavaroise_jauge_ivresse", "bavaroise", "Ivresse", "Jauge : bulles de bière ; plus haute, coups plus forts, elle titube.")
    for x, y, r in ((52, 88, 26), (86, 50, 17), (58, 34, 11), (88, 16, 7), (100, 86, 9)):
        ic.gemme(regulier((x, y), r, 8, -67.5), BIERE, table=0.55, teinte_table=BAVAROISE["ambre"])
        ic.gemme([(x - r * 0.55, y - r * 0.55), (x - r * 0.2, y - r * 0.62), (x - r * 0.42, y - r * 0.2)],
                 BV("mousse", "mousse claire"))
    return ic


# ---- clochard

def classe_clochard_a():
    ic = Icone("classe_clochard_a", "clochard_variantes", "Clochard, variante A : bouteille dans le kraft",
               "Bouteille dans son sac en papier, goulot en verre brun. Cadre gris usé, fond moutarde sombre (le gaz).")
    cadre_hex(ic, "clochard")
    bouteille_kraft(ic, tr(64, 72, 1.4, 12), 1.4)
    return ic


def classe_clochard_b():
    ic = Icone("classe_clochard_b", "clochard_variantes", "Clochard, variante B : baluchon",
               "Baluchon rouge usé à pois crème au bout d'un bâton.")
    cadre_hex(ic, "clochard")
    ic.bande([(26, 106), (92, 30)], 8, TERRE)
    ic.gemme(regulier((80, 60), 22, 9, -90), ROUGE_USE, table=0.55, teinte_table=CLOCHARD["rouge usé"])
    ic.gemme([(72, 38), (88, 36), (92, 44), (70, 46)], ROUGE_USE)
    for x, y in ((72, 56), (88, 64), (78, 72), (90, 50)):
        ic.gemme([(x, y - 3.5), (x + 3.5, y), (x, y + 3.5), (x - 3.5, y)], CREME)
    return ic


def clochard_coup_de_bouteille():
    ic = Icone("clochard_coup_de_bouteille", "clochard", "Coup de bouteille", "RT : le 3e coup fait éclater la bouteille.")
    bouteille_kraft(ic, tr(52, 74, 1.25, 40), 1.25)
    for x, y, a in ((96, 24, 0), (110, 44, 30), (86, 10, -30), (112, 20, 60)):
        f = tr(x, y, 1.0, a)
        ic.gemme(_t(f, [(0, -7), (5, 3), (-4, 5)]), VERRE)
    impact(ic, 92, 36, 3, 8, 16, 5, CL("gaz clair", "gaz pâle"), a0=-30, ouverture=90)
    return ic


def clochard_pet_de_defense():
    ic = Icone("clochard_pet_de_defense", "clochard", "Pet de défense", "LT : petit nuage derrière lui qui repousse et empoisonne.")
    nuage_gaz(ic, [(44, 92, 20), (72, 94, 18), (58, 76, 22), (84, 78, 15)])
    for x in (40, 64, 88):
        volute(ic, x, 50, 12, 6, 7, CL("gaz clair", "gaz pâle"))
    return ic


def clochard_nuage_pestilentiel():
    ic = Icone("clochard_nuage_pestilentiel", "clochard", "Nuage pestilentiel", "LB : grand nuage moutarde qui ralentit et ronge.")
    cen = (64, 104)
    ext = [(cen[0] + 56 * math.cos(math.radians(a)), cen[1] + 15 * math.sin(math.radians(a))) for a in range(0, 360, 20)]
    inte = [(cen[0] + 44 * math.cos(math.radians(a)), cen[1] + 9 * math.sin(math.radians(a))) for a in range(0, 360, 20)]
    n = len(ext)
    for i in range(n):
        a, b = ext[i], ext[(i + 1) % n]
        ic.poly([a, b, inte[(i + 1) % n], inte[i]], teinte(CL("gaz sombre", "gaz"), sub(cen, mul(add(a, b), 0.5))))
    nuage_gaz(ic, [(30, 78, 18), (98, 78, 18), (48, 62, 22), (80, 60, 22), (64, 42, 22), (64, 84, 20), (40, 40, 12),
                   (90, 36, 13)])
    return ic


def clochard_pet_propulsion():
    ic = Icone("clochard_pet_propulsion", "clochard", "Pet-propulsion", "RB : bond en avant, petit nuage au départ.")
    nuage_gaz(ic, [(22, 104, 14), (40, 110, 12), (30, 90, 12)])
    arc = [(34 + 64 * t, 92 - 70 * math.sin(math.pi * 0.5 * t) + 0 * t) for t in [i / 8 for i in range(9)]]
    ic.bande(arc, [3 + 8 * i / 8 for i in range(9)], ROUGE_USE)
    f = tr(arc[-1][0], arc[-1][1], 1.0, math.degrees(math.atan2(arc[-1][1] - arc[-2][1], arc[-1][0] - arc[-2][0])))
    ic.gemme(_t(f, [(-4, -14), (16, 0), (-4, 14)]), ROUGE_USE)
    return ic


def clochard_debrouille():
    ic = Icone("clochard_debrouille", "clochard", "Débrouille", "Passif : un peu plus d'or sur les squelettes (+10 %).")
    piece(ic, 54, 98, 34, 13)
    piece(ic, 54, 84, 34, 13)
    piece(ic, 54, 70, 34, 13)
    ic.gemme([(96 + 18 * math.cos(math.radians(a)), 50 + 18 * math.sin(math.radians(a))) for a in range(0, 360, 30)],
             OR_PIECE, table=0.6, teinte_table=c("Critique", "Or chaud"))
    blanc = c("Critique", "Blanc chaud")
    ic.poly([(92, 30), (100, 30), (100, 38), (108, 38), (108, 46), (100, 46), (100, 54), (92, 54), (92, 46),
             (84, 46), (84, 38), (92, 38)], blanc)
    return ic


def clochard_jauge_gaz():
    ic = Icone("clochard_jauge_gaz", "clochard", "Gaz", "Jauge : bulle de gaz moutarde ; les pets la dépensent.")
    ic.gemme(regulier((56, 72), 38, 10, -72), GAZ, table=0.55, teinte_table=CLOCHARD["gaz"])
    for x, y, r in ((100, 30, 11), (88, 12, 6)):
        ic.gemme(regulier((x, y), r, 8, -67.5), GAZ, table=0.5, teinte_table=CLOCHARD["gaz"])
    ic.gemme([(34, 50), (44, 42), (40, 58)], CL("gaz pâle", "gaz pâle"))
    return ic


# Variante retenue par classe (A par défaut) pour classe_<nom>.svg.
NOUVELLES_CLASSES = {
    "barde": ({"a": classe_barde_a, "b": classe_barde_b}, "a", "Barde",
              [barde_coup_de_luth, barde_jouer, barde_ballade_entrainante, barde_accord_dissonant,
               barde_jauge_inspiration]),
    "bavaroise": ({"a": classe_bavaroise_a, "b": classe_bavaroise_b}, "a", "Bavaroise",
                  [bavaroise_coups_de_chopes, bavaroise_trinquer, bavaroise_tournee_generale,
                   bavaroise_charge_du_tonneau, bavaroise_jauge_ivresse]),
    "clochard": ({"a": classe_clochard_a, "b": classe_clochard_b}, "a", "Clochard",
                 [clochard_coup_de_bouteille, clochard_pet_de_defense, clochard_nuage_pestilentiel,
                  clochard_pet_propulsion, clochard_debrouille, clochard_jauge_gaz]),
}


def classe_retenue(cle):
    variantes, choix, titre, _ = NOUVELLES_CLASSES[cle]
    ic = variantes[choix]()
    ic.nom, ic.famille = "classe_" + cle, "classes"
    ic.titre = "%s (à venir)" % titre
    ic.notes = "Variante %s en attendant le choix de Quentin." % choix.upper()
    return ic


def nouvelles_variantes():
    return [f() for cle in NOUVELLES_CLASSES for f in NOUVELLES_CLASSES[cle][0].values()]


def nouvelles_competences():
    return [f() for cle in NOUVELLES_CLASSES for f in NOUVELLES_CLASSES[cle][3]]


# ---------------------------------------------------------------------------------------------- mécanicien (à venir)

def engrenage(ic, centre, r_ext, r_int, dents, rampe, moyeu, a0=0.0):
    """Roue dentée à facettes, moyeu sombre au centre."""
    pts = []
    for i in range(dents):
        base = a0 + 360.0 * i / dents
        pas = 360.0 / dents
        for da, r in ((-0.30, r_int), (-0.17, r_ext), (0.17, r_ext), (0.30, r_int)):
            pts.append(polaire(centre, r, base + da * pas))
    ic.gemme(pts, rampe, table=0.62, teinte_table=rampe[1], decalage=0.05)
    ic.gemme(regulier(centre, r_int * 0.42, 8, 22.5), moyeu)


def cle(ic, depart, arrivee, largeur=11.0, rampe=ACIER_CLAIR):
    """Clé plate : œil fermé à `depart`, mâchoire ouverte à `arrivee` (clé engineer_Wrench)."""
    f = repere(depart, sub(arrivee, depart))
    lg = math.hypot(*sub(arrivee, depart))
    ic.bande([f(6, 0), f(lg - 10, 0)], largeur, rampe)
    r = largeur * 1.45
    machoire = [f(lg + r * math.cos(math.radians(a)), r * math.sin(math.radians(a))) for a in range(45, 316, 30)]
    machoire += [f(lg + r * 0.7, -r * 0.4), f(lg + 1, -r * 0.4), f(lg + 1, r * 0.4), f(lg + r * 0.7, r * 0.4)]
    ic.gemme(machoire, rampe, centre=f(lg - r * 0.4, 0))
    ic.gemme([f(r * 1.05 * math.cos(math.radians(a)), r * 1.05 * math.sin(math.radians(a))) for a in range(0, 360, 45)],
             rampe, table=0.45, teinte_table=c("Rage", "Fer sombre"), decalage=0.0)


def classe_mecanicien_a():
    ic = Icone("classe_mecanicien_a", "mecanicien_variantes", "Mécanicien, variante A : clé et engrenage",
               "Clé plate en acier sur un engrenage de laiton. Cadre laiton, fond acier (Fer / Fer sombre).")
    cadre_hex(ic, "mecanicien")
    engrenage(ic, (64, 64), 40, 31, 8, LAITON, [c("Rage", "Fer sombre"), c("Rage", "Fer")], a0=22.5)
    cle(ic, (34, 96), (92, 36), largeur=12)
    return ic


def classe_mecanicien_b():
    ic = Icone("classe_mecanicien_b", "mecanicien_variantes", "Mécanicien, variante B : clé et marteau croisés",
               "Clé plate en acier croisée avec un marteau (manche bois, tête laiton).")
    cadre_hex(ic, "mecanicien")
    manche = ((94, 100), (46, 48))
    ic.bande(list(manche), 10, BOIS_CLAIR)
    f = repere(manche[1], sub(manche[1], manche[0]))
    tete = [f(-8, -19), f(8, -19), f(10, -10), f(10, 14), f(-10, 14), f(-10, -10)]
    ic.gemme(tete, LAITON, table=0.55, teinte_table=c("Critique", "Or chaud"))
    cle(ic, (34, 96), (90, 38), largeur=12)
    return ic


# Variante retenue pour classe_mecanicien.svg (A par défaut, à changer selon le choix de Quentin, puis relancer).
MECANICIEN_CHOIX = "a"


def classe_mecanicien():
    ic = {"a": classe_mecanicien_a, "b": classe_mecanicien_b}[MECANICIEN_CHOIX]()
    ic.nom, ic.famille = "classe_mecanicien", "classes"
    ic.titre = "Mécanicien (à venir)"
    ic.notes = "Variante %s en attendant le choix de Quentin." % MECANICIEN_CHOIX.upper()
    return ic


# ---------------------------------------------------------------------------------------------- druide (à venir)

def classe_druide_a():
    ic = Icone("classe_druide_a", "druide_variantes", "Druide, variante A : bois de cerf",
               "Deux bois de cerf ivoire et une pierre d'ambre. Cadre en os, fond terre profonde / charbon.")
    cadre_hex(ic, "druide")
    bois = [(58, 90), (50, 76), (42, 62), (36, 46), (36, 30), (42, 18)]
    andouillers = [([(49, 74), (36, 72), (26, 64)], [6, 4.5, 0]), ([(41, 58), (28, 50), (22, 38)], [5.5, 4, 0]),
                   ([(36, 42), (46, 32), (50, 20)], [5, 3.5, 0])]
    for cote in (lambda pts: pts, miroir):
        for chemin, larg in andouillers:
            ic.bande(cote(chemin), larg, BOIS_CERF)
        ic.bande(cote(bois), [10, 9, 8, 7, 5.5, 0], BOIS_CERF)
    ic.gemme([(64, 78), (74, 90), (64, 104), (54, 90)], AMBRE, table=0.45, teinte_table=c("Critique", "Or clair"))
    return ic


def classe_druide_b():
    ic = Icone("classe_druide_b", "druide_variantes", "Druide, variante B : bâton noueux",
               "Bâton noueux (druid_staff) dont les branches tiennent une pierre d'ambre.")
    cadre_hex(ic, "druide")
    ic.bande([(56, 112), (61, 98), (55, 84), (63, 70), (59, 58), (64, 48)], [13, 14, 12, 14, 12, 11], BOIS_CLAIR)
    for chemin in ([(63, 50), (50, 44), (44, 32), (48, 20), (56, 14)], [(64, 48), (78, 42), (84, 30), (80, 18), (72, 14)]):
        ic.bande(chemin, [11, 9.5, 8, 6, 0], BOIS_CLAIR)
    ic.bande([(58, 86), (46, 80), (38, 70)], [9, 6, 0], BOIS_CLAIR)
    ic.gemme(regulier((64, 30), 13, 7, -90), AMBRE, table=0.5, teinte_table=c("Critique", "Or clair"))
    return ic


def classe_druide_c():
    ic = Icone("classe_druide_c", "druide_variantes", "Druide, variante C : lune et feuille d'automne",
               "Croissant de lune ivoire et feuille de chêne d'automne (ambre, ocre).")
    cadre_hex(ic, "druide")
    ext = [polaire((62, 62), 36, a) for a in range(60, 301, 12)]
    p1, p2 = ext[-1], ext[0]
    cx2 = 96.0
    r2 = math.hypot(p1[0] - cx2, p1[1] - 62)
    a1 = math.degrees(math.atan2(p1[1] - 62, p1[0] - cx2))
    a2 = math.degrees(math.atan2(p2[1] - 62, p2[0] - cx2)) - 360  # par la gauche du second cercle
    inte = [polaire((cx2, 62), r2, a1 + (a2 - a1) * i / 12) for i in range(13)]
    ic.gemme(ext + inte[1:-1], LUNE, centre=(40, 62))
    feuille(ic, (96, 108), (64, 50), AUTOMNE)
    return ic


def feuille(ic, tige, pointe, rampe):
    """Feuille de chêne à lobes, facettée de part et d'autre de la nervure."""
    f = repere(tige, sub(pointe, tige))
    lg = math.hypot(*sub(pointe, tige))
    profil = [(0.12, 4), (0.2, 11), (0.3, 16), (0.4, 10), (0.52, 17), (0.63, 11), (0.75, 15), (0.87, 8), (1.0, 0)]
    ic.bande([f(-8, 0), f(lg * 0.14, 0)], 4, [c("Terre", "Terre claire"), c("Terre", "Sable")])
    for signe in (1, -1):
        bord = [f(lg * k, signe * t) for k, t in profil]
        nerv = [f(lg * k, 0) for k, _ in profil]
        ic.poly([nerv[0]] + bord + [nerv[-1]], rampe[0])
        for i in range(len(profil) - 1):
            e = sub(bord[i + 1], bord[i])
            nn = (-e[1] * signe, e[0] * signe)
            ic.poly([nerv[i], bord[i], bord[i + 1], nerv[i + 1]], teinte(rampe, nn))
    ic.bande([f(lg * 0.1, 0), f(lg * 0.9, 0)], [2.6, 1.2], [c("Terre", "Terre sombre"), c("Terre", "Terre sombre")])


# Variante retenue pour classe_druide.svg (à changer selon le choix de Quentin, puis relancer).
DRUIDE_CHOIX = "a"


def classe_druide():
    ic = {"a": classe_druide_a, "b": classe_druide_b, "c": classe_druide_c}[DRUIDE_CHOIX]()
    ic.nom, ic.famille = "classe_druide", "classes"
    ic.titre = "Druide (à venir)"
    ic.notes = "Variante %s en attendant le choix de Quentin." % DRUIDE_CHOIX.upper()
    return ic


def druide_metamorphose():
    ic = Icone("druide_metamorphose", "druide", "Métamorphose en ours",
               "Provisoire : le druide prend la forme d'une bête.")
    for x in (34, 94):
        ic.gemme(regulier((x, 34), 15, 7, -90), BOIS_DRUIDE, table=0.5, teinte_table=c("Terre", "Terre sombre"))
    tete = [(64 + 44 * math.cos(math.radians(a)), 70 + 40 * math.sin(math.radians(a))) for a in range(-90, 270, 36)]
    ic.gemme(tete, BOIS_DRUIDE, table=0.6, teinte_table=c("Terre", "Terre claire"))
    ic.gemme([(64 + 20 * math.cos(math.radians(a)), 90 + 15 * math.sin(math.radians(a))) for a in range(-90, 270, 45)],
             [c("Terre", "Sable"), c("Os", "Os pâle")], table=0.5, teinte_table=c("Terre", "Sable"))
    sombre = c("Terre", "Terre profonde")
    ic.poly([(56, 80), (72, 80), (64, 88)], sombre)
    for x in (46, 82):
        ic.poly([(x - 6, 64), (x, 58), (x + 6, 64), (x, 68)], sombre)
    return ic


def druide_ronces():
    ic = Icone("druide_ronces", "druide", "Ronces", "Provisoire : ronces qui entravent les ennemis.")
    tiges = [
        [(10, 104), (30, 86), (50, 88), (66, 70), (84, 58), (104, 56), (118, 42)],
        [(18, 30), (36, 36), (50, 52), (60, 76), (78, 94), (100, 100), (116, 116)],
    ]
    for chemin in tiges:
        n = len(chemin)
        ic.bande(chemin, [7 + 5 * math.sin(math.pi * (i + 0.5) / n) for i in range(n)], BOIS_DRUIDE)
        for i in range(1, n - 1):
            a, b = chemin[i], chemin[i + 1]
            m = mul(add(a, b), 0.5)
            d = norm(sub(b, a))
            nr = (-d[1], d[0]) if i % 2 else (d[1], -d[0])
            base1 = add(m, mul(d, -4))
            base2 = add(m, mul(d, 4))
            bout = add(add(m, mul(nr, 13)), mul(d, 4))
            ic.poly([add(base1, mul(nr, 3)), add(base2, mul(nr, 3)), bout], c("Terre", "Sable"))
    return ic


def druide_soin_nature():
    ic = Icone("druide_soin_nature", "druide", "Soin de nature",
               "Provisoire : fleur de lumière, en blanc chaud et or comme les autres soins.")
    cen = (62, 66)
    for i in range(6):
        a = -90 + i * 60
        ic.bande([polaire(cen, 10, a), polaire(cen, 28, a), polaire(cen, 50, a)], [6, 24, 0], SOIN[1:])
    ic.gemme(regulier(cen, 14, 7, -90), AMBRE, table=0.5, teinte_table=c("Critique", "Or clair"))
    for (x, y, r) in ((108, 22, 6), (116, 42, 4)):
        ic.gemme([(x, y - r * 1.5), (x + r, y), (x, y + r * 1.5), (x - r, y)], [c("Sacre", "Or"), SOIN_BLANC])
    return ic


# ---------------------------------------------------------------------------------------------- Nyxessa (emblème du jeu)
# Relique : gemme verte à facettes au-dessus d'un rocher, ceinture de petites gemmes en orbite. Seule icône où le vert
# est de mise (palette Nyxessa). Pas d'hexagone : emblème du jeu (page du wiki, launcher, exe dans la barre des tâches).
# Liseré sombre (encre de l'interface) pour rester lisible sur une barre des tâches claire.

ENCRE = "#161a24"
NYX = [c("Nyxessa", "Émeraude profonde"), c("Nyxessa", "Émeraude"), c("Nyxessa", "Vert Nyx"),
       c("Nyxessa", "Vert clair"), c("Nyxessa", "Éclat")]
ROCHER = [c("Rage", "Fer sombre"), c("Rage", "Fer"), c("Rage", "Fer clair")]
NYXESSA_CHOIX = "a"          # variante des fichiers nyxessa.* (A par défaut)
NYXESSA_SEUIL_SIMPLE = 32    # jusqu'à cette taille (px), la version simplifiée est rastérisée
NYXESSA_TAILLES_PNG = [16, 24, 32, 48, 64, 128, 256, 512, 1024]
NYXESSA_TAILLES_ICO = [256, 128, 64, 48, 32, 24, 16]


def contour_convexe(pts, d):
    """Polygone convexe agrandi de d (liseré d'épaisseur constante)."""
    cen = centroide(pts)
    n = len(pts)
    lignes = []
    for i in range(n):
        a, b = pts[i], pts[(i + 1) % n]
        e = norm(sub(b, a))
        nn = (-e[1], e[0])
        if dot(nn, sub(mul(add(a, b), 0.5), cen)) < 0:
            nn = (-nn[0], -nn[1])
        lignes.append((add(a, mul(nn, d)), e))
    res = []
    for i in range(n):
        (p1, e1), (p2, e2) = lignes[i - 1], lignes[i]
        det = e1[0] * e2[1] - e1[1] * e2[0]
        if abs(det) < 1e-9:
            res.append(p2)
            continue
        q = sub(p2, p1)
        t = (q[0] * e2[1] - q[1] * e2[0]) / det
        res.append(add(p1, mul(e1, t)))
    return res


def cristal(ic, cx, haut, bas, y_large, demi, liseré):
    h = bas - haut
    T, B = (cx, haut), (cx, bas)
    L, R = (cx - demi, y_large), (cx + demi, y_large)
    M1 = (cx - demi * 0.4, y_large + h * 0.06)
    M2 = (cx + demi * 0.42, y_large + h * 0.06)
    if liseré:
        ic.poly(contour_convexe([T, R, B, L], liseré), ENCRE)
    sombre, emeraude, vif, clair, eclat = NYX
    ic.poly([T, L, M1], clair)
    ic.poly([T, M1, M2], vif)
    ic.poly([T, M2, R], emeraude)
    ic.poly([L, B, M1], vif)
    ic.poly([M1, B, M2], emeraude)
    ic.poly([M2, B, R], sombre)
    ic.poly([T, add(T, mul(sub(L, T), 0.72)), add(T, mul(sub(M1, T), 0.42))], eclat)


def ceinture(ic, centre, rx, ry, incl, n, taille, devant, liseré, phase=0.0):
    ca, sa = math.cos(math.radians(incl)), math.sin(math.radians(incl))
    for i in range(n):
        th = math.radians(phase + 360.0 * i / n)
        prof = math.sin(th)
        if (prof > 0) != devant:
            continue
        x0, y0 = rx * math.cos(th), ry * prof
        x, y = centre[0] + x0 * ca - y0 * sa, centre[1] + x0 * sa + y0 * ca
        t = taille * (0.75 + 0.5 * (prof + 1) / 2)
        pts = [(x, y - t * 1.3), (x + t, y), (x, y + t * 1.3), (x - t, y)]
        if liseré:
            ic.poly(contour_convexe(pts, liseré), ENCRE)
        ic.gemme(pts, [NYX[2], NYX[3]] if devant else [NYX[1], NYX[2]], dessous=False)


def rocher(ic, pts, liseré):
    if liseré:
        ic.poly(contour_convexe(pts, liseré), ENCRE)
    ic.gemme(pts, ROCHER, table=0.5, teinte_table=c("Rage", "Fer"))


def nyxessa(variante, simple):
    nom = "nyxessa_%s%s" % (variante, "_simple" if simple else "")
    titre = "Nyxessa, variante %s%s" % (variante.upper(), " (petites tailles)" if simple else "")
    ic = Icone(nom, "nyxessa", titre)
    if variante == "a":
        if simple:
            ic.notes = "Gemme plus large, liseré plus épais, quatre gemmes en orbite."
            ceinture(ic, (64, 66), 58, 17, -10, 4, 10, False, 5, phase=35)
            cristal(ic, 64, 4, 124, 52, 31, 6)
            ceinture(ic, (64, 66), 58, 17, -10, 4, 10, True, 5, phase=35)
        else:
            ic.notes = "Gemme verte à facettes dans sa ceinture de gemmes en orbite."
            ceinture(ic, (64, 64), 58, 13, -10, 22, 3.8, False, 1.6)
            cristal(ic, 64, 4, 124, 52, 26, 3)
            ceinture(ic, (64, 64), 58, 13, -10, 22, 3.8, True, 1.6)
    else:
        caillou = [(38, 124), (90, 124), (96, 112), (86, 100), (64, 96), (44, 100), (32, 112)]
        if simple:
            ic.notes = "Gemme et rocher, sans ceinture."
            rocher(ic, [(x, y + 2) for x, y in caillou], 5)
            cristal(ic, 64, 3, 94, 42, 26, 5)
        else:
            ic.notes = "Gemme en ceinture au-dessus de son rocher (pierre de la place du village)."
            rocher(ic, caillou, 3)
            ic.poly([(40, 108), (90, 106), (92, 110), (38, 112)], NYX[2])
            ceinture(ic, (64, 46), 50, 11, -10, 20, 3.4, False, 1.6)
            cristal(ic, 64, 3, 90, 40, 21, 3)
            ceinture(ic, (64, 46), 50, 11, -10, 20, 3.4, True, 1.6)
    return ic


def nyxessa_pour(variante, taille):
    return nyxessa(variante, taille <= NYXESSA_SEUIL_SIMPLE)


def exporter_nyxessa():
    """SVG des deux variantes (pleine et simplifiée), puis PNG et ICO de la variante retenue dans Nyxessa/."""
    SORTIE_NYXESSA.mkdir(parents=True, exist_ok=True)
    icones = {}
    for v in ("a", "b"):
        for simple in (False, True):
            ic = nyxessa(v, simple)
            icones[ic.nom] = ic
            (SORTIE_NYXESSA / (ic.nom + ".svg")).write_text(ic.svg(), encoding="utf-8", newline="\n")
    for simple in (False, True):
        ic = nyxessa(NYXESSA_CHOIX, simple)
        ic.nom = "nyxessa_simple" if simple else "nyxessa"
        (SORTIE_NYXESSA / (ic.nom + ".svg")).write_text(ic.svg(), encoding="utf-8", newline="\n")
    pngs = {}
    for t in NYXESSA_TAILLES_PNG:
        ic = nyxessa_pour(NYXESSA_CHOIX, t)
        donnees = raster.png(t, t, raster.rasteriser(ic.formes, t))
        (SORTIE_NYXESSA / ("nyxessa_%d.png" % t)).write_bytes(donnees)
        pngs[t] = donnees
    (SORTIE_NYXESSA / "nyxessa.ico").write_bytes(raster.ico([(t, pngs[t]) for t in NYXESSA_TAILLES_ICO]))
    # Aperçus pour la barre des tâches de la planche : vrais PNG rastérisés, les deux variantes.
    apercus = {}
    for v in ("a", "b"):
        for t in (16, 24, 32, 48):
            ic = nyxessa_pour(v, t)
            apercus[(v, t)] = pngs[t] if v == NYXESSA_CHOIX else raster.png(t, t, raster.rasteriser(ic.formes, t))
    return icones, apercus


# ---------------------------------------------------------------------------------------------- catalogue

CLASSES = [classe_paladin, classe_mage_feu, classe_rodeur, classe_assassin, classe_viking, classe_druide,
           classe_mecanicien]
MECANICIEN_VARIANTES = [classe_mecanicien_a, classe_mecanicien_b]
DRUIDE_VARIANTES = [classe_druide_a, classe_druide_b, classe_druide_c]
DRUIDE_COMPETENCES = [druide_metamorphose, druide_ronces, druide_soin_nature]
COMPETENCES = [
    paladin_epee, paladin_garde, paladin_charge_belier, paladin_soin,
    mage_boule_de_feu, mage_cone_de_flammes, mage_brulure, jauge_mana,
    rodeur_tir, rodeur_visee, rodeur_nuee_de_fleches, rodeur_roulade_salve,
    assassin_dague, assassin_arbalete, assassin_fumigene, assassin_furtif,
    viking_hache, viking_attaque_tournante, viking_rugissement, viking_saut_percutant, jauge_rage,
    commun_esquive, commun_potion_soin, commun_coup_critique,
]

NOMS_CLASSES = {"paladin": "Paladin", "mage_feu": "Mage de feu", "rodeur": "Rôdeur", "assassin": "Assassin",
                "viking": "Viking", "communes": "Communes",
                "druide": "Druide", "barde": "Barde", "bavaroise": "Bavaroise", "clochard": "Clochard"}

# Barre de compétences du HUD (RT, LT, LB, RB) : (icône ou None, invite, état) ; état = "", "active", "recharge:N:f".
BARRES = {
    "paladin": [("paladin_epee", "RT", ""), ("paladin_garde", "LT", "active"),
                ("paladin_charge_belier", "LB", "recharge:9:0.62"), ("paladin_soin", "RB", "")],
    "mage_feu": [("mage_boule_de_feu", "RT", ""), ("mage_cone_de_flammes", "LT", ""), (None, "LB", ""),
                 (None, "RB", "")],
    "rodeur": [("rodeur_tir", "RT", ""), ("rodeur_visee", "LT", "active"),
               ("rodeur_nuee_de_fleches", "LB", "recharge:4:0.35"), ("rodeur_roulade_salve", "RB", "")],
    "assassin": [("assassin_dague", "RT", ""), ("assassin_arbalete", "LT", "recharge:6:0.75"),
                 ("assassin_fumigene", "LB", ""), (None, "RB", "")],
    "viking": [("viking_hache", "RT", ""), ("viking_attaque_tournante", "LT", ""), ("viking_rugissement", "LB", ""),
               ("viking_saut_percutant", "RB", "recharge:3:0.25")],
}
BARRES.update({
    "barde": [("barde_coup_de_luth", "RT", ""), ("barde_jouer", "LT", "active"),
              ("barde_ballade_entrainante", "LB", "recharge:5:0.4"), ("barde_accord_dissonant", "RB", "")],
    "bavaroise": [("bavaroise_coups_de_chopes", "RT", ""), ("bavaroise_trinquer", "LT", "recharge:2:0.3"),
                  ("bavaroise_tournee_generale", "LB", ""), ("bavaroise_charge_du_tonneau", "RB", "")],
    "clochard": [("clochard_coup_de_bouteille", "RT", ""), ("clochard_pet_de_defense", "LT", ""),
                 ("clochard_nuage_pestilentiel", "LB", "recharge:7:0.55"), ("clochard_pet_propulsion", "RB", "")],
})
JAUGES = {"mage_feu": ("jauge_mana", "Mana", "#4a8fe0", 0.8), "viking": ("jauge_rage", "Rage", "#f07b2a", 0.55),
          "barde": ("barde_jauge_inspiration", "Inspiration", "#d4a82a", 0.6),
          "bavaroise": ("bavaroise_jauge_ivresse", "Ivresse", "#e0962a", 0.45),
          "clochard": ("clochard_jauge_gaz", "Gaz", "#a8782a", 0.7)}
# Indicateur de passif sur le portrait.
PASSIFS = {"assassin": "assassin_furtif", "clochard": "clochard_debrouille"}

A_TRANCHER = [
    "Rôdeur, Viser (LT) : icône ajoutée (cercle de charge à quatre crans), absente de la liste demandée.",
    "Flèches et carreaux : bois (Terre), fer (accents Fer de Rage), plumes ocre ; aucune lueur.",
    "L'aura de soin du jeu (palette Soin, menthe) n'est pas encore passée en blanc et or : les icônes ont pris "
    "de l'avance sur l'effet.",
]

# Icônes modifiées depuis la version 1 (commit fb1b64a), avec la raison ; l'ancienne version, gardée dans
# Historique/v1/, est montrée à côté de la nouvelle en tête de la planche.
MODIFIEES = [
    # (nom, raison) : icônes changées depuis la dernière version validée. Vide : la v2 (diagonale, soin blanc et or,
    # rôdeur refait) est validée et commitée.
]
HISTORIQUE = ICI / "Historique" / "v1"

# Teintes interdites hors Nyxessa (règle : le vert est réservé à Nyxessa).
VERTS_INTERDITS = set(P["Nyxessa"].values()) | {c("Chasse", "Sous-bois"), c("Chasse", "Forêt"), c("Chasse", "Olive"),
                                                c("Os", "Magie")} | MENTHE


def _verdatre(col):
    """Vrai si la teinte tire vers le vert (teinte entre 56° et 180°, saturée ; le jaune du feu est à 50°) : interdit hors Nyxessa."""
    r, g, b = (int(col[i:i + 2], 16) / 255.0 for i in (1, 3, 5))
    mx, mn = max(r, g, b), min(r, g, b)
    if mx - mn < 0.08:
        return False
    if mx == r:
        h = (60 * (g - b) / (mx - mn)) % 360
    elif mx == g:
        h = 60 * (b - r) / (mx - mn) + 120
    else:
        h = 60 * (r - g) / (mx - mn) + 240
    return 56 <= h <= 180


def verifier(ic):
    couleurs = {col for _, col in ic.formes}
    interdites = set() if ic.famille == "nyxessa" else couleurs & VERTS_INTERDITS
    if not interdites and ic.famille != "nyxessa":
        interdites = {col for col in couleurs if _verdatre(col)}
    if interdites:
        raise SystemExit("%s : teintes vertes interdites %s" % (ic.nom, sorted(interdites)))
    for pts, _ in ic.formes:
        for x, y in pts:
            if not (-0.5 <= x <= 128.5 and -0.5 <= y <= 128.5):
                print("  attention : %s déborde du cadre (%.1f, %.1f)" % (ic.nom, x, y))
                return
    return couleurs


# ---------------------------------------------------------------------------------------------- planche

CSS = """
:root { --encre:#161a24; --nuit:#1b2130; --panneau:#232a3a; --ardoise:#3a4258; --bord:#3d4660; --bord-fort:#56607c;
  --ligne:#333b52; --texte:#f4ecd8; --texte-2:#c3bca9; --texte-off:#8a8578; --or:#d9b264; }
* { box-sizing:border-box; }
html, body { margin:0; background:var(--encre); color:var(--texte);
  font-family:"Fredoka", "Fredoka One", system-ui, sans-serif; }
main { max-width:1180px; margin:0 auto; padding:32px 16px 64px; }
h1 { font-size:34px; font-weight:700; margin:0 0 6px; letter-spacing:.5px; }
h2 { font-size:15px; font-weight:600; color:var(--or); letter-spacing:1.6px; text-transform:uppercase;
  margin:44px 0 14px; }
p.intro, p.note, ul.note { color:var(--texte-2); max-width:880px; line-height:1.45; margin:6px 0; }
.grille { display:grid; grid-template-columns:repeat(auto-fill, minmax(262px, 1fr)); gap:12px; }
.carte { background:var(--nuit); border:1px solid var(--ligne); border-radius:14px; padding:14px 14px 12px; }
.carte .tailles { display:flex; align-items:flex-end; gap:14px; }
.carte svg { display:block; }
.carte .nom { font-weight:600; font-size:17px; margin-top:10px; }
.carte .fichier { font-family:ui-monospace, Consolas, monospace; font-size:12px; color:var(--texte-off); }
.provisoire { display:inline-block; margin-left:8px; padding:1px 8px; border-radius:10px; border:1.5px solid var(--or);
  color:var(--or); font-size:11px; font-weight:600; letter-spacing:.5px; vertical-align:middle; }
.carte .quoi { font-size:13px; color:var(--texte-2); margin-top:3px; line-height:1.35; }
.bande { border-radius:14px; padding:16px; display:flex; flex-wrap:wrap; gap:14px; align-items:center; margin-bottom:10px; }
.bande.ardoise { background:var(--ardoise); }
.bande.encre { background:var(--encre); border:1px solid var(--ligne); }
.bande .etiquette { width:100%; font-size:13px; color:var(--texte-2); }
.bande.ardoise .etiquette { color:var(--texte); }
.hud { background:var(--nuit); border:1px solid var(--ligne); border-radius:14px; padding:18px; margin-bottom:12px;
  display:flex; flex-wrap:wrap; align-items:flex-end; gap:28px; }
.hud .joueur { display:flex; align-items:center; gap:12px; min-width:250px; }
.hud .portrait { position:relative; width:64px; height:64px; }
.hud .portrait .indic { position:absolute; right:-10px; top:-10px; width:32px; height:32px; border-radius:16px;
  background:#1d2230; border:2px solid var(--bord); display:flex; align-items:center; justify-content:center; }
.hud .jauges { background:rgba(16,19,27,.78); border-radius:12px; padding:8px 12px; width:190px; }
.hud .jauge { font-size:12px; color:var(--texte-2); display:flex; align-items:center; gap:6px; margin:3px 0; }
.hud .jauge .barre { flex:1; height:8px; border-radius:4px; background:#10131b; overflow:hidden; }
.hud .jauge .barre i { display:block; height:100%; }
.hud .classe-nom { font-weight:600; font-size:18px; margin-bottom:4px; }
.barre-comp { display:flex; gap:10px; align-items:flex-end; }
.empl { display:flex; flex-direction:column; align-items:center; gap:6px; }
.case { position:relative; width:64px; height:64px; border-radius:12px; border:2px solid var(--bord-fort);
  background:var(--panneau); overflow:hidden; display:flex; align-items:center; justify-content:center; }
.case svg { width:48px; height:48px; }
.case.active { border-color:var(--or); background:#3a3420; }
.case.vide { border-color:var(--ligne); background:rgba(16,19,27,.5); }
.case .voile { position:absolute; left:0; right:0; bottom:0; background:rgba(10,12,18,.62); }
.case .sec { position:absolute; inset:0; display:flex; align-items:center; justify-content:center; font-weight:700;
  font-size:24px; color:#fff; }
.touche { min-width:30px; height:20px; padding:0 5px; border-radius:5px; background:#f4ecd8; color:#1f2433;
  border-bottom:3px solid #9a8f78; font-size:11px; font-weight:700; display:flex; align-items:center;
  justify-content:center; letter-spacing:.3px; }
.empl.vide .touche { opacity:.35; }
.extra { display:flex; gap:10px; align-items:flex-end; padding-left:18px; border-left:1px solid var(--ligne); }
.ennemi { font-size:12px; color:var(--texte-2); display:flex; align-items:center; gap:6px; }
.ennemi .barre { width:110px; height:8px; border-radius:4px; background:#10131b; overflow:hidden; }
.ennemi .barre i { display:block; height:100%; width:64%; background:#e0483e; }
table.pal { border-collapse:collapse; font-size:13px; color:var(--texte-2); }
table.pal td { padding:4px 10px 4px 0; vertical-align:middle; }
.pastille { display:inline-block; width:18px; height:18px; border-radius:4px; vertical-align:middle; margin-right:3px;
  border:1px solid rgba(255,255,255,.08); }
.modifs { display:grid; grid-template-columns:repeat(auto-fill, minmax(min(100%, 520px), 1fr)); gap:12px; }
.modif { background:var(--nuit); border:1px solid var(--ligne); border-radius:14px; padding:12px 14px;
  display:flex; flex-wrap:wrap; align-items:center; gap:12px; }
.modif .tailles { display:flex; align-items:flex-end; gap:10px; }
.modif .lib { font-size:12px; color:var(--texte-off); margin-bottom:4px; }
.modif .fleche-av { color:var(--or); font-size:22px; }
.modif .txt { flex:1; min-width:180px; }
.modif .nom { font-weight:600; font-size:16px; }
.modif .fichier { font-family:ui-monospace, Consolas, monospace; font-size:12px; color:var(--texte-off); }
.modif .quoi { font-size:13px; color:var(--texte-2); margin-top:3px; }
.nyxs { grid-template-columns:repeat(auto-fill, minmax(min(100%, 470px), 1fr)); }
.carte.nyx .tailles { flex-wrap:wrap; margin:8px 0; }
.carte.nyx .fond-clair { display:flex; align-items:flex-end; gap:10px; background:#f3efe6; border-radius:10px; padding:6px; }
.carte.nyx .px { display:flex; flex-direction:column; align-items:center; gap:3px; font-size:11px; color:var(--texte-off); }
.carte.nyx .simple { display:flex; flex-direction:column; align-items:center; font-size:11px; color:var(--texte-off);
  margin-left:12px; }
.carte.nyx img { display:block; }
.barre-tb { display:flex; align-items:center; gap:4px; height:48px; border-radius:8px; padding:0 10px; margin-top:8px; }
.tb-lib { font-size:11px; margin-right:auto; }
.case-tb { width:40px; height:40px; border-radius:6px; display:flex; align-items:center; justify-content:center; }
.case-tb i { display:block; width:24px; height:24px; border-radius:5px; }
.sprite { position:absolute; width:0; height:0; overflow:hidden; }
"""


def _use(nom, taille):
    return ('<svg width="%d" height="%d" viewBox="0 0 128 128" role="img" aria-label="%s"><use href="#i-%s"/></svg>'
            % (taille, taille, html.escape(nom), nom))


def carte(ic, provisoire=False):
    etiquette = '<span class="provisoire">provisoire</span>' if provisoire or ic.famille == "druide" else ""
    return ('<div class="carte"><div class="tailles">%s%s%s</div><div class="nom">%s%s</div>'
            '<div class="fichier">%s.svg</div><div class="quoi">%s</div></div>'
            % (_use(ic.nom, 128), _use(ic.nom, 64), _use(ic.nom, 40), html.escape(ic.titre), etiquette, ic.nom,
               html.escape(ic.notes)))


def hud(classe, par_nom):
    ic_classe = par_nom["classe_" + classe]
    jauges = ['<div class="jauge">Vie<span class="barre"><i style="width:78%;background:#e0483e"></i></span></div>',
              '<div class="jauge">Endurance<span class="barre"><i style="width:60%;background:#f2c14e"></i></span></div>']
    if classe in JAUGES:
        nom, lib, coul, v = JAUGES[classe]
        jauges.append('<div class="jauge">%s%s<span class="barre"><i style="width:%d%%;background:%s"></i></span></div>'
                      % (_use(nom, 20), lib, v * 100, coul))
    indic = ""
    if classe in PASSIFS:
        indic = '<span class="indic">%s</span>' % _use(PASSIFS[classe], 24)
    cases = []
    for nom, invite, etat in BARRES[classe]:
        if nom is None:
            cases.append('<div class="empl vide"><div class="case vide"></div><div class="touche">%s</div></div>' % invite)
            continue
        cls, voile = "case", ""
        if etat == "active":
            cls += " active"
        elif etat.startswith("recharge"):
            _, sec, frac = etat.split(":")
            voile = '<div class="voile" style="height:%d%%"></div><div class="sec">%s</div>' % (float(frac) * 100, sec)
        cases.append('<div class="empl"><div class="%s">%s%s</div><div class="touche">%s</div></div>'
                     % (cls, _use(nom, 48), voile, invite))
    extra = ('<div class="extra"><div class="empl"><div class="case">%s</div><div class="touche">B</div></div>'
             '<div class="empl"><div class="case">%s</div><div class="touche">&#8593;</div></div></div>'
             % (_use("commun_esquive", 48), _use("commun_potion_soin", 48)))
    ennemi = ""
    if classe == "mage_feu":
        ennemi = ('<div class="ennemi">Squelette %s<span class="barre"><i></i></span></div>' % _use("mage_brulure", 22))
    return ('<div class="hud"><div class="joueur"><div class="portrait">%s%s</div><div><div class="classe-nom">%s</div>'
            '<div class="jauges">%s</div></div></div><div class="barre-comp">%s</div>%s%s</div>'
            % (_use(ic_classe.nom, 64), indic, NOMS_CLASSES[classe], "".join(jauges), "".join(cases), extra, ennemi))


def bloc_modifiees(par_nom):
    lignes = []
    for nom, pourquoi in MODIFIEES:
        ic = par_nom[nom]
        avant = ""
        if (HISTORIQUE / (nom + ".svg")).exists():
            avant = ('<div class="av"><div class="lib">avant</div><div class="tailles">'
                     '<svg width="96" height="96" viewBox="0 0 128 128"><use href="#a-%s"/></svg>'
                     '<svg width="40" height="40" viewBox="0 0 128 128"><use href="#a-%s"/></svg></div></div>'
                     '<span class="fleche-av">&#8594;</span>' % (nom, nom))
        apres = ('<div class="av"><div class="lib">après</div><div class="tailles">%s%s</div></div>'
                 % (_use(nom, 96), _use(nom, 40)))
        lignes.append('<div class="modif">%s%s<div class="txt"><div class="nom">%s</div>'
                      '<div class="fichier">%s.svg</div><div class="quoi">%s</div></div></div>'
                      % (avant, apres, html.escape(ic.titre), nom, html.escape(pourquoi)))
    if not lignes:
        return ""
    return '<h2>Modifiées</h2><div class="modifs">%s</div>' % "".join(lignes)


def _img(donnees, taille):
    return ('<img width="%d" height="%d" alt="" style="image-rendering:auto" src="data:image/png;base64,%s">'
            % (taille, taille, base64.b64encode(donnees).decode("ascii")))


def bloc_nyxessa(nyx, apercus):
    cartes = []
    for v in ("a", "b"):
        plein, simple = nyx["nyxessa_%s" % v], nyx["nyxessa_%s_simple" % v]
        petits = "".join('<div class="px"><div>%s</div><span>%d</span></div>' % (_img(apercus[(v, t)], t), t)
                         for t in (48, 32, 24, 16))
        barres = ""
        for theme, fond, autre in (("clair", "#f3f3f3", "#d6d9de"), ("sombre", "#1f1f1f", "#3a3d42")):
            cases = "".join('<div class="case-tb" title="%d px">%s</div>' % (t, _img(apercus[(v, t)], t))
                            for t in (16, 24, 32))
            leurres = "".join('<div class="case-tb"><i style="background:%s"></i></div>' % autre for _ in range(3))
            barres += ('<div class="barre-tb" style="background:%s"><span class="tb-lib" style="color:%s">'
                       'Barre %s · 16, 24, 32 px</span>%s%s</div>'
                       % (fond, "#555" if theme == "clair" else "#aaa", theme, leurres, cases))
        choix = ' <span class="provisoire">nyxessa.*</span>' if v == NYXESSA_CHOIX else ""
        cartes.append(
            '<div class="carte nyx"><div class="nom">%s%s</div><div class="quoi">%s</div>'
            '<div class="tailles">%s%s<div class="fond-clair">%s%s</div></div>'
            '<div class="quoi">Petites tailles (PNG rastérisés par le script, version simplifiée jusqu\'à %d px) :</div>'
            '<div class="tailles">%s<div class="simple">%s<span>SVG simplifié</span></div></div>%s</div>'
            % (html.escape(plein.titre), choix, html.escape(plein.notes), _use(plein.nom, 128), _use(plein.nom, 64),
               _use(plein.nom, 128), _use(plein.nom, 64), NYXESSA_SEUIL_SIMPLE, petits, _use(simple.nom, 64), barres))
    return ('<h2>Nyxessa (emblème du jeu)</h2>'
            '<p class="note">Pour la page du wiki, le launcher et l\'exe du jeu dans la barre des tâches. Palette '
            'Nyxessa, liseré sombre pour les fonds clairs. Fichiers dans <code>ArtSources/Icones/Nyxessa/</code> : '
            'SVG pleins et simplifiés, PNG 16 à 1024 px et <code>nyxessa.ico</code> (16 à 256 px) de la variante %s.</p>'
            '<div class="grille nyxs">%s</div>' % (NYXESSA_CHOIX.upper(), "".join(cartes)))


def bloc_nouvelle_classe(cle, par_nom):
    variantes, choix, titre, comps = NOUVELLES_CLASSES[cle]
    cartes_v = "".join(carte(par_nom["classe_%s_%s" % (cle, v)]) for v in variantes)
    cartes_c = "".join(carte(par_nom[f.__name__]) for f in comps)
    return ('<h2>%s (classe à venir)</h2>'
            '<p class="note">Kit proposé dans le wiki (<code>classe-%s.md</code>, {à confirmer}). Emblème en deux '
            'variantes : le choix de Quentin deviendra <code>classe_%s.svg</code> (aujourd\'hui la variante %s). '
            'Puis une icône par action, la jauge et le passif, et la barre du HUD.</p>'
            '<div class="grille">%s</div><div class="grille" style="margin-top:12px">%s</div>'
            '<div style="margin-top:12px">%s</div>'
            % (titre, cle, cle, choix.upper(), cartes_v, cartes_c, hud(cle, par_nom)))


def bloc_mecanicien(par_nom):
    variantes = "".join(carte(par_nom[f.__name__]) for f in MECANICIEN_VARIANTES)
    return ('<h2>Mécanicien (classe à venir)</h2>'
            '<p class="note">Arme, rôle et compétences pas encore définis (wiki : « bientôt »). Modèle pressenti : '
            'l\'ingénieur KayKit et sa clé <code>engineer_Wrench</code>. Sans vert : laiton (ambre et ors de Critique), '
            'acier clair, fond coupé en diagonale acier / acier sombre (Fer de Rage), cadre laiton. Deux variantes '
            'd\'emblème : le choix de Quentin deviendra <code>classe_mecanicien.svg</code> (aujourd\'hui la '
            'variante %s).</p><div class="grille">%s</div>' % (MECANICIEN_CHOIX.upper(), variantes))


def bloc_druide(par_nom):
    variantes = "".join(carte(par_nom[f.__name__]) for f in DRUIDE_VARIANTES)
    comps = "".join(carte(par_nom[f.__name__], provisoire=True) for f in DRUIDE_COMPETENCES)
    return ('<h2>Druide (classe à venir)</h2>'
            '<p class="note">Arme, rôle et compétences pas encore définis (wiki : « bientôt »). Identité sans vert : '
            'bois et terre, ambre, bois de cerf et lune en os, ocres d\'automne. Cadre en os, fond coupé en '
            'diagonale terre profonde / charbon. Trois variantes d\'emblème : le choix de Quentin deviendra '
            '<code>classe_druide.svg</code> (aujourd\'hui la variante %s).</p>'
            '<div class="grille">%s</div>'
            '<p class="note">Compétences plausibles, <strong>provisoires</strong> : rien n\'est décidé.</p>'
            '<div class="grille">%s</div>' % (DRUIDE_CHOIX.upper(), variantes, comps))


def planche(classes, competences, nyx, apercus):
    par_nom = {ic.nom: ic for ic in classes + competences}
    sprite = "".join('<symbol id="i-%s" viewBox="0 0 128 128">%s</symbol>' % (ic.nom, ic.polygones())
                     for ic in classes + competences + list(nyx.values()))
    for nom, _ in MODIFIEES:
        ancien = HISTORIQUE / (nom + ".svg")
        if ancien.exists():
            polys = "".join(re.findall(r"<polygon [^>]*/>", ancien.read_text(encoding="utf-8")))
            sprite += '<symbol id="a-%s" viewBox="0 0 128 128">%s</symbol>' % (nom, polys)
    corps = []
    corps.append('<h1>Icônes : classes et compétences</h1>')
    corps.append('<p class="intro">Planche de revue générée par <code>ArtSources/Icones/generer_icones.py</code>. '
                 'Chaque icône à 128, 64 et 40 px (taille du HUD) sur le bleu nuit des panneaux (#1b2130). '
                 'Gemmes low poly : polygones à bords nets, lumière unique en haut à gauche, couleurs lues dans les '
                 'palettes de thème du jeu. Les classes ont un cadre hexagonal, les compétences sont un glyphe seul '
                 '(le HUD dessine le cadre et la recharge).</p>')
    for cle in NOUVELLES_CLASSES:
        corps.append(bloc_nouvelle_classe(cle, par_nom))
    corps.append(bloc_nyxessa(nyx, apercus))
    corps.append(bloc_mecanicien(par_nom))
    corps.append(bloc_druide(par_nom))
    corps.append(bloc_modifiees(par_nom))
    corps.append('<h2>À trancher</h2><ul class="note">%s</ul>' % "".join("<li>%s</li>" % html.escape(t) for t in A_TRANCHER))
    corps.append('<h2>Classes</h2><div class="grille">%s</div>'
                 % "".join(carte(ic) for ic in classes if ic.famille == "classes"))
    for famille in ("paladin", "mage_feu", "rodeur", "assassin", "viking", "communes", "druide"):
        liste = [ic for ic in competences if ic.famille == famille]
        corps.append('<h2>%s</h2><div class="grille">%s</div>'
                     % (NOMS_CLASSES[famille], "".join(carte(ic) for ic in liste)))
    corps.append('<h2>Sur fond ardoise et sur l\'encre de l\'interface</h2>')
    tout = classes + competences
    corps.append('<div class="bande ardoise"><div class="etiquette">Ardoise #3a4258, 64 px</div>%s</div>'
                 % "".join(_use(ic.nom, 64) for ic in tout))
    corps.append('<div class="bande ardoise"><div class="etiquette">Ardoise #3a4258, 40 px</div>%s</div>'
                 % "".join(_use(ic.nom, 40) for ic in tout))
    corps.append('<div class="bande encre"><div class="etiquette">Encre #161a24, 40 px</div>%s</div>'
                 % "".join(_use(ic.nom, 40) for ic in tout))
    corps.append('<h2>Mise en situation : barre de compétences du HUD</h2>')
    corps.append('<p class="note">Emplacements de 64 px (icône de 48 px, soit la proportion du HUD : case 96, marge 12). '
                 'Invite écrite RT, LT, LB, RB ; à droite, esquive (B) et potion (croix haut). États montrés : '
                 'bordure or = active (garde levée, visée), voile sombre = recharge. Emplacements vides : '
                 'compétences « vides pour l\'instant » du wiki.</p>')
    for classe in ("paladin", "mage_feu", "rodeur", "assassin", "viking"):
        corps.append(hud(classe, par_nom))
    lignes_pal = []
    for theme in ("Sacre", "Feu", "Chasse", "Terre", "Ombre", "Rage", "Critique", "Os", "BouclierPlein"):
        lignes_pal.append('<tr><td>%s</td><td>%s</td></tr>' % (theme, "".join(
            '<span class="pastille" title="%s %s" style="background:%s"></span>' % (html.escape(n), v, v)
            for n, v in P[theme].items())))
    corps.append('<h2>Palettes lues</h2><table class="pal">%s</table>' % "".join(lignes_pal))
    return ('<!DOCTYPE html>\n<html lang="fr"><head><meta charset="utf-8">'
            '<meta name="viewport" content="width=device-width, initial-scale=1">'
            '<title>Icônes Deathless</title><style>%s</style></head><body>'
            '<svg class="sprite" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">%s</svg><main>%s</main>'
            '</body></html>\n' % (CSS, sprite, "\n".join(corps)))


# ---------------------------------------------------------------------------------------------- main

def main():
    SORTIE_CLASSES.mkdir(parents=True, exist_ok=True)
    SORTIE_COMPETENCES.mkdir(parents=True, exist_ok=True)
    PLANCHE.parent.mkdir(parents=True, exist_ok=True)
    classes = ([f() for f in CLASSES] + [classe_retenue(cle) for cle in NOUVELLES_CLASSES]
               + [f() for f in DRUIDE_VARIANTES] + [f() for f in MECANICIEN_VARIANTES] + nouvelles_variantes())
    competences = [f() for f in COMPETENCES] + [f() for f in DRUIDE_COMPETENCES] + nouvelles_competences()
    total = 0
    for ic in classes + competences:
        verifier(ic)
        dossier = SORTIE_CLASSES if ic.famille == "classes" or ic.famille.endswith("_variantes") else SORTIE_COMPETENCES
        texte = ic.svg()
        (dossier / (ic.nom + ".svg")).write_text(texte, encoding="utf-8", newline="\n")
        total += len(texte.encode("utf-8"))
    attendus = {ic.nom + ".svg" for ic in classes + competences}
    for dossier in (SORTIE_CLASSES, SORTIE_COMPETENCES):
        for f in dossier.glob("*.svg"):
            if f.name not in attendus:
                print("  fichier orphelin (non régénéré) : %s" % f)
    nyx, apercus = exporter_nyxessa()
    PLANCHE.write_text(planche(classes, competences, nyx, apercus), encoding="utf-8", newline="\n")
    print("%d classes, %d compétences, %.1f Ko de SVG ; planche : %s"
          % (len(classes), len(competences), total / 1024.0, PLANCHE))


if __name__ == "__main__":
    main()
