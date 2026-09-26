"""Marteau sur l'enclume, trois variantes (Deathless, 26 septembre 2026, demande de Quentin pour le forgeron du village :
« contente-toi de 3 sons aléatoires de marteau contre l'enclume »). Aucun son d'enclume n'existe dans le projet et
rien n'est téléchargé : les sons sont synthétisés ici, en Python pur (bibliothèque standard seulement : wave, math,
random, struct). WAV 44,1 kHz, mono, 16 bits. Graines fixes : relancer donne exactement les mêmes fichiers.

Modèle d'un coup de marteau sur une enclume :
- clang métallique : somme de partiels inharmoniques aigus (de 1,5 à 5 kHz environ, rapports de fréquences d'une
  barre épaisse, pas d'une corde), chacun doublé d'un jumeau désaccordé de quelques hertz (léger battement, le « chant »
  de l'enclume) ; attaque très sèche (0,4 ms), puis décroissance exponentielle, plus lente pour les graves (T60 de
  1,2 s) que pour les aigus (0,6 s) ;
- choc : bref bruit d'impact au tout début (bruit blanc passé en passe-haut, 3 à 4 ms), et un « toc » grave très
  court (la masse du marteau et le billot) ;
- variantes : hauteur de base différente (1500 à 1700 Hz), rapports des partiels et amplitudes tirés autour du modèle
  (le timbre change un peu d'un coup à l'autre), force du choc différente.
Chaque fichier est normalisé à -1,4 dBFS (crête à 0,85) : aucune saturation. Aucune réverbération : l'espace vient
du jeu (son 3D dans la forge).

Usage : python -B synth_enclume.py [dossier_sortie]   (par défaut : le dossier du script ; -B évite un __pycache__
dans Assets/). Écrit enclume_1.wav, enclume_2.wav, enclume_3.wav. Catalogue : id forge_enclume (Wiki/data/sons.json).
"""
import math
import os
import random
import struct
import sys
import wave

RATE = 44100
DUREE = 1.25          # s
FONDU = 0.06          # s, fondu de fin (pas de clic à la coupure)
CRETE = 0.85          # crête après normalisation (-1,4 dBFS)

# Modèle commun : rapports des partiels (barre épaisse, inharmonique), amplitudes relatives, T60 (s).
RAPPORTS = [1.0, 1.52, 2.13, 2.71, 3.18]
AMPLITUDES = [1.0, 0.72, 0.55, 0.36, 0.26]
T60 = [1.2, 1.0, 0.85, 0.7, 0.6]

# Variantes : graine, fréquence de base (Hz), force du choc, T60 multiplié.
VARIANTES = [
    (901, 1580.0, 1.0, 1.0),
    (902, 1690.0, 0.8, 0.9),
    (903, 1500.0, 1.15, 1.08),
]


def partiels(rng, f0, allonge):
    """Liste (fréquence, amplitude, constante de temps, phase) des partiels d'une variante, jumeaux compris."""
    res = []
    for r, a, t60 in zip(RAPPORTS, AMPLITUDES, T60):
        f = f0 * r * rng.uniform(0.975, 1.025)          # timbre : rapports un peu décalés
        a *= rng.uniform(0.7, 1.3)                       # timbre : poids des partiels
        tau = t60 * allonge * rng.uniform(0.9, 1.1) / 6.9078   # exp(-t / tau) : -60 dB à T60
        ecart = rng.uniform(1.5, 4.0)                    # battement du jumeau (Hz)
        res.append((f, a * 0.62, tau, rng.uniform(0, 2 * math.pi)))
        res.append((f + ecart, a * 0.38, tau * 0.92, rng.uniform(0, 2 * math.pi)))
    return res


def coup(graine, f0, force, allonge):
    rng = random.Random(graine)
    n = int(DUREE * RATE)
    sortie = [0.0] * n
    # clang : partiels inharmoniques, attaque de 0,4 ms
    attaque = 0.0004
    for f, a, tau, ph in partiels(rng, f0, allonge):
        w = 2 * math.pi * f / RATE
        k = math.exp(-1.0 / (tau * RATE))
        env = a
        for i in range(n):
            t = i / RATE
            g = env * (t / attaque if t < attaque else 1.0)
            sortie[i] += g * math.sin(w * i + ph)
            env *= k
    # choc : bruit blanc en passe-haut (premier ordre, coupure ~1,2 kHz), enveloppe de 3,5 ms
    rc = 1.0 / (2 * math.pi * 1200.0)
    alpha = rc / (rc + 1.0 / RATE)
    precedent_x = precedent_y = 0.0
    tau_bruit = 0.0035 * rng.uniform(0.85, 1.15)
    for i in range(int(0.03 * RATE)):
        t = i / RATE
        x = rng.uniform(-1.0, 1.0)
        y = alpha * (precedent_y + x - precedent_x)
        precedent_x, precedent_y = x, y
        env = (t / 0.0003 if t < 0.0003 else 1.0) * math.exp(-t / tau_bruit)
        sortie[i] += 1.1 * force * env * y
    # toc grave très court (masse du marteau, billot)
    f_toc = rng.uniform(210.0, 260.0)
    for i in range(int(0.06 * RATE)):
        t = i / RATE
        sortie[i] += 0.45 * force * math.exp(-t / 0.012) * math.sin(2 * math.pi * f_toc * t)
    # fondu de fin
    nf = int(FONDU * RATE)
    for i in range(nf):
        sortie[n - nf + i] *= 0.5 * (1 + math.cos(math.pi * i / nf))
    # normalisation (aucune saturation)
    crete = max(abs(v) for v in sortie) or 1.0
    return [v * CRETE / crete for v in sortie]


def ecrire(chemin, echantillons):
    with wave.open(chemin, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", max(-32767, min(32767, int(round(v * 32767))))) for v in echantillons))


def main():
    dossier = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
    for k, (graine, f0, force, allonge) in enumerate(VARIANTES, 1):
        chemin = os.path.join(dossier, "enclume_%d.wav" % k)
        ecrire(chemin, coup(graine, f0, force, allonge))
        print("écrit", chemin)


if __name__ == "__main__":
    main()
