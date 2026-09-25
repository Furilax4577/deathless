"""Sons des projectiles (demande de Quentin) : missile magique du nécromancien (voix des limbes en vol, cri à
l'explosion) et boule de feu du mage (lancer, vol en boucle, explosion). Même boîte à outils que synth_sounds2.py.
Usage : python synth_sounds3.py <dossier_sortie>
"""
import math
import os
import random
import struct
import sys
import wave

import synth_sounds2 as base
from synth_sounds2 import RATE, zeros, mix, white, bandpass, lowpass, envelope, tone, reverb, grains

# Formants de voyelles (fréquence, largeur relative via Q, gain) : « ou », « o », « a ».
VOWELS = {
    "ou": ((300, 8, 1.0), (870, 10, 0.35), (2250, 14, 0.12)),
    "o": ((500, 8, 1.0), (900, 10, 0.5), (2400, 14, 0.15)),
    "a": ((780, 7, 1.0), (1180, 9, 0.6), (2500, 12, 0.3), (3500, 14, 0.12)),
}


def glottal(freq_of, seconds, jitter=0.01, vibrato=(5.0, 0.012)):
    """Source vocale : dent de scie adoucie (cordes vocales), avec vibrato et légère instabilité de hauteur."""
    n = int(seconds * RATE)
    out = [0.0] * n
    phase = 0.0
    drift = 0.0
    vib_rate, vib_depth = vibrato
    for i in range(n):
        k = i / max(1, n - 1)
        t = i / RATE
        if i % 441 == 0:
            drift = drift * 0.7 + random.uniform(-jitter, jitter)
        f = freq_of(k) * (1 + vib_depth * math.sin(2 * math.pi * vib_rate * t) + drift)
        phase = (phase + f / RATE) % 1.0
        out[i] = 1.0 - 2.0 * phase
    return lowpass(out, 5000)


def formant(source, vowel_of):
    """Filtre la source par les formants de la voyelle ; vowel_of(k) -> (voyelle A, voyelle B, mélange)."""
    n = len(source)
    out = [0.0] * n
    names = list(VOWELS.keys())
    for slot in range(4):
        def freq(k, slot=slot):
            a, b, m = vowel_of(k)
            fa = VOWELS[a][min(slot, len(VOWELS[a]) - 1)][0]
            fb = VOWELS[b][min(slot, len(VOWELS[b]) - 1)][0]
            return fa + (fb - fa) * m
        a0, b0, _ = vowel_of(0.0)
        q = VOWELS[a0][min(slot, len(VOWELS[a0]) - 1)][1]
        gain = VOWELS[a0][min(slot, len(VOWELS[a0]) - 1)][2] if slot < len(VOWELS[a0]) else 0.05
        band = bandpass(source, freq, q)
        for i in range(n):
            out[i] += band[i] * gain
    return out


def saturate(sig, drive):
    return [math.tanh(s * drive) for s in sig]


def loopable(sig, fade_seconds):
    """Boucle sans raccord : la fin est fondue dans le début."""
    fade = int(fade_seconds * RATE)
    n = len(sig)
    out = sig[:n - fade]
    for i in range(fade):
        w = i / fade
        out[i] = sig[i] * w + sig[n - fade + i] * (1 - w)
    return out


def write_raw(path, buf, peak=0.8):
    m = max(1e-9, max(abs(s) for s in buf))
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s / m * peak)) * 32767)) for s in buf))
    print("ok", os.path.basename(path), round(len(buf) / RATE, 2), "s (boucle)")


# ------------------------------------------------------------------ missile magique

def limbo_voices():
    """Chœur des limbes : voix graves désaccordées qui glissent lentement entre « ou » et « o », chuchotements."""
    d = 3.4
    buf = zeros(d)
    # Accord sombre et instable (la, do, mi bémol, un triton en dessous) ; chaque voix dérive à son rythme.
    for base_f, gain in ((110, 1.0), (130.8, 0.8), (155.6, 0.7), (77.8, 0.6), (164.8, 0.4)):
        rate = random.uniform(0.15, 0.35)
        ph = random.random() * 6.28
        src = glottal(lambda k, b=base_f, r=rate, p=ph: b * (1 + 0.03 * math.sin(2 * math.pi * r * k * d + p)), d,
                      jitter=0.006, vibrato=(random.uniform(4, 6), 0.01))
        morph_rate = random.uniform(0.3, 0.6)
        voice = formant(src, lambda k, m=morph_rate, p=ph: ("ou", "o", 0.5 + 0.5 * math.sin(2 * math.pi * m * k * d + p)))
        swell = random.uniform(0.25, 0.5)
        voice = envelope(voice, lambda t, k, s=swell, p=ph: 0.55 + 0.45 * math.sin(2 * math.pi * s * t + p))
        mix(buf, voice, 0.0, gain)
    # Chuchotements : souffle passé dans les formants, par bouffées.
    breath = formant(white(int(d * RATE)), lambda k: ("a", "ou", 0.5 + 0.5 * math.sin(k * 9)))
    breath = envelope(breath, lambda t, k: max(0.0, math.sin(2 * math.pi * 1.3 * t)) ** 3 * 0.9)
    mix(buf, breath, 0.0, 0.5)
    buf = reverb(buf, size=1.5, wet=0.55, tail=0.0)
    return loopable(buf, 0.6)


