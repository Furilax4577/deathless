#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Icônes de la roue à emotes (8 glyphes, 26/09/2026) : Emotes/emote_<id>.svg.

Même langage et mêmes règles que generer_icones.py (dont on reprend les primitives, les rampes et la vérification
des teintes) : polygones pleins, facettes à lumière haut gauche, cadre 128 × 128, pas de vert. Les personnages sont
en os ivoire (rampe OS), les accents en or (CRITIQUE), la chope est celle de la bavaroise.

Relançable : `python generer_emotes.py` (écrit Emotes/*.svg ; `--png dossier` ajoute un aperçu PNG par icône).
Les SVG sont copiés dans Assets/UI/Icones/Emotes/ par Deathless > UI > 6. Table des icônes (IconesUIOutil).
Identifiants lus par le jeu : EmotesHeros.Definitions (Icone = « emote_ » + id).
"""
import math
import sys
from pathlib import Path

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
import generer_icones as g  # noqa: E402
import raster  # noqa: E402

SORTIE = g.ICI / "Emotes"
OS, CRITIQUE, RAGE, TERRE = g.OS, g.CRITIQUE, g.RAGE, g.TERRE
ENCRE = [g.c("Rage", "Fer sombre"), g.c("Rage", "Fer sombre")]
SOL = [g.c("Terre", "Terre sombre"), g.c("Terre", "Terre claire")]


def tete(ic, x, y, r=11.0):
    ic.gemme(g.regulier((x, y), r, 8, -90 + 22.5), OS, table=0.55, teinte_table=OS[1], decalage=0.08)


def membre(ic, pts, largeur):
    ic.bande(pts, largeur, OS)


def sol(ic, y, x0=14, x1=114):
    ic.bande([(x0, y), (x1, y)], 5, SOL)


def etincelle(ic, x, y, r):
    """Petite étoile à quatre branches (or)."""
    ic.gemme([(x, y - r), (x + r * 0.3, y - r * 0.3), (x + r, y), (x + r * 0.3, y + r * 0.3),
              (x, y + r), (x - r * 0.3, y + r * 0.3), (x - r, y), (x - r * 0.3, y - r * 0.3)], CRITIQUE, centre=(x, y))


def lettre_z(ic, x, y, s, rampe):
    e = 0.24 * s
    ic.gemme([(x, y), (x + s, y), (x + s, y + e), (x + e * 1.2, y + s - e), (x + s, y + s - e), (x + s, y + s),
              (x, y + s), (x, y + s - e), (x + s - e * 1.2, y + e), (x, y + e)], rampe, centre=(x + s / 2, y + s / 2))


def emote_salut():
    ic = g.Icone("emote_salut", "emotes", "Salut", "Roue à emotes : Waving.")
    for i, x in enumerate((47, 58, 69, 80)):
        haut = (30, 20, 22, 32)[i]
        membre(ic, [(x, 62), (x + (i - 1.5) * 3, haut)], 10)
    membre(ic, [(44, 84), (30, 66), (26, 56)], 10)
    ic.gemme([(42, 58), (86, 58), (88, 94), (74, 110), (54, 110), (40, 94)], OS, table=0.5, teinte_table=OS[1])
    for sens in (1, -1):   # traits du geste, de part et d'autre de la main
        cx = 64 + sens * 44
        ic.bande([(cx - sens * 4, 24), (cx + sens * 2, 34), (cx + sens * 4, 46)], [0, 6, 0], CRITIQUE)
        ic.bande([(cx - sens * 2, 58), (cx + sens * 4, 66), (cx + sens * 6, 78)], [0, 6, 0], CRITIQUE)
    return ic


def emote_acclamation():
    ic = g.Icone("emote_acclamation", "emotes", "Acclamation", "Roue à emotes : Cheering.")
    membre(ic, [(58, 90), (50, 118)], 11)
    membre(ic, [(70, 90), (78, 118)], 11)
    membre(ic, [(55, 62), (34, 30)], 9)
    membre(ic, [(73, 62), (94, 30)], 9)
    for x in (32, 96):
        ic.gemme(g.regulier((x, 27), 7.5, 6, -90), OS, centre=(x, 27))
    ic.gemme([(51, 56), (77, 56), (73, 92), (55, 92)], OS, table=0.45, teinte_table=OS[1])
    tete(ic, 64, 41, 12)
    etincelle(ic, 64, 12, 9)
    etincelle(ic, 16, 50, 7)
    etincelle(ic, 112, 50, 7)
    return ic


def emote_provocation():
    ic = g.Icone("emote_provocation", "emotes", "Provocation", "Roue à emotes : Skeletons_Taunt.")
    ic.bande([(60, 96), (60, 122)], 26, OS)
    ic.gemme([(38, 52), (86, 50), (92, 84), (80, 100), (46, 100), (36, 84)], OS, table=0.55, teinte_table=OS[1])
    for x in (50, 62, 74):
        ic.bande([(x, 53), (x + 1, 66)], 2.6, [OS[0], OS[0]])
    ic.bande([(42, 76), (72, 72)], 11, [OS[1], OS[2]])
    # Veine de colère (rouge de Rage) : quatre crochets autour d'un point.
    cx, cy, a, b = 102, 26, 5, 13
    for dx, dy in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
        ic.bande([(cx + dx * a, cy + dy * b), (cx + dx * a, cy + dy * a), (cx + dx * b, cy + dy * a)], 5, RAGE)
    return ic


def emote_assis():
    ic = g.Icone("emote_assis", "emotes", "S'asseoir", "Roue à emotes : Sit_Floor_Down, Sit_Floor_Idle, Sit_Floor_StandUp.")
    sol(ic, 112)
    membre(ic, [(50, 94), (84, 94), (90, 106)], 13)
    membre(ic, [(48, 50), (48, 94)], 17)
    membre(ic, [(50, 56), (64, 78), (82, 84)], 9)
    tete(ic, 49, 34, 12)
    return ic


def emote_repos():
    ic = g.Icone("emote_repos", "emotes", "Se reposer", "Roue à emotes : Lie_Down, Lie_Idle, Lie_StandUp.")
    sol(ic, 108)
    membre(ic, [(40, 92), (90, 94)], 16)
    membre(ic, [(90, 94), (116, 97)], 12)
    membre(ic, [(50, 90), (66, 80), (80, 88)], 8)
    tete(ic, 27, 88, 12)
    lettre_z(ic, 62, 18, 24, [g.c("Os", "Os"), g.c("Os", "Os pâle")])
    lettre_z(ic, 92, 44, 15, [g.c("Os", "Os"), g.c("Os", "Os pâle")])
    return ic


def emote_pompes():
    ic = g.Icone("emote_pompes", "emotes", "Pompes", "Roue à emotes : Push_Ups.")
    sol(ic, 104)
    membre(ic, [(86, 70), (22, 94)], 15)
    membre(ic, [(80, 72), (82, 101)], 9)
    tete(ic, 98, 62, 11)
    # Double flèche verticale (or) : haut, bas.
    x = 50
    ic.bande([(x, 30), (x, 60)], 6, CRITIQUE)
    ic.gemme([(x, 16), (x + 10, 30), (x - 10, 30)], CRITIQUE, centre=(x, 26))
    ic.gemme([(x, 74), (x + 10, 60), (x - 10, 60)], CRITIQUE, centre=(x, 64))
    return ic


def emote_boire():
    ic = g.Icone("emote_boire", "emotes", "Boire un coup", "Roue à emotes : Use_Item, chope KayKit (pleine puis vide).")
    g.chope(ic, g.tr(60, 74, 1.5), 1.5)
    return ic


def emote_mort():
    ic = g.Icone("emote_mort", "emotes", "Faire le mort", "Roue à emotes : Death_B, puis Lie_StandUp.")
    sol(ic, 108)
    membre(ic, [(58, 94), (60, 64)], 8)
    membre(ic, [(74, 94), (76, 64)], 8)
    membre(ic, [(44, 96), (92, 97)], 16)
    membre(ic, [(92, 97), (116, 99)], 12)
    tete(ic, 29, 90, 15)
    for x in (23, 35):   # yeux en croix
        y = 87
        ic.bande([(x - 3.5, y - 3.5), (x + 3.5, y + 3.5)], 2.8, ENCRE, dessous=False)
        ic.bande([(x - 3.5, y + 3.5), (x + 3.5, y - 3.5)], 2.8, ENCRE, dessous=False)
    return ic


EMOTES = [emote_salut, emote_acclamation, emote_provocation, emote_assis, emote_repos, emote_pompes, emote_boire, emote_mort]


def main():
    SORTIE.mkdir(parents=True, exist_ok=True)
    apercu = None
    if "--png" in sys.argv:
        apercu = Path(sys.argv[sys.argv.index("--png") + 1])
        apercu.mkdir(parents=True, exist_ok=True)
    for f in EMOTES:
        ic = f()
        g.verifier(ic)
        (SORTIE / (ic.nom + ".svg")).write_text(ic.svg(), encoding="utf-8", newline="\n")
        if apercu is not None:
            for taille in (128, 48):
                rgba = raster.rasteriser(ic.formes, taille)
                (apercu / ("%s_%d.png" % (ic.nom, taille))).write_bytes(raster.png(taille, taille, rgba))
    print("%d icônes d'emotes dans %s" % (len(EMOTES), SORTIE))


if __name__ == "__main__":
    main()
