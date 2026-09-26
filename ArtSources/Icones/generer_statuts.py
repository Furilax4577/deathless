#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Icônes des statuts (26/09/2026) : Statuts/statut_<id>.svg (HUD du joueur, au-dessus des ennemis, menu du personnage).

Même langage et mêmes règles que generer_icones.py (primitives, rampes lues dans les palettes du jeu, vérification des
teintes : pas de vert). Glyphe seul sur fond transparent : le HUD dessine la case (liseré rouge d'affliction, or de
bienfait) et la jauge de durée. Formes larges et peu nombreuses : lisibles à 24 px (au-dessus des ennemis, taille ×1).

- statut_brulure : grande flamme et sa petite sœur (Feu, le feu couleur feu) ;
- statut_ralenti : escargot, coquille en terre et sable, corps en os (Terre, Os) ;
- statut_etourdi : trois étoiles d'or sur leur orbite, comme l'indicateur du jeu (Sacré) ;
- statut_ivresse : chope de bois cerclée de fer qui penche, mousse et bulles de bière (Terre, Fer de Rage, Os, Critique) ;
- statut_provoque : veine de colère, quatre crochets rouges (Rage) ;
- statut_renverse : silhouette couchée en os, flèche de bascule qui retombe (Rage) — Renversé, 26/09/2026.

