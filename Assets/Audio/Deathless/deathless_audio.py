"""Briques communes de synthèse des sons de Deathless (identité sonore propre, lot 1 du 26/09/2026).

Cahier des charges : Docs/son-cahier-des-charges.md (palette de timbres, niveaux, nommage). Chaque famille a son
script (Nyxessa/synth_nyxessa.py, Interface/synth_interface.py…) qui importe ce module. Python 3 standard seulement
(math, random, struct, wave, cmath) : aucun paquet, aucun échantillon, aucun téléchargement.

Conventions tenues par ces briques :
- WAV 44,1 kHz, mono, 16 bits ; aucune réverbération (l'espace vient du jeu) ;
- graines fixes : un script relancé réécrit exactement les mêmes fichiers ;
- le son part dès le déclenchement (attaques de 0,3 à 2 ms, jamais plus de 20 ms de silence en tête) ;
- mastering `master` : gain tel que le niveau perçu (RMS maximal sur 50 ms) atteigne la cible de la famille, sans que
  la crête dépasse 0,85 (-1,4 dBFS) ; fondu de fin en cosinus (pas de clic à la coupure).

Gamme commune : mi mineur pentatonique (mi, sol, la, si, ré). Les gemmes de Nyxessa, les notes de l'interface et les
musiques partagent cette gamme : un tintement joué pendant la musique ne sonne jamais faux.

Usage des scripts : python -B <script> [dossier_sortie]   (-B : pas de __pycache__ dans Assets/, Unity lui ferait des
.meta). Planche de contrôle : python -B Assets/Audio/Deathless/controle.py
"""
import cmath
import math
import random
import struct
import wave

RATE = 44100
CRETE = 0.85            # -1,4 dBFS
FENETRE_NIVEAU = 0.05   # s, fenêtre du niveau perçu (RMS maximal)

# --- Gamme : mi mineur pentatonique -------------------------------------------------------------------------------
_DEMITONS = {"C": -9, "D": -7, "E": -5, "F": -4, "G": -2, "A": 0, "B": 2}


def note(nom):
    """Fréquence d'une note ('E6', 'B5', 'F#4') ; la4 = 440 Hz."""
    lettre, reste = nom[0], nom[1:]
    alt = 0
    if reste.startswith("#"):
        alt, reste = 1, reste[1:]
    elif reste.startswith("b"):
        alt, reste = -1, reste[1:]
    octave = int(reste)
    return 440.0 * 2.0 ** ((_DEMITONS[lettre] + alt + 12 * (octave - 4)) / 12.0)


PENTA = ["E", "G", "A", "B", "D"]


def penta(octave_min, octave_max):
    """Notes de la gamme commune entre deux octaves (fréquences croissantes)."""
    res = []
    for o in range(octave_min, octave_max + 1):
        for n in PENTA:
            res.append(note(n + str(o)))
    return sorted(res)


# --- Tampons -------------------------------------------------------------------------------------------------------
def tampon(duree):
    return [0.0] * int(round(duree * RATE))


def idx(t):
    return int(round(t * RATE))


# --- Oscillateurs --------------------------------------------------------------------------------------------------
def mode(buf, f, amp, t60, debut=0.0, attaque=0.0005, phase=0.0):
    """Sinusoïde amortie (un mode de résonance) : -60 dB au bout de t60 s. Rotation complexe : rapide en Python."""
    if f <= 0 or f >= RATE / 2 or amp == 0:
        return
    i0 = idx(debut)
    n = min(len(buf) - i0, int(t60 * 1.15 * RATE))
    if n <= 0:
        return
    k = math.exp(-6.9078 / (t60 * RATE))
    w = k * cmath.exp(1j * 2 * math.pi * f / RATE)
    z = amp * cmath.exp(1j * phase)
    na = max(1, int(attaque * RATE))
    for i in range(n):
        v = z.imag
        if i < na:
            v *= i / na
        buf[i0 + i] += v
        z *= w


def sinus_glisse(buf, f0, f1, amp, duree, debut=0.0, attaque=0.002, relache=None, courbe=1.0, phase=0.0):
    """Sinus dont la fréquence glisse de f0 à f1 (courbe exponentielle), enveloppe attaque / décroissance."""
    i0 = idx(debut)
    n = min(len(buf) - i0, idx(duree))
    relache = duree * 0.6 if relache is None else relache
    ph = phase
    for i in range(n):
        u = i / n
        f = f0 * (f1 / f0) ** (u ** courbe)
        ph += 2 * math.pi * f / RATE
        t = i / RATE
        env = min(1.0, t / attaque) if attaque > 0 else 1.0
        if t > duree - relache:
            env *= max(0.0, (duree - t) / relache)
        buf[i0 + i] += amp * env * math.sin(ph)


# --- Bruits et filtres ---------------------------------------------------------------------------------------------
def bruit(rng, n):
    return [rng.uniform(-1.0, 1.0) for _ in range(n)]


def passe_bas(x, fc):
    a = 1.0 - math.exp(-2 * math.pi * fc / RATE)
    y, s = [0.0] * len(x), 0.0
    for i, v in enumerate(x):
        s += a * (v - s)
        y[i] = s
    return y


def passe_haut(x, fc):
    bas = passe_bas(x, fc)
    return [v - b for v, b in zip(x, bas)]


def passe_bande(x, fc, q=1.0):
    """Passe-bande (filtre à variable d'état de Chamberlin), fc fixe ou fonction de u (0 → 1) pour un balayage."""
    n = len(x)
    y = [0.0] * n
    low = band = 0.0
    amort = 1.0 / q
    for i, v in enumerate(x):
        f = fc(i / n) if callable(fc) else fc
        f = min(f, RATE / 6.5)
        g = 2 * math.sin(math.pi * f / RATE)
        low += g * band
        high = v - low - amort * band
        band += g * high
        y[i] = band
    return y


