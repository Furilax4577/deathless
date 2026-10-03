"""Bruits de pas v2 : BOIS, SABLE et MÉTAL (03/10/2026), en attente d'écoute : bois_v2_1..5, sable_v2_1..5, metal_v2_1..5.
Les bois_*, sable_* et metal_* de synth_pas.py (v1) restent ceux que joue le jeu (ids pas_bois, pas_sable, pas_metal) jusqu'à l'accord
de Quentin ; les v2 sont les ids pas_bois_v2, pas_sable_v2, pas_metal_v2 (statut à écouter, champ `remplace`).

Pourquoi : la v1 était synthétisée en Python pur et validée au seul spectre, sans oreille ; bois (centroïde 300 Hz : un
« boum »), sable (grondement sourd) et métal (« tic » de cloche) sonnaient faux. La v2 part de VRAIS échantillons
(Kenney RPG Audio, CC0, Assets/Audio/Kenney/RPGAudio/) retravaillés, complétés par une synthèse modale courte :
  bois_v2_1..5  « toc » boisé sourd : l'impact d'un coup de bois (chop, bookPlace1-3, doorClose_4 : posé sur une table,
              claqué), rogné à l'attaque, assombri (passe-bas 2,2-3,4 kHz), coupé en 180 ms ; résonance de planche
              (deux modes 190-330 Hz et x2,35, 25-45 ms) et poids du pied (70-100 Hz) ; un petit grincement très discret
              (creak3 filtré 500-2400 Hz, -17 dB) sur bois_2 et bois_5.
  sable_v2_1..5 « froissement granuleux » doux : l'allure d'un pas Kenney (footstep01, 02, 04, 05, 09) filtrée en 600-4200 Hz
              (la texture), grains synthétiques de 1,5 à 3,6 kHz (nuée dont la densité retombe), souffle de sable
              350-2600 Hz, et la foulée (passe-bas 170 Hz du même pas) : écrasement, jamais de sifflement (rien > 5,5 kHz).
  metal_v2_1..5 « tchank » sourd d'une plaque ou d'une botte ferrée : l'attaque de metalClick (x2), metalLatch, metalPot1 et
              metalPot3, passe-bande 200 Hz-3,1 kHz, décroissance forcée de 35-48 ms ; modes de plaque inharmoniques et
              graves (330-560 Hz, rapports 1 : 1,58 : 2,31 : 3,1, 15-80 ms : ni cloche ni sifflement) ; semelle (passe-bas
              260 Hz d'un pas Kenney).
Mastering : niveau perçu (RMS maximal sur 50 ms) de -14 dBFS, crête plafonnée à -1,4 dBFS par un limiteur doux (les
transitoires courts ne tiendraient pas -14 sans lui), fondu de fin de 20 ms. WAV 44,1 kHz mono 16 bits. Cinq variantes par matière,
comme la v1 (le basculement se fera dans le catalogue après l'accord : les ids du jeu ne bougent pas d'ici là).

Ce script tourne avec le Python EMBARQUÉ DE BLENDER, qui apporte à la fois numpy et `aud` (décodeur OGG) :
  C:/Users/Furilax/Tools/blender-5.2.2-windows-x64/blender.exe -b --factory-startup --python synth_pas_v2.py -- [dossier_sortie]  (le « -- » est obligatoire, même sans argument)
  [nom ...]
(par défaut : le dossier du script, les 15 sons ; `--mesures` en tête des noms ne fait qu'analyser les fichiers du dossier).
Les échantillons Kenney bruts ne sont pas versionnés : ils sont décodés à la volée depuis les OGG du projet, rien à
régénérer à part. Graines 5200 à 5499 (bois 5200, sable 5300, métal 5400).
Page d'écoute : Docs/audio-ecoute/pas-v2.html, produite par Docs/audio-ecoute/faire_page_pas.py (même interpréteur).
"""
import math
import os
import sys
import wave

import numpy as np

RATE = 44100
ICI = os.path.dirname(os.path.abspath(__file__))
KENNEY = os.path.normpath(os.path.join(ICI, "..", "..", "Kenney", "RPGAudio"))
CIBLE_DB = -14.0
CRETE = 0.85            # -1,4 dBFS
_cache = {}


