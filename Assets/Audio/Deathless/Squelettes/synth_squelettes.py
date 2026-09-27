"""Squelettes (Deathless, lot 3 du cahier des charges son, direction sombre, 27/09/2026 ; les cinq premiers fichiers
sont les échantillons du 26/09/2026 au soir, inchangés).

Os et poussière : **os creux** (petits tubes frappés, modes impairs, secs), cliquetis, crécelle d'os, terre qui s'ouvre
(peau grave, bruit brun), gravier et sable. Jamais de voix humaine : les seules voix sont celles des âmes captives du
missile crâne du mage (celui de Nyxessa, 5 demi-tons plus aigu : un crâne plus petit). À la mort, une seule gemme
lointaine et très douce : la magie de Nyxessa qui quitte le squelette. Détail : § 3.13 du cahier.

Sons écrits (Assets/Audio/Deathless/Squelettes/, WAV 44,1 kHz mono 16 bits, 3D, graines 2001 à 2099) :
  squelette_sortie_1..3             la terre s'ouvre, les mottes retombent, les os s'assemblent
  squelette_pas_1..4                pas d'os léger sur la terre (niveau très bas : ils sont jusqu'à 60)
  squelette_preparation_1..3        crécelle d'os qui s'accélère jusqu'à l'impact (0,7 s), souffle qui monte
  squelette_coup_1..3               sbire, voleur : souffle de lame rouillée, os du bras, sans chant de métal
  guerrier_coup_1..3                guerrier : souffle grave, masse, os qui craquent à l'effort
  squelette_touche_1..4             os creux frappé, éclats d'os
  squelette_mort_1..3               les os s'effondrent en grappe, poussière, soupir de sable, gemme lointaine
  squelette_aube_1..2               à l'aube : la poussière monte et se disperse, pluie d'os légers
  squelette_etourdi_1..2            étourdi : la mâchoire claque deux fois, les os vacillent
  squelette_repousse_1..3           repoussé : glissement sur la terre, cliquetis
  mage_squelette_incantation_1..2   le mage prépare son crâne : roulement d'os lent qui s'accélère, souffle qui se charge
  mage_squelette_tir_1..2           le mage tire : souffle qui part, mâchoire qui claque
  mage_squelette_missile_vol_boucle vol du crâne du mage (boucle 2 s) : plaintes des limbes, 5 demi-tons plus aiguës
  mage_squelette_missile_eclat_1..2 éclat du crâne du mage : cri bref plus aigu que celui de Nyxessa, gemmes sombres
  voleur_elan_1..2                  le voleur se rue : pas d'os rapides, deux lames courtes
  elite_aura_boucle                 aura d'un élite (boucle 3 s, de près) : grondement pulsé, cliquetis lourd
  squelette_danse_boucle            squelettes qui dansent (Drop de DJ Bob ; boucle 2 s à 120 BPM) : cliquetis en rythme

Usage : python -B synth_squelettes.py [dossier_sortie] [nom ...]   (par défaut : le dossier du script, tous les sons).
Catalogue : ids dl_squelette_*, dl_guerrier_*, dl_mage_squelette_*, dl_voleur_*, dl_elite_* dans Wiki/data/sons.json.
"""

import math
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import deathless_audio as da  # noqa: E402

N = da.note
E = "echantillons"   # lot : échantillons de la direction sombre (26/09/2026), pour juger le bain avant la production


def env(x, attaque, tenue, relache, forme=1.5):
    return da.enveloppe(x, attaque, tenue, relache, forme)


def sortie(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.2)
    da.peau(rng, buf, 62.0, 0.8, 0.0, 0.3, 1.4, 0.8)
    terre = da.passe_bas(da.bruit_brun(rng, da.idx(0.5)), 300.0)
    da.ajouter(buf, env(terre, 0.005, 0.1, 0.39), 0.0, 0.6)
    da.gravier(rng, buf, 0.05, 0.7, 45, 0.35, densite=lambda u: u ** 1.6)                   # les mottes retombent
    da.cliquetis(rng, buf, 0.45, 0.5, 10, 0.3)                                              # les os s'assemblent
    return buf