def enveloppe(x, attaque, tenue, relache, forme=1.0):
    """Enveloppe attaque linéaire, tenue, décroissance (puissance `forme`) sur toute la longueur de x."""
    n = len(x)
    na, nt = idx(attaque), idx(tenue)
    nr = max(1, n - na - nt)
    res = [0.0] * n
    for i in range(n):
        if i < na:
            g = i / max(1, na)
        elif i < na + nt:
            g = 1.0
        else:
            g = max(0.0, 1.0 - (i - na - nt) / nr) ** forme
        res[i] = x[i] * g
    return res


def ajouter(buf, x, debut=0.0, gain=1.0):
    i0 = idx(debut)
    for i, v in enumerate(x):
        j = i0 + i
        if 0 <= j < len(buf):
            buf[j] += gain * v


def choc(rng, buf, debut, amp, duree=0.002, fc=3000.0, q=0.7):
    """Petit bruit d'impact (quelques ms) : la « dureté » d'un coup."""
    n = idx(duree * 4)
    x = passe_bande(bruit(rng, n), fc, q)
    tau = duree
    x = [v * math.exp(-(i / RATE) / tau) * min(1.0, i / 12.0) for i, v in enumerate(x)]
    ajouter(buf, x, debut, amp)


# --- Timbres de la palette -----------------------------------------------------------------------------------------
# Gemme (Nyxessa) : petite barre de cristal libre aux deux bouts (rapports 1 : 2,756 : 5,404 : 8,933), chaque partiel
# doublé d'un jumeau désaccordé de 0,6 à 3 Hz (le « frisson » de la gemme), aigus plus brefs que le fondamental.
GEMME_RAPPORTS = [1.0, 2.756, 5.404, 8.933]
GEMME_AMPS = [1.0, 0.42, 0.2, 0.09]
GEMME_T60 = [1.0, 0.55, 0.3, 0.16]


def gemme(rng, buf, f0, amp, t60, debut=0.0, eclat=1.0, jumeau=True, durete=1.0):
    """Tintement de gemme. `eclat` pondère les partiels aigus, `durete` le petit choc de verre à l'attaque."""
    for r, a, t in zip(GEMME_RAPPORTS, GEMME_AMPS, GEMME_T60):
        f = f0 * r * rng.uniform(0.996, 1.004)
        if f >= RATE * 0.45:
            continue
        poids = a * (eclat if r > 1 else 1.0)
        ph = rng.uniform(0, 2 * math.pi)
        if jumeau:
            ecart = rng.uniform(0.6, 3.0)
            mode(buf, f, amp * poids * 0.62, t60 * t, debut, 0.0004, ph)
            mode(buf, f + ecart, amp * poids * 0.38, t60 * t * 0.93, debut, 0.0004, ph + 1.3)
        else:
            mode(buf, f, amp * poids, t60 * t, debut, 0.0004, ph)
    if durete > 0:
        choc(rng, buf, debut, amp * 0.35 * durete, 0.0012, min(7000.0, f0 * 3.0), 0.8)


def scintillement(rng, buf, debut, duree, notes, nombre, amp, t60=(0.12, 0.35), densite=None, registre=None):
    """Nuage de petites gemmes. `densite(u)` répartit les instants (u de 0 à 1), `registre(u)` choisit la zone de la
    liste de notes (0 = grave, 1 = aigu) : un scintillement peut monter ou descendre."""
    for k in range(nombre):
        u = rng.random()
        if densite is not None:
            u = densite(u)
        t = debut + u * duree
        if registre is not None:
            centre = registre(u) * (len(notes) - 1)
            j = int(round(min(len(notes) - 1, max(0, centre + rng.uniform(-1.5, 1.5)))))
        else:
            j = rng.randrange(len(notes))
        gemme(rng, buf, notes[j] * rng.uniform(0.998, 1.002), amp * rng.uniform(0.45, 1.0),
              rng.uniform(*t60), t, eclat=0.7, jumeau=False, durete=0.4)


# Bois sec (interface) : lame de marimba accordée 1 : 3,93 : 9,24, amortie très vite, plus un clic de bois (bande
# 2-5 kHz, 1 ms). Plus le t60 est court, plus c'est « sec » (bloc de bois) ; long : note de marimba chaleureuse.
BOIS_RAPPORTS = [1.0, 3.93, 9.24]
BOIS_AMPS = [1.0, 0.3, 0.1]
BOIS_T60 = [1.0, 0.38, 0.16]


def bois(rng, buf, f0, amp, t60, debut=0.0, clic=1.0, clarte=1.0):
    for r, a, t in zip(BOIS_RAPPORTS, BOIS_AMPS, BOIS_T60):
        f = f0 * r
        if f >= RATE * 0.45:
            continue
        mode(buf, f, amp * a * (clarte if r > 1 else 1.0), t60 * t, debut, 0.0006, rng.uniform(0, 2 * math.pi))
    if clic > 0:
        choc(rng, buf, debut, amp * 0.5 * clic, 0.0009, rng.uniform(2600, 4200), 0.9)


def tambour_bois(rng, buf, f0, amp, debut=0.0, t60=0.25):
    """Caisse de bois grave (tambour de fête) : fondamental qui retombe un peu, deux modes, un souffle d'impact."""
    sinus_glisse(buf, f0 * 1.25, f0, amp, t60, debut, 0.0008, t60 * 0.95, 0.35)
    mode(buf, f0 * 1.59, amp * 0.35, t60 * 0.5, debut, 0.0008)
    mode(buf, f0 * 2.14, amp * 0.18, t60 * 0.35, debut, 0.0008)
    choc(rng, buf, debut, amp * 0.45, 0.003, 1400.0, 0.6)


