"""Missile magique, quatrième essai (Quentin : « murmures en trajectoire et cri grave à l'explosion »).
Murmures : de vrais mots chuchotés (syllabes consonne + voyelle avec coarticulation), plusieurs chuchoteurs à des
distances différentes, fricatives réalistes (s, ch, f), occlusives (t, k, p), sifflements prolongés.
Cri grave : hurlement de gorge d'homme (fondamentale 120 -> 230 -> 80 Hz), raucité et voix cassée, doublé une octave
plus bas, saturé, sur le crâne qui éclate. Usage : python synth_sounds6.py <dossier_sortie>
"""
import math
import os
import random
import sys

import synth_sounds2 as base
import synth_sounds3 as prev
import synth_sounds4 as v4
import synth_sounds5 as v5
from synth_sounds2 import RATE, zeros, mix, white, bandpass, lowpass, envelope, tone, reverb, grains


def highpass(sig, cutoff):
    low = lowpass(sig, cutoff)
    return [s - l for s, l in zip(sig, low)]


def fricative(kind, length):
    n = int(length * RATE)
    if kind == "s":
        # « s » adouci : plus bas (4,5 kHz) et court ; les sifflements aigus répétés gâchaient la boucle (Quentin).
        sig = bandpass(white(n), lambda k: 4500, 1.8)
    elif kind == "h":
        # Attaque soufflée d'une voyelle (« h ») : souffle large et grave, sans sifflement.
        sig = [x * 0.6 for x in bandpass(white(n), lambda k: 1400, 0.7)]
    elif kind == "ch":
        sig = bandpass(white(n), lambda k: 3000, 1.5)
    else:  # « f » : large et faible
        sig = [x * 0.5 for x in highpass(white(n), 1500)]
    return envelope(sig, lambda t, k: math.sin(math.pi * k) ** 0.8)


def plosive(kind):
    centre = {"t": 4500, "k": 2200, "p": 900}[kind]
    burst = bandpass(white(int(0.02 * RATE)), lambda k: centre, 1.3)
    return envelope(burst, lambda t, k: (1 - k) ** 2)


def whispered_word(syllables):
    """Un mot chuchoté de `syllables` syllabes (consonne puis voyelle, la voyelle glisse vers la suivante)."""
    vowels = list(v4.FORMANTS.keys())
    buf = zeros(syllables * 0.35 + 0.3)
    t = 0.0
    current = random.choice(vowels)
    for s in range(syllables):
        # Surtout des attaques soufflées et des occlusives ; « s » et « ch » rares, courts et discrets.
        c = random.choices(["h", "k", "p", "t", "f", "ch", "s"], weights=[5, 3, 3, 2, 2, 1, 0.6])[0]
        if c in ("s", "ch", "f", "h"):
            length = random.uniform(0.04, 0.07) if c in ("s", "ch") else random.uniform(0.05, 0.1)
            mix(buf, fricative(c, length), t, {"s": 0.22, "ch": 0.28, "f": 0.35, "h": 0.5}[c])
            t += length * 0.8
        else:
            mix(buf, plosive(c), t, 0.9)
            t += 0.03
        nxt = random.choice(vowels)
        length = random.uniform(0.08, 0.17)
        vowel = envelope(v5.whisper_vowel(length, current, nxt), lambda tt, k: math.sin(math.pi * k) ** 0.6)
        mix(buf, vowel, t, 0.45 * (1.0 if s < syllables - 1 else 0.7))  # la dernière syllabe retombe
        t += length * 0.9
        current = nxt
    return buf[:int((t + 0.05) * RATE)]


def whisperer(seconds, distance):
    """Un chuchoteur : des mots séparés de courtes pauses ; plus il est loin, plus il est sourd et faible."""
    buf = zeros(seconds)
    t = random.uniform(0.0, 0.5)
    while t < seconds - 0.4:
        word = whispered_word(random.randint(1, 4))
        mix(buf, word, t, 1.0)
        t += len(word) / RATE + random.uniform(0.08, 0.35)
    if distance > 0.3:
        buf = lowpass(buf, 6000 - 3500 * distance)
    return [x * (1.0 - 0.55 * distance) for x in buf]


