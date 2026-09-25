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
import codecs
import html
import math
import re
from pathlib import Path

ICI = Path(__file__).resolve().parent
MAIN = ICI.parent.parent
DOSSIER_PALETTES = MAIN / "Assets" / "VFX" / "_Palettes"
SORTIE_CLASSES = ICI / "Classes"
SORTIE_COMPETENCES = ICI / "Competences"
PLANCHE = MAIN / "Docs" / "icones" / "planche.html"

# ---------------------------------------------------------------------------------------------- palettes

# Secours si les assets de palette sont absents (valeurs relevées le 25/09/2026, identiques aux assets).
SECOURS = {
    "Feu": {"Braise": "#4a1206", "Rouge": "#cc1f08", "Orange": "#ff610a", "Jaune": "#ffe666",
            "Blanc chaud": "#fff4d6", "Charbon": "#2e2a28", "Cendre": "#6a615a"},
    "Nyxessa": {"Émeraude sombre": "#145032", "Émeraude": "#1e5a32", "Vert vif": "#3fae5a",
                "Vert clair": "#9fe870", "Éclat": "#e8ffc8"},
    "Terre": {"Terre profonde": "#3a281a", "Terre sombre": "#5b3f2a", "Terre claire": "#8a6a48",
              "Sable": "#b8966c", "Pierre": "#8c877f"},
    "Rage": {"Rouge noir": "#3a0a08", "Rouge sombre": "#6e1410", "Rouge vif": "#b3261e", "Rouge pâle": "#ff7359",
             "Ivoire": "#e8dcc0", "Ivoire clair": "#fff5e0", "Fer": "#5a5f66", "Fer sombre": "#3f444a",
             "Fer clair": "#7a8088"},
    "Sacre": {"Nuit": "#1e2a3a", "Or sombre": "#b8903a", "Or": "#e8c872", "Or clair": "#f4e2a8", "Acier": "#5a7aa0"},
    "Soin": {"Menthe profonde": "#1b6a4c", "Menthe sombre": "#2e9e72", "Menthe": "#4fcf9a",
             "Menthe claire": "#b8f5d8"},
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
SOIN = [c("Soin", "Menthe sombre"), c("Soin", "Menthe"), c("Soin", "Menthe claire")]
CRITIQUE = [c("Critique", "Ambre"), c("Critique", "Or chaud"), c("Critique", "Or clair")]
OS = [c("Os", "Os gris"), c("Os", "Os"), c("Os", "Os pâle")]
MANA = [c("BouclierPlein", "Bleu nuit"), c("BouclierPlein", "Bleu"), c("BouclierPlein", "Bleu vif"),
        c("BouclierPlein", "Bleu pâle")]

# Cadres des classes : rampe de la bordure (sombre -> claire, 4 niveaux) et fond de l'hexagone.
CADRES = {
    "paladin": ([c("Sacre", "Or sombre"), c("Sacre", "Or"), c("Sacre", "Or"), c("Sacre", "Or clair")],
                c("Sacre", "Nuit")),
    "mage_feu": ([c("Feu", "Rouge"), c("Feu", "Orange"), c("Feu", "Orange"), c("Feu", "Jaune")], c("Feu", "Braise")),
    "rodeur": ([c("Terre", "Terre claire"), c("Chasse", "Ocre"), c("Chasse", "Ocre"), c("Chasse", "Ocre clair")],
               c("Terre", "Terre profonde")),
    "assassin": ([c("Ombre", "Violet"), c("Ombre", "Violet"), c("Ombre", "Lilas"), c("Ombre", "Lilas")],
                 c("Ombre", "Nuit")),
    "viking": ([c("Rage", "Rouge sombre"), c("Rage", "Rouge vif"), c("Rage", "Rouge vif"), c("Rage", "Rouge pâle")],
               c("Rage", "Rouge noir")),
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
    rampe, fond = CADRES[classe]
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
    ic.poly(inte, fond)


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
    ic = Icone("classe_rodeur", "classes", "Rôdeur", "Arc et flèche croisés, bois et ocres (Chasse, sans vert).")
    cadre_hex(ic, "rodeur")
    centre = (98, 64)
    arc = [polaire(centre, 58, a) for a in range(142, 219, 8)]
    haut, bas = arc[0], arc[-1]
    ic.bande([bas, haut], 2.6, [CORDE, CORDE])
    ic.bande(arc, [4, 6, 8, 9, 10, 10, 9, 8, 6, 4], ARC)
    ic.bande([polaire(centre, 58, 174), polaire(centre, 58, 186)], 12, OCRE)
    fleche(ic, (40, 104), (98, 26), epaisseur=5, tete=15)
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
    ic.bande([(64, 112), (64, 22)], 8, RAGE)
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
    ic = Icone("paladin_soin", "paladin", "Soin sur soi", "Compétence 2 (RB) : croix de soin (thème Soin, menthe).")
    b = 13
    cx, cy = 58, 70
    croix = [(cx - b, cy - 44), (cx + b, cy - 44), (cx + b, cy - b), (cx + 44, cy - b), (cx + 44, cy + b),
             (cx + b, cy + b), (cx + b, cy + 44), (cx - b, cy + 44), (cx - b, cy + b), (cx - 44, cy + b),
             (cx - 44, cy - b), (cx - b, cy - b)]
    ic.gemme(croix, SOIN, table=0.62, centre=(cx, cy), teinte_table=c("Soin", "Menthe"), decalage=0.04)
    for (x, y, r) in ((106, 40, 7), (96, 20, 5), (114, 20, 4)):
        ic.gemme([(x, y - r * 1.5), (x + r, y), (x, y + r * 1.5), (x - r, y)],
                 [c("Soin", "Menthe"), c("Soin", "Menthe claire")])
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
    ic = Icone("commun_potion_soin", "communes", "Potion de soin", "Commune (croix haut / 1) : vendue par le druide.")
    ic.gemme(regulier((64, 80), 36, 12, -75), SOIN, table=0.6, teinte_table=c("Soin", "Menthe"), decalage=0.08)
    ic.bande([(64, 48), (64, 30)], 20, OS)
    ic.bande([(64, 32), (64, 26)], 28, OS)
    ic.bande([(64, 27), (64, 10)], 16, TERRE)
    ic.poly([(44, 64), (50, 58), (46, 76), (40, 80)], c("Soin", "Menthe claire"))
    b = 5
    cx, cy = 66, 84
    ic.poly([(cx - b, cy - 16), (cx + b, cy - 16), (cx + b, cy - b), (cx + 16, cy - b), (cx + 16, cy + b),
             (cx + b, cy + b), (cx + b, cy + 16), (cx - b, cy + 16), (cx - b, cy + b), (cx - 16, cy + b),
             (cx - 16, cy - b), (cx - b, cy - b)], c("Os", "Os pâle"))
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


# ---------------------------------------------------------------------------------------------- catalogue

CLASSES = [classe_paladin, classe_mage_feu, classe_rodeur, classe_assassin, classe_viking]
COMPETENCES = [
    paladin_epee, paladin_garde, paladin_charge_belier, paladin_soin,
    mage_boule_de_feu, mage_cone_de_flammes, mage_brulure, jauge_mana,
    rodeur_tir, rodeur_visee, rodeur_nuee_de_fleches, rodeur_roulade_salve,
    assassin_dague, assassin_arbalete, assassin_fumigene, assassin_furtif,
    viking_hache, viking_attaque_tournante, viking_rugissement, viking_saut_percutant, jauge_rage,
    commun_esquive, commun_potion_soin, commun_coup_critique,
]

NOMS_CLASSES = {"paladin": "Paladin", "mage_feu": "Mage de feu", "rodeur": "Rôdeur", "assassin": "Assassin",
                "viking": "Viking", "communes": "Communes"}

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
JAUGES = {"mage_feu": ("jauge_mana", "Mana", "#4a8fe0", 0.8), "viking": ("jauge_rage", "Rage", "#f07b2a", 0.55)}

A_TRANCHER = [
    "Soin sur soi et potion de soin : thème Soin (menthe) comme l'aura du jeu. C'est une teinte vert-bleu, proche de "
    "la règle « le vert est réservé à Nyxessa » : garder la menthe, ou passer le soin en blanc et or ?",
    "Mana : aucun thème d'effet ; la goutte reprend le bleu de la jauge du HUD (palette BouclierPlein). "
    "Autre piste : une gemme de feu.",
    "Rage : poing serré en rouges du thème Rage (la jauge du HUD est orange #f07b2a).",
    "Rôdeur, Viser (LT) : icône ajoutée (cercle de charge à quatre crans), absente de la liste demandée.",
    "Classe Assassin : capuche et regard plutôt qu'une dague, pour ne pas doubler l'icône de la dague.",
    "Flèches et carreaux : bois (Terre), fer (accents Fer de Rage), plumes ocre ; aucune lueur.",
]

# Teintes interdites hors Nyxessa (règle : le vert est réservé à Nyxessa).
VERTS_INTERDITS = set(P["Nyxessa"].values()) | {c("Chasse", "Sous-bois"), c("Chasse", "Forêt"), c("Chasse", "Olive"),
                                                c("Os", "Magie")}


def verifier(ic):
    couleurs = {col for _, col in ic.formes}
    interdites = couleurs & VERTS_INTERDITS
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
.sprite { position:absolute; width:0; height:0; overflow:hidden; }
"""


def _use(nom, taille):
    return ('<svg width="%d" height="%d" viewBox="0 0 128 128" role="img" aria-label="%s"><use href="#i-%s"/></svg>'
            % (taille, taille, html.escape(nom), nom))


def carte(ic):
    return ('<div class="carte"><div class="tailles">%s%s%s</div><div class="nom">%s</div>'
            '<div class="fichier">%s.svg</div><div class="quoi">%s</div></div>'
            % (_use(ic.nom, 128), _use(ic.nom, 64), _use(ic.nom, 40), html.escape(ic.titre), ic.nom,
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
    if classe == "assassin":
        indic = '<span class="indic">%s</span>' % _use("assassin_furtif", 24)
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


def planche(classes, competences):
    par_nom = {ic.nom: ic for ic in classes + competences}
    sprite = "".join('<symbol id="i-%s" viewBox="0 0 128 128">%s</symbol>' % (ic.nom, ic.polygones())
                     for ic in classes + competences)
    corps = []
    corps.append('<h1>Icônes : classes et compétences</h1>')
    corps.append('<p class="intro">Planche de revue générée par <code>ArtSources/Icones/generer_icones.py</code>. '
                 'Chaque icône à 128, 64 et 40 px (taille du HUD) sur le bleu nuit des panneaux (#1b2130). '
                 'Gemmes low poly : polygones à bords nets, lumière unique en haut à gauche, couleurs lues dans les '
                 'palettes de thème du jeu. Les classes ont un cadre hexagonal, les compétences sont un glyphe seul '
                 '(le HUD dessine le cadre et la recharge).</p>')
    corps.append('<h2>À trancher</h2><ul class="note">%s</ul>' % "".join("<li>%s</li>" % html.escape(t) for t in A_TRANCHER))
    corps.append('<h2>Classes</h2><div class="grille">%s</div>' % "".join(carte(ic) for ic in classes))
    for famille in ("paladin", "mage_feu", "rodeur", "assassin", "viking", "communes"):
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
    for theme in ("Sacre", "Feu", "Chasse", "Terre", "Ombre", "Rage", "Soin", "Critique", "Os", "BouclierPlein"):
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
    classes = [f() for f in CLASSES]
    competences = [f() for f in COMPETENCES]
    total = 0
    for ic in classes + competences:
        verifier(ic)
        dossier = SORTIE_CLASSES if ic.famille == "classes" else SORTIE_COMPETENCES
        texte = ic.svg()
        (dossier / (ic.nom + ".svg")).write_text(texte, encoding="utf-8", newline="\n")
        total += len(texte.encode("utf-8"))
    attendus = {ic.nom + ".svg" for ic in classes + competences}
    for dossier in (SORTIE_CLASSES, SORTIE_COMPETENCES):
        for f in dossier.glob("*.svg"):
            if f.name not in attendus:
                print("  fichier orphelin (non régénéré) : %s" % f)
    PLANCHE.write_text(planche(classes, competences), encoding="utf-8", newline="\n")
    print("%d classes, %d compétences, %.1f Ko de SVG ; planche : %s"
          % (len(classes), len(competences), total / 1024.0, PLANCHE))


if __name__ == "__main__":
    main()