# ----------------------------------------------------------------------------------------------------- entrées / sorties
def charger(nom):
    """Échantillon Kenney (OGG) décodé par aud, mono, ré-échantillonné à 44,1 kHz (float)."""
    if nom in _cache:
        return _cache[nom].copy()
    import aud
    s = aud.Sound.file(os.path.join(KENNEY, nom + ".ogg"))
    taux = s.specs[0]
    d = np.asarray(s.data(), dtype=np.float64)
    x = d.mean(axis=1) if d.ndim > 1 else d
    if taux != RATE:
        t = np.arange(int(len(x) * RATE / taux)) * taux / RATE
        x = np.interp(t, np.arange(len(x)), x)
    _cache[nom] = x
    return x.copy()


def ecrire(chemin, x):
    pcm = np.clip(np.round(x * 32767.0), -32767, 32767).astype("<i2")
    with wave.open(chemin, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm.tobytes())


def lire(chemin):
    with wave.open(chemin, "rb") as w:
        n = w.getnframes()
        return np.frombuffer(w.readframes(n), dtype="<i2").astype(np.float64) / 32768.0


# ----------------------------------------------------------------------------------------------------- briques
def idx(s):
    return int(round(s * RATE))


def filtre(x, bas=None, haut=None, ordre=2):
    """Passe-haut (coupure `haut`) et/ou passe-bas (coupure `bas`) de gabarit Butterworth, par FFT (phase nulle).
    Un bloc de silence est ajouté de part et d'autre : pas d'enroulement."""
    n = len(x)
    m = 1 << int(math.ceil(math.log2(n + 8192)))
    X = np.fft.rfft(np.pad(x, (4096, m - n - 4096)))
    f = np.fft.rfftfreq(m, 1.0 / RATE)
    g = np.ones_like(f)
    if bas:
        g /= np.sqrt(1.0 + (f / bas) ** (2 * ordre))
    if haut:
        g *= 1.0 / np.sqrt(1.0 + (haut / np.maximum(f, 1e-3)) ** (2 * ordre))
    return np.fft.irfft(X * g, m)[4096:4096 + n]


def bande(x, f1, f2, ordre=2):
    return filtre(x, bas=f2, haut=f1, ordre=ordre)


def vitesse(x, r):
    """Lecture à la vitesse r (hauteur et durée changent : 1,1 = plus aigu de 10 %), interpolation linéaire."""
    t = np.arange(0.0, len(x) - 1, r)
    return np.interp(t, np.arange(len(x)), x)


def attaque(x, frac=0.35):
    """Indice du premier échantillon dont la valeur absolue atteint `frac` de la crête (début du coup)."""
    return int(np.argmax(np.abs(x) >= frac * np.abs(x).max()))


def coup(nom, frac=0.35, avant=0.0015, duree=0.22, apres=0, r=1.0):
    """Le coup d'un échantillon Kenney : de `avant` s avant l'attaque (détectée à partir de l'indice `apres`) sur
    `duree` s, joué à la vitesse r. Montée de 1 ms au début : pas de clic."""
    x = charger(nom)
    i = apres + attaque(x[apres:], frac)
    d = max(0, i - idx(avant))
    seg = x[d:d + int(idx(duree) * r) + 2]
    if r != 1.0:
        seg = vitesse(seg, r)
    seg = seg[:idx(duree)].copy()
    nf = idx(0.001)
    seg[:nf] *= np.linspace(0.0, 1.0, nf)
    return seg


def temps(n):
    return np.arange(n) / RATE


def decroissance(n, tau, depart=0.0):
    t = temps(n)
    return np.exp(-np.maximum(t - depart, 0.0) / tau)


def mode(n, f, tau, amp=1.0, phase=0.0, retard=0.0):
    """Sinus amorti (montée de 0,4 ms) : un mode de planche ou de plaque."""
    t = temps(n) - retard
    y = np.where(t >= 0, np.sin(2 * math.pi * f * t + phase) * np.exp(-np.maximum(t, 0) / tau), 0.0)
    return amp * y * np.minimum(1.0, np.maximum(t, 0) / 0.0004)


def fondu_fin(x, s):
    nf = min(len(x), idx(s))
    x[len(x) - nf:] *= 0.5 * (1 + np.cos(np.pi * np.arange(nf) / nf))
    return x