def murmurs(seed):
    random.seed(seed)
    d = 4.0
    buf = zeros(d)
    for distance in (0.0, 0.25, 0.5, 0.7, 0.85):
        mix(buf, whisperer(d, distance), 0.0, 1.0)
    # Un souffle qui passe, comme une présence qui frôle.
    mix(buf, envelope(bandpass(white(int(d * RATE)), lambda k: 700 + 500 * math.sin(k * 6), 0.8),
                      lambda t, k: 0.3 + 0.7 * max(0.0, math.sin(2 * math.pi * 0.5 * t)) ** 2), 0.0, 0.12)
    # Anti-sifflante : on coupe doucement le haut du spectre (plus de « pshiiit »), les voix restent intelligibles.
    buf = lowpass(lowpass(buf, 4200), 5200)
    buf = reverb(buf, size=1.2, wet=0.4, tail=0.0)
    return prev.loopable(buf, 0.7)


def deep_scream(seed, peak_hz):
    random.seed(seed)
    d = 1.9
    buf = zeros(d)
    mix(buf, v4.reverse_swell(0.3, 500, 1500), 0.0, 0.5)

    def contour(k):
        # Attaque qui monte, tenue qui tremble en poussant, puis effondrement.
        if k < 0.12:
            return 120 + (peak_hz - 120) * (k / 0.12) ** 0.7
        if k < 0.5:
            return peak_hz * (1 + 0.04 * math.sin(k * 60))
        return peak_hz * (1 - 0.65 * ((k - 0.5) / 0.5) ** 1.1)

    length = 1.25
    for octave, gain, sub, rough in ((1.0, 1.0, 0.4, 0.55), (0.5, 0.8, 0.65, 0.75), (0.25, 0.35, 0.5, 0.9)):
        v = v4.voice(lambda k, o=octave: contour(k) * o,
                     lambda k: ("a", "o", max(0.0, (k - 0.55) * 2.2)) if k > 0.55 else ("o", "a", min(1.0, k * 5)),
                     length, breath=0.45, rough=rough, jitter=0.035, vib=(6.5, 0.02), subharmonic=sub)
        v = prev.saturate(v, 2.8)
        v = envelope(v, lambda t, k: min(1.0, t * 20) * (1 - k) ** 0.9)
        mix(buf, v, 0.25, gain)
    # Poitrine : grave qui vibre avec le cri.
    mix(buf, tone(lambda k: 55, length, lambda t, k: min(1.0, t * 10) * (1 - k), ((1.0, 1.0), (2.0, 0.3))), 0.25, 0.25)
    # Le crâne éclate au début du cri.
    crack = bandpass(white(int(0.06 * RATE)), lambda k: 2800 - 1500 * k, 1.1)
    mix(buf, envelope(crack, lambda t, k: math.exp(-5 * k)), 0.25, 1.0)
    mix(buf, tone(lambda k: 60 * (1 - 0.5 * k), 0.7, lambda t, k: math.exp(-5 * t) * min(1, t * 400), ((1.0, 1.0), (1.6, 0.3))), 0.25, 0.8)
    mix(buf, grains(0.8, 14, (700, 2000), (0.01, 0.03), (0.08, 0.25), q=9, spread=lambda: random.random() ** 2 * 0.6), 0.25, 1.0)
    return reverb(buf, size=1.6, wet=0.45, tail=1.5)


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(out, exist_ok=True)
    prev.write_raw(os.path.join(out, "missile_vol_murmures_1.wav"), murmurs(101))
    prev.write_raw(os.path.join(out, "missile_vol_murmures_2.wav"), murmurs(202))
    base.write(os.path.join(out, "missile_explosion_cri_grave_1.wav"), deep_scream(303, 230))
    base.write(os.path.join(out, "missile_explosion_cri_grave_2.wav"), deep_scream(404, 185))
