"""Missile magique, troisième essai : deux concepts pour le vol, deux pour l'explosion.
Vol A « complainte » : hurlements de fantôme à la thérémine (glissades d'une octave, vibrato large, voyelle « ou »).
Vol B « essaim de murmures » : une foule qui chuchote (souffle mis en forme de voyelles, consonnes « s », « ch », « t »),
sur un bourdon grave.
Explosion A « banshee » : aspiration, puis un cri suraigu qui plonge, sur un coup sourd.
Explosion B « râle » : un grognement guttural qui enfle et se brise, voix doublée en sous-harmonique.
Usage : python synth_sounds5.py <dossier_sortie>
"""
import math
import os
import random
import sys

import synth_sounds2 as base
import synth_sounds3 as prev
import synth_sounds4 as v4
from synth_sounds2 import RATE, zeros, mix, white, bandpass, lowpass, envelope, tone, reverb, grains


def theremin(freq_of, seconds, vib=(6.0, 0.035)):
    """Voix de thérémine : sinus et quelques harmoniques douces, vibrato large."""
    n = int(seconds * RATE)
    out = [0.0] * n
    ph = random.random() * 6.28
    for i in range(n):
        t = i / RATE
        f = freq_of(i / max(1, n - 1)) * (1 + vib[1] * math.sin(2 * math.pi * vib[0] * t))
        ph += 2 * math.pi * f / RATE
        out[i] = math.sin(ph) + 0.25 * math.sin(2 * ph) + 0.08 * math.sin(3 * ph)
    return out


def wail_flight():
    d = 4.2
    buf = zeros(d)
    for _ in range(5):
        start = random.uniform(0, d * 0.5)
        length = random.uniform(1.6, 2.6)
        low = random.uniform(180, 300)
        up = random.uniform(1.5, 2.1)
        shape = random.random()
        # « ouuuOOOuuu » : montée puis redescente, une voix sur deux commence par le haut.
        w = theremin(lambda k, l=low, u=up, s=shape: l * (1 + (u - 1) * math.sin(math.pi * (k if s > 0.5 else 1 - k * 0.7))), length,
                     vib=(random.uniform(5, 7), random.uniform(0.02, 0.04)))
        w = bandpass(w, lambda k: 700, 0.9)  # un peu de « bouche fermée »
        w = envelope(w, lambda t, k: math.sin(math.pi * k) ** 2)
        mix(buf, w, start, random.uniform(0.5, 1.0))
    # Souffle froid et bourdon lointain.
    mix(buf, envelope(bandpass(white(int(d * RATE)), lambda k: 900, 0.7), lambda t, k: 0.5 + 0.5 * math.sin(2 * math.pi * 0.7 * t)), 0.0, 0.12)
    mix(buf, tone(lambda k: 49, d, lambda t, k: 1.0, ((1.0, 1.0), (1.5, 0.35))), 0.0, 0.18)
    buf = reverb(buf, size=1.8, wet=0.6, tail=0.0)
    return prev.loopable(buf, 0.8)


def consonant(kind):
    """Petite consonne chuchotée : « s » (sifflement aigu), « ch » (plus grave), « t »/« k » (claquement bref)."""
    if kind == "s":
        return envelope(bandpass(white(int(0.12 * RATE)), lambda k: 6500, 2.0), lambda t, k: math.sin(math.pi * k))
    if kind == "ch":
        return envelope(bandpass(white(int(0.14 * RATE)), lambda k: 3200, 1.6), lambda t, k: math.sin(math.pi * k))
    return envelope(bandpass(white(int(0.015 * RATE)), lambda k: 2500, 1.0), lambda t, k: 1 - k)


def whisper_vowel(length, a, b):
    """Voyelle chuchotée : souffle pur (sans cordes vocales) passé dans les résonateurs de formants en cascade."""
    n = int(length * RATE)
    sig = white(n)
    for slot in range(4):
        y1 = y2 = 0.0
        out = [0.0] * n
        for i in range(n):
            if i % 32 == 0:
                F, BW = v4.blend(a, b, i / max(1, n - 1))[slot]
                C = -math.exp(-2 * math.pi * BW / RATE)
                B = 2 * math.exp(-math.pi * BW / RATE) * math.cos(2 * math.pi * F / RATE)
                A = 1 - B - C
            y = A * sig[i] + B * y1 + C * y2
            y2, y1 = y1, y
            out[i] = y
        sig = out
    peak = max(1e-9, max(abs(x) for x in sig))
    return [x / peak for x in sig]


def whisper_word(seconds):
    """Un « mot » chuchoté : voyelles soufflées (souffle filtré par les formants) séparées de consonnes."""
    buf = zeros(seconds)
    t = 0.0
    while t < seconds - 0.2:
        c = random.choice(["s", "ch", "t", "k", "s"])
        mix(buf, consonant(c), t, 0.6 if c in ("s", "ch") else 0.9)
        t += 0.06 if c in ("t", "k") else 0.1
        length = random.uniform(0.1, 0.25)
        vowel = random.choice(list(v4.FORMANTS.keys()))
        other = random.choice(list(v4.FORMANTS.keys()))
        breath = envelope(whisper_vowel(length, vowel, other), lambda tt, k: math.sin(math.pi * k) ** 0.7)
        mix(buf, breath, t, 0.35)
        t += length + random.uniform(0.02, 0.12)
    return buf


