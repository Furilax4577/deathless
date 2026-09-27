"""Ambiances (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

Des tapis stéréo en boucle, au niveau bas (RMS moyen -30 dBFS) : la nuit du village (vent froid, brume, grillons,
un hibou), le donjon (souffle creux, gouttes, métal qui grince au loin, une plainte lointaine). Les deux canaux sont
décorrélés (bruits tirés séparément) : l'espace est large sans réverbération. Détail : § 3.19 du cahier.

Sons écrits (Assets/Audio/Deathless/Ambiances/, WAV 44,1 kHz **stéréo** 16 bits, 2D, graines 2601 à 2649) :
  ambiance_village_nuit_boucle   village la nuit (boucle 20 s)
  ambiance_donjon_boucle         donjon (boucle 20 s)
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


CANAUX = 2
MESURE = "rms"
DUREE, FONDU = 20.0, 1.0


def continu(rng, fabrique):
    """Deux canaux décorrélés d'un même fond, fondus à la jointure de la boucle."""
    return tuple(da.fondre_boucle(fabrique(random.Random(rng.random())), DUREE, FONDU) for _ in range(2))


def evenements():
    return da.tampon(DUREE + 3.0), da.tampon(DUREE + 3.0)


def village_nuit(graine):
    rng = random.Random(graine)
    vent = continu(rng, lambda r: da.souffle_module(r, DUREE + FONDU, 450.0, 0.7, 0.1, 0.6, 0.35))
    brume = continu(rng, lambda r: da.passe_bas(da.bruit_brun(r, da.idx(DUREE + FONDU)), 160.0))
    g, d = evenements()
    for _ in range(26):                                                                     # grillons
        t0 = rng.uniform(0, DUREE)
        f = rng.uniform(4200.0, 5000.0)
        ev = da.tampon(0.6)
        for k in range(rng.randint(6, 16)):
            da.mode(ev, f, rng.uniform(0.5, 1.0), 0.012, k / 32.0, 0.001)
        da.ajouter_stereo(g, d, ev, t0, rng.uniform(0.04, 0.1), rng.uniform(-0.9, 0.9))
    for t0, pan in ((6.5, -0.6), (14.2, 0.5)):                                              # un hibou, loin
        ev = da.tampon(1.2)
        for k, t in enumerate((0.0, 0.45)):
            da.sinus_glisse(ev, 400.0, 360.0, 0.5, 0.35, t, 0.03, 0.25, 1.0)
        da.ajouter_stereo(g, d, da.passe_bas(ev, 1200.0), t0, 0.12, pan)
    g, d = da.plier(g, DUREE), da.plier(d, DUREE)
    return ([0.35 * a + 0.5 * b + c for a, b, c in zip(vent[0], brume[0], g)],
            [0.35 * a + 0.5 * b + c for a, b, c in zip(vent[1], brume[1], d)])


def donjon(graine):
    rng = random.Random(graine)
    creux = continu(rng, lambda r: da.passe_bande(da.bruit_brun(r, da.idx(DUREE + FONDU)), 160.0, 0.8))
    air = continu(rng, lambda r: da.souffle_module(r, DUREE + FONDU, 700.0, 1.0, 0.15, 0.7, 0.3))
    g, d = evenements()
    for _ in range(12):                                                                     # gouttes
        t0 = rng.uniform(0, DUREE)
        ev = da.tampon(0.3)
        f = rng.choice(da.penta(5, 6))
        da.sinus_glisse(ev, f * 0.6, f, 0.4, 0.025, 0.0, 0.001, 0.02, 0.7)
        da.mode(ev, f * 1.8, 0.15, 0.08, 0.01, 0.001)
        da.ajouter_stereo(g, d, ev, t0, rng.uniform(0.08, 0.2), rng.uniform(-0.8, 0.8))
    for t0, pan in ((4.0, -0.7), (13.0, 0.6)):                                             # métal qui grince au loin
        m = da.passe_bas(da.metal_frotte(rng, 2.5, rng.uniform(140.0, 190.0), attaque=0.6, tremble=0.6), 1500.0)
        da.ajouter_stereo(g, d, env(m, 0.3, 1.2, 1.0), t0, 0.08, pan)
    v = da.voix(rng, 2.2, lambda u: 150.0 * (1 - 0.2 * u), lambda u: ("ou", "o", u), souffle=0.7)
    da.ajouter_stereo(g, d, da.passe_bas(env(v, 0.5, 0.8, 0.9), 700.0), 9.0, 0.06, 0.2)       # une plainte lointaine
    g, d = da.plier(g, DUREE), da.plier(d, DUREE)
    return ([0.5 * a + 0.3 * b + c for a, b, c in zip(creux[0], air[0], g)],
            [0.5 * a + 0.3 * b + c for a, b, c in zip(creux[1], air[1], d)])


# (nom du fichier, lot du plan de production, cible en dB (RMS moyen), fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("ambiance_village_nuit_boucle", E, -30.0, da.BOUCLE, lambda: village_nuit(2601)),
    ("ambiance_donjon_boucle", E, -30.0, da.BOUCLE, lambda: donjon(2602)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv, MESURE)


if __name__ == "__main__":
    main()
