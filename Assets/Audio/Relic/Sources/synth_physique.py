"""Sons d'armes de trait par modélisation physique (Deathless, 25 septembre 2026, demande de Quentin : « le son de
l'arc doit être plus réaliste », puis « idem pour l'arbalète »). Python pur, WAV 44,1 kHz 16 bits mono, aucun
échantillon, graines fixes : relancer donne exactement les mêmes fichiers.

Pourquoi les versions précédentes (synth_sounds7.py : bow_shot, bow_shot_v2, bow_shot_charged, crossbow_shot)
sonnaient synthétiques : corde de Karplus-Strong qui tient une note propre 0,4 à 0,8 s (une vraie corde est étouffée
par les branches en moins de 0,15 s), sans chute de hauteur ni claquement ; « souffle » en bruit filtré dont la bande
MONTE (un balayage, alors qu'une flèche qui s'éloigne descend et s'éteint) ; coup sourd en sinus qui chute (une
grosse caisse) ; résonance du bois en bruit très résonant (une clave) ; tout démarre au même instant ; et une
réverbération de Schroeder qui ajoute 0,3 à 0,9 s de queue métallique.

Ici chaque source est modélisée et placée dans le temps comme dans la réalité :
- corde : guide d'onde (ligne à retard à lecture fractionnaire) excité par la forme triangulaire de la corde tirée,
  pertes par période et filtre passe-bas dans la boucle (amortissement rapide réglé en T60), hauteur qui retombe
  pendant que les branches se stabilisent, saturation dépendant de l'amplitude (claquement à l'attaque, puis corde
  propre) ;
- bois des branches, crosse, métal de la noix et de la détente : synthèse modale (résonateurs à deux pôles, chacun
  une fréquence et un T60) excitée par une impulsion dont la largeur fixe la dureté du choc (métal 0,1 ms, bois 0,5 ms,
  cuir 2 ms) ;
- protège-bras : bref claquement de cuir (bruit large bande très court + un mode grave) ;
- départ du projectile : bruit filtré dont la bande descend (effet Doppler), atténué comme 1 / distance et de plus
  en plus sourd (absorption de l'air), avec le battement de l'empennage ;
- grincements (bander l'arc, recharger l'arbalète) : frottement « colle-glisse », train d'impulsions irrégulier dont
  la cadence monte avec la tension, qui excite les modes du bois.
Aucune réverbération : les sons sont secs, l'espace viendra du jeu.

Usage : python -B synth_physique.py <dossier_sortie> [nom ...]   (sans nom : tout le catalogue ; -B évite un
__pycache__ dans Assets/). Variantes écrites <nom>_1.wav, <nom>_2.wav...
"""
import math
import os
import random
import sys

import synth_sounds2 as base
from synth_sounds2 import RATE, zeros, mix, white, bandpass, lowpass


# ------------------------------------------------------------------ briques physiques

def highpass(sig, cutoff):
    low = lowpass(sig, cutoff)
    return [s - l for s, l in zip(sig, low)]


def resonator(x, freq, t60, gain=1.0):
    """Mode amorti : filtre à deux pôles réglé par sa fréquence et son temps de décroissance à -60 dB (T60).
    Une impulsion unité donne une sinusoïde amortie d'amplitude `gain`."""
    r = 10 ** (-3.0 / (t60 * RATE))
    w = 2 * math.pi * freq / RATE
    a1, a2, b0 = 2 * r * math.cos(w), -r * r, math.sin(w) * gain
    y1 = y2 = 0.0
    out = [0.0] * len(x)
    for i, s in enumerate(x):
        y = b0 * s + a1 * y1 + a2 * y2
        y2, y1 = y1, y
        out[i] = y
    return out


def modal(x, modes, spread=0.0):
    """Corps vibrant : somme de modes (fréquence, T60, amplitude), fréquences décalées au hasard de ±spread."""
    out = [0.0] * len(x)
    for f, t60, amp in modes:
        f *= 1 + random.uniform(-spread, spread)
        for i, s in enumerate(resonator(x, f, t60, amp)):
            out[i] += s
    return out


