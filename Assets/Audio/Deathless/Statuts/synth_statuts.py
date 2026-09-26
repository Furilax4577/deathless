"""Statuts (Deathless, échantillons de la direction sombre, 26/09/2026 au soir).

Des sons courts et discrets, joués sur beaucoup de cibles à la fois : étoiles de métal fin pour l'étourdi (pas de
gemme), petit feu pour la brûlure, chute lourde pour le renversé, hoquet et tangage pour l'ivresse (humour permis :
c'est la taverne). Détail : § 3.16 du cahier.

Sons écrits (Assets/Audio/Deathless/Statuts/, WAV 44,1 kHz mono 16 bits, graines 2201 à 2249) :
  etourdi_boucle     étourdi (boucle 1 s, 3D) : trois étoiles de métal qui tournent deux fois par seconde
  brulure_boucle     brûlure (boucle 1 s, 3D) : petit feu, crépitements épars
  renverse_chute     renversé (3D) : chute sur le dos, équipement, souffle coupé
  ivresse_debut_1    ivresse (2D) : hoquet, note de bois qui tangue
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


def etourdi(graine):
    rng = random.Random(graine)
    duree = 1.0
    ev = da.tampon(duree + 0.3)
    for k in range(6):
        f = (2600.0, 3100.0, 3500.0)[k % 3]
        da.plaque(rng, ev, f, 0.3, 0.12, k / 6.0 + 0.01, durete=0.2)
    return da.plier(ev, duree)


def brulure(graine):
    rng = random.Random(graine)
    duree, fondu = 1.0, 0.15
    f = da.feu(rng, duree + fondu, 9, 0.35)
    return da.fondre_boucle(f, duree, fondu)


def renverse(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.8)
    da.ajouter(buf, da.souffle(rng, 0.25, 1200.0, 500.0, 1.0, 0.02, 0.22, 1.5), 0.0, 0.3)
    da.peau(rng, buf, 65.0, 0.9, 0.2, 0.35, 1.4, 1.0)
    da.peau(rng, buf, 90.0, 0.5, 0.28, 0.2, 1.3, 0.6)
    for _ in range(5):
        da.plaque(rng, buf, rng.uniform(1800.0, 3000.0), 0.08, 0.04, 0.2 + rng.uniform(0, 0.1), durete=0.3)
    return buf


def ivresse(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.8)
    da.sinus_glisse(buf, 280.0, 560.0, 0.5, 0.05, 0.0, 0.003, 0.04, 0.6)                    # hic !
    da.ajouter(buf, env(da.passe_bande(da.bruit(rng, da.idx(0.06)), 900.0, 1.5), 0.002, 0.01, 0.048), 0.0, 0.3)
    da.sinus_glisse(buf, N("E4"), N("E4") * 0.94, 0.3, 0.6, 0.15, 0.005, 0.5, 1.0)            # la tête tourne
    da.sinus_glisse(buf, N("E4") * 3.93, N("E4") * 3.93 * 0.94, 0.05, 0.3, 0.15, 0.005, 0.28, 1.0)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("etourdi_boucle", E, -22.0, da.BOUCLE, lambda: etourdi(2201)),
    ("brulure_boucle", E, -22.0, da.BOUCLE, lambda: brulure(2202)),
    ("renverse_chute", E, -14.0, None, lambda: renverse(2203)),
    ("ivresse_debut_1", E, -17.0, None, lambda: ivresse(2204)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
