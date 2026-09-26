"""Portail et téléportation (Deathless, lot 2 du cahier des charges son, 26/09/2026).

Le portail est un disque de gemmes vertes, énergie de Nyxessa : il parle avec la **gemme** de la relique (partiels
1 : 2,756 : 5,404 : 8,933, jumeaux désaccordés), en anneaux (courses de gemmes qui s'ouvrent ou se referment), avec
un **bourdon de verre** tant qu'il est ouvert. Un passage est une goutte tombée dans l'eau : une goutte grave, puis
des anneaux (souffles concentriques) ; à la sortie, les anneaux font le chemin inverse. Les arrivées (chute du ciel au
donjon, sortie du sol au village) mêlent l'air, la pierre et la terre au dernier accord de gemmes. Détail :
Docs/son-cahier-des-charges.md (§ 3.4).

Sons écrits (Assets/Audio/Deathless/Portail/, WAV 44,1 kHz mono 16 bits, graines 1301 à 1349) :
  portail_ouverture         l'anneau s'ouvre (gemmes du grave vers l'aigu), souffle qui s'étale, le bourdon s'installe
  portail_fermeture         l'anneau se referme vers le centre, souffle aspiré, gemme grave qui s'éteint
  portail_bourdon_boucle    portail ouvert (boucle 6 s) : bourdon de verre, eau qui tourne, rares gemmes
  portail_depart            un corps part en gemmes : goutte grave, trois anneaux qui s'élargissent, gemmes aspirées
  portail_arrivee           les gemmes jaillissent et reforment le corps : anneaux qui convergent, accord posé
  portail_chute_ciel        arrivée au donjon (Spawn_Air) : souffle qui descend, réception sur la pierre
  portail_sortie_sol        retour au village (Spawn_Ground) : la terre s'ouvre, gravier qui monte, deux gemmes
  portail_ferme_refus       Interagir près du portail fermé la nuit : gemme étouffée, toc de pierre (2D)

Usage : python -B synth_portail.py [dossier_sortie] [nom ...]   (par défaut : le dossier du script, tous les sons).
Catalogue : ids dl_portail_* dans Wiki/data/sons.json.
"""
import math
import os
import random
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
import deathless_audio as da  # noqa: E402

N = da.note


def bourdon_verre(duree, amp, entree=0.0):
    """Bourdon de verre du portail : mi3, si3, mi4 et leurs partiels 2,756 (pour qu'il s'entende aussi sur de petites
    enceintes), chacun doublé d'un jumeau à 1/3 Hz. Fréquences arrondies à un nombre entier de périodes sur `duree`
    (la boucle se raccorde exactement) ; `entree` : montée progressive en secondes (0 : plein dès le début)."""
    n = da.idx(duree)
    res = [0.0] * n
    for f, a in ((N("E3"), 0.3), (N("B3"), 0.22), (N("E4"), 0.16), (N("E3") * 2.756, 0.06), (N("E4") * 2.756, 0.05)):
        for ecart, ph in ((0.0, 0.0), (1 / 3.0, 2.1)):
            fb = round((f + ecart) * duree) / duree
            w = 2 * math.pi * fb / da.RATE
            for i in range(n):
                res[i] += amp * a * 0.5 * math.sin(w * i + ph)
    if entree > 0:
        ne = da.idx(entree)
        for i in range(min(n, ne)):
            res[i] *= i / ne
    return res


def anneau(rng, buf, debut, duree, notes, amp):
    """Course de gemmes autour du disque, dans l'ordre des notes, amplitude en arche."""
    for k, f in enumerate(notes):
        u = k / max(1, len(notes) - 1)
        arc = 0.6 + 0.4 * (1 - abs(2 * u - 1))
        da.gemme(rng, buf, f, amp * arc, 0.45, debut + duree * u, eclat=0.8, jumeau=False, durete=0.5)


def ouverture(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.65)
    anneau(rng, buf, 0.0, 0.6, da.penta(4, 6), 0.22)
    da.ajouter(buf, da.souffle(rng, 1.5, 400.0, 3000.0, 0.8, 0.3, 1.2, 1.3), 0.0, 0.45)
    bourdon = bourdon_verre(0.85, 1.0, entree=0.6)
    da.ajouter(buf, bourdon, 0.8, 1.0)
    return buf


def fermeture(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.45)
    notes = list(reversed(da.penta(4, 6)))
    anneau(rng, buf, 0.0, 0.5, notes, 0.2)
    da.ajouter(buf, da.souffle(rng, 0.7, 3000.0, 300.0, 1.2, 0.08, 0.6, 1.3), 0.0, 0.55)
    da.gemme(rng, buf, N("E4"), 0.45, 0.9, 0.55, eclat=0.5)
    da.sub(buf, 82.4, 50.0, 0.25, 0.5, 0.55)
    return buf


def bourdon(graine):
    """Boucle de 6 s : bourdon de verre (battements de 1/3 Hz, 2 par boucle), eau qui tourne (passe-bande dont la
    fréquence tourne à 0,5 Hz, 3 tours par boucle, fondu à la jointure), trois gemmes rares repliées sur la boucle."""
    rng = random.Random(graine)
    duree, fondu = 6.0, 0.4
    n = da.idx(duree + fondu)
    eau = da.passe_bande(da.bruit(rng, n),
                         lambda u: 550.0 + 250.0 * math.sin(2 * math.pi * 0.5 * u * (duree + fondu)), 2.0)
    eau = da.fondre_boucle([v * 0.35 for v in eau], duree, fondu)
    evenements = da.tampon(duree + 1.0)
    for t, n_ in ((0.7, "E6"), (2.9, "B6"), (4.6, "G6")):
        da.gemme(rng, evenements, N(n_), 0.08, 0.6, t, eclat=0.6, durete=0.2)
    return [a + b + c for a, b, c in zip(bourdon_verre(duree, 1.0), eau, da.plier(evenements, duree))]


