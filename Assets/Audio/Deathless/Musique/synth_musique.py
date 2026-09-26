"""Musiques (Deathless, échantillons de la direction sombre, 26/09/2026 au soir) : les trois morceaux du jeu, pour juger
le bain avant la production complète (§ 4 du cahier des charges).

Instruments, tous synthétisés avec les briques de deathless_audio : **chœur sourd** (voix fantômes par formants),
**bourdon** grave qui bat, **cordes pincées** (Karplus-Strong : luth, basse), **peaux** (tambours sur cadre, toms,
grosse peau), **os creux** (charleston d'os), **métal frotté** (archet sur une plaque), gemmes sombres de Nyxessa,
gouttes et souffles. Gamme commune : mi mineur (nuit, donjon) et sol majeur, son relatif (jour).
Chaque morceau est une **boucle exacte** : les notes qui débordent de la fin sont repliées sur le début, les bourdons
ont un nombre entier de périodes, et la réverbération (Schroeder, **réservée aux musiques** : le jeu ne donne aucun
espace à une source 2D) est calculée en boucle. Stéréo par panoramique à puissance constante et réverbération de
taille différente à gauche et à droite.

Sons écrits (Assets/Audio/Deathless/Musique/, WAV 44,1 kHz **stéréo** 16 bits, graines 3001 à 3099) :
  musique_jour_boucle     village de jour, menu : 92 BPM, sol majeur, 16 mesures (41,7 s) ; luth en arpèges, tambour
                          sur cadre, bourdon doux, chœur « o » en nappe ; deux mesures presque vides (8 et 16)
  musique_nuit_boucle     village la nuit : 110 BPM, mi mineur avec couleurs phrygiennes (fa, si majeur), 16 mesures
                          (34,9 s) ; bourdon, chœur sourd, basse pincée en croches, toms et grosse peau, charleston
                          d'os, métal frotté toutes les 4 mesures, gemmes sombres ; toutes les couches ouvertes
  musique_donjon_boucle   donjon : 72 BPM, mi dorien, 12 mesures (40 s) ; bourdon creux, souffle, gouttes accordées,
                          peau grave, basse rare, métal frotté, plaintes lointaines

Usage : python -B synth_musique.py [dossier_sortie] [nom ...]   (plusieurs minutes de calcul au total).
Catalogue : ids dl_musique_* dans Wiki/data/sons.json.
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import deathless_audio as da  # noqa: E402

N = da.note
E = "echantillons"
CANAUX = 2
MESURE = "rms"
TRAINE = 5.0     # s : ce qui déborde de la fin est replié sur le début


def env(x, attaque, tenue, relache, forme=1.5):
    return da.enveloppe(x, attaque, tenue, relache, forme)


class Piste:
    """Deux canaux de la boucle plus sa traîne ; `ajouter` place un son mono au temps t avec un gain et un panoramique."""

    def __init__(self, duree):
        self.duree = duree
        self.g = da.tampon(duree + TRAINE)
        self.d = da.tampon(duree + TRAINE)

    def ajouter(self, x, t, gain, pan=0.0):
        da.ajouter_stereo(self.g, self.d, x, t, gain, pan)

    def continu(self, x, gain, pan=0.0):
        """Son de la durée exacte de la boucle (bourdon), sans repli."""
        xg, xd = da.panoramique(x, pan)
        for i in range(min(len(x), da.idx(self.duree))):
            self.g[i] += gain * xg[i]
            self.d[i] += gain * xd[i]

    def finir(self, taille, humide):
        g, d = da.plier(self.g, self.duree), da.plier(self.d, self.duree)
        g = da.reverberation(g, taille, humide, circulaire=True)
        d = da.reverberation(d, taille * 1.07, humide, circulaire=True)
        return g, d


def choeur_accord(rng, piste, notes, t, duree, voyelles, gain, pan_ecart=0.5, nombre=2):
    for k, f in enumerate(notes):
        c = da.choeur(rng, duree, f, lambda u: (voyelles[0], voyelles[1], u), nombre=nombre, souffle=0.5,
                      vibrato=(4.0, 0.01))
        pan = pan_ecart * (2 * k / max(1, len(notes) - 1) - 1)
        piste.ajouter(env(c, min(0.6, duree * 0.25), duree * 0.4, duree * 0.35, 1.2), t, gain, pan)


def pincee(rng, piste, f, t, gain, pan, t60=0.8, brillance=0.45, duree=None):
    duree = duree or min(2.5, t60 * 1.2)
    c = da.corde(rng, duree, f, t60, brillance)
    piste.ajouter(env(c, 0.001, duree * 0.5, duree * 0.5, 1.0), t, gain, pan)


def coup(fabrique, duree):
    buf = da.tampon(duree)
    fabrique(buf)
    return buf


# --- Nuit ------------------------------------------------------------------------------------------------------------
NUIT_ACCORDS = [  # (fondamentale de la basse, notes du chœur), deux mesures chacun
    ("E2", ("E3", "G3", "B3")), ("E2", ("E3", "G3", "B3")), ("C2", ("C3", "E3", "G3")), ("D2", ("D3", "F#3", "A3")),
    ("E2", ("E3", "G3", "B3")), ("E2", ("E3", "G3", "B3")), ("F2", ("F3", "A3", "C4")), ("B1", ("B2", "D#3", "F#3")),
]


def nuit(graine):
    rng = random.Random(graine)
    bpm, mesures = 110.0, 16
    temps = 60.0 / bpm
    mesure = 4 * temps
    L = mesures * mesure
    p = Piste(L)
    p.continu(da.bourdon(rng, L, [N("E1"), N("B1")], 0.25, 320.0, boucle=True), 0.15)
    for k, (basse, choeur) in enumerate(NUIT_ACCORDS):
        t0 = 2 * k * mesure
        choeur_accord(rng, p, [N(n) for n in choeur], t0, 2 * mesure + 0.6, ("ou", "o"), 0.24)
        for c in range(16):                                                    # basse pincée en croches
            accent = 1.0 if c % 4 == 0 else (0.75 if c % 2 == 0 else 0.55)
            f = N(basse) * (2.0 if c in (6, 14) else 1.0)
            pincee(rng, p, f, t0 + c * temps / 2, 0.22 * accent, -0.1, 0.35, 0.25, 0.4)
    for m in range(mesures):
        t0 = m * mesure
        if m % 2 == 0:
            p.ajouter(coup(lambda b: da.peau(rng, b, 50.0, 1.0, 0.0, 0.6, 1.6, 1.0), 0.8), t0, 0.3, 0.0)
        for beat, f, g, pan in ((1.5, 98.0, 0.3, 0.3), (2.0, 82.0, 0.35, -0.3), (3.0, 110.0, 0.28, 0.4),
                                (3.5, 98.0, 0.25, 0.3)):
            p.ajouter(coup(lambda b, f=f: da.peau(rng, b, f, 1.0, 0.0, 0.3, 1.4, 0.9), 0.4), t0 + beat * temps, g, pan)
        for c in range(8):                                                     # charleston d'os
            p.ajouter(coup(lambda b: da.os_creux(rng, b, rng.uniform(1500.0, 2200.0), 1.0, 0.0, 0.025), 0.1),
                      t0 + c * temps / 2, 0.12 if c % 2 else 0.07, 0.5)
        if m % 4 == 0:                                                         # métal frotté
            f = N(NUIT_ACCORDS[m // 2][0]) * 4
            m_ = da.metal_frotte(rng, 4 * mesure * 0.9, f, attaque=1.2, tremble=0.4)
            p.ajouter(env(m_, 0.01, 4 * mesure * 0.6, 4 * mesure * 0.29), t0, 0.08, -0.5)
    for m in (3, 11):                                                          # gemmes sombres de Nyxessa
        for k, n in enumerate(("E5", "B4", "G4")):
            g = coup(lambda b, n=n: da.gemme(rng, b, N(n), 1.0, 1.2, 0.0, eclat=0.35, durete=0.2), 1.5)
            p.ajouter(g, m * mesure + k * temps, 0.1, 0.35)
    return p.finir(1.0, 0.25)


# --- Jour ------------------------------------------------------------------------------------------------------------
JOUR_ACCORDS = [
    ("G2", ("G3", "B3", "D4", "G4")), ("D2", ("D3", "F#3", "A3", "D4")), ("E2", ("E3", "G3", "B3", "E4")),
    ("C2", ("C3", "E3", "G3", "C4")), ("G2", ("G3", "B3", "D4", "G4")), ("D2", ("D3", "F#3", "A3", "D4")),
    ("C2", ("C3", "E3", "G3", "C4")), ("D2", ("D3", "F#3", "A3", "D4")),
]
ARPEGE = (0, 1, 2, 3, 2, 1, 2, 1)


def jour(graine):
    rng = random.Random(graine)
    bpm, mesures = 92.0, 16
    temps = 60.0 / bpm
    mesure = 4 * temps
    L = mesures * mesure
    p = Piste(L)
    p.continu(da.bourdon(rng, L, [N("G1"), N("D2")], 0.2, 450.0, boucle=True), 0.08)
    for k, (basse, notes) in enumerate(JOUR_ACCORDS):
        t0 = 2 * k * mesure
        fs = [N(n) for n in notes]
        choeur_accord(rng, p, fs[:3], t0, 2 * mesure + 0.5, ("o", "o"), 0.11)
        pincee(rng, p, N(basse), t0, 0.25, 0.0, 1.6, 0.3, 2.5)
        pincee(rng, p, N(basse), t0 + mesure, 0.18, 0.0, 1.4, 0.3, 2.2)
        for m in range(2):
            vide = (2 * k + m) in (7, 15)                                      # mesures presque vides
            for c in range(8):
                if vide and c > 0:
                    break
                f = fs[ARPEGE[c]] * (2.0 if (m == 1 and c in (3, 4)) else 1.0)
                pincee(rng, p, f, t0 + m * mesure + c * temps / 2, 0.2 if c % 2 == 0 else 0.14,
                       0.35 if c % 2 else -0.35, 1.1, 0.5, 1.4)
    for m in range(mesures):
        t0 = m * mesure
        p.ajouter(coup(lambda b: da.peau(rng, b, 110.0, 1.0, 0.0, 0.3, 1.25, 0.7), 0.4), t0, 0.16, -0.1)
        p.ajouter(coup(lambda b: da.peau(rng, b, 147.0, 1.0, 0.0, 0.2, 1.2, 0.6), 0.3), t0 + 2 * temps, 0.12, 0.1)
        p.ajouter(coup(lambda b: da.os_creux(rng, b, 1400.0, 1.0, 0.0, 0.02), 0.1), t0 + 3.5 * temps, 0.06, 0.4)
    return p.finir(1.1, 0.22)


# --- Donjon ----------------------------------------------------------------------------------------------------------
def donjon(graine):
    rng = random.Random(graine)
    bpm, mesures = 72.0, 12
    temps = 60.0 / bpm
    mesure = 4 * temps
    L = mesures * mesure
    p = Piste(L)
    p.continu(da.bourdon(rng, L, [N("E1"), N("B1")], 1 / 8.0, 220.0, boucle=True), 0.15)
    fondu = 1.0
    for pan in (-0.6, 0.6):
        s = da.souffle_module(random.Random(rng.random()), L + fondu, 600.0, 1.0, 1 / 8.0, 0.6, 0.3)
        p.continu(da.fondre_boucle(s, L, fondu), 0.12, pan)
    for m in range(0, mesures, 2):
        p.ajouter(coup(lambda b: da.peau(rng, b, 44.0, 1.0, 0.0, 1.0, 1.4, 0.8), 1.2), m * mesure, 0.25, 0.0)
    for m, n in ((0, "E2"), (2, "E2"), (4, "G2"), (6, "D2"), (8, "E2"), (10, "B1")):
        pincee(rng, p, N(n), m * mesure + 2 * temps, 0.3, -0.15, 2.0, 0.2, 3.0)
    gouttes = da.penta(4, 6)
    for _ in range(16):                                                        # gouttes accordées
        t = rng.randrange(mesures * 8) * temps / 2
        f = rng.choice(gouttes)
        g = coup(lambda b, f=f: (da.sinus_glisse(b, f * 0.6, f, 0.5, 0.03, 0.0, 0.001, 0.025, 0.7),
                                 da.gemme(rng, b, f, 0.25, 1.4, 0.02, eclat=0.25, jumeau=True, durete=0.0)), 1.8)
        p.ajouter(g, t, 0.22, rng.uniform(-0.8, 0.8))
    for m, f in ((1, 110.0), (5, 123.5), (9, 98.0)):                          # métal frotté
        m_ = da.metal_frotte(rng, 3 * mesure, f, attaque=1.5, tremble=0.5)
        p.ajouter(env(m_, 0.01, 2 * mesure, mesure), m * mesure, 0.09, 0.5 if m % 2 else -0.5)
    for m, f0 in ((3, 196.0), (7, 175.0), (11, 165.0)):                       # plaintes lointaines
        v = da.voix(rng, 2.5, lambda u, f=f0: f * (1 - 0.25 * u ** 1.2), lambda u: ("ou", "o", u), souffle=0.6,
                    vibrato=(5.0, 0.02))
        p.ajouter(da.passe_bas(env(v, 0.5, 0.8, 1.2), 900.0), m * mesure, 0.16, rng.uniform(-0.5, 0.5))
    return p.finir(1.35, 0.35)


# (nom du fichier, lot du plan de production, cible en dB (RMS moyen), fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("musique_jour_boucle", E, -20.0, da.BOUCLE, lambda: jour(3001)),
    ("musique_nuit_boucle", E, -20.0, da.BOUCLE, lambda: nuit(3002)),
    ("musique_donjon_boucle", E, -21.0, da.BOUCLE, lambda: donjon(3003)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv, MESURE)


if __name__ == "__main__":
    main()
