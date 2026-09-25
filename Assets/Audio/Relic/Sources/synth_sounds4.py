"""Sons des projectiles, deuxième essai (« retente »). Synthèse vocale plus réaliste (impulsions glottiques de
Rosenberg, résonateurs de formants en cascade à la Klatt, souffle mêlé à la voix, raucité par dédoublement de période),
souffles inversés, bruit brun pour le feu. Usage : python synth_sounds4.py <dossier_sortie>
"""
import math
import os
import random
import sys

import synth_sounds2 as base
import synth_sounds3 as prev
from synth_sounds2 import RATE, zeros, mix, white, bandpass, lowpass, envelope, tone, reverb, grains

# Formants (F1..F4 en Hz, largeurs de bande en Hz).
FORMANTS = {
    "ou": ((320, 60), (800, 80), (2400, 140), (3300, 200)),
    "o": ((480, 70), (880, 90), (2500, 150), (3400, 200)),
    "a": ((850, 90), (1250, 110), (2700, 160), (3600, 220)),
    "e": ((600, 70), (1700, 100), (2600, 150), (3500, 200)),
}


def blend(a, b, m):
    return tuple((fa + (fb - fa) * m, ba + (bb - ba) * m) for (fa, ba), (fb, bb) in zip(FORMANTS[a], FORMANTS[b]))


def voice(freq_of, vowel_of, seconds, breath=0.25, rough=0.0, jitter=0.012, vib=(5.0, 0.015), subharmonic=0.0):
    """Voix : impulsions glottiques de Rosenberg (ouverture 60 %), souffle, instabilités, puis formants en cascade.
    vowel_of(k) -> (voyelle A, voyelle B, mélange). rough : modulation d'amplitude rapide (raucité) ; subharmonic :
    une période sur deux affaiblie (voix cassée, cri)."""
    n = int(seconds * RATE)
    src = [0.0] * n
    phase = 0.0
    drift = 0.0
    period_count = 0
    vib_rate, vib_depth = vib
    for i in range(n):
        k = i / max(1, n - 1)
        t = i / RATE
        if i % 300 == 0:
            drift = drift * 0.8 + random.uniform(-jitter, jitter)
        f = freq_of(k) * (1 + vib_depth * math.sin(2 * math.pi * vib_rate * t) + drift)
        phase += f / RATE
        if phase >= 1.0:
            phase -= 1.0
            period_count += 1
        # Impulsion de Rosenberg : montée en demi-cosinus, retombée rapide, puis glotte fermée.
        oq = 0.6
        if phase < oq * 0.66:
            g = 0.5 * (1 - math.cos(math.pi * phase / (oq * 0.66)))
        elif phase < oq:
            g = math.cos(0.5 * math.pi * (phase - oq * 0.66) / (oq * 0.34))
        else:
            g = 0.0
        if subharmonic and period_count % 2:
            g *= 1 - subharmonic
        if rough:
            g *= 1 - rough * (0.5 + 0.5 * math.sin(2 * math.pi * 47 * t + math.sin(t * 13)))
        src[i] = g
    # Dérivée (la bouche rayonne la dérivée du débit), plus le souffle qui passe par les mêmes formants.
    d = [src[i] - src[i - 1] if i else 0.0 for i in range(n)]
    peak = max(1e-9, max(abs(x) for x in d))
    sig = [x / peak + breath * random.uniform(-1, 1) * (0.3 + 0.7 * src[i]) for i, x in enumerate(d)]
    # Résonateurs de Klatt en cascade (F1 -> F4), fréquences mises à jour toutes les 32 échantillons.
    for slot in range(4):
        y1 = y2 = 0.0
        out = [0.0] * n
        for i in range(n):
            if i % 32 == 0:
                a, b, m = vowel_of(i / max(1, n - 1))
                F, BW = blend(a, b, m)[slot]
                C = -math.exp(-2 * math.pi * BW / RATE)
                B = 2 * math.exp(-math.pi * BW / RATE) * math.cos(2 * math.pi * F / RATE)
                A = 1 - B - C
            y = A * sig[i] + B * y1 + C * y2
            y2, y1 = y1, y
            out[i] = y
        sig = out
    return sig


def brown(n, leak=0.02):
    """Bruit brun : bruit blanc intégré (grave et grondant comme le feu ou le vent)."""
    y = 0.0
    out = []
    for _ in range(n):
        y = y * (1 - leak) + random.uniform(-1, 1) * 0.1
        out.append(y)
    peak = max(1e-9, max(abs(x) for x in out))
    return [x / peak for x in out]


