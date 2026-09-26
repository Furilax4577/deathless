"""Bouclier de Nyxessa et villageois sorcier (Deathless, lot 2, regénéré sous la direction sombre le 26/09/2026 au soir).

Le bouclier est un mur de gemmes nourri par l'âme de Nyxessa : il se lève sur un **bourdon** et un **métal frotté**
(un archet sur une plaque), encaisse en **coups sourds d'os et de peau**, faiblit en plaintes qui glissent vers le
bas, et se brise en **chœur qui se déchire**. Le sorcier n'a pas de mots : son incantation est une **voix sourde
continue**, sa canalisation un **bourdon qui bat**. La gemme reste, grave et assourdie, toujours sous une voix ou un
bourdon. Détail : Docs/son-cahier-des-charges.md (§ 3.3, § 8.2).

Sons écrits (Assets/Audio/Deathless/Bouclier/, WAV 44,1 kHz mono 16 bits, 3D, graines 1351 à 1399) :
  bouclier_leve                    bourdon et métal frotté qui enflent, chœur sourd, gemmes graves qui montent
  bouclier_touche_1..4             coup encaissé : peau et os, métal bref, petite riposte d'os
  bouclier_etat_entame             passage à l'orange : plainte qui glisse d'un demi-ton, métal frotté
  bouclier_etat_critique           passage au rouge : même geste plus bas et plus rauque, craquements
  bouclier_brise                   le bouclier cède : craquement, chœur qui se déchire et tombe, bourdon qui s'effondre
  bouclier_breche_1..2             coup de brèche de Morgrim : fer sur le mur, cri bref du mur, peau grave
  bouclier_palier                  palier : métal frotté et chœur qui s'ouvrent sur le bourdon
  sorcier_incantation_boucle       incantation (boucle 3 s) : voix sourde continue, souffle, trois gemmes graves
  sorcier_canalisation_boucle      canalisation (boucle 4 s) : bourdon qui bat, gemmes qui voyagent le long du lien
  sorcier_canalisation_eclat_1..2  un coup avance la recharge d'un missile : gemme grave et souffle de voix

Usage : python -B synth_bouclier.py [dossier_sortie] [nom ...]   (par défaut : le dossier du script, tous les sons).
Catalogue : ids dl_bouclier_* et dl_sorcier_* dans Wiki/data/sons.json.
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import deathless_audio as da  # noqa: E402

N = da.note
MUR = da.penta(3, 4)            # mi3 à ré5 : les gemmes du mur, graves


def env(x, attaque, tenue, relache, forme=1.5):
    return da.enveloppe(x, attaque, tenue, relache, forme)


def leve(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.7)
    b = da.bourdon(rng, 1.7, [N("B1"), N("E2")], 0.3, 450.0)
    da.ajouter(buf, env(b, 0.35, 0.9, 0.45, 1.2), 0.0, 0.45)
    m = da.metal_frotte(rng, 1.6, 196.0, attaque=0.35)
    da.ajouter(buf, env(m, 0.01, 1.2, 0.39), 0.0, 0.35)
    c = da.choeur(rng, 1.4, N("E3"), lambda u: ("ou", "o", u), nombre=3, souffle=0.55)
    da.ajouter(buf, env(c, 0.4, 0.6, 0.4), 0.25, 0.3)
    da.peau(rng, buf, 55.0, 0.5, 0.0, 0.5, 1.4, 0.6)
    da.scintillement(rng, buf, 0.1, 1.0, MUR, 30, 0.08, (0.15, 0.4), densite=lambda u: u ** 0.8, registre=lambda u: u)
    return buf


def touche(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.5)
    da.peau(rng, buf, rng.uniform(70.0, 95.0), 0.7, 0.0, 0.18, 1.5, 1.0)
    da.os_creux(rng, buf, rng.uniform(420.0, 600.0), 0.35, 0.002, 0.05)
    da.plaque(rng, buf, rng.uniform(480.0, 620.0), 0.12, 0.15, 0.0, durete=0.3)
    da.gemme(rng, buf, MUR[rng.randrange(3, 7)], 0.1, 0.25, 0.004, eclat=0.3, durete=0.0)
    da.os_creux(rng, buf, rng.uniform(1100.0, 1500.0), 0.18, 0.07, 0.025)             # la riposte
    return buf


def etat(graine, f_voix, craque, rauque):
    rng = random.Random(graine)
    buf = da.tampon(0.85)
    demi = 2 ** (-1 / 12.0)
    v = da.voix(rng, 0.75, lambda u: f_voix * (1 + (demi - 1) * min(1.0, u * 1.5)), lambda u: ("o", "ou", u),
                souffle=0.45, rauque=rauque, vibrato=(7.0, 0.03))
    da.ajouter(buf, env(v, 0.01, 0.25, 0.49), 0.0, 0.5)
    m = da.metal_frotte(rng, 0.8, f_voix * 0.75, attaque=0.1, tremble=0.6)
    da.ajouter(buf, env(m, 0.01, 0.3, 0.49), 0.0, 0.3)
    da.peau(rng, buf, 70.0, 0.3, 0.0, 0.25, 1.3, 0.4)
    if craque:
        for k in range(5):
            da.choc(rng, buf, 0.05 + 0.06 * k + rng.uniform(0, 0.03), rng.uniform(0.15, 0.3), 0.0007,
                    rng.uniform(2500.0, 5000.0), 1.2)
    return buf


def brise(graine):
    rng = random.Random(graine)
    buf = da.tampon(2.3)
    craque = da.passe_haut(da.bruit(rng, da.idx(0.1)), 600.0)
    da.ajouter(buf, env(craque, 0.0006, 0.01, 0.09, 2.5), 0.0, 0.7)
    da.peau(rng, buf, 50.0, 0.8, 0.0, 0.8, 1.5, 1.0)
    c = da.choeur(rng, 1.6, lambda u: 220.0 * (1 - 0.5 * u ** 1.2), lambda u: ("a", "o", u), nombre=4,
                  souffle=0.4, octaves=(1.0, 0.5))
    da.ajouter(buf, env(da.saturer(c, 3.0), 0.004, 0.3, 1.3, 1.2), 0.0, 0.55)            # le chœur se déchire
    b = da.bourdon(rng, 2.0, [N("E1"), N("B1")], 0.4, 350.0)
    da.ajouter(buf, env(b, 0.005, 0.2, 1.8, 1.4), 0.0, 0.4)
    eclats = sorted(rng.uniform(250.0, 1200.0) for _ in range(24))
    da.scintillement(rng, buf, 0.0, 1.0, eclats, 35, 0.12, (0.1, 0.35), densite=lambda u: u ** 2.0)
    return buf


def breche(graine, f_fer):
    rng = random.Random(graine)
    buf = da.tampon(0.9)
    da.plaque(rng, buf, f_fer, 0.45, 0.4, 0.0, durete=1.4)                            # le fer de la martache
    da.peau(rng, buf, 58.0, 0.7, 0.0, 0.35, 1.5, 1.0)
    cr = da.voix(rng, 0.35, lambda u: 392.0 * (1 - 0.3 * u), "a", souffle=0.3, rauque=0.6, sous_harm=0.5)
    da.ajouter(buf, env(da.saturer(cr, 3.0), 0.004, 0.08, 0.26), 0.01, 0.35)             # le mur crie
    for k in range(4):
        da.choc(rng, buf, 0.03 + 0.05 * k, 0.2, 0.0007, rng.uniform(2500.0, 5000.0), 1.2)
    return buf


def palier(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.3)
    m = da.metal_frotte(rng, 1.25, 165.0, attaque=0.25)
    da.ajouter(buf, env(m, 0.01, 0.8, 0.44), 0.0, 0.35)
    c = da.choeur(rng, 1.2, N("B2"), lambda u: ("ou", "a", u), nombre=3, octaves=(1.0, 2.0))
    da.ajouter(buf, env(c, 0.2, 0.5, 0.5), 0.05, 0.45)
    b = da.bourdon(rng, 1.3, [N("E1"), N("B1")], 0.3, 400.0)
    da.ajouter(buf, env(b, 0.2, 0.6, 0.5), 0.0, 0.3)
    da.peau(rng, buf, 62.0, 0.45, 0.0, 0.4, 1.4, 0.5)
    da.scintillement(rng, buf, 0.0, 0.7, MUR, 16, 0.08, (0.15, 0.35), densite=lambda u: u ** 0.5)
    return buf


def incantation(graine):
    """Boucle de 3 s : voix sourde continue (110 Hz, voyelle qui oscille « ou » ↔ « o » deux fois, souffle 0,6),
    souffle qui respire (2 cycles), trois gemmes graves repliées sur la boucle."""
    rng = random.Random(graine)
    duree, fondu = 3.0, 0.3
    tot = duree + fondu
    v = da.voix(rng, tot, lambda u: 110.0 * (1 + 0.02 * math.sin(2 * math.pi * u * tot / 1.5)),
                lambda u: ("ou", "o", 0.5 + 0.5 * math.sin(2 * math.pi * u * tot / 1.5)), souffle=0.6, vibrato=(4.0, 0.01))
    v2 = da.voix(rng, tot, 165.0, lambda u: ("o", "ou", 0.5 + 0.5 * math.sin(2 * math.pi * u * tot / 1.5 + 1.0)),
                 souffle=0.7, vibrato=(3.5, 0.012))
    s = da.souffle_module(rng, tot, 700.0, 0.9, 1 / 1.5, 0.7, 0.2)
    continu = da.fondre_boucle([0.6 * a + 0.3 * b + 0.2 * c for a, b, c in zip(v, v2, s)], duree, fondu)
    evenements = da.tampon(duree + 1.5)
    for t, n_ in ((0.15, "E4"), (1.15, "B4"), (2.15, "E5")):
        da.gemme(rng, evenements, N(n_), 0.12, 1.0, t, eclat=0.3, durete=0.2)
    return [a + b for a, b in zip(continu, da.plier(evenements, duree))]


def canalisation(graine):
    """Boucle de 4 s : bourdon si1 + mi2 avec battements (jumeau à 0,5 Hz, 2 battements par boucle), fréquences
    arrondies à un nombre entier de périodes (jointure exacte), huit gemmes graves qui voyagent (une toutes les 0,5 s)."""
    rng = random.Random(graine)
    duree = 4.0
    b = da.bourdon(rng, duree, [N("B1"), N("E2"), N("B2")], 0.5, 500.0, boucle=True)
    evenements = da.tampon(duree + 0.6)
    for k in range(8):
        da.gemme(rng, evenements, N("B4") if k % 2 == 0 else N("E5"), 0.08, 0.4, 0.05 + 0.5 * k,
                 eclat=0.3, jumeau=False, durete=0.3)
    return [0.5 * a + c for a, c in zip(b, da.plier(evenements, duree))]


def canalisation_eclat(graine, n_):
    rng = random.Random(graine)
    buf = da.tampon(0.4)
    da.gemme(rng, buf, N(n_), 0.35, 0.3, 0.0, eclat=0.4)
    v = da.voix(rng, 0.3, lambda u: 220.0 * (1 + 0.3 * u), "o", souffle=0.6)
    da.ajouter(buf, env(v, 0.01, 0.08, 0.21), 0.0, 0.3)
    da.ajouter(buf, da.souffle(rng, 0.25, 600.0, 2000.0, 1.3, 0.03, 0.2, 1.5), 0.0, 0.12)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("bouclier_leve", 2, -14.0, None, lambda: leve(1351)),
    ("bouclier_touche_1", 2, -15.0, None, lambda: touche(1352)),
    ("bouclier_touche_2", 2, -15.0, None, lambda: touche(1353)),
    ("bouclier_touche_3", 2, -15.0, None, lambda: touche(1354)),
    ("bouclier_touche_4", 2, -15.0, None, lambda: touche(1355)),
    ("bouclier_etat_entame", 2, -16.0, None, lambda: etat(1356, 247.0, False, 0.2)),
    ("bouclier_etat_critique", 2, -15.0, None, lambda: etat(1357, 196.0, True, 0.55)),
    ("bouclier_brise", 2, -12.5, None, lambda: brise(1358)),
    ("bouclier_breche_1", 2, -13.0, None, lambda: breche(1359, 820.0)),
    ("bouclier_breche_2", 2, -13.0, None, lambda: breche(1360, 960.0)),
    ("bouclier_palier", 2, -14.0, None, lambda: palier(1361)),
    ("sorcier_incantation_boucle", 2, -18.0, da.BOUCLE, lambda: incantation(1362)),
    ("sorcier_canalisation_boucle", 2, -20.0, da.BOUCLE, lambda: canalisation(1363)),
    ("sorcier_canalisation_eclat_1", 2, -17.0, None, lambda: canalisation_eclat(1364, "B4")),
    ("sorcier_canalisation_eclat_2", 2, -17.0, None, lambda: canalisation_eclat(1365, "E5")),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