def preparation(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.7)
    t, ecart = 0.0, 0.08
    while t < 0.66:
        da.os_creux(rng, buf, rng.uniform(900.0, 1600.0), 0.4 * (0.5 + t), t, 0.02)
        t += ecart
        ecart = max(0.018, ecart * 0.86)
    da.ajouter(buf, da.souffle(rng, 0.7, 300.0, 1200.0, 1.2, 0.6, 0.1, 1.2), 0.0, 0.3)
    return buf


def touche(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.25)
    da.os_creux(rng, buf, rng.uniform(450.0, 650.0), 0.7, 0.0, 0.05)
    da.os_creux(rng, buf, rng.uniform(800.0, 950.0), 0.4, 0.003, 0.04)
    da.cliquetis(rng, buf, 0.01, 0.08, 4, 0.2)
    return buf


def mort(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.0)
    for _ in range(16):
        u = rng.random() ** 1.8
        da.os_creux(rng, buf, rng.uniform(380.0, 1200.0), rng.uniform(0.2, 0.5), u * 0.5, rng.uniform(0.03, 0.06))
    poussiere = da.passe_bas(da.bruit_brun(rng, da.idx(0.9)), 500.0)
    da.ajouter(buf, env(poussiere, 0.01, 0.1, 0.79, 1.8), 0.02, 0.4)
    da.ajouter(buf, da.souffle(rng, 0.6, 2000.0, 600.0, 1.2, 0.1, 0.5, 1.5), 0.2, 0.2)       # soupir de sable
    da.gemme(rng, buf, N("E5"), 0.05, 0.4, 0.45, eclat=0.3, durete=0.0)                      # la magie s'en va
    return buf


def aube(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.5)
    da.ajouter(buf, da.souffle(rng, 1.3, 400.0, 3000.0, 1.0, 0.2, 1.1, 1.3), 0.0, 0.6)
    for _ in range(10):
        da.os_creux(rng, buf, rng.uniform(700.0, 1600.0), rng.uniform(0.1, 0.3), rng.uniform(0.0, 0.9), 0.03)
    da.gravier(rng, buf, 0.1, 1.0, 30, 0.12)
    return buf


# --- Lot 3 (27/09/2026) ----------------------------------------------------------------------------------------------
import importlib.util  # noqa: E402

_chemin_nyx = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Nyxessa", "synth_nyxessa.py")
_spec = importlib.util.spec_from_file_location("synth_nyxessa", _chemin_nyx)
nyx = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(nyx)
QUARTE = 2 ** (5 / 12.0)        # le crâne du mage est 1,5 fois plus petit : 5 demi-tons plus aigu


def pas(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.12)
    da.os_creux(rng, buf, rng.uniform(700.0, 1100.0), 0.5, 0.0, 0.025)
    da.peau(rng, buf, rng.uniform(110.0, 140.0), 0.3, 0.0, 0.04, 1.2, 0.4)
    da.gravier(rng, buf, 0.0, 0.05, 3, 0.15)
    return buf


def coup(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.3)
    da.ajouter(buf, da.souffle(rng, 0.15, 500.0, 2200.0, 1.1, 0.02, 0.13, 1.6), 0.0, 0.8)
    da.cliquetis(rng, buf, 0.0, 0.06, 2, 0.2)
    return buf


def guerrier_coup(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.4)
    da.ajouter(buf, da.souffle(rng, 0.22, 300.0, 1400.0, 0.9, 0.03, 0.19, 1.5), 0.0, 0.8)
    da.sub(buf, 65.0, 45.0, 0.3, 0.2, 0.02)
    da.os_creux(rng, buf, rng.uniform(380.0, 460.0), 0.3, 0.0, 0.05)
    da.os_creux(rng, buf, rng.uniform(500.0, 600.0), 0.2, 0.05, 0.04)
    return buf


