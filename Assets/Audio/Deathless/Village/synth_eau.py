"""Eau du village (carte v5, 01/10/2026 ; refaite le 02/10/2026) : la cascade qui tombe dans le bassin, au pied de la montagne du nord.

Son écrit (Assets/Audio/Deathless/Village/, WAV 44,1 kHz mono 16 bits, 3D, graine 2702) :
  cascade_boucle   chute d'eau continue, boucle de 16 s SANS COUPURE : grondement sourd de la masse d'eau (bruit brun),
                   rideau d'eau (deux bandes de bruit, 900 Hz et 2,2 kHz), brillance des embruns (3 à 9 kHz) et éclaboussures
                   (petits chocs aigus et gloussements graves semés au hasard).

Retour de Quentin du 02/10/2026 (« créer le son de la cascade ») : la première version (12 s, bruit modulé à trois cycles par
boucle, niveau perçu -20 dB, jonction par fondu enchaîné) respirait comme une vague et restait très discrète une fois la
distance appliquée. Cette version : une cascade est un son STABLE, donc la modulation d'amplitude est faible et incommensurable
(5, 13 et 29 cycles sur 16 s : aucun battement audible) ; tout est périodique de 16 s EXACTEMENT : les filtres tournent
en boucle (da.circulaire : on filtre deux tours et on garde le second), les modulations font un nombre entier de cycles, les
éclaboussures sont repliées (da.plier) : la jonction n'a ni fondu, ni saut, ni trou ; niveau perçu -15,5 dB.

Catalogue : id « village_cascade » (Wiki/data/sons.json), joué en boucle par CascadeVillage au pied de la chute (portée 50 m).
    python Assets/Audio/Deathless/Village/synth_eau.py
"""

import math
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import deathless_audio as da  # noqa: E402

E = "village_v5"
DUREE = 16.0


def lfo(n, cycles, phase):
    """Sinus de `cycles` cycles entiers sur la boucle (périodique par construction)."""
    return [math.sin(2 * math.pi * cycles * i / n + phase) for i in range(n)]


def cascade(graine):
    rng = random.Random(graine)
    n = da.idx(DUREE)

    def bruit_brun_circ(x):
        y, res = 0.0, [0.0] * len(x)
        for i, v in enumerate(x):
            y = 0.985 * y + 0.15 * v
            res[i] = y
        return res

    def normaliser(x):
        rms = math.sqrt(sum(v * v for v in x) / len(x)) or 1.0
        return [v / rms for v in x]

    # masse d'eau : grondement grave
    grave = normaliser(da.circulaire(lambda x: da.passe_bas(x, 320.0), da.circulaire(bruit_brun_circ, da.bruit(rng, n))))
    # rideau d'eau : deux bandes, deux ondulations lentes et décalées
    r1 = normaliser(da.circulaire(lambda x: da.passe_bande(x, 900.0, 0.55), da.bruit(rng, n)))
    r2 = normaliser(da.circulaire(lambda x: da.passe_bande(x, 2200.0, 0.7), da.bruit(rng, n)))
    m1 = [1 + 0.10 * a + 0.06 * b for a, b in zip(lfo(n, 5, 0.4), lfo(n, 13, 1.9))]
    m2 = [1 + 0.08 * a + 0.05 * b for a, b in zip(lfo(n, 7, 2.2), lfo(n, 29, 0.7))]
    # brillance des embruns
    aigu = normaliser(da.circulaire(lambda x: da.passe_haut(da.passe_bas(x, 9000.0), 3500.0), da.bruit(rng, n)))
    m3 = [1 + 0.12 * a for a in lfo(n, 19, 1.1)]
    buf = [0.50 * g + 0.62 * a * x1 + 0.38 * b * x2 + 0.16 * c * h
           for g, x1, x2, h, a, b, c in zip(grave, r1, r2, aigu, m1, m2, m3)]
    # éclaboussures : chocs courts, repliés à la jonction ; gloussements graves (gouttes qui tombent dans le bassin)
    ev = da.tampon(DUREE + 0.6)
    for _ in range(420):
        t = rng.uniform(0.0, DUREE)
        da.choc(rng, ev, t, rng.uniform(0.04, 0.15), duree=rng.uniform(0.002, 0.008), fc=rng.uniform(1800.0, 6500.0), q=0.8)
    for _ in range(70):
        t = rng.uniform(0.0, DUREE)
        f0 = rng.uniform(380.0, 900.0)
        da.sinus_glisse(ev, f0, f0 * rng.uniform(1.6, 2.4), rng.uniform(0.015, 0.05), rng.uniform(0.03, 0.07), debut=t, attaque=0.004)
    ev = da.plier(ev, DUREE)
    scale = 0.35
    return [a + scale * b for a, b in zip(buf, ev)]


# (nom du fichier, lot, cible de niveau perçu en dB, fondu / None / BOUCLE, fabrique)
SONS = [
    ("cascade_boucle", E, -15.5, da.BOUCLE, lambda: cascade(2702)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