def reverse_swell(seconds, low, high):
    """Souffle inversé : un bruit filtré passé dans la réverbération puis retourné (aspiration spectrale)."""
    burst = bandpass(white(int(0.15 * RATE)), lambda k: (low + high) / 2, 1.2)
    wet = reverb(envelope(burst, lambda t, k: 1 - k), size=1.6, wet=1.0, tail=seconds)
    wet = wet[:int(seconds * RATE)]
    wet.reverse()
    return wet


# ------------------------------------------------------------------ missile magique

def limbo_voices():
    """Voix des limbes : plusieurs âmes qui gémissent, chacune entre et sort, glisse sur plusieurs demi-tons entre
    « ou » et « o » ; souffles inversés ; bourdon grave."""
    d = 4.0
    buf = zeros(d)
    for _ in range(7):
        start = random.uniform(0.0, d * 0.55)
        length = random.uniform(1.4, 2.4)
        f0 = random.choice([98, 110, 116.5, 130.8, 146.8, 155.6, 196])
        bend = random.choice([-1, 1]) * random.uniform(2, 5)  # demi-tons de glissade (gémissement)
        shape = random.random()
        v = voice(lambda k, f0=f0, b=bend, s=shape: f0 * 2 ** ((b * math.sin(math.pi * (k * 0.8 + s * 0.2))) / 12),
                  lambda k, s=shape: ("ou", "o", 0.5 + 0.5 * math.sin(k * 5 + s * 6)),
                  length, breath=0.45, jitter=0.02, vib=(random.uniform(4, 6.5), 0.02))
        v = envelope(v, lambda t, k: math.sin(math.pi * k) ** 1.5)
        mix(buf, v, start, random.uniform(0.5, 1.0))
    for at in (0.2, 1.7, 2.9):
        mix(buf, reverse_swell(1.1, 400, 1400), at, 0.5)
    mix(buf, tone(lambda k: 55, d, lambda t, k: 0.6 + 0.4 * math.sin(2 * math.pi * 0.4 * t), ((1.0, 1.0), (1.5, 0.3), (2.01, 0.25))), 0.0, 0.25)
    buf = reverb(buf, size=1.7, wet=0.6, tail=0.0)
    return prev.loopable(buf, 0.8)


def limbo_scream():
    """Cri : une aspiration inversée, puis un hurlement rauque et cassé qui monte, tremble et s'effondre, doublé plus
    grave ; le crâne qui éclate dessous ; longue queue de réverbération."""
    d = 1.6
    buf = zeros(d)
    mix(buf, reverse_swell(0.35, 800, 2500), 0.0, 0.7)

    def contour(k):
        if k < 0.08:
            return 280 + 620 * (k / 0.08)
        if k < 0.4:
            return 900 + 70 * math.sin(k * 55)
        return 900 * (1 - 0.7 * ((k - 0.4) / 0.6) ** 1.2)

    scream_len = 1.1
    for octave, gain, sub in ((1.0, 1.0, 0.45), (0.5, 0.6, 0.6), (1.5, 0.25, 0.3)):
        v = voice(lambda k, o=octave: contour(k) * o, lambda k: ("a", "o", max(0.0, (k - 0.55) * 2.2)) if k > 0.55 else ("e", "a", min(1.0, k * 6)),
                  scream_len, breath=0.55, rough=0.5, jitter=0.04, vib=(11, 0.03), subharmonic=sub)
        v = prev.saturate(v, 1.8)
        v = envelope(v, lambda t, k: min(1.0, t * 30) * (1 - k) ** 1.2)
        mix(buf, v, 0.3, gain)
    crack = bandpass(white(int(0.06 * RATE)), lambda k: 3000 - 1600 * k, 1.2)
    mix(buf, envelope(crack, lambda t, k: math.exp(-5 * k)), 0.3, 1.1)
    mix(buf, tone(lambda k: 90 * (1 - 0.5 * k), 0.5, lambda t, k: math.exp(-7 * t), ((1.0, 1.0), (1.6, 0.4))), 0.3, 0.8)
    mix(buf, grains(0.9, 16, (700, 2000), (0.01, 0.03), (0.1, 0.3), q=9, spread=lambda: random.random() ** 2 * 0.6), 0.3, 1.0)
    return reverb(buf, size=1.6, wet=0.5, tail=1.6)