Relançable : `python generer_statuts.py` (écrit Statuts/*.svg ; `--png dossier` ajoute des aperçus PNG à 128, 48 et
24 px). Les SVG sont copiés dans Assets/UI/Icones/Statuts/ par Deathless > UI > 6. Table des icônes (IconesUIOutil).
Identifiants lus par le jeu : CatalogueStatuts (Icone = « statut_ » + id).
"""
import math
import sys
from pathlib import Path

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import generer_icones as g  # noqa: E402
import raster  # noqa: E402

SORTIE = g.ICI / "Statuts"

COQUILLE = [g.c("Terre", "Terre sombre"), g.c("Terre", "Terre claire"), g.c("Terre", "Sable")]
SPIRALE = [g.c("Terre", "Terre profonde"), g.c("Terre", "Terre sombre")]
OR = g.SACRE                                     # or sombre, or, or clair
ORBITE = [g.c("Sacre", "Or sombre"), g.c("Sacre", "Or")]
BOIS = g.BOIS
FER = g.FER_RAGE
MOUSSE = g.OS
BULLES = g.CRITIQUE
RAGE = g.RAGE


def etoile(ic, x, y, r, rampe=OR, pincement=0.3):
    """Étoile à quatre branches (gemmes plates de l'indicateur d'étourdissement)."""
    p = r * pincement
    ic.gemme([(x, y - r), (x + p, y - p), (x + r, y), (x + p, y + p), (x, y + r), (x - p, y + p), (x - r, y), (x - p, y - p)],
             rampe, centre=(x, y))


def statut_brulure():
    ic = g.Icone("statut_brulure", "statuts", "Brûlure", "Statut : dégâts par seconde (mage, style feu).")
    g.flamme(ic, 96, 120, 58, 22)
    g.flamme(ic, 56, 122, 108, 44)
    return ic


def statut_ralenti():
    ic = g.Icone("statut_ralenti", "statuts", "Ralenti", "Statut : déplacements ralentis (chute, eau du donjon).")
    # Corps (os) : pied allongé, tête relevée à gauche, deux cornes.
    ic.bande([(24, 110), (60, 114), (124, 110)], [26, 24, 10], MOUSSE)
    ic.bande([(26, 112), (18, 88), (16, 66)], [26, 22, 19], MOUSSE)
    ic.bande([(12, 70), (7, 42)], [9, 6], MOUSSE)
    ic.bande([(22, 70), (30, 38)], [9, 6], MOUSSE)
    ic.gemme(g.regulier((8, 40), 6.5, 6, -90), MOUSSE)
    ic.gemme(g.regulier((31, 36), 6.5, 6, -90), MOUSSE)
    # Coquille : gemme ronde à table, spirale en terre profonde.
    cx, cy = 78, 58
    ic.gemme(g.regulier((cx, cy), 46, 14, -90), COQUILLE, table=0.62, teinte_table=g.c("Terre", "Terre claire"), decalage=0.08)
    spirale = []
    for i in range(22):
        a = math.radians(-90 + i * 30)
        r = 7 + i * 1.4
        spirale.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    ic.bande(spirale, 10, SPIRALE)
    return ic


def statut_etourdi():
    ic = g.Icone("statut_etourdi", "statuts", "Étourdi", "Statut : ni déplacement ni attaque (charge, parade, saut percutant).")
    # Orbite : anneau plat derrière les étoiles.
    orbite = [(64 + 58 * math.cos(math.radians(a)), 82 + 22 * math.sin(math.radians(a))) for a in range(0, 361, 20)]
    ic.bande(orbite, 9, ORBITE)
    etoile(ic, 64, 42, 40, pincement=0.32)
    etoile(ic, 25, 88, 24, pincement=0.34)
    etoile(ic, 103, 92, 24, pincement=0.34)
    return ic


def statut_ivresse():
    ic = g.Icone("statut_ivresse", "statuts", "Ivresse", "Statut : la tête tourne (taverne) ; un bienfait, liseré or.")
    f = g.tr(70, 74, 1.55, 14)
    ic.bande(g._t(f, [(18, -12), (30, -10), (34, 2), (30, 14), (18, 16)]), 8 * 1.55, FER)          # anse
    ic.gemme(g._t(f, [(-20, -20), (20, -20), (18, 28), (-18, 28)]), BOIS, table=0.62,
             teinte_table=g.c("Terre", "Terre claire"), decalage=0.05)                                # fût en bois
    for y in (-8, 18):
        ic.bande(g._t(f, [(-19.6, y), (19.4, y)]), 6 * 1.55, FER)                                      # cerclages
    ic.gemme(g._t(f, [(-24, -18), (-26, -27), (-15, -35), (-3, -37), (8, -33), (17, -38), (26, -28), (24, -18)]),
             MOUSSE, centre=f(-2, -26))                                                                  # mousse
    for x, y, r in ((20, 38, 11), (34, 14, 7), (14, 64, 6)):
        ic.gemme(g.regulier((x, y), r, 8, -67.5), BULLES, table=0.5, teinte_table=g.c("Critique", "Or chaud"))
    return ic


def statut_provoque():
    ic = g.Icone("statut_provoque", "statuts", "Provoqué", "Statut : s'acharne sur le héros qui l'a provoqué (rugissement).")
    cx, cy, a, b = 64, 64, 11, 46
    for dx, dy in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
        ic.bande([(cx + dx * a, cy + dy * b), (cx + dx * a, cy + dy * a), (cx + dx * b, cy + dy * a)], 17, RAGE)
    return ic


def statut_renverse():
    ic = g.Icone("statut_renverse", "statuts", "Renversé", "Statut : tombe à la renverse puis se relève, sans contrôle "
        "(charge écrasante de Morgrim massue, onde de choc non sautée, grosse chute).")
    # Silhouette couchée (os) : tête ronde à gauche, corps allongé au sol, bras étendu.
    ic.gemme(g.regulier((30, 100), 13, 10, 0), MOUSSE)
    ic.bande([(41, 100), (58, 96), (86, 96), (104, 92)], [18, 16, 14, 12], MOUSSE)
    ic.bande([(60, 97), (66, 78), (64, 58)], [8, 7, 6], MOUSSE)
    # Flèche de bascule : arc qui part debout (haut) et retombe à la renverse, pointe vers le bas.
    arc = []
    cx, cy, r = 70, 52, 40
    for i in range(16):
        a = math.radians(205 - i * 8.5)
        arc.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    ic.bande(arc, 10, RAGE)
    bx, by = arc[-1]
    ic.gemme([(bx - 13, by - 3), (bx + 3, by - 15), (bx + 9, by + 9)], RAGE, centre=(bx, by - 3))
    return ic


STATUTS = [statut_brulure, statut_ralenti, statut_etourdi, statut_ivresse, statut_provoque, statut_renverse]


def main():
    SORTIE.mkdir(parents=True, exist_ok=True)
    apercu = None
    if "--png" in sys.argv:
        apercu = Path(sys.argv[sys.argv.index("--png") + 1])
        apercu.mkdir(parents=True, exist_ok=True)
    for f in STATUTS:
        ic = f()
        g.verifier(ic)
        (SORTIE / (ic.nom + ".svg")).write_text(ic.svg(), encoding="utf-8", newline="\n")
        if apercu is not None:
            for taille in (128, 48, 24):
                rgba = raster.rasteriser(ic.formes, taille)
                (apercu / ("%s_%d.png" % (ic.nom, taille))).write_bytes(raster.png(taille, taille, rgba))
    print("%d icônes de statut dans %s" % (len(STATUTS), SORTIE))


if __name__ == "__main__":
    main()
