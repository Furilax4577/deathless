"""Vol B « murmures » sans bruits parasites (Quentin) : plus aucune consonne (ni claquements « t », « k », « p », ni
sifflantes), seulement des voyelles soufflées qui glissent, sur le chœur grave. Deux versions : murmures présents
(fichier 1), chœur presque seul (fichier 2). Usage : python synth_b_fix2.py <sortie1.wav> <sortie2.wav>"""
import math
import random
import sys

import synth_sounds3 as prev
import synth_sounds5 as v5
from synth_sounds2 import RATE, zeros, mix, white, bandpass, lowpass, envelope, tone, reverb, grains


def soft_word(seconds):
    """Un « mot » de voyelles soufflées enchaînées, sans consonne : la bouche change de forme, rien ne claque."""
    buf = zeros(seconds)
    t = 0.0
    vowels = ["ou", "o", "a", "e"]
    current = random.choice(vowels)
    while t < seconds - 0.25:
        nxt = random.choice(vowels)
        length = random.uniform(0.14, 0.3)
        v = envelope(v5.whisper_vowel(length, current, nxt), lambda tt, k: math.sin(math.pi * k) ** 1.2)
        mix(buf, v, t, 0.4)
        t += length * 0.85
        current = nxt
    return buf


def murmurs(choir_only):
    d = 4.0
    buf = zeros(d)
    if not choir_only:
        for _ in range(7):
            at = random.uniform(0, d - 1.0)
            mix(buf, soft_word(random.uniform(0.7, 1.4)), at, random.uniform(0.35, 0.8))
    else:
        for _ in range(3):
            mix(buf, soft_word(random.uniform(0.8, 1.4)), random.uniform(0, d - 1.2), 0.2)
    import synth_sounds4 as v4
    for f0 in (82.4, 98, 123.5):
        v = v4.voice(lambda k, f=f0: f * (1 + 0.01 * math.sin(k * 7)), lambda k: ("ou", "o", 0.5 + 0.5 * math.sin(k * 4)), d,
                     breath=0.5, jitter=0.01)
        mix(buf, envelope(v, lambda t, k: 0.6 + 0.4 * math.sin(2 * math.pi * 0.3 * t)), 0.0, 0.18 if not choir_only else 0.3)
    buf = lowpass(lowpass(buf, 4200), 5200)
    buf = reverb(buf, size=1.5, wet=0.5, tail=0.0)
    return prev.loopable(buf, 0.7)


random.seed(47)
prev.write_raw(sys.argv[1], murmurs(False))
random.seed(48)
prev.write_raw(sys.argv[2], murmurs(True))
