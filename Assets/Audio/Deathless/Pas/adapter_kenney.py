"""Pas Kenney (RPG Audio, CC0) adaptés aux familles de matières, 02/10/2026.

Les OGG stéréo 48 kHz de Assets/Audio/Kenney/RPGAudio/ ne se décodent pas en Python standard : un export d'éditeur
(Unity, AudioClip.GetData) en a tiré des WAV mono 16 bits rognés (silence de tête et de queue, 250 ms au plus) dans un
dossier `brut`. Ce script les ré-échantillonne à 44,1 kHz (interpolation linéaire), applique le même mastering que les
sons générés (`master`, -12 dB de niveau perçu, fondu de fin de 20 ms) et écrit les variantes du jeu :
  pierre_4 = footstep00   pierre_5 = footstep07   pierre_6 = footstep08   (les plus brefs et les plus durs : pierre)
  terre_6  = footstep06                                                    (le plus moyen et le plus mat : terre)
Les autres (01, 02, 03, 04, 05, 09) restent disponibles dans le catalogue sous kenney_rpg_footstep.
Usage : python -B adapter_kenney.py <dossier_brut> [dossier_sortie]
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import deathless_audio as da  # noqa: E402

CORRESPONDANCE = (("pierre_4", "footstep00"), ("pierre_5", "footstep07"), ("pierre_6", "footstep08"), ("terre_6", "footstep06"))


def reechantillonner(x, taux):
    if taux == da.RATE:
        return x
    n = int(len(x) * da.RATE / taux)
    res = []
    for i in range(n):
        p = i * taux / da.RATE
        k = int(p)
        f = p - k
        a = x[k]
        b = x[min(k + 1, len(x) - 1)]
        res.append(a + (b - a) * f)
    return res


def main():
    brut = sys.argv[1]
    sortie = sys.argv[2] if len(sys.argv) > 2 else os.path.dirname(os.path.abspath(__file__))
    for nom, source in CORRESPONDANCE:
        x, taux, _ = da.lire(os.path.join(brut, source + ".wav"))
        x = reechantillonner(x, taux)
        x = da.master(x, -12.0, 0.02)
        da.ecrire(os.path.join(sortie, nom + ".wav"), x)
        print("écrit", nom, "(%d ms)" % (1000 * len(x) / da.RATE))


if __name__ == "__main__":
    main()