def ajouter(dest, src, debut_s=0.0, gain=1.0):
    i = idx(debut_s)
    k = min(len(src), len(dest) - i)
    if k > 0:
        dest[i:i + k] += gain * src[:k]


def rms50(x):
    w = idx(0.05)
    if len(x) <= w:
        return 10 * math.log10(max(1e-12, float(np.mean(x * x))))
    c = np.cumsum(np.r_[0.0, x * x])
    return 10 * math.log10(max(1e-12, float(((c[w:] - c[:-w]) / w).max())))


def limiteur(x, plafond=CRETE):
    """Écrêtage doux (tangente hyperbolique) au-dessus d'un genou à 60 % du plafond."""
    k = 0.6 * plafond
    a = np.abs(x)
    sur = a > k
    y = x.copy()
    y[sur] = np.sign(x[sur]) * (k + (plafond - k) * np.tanh((a[sur] - k) / (plafond - k)))
    return y


def master(x, cible_db=CIBLE_DB, fondu=0.02):
    x = x - x.mean()
    x = fondu_fin(x, fondu)
    nd = idx(0.0008)
    x[:nd] *= np.linspace(0.0, 1.0, nd)             # montée de 0,8 ms : pas de clic au déclenchement
    gain = 10 ** ((cible_db - rms50(x)) / 20.0)
    y = limiteur(x * gain)
    # le limiteur a pu baisser le niveau perçu : un second passage le ramène (le plafond reste tenu)
    gain2 = 10 ** ((cible_db - rms50(y)) / 20.0)
    y = limiteur(x * gain * min(gain2, 1.6))
    return np.clip(y, -CRETE, CRETE)


# ----------------------------------------------------------------------------------------------------- bois
# (source du coup, seuil d'attaque, vitesse, passe-bas, f1 de planche, grincement ?)
BOIS = (
    ("chop",        0.45, 0.85, 2400.0, 360.0, False),
    ("bookPlace1",  0.55, 1.00, 3000.0, 440.0, True),
    ("doorClose_4", 0.50, 1.05, 2200.0, 320.0, False),
    ("bookPlace2",  0.55, 1.12, 3400.0, 520.0, False),
    ("chop",        0.45, 1.00, 2800.0, 410.0, True),
)


def bois(k, graine):
    rng = np.random.default_rng(graine)
    source, seuil, r, bas, f1, grince = BOIS[k]
    n = idx(0.21)
    coup_ = coup(source, seuil, duree=0.30, r=r)
    coup_ = bande(coup_, 170.0, bas)
    coup_ = coup_[:n] * decroissance(n, 0.050)[:len(coup_[:n])]
    buf = np.zeros(n)
    ajouter(buf, coup_ / (np.abs(coup_).max() + 1e-9), 0.0, 1.0)
    f1 *= rng.uniform(0.96, 1.04)
    buf += mode(n, f1, 0.032, 0.30, rng.uniform(0, 6.28))                    # la planche
    buf += mode(n, f1 * 2.35 * rng.uniform(0.97, 1.03), 0.020, 0.16, rng.uniform(0, 6.28))
    buf += mode(n, rng.uniform(70.0, 100.0), 0.030, 0.14)                    # le poids du pied dans la charpente
    if grince:
        c = charger("creak3")
        i = attaque(c, 0.12)
        g = bande(c[i:i + idx(0.15)], 500.0, 2400.0)
        m = len(g)
        g = g * np.sin(np.pi * np.arange(m) / m) ** 1.5                       # monte et retombe doucement
        ajouter(buf, g / (np.abs(g).max() + 1e-9), 0.045, 0.14)              # -17 dB environ : à peine un souffle de bois qui travaille
    return buf[:idx(0.21)]


# ----------------------------------------------------------------------------------------------------- sable
SABLE = ("footstep01", "footstep02", "footstep04", "footstep05", "footstep09")