def souffle(rng, duree, f_debut, f_fin, q=1.2, attaque=0.1, relache=None, forme=1.5):
    """Souffle d'air filtré qui balaie une bande de fréquences (montée, retour, passage)."""
    n = idx(duree)
    relache = duree - attaque if relache is None else relache
    x = passe_bande(bruit(rng, n), lambda u: f_debut * (f_fin / f_debut) ** u, q)
    return enveloppe(x, attaque, max(0.0, duree - attaque - relache), relache, forme)


def sub(buf, f0, f1, amp, duree, debut=0.0, attaque=0.004):
    """Poussée grave (énergie, masse) : sinus qui glisse, décroissance douce."""
    sinus_glisse(buf, f0, f1, amp, duree, debut, attaque, duree * 0.85, 0.5)


# Éclat d'or (critiques, pièces, métal fin) : plaque mince de métal, partiels inharmoniques 1 : 1,59 : 2,14 : 2,65
# (modes d'une plaque circulaire), aigus aussi longs que le fondamental (le métal « sonne », contrairement au bois),
# T60 court de 0,15 à 0,5 s : l'or brille un instant. Pas de jumeau désaccordé : c'est ce frisson qui distingue la
# gemme de Nyxessa du métal. Grave (f0 de 700 Hz à 1,2 kHz) : fer, marteau ; aigu (2,5 à 4 kHz) : or, pièces.
PLAQUE_RAPPORTS = [1.0, 1.59, 2.14, 2.65, 3.16]
PLAQUE_AMPS = [1.0, 0.8, 0.6, 0.45, 0.3]
PLAQUE_T60 = [1.0, 0.9, 0.8, 0.7, 0.6]


def plaque(rng, buf, f0, amp, t60, debut=0.0, durete=1.0):
    for r, a, t in zip(PLAQUE_RAPPORTS, PLAQUE_AMPS, PLAQUE_T60):
        f = f0 * r * rng.uniform(0.99, 1.01)
        if f >= RATE * 0.45:
            continue
        mode(buf, f, amp * a * rng.uniform(0.8, 1.2), t60 * t, debut, 0.0003, rng.uniform(0, 2 * math.pi))
    if durete > 0:
        choc(rng, buf, debut, amp * 0.5 * durete, 0.0008, min(7000.0, f0 * 2.0), 0.8)


# Poussière et terre : grains de gravier (impulsions de 2 ms en bande 1 à 3 kHz) dont la densité suit `densite(u)` et
# la bande `bande(u)` (u de 0 à 1 sur la durée) : ruissellement qui retombe, gravier qui monte, sol qui tremble.
def gravier(rng, buf, debut, duree, nombre, amp, densite=None, bande=None):
    n = idx(0.004)
    for _ in range(nombre):
        u = rng.random()
        if densite is not None:
            u = densite(u)
        fc = bande(u) if bande is not None else rng.uniform(1000.0, 3000.0)
        grain = passe_bande(bruit(rng, n), fc * rng.uniform(0.8, 1.25), 1.5)
        grain = [v * math.exp(-(i / RATE) / 0.0008) for i, v in enumerate(grain)]
        ajouter(buf, grain, debut + u * duree, amp * rng.uniform(0.3, 1.0))


def pas_pierre(rng, buf, debut, amp):
    """Pied ou corps qui se pose sur la pierre : toc sourd (modes 300 et 520 Hz, 40 ms), grain de gravier."""
    mode(buf, 300.0 * rng.uniform(0.9, 1.1), amp, 0.045, debut, 0.0006)
    mode(buf, 520.0 * rng.uniform(0.9, 1.1), amp * 0.5, 0.03, debut, 0.0006)
    choc(rng, buf, debut, amp * 0.5, 0.0015, 1500.0, 0.7)
    gravier(rng, buf, debut, 0.08, 6, amp * 0.25, densite=lambda u: u ** 2)


# --- Direction sombre (26/09/2026 au soir) ---------------------------------------------------------------------------
# Retour de Quentin après écoute des lots 1 et 2 : « trop cristallin, enfantin ». Les briques qui suivent donnent le
# caractère sombre et organique : voix fantômes et chœurs sourds (l'âme captive de Nyxessa), bourdons graves, métal
# frotté, os creux, peaux tendues, souffles modulés, cordes pincées, feu. La gemme reste la signature de Nyxessa, plus
# grave, et toujours mêlée à une voix ou à un bourdon.

# Voix fantôme : synthèse par formants, le modèle source-filtre de la voix humaine.
# - Source : impulsions glottiques de Rosenberg (ouverture 60 % de la période : montée en demi-cosinus sur 2/3, retombée
#   en quart de cosinus sur 1/3, glotte fermée ensuite), dérivées (la bouche rayonne la dérivée du débit), avec gigue de
#   hauteur (dérive aléatoire lissée), vibrato lent, raucité (modulation d'amplitude rapide à 47 Hz) et sous-harmonique
#   (une période sur deux affaiblie : la voix se casse, le cri). Le souffle (bruit blanc) passe surtout quand la glotte
#   est ouverte, comme dans une vraie voix soufflée.
# - Filtre : quatre résonateurs de Klatt en cascade (formants F1 à F4 de la voyelle, avec leurs largeurs de bande), mis à
#   jour tous les 32 échantillons quand la voyelle glisse (« ou » → « o » → « a »).
# - Paramètres : fréquence (Hz, ou fonction de u de 0 à 1 : glissandos), voyelle (nom ou fonction de u → (A, B,
#   mélange)), souffle (0 : voix pleine, 1 : chuchotement), rauque, gigue, vibrato (fréquence, profondeur), sous_harm.
# Les voyelles sombres (« ou », « o ») donnent la plainte ; « a » ouvert donne l'appel et le cri.
VOYELLES = {
    "ou": ((300, 60), (750, 80), (2300, 140), (3200, 200)),
    "o": ((450, 70), (820, 90), (2450, 150), (3350, 200)),
    "a": ((750, 90), (1150, 110), (2600, 160), (3500, 220)),
    "e": ((550, 70), (1650, 100), (2550, 150), (3450, 200)),
    "eu": ((480, 70), (1350, 100), (2350, 150), (3300, 200)),
}


