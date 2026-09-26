"""Paladin, épée et bouclier (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

Acier court et sec, bouclier de bois cerclé qui sonne comme une peau, impacts sur l'os creux des squelettes. La
parade parfaite ajoute un éclat d'or (thème Sacré) et un coup de bouclier grave ; le soin est un chœur chaud en sol
majeur, doux, jamais de gemme (le soin n'est pas Nyxessa). Détail : § 3.7 du cahier.

Sons écrits (Assets/Audio/Deathless/Paladin/, WAV 44,1 kHz mono 16 bits, 3D, graines 1501 à 1549) :
  epee_elan_1        souffle de lame court, acier qui chante à peine
  epee_impact_1      l'épée dans l'os : os creux, tranchant, peau
  blocage_1          coup bloqué : bois cerclé (peau et modes de bois), cerclage de fer
  parade_parfaite    parade, éclat d'or en quinte, bond et coup de bouclier grave
  soin               soin sur soi : chœur chaud qui monte (sol majeur), éclat d'or doux
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


def elan(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.3)
    da.ajouter(buf, da.souffle(rng, 0.14, 600.0, 3000.0, 1.3, 0.02, 0.12, 1.6), 0.0, 0.7)
    da.plaque(rng, buf, 1450.0, 0.06, 0.1, 0.02, durete=0.0)
    return buf


def impact(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.3)
    da.os_creux(rng, buf, rng.uniform(500.0, 700.0), 0.6, 0.0, 0.06)
    da.os_creux(rng, buf, rng.uniform(900.0, 1200.0), 0.3, 0.004, 0.04)
    tranchant = da.passe_bande(da.bruit(rng, da.idx(0.015)), 4000.0, 1.0)
    da.ajouter(buf, env(tranchant, 0.0005, 0.002, 0.012), 0.0, 0.5)
    da.peau(rng, buf, 110.0, 0.4, 0.0, 0.1, 1.3, 0.3)
    return buf


def blocage(graine, buf=None, t0=0.0):
    rng = random.Random(graine)
    buf = da.tampon(0.4) if buf is None else buf
    da.peau(rng, buf, 150.0, 0.7, t0, 0.12, 1.2, 1.0)
    da.mode(buf, 180.0, 0.4, 0.12, t0, 0.0006)
    da.mode(buf, 420.0, 0.25, 0.08, t0, 0.0006)
    da.plaque(rng, buf, 900.0, 0.18, 0.12, t0, durete=0.5)
    return buf


def parade_parfaite(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.9)
    blocage(graine, buf)
    da.plaque(rng, buf, 1318.0, 0.3, 0.5, 0.01, durete=0.8)
    da.plaque(rng, buf, 1975.0, 0.2, 0.45, 0.01, durete=0.0)
    da.ajouter(buf, da.souffle(rng, 0.2, 500.0, 1500.0, 1.2, 0.03, 0.17, 1.5), 0.15, 0.35)
    da.peau(rng, buf, 70.0, 0.8, 0.32, 0.3, 1.5, 1.0)                                    # coup de bouclier
    return buf


def soin(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.5)
    for f in (N("G3"), N("B3"), N("D4")):
        c = da.choeur(rng, 1.3, lambda u, f=f: f * (1 + 0.02 * u), lambda u: ("o", "a", u), nombre=2, souffle=0.45)
        da.ajouter(buf, env(c, 0.25, 0.5, 0.55), 0.0, 0.3)
    da.plaque(rng, buf, 1568.0, 0.12, 0.5, 0.3, durete=0.2)
    da.ajouter(buf, da.souffle(rng, 1.2, 600.0, 2400.0, 1.2, 0.5, 0.7, 1.4), 0.0, 0.15)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("epee_elan_1", E, -15.0, None, lambda: elan(1501)),
    ("epee_impact_1", E, -14.0, None, lambda: impact(1502)),
    ("blocage_1", E, -14.0, None, lambda: blocage(1503)),
    ("parade_parfaite", E, -12.5, None, lambda: parade_parfaite(1504)),
    ("soin", E, -15.0, None, lambda: soin(1505)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