def grains(rng, n, densite, f_min, f_max, amp):
    """Nuée de petites impulsions amorties (1 à 3 ms) : `densite(t)` en grains par seconde."""
    out = np.zeros(n)
    t = 0.0
    duree = n / RATE
    while t < duree:
        lam = densite(t)
        t += rng.exponential(1.0 / max(lam, 1.0))
        if t >= duree:
            break
        f = rng.uniform(f_min, f_max)
        tau = rng.uniform(0.0006, 0.0016)
        g = mode(n, f, tau, amp * rng.uniform(0.3, 1.0), rng.uniform(0, 6.28), retard=t)
        out += g
    return out


def sable(k, graine):
    rng = np.random.default_rng(graine)
    n = idx(0.225)
    src = charger(SABLE[k])
    i = attaque(src, 0.30)
    seg = src[max(0, i - idx(0.004)):][:n]
    seg = np.pad(seg, (0, n - len(seg)))
    texture = bande(seg, 600.0, 4200.0)
    texture = texture / (np.abs(texture).max() + 1e-9)
    foulee = filtre(seg, bas=170.0, haut=45.0)
    foulee = foulee / (np.abs(foulee).max() + 1e-9) * decroissance(n, 0.060)
    t = temps(n)
    # souffle de sable : bruit rose-brun 350-2600 Hz, montée rapide (9 ms), retombée de 70 ms
    bruit = bande(rng.standard_normal(n), 350.0, 2600.0)
    env = np.minimum(1.0, t / 0.009) * np.exp(-np.maximum(t - 0.009, 0) / 0.070)
    # écrasement : modulation lente irrégulière (les grains qui cèdent un à un)
    lent = np.abs(filtre(rng.standard_normal(n), bas=60.0))
    lent = lent / (lent.max() + 1e-9)
    souffle = bruit * env * (0.55 + 0.45 * lent)
    souffle = souffle / (np.abs(souffle).max() + 1e-9)
    nuee = grains(rng, n, lambda u: 700.0 * np.exp(-u / 0.075) + 90.0, 1500.0, 3600.0, 1.0)
    nuee = nuee / (np.abs(nuee).max() + 1e-9)
    buf = 0.50 * texture * np.minimum(1.0, t / 0.006) + 0.60 * souffle + 0.40 * nuee + 0.55 * foulee
    buf = filtre(buf, bas=5500.0, haut=60.0)
    return buf


# ----------------------------------------------------------------------------------------------------- métal
# (source, seuil, indice de départ en s, vitesse, passe-haut, passe-bas, tau, f0 de plaque, semelle)
METAL = (
    ("metalClick", 0.45, 0.00, 0.80, 450.0, 3200.0, 0.040, 640.0, "footstep09"),
    ("metalClick", 0.45, 0.14, 0.92, 500.0, 3300.0, 0.036, 760.0, "footstep03"),
    ("metalLatch", 0.40, 0.00, 0.72, 420.0, 3000.0, 0.042, 700.0, "footstep09"),
    ("metalPot1",  0.35, 0.00, 0.85, 450.0, 3100.0, 0.048, 840.0, "footstep03"),
    ("metalPot3",  0.35, 0.00, 0.95, 500.0, 3300.0, 0.034, 920.0, "footstep09"),
)


def metal(k, graine):
    rng = np.random.default_rng(graine)
    source, seuil, depart, r, hz, bz, tau, f0, semelle = METAL[k]
    n = idx(0.230)
    c = coup(source, seuil, duree=0.30, apres=idx(depart), r=r)
    c = bande(c, hz, bz)[:n]
    c = c * decroissance(len(c), tau)
    buf = np.zeros(n)
    ajouter(buf, c / (np.abs(c).max() + 1e-9), 0.0, 0.90)
    f0 *= rng.uniform(0.96, 1.05)
    for ratio, amp, tm in ((1.0, 1.0, 0.080), (1.58, 0.50, 0.045), (2.31, 0.30, 0.030), (3.10, 0.15, 0.018)):
        buf += mode(n, f0 * ratio * rng.uniform(0.985, 1.015), tm, 0.26 * amp, rng.uniform(0, 6.28))
    s = charger(semelle)
    s = filtre(s[attaque(s, 0.35):][:n], bas=260.0, haut=50.0)
    s = np.pad(s, (0, n - len(s)))
    buf += 0.30 * s / (np.abs(s).max() + 1e-9) * decroissance(n, 0.030)
    return buf