def _formants(a, b, m):
    return [(fa + (fb - fa) * m, ba + (bb - ba) * m) for (fa, ba), (fb, bb) in zip(VOYELLES[a], VOYELLES[b])]


def voix(rng, duree, frequence, voyelle="ou", souffle=0.3, rauque=0.0, gigue=0.01, vibrato=(5.0, 0.015),
         sous_harm=0.0):
    """Voix fantôme (voir le modèle ci-dessus). Renvoie une liste normalisée à une crête de 1."""
    n = idx(duree)
    f_de = frequence if callable(frequence) else (lambda u, f=frequence: f)
    v_de = voyelle if callable(voyelle) else (lambda u, v=voyelle: (v, v, 0.0))
    vib_f, vib_p = vibrato
    ph_vib = rng.uniform(0, 2 * math.pi)
    src = [0.0] * n
    phase = derive = 0.0
    periode = 0
    for i in range(n):
        u = i / max(1, n - 1)
        t = i / RATE
        if i % 300 == 0:
            derive = derive * 0.8 + rng.uniform(-gigue, gigue)
        f = f_de(u) * (1 + vib_p * math.sin(2 * math.pi * vib_f * t + ph_vib) + derive)
        phase += f / RATE
        if phase >= 1.0:
            phase -= 1.0
            periode += 1
        if phase < 0.396:
            g = 0.5 * (1 - math.cos(math.pi * phase / 0.396))
        elif phase < 0.6:
            g = math.cos(0.5 * math.pi * (phase - 0.396) / 0.204)
        else:
            g = 0.0
        if sous_harm and periode % 2:
            g *= 1 - sous_harm
        if rauque:
            g *= 1 - rauque * (0.5 + 0.5 * math.sin(2 * math.pi * 47 * t + math.sin(t * 13)))
        src[i] = g
    d = [src[i] - src[i - 1] if i else 0.0 for i in range(n)]
    crete = max(1e-9, max(abs(v) for v in d))
    sig = [v / crete + souffle * rng.uniform(-1, 1) * (0.3 + 0.7 * src[i]) for i, v in enumerate(d)]
    for rang in range(4):
        y1 = y2 = 0.0
        sortie = [0.0] * n
        A = B = C = 0.0
        for i in range(n):
            if i % 32 == 0:
                a, b, m = v_de(i / max(1, n - 1))
                F, BW = _formants(a, b, m)[rang]
                C = -math.exp(-2 * math.pi * BW / RATE)
                B = 2 * math.exp(-math.pi * BW / RATE) * math.cos(2 * math.pi * F / RATE)
                A = 1 - B - C
            y = A * sig[i] + B * y1 + C * y2
            y2, y1 = y1, y
            sortie[i] = y
        sig = sortie
    crete = max(1e-9, max(abs(v) for v in sig))
    return [v / crete for v in sig]


def choeur(rng, duree, frequence, voyelle="ou", nombre=4, desaccord=0.012, souffle=0.45, octaves=(1.0,),
           vibrato=(4.5, 0.012), gigue=0.008):
    """Chœur sourd : `nombre` voix fantômes par octave, désaccordées de ± `desaccord` (fraction de la fréquence), vibratos
    et gigues indépendants : ça bat, ça respire, on n'entend aucune voix seule. Normalisé à une crête de 1."""
    n = idx(duree)
    res = [0.0] * n
    f_de = frequence if callable(frequence) else (lambda u, f=frequence: f)
    for o in octaves:
        for k in range(nombre):
            ecart = 1 + desaccord * (2 * k / max(1, nombre - 1) - 1) if nombre > 1 else 1.0
            v = voix(rng, duree, lambda u, e=ecart, o=o: f_de(u) * e * o, voyelle, souffle=souffle, gigue=gigue,
                     vibrato=(vibrato[0] * rng.uniform(0.8, 1.2), vibrato[1] * rng.uniform(0.7, 1.3)))
            poids = 1.0 / (1 + 0.6 * abs(math.log2(o)))
            for i in range(n):
                res[i] += v[i] * poids
    crete = max(1e-9, max(abs(v) for v in res))
    return [v / crete for v in res]


def passe_bas_variable(x, fc):
    """Passe-bas du premier ordre dont la coupure suit `fc(u)` (u de 0 à 1) : une voix qui s'éloigne perd ses aigus."""
    n = len(x)
    y, s = [0.0] * n, 0.0
    a = 0.0
    for i, v in enumerate(x):
        if i % 32 == 0:
            a = 1.0 - math.exp(-2 * math.pi * fc(i / max(1, n - 1)) / RATE)
        s += a * (v - s)
        y[i] = s
    return y


def saturer(x, k=2.5):
    """Saturation douce (tangente hyperbolique) : le cri se déchire sans écrêter."""
    t = math.tanh(k)
    return [math.tanh(k * v) / t for v in x]