def strike(width, seconds, grit=0.3, unit=True):
    """Excitation d'un choc : demi-sinusoïde de largeur `width` (plus elle est courte, plus le choc est dur), un peu
    rugueuse (`grit`), d'aire unité (sauf `unit=False` : bouffée brute), dans un signal de `seconds` secondes."""
    buf = zeros(seconds)
    n = max(2, int(width * RATE))
    for i in range(min(n, len(buf))):
        buf[i] = math.sin(math.pi * i / n) * (1 - grit + grit * random.uniform(-1, 1))
    if not unit:
        return buf  # bouffée brute (frottement, cuir), à filtrer
    area = sum(abs(s) for s in buf) or 1.0
    return [s / area for s in buf]  # choc d'aire unité : le gain d'un mode est son amplitude


def string(f0, seconds, t60, bright=0.5, glide=0.12, glide_time=0.03, drive=2.5, pos=0.5, grit=0.05):
    """Corde lâchée (guide d'onde) : forme triangulaire de la corde tirée (pincée en `pos`) envoyée dans une ligne à
    retard bouclée ; pertes réglées pour un T60 donné, passe-bas dans la boucle (`bright` de 0 à 1 : corde sourde ou
    claire) ; hauteur qui part de f0 * (1 + glide) et retombe sur f0 en `glide_time` s ; saturation dépendant de
    l'amplitude (`drive`) : la corde claque fort à l'attaque puis vibre proprement en s'éteignant."""
    n = int(seconds * RATE)
    size = int(RATE / f0) + 8
    line = [0.0] * size
    period = int(RATE / (f0 * (1 + glide)))
    apex = max(1, int(pos * period))
    exc = [(i / apex if i < apex else (period - i) / max(1, period - apex)) for i in range(period)]
    mean = sum(exc) / period
    exc = [e - mean + grit * random.uniform(-1, 1) for e in exc]
    loss = 10 ** (-3.0 / (t60 * f0))
    out = [0.0] * n
    state = 0.0
    for i in range(n):
        f = f0 * (1 + glide * math.exp(-i / (glide_time * RATE)))
        delay = RATE / f - 0.5 * (1 - bright) / max(bright, 0.05)  # le passe-bas retarde un peu : compensation
        pos_r = i - max(2.0, delay)
        j = math.floor(pos_r)
        frac = pos_r - j
        s = line[j % size] * (1 - frac) + line[(j + 1) % size] * frac
        state += bright * (s - state)
        v = loss * state + (exc[i] if i < period else 0.0)
        line[i % size] = v
        out[i] = v
    peak = max(1e-9, max(abs(s) for s in out))
    norm = math.tanh(drive)
    return [math.tanh(drive * s / peak) / norm for s in out]


def recede(seconds, f_start, f_end, q, speed, attack=0.004, air=6000.0, flutter=(0.0, 50.0)):
    """Projectile qui part : bruit filtré dont la bande descend (Doppler), amplitude en 1 / distance (le projectile
    part à `speed` m/s d'un mètre de l'oreille), aigus de plus en plus absorbés, battement de l'empennage
    (`flutter` : profondeur, fréquence)."""
    n = int(seconds * RATE)
    sig = bandpass(white(n), lambda k: f_start * (f_end / f_start) ** (k ** 0.6), q)
    depth, rate = flutter
    out = [0.0] * n
    y = 0.0
    for i, s in enumerate(sig):
        t = i / RATE
        dist = 1.0 + speed * t
        cutoff = air / dist ** 0.5
        y += (1 - math.exp(-2 * math.pi * cutoff / RATE)) * (s - y)
        out[i] = y * min(1.0, t / attack) / dist * (1 + depth * math.sin(2 * math.pi * rate * t))
    return out


def stick_slip(seconds, rate_of, amp_of, jitter=0.45):
    """Frottement colle-glisse (bois qui se tend, corde qui glisse, cliquet) : train d'impulsions irrégulier dont la
    cadence rate_of(k) et la force amp_of(k) suivent la progression k de 0 à 1. À passer dans `modal`."""
    buf = zeros(seconds)
    t = 0.0
    while True:
        k = t / seconds
        if k >= 1:
            break
        i = int(t * RATE)
        buf[i] += amp_of(k) * random.uniform(0.5, 1.0) * random.choice((-1, 1))
        t += (1 + random.uniform(-jitter, jitter)) / max(1e-3, rate_of(k))
    return buf


def rustle(seconds, cutoff, env):
    """Froissement (tissu, cuir) : bruit sourd modulé par de petites bouffées au hasard."""
    n = int(seconds * RATE)
    sig = lowpass(highpass(white(n), 300), cutoff)
    gust = lowpass([random.uniform(0, 1) ** 3 for _ in range(n)], 25)
    m = max(gust) or 1
    return [s * g / m * env(i / max(1, n - 1)) for i, (s, g) in enumerate(zip(sig, gust))]