SONS = []
for fabrique, nom, graine in ((bois, "bois", 5200), (sable, "sable", 5300), (metal, "metal", 5400)):
    for k in range(5):
        SONS.append(("%s_v2_%d" % (nom, k + 1), (lambda f=fabrique, kk=k, g=graine + k: f(kk, g))))


# ----------------------------------------------------------------------------------------------------- mesures
def mesures(x):
    """Durée (ms), crête (dBFS), niveau perçu RMS50 (dBFS), centroïde spectral (Hz), attaque (ms jusqu'à 90 % de
    l'enveloppe maximale), silence de tête (ms, seuil -40 dB de la crête), premier/dernier échantillon (clics),
    part de l'énergie sous 250 Hz et au-dessus de 6 kHz (%)."""
    pk = float(np.abs(x).max())
    env = np.sqrt(np.convolve(x * x, np.ones(idx(0.004)) / idx(0.004), "same"))
    att = float(np.argmax(env >= 0.9 * env.max())) / RATE * 1000
    tete = float(np.argmax(np.abs(x) >= pk * 0.01)) / RATE * 1000
    sp = np.abs(np.fft.rfft(x * np.hanning(len(x)))) ** 2
    f = np.fft.rfftfreq(len(x), 1.0 / RATE)
    cent = float((sp * f).sum() / sp.sum())
    return {
        "duree_ms": 1000.0 * len(x) / RATE, "crete_db": 20 * math.log10(pk), "rms50_db": rms50(x), "centroide_hz": cent,
        "attaque_ms": att, "silence_tete_ms": tete, "debut": float(abs(x[0])), "fin": float(abs(x[-1])),
        "grave_pct": 100.0 * float(sp[f < 250].sum() / sp.sum()), "aigu_pct": 100.0 * float(sp[f > 6000].sum() / sp.sum()),
    }


def diversite(liste):
    """Écart moyen deux à deux entre variantes : 1 - corrélation maximale (décalage libre ±5 ms) des enveloppes de
    spectre court terme ; proche de 0 = quasi identiques, > 0,3 = bien distinctes."""
    def sig(x):
        w = 512
        cols = [np.abs(np.fft.rfft(x[s:s + w] * np.hanning(w)))[:128] for s in range(0, max(1, len(x) - w), 256)]
        v = np.log10(np.array(cols) + 1e-4).ravel()
        return (v - v.mean()) / (v.std() + 1e-9)
    nmin = min(len(x) for x in liste)
    sigs = [sig(x[:nmin]) for x in liste]
    m = min(len(s) for s in sigs)
    vals = []
    for a in range(len(sigs)):
        for b in range(a + 1, len(sigs)):
            vals.append(1.0 - float(np.mean(sigs[a][:m] * sigs[b][:m])))
    return float(np.mean(vals))


def tableau(dossier, noms):
    print("%-10s %6s %6s %6s %6s %6s %6s %6s %5s %5s" % ("fichier", "dur ms", "crête", "RMS50", "centr.", "att ms", "tête", "clic", "<250", ">6k"))
    par = {}
    for nom in noms:
        x = lire(os.path.join(dossier, nom + ".wav"))
        m = mesures(x)
        par.setdefault(nom.split("_")[0], []).append(x)
        print("%-10s %6.0f %6.1f %6.1f %6.0f %6.1f %6.1f %6.4f %5.0f %5.1f" % (
            nom, m["duree_ms"], m["crete_db"], m["rms50_db"], m["centroide_hz"], m["attaque_ms"], m["silence_tete_ms"],
            max(m["debut"], m["fin"]), m["grave_pct"], m["aigu_pct"]))
    for mat, l in par.items():
        print("diversité %s : %.2f" % (mat, diversite(l)))


def main():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    seulement_mesures = "--mesures" in args
    args = [a for a in args if a != "--mesures"]
    sortie = ICI
    if args and os.path.isdir(args[0]):
        sortie, args = args[0], args[1:]
    noms = [nom for nom, _ in SONS if not args or nom in args]
    if not seulement_mesures:
        for nom, fabrique in SONS:
            if nom in noms:
                ecrire(os.path.join(sortie, nom + ".wav"), master(fabrique()))
                print("écrit", nom)
    tableau(sortie, noms)


if __name__ == "__main__":
    main()
