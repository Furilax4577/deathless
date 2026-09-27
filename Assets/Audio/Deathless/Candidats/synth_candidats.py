"""Candidats du mois (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

L'humour est réservé aux classes comiques, et il vient du **timbre** et du **rythme** : le pet est un train
d'impulsions à hauteur instable passé dans deux formants mous ; le scratch, un bruit et une note dont la hauteur suit
la main ; le « bwoiing » du luth, une corde grave dont la tension lâche. Détail : § 3.12 du cahier.

Sons écrits (Assets/Audio/Deathless/Candidats/, WAV 44,1 kHz mono 16 bits, 3D, graines 2701 à 2749) :
  clochard_pet_defense_1   pet de défense : impulsions à 60-120 Hz, formants 250 et 600 Hz, « pfft » final
  djbob_scratch_1          scratch aller-retour : la hauteur suit la main
  barde_bwoiing            3e coup du luth : corde grave dont la hauteur plonge
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


def pet(graine):
    rng = random.Random(graine)
    duree = 0.7
    n = da.idx(duree)
    x = [0.0] * n
    t, f = 0.0, rng.uniform(70.0, 90.0)
    while t < duree * 0.8:
        u = t / duree
        x[da.idx(t)] = 1.0 - 0.6 * u
        f = max(55.0, min(125.0, f * rng.uniform(0.88, 1.14) + (1 - u) * 1.5))
        t += 1.0 / f
    y = [0.0] * n
    for fc, q, g in ((250.0, 3.0, 1.0), (600.0, 4.0, 0.5)):
        z = da.passe_bande(x, fc, q)
        y = [a + g * b for a, b in zip(y, z)]
    y = env(y, 0.005, 0.4, 0.29, 1.3)
    pfft = da.passe_bande(da.bruit(rng, da.idx(0.15)), 1200.0, 1.0)
    da.ajouter(y, env(pfft, 0.01, 0.03, 0.11), 0.52, 0.25)
    return y


def scratch(graine):
    rng = random.Random(graine)
    duree = 0.6
    n = da.idx(duree)
    buf = [0.0] * n
    ph = 0.0
    br = da.bruit(rng, n)
    vitesse = [math.sin(2 * math.pi * 3.3 * i / da.RATE) for i in range(n)]                  # aller-retour de la main
    for i in range(n):
        v = vitesse[i]
        ph += 2 * math.pi * 220.0 * (1 + 1.5 * abs(v)) / da.RATE
        buf[i] = abs(v) * (0.5 * math.sin(ph) + 0.3 * br[i])
    return da.passe_bande(buf, lambda u: 700.0 + 1600.0 * abs(math.sin(2 * math.pi * 3.3 * u * duree)), 1.2)


def bwoiing(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.8)
    for k, a in ((1, 0.5), (2, 0.3), (3, 0.18), (4, 0.1)):
        da.sinus_glisse(buf, 123.0 * k, 70.0 * k, a, 0.75, 0.0, 0.002, 0.7, 0.5)
    da.mode(buf, 250.0, 0.4, 0.08, 0.0, 0.0006)                                              # la caisse du luth
    da.choc(rng, buf, 0.0, 0.3, 0.002, 1500.0, 0.8)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("clochard_pet_defense_1", E, -14.0, None, lambda: pet(2701)),
    ("djbob_scratch_1", E, -14.0, None, lambda: scratch(2702)),
    ("barde_bwoiing", E, -14.0, None, lambda: bwoiing(2703)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
