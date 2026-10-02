"""Bruits de pas par matière (Deathless, 02/10/2026) : herbe, terre, sable, bois, métal, pierre.

Un pas de botte est un impact bref (le talon) suivi, selon le sol, d'un frottement (herbe, sable, terre) ou d'une
résonance (bois, métal). Chaque matière a cinq variantes (graines différentes : fréquences, grains et niveaux qui
bougent un peu), toutes en WAV 44,1 kHz mono 16 bits, de 100 à 250 ms, normalisées à environ -12 dBFS (niveau perçu ;
crête plafonnée à -1,4 dBFS) : le volume relatif de chaque matière est réglé dans le jeu (PasMatiere), pas ici.
Graines 4100 à 4699 (une centaine par matière) : plage « Pas » du cahier des charges, hors de celle des musiques (3000-3999).
Python standard seulement (briques de deathless_audio.py) : aucun paquet, aucun téléchargement.

  herbe_1..5   froissement doux : bruit rose filtré en bande 1,5 à 5 kHz, enveloppe de 120 ms, attaque lente
  terre_1..5   sourd et granuleux : bruit brun 150 à 800 Hz, grains de terre
  sable_1..5   glissant et plus clair que la terre : grains fins, souffle bas, attaque douce
  bois_1..5    « toc » résonnant : impulsion et deux modes (250 à 900 Hz), décroissance de 150 ms
  metal_1..5   « tic » clair résonnant : impulsion et modes inharmoniques de 1,2 à 6 kHz, 200 ms
  pierre_1..3  claquement sec et court : toc sourd, éclat de talon, grains de gravier (pierre_4..6 : Kenney, adapter_kenney.py)

Usage : python -B synth_pas.py [dossier_sortie] [nom ...]   (par défaut : le dossier du script, tous les sons).
Catalogue : ids pas_herbe, pas_terre, pas_sable, pas_bois, pas_metal, pas_pierre dans Wiki/data/sons.json.
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import deathless_audio as da  # noqa: E402


def rose(rng, n):
    """Bruit rose approché : quatre bruits blancs filtrés en bas à des coupures étagées, somme."""
    blanc = da.bruit(rng, n)
    somme = [0.0] * n
    for fc, g in ((80.0, 1.0), (400.0, 0.8), (1800.0, 0.6), (9000.0, 0.4)):
        bas = da.passe_bas(blanc, fc)
        for i in range(n):
            somme[i] += g * bas[i]
    return somme


def herbe(graine):
    rng = random.Random(graine)
    duree = 0.18
    n = da.idx(duree)
    x = da.passe_bande(rose(rng, n), lambda u: rng.uniform(0, 1) * 0 + 2200.0 + 1800.0 * u, 0.7)
    y = da.passe_bande(rose(rng, n), 1600.0 * rng.uniform(0.9, 1.15), 0.9)
    mix = [a * 0.7 + b * 0.5 for a, b in zip(x, y)]
    # froissement : amplitude modulée par un bruit lent (brins qui se couchent les uns après les autres)
    lent = da.passe_bas(da.bruit(rng, n), 70.0)
    crete = max(1e-9, max(abs(v) for v in lent))
    mix = [v * (0.55 + 0.45 * abs(l) / crete) for v, l in zip(mix, lent)]
    mix = da.enveloppe(mix, 0.022, 0.02, duree - 0.042, 1.6)
    buf = da.tampon(duree)
    da.ajouter(buf, mix, 0.0, 1.0)
    da.mode(buf, rng.uniform(95.0, 130.0), 0.12, 0.05, 0.01, 0.012)       # le poids du pied, très en retrait
    da.gravier(rng, buf, 0.01, 0.1, 5, 0.06, bande=lambda u: 3000.0 + 1500.0 * u)
    return buf


def terre(graine):
    rng = random.Random(graine)
    duree = 0.17
    n = da.idx(duree)
    brun = da.passe_bande(da.bruit_brun(rng, n), rng.uniform(260.0, 420.0), 0.8)
    bas = da.passe_bas(da.bruit_brun(rng, n), 800.0)
    mix = [a * 0.8 + b * 0.5 for a, b in zip(brun, bas)]
    mix = da.enveloppe(mix, 0.006, 0.01, duree - 0.016, 2.0)
    buf = da.tampon(duree)
    da.ajouter(buf, mix, 0.0, 1.0)
    da.mode(buf, rng.uniform(85.0, 120.0), 0.6, 0.06, 0.0, 0.002)        # talon : thump grave
    da.gravier(rng, buf, 0.004, 0.12, 12, 0.14, densite=lambda u: u ** 1.6, bande=lambda u: 500.0 + 900.0 * u)
    return buf


def sable(graine):
    rng = random.Random(graine)
    duree = 0.22
    n = da.idx(duree)
    souffle = da.passe_bande(da.bruit_brun(rng, n), rng.uniform(420.0, 620.0), 0.7)
    souffle = da.enveloppe(souffle, 0.03, 0.03, duree - 0.06, 1.5)
    buf = da.tampon(duree)
    da.ajouter(buf, souffle, 0.0, 0.8)
    da.gravier(rng, buf, 0.0, 0.17, 38, 0.11, densite=lambda u: u ** 1.2, bande=lambda u: 1200.0 + 1300.0 * u)
    da.mode(buf, rng.uniform(90.0, 115.0), 0.25, 0.05, 0.015, 0.01)
    return buf


def bois(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.2)
    f1 = rng.uniform(250.0, 420.0)
    f2 = rng.uniform(620.0, 900.0)
    da.mode(buf, f1, 0.9, 0.15, 0.0, 0.0008, rng.uniform(0, 6.28))
    da.mode(buf, f2, 0.5, 0.10, 0.0, 0.0008, rng.uniform(0, 6.28))
    da.mode(buf, f1 * 1.5, 0.18, 0.06, 0.0, 0.0008)
    da.mode(buf, rng.uniform(80.0, 110.0), 0.4, 0.05, 0.0, 0.002)       # poids du pied dans la planche
    da.choc(rng, buf, 0.0, 0.5, 0.0009, rng.uniform(2200.0, 3400.0), 0.9)
    return buf


def metal(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.25)
    f0 = rng.uniform(1200.0, 1500.0)
    da.plaque(rng, buf, f0, 0.55, 0.2, 0.0, 1.0)                         # partiels 1 : 1,59 : 2,14 : 2,65 : 3,16 (jusqu'à ~4,7 kHz)
    da.mode(buf, f0 * rng.uniform(4.0, 4.4), 0.12, 0.09, 0.0, 0.0003)    # un éclat vers 5-6 kHz
    da.mode(buf, rng.uniform(220.0, 300.0), 0.5, 0.05, 0.0, 0.0008)      # la semelle sur la tôle
    da.choc(rng, buf, 0.0, 0.35, 0.0007, 4500.0, 0.8)
    return buf


def pierre(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.12)
    da.mode(buf, rng.uniform(380.0, 480.0), 0.9, 0.04, 0.0, 0.0004)
    da.mode(buf, rng.uniform(850.0, 1150.0), 0.45, 0.025, 0.0, 0.0004)
    da.mode(buf, rng.uniform(95.0, 130.0), 0.35, 0.04, 0.0, 0.001)
    da.choc(rng, buf, 0.0, 0.8, 0.0012, rng.uniform(1800.0, 2600.0), 0.8)
    da.gravier(rng, buf, 0.0, 0.07, 5, 0.12, densite=lambda u: u ** 2)
    return buf


CIBLE = -12.0
SONS = []
for fabrique, nom, graine in ((herbe, "herbe", 4100), (terre, "terre", 4200), (sable, "sable", 4300),
                              (bois, "bois", 4400), (metal, "metal", 4500), (pierre, "pierre", 4600)):
    for k in range(1, 4 if nom == "pierre" else 6):
        SONS.append(("%s_%d" % (nom, k), 4, CIBLE, 0.02, (lambda f=fabrique, g=graine + k: f(g))))


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