def etourdi(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.6)
    da.os_creux(rng, buf, rng.uniform(780.0, 840.0), 0.6, 0.0, 0.03)
    da.os_creux(rng, buf, rng.uniform(900.0, 980.0), 0.5, 0.12, 0.03)
    for k in range(6):
        da.os_creux(rng, buf, rng.uniform(1200.0, 2000.0), 0.15, 0.2 + 0.06 * k + rng.uniform(0, 0.02), 0.02)
    return buf


def repousse(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.4)
    frotte = da.passe_bande(da.bruit_brun(rng, da.idx(0.3)), 350.0, 1.0)
    da.ajouter(buf, env(frotte, 0.01, 0.1, 0.19), 0.0, 0.8)
    da.cliquetis(rng, buf, 0.0, 0.2, 4, 0.25)
    return buf


def incantation(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.8)
    t, ecart = 0.0, 0.1
    while t < 0.72:
        da.os_creux(rng, buf, rng.uniform(600.0, 900.0), 0.25 + 0.4 * t, t, 0.03)
        t += ecart
        ecart = max(0.035, ecart * 0.9)
    da.ajouter(buf, da.souffle(rng, 0.75, 400.0, 1500.0, 1.2, 0.6, 0.15, 1.3), 0.0, 0.4)
    return buf


def tir(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.5)
    da.ajouter(buf, da.souffle(rng, 0.35, 600.0, 2500.0, 1.2, 0.01, 0.33, 1.6), 0.0, 0.6)
    da.os_creux(rng, buf, rng.uniform(850.0, 950.0), 0.5, 0.0, 0.03)
    da.os_creux(rng, buf, rng.uniform(950.0, 1050.0), 0.4, 0.07, 0.03)
    return buf


def voleur_elan(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.5)
    for k in range(5):
        da.os_creux(rng, buf, rng.uniform(800.0, 1200.0), 0.3, 0.06 * k, 0.02)
    for t in (0.25, 0.33):
        da.ajouter(buf, da.souffle(rng, 0.09, 2000.0, 5000.0, 1.4, 0.01, 0.08, 1.6), t, 0.5)
    return buf


def elite_aura(graine):
    """Boucle de 3 s : grondement de 70 Hz (arrondi à un nombre entier de périodes) qui pulse à 4/3 Hz (4 pulsations
    par boucle), six gros os qui s'entrechoquent, repliés sur la boucle."""
    rng = random.Random(graine)
    duree = 3.0
    n = da.idx(duree)
    f = round(70.0 * duree) / duree
    res = [0.0] * n
    for k, a in ((1, 1.0), (2, 0.4), (3, 0.2)):
        w = 2 * math.pi * f * k / da.RATE
        for i in range(n):
            res[i] += a * math.sin(w * i)
    res = [v * (0.55 + 0.45 * math.sin(2 * math.pi * (4 / 3.0) * i / da.RATE) ** 2) * 0.35 for i, v in enumerate(res)]
    ev = da.tampon(duree + 0.3)
    for k in range(6):
        da.os_creux(rng, ev, rng.uniform(350.0, 600.0), 0.3, k * 0.5 + rng.uniform(0, 0.1), 0.05)
    return [a + b for a, b in zip(res, da.plier(ev, duree))]


