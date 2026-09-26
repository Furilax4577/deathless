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
    nf = min(len(buf), idx(fondu))
    for i in range(nf):
        buf[len(buf) - nf + i] *= 0.5 * (1 + math.cos(math.pi * i / nf))
    # retire une éventuelle composante continue (moyenne), sans toucher au transitoire
    moyenne = sum(buf) / len(buf)
    buf = [v - moyenne for v in buf]
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
    crete = max(abs(v) for v in x) or 1e-9
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