def limbo_scream():
    """Cri à l'explosion : voix qui monte d'un coup, tient en tremblant puis s'effondre, doublée une octave plus bas
    (spectrale), sur le craquement du crâne qui éclate."""
    d = 1.2
    buf = zeros(d)

    def contour(k):
        if k < 0.1:
            return 320 + 480 * (k / 0.1)
        if k < 0.45:
            return 800 + 60 * math.sin(k * 40)
        return 800 - 560 * ((k - 0.45) / 0.55) ** 1.3

    for octave, gain, jitter in ((1.0, 1.0, 0.03), (0.5, 0.55, 0.05), (1.01, 0.5, 0.04)):
        src = glottal(lambda k, o=octave: contour(k) * o, d, jitter=jitter, vibrato=(9.0, 0.025))
        voice = formant(src, lambda k: ("a", "o", min(1.0, max(0.0, (k - 0.5) * 2))))
        voice = saturate(voice, 2.2)
        voice = envelope(voice, lambda t, k: min(1.0, t * 25) * (1 - k) ** 1.4)
        mix(buf, voice, 0.02, gain)
    # Souffle rauque dans le cri.
    rasp = formant(white(int(d * RATE)), lambda k: ("a", "a", 0.0))
    mix(buf, envelope(rasp, lambda t, k: min(1.0, t * 25) * (1 - k) ** 2), 0.02, 0.35)
    # Le crâne éclate : craquement et débris d'os, sous le cri.
    crack = bandpass(white(int(0.06 * RATE)), lambda k: 3200 - 1800 * k, 1.2)
    mix(buf, envelope(crack, lambda t, k: math.exp(-5 * k)), 0.0, 1.2)
    mix(buf, grains(0.8, 18, (700, 2200), (0.01, 0.03), (0.1, 0.35), q=9, spread=lambda: random.random() ** 2 * 0.6), 0.0, 1.0)
    return reverb(buf, size=1.4, wet=0.45, tail=1.2)


# ------------------------------------------------------------------ boule de feu

def fire_cast():
    """Lancer : souffle de feu qui s'embrase (« fwoosh »), grave bref."""
    d = 0.9
    buf = zeros(d)
    whoosh = bandpass(white(int(d * RATE)), lambda k: 250 + 1600 * math.sin(math.pi * min(1, k * 1.4)), 0.9)
    mix(buf, envelope(whoosh, lambda t, k: min(1.0, t * 12) * (1 - k) ** 1.6), 0.0, 1.0)
    mix(buf, envelope(lowpass(white(int(d * RATE)), 250), lambda t, k: min(1.0, t * 20) * (1 - k) ** 2), 0.0, 2.0)
    mix(buf, grains(d, 14, (1500, 4000), (0.003, 0.01), (0.1, 0.3), q=5), 0.0, 1.0)
    return reverb(buf, size=0.9, wet=0.2, tail=0.5)


def fire_flight():
    """Vol en boucle : grondement de flammes qui vacille, crépitements."""
    d = 2.6
    n = int(d * RATE)
    roar = lowpass(white(n), 700)
    # Vacillement : amplitude qui ondule à plusieurs vitesses (flamme qui respire).
    rates = [(random.uniform(3, 6), random.random() * 6.28), (random.uniform(8, 13), random.random() * 6.28)]
    roar = envelope(roar, lambda t, k: 0.65 + 0.2 * math.sin(2 * math.pi * rates[0][0] * t + rates[0][1])
                    + 0.15 * math.sin(2 * math.pi * rates[1][0] * t + rates[1][1]))
    buf = [s * 2.5 for s in roar]
    body = bandpass(white(n), lambda k: 380, 2.0)
    mix(buf, body, 0.0, 0.6)
    mix(buf, grains(d, 60, (1200, 5000), (0.002, 0.008), (0.08, 0.3), q=4), 0.0, 1.0)
    return loopable(buf, 0.4)


def fire_explosion():
    """Explosion : coup grave, souffle de flammes qui s'étale et retombe, débris qui crépitent."""
    d = 2.0
    buf = zeros(d)
    mix(buf, tone(lambda k: 62 * (1 - 0.5 * k), 1.2, lambda t, k: math.exp(-3.5 * t) * min(1, t * 500), ((1.0, 1.0), (1.5, 0.3), (2.0, 0.2))), 0.0, 1.0)
    blast = bandpass(white(int(1.6 * RATE)), lambda k: 2600 * (1 - k) ** 2 + 180, 0.7)
    mix(buf, envelope(blast, lambda t, k: min(1.0, t * 300) * math.exp(-3.2 * t)), 0.0, 1.3)
    mix(buf, envelope(lowpass(white(int(1.4 * RATE)), 300), lambda t, k: min(1.0, t * 300) * math.exp(-2.5 * t)), 0.0, 2.5)
    mix(buf, grains(d, 70, (900, 4500), (0.003, 0.012), (0.08, 0.35), q=4, spread=lambda: random.random() ** 1.7 * 0.8), 0.03, 1.0)
    return reverb(buf, size=1.3, wet=0.35, tail=1.2)


if __name__ == "__main__":
    random.seed(23)
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(out, exist_ok=True)
    write_raw(os.path.join(out, "missile_vol_voix_limbes.wav"), limbo_voices())
    base.write(os.path.join(out, "missile_explosion_cri.wav"), limbo_scream())
    base.write(os.path.join(out, "boule_feu_lancer.wav"), fire_cast())
    write_raw(os.path.join(out, "boule_feu_vol.wav"), fire_flight())
    base.write(os.path.join(out, "boule_feu_explosion.wav"), fire_explosion())
