"""Eau du village (carte v5, 01/10/2026) : la cascade qui tombe dans le bassin, au pied de la montagne du nord.

Son écrit (Assets/Audio/Deathless/Village/, WAV 44,1 kHz mono 16 bits, 3D, graine 2701) :
  cascade_boucle   chute d'eau continue (boucle de 12 s) : grondement sourd de la masse d'eau (bruit brun), rideau
                   (bruit en passe-bande qui respire), éclaboussures (petits chocs aigus semés au hasard)

Catalogue : id « village_cascade » (Wiki/data/sons.json), joué en boucle par CascadeVillage au pied de la chute.
    python Assets/Audio/Deathless/Village/synth_eau.py
"""

import math
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import deathless_audio as da  # noqa: E402

E = "village_v5"
DUREE, FONDU = 12.0, 1.0


def cascade(graine):
    rng = random.Random(graine)
    total = DUREE + FONDU
    n = da.idx(total)
    # masse d'eau : grondement grave
    grave = da.passe_bas(da.bruit_brun(random.Random(rng.random()), n), 260.0)
    # rideau : bruit qui respire (3 cycles par boucle), bande médium
    rideau = da.souffle_module(random.Random(rng.random()), total, 1400.0, 0.7, 3.0 / DUREE, 0.25, 0.2)
    # brillance : bruit aigu léger
    aigu = da.passe_haut(da.bruit(random.Random(rng.random()), n), 3500.0)
    buf = [0.55 * g + 0.5 * r + 0.12 * a for g, r, a in zip(grave, rideau, aigu)]
    buf = da.fondre_boucle(buf, DUREE, FONDU)
    # éclaboussures : chocs courts semés sur la boucle (repliés à la jointure)
    ev = da.tampon(DUREE + 0.5)
    for _ in range(160):
        t = rng.uniform(0.0, DUREE)
        da.choc(rng, ev, t, rng.uniform(0.05, 0.16), duree=rng.uniform(0.002, 0.008), fc=rng.uniform(2200.0, 6000.0), q=0.8)
    ev = da.plier(ev, DUREE)
    return [a + b for a, b in zip(buf, ev)]


# (nom du fichier, lot, cible de niveau perçu en dB, fondu / None / BOUCLE, fabrique)
SONS = [
    ("cascade_boucle", E, -20.0, da.BOUCLE, lambda: cascade(2701)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