def finish(buf, seconds, drive=1.0):
    """Mastering commun : buffer ramené à `seconds`, grave inutile coupé (< 40 Hz), crêtes arrondies (tanh léger, pour
    un niveau perçu proche des autres sons du projet), fondu de sortie. La normalisation (crête à 0,8) est faite
    par base.write, comme pour tous les sons du projet."""
    buf = highpass(buf[:int(seconds * RATE)] + [0.0] * max(0, int(seconds * RATE) - len(buf)), 40)
    peak = max(1e-9, max(abs(s) for s in buf))
    buf = [math.tanh(drive * s / peak) for s in buf]
    fade = int(0.04 * RATE)
    for i in range(fade):
        buf[-1 - i] *= i / fade
    return buf


def loudness_db(buf):
    """Volume perçu approché : RMS maximal sur 50 ms (fenêtres glissantes au quart), après un passe-haut à 150 Hz."""
    y, hp = 0.0, []
    a = 1 - math.exp(-2 * math.pi * 150 / RATE)
    for x in buf:
        y += a * (x - y)
        hp.append(x - y)
    w = int(0.05 * RATE)
    best = 0.0
    for i in range(0, max(1, len(hp) - w), w // 4):
        seg = hp[i:i + w]
        best = max(best, sum(s * s for s in seg) / len(seg))
    return 10 * math.log10(max(best, 1e-12))


LOUDNESS_DB = -14.0  # médiane des sons du projet (-12 à -19 dB mesurés le 25/09/2026, même mesure)


def level_peak(buf, target_db=LOUDNESS_DB, peak=0.8):
    """Crête d'écriture pour base.write : 0,8 comme tous les sons du projet, abaissée si le son, normalisé à 0,8,
    serait plus fort que `target_db` (les sons très denses sonneraient plus fort que les autres à crête égale)."""
    m = max(1e-9, max(abs(s) for s in buf))
    level = loudness_db([s / m * peak for s in buf])
    return peak * 10 ** (min(0.0, target_db - level) / 20)


# ------------------------------------------------------------------ arc

BOW_LIMBS = ((72, 0.06, 0.5), (205, 0.045, 0.8), (470, 0.03, 0.5), (860, 0.02, 0.35), (1420, 0.012, 0.2), (2350, 0.008, 0.1))


def _bow(f0, power, whoosh_len, speed):
    d = 0.5
    buf = zeros(d)
    release = 0.0
    # La corde glisse du gant ou des doigts : petit frottement sec.
    mix(buf, highpass(strike(0.0015, 0.02, 0.9, False), 1500), release, 0.08)
    # La corde revient en ~12 ms et s'arrête à la hauteur de bracing : claquement de la corde...
    brace = release + random.uniform(0.010, 0.014)
    twang = string(f0, 0.3, t60=random.uniform(0.2, 0.25) * (1 + 0.3 * (power - 1)), bright=0.55,
                   glide=0.10 + 0.08 * (power - 1), glide_time=0.025, drive=2.2 + 1.5 * (power - 1), pos=0.5)
    mix(buf, lowpass(highpass(twang, 70), 3800), brace, 0.8 * power)
    # ... et choc transmis aux branches et à la poignée : résonance boisée sourde, très courte.
    limbs = modal(strike(0.0006, 0.2, 0.4), BOW_LIMBS, spread=0.06)
    mix(buf, limbs, brace, 0.35 * power)
    # La corde frappe le protège-bras (cuir) : claquement bref, pas toujours de la même force.
    guard = random.uniform(0.35, 0.75)
    slap = bandpass(strike(0.0025, 0.03, 0.95, False), lambda k: 2100, 0.8)
    slap = [s + m for s, m in zip(slap, modal(strike(0.002, 0.03, 0.5), ((260, 0.02, 0.6),)))]
    mix(buf, slap, brace + random.uniform(0.006, 0.012), guard * power)
    # L'empennage frôle le repose-flèche, puis la flèche file et s'éloigne.
    mix(buf, bandpass(strike(0.012, 0.02, 1.0, False), lambda k: 3200, 1.4), brace + 0.004, 0.18)
    air = recede(whoosh_len, random.uniform(3600, 4400), 1500, 0.9, speed, attack=0.006, air=7000,
                 flutter=(0.18, random.uniform(40, 55)))
    mix(buf, air, brace + 0.003, 0.9 * power)
    return finish(buf, d)


def bow_shot_v3():
    # Tir d'arc : corde 105-125 Hz, souffle court.
    return _bow(random.uniform(105, 125), random.uniform(0.92, 1.05), random.uniform(0.26, 0.32), 55)


def bow_shot_v3_charged():
    # Arc bandé à fond : corde plus tendue (plus haute), claquement plus fort, flèche plus rapide au souffle plus long.
    return _bow(random.uniform(140, 150), 1.35, 0.42, 75)


def bow_draw():
    # Bander l'arc (0,9 s) : froissement de la manche, flèche qui glisse sur le repose-flèche, grincement des
    # branches dont la cadence et la force montent avec la tension, fibres de la corde, puis la corde touche l'ancrage.
    d = 0.9
    buf = zeros(d)
    mix(buf, rustle(0.35, 1800, lambda k: math.sin(math.pi * k) ** 1.5), 0.0, 0.25)
    creak_upper = stick_slip(0.78, lambda k: 14 + 55 * k ** 1.3, lambda k: 0.25 + 0.75 * k ** 1.5)
    mix(buf, modal(creak_upper, ((330, 0.03, 1.0), (620, 0.025, 0.7), (1180, 0.018, 0.45), (1950, 0.012, 0.25),
                                 (3000, 0.007, 0.12)), spread=0.03), 0.06, 0.9)
    creak_lower = stick_slip(0.7, lambda k: 10 + 40 * k ** 1.5, lambda k: 0.15 + 0.6 * k ** 2)
    mix(buf, modal(creak_lower, ((290, 0.03, 1.0), (545, 0.025, 0.7), (1060, 0.018, 0.4), (1750, 0.01, 0.2)),
                   spread=0.03), 0.12, 0.6)
    fibres = stick_slip(0.7, lambda k: 30 + 90 * k, lambda k: 0.1 + 0.4 * k)
    mix(buf, modal(fibres, ((2600, 0.006, 0.6), (3900, 0.005, 0.4), (5200, 0.004, 0.25))), 0.1, 0.35)
    scrape = bandpass(white(int(0.6 * RATE)), lambda k: 2200 + 400 * k, 2.0)
    mix(buf, [s * math.sin(math.pi * min(1, i / (0.6 * RATE))) for i, s in enumerate(scrape)], 0.05, 0.05)
    mix(buf, modal(strike(0.001, 0.05, 0.5), ((880, 0.012, 0.6), (1900, 0.008, 0.3))), 0.82, 0.35)
    return finish(buf, d)


# ------------------------------------------------------------------ arbalète

METAL_TRIGGER = ((2350, 0.03, 0.6), (3710, 0.025, 0.5), (5480, 0.018, 0.4), (7900, 0.012, 0.25))
METAL_NUT = ((1650, 0.035, 0.7), (2890, 0.03, 0.55), (4420, 0.02, 0.4), (6300, 0.015, 0.25))
STOCK = ((95, 0.07, 1.0), (170, 0.05, 0.8), (310, 0.04, 0.6), (560, 0.03, 0.45), (980, 0.02, 0.3), (1700, 0.012, 0.2))
STEEL_PROD = ((420, 0.08, 0.5), (1130, 0.06, 0.35), (2150, 0.04, 0.2))


def crossbow_shot_v3():
    # Tir d'arbalète : détente puis noix (clics métalliques secs), corde épaisse et très tendue qui claque plus haut et
    # s'étouffe plus vite que l'arc, branches qui frappent la butée (choc lourd et sec dans la crosse), carreau qui
    # siffle et s'éloigne vite.
    d = 0.4
    buf = zeros(d)
    mix(buf, modal(strike(0.0001, 0.08, 0.2), METAL_TRIGGER, spread=0.04), 0.0, 0.45)
    nut = random.uniform(0.004, 0.006)
    mix(buf, modal(strike(0.0001, 0.08, 0.2), METAL_NUT, spread=0.04), nut, 0.6)
    stop = nut + random.uniform(0.005, 0.007)
    twang = string(random.uniform(185, 215), 0.2, t60=random.uniform(0.09, 0.11), bright=0.65, glide=0.07,
                   glide_time=0.012, drive=4.0, pos=0.5)
    mix(buf, lowpass(highpass(twang, 90), 4500), stop, 0.6)
    mix(buf, modal(strike(0.0004, 0.2, 0.5), STOCK, spread=0.05), stop, 0.45)
    mix(buf, modal(strike(0.0002, 0.2, 0.3), STEEL_PROD, spread=0.03), stop, 0.2)
    mix(buf, highpass(strike(0.0008, 0.01, 1.0, False), 2000), stop, 0.35)
    bolt = recede(0.2, random.uniform(6200, 6800), 3800, 2.5, 90, attack=0.002, air=9000, flutter=(0.1, 70))
    mix(buf, bolt, stop + 0.002, 0.8)
    hiss = recede(0.15, 5000, 2600, 0.8, 90, attack=0.002, air=9000)
    mix(buf, hiss, stop + 0.002, 0.3)
    return finish(buf, d)


def crossbow_reload():
    # Recharger l'arbalète (1,15 s) : main sur le levier, crochet qui prend la corde, traction au levier (cliquet
    # qui saute les dents, crosse et corde qui grincent sous la tension), corde qui tombe dans la noix (clic
    # d'armement), levier relâché.
    d = 1.15
    buf = zeros(d)
    mix(buf, rustle(0.2, 2200, lambda k: math.sin(math.pi * k)), 0.0, 0.3)
    mix(buf, modal(strike(0.0002, 0.06, 0.3), ((1900, 0.02, 0.6), (3300, 0.015, 0.4), (5100, 0.01, 0.2))), 0.12, 0.35)
    t, teeth = 0.18, 9
    for n in range(teeth):
        k = n / (teeth - 1)
        pawl = ((2600, 0.02, 0.6), (4100, 0.015, 0.45), (6200, 0.01, 0.3))
        mix(buf, modal(strike(0.00008, 0.04, 0.2), pawl, spread=0.03), t, 0.3 + 0.25 * k)
        t += random.uniform(0.066, 0.078) * (1 - 0.15 * k)
    creak = stick_slip(0.68, lambda k: 12 + 30 * k, lambda k: 0.3 + 0.7 * k)
    mix(buf, modal(creak, ((240, 0.035, 1.0), (470, 0.028, 0.7), (910, 0.02, 0.45), (1600, 0.012, 0.25)),
                   spread=0.03), 0.17, 0.9)
    cord = stick_slip(0.65, lambda k: 40 + 60 * k, lambda k: 0.1 + 0.3 * k)
    mix(buf, modal(cord, ((1500, 0.008, 0.5), (2700, 0.006, 0.35))), 0.18, 0.3)
    arm = t + 0.03
    mix(buf, modal(strike(0.0001, 0.12, 0.2), ((1400, 0.045, 0.9), (2620, 0.035, 0.7), (4150, 0.025, 0.5),
                                                (6200, 0.015, 0.3)), spread=0.02), arm, 0.55)
    mix(buf, modal(strike(0.0005, 0.15, 0.4), ((140, 0.05, 1.0), (300, 0.04, 0.7), (620, 0.03, 0.4))), arm, 0.35)
    mix(buf, modal(strike(0.0001, 0.06, 0.2), METAL_NUT, spread=0.04), arm + 0.006, 0.4)
    for n in range(3):
        mix(buf, modal(strike(0.0001, 0.04, 0.2), ((3100, 0.012, 0.5), (4700, 0.01, 0.3)), spread=0.08),
            arm + 0.09 + n * random.uniform(0.02, 0.035), 0.15 - 0.04 * n)
    return finish(buf, d)


# ------------------------------------------------------------------ catalogue

CATALOG = {
    # nom : (fonction, nombre de variantes, graine) — graines à la suite de synth_sounds7.py (701 à 771)
    "bow_shot_v3": (bow_shot_v3, 3, 772),
    "bow_shot_v3_charged": (bow_shot_v3_charged, 1, 773),
    "bow_draw": (bow_draw, 1, 774),
    "crossbow_shot_v3": (crossbow_shot_v3, 3, 775),
    "crossbow_reload": (crossbow_reload, 1, 776),
}
LOUD = {"bow_shot_v3_charged": -12.5}  # volume perçu visé quand il diffère de LOUDNESS_DB (tir chargé : plus fort)


def render(name, variant, out):
    fn, count, seed = CATALOG[name]
    random.seed(seed * 10 + variant)
    file = name if count == 1 else "%s_%d" % (name, variant + 1)
    buf = fn()
    base.write(os.path.join(out, file + ".wav"), buf, level_peak(buf, LOUD.get(name, LOUDNESS_DB)))
    return file


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    names = sys.argv[2:] or list(CATALOG)
    os.makedirs(out, exist_ok=True)
    for n in names:
        for v in range(CATALOG[n][1]):
            render(n, v, out)