# Bourdon grave : oscillateurs en dents de scie adoucies (10 harmoniques, amplitudes 1/k × 0,85^k), lus dans une table,
# doublés d'un jumeau écarté de `battement` Hz (battements lents), filtrés en passe-bas (`coupure`). Des fréquences
# arrondies à un nombre entier de périodes sur la durée donnent une boucle exacte.
_TAILLE_TABLE = 4096
TABLE_SCIE = [sum(math.sin(2 * math.pi * k * i / _TAILLE_TABLE) * (0.85 ** k) / k for k in range(1, 11))
              for i in range(_TAILLE_TABLE)]
TABLE_SINUS = [math.sin(2 * math.pi * i / _TAILLE_TABLE) for i in range(_TAILLE_TABLE)]


def oscillateur(n, f, table=TABLE_SCIE, phase=0.0, vibrato=None):
    """Lecture de table à fréquence fixe (ou `vibrato(t)` : facteur de fréquence, évalué tous les 64 échantillons)."""
    res = [0.0] * n
    pas = f * _TAILLE_TABLE / RATE
    p = phase * _TAILLE_TABLE
    facteur = 1.0
    for i in range(n):
        if vibrato is not None and i % 64 == 0:
            facteur = vibrato(i / RATE)
        res[i] = table[int(p) % _TAILLE_TABLE]
        p += pas * facteur
    return res


def bourdon(rng, duree, frequences, battement=0.25, coupure=700.0, boucle=False):
    n = idx(duree)
    res = [0.0] * n
    for f in frequences:
        for ecart in (0.0, battement):
            fb = f + ecart
            if boucle:
                fb = round(fb * duree) / duree
            o = oscillateur(n, fb, TABLE_SCIE, rng.random())
            for i in range(n):
                res[i] += o[i]
    if boucle:
        res = circulaire(lambda x: passe_bas(passe_bas(x, coupure), coupure), res)
    else:
        res = passe_bas(passe_bas(res, coupure), coupure)
    crete = max(1e-9, max(abs(v) for v in res))
    return [v / crete for v in res]


def circulaire(filtre, x):
    """Applique un filtre à une boucle comme si elle tournait déjà : on filtre deux tours et on garde le second (l'état
    du filtre à la fin du premier tour est celui du début du second : pas de saut à la jointure)."""
    n = len(x)
    return filtre(x + x)[n:]


# Métal frotté : un archet sur une plaque ou une scie. Partiels inharmoniques de plaque (1, 1,59, 2,14, 2,30, 2,65,
# 2,92, 3,16, 3,50), chacun excité **lentement** (montée de `attaque` s) avec une amplitude qui tremble (bruit lissé à
# 5 Hz : le frottement accroche et glisse), plus un filet de bruit d'archet autour du partiel 2,14. Le son enfle, grince
# un peu, et s'éteint quand l'archet quitte la plaque (relâche des 30 derniers pour cent).
METAL_RAPPORTS = [1.0, 1.59, 2.14, 2.30, 2.65, 2.92, 3.16, 3.50]
METAL_AMPS = [1.0, 0.7, 0.8, 0.4, 0.5, 0.3, 0.35, 0.2]


