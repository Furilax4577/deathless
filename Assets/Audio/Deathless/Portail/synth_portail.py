"""Portail et téléportation (Deathless, lot 2, regénéré sous la direction sombre le 26/09/2026 au soir).

Le portail est une plaie ouverte dans l'énergie de Nyxessa : un **bourdon grave permanent** tant qu'il est ouvert, qui
s'ouvre et se referme en **souffle et chœur**. Un corps qui part ou arrive est une **voix qui glisse** (vers le haut en
partant, vers le bas en arrivant), et les arrivées au sol (chute du ciel au donjon, sortie du sol au village) sont
faites de **peaux** et de **souffles**, de terre et de pierre. La gemme sombre ne sonne qu'à la fin, quand le corps est
entier. Détail : Docs/son-cahier-des-charges.md (§ 3.4, § 8.2).

Sons écrits (Assets/Audio/Deathless/Portail/, WAV 44,1 kHz mono 16 bits, graines 1301 à 1349) :
  portail_ouverture         souffle qui s'ouvre, chœur qui monte (« ou » → « a »), le bourdon s'installe
  portail_fermeture         le chœur retombe (« a » → « ou »), souffle aspiré, le bourdon s'éteint, peau grave
  portail_bourdon_boucle    portail ouvert (boucle 6 s) : bourdon grave qui bat, souffle qui tourne, rares gemmes
  portail_depart            un corps part : peau grave (la goutte), voix qui monte et s'éloigne, anneaux de souffle
  portail_arrivee           un corps arrive : voix qui descend et se pose, anneaux qui convergent, gemmes graves
  portail_chute_ciel        arrivée au donjon (Spawn_Air) : souffle qui tombe, grosse peau et pierre au contact
  portail_sortie_sol        retour au village (Spawn_Ground) : la terre gronde et s'ouvre, gravier, voix basse
  portail_ferme_refus       Interagir près du portail fermé la nuit : peau étouffée, souffle de voix, gemme morte (2D)

Usage : python -B synth_portail.py [dossier_sortie] [nom ...]   (par défaut : le dossier du script, tous les sons).
Catalogue : ids dl_portail_* dans Wiki/data/sons.json.
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import deathless_audio as da  # noqa: E402

N = da.note


def env(x, attaque, tenue, relache, forme=1.5):
    return da.enveloppe(x, attaque, tenue, relache, forme)


def ouverture(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.65)
    da.ajouter(buf, da.souffle(rng, 1.4, 300.0, 2500.0, 0.9, 0.3, 1.1, 1.3), 0.0, 0.45)
    c = da.choeur(rng, 1.2, lambda u: N("B2") * (1 + 0.5 * u), lambda u: ("ou", "a", u), nombre=4, octaves=(1.0, 2.0))
    da.ajouter(buf, env(c, 0.25, 0.5, 0.45), 0.05, 0.45)
    b = da.bourdon(rng, 1.1, [N("E1"), N("B1"), N("E2")], 0.33, 400.0)
    da.ajouter(buf, env(b, 0.5, 0.45, 0.15, 1.0), 0.55, 0.45)
    da.peau(rng, buf, 58.0, 0.5, 0.0, 0.4, 1.4, 0.5)
    return buf


def fermeture(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.45)
    c = da.choeur(rng, 1.0, lambda u: N("E3") * (1 - 0.4 * u), lambda u: ("a", "ou", u), nombre=4, octaves=(1.0, 0.5))
    da.ajouter(buf, env(c, 0.02, 0.35, 0.63), 0.0, 0.5)
    da.ajouter(buf, da.souffle(rng, 0.8, 2500.0, 250.0, 1.2, 0.08, 0.7, 1.3), 0.0, 0.45)
    b = da.bourdon(rng, 1.3, [N("E1"), N("B1")], 0.33, 400.0)
    da.ajouter(buf, env(b, 0.005, 0.3, 1.0, 1.4), 0.0, 0.4)
    da.peau(rng, buf, 50.0, 0.6, 0.75, 0.5, 1.3, 0.6)                                 # le portail se scelle
    da.gemme(rng, buf, N("E3"), 0.2, 0.6, 0.75, eclat=0.2, durete=0.0)
    return buf


def bourdon(graine):
    """Boucle de 6 s : bourdon mi1 + si1 + mi2 (jumeaux à 1/3 Hz, fréquences arrondies : jointure exacte), souffle qui
    tourne (0,5 Hz, trois cycles, fondu à la jointure), trois gemmes graves repliées."""
    rng = random.Random(graine)
    duree, fondu = 6.0, 0.4
    b = da.bourdon(rng, duree, [N("E1"), N("B1"), N("E2")], 1 / 3.0, 380.0, boucle=True)
    s = da.souffle_module(rng, duree + fondu, 500.0, 1.5, 0.5, 0.6, 0.3)
    s = da.fondre_boucle(s, duree, fondu)
    evenements = da.tampon(duree + 1.2)
    for t, n_ in ((0.7, "E4"), (2.9, "B3"), (4.6, "G4")):
        da.gemme(rng, evenements, N(n_), 0.06, 0.8, t, eclat=0.25, durete=0.1)
    return [0.6 * a + 0.3 * b_ + c for a, b_, c in zip(b, s, da.plier(evenements, duree))]


def depart(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.1)
    da.peau(rng, buf, 70.0, 0.6, 0.0, 0.25, 2.0, 0.6)                                   # la goutte, grave
    v = da.voix(rng, 0.9, lambda u: 147.0 * (3.0 ** (u ** 0.8)), lambda u: ("ou", "a", u), souffle=0.45)
    v = da.passe_bas_variable(env(v, 0.02, 0.3, 0.58), lambda u: 4000.0 * (1 - 0.8 * u))  # la voix monte et s'éloigne
    da.ajouter(buf, v, 0.03, 0.5)
    for t, (f0, f1) in ((0.05, (400.0, 1000.0)), (0.2, (600.0, 1600.0)), (0.35, (900.0, 2500.0))):
        da.ajouter(buf, da.souffle(rng, 0.25, f0, f1, 1.6, 0.03, 0.22, 1.6), t, 0.3)
    return buf


def arrivee(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.1)
    for t, (f0, f1) in ((0.0, (2500.0, 900.0)), (0.12, (1600.0, 600.0)), (0.24, (1000.0, 400.0))):
        da.ajouter(buf, da.souffle(rng, 0.25, f0, f1, 1.6, 0.02, 0.22, 1.6), t, 0.3)
    v = da.voix(rng, 0.8, lambda u: 440.0 * (1 / 2.25) ** (u ** 0.7), lambda u: ("a", "o", u), souffle=0.4)
    da.ajouter(buf, env(v, 0.005, 0.3, 0.495), 0.0, 0.5)                                # la voix descend et se pose
    da.gemme(rng, buf, N("E4"), 0.25, 0.5, 0.75, eclat=0.3)
    da.gemme(rng, buf, N("B4"), 0.18, 0.45, 0.75, eclat=0.3, durete=0.0)
    da.peau(rng, buf, 65.0, 0.35, 0.75, 0.3, 1.3, 0.3)
    return buf


def chute_ciel(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.3)
    da.ajouter(buf, da.souffle(rng, 1.0, 2500.0, 350.0, 1.1, 0.25, 0.75, 1.2), 0.0, 0.55)  # la chute
    contact = 1.0
    da.peau(rng, buf, 52.0, 0.9, contact, 0.5, 1.6, 1.2)                                  # réception : grosse peau
    da.pas_pierre(rng, buf, contact, 0.5)
    da.gravier(rng, buf, contact, 0.25, 22, 0.15, densite=lambda u: u ** 1.8)
    return buf


def sortie_sol(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.3)
    gronde = da.passe_bas(da.bruit_brun(rng, da.idx(0.6)), 200.0)
    da.ajouter(buf, env(gronde, 0.005, 0.15, 0.44, 1.5), 0.0, 0.8)                         # la terre gronde
    da.peau(rng, buf, 60.0, 0.6, 0.0, 0.5, 1.4, 0.6)
    da.peau(rng, buf, 75.0, 0.35, 0.35, 0.35, 1.3, 0.4)
    da.gravier(rng, buf, 0.05, 0.85, 70, 0.3, densite=lambda u: u ** 1.3, bande=lambda u: 800.0 + 1700.0 * u)
    v = da.voix(rng, 0.4, lambda u: 110.0 * (1 + 0.2 * u), "o", souffle=0.5)
    da.ajouter(buf, env(v, 0.03, 0.1, 0.27), 0.95, 0.3)                                    # le corps est entier
    da.gemme(rng, buf, N("E4"), 0.2, 0.4, 1.0, eclat=0.3)
    return buf


def ferme_refus(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.45)
    da.peau(rng, buf, 70.0, 0.6, 0.0, 0.15, 1.2, 0.8)
    v = da.voix(rng, 0.3, lambda u: 147.0 * (1 - 0.15 * u), "ou", souffle=0.7)
    da.ajouter(buf, env(v, 0.01, 0.05, 0.24), 0.0, 0.3)
    da.gemme(rng, buf, N("E4"), 0.2, 0.2, 0.0, eclat=0.1, durete=0.1)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("portail_ouverture", 2, -14.0, 0.15, lambda: ouverture(1301)),
    ("portail_fermeture", 2, -14.0, None, lambda: fermeture(1302)),
    ("portail_bourdon_boucle", 2, -20.0, da.BOUCLE, lambda: bourdon(1303)),
    ("portail_depart", 2, -14.0, None, lambda: depart(1304)),
    ("portail_arrivee", 2, -14.0, None, lambda: arrivee(1305)),
    ("portail_chute_ciel", 2, -14.0, None, lambda: chute_ciel(1306)),
    ("portail_sortie_sol", 2, -14.0, None, lambda: sortie_sol(1307)),
    ("portail_ferme_refus", 2, -19.0, 0.03, lambda: ferme_refus(1308)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