def whisper_flight():
    d = 4.0
    buf = zeros(d)
    for _ in range(9):
        at = random.uniform(0, d - 1.0)
        mix(buf, whisper_word(random.uniform(0.6, 1.2)), at, random.uniform(0.4, 1.0))
    # Chœur grave presque inaudible dessous, qui donne la profondeur.
    for f0 in (82.4, 98, 123.5):
        v = v4.voice(lambda k, f=f0: f * (1 + 0.01 * math.sin(k * 7)), lambda k: ("ou", "o", 0.5 + 0.5 * math.sin(k * 4)), d,
                     breath=0.5, jitter=0.01)
        mix(buf, envelope(v, lambda t, k: 0.6 + 0.4 * math.sin(2 * math.pi * 0.3 * t)), 0.0, 0.18)
    buf = reverb(buf, size=1.5, wet=0.5, tail=0.0)
    return prev.loopable(buf, 0.7)


def banshee_explosion():
    d = 2.0
    buf = zeros(d)
    mix(buf, v4.reverse_swell(0.4, 1500, 4000), 0.0, 0.6)

    def dive(k):
        return 1900 * (1 - 0.75 * k ** 0.8) if k > 0.06 else 1300 + 600 * k / 0.06

    for mult, gain in ((1.0, 1.0), (1.007, 0.7), (0.5, 0.35)):
        s = theremin(lambda k, m=mult: dive(k) * m, 1.2, vib=(9.0, 0.03))
        s = prev.saturate([x * 1.5 for x in s], 1.5)
        s = envelope(s, lambda t, k: min(1.0, t * 40) * (1 - k) ** 1.3)
        mix(buf, s, 0.38, gain)
    shriek = bandpass(white(int(1.0 * RATE)), lambda k: 3500 - 2000 * k, 2.5)
    mix(buf, envelope(shriek, lambda t, k: min(1.0, t * 40) * (1 - k) ** 2), 0.38, 0.3)
    mix(buf, tone(lambda k: 70 * (1 - 0.5 * k), 0.8, lambda t, k: math.exp(-5 * t) * min(1, t * 400), ((1.0, 1.0), (2.0, 0.2))), 0.38, 0.9)
    crack = bandpass(white(int(0.05 * RATE)), lambda k: 3000 - 1500 * k, 1.2)
    mix(buf, envelope(crack, lambda t, k: math.exp(-5 * k)), 0.38, 0.9)
    return reverb(buf, size=1.7, wet=0.5, tail=1.6)


def growl_explosion():
    d = 1.8
    buf = zeros(d)
    # Râle : voix très grave, cassée (sous-harmonique forte), raucité maximale, qui enfle puis se brise.
    for f0, gain in ((95, 1.0), (47.5, 0.7), (142, 0.3)):
        g = v4.voice(lambda k, f=f0: f * (1 + 0.25 * math.sin(math.pi * min(1, k * 1.3))), lambda k: ("o", "a", min(1, k * 2)), 1.1,
                     breath=0.7, rough=0.8, jitter=0.05, vib=(7, 0.02), subharmonic=0.7)
        g = prev.saturate(g, 2.5)
        g = envelope(g, lambda t, k: math.sin(math.pi * min(1, k * 1.15)) ** 0.8)
        mix(buf, g, 0.0, gain)
    # La brisure : craquement, coup sourd, débris et soupir qui s'échappe.
    at = 0.85
    crack = bandpass(white(int(0.06 * RATE)), lambda k: 2800 - 1500 * k, 1.1)
    mix(buf, envelope(crack, lambda t, k: math.exp(-5 * k)), at, 1.3)
    mix(buf, tone(lambda k: 60 * (1 - 0.5 * k), 0.7, lambda t, k: math.exp(-5 * t) * min(1, t * 400), ((1.0, 1.0), (1.6, 0.3))), at, 1.0)
    mix(buf, grains(0.8, 16, (700, 2000), (0.01, 0.03), (0.1, 0.3), q=9, spread=lambda: random.random() ** 2 * 0.6), at, 1.0)
    exhale = bandpass(white(int(0.8 * RATE)), lambda k: 800 - 400 * k, 2.0)
    mix(buf, envelope(exhale, lambda t, k: math.sin(math.pi * k) * (1 - k)), at + 0.05, 0.4)
    return reverb(buf, size=1.5, wet=0.45, tail=1.4)


if __name__ == "__main__":
    random.seed(47)
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(out, exist_ok=True)
    prev.write_raw(os.path.join(out, "missile_vol_A_complainte.wav"), wail_flight())
    prev.write_raw(os.path.join(out, "missile_vol_B_murmures.wav"), whisper_flight())
    base.write(os.path.join(out, "missile_explosion_A_banshee.wav"), banshee_explosion())
    base.write(os.path.join(out, "missile_explosion_B_rale.wav"), growl_explosion())