# ------------------------------------------------------------------ boule de feu

def fire_crackle(seconds, rate_per_s, loud=(0.2, 0.8)):
    """Braise : pops secs et irréguliers (clics de hauteurs variées), plus serrés par moments."""
    buf = zeros(seconds)
    t = 0.0
    while t < seconds - 0.02:
        t += random.expovariate(rate_per_s)
        f = random.uniform(1500, 6500)
        d = random.uniform(0.001, 0.005)
        pop = bandpass(white(int(d * RATE) + 8), lambda k, f=f: f, 3)
        pop = envelope(pop, lambda tt, k: math.exp(-5 * k))
        mix(buf, pop, min(t, seconds - 0.02), random.uniform(*loud) * (1.0 if random.random() > 0.15 else 2.2))
    return buf


def fire_cast():
    d = 1.0
    buf = zeros(d)
    n = int(d * RATE)
    roar = bandpass(brown(n, 0.01), lambda k: 150 + 900 * math.sin(math.pi * min(1, k * 1.5)), 0.6)
    mix(buf, envelope(roar, lambda t, k: min(1.0, t * 15) * (1 - k) ** 1.4), 0.0, 1.0)
    whoosh = bandpass(white(n), lambda k: 400 + 2600 * math.sin(math.pi * min(1, k * 1.6)), 1.2)
    mix(buf, envelope(whoosh, lambda t, k: min(1.0, t * 10) * (1 - k) ** 2), 0.0, 0.35)
    mix(buf, fire_crackle(d, 25), 0.0, 0.5)
    return reverb(buf, size=0.9, wet=0.18, tail=0.4)


def fire_flight():
    d = 3.0
    n = int(d * RATE)
    roar = lowpass(brown(n, 0.015), 900)
    phases = [(random.uniform(2, 4), random.random() * 6.28), (random.uniform(6, 11), random.random() * 6.28),
              (random.uniform(15, 22), random.random() * 6.28)]
    roar = envelope(roar, lambda t, k: 0.6 + sum(0.15 * math.sin(2 * math.pi * r * t + p) for r, p in phases))
    buf = [s * 1.2 for s in roar]
    hiss = bandpass(white(n), lambda k: 2500, 0.5)
    mix(buf, envelope(hiss, lambda t, k: 0.5 + 0.5 * math.sin(2 * math.pi * 3.1 * t) ** 2), 0.0, 0.08)
    mix(buf, fire_crackle(d, 35, (0.1, 0.45)), 0.0, 1.0)
    return prev.loopable(buf, 0.5)


def fire_explosion():
    d = 2.4
    buf = zeros(d)
    n = int(1.9 * RATE)
    # Claquement initial (front d'onde), puis masse d'air grave qui roule.
    mix(buf, envelope(white(int(0.02 * RATE)), lambda t, k: 1 - k), 0.0, 0.8)
    body = lowpass(brown(n, 0.004), 500)
    mix(buf, envelope(body, lambda t, k: min(1.0, t * 400) * math.exp(-2.4 * t)), 0.0, 2.2)
    blast = bandpass(white(n), lambda k: 3000 * (1 - k) ** 3 + 200, 0.6)
    mix(buf, envelope(blast, lambda t, k: min(1.0, t * 400) * math.exp(-4.5 * t)), 0.0, 1.0)
    mix(buf, tone(lambda k: 48 * (1 - 0.35 * k), 1.4, lambda t, k: math.exp(-3 * t) * min(1, t * 500), ((1.0, 1.0), (2.0, 0.2))), 0.0, 0.9)
    # Flammes qui retombent et braises qui crépitent.
    mix(buf, envelope(fire_crackle(1.8, 60, (0.15, 0.6)), lambda t, k: (1 - k) ** 1.5), 0.05, 1.0)
    return reverb(buf, size=1.5, wet=0.35, tail=1.3)


if __name__ == "__main__":
    random.seed(31)
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(out, exist_ok=True)
    prev.write_raw(os.path.join(out, "missile_vol_voix_limbes_v2.wav"), limbo_voices())
    base.write(os.path.join(out, "missile_explosion_cri_v2.wav"), limbo_scream())
    base.write(os.path.join(out, "boule_feu_lancer_v2.wav"), fire_cast())
    prev.write_raw(os.path.join(out, "boule_feu_vol_v2.wav"), fire_flight())
    base.write(os.path.join(out, "boule_feu_explosion_v2.wav"), fire_explosion())