def depart(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.1)
    da.sinus_glisse(buf, 400.0, 140.0, 0.6, 0.07, 0.0, 0.0008, 0.06, 0.6)          # la goutte
    for t, (f0, f1) in ((0.05, (600.0, 1500.0)), (0.2, (1000.0, 2500.0)), (0.35, (1500.0, 4000.0))):
        da.ajouter(buf, da.souffle(rng, 0.25, f0, f1, 1.6, 0.03, 0.22, 1.6), t, 0.4)   # trois anneaux
    da.scintillement(rng, buf, 0.2, 0.8, da.penta(5, 7), 30, 0.14, (0.08, 0.25),
                     densite=lambda u: u ** 0.8, registre=lambda u: u)               # gemmes aspirées
    return buf


def arrivee(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.1)
    for t, (f0, f1) in ((0.0, (4000.0, 1500.0)), (0.12, (2500.0, 1000.0)), (0.24, (1500.0, 600.0))):
        da.ajouter(buf, da.souffle(rng, 0.25, f0, f1, 1.6, 0.03, 0.22, 1.6), t, 0.4)
    da.gemme(rng, buf, N("E7"), 0.18, 0.25, 0.002, eclat=0.6, jumeau=False, durete=0.5)
    da.scintillement(rng, buf, 0.0, 0.7, da.penta(5, 7), 30, 0.14, (0.08, 0.25),
                     densite=lambda u: u ** 1.2, registre=lambda u: 1.0 - 0.7 * u)   # les gemmes retombent
    da.gemme(rng, buf, N("E5"), 0.4, 0.5, 0.75, eclat=0.8)
    da.gemme(rng, buf, N("B5"), 0.3, 0.45, 0.75, eclat=0.8, durete=0.0)
    return buf


def chute_ciel(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.3)
    da.ajouter(buf, da.souffle(rng, 1.0, 3000.0, 500.0, 1.1, 0.3, 0.7, 1.2), 0.0, 0.55)  # la chute
    da.gemme(rng, buf, N("B6"), 0.14, 0.3, 0.003, eclat=0.5, jumeau=False, durete=0.3)
    contact = 1.0
    da.sub(buf, 90.0, 45.0, 0.35, 0.22, contact)                                          # réception
    da.pas_pierre(rng, buf, contact, 0.6)
    da.gravier(rng, buf, contact, 0.25, 22, 0.18, densite=lambda u: u ** 1.8)
    return buf


def sortie_sol(graine):
    rng = random.Random(graine)
    buf = da.tampon(1.3)
    gronde = da.passe_bas(da.passe_bas(da.bruit(rng, da.idx(0.35)), 220.0), 220.0)
    da.ajouter(buf, da.enveloppe(gronde, 0.004, 0.08, 0.26, 1.5), 0.0, 1.2)             # la terre s'ouvre
    da.sub(buf, 62.0, 40.0, 0.3, 0.35)
    da.gravier(rng, buf, 0.05, 0.85, 70, 0.35, densite=lambda u: u ** 1.3,
               bande=lambda u: 800.0 + 1700.0 * u)                                       # le gravier monte
    da.gemme(rng, buf, N("E5"), 0.36, 0.5, 1.0, eclat=0.8)                               # le corps est entier
    da.gemme(rng, buf, N("B5"), 0.3, 0.45, 1.08, eclat=0.8)
    return buf


def ferme_refus(graine):
    rng = random.Random(graine)
    buf = da.tampon(0.4)
    da.gemme(rng, buf, N("E5"), 0.4, 0.25, 0.0, eclat=0.2, durete=0.2)
    da.mode(buf, 300.0, 0.4, 0.04, 0.0, 0.0006)
    da.choc(rng, buf, 0.0, 0.3, 0.0012, 1500.0, 0.7)
    return buf


# (nom du fichier, lot du plan de production, cible de niveau perçu en dB, fondu de fin en s / None / BOUCLE, fabrique)
SONS = [
    ("portail_ouverture", 2, -14.0, 0.15, lambda: ouverture(1301)),
    ("portail_fermeture", 2, -14.0, None, lambda: fermeture(1302)),
    ("portail_bourdon_boucle", 2, -20.0, da.BOUCLE, lambda: bourdon(1303)),
    ("portail_depart", 2, -14.0, None, lambda: depart(1304)),
    ("portail_arrivee", 2, -14.0, None, lambda: arrivee(1305)),
    ("portail_chute_ciel", 2, -14.0, None, lambda: chute_ciel(1306)),
    ("portail_sortie_sol", 2, -14.0, None, lambda: sortie_sol(1307)),
    ("portail_ferme_refus", 2, -19.0, 0.03, lambda: ferme_refus(1308)),
]


def main():
    da.produire(SONS, os.path.dirname(os.path.abspath(__file__)), sys.argv)


if __name__ == "__main__":
    main()