def _bruit_lisse(rng, n, frequence):
    """Bruit lissé (points aléatoires tous les 1/frequence s, interpolés en cosinus), entre -1 et 1."""
    pas = max(1, int(RATE / frequence))
    points = [rng.uniform(-1, 1) for _ in range(n // pas + 2)]
    res = [0.0] * n
    for i in range(n):
        k, r = divmod(i, pas)
        m = 0.5 - 0.5 * math.cos(math.pi * r / pas)
        res[i] = points[k] * (1 - m) + points[k + 1] * m
    return res


def metal_frotte(rng, duree, f0, attaque=0.4, tremble=0.35):
    n = idx(duree)
    res = [0.0] * n
    nr = int(n * 0.3)
    for r, a in zip(METAL_RAPPORTS, METAL_AMPS):
        f = f0 * r * rng.uniform(0.995, 1.005)
        if f >= RATE * 0.45:
            continue
        lisse = _bruit_lisse(rng, n, 5.0)
        w = 2 * math.pi * f / RATE
        ph = rng.uniform(0, 2 * math.pi)
        att = attaque * rng.uniform(0.7, 1.3)
        for i in range(n):
            t = i / RATE
            env = (1 - math.exp(-t / att)) * (1 + tremble * lisse[i])
            if i > n - nr:
                env *= (n - i) / nr
            res[i] += a * env * math.sin(w * i + ph)
    archet = passe_bande(bruit(rng, n), f0 * 2.14, 6.0)
    archet = enveloppe(archet, attaque, max(0.0, duree * 0.7 - attaque), duree * 0.3, 1.0)
    crete_a = max(1e-9, max(abs(v) for v in archet))
    crete = max(1e-9, max(abs(v) for v in res))
    return [v / crete + 0.12 * b / crete_a for v, b in zip(res, archet)]


# Os creux : un petit tube d'os fermé frappé. Modes impairs (1 : 3,03 : 5,1, légèrement désaccordés), T60 court (40 à
# 80 ms) : sec, sans résonance, creux. Le cliquetis est une grappe de petits os (f0 de 1,2 à 2,6 kHz, T60 15 à 30 ms).
def os_creux(rng, buf, f0, amp, debut=0.0, t60=0.06):
    for r, a, t in ((1.0, 1.0, 1.0), (3.03, 0.5, 0.6), (5.1, 0.25, 0.4)):
        mode(buf, f0 * r * rng.uniform(0.98, 1.02), amp * a, t60 * t, debut, 0.0004, rng.uniform(0, 6.3))
    choc(rng, buf, debut, amp * 0.4, 0.0008, 2500.0, 0.9)


def cliquetis(rng, buf, debut, duree, nombre, amp, densite=None):
    for _ in range(nombre):
        u = rng.random()
        if densite is not None:
            u = densite(u)
        os_creux(rng, buf, rng.uniform(1200.0, 2600.0), amp * rng.uniform(0.4, 1.0), debut + u * duree,
                 rng.uniform(0.015, 0.03))


# Peau tendue : membrane circulaire frappée. Fondamental dont la hauteur retombe (tension qui se relâche : f0 × `chute`
# → f0 en 30 ms), modes de membrane 1,594 / 2,136 / 2,296 / 2,653 plus brefs, bruit de baguette ou de main passé en bas.
# Grave (50 à 90 Hz) : grosse caisse, tambour de guerre ; médium (120 à 250 Hz) : tom, tambour sur cadre.
def peau(rng, buf, f0, amp, debut=0.0, t60=0.35, chute=1.4, durete=1.0):
    i0 = idx(debut)
    n = min(len(buf) - i0, idx(t60 * 1.2))
    ph = 0.0
    k = math.exp(-6.9078 / (t60 * RATE))
    env = amp
    for i in range(n):
        t = i / RATE
        f = f0 * (1 + (chute - 1) * math.exp(-t / 0.03))
        ph += 2 * math.pi * f / RATE
        buf[i0 + i] += env * min(1.0, i / 20.0) * math.sin(ph)
        env *= k
    for r, a, t in ((1.594, 0.35, 0.45), (2.136, 0.22, 0.3), (2.296, 0.15, 0.25), (2.653, 0.1, 0.2)):
        mode(buf, f0 * r, amp * a, t60 * t, debut, 0.0005, rng.uniform(0, 6.3))
    if durete > 0:
        x = passe_bas(bruit(rng, idx(0.02)), 2500.0)
        x = [v * math.exp(-(i / RATE) / 0.004) for i, v in enumerate(x)]
        ajouter(buf, x, debut, amp * 0.6 * durete)


def souffle_module(rng, duree, fc, q=1.0, frequence_mod=0.3, profondeur=0.5, derive=0.25):
    """Souffle qui respire : bruit en passe-bande dont la fréquence centrale dérive (± `derive`) et dont l'amplitude
    ondule (`profondeur`) à `frequence_mod` Hz. Pour une boucle, choisir un nombre entier de cycles sur la durée."""
    n = idx(duree)
    ph = rng.uniform(0, 2 * math.pi)
    x = passe_bande(bruit(rng, n), lambda u: fc * (1 + derive * math.sin(2 * math.pi * frequence_mod * u * duree + ph)), q)
    return [v * (1 - profondeur * (0.5 + 0.5 * math.sin(2 * math.pi * frequence_mod * i / RATE + ph + 1.3)))
            for i, v in enumerate(x)]


def bruit_brun(rng, n):
    """Bruit brun (bruit blanc intégré avec fuite) : grondement, terre, masse. Crête normalisée à 1."""
    y, res = 0.0, [0.0] * n
    for i in range(n):
        y = 0.985 * y + 0.15 * rng.uniform(-1, 1)
        res[i] = y
    crete = max(1e-9, max(abs(v) for v in res))
    return [v / crete for v in res]


# Corde pincée (Karplus-Strong) : ligne à retard d'une période, remplie d'un bruit passé en bas (`brillance` : 0 sourd,
# 1 clair), rebouclée à travers une moyenne de deux échantillons et un gain réglé sur le T60 ; lecture fractionnaire
# (interpolation linéaire) pour que la note soit juste. Luth, cordes grattées, basse pincée, corde d'arc.
def corde(rng, duree, f, t60=1.2, brillance=0.5):
    n = idx(duree)
    periode = RATE / f
    taille = int(periode) + 3
    init = passe_bas(bruit(rng, taille), 800.0 + 7000.0 * brillance)
    ligne = init + [0.0] * n
    g = 10 ** (-3.0 / (t60 * f))
    ent, frac = int(periode), periode - int(periode)
    for i in range(taille, taille + n):
        a = ligne[i - ent] * (1 - frac) + ligne[i - ent - 1] * frac
        b = ligne[i - ent - 1] * (1 - frac) + ligne[i - ent - 2] * frac
        ligne[i] = g * 0.5 * (a + b)
    crete = max(1e-9, max(abs(v) for v in init))
    return [v / crete for v in ligne[:n]]


# Feu : grondement (bruit brun passé en bas à 450 Hz, ondulant lentement) et crépitements (impulsions de 0,5 à 2 ms en
# bande 2 à 6 kHz, `crepitements` par seconde). Le feu crépite et gronde, il ne tinte jamais.
def feu(rng, duree, crepitements=18, grondement=1.0):
    n = idx(duree)
    base = passe_bas(bruit_brun(rng, n), 450.0)
    lisse = _bruit_lisse(rng, n, 3.0)
    res = [grondement * b * (0.7 + 0.3 * l) for b, l in zip(base, lisse)]
    crete = max(1e-9, max(abs(v) for v in res))
    res = [v / crete * 0.6 for v in res]
    for _ in range(int(crepitements * duree)):
        t = rng.uniform(0, duree)
        grain = passe_bande(bruit(rng, idx(0.006)), rng.uniform(2000.0, 6000.0), 1.2)
        tau = rng.uniform(0.0005, 0.002)
        grain = [v * math.exp(-(i / RATE) / tau) for i, v in enumerate(grain)]
        ajouter(res, grain, t, rng.uniform(0.2, 0.9))
    return res


# --- Stéréo (ambiances, musiques) ------------------------------------------------------------------------------------
def panoramique(x, pan):
    """Place un signal mono dans le champ stéréo à puissance constante (pan de -1 gauche à 1 droite)."""
    a = (pan + 1) * math.pi / 4
    return [v * math.cos(a) for v in x], [v * math.sin(a) for v in x]


def ajouter_stereo(g, d, x, debut=0.0, gain=1.0, pan=0.0):
    xg, xd = panoramique(x, pan)
    ajouter(g, xg, debut, gain)
    ajouter(d, xd, debut, gain)


def reverberation(x, taille=1.0, humide=0.25, amorti=0.3, circulaire=False):
    """Réverbération de Schroeder (quatre peignes filtrés en parallèle, deux passe-tout en série), **réservée aux
    musiques** (les effets restent secs : l'espace vient du jeu). `circulaire` : le signal est une boucle, la queue de la
    fin revient au début (on traite deux tours et on garde le second)."""
    n = len(x)
    entree = x + x if circulaire else x
    m = len(entree)
    mouille = [0.0] * m
    for ms, fb in ((29.7, 0.805), (37.1, 0.827), (41.1, 0.783), (43.7, 0.764)):
        dl = int(ms * taille * RATE / 1000)
        ligne = [0.0] * m
        filtre = 0.0
        for i in range(m):
            sortie = ligne[i - dl] if i >= dl else 0.0
            filtre = sortie * (1 - amorti) + filtre * amorti
            ligne[i] = entree[i] + fb * filtre
            mouille[i] += sortie
    for ms, gain in ((5.0, 0.7), (1.7, 0.7)):
        dl = int(ms * RATE / 1000)
        tamp = [0.0] * m
        sortie = [0.0] * m
        for i in range(m):
            retard = tamp[i - dl] if i >= dl else 0.0
            tamp[i] = mouille[i] + gain * retard
            sortie[i] = retard - gain * tamp[i]
        mouille = sortie
    mouille = mouille[n:] if circulaire else mouille
    return [a * (1 - humide) + 0.25 * humide * b for a, b in zip(x, mouille)]


# --- Boucles sans raccord ------------------------------------------------------------------------------------------
# Une boucle de L secondes se fabrique en deux parts :
# - les événements (tintements, coups) sont rendus sur L + traîne, puis la traîne est **repliée** sur le début
#   (`plier`) : une gemme qui sonne encore à la fin continue exactement au début, sans coupure ;
# - le continu (souffles, bourdons) est rendu sur L + F, puis ses F dernières secondes sont fondues à puissance
#   constante sur le début (`fondre_boucle`) : le bruit passe la jointure sans trou ni bosse.
# Les modulations (respiration, battements, vibrato) font un nombre entier de cycles sur L.
def plier(x, duree):
    n = idx(duree)
    res = list(x[:n]) + [0.0] * max(0, n - len(x))
    for i in range(n, len(x)):
        res[i % n] += x[i]
    return res


def fondre_boucle(x, duree, fondu):
    n, nf = idx(duree), idx(fondu)
    assert len(x) >= n + nf, "le continu doit durer la boucle plus le fondu"
    res = list(x[:n])
    for i in range(nf):
        a = math.sin(0.5 * math.pi * i / nf)
        b = math.cos(0.5 * math.pi * i / nf)
        res[i] = x[i] * a + x[n + i] * b
    return res


def master_boucle(buf, cible_db):
    """Comme `master`, sans fondu de fin (la boucle reprend au début) ; le niveau perçu d'une boucle est son RMS
    maximal sur 50 ms, comme un effet."""
    moyenne = sum(buf) / len(buf)
    buf = [v - moyenne for v in buf]
    crete = max(abs(v) for v in buf) or 1.0
    gain = min(CRETE / crete, 10 ** ((cible_db - niveau_percu(buf)) / 20.0))
    return [v * gain for v in buf]


# --- Mastering et écriture -----------------------------------------------------------------------------------------
def niveau_percu(x):
    """RMS maximal sur 50 ms (fenêtres glissantes par pas de 10 ms), en dB pleine échelle."""
    n = idx(FENETRE_NIVEAU)
    pas = max(1, idx(0.01))
    meilleur = 0.0
    carres = [v * v for v in x]
    for i in range(0, max(1, len(x) - n + 1), pas):
        s = sum(carres[i:i + n]) / n
        meilleur = max(meilleur, s)
    return 10 * math.log10(meilleur) if meilleur > 0 else -120.0


def master(buf, cible_db, fondu=0.03):
    """Fondu de fin, puis gain vers la cible de niveau perçu, crête plafonnée à 0,85 (-1,4 dBFS)."""
    # retire une éventuelle composante continue (moyenne) avant le fondu : la fin retombe exactement à zéro
    moyenne = sum(buf) / len(buf)
    buf = [v - moyenne for v in buf]
    nf = min(len(buf), idx(fondu))
    for i in range(nf):
        buf[len(buf) - nf + i] *= 0.5 * (1 + math.cos(math.pi * i / nf))
    crete = max(abs(v) for v in buf) or 1.0
    niveau = niveau_percu(buf)
    gain = min(CRETE / crete, 10 ** ((cible_db - niveau) / 20.0))
    return [v * gain for v in buf]


def ecrire(chemin, echantillons):
    with wave.open(chemin, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", max(-32767, min(32767, int(round(v * 32767)))))
                               for v in echantillons))


def lire(chemin):
    with wave.open(chemin, "rb") as w:
        n, canaux, larg, taux = w.getnframes(), w.getnchannels(), w.getsampwidth(), w.getframerate()
        brut = w.readframes(n)
    assert larg == 2, "16 bits attendus"
    vals = struct.unpack("<%dh" % (n * canaux), brut)
    if canaux > 1:
        vals = [sum(vals[i:i + canaux]) / canaux for i in range(0, len(vals), canaux)]
    return [v / 32768.0 for v in vals], taux, canaux


# --- Analyse (planche de contrôle) ---------------------------------------------------------------------------------
BANDES = [(20, 250, "< 250 Hz"), (250, 1000, "250-1k"), (1000, 4000, "1-4k"), (4000, 10000, "4-10k"),
          (10000, 22050, "> 10k")]


def _fft(a):
    n = len(a)
    j = 0
    for i in range(1, n):
        bit = n >> 1
        while j & bit:
            j ^= bit
            bit >>= 1
        j |= bit
        if i < j:
            a[i], a[j] = a[j], a[i]
    longueur = 2
    while longueur <= n:
        w = cmath.exp(-2j * math.pi / longueur)
        for i in range(0, n, longueur):
            wk = 1.0
            moitie = longueur // 2
            for k in range(moitie):
                u = a[i + k]
                v = a[i + k + moitie] * wk
                a[i + k] = u + v
                a[i + k + moitie] = u - v
                wk *= w
        longueur <<= 1
    return a


def spectre_bandes(x, taux=RATE, taille=4096):
    """Part de l'énergie (en %) dans chaque bande de BANDES, spectre moyen sur des trames de Hann."""
    fen = [0.5 - 0.5 * math.cos(2 * math.pi * i / (taille - 1)) for i in range(taille)]
    energie = [0.0] * len(BANDES)
    for d in range(0, max(1, len(x) - taille // 2), taille // 2):
        trame = x[d:d + taille]
        if len(trame) < taille:
            trame = trame + [0.0] * (taille - len(trame))
        spec = _fft([complex(v * f) for v, f in zip(trame, fen)])
        for k in range(1, taille // 2):
            f = k * taux / taille
            p = abs(spec[k]) ** 2
            for b, (lo, hi, _) in enumerate(BANDES):
                if lo <= f < hi:
                    energie[b] += p
                    break
    total = sum(energie) or 1.0
    return [100.0 * e / total for e in energie]


def analyser(chemin):
    x, taux, canaux = lire(chemin)
    with wave.open(chemin, "rb") as w:                  # crête : le canal le plus fort, pas la moyenne des canaux
        brut = w.readframes(w.getnframes())
    crete = max(abs(v) for v in struct.unpack("<%dh" % (len(brut) // 2), brut)) / 32768.0 or 1e-9
    rms = math.sqrt(sum(v * v for v in x) / len(x)) or 1e-9
    seuil = 0.01  # -40 dBFS
    tete = next((i for i, v in enumerate(x) if abs(v) >= seuil), len(x))
    return {
        "duree": len(x) / taux,
        "taux": taux,
        "canaux": canaux,
        "crete_db": 20 * math.log10(crete),
        "rms_db": 20 * math.log10(rms),
        "niveau_db": niveau_percu(x),
        "tete_ms": 1000.0 * tete / taux,
        "fin": abs(x[-1]),
        "bandes": spectre_bandes(x, taux),
    }


# --- Écriture d'une famille ----------------------------------------------------------------------------------------
BOUCLE = "boucle"   # valeur de « fondu » d'une boucle : mastering sans fondu de fin


def niveau_rms(x):
    rms = math.sqrt(sum(v * v for v in x) / len(x)) if x else 0.0
    return 20 * math.log10(rms) if rms > 0 else -120.0


def master_stereo(g, d, cible_db, mesure="niveau", fondu=None):
    """Mastering d'un son stéréo : gain commun aux deux canaux ; `mesure` = « niveau » (RMS maximal sur 50 ms, comme un
    effet) ou « rms » (RMS moyen sur tout le fichier : ambiances et musiques). Crête plafonnée à -1,4 dBFS."""
    if fondu:
        nf = idx(fondu)
        for c in (g, d):
            for i in range(nf):
                c[len(c) - nf + i] *= 0.5 * (1 + math.cos(math.pi * i / nf))
    moy = [(a + b) * 0.5 for a, b in zip(g, d)]
    niveau = niveau_rms(moy) if mesure == "rms" else niveau_percu(moy)
    crete = max(max(abs(v) for v in g), max(abs(v) for v in d)) or 1.0
    gain = min(CRETE / crete, 10 ** ((cible_db - niveau) / 20.0))
    return [v * gain for v in g], [v * gain for v in d]


def ecrire_stereo(chemin, g, d):
    with wave.open(chemin, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<hh", max(-32767, min(32767, int(round(a * 32767)))),
                                           max(-32767, min(32767, int(round(b * 32767)))))
                               for a, b in zip(g, d)))


def produire(sons, dossier_defaut, argv, mesure="niveau"):
    """Écrit les sons d'une famille. `sons` : liste de (nom, lot, cible dB, fondu en s / None / BOUCLE, fabrique) ;
    une fabrique rend une liste (mono) ou un couple (gauche, droite) (stéréo). `mesure` : « niveau » (RMS maximal sur
    50 ms) ou « rms » (RMS moyen, ambiances et musiques). argv : [dossier_sortie] [nom ...]."""
    import os
    dossier = argv[1] if len(argv) > 1 else dossier_defaut
    noms = argv[2:]
    for nom, _lot, cible, fondu, fabrique in sons:
        if noms and nom not in noms:
            continue
        buf = fabrique()
        chemin = os.path.join(dossier, nom + ".wav")
        if isinstance(buf, tuple):
            g, d = master_stereo(list(buf[0]), list(buf[1]), cible, mesure,
                                 None if fondu in (None, BOUCLE) else fondu)
            ecrire_stereo(chemin, g, d)
        else:
            if fondu == BOUCLE:
                buf = master_boucle(buf, cible)
            elif fondu is None:
                buf = master(buf, cible)
            else:
                buf = master(buf, cible, fondu)
            ecrire(chemin, buf)
        print("écrit", chemin)