def danse(graine):
    """Boucle de 2 s à 120 BPM (quatre temps) : un cliquetis d'os par croche, accentué sur les temps."""
    rng = random.Random(graine)
    duree = 2.0
    ev = da.tampon(duree + 0.2)
    for c in range(8):
        fort = c % 2 == 0
        da.os_creux(rng, ev, rng.uniform(600.0, 900.0) if fort else rng.uniform(1300.0, 1900.0),
                    0.6 if fort else 0.3, c * 0.25, 0.04 if fort else 0.02)
        if fort:
            da.cliquetis(rng, ev, c * 0.25 + 0.01, 0.05, 3, 0.15)
    return da.plier(ev, duree)


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("squelette_sortie_1", 3, -14.0, None, lambda: sortie(2001)),
    ("squelette_sortie_2", 3, -14.0, None, lambda: sortie(2011)),
    ("squelette_sortie_3", 3, -14.0, None, lambda: sortie(2012)),
    ("squelette_pas_1", 3, -28.0, 0.01, lambda: pas(2013)),
    ("squelette_pas_2", 3, -28.0, 0.01, lambda: pas(2014)),
    ("squelette_pas_3", 3, -28.0, 0.01, lambda: pas(2015)),
    ("squelette_pas_4", 3, -28.0, 0.01, lambda: pas(2016)),
    ("squelette_preparation_1", 3, -16.0, None, lambda: preparation(2002)),
    ("squelette_preparation_2", 3, -16.0, None, lambda: preparation(2017)),
    ("squelette_preparation_3", 3, -16.0, None, lambda: preparation(2018)),
    ("squelette_coup_1", 3, -16.0, None, lambda: coup(2019)),
    ("squelette_coup_2", 3, -16.0, None, lambda: coup(2020)),
    ("squelette_coup_3", 3, -16.0, None, lambda: coup(2021)),
    ("guerrier_coup_1", 3, -15.0, None, lambda: guerrier_coup(2022)),
    ("guerrier_coup_2", 3, -15.0, None, lambda: guerrier_coup(2023)),
    ("guerrier_coup_3", 3, -15.0, None, lambda: guerrier_coup(2024)),
    ("squelette_touche_1", 3, -16.0, None, lambda: touche(2003)),
    ("squelette_touche_2", 3, -16.0, None, lambda: touche(2025)),
    ("squelette_touche_3", 3, -16.0, None, lambda: touche(2026)),
    ("squelette_touche_4", 3, -16.0, None, lambda: touche(2027)),
    ("squelette_mort_1", 3, -15.0, None, lambda: mort(2004)),
    ("squelette_mort_2", 3, -15.0, None, lambda: mort(2028)),
    ("squelette_mort_3", 3, -15.0, None, lambda: mort(2029)),
    ("squelette_aube_1", 3, -15.0, None, lambda: aube(2005)),
    ("squelette_aube_2", 3, -15.0, None, lambda: aube(2030)),
    ("squelette_etourdi_1", 3, -17.0, None, lambda: etourdi(2031)),
    ("squelette_etourdi_2", 3, -17.0, None, lambda: etourdi(2032)),
    ("squelette_repousse_1", 3, -17.0, None, lambda: repousse(2033)),
    ("squelette_repousse_2", 3, -17.0, None, lambda: repousse(2034)),
    ("squelette_repousse_3", 3, -17.0, None, lambda: repousse(2035)),
    ("mage_squelette_incantation_1", 3, -17.0, None, lambda: incantation(2036)),
    ("mage_squelette_incantation_2", 3, -17.0, None, lambda: incantation(2037)),
    ("mage_squelette_tir_1", 3, -15.0, None, lambda: tir(2038)),
    ("mage_squelette_tir_2", 3, -15.0, None, lambda: tir(2039)),
    ("mage_squelette_missile_vol_boucle", 3, -18.0, da.BOUCLE,
     lambda: nyx.missile_vol(2040, tuple(f * QUARTE for f in (82.4, 98.0, 123.5)))),
    ("mage_squelette_missile_eclat_1", 3, -15.0, None, lambda: nyx.missile_eclat(2041, 330.0)),
    ("mage_squelette_missile_eclat_2", 3, -15.0, None, lambda: nyx.missile_eclat(2042, 370.0)),
    ("voleur_elan_1", 3, -16.0, None, lambda: voleur_elan(2043)),
    ("voleur_elan_2", 3, -16.0, None, lambda: voleur_elan(2044)),
    ("elite_aura_boucle", 3, -22.0, da.BOUCLE, lambda: elite_aura(2045)),
    ("squelette_danse_boucle", 3, -20.0, da.BOUCLE, lambda: danse(2046)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
