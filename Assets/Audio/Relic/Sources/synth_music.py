"""Musiques de Relic générées par programme (23 septembre 2026, demande de Quentin : « deux trois musiques pour
dehors / dedans, bouclables à l'infini »). Python pur, WAV 44,1 kHz 16 bits stéréo.

Boucle sans raccord : les nappes tenues (bourdons, cordes) sont rendues un peu plus longues que la boucle puis leur
fin est fondue dans leur début ; les événements (notes pincées, tambours, voix) sont rendus avec leur queue de
réverbération, qui est rajoutée au début de la boucle. Le morceau repart donc exactement comme il finit.

Usage : python synth_music.py <dossier_sortie> [jour|nuit|donjon ...]
"""
import math
import os
import random
import struct
import sys
import wave
from multiprocessing import Pool

import synth_sounds3 as prev
import synth_sounds4 as v4
from synth_sounds2 import RATE, mix, white, bandpass, lowpass, envelope, reverb

NOTES = {"C": 0, "C#": 1, "Db": 1, "D": 2, "D#": 3, "Eb": 3, "E": 4, "F": 5, "F#": 6, "Gb": 6, "G": 7,
         "G#": 8, "Ab": 8, "A": 9, "A#": 10, "Bb": 10, "B": 11}


def hz(name):
    """« D3 », « Bb2 »... -> fréquence (La 4 = 440 Hz)."""
    pitch, octave = name[:-1], int(name[-1])
    return 440.0 * 2 ** ((NOTES[pitch] + 12 * (octave + 1) - 69) / 12)


# ------------------------------------------------------------------ instruments

def lute(freq, seconds, vel=1.0, bright=0.45, decay=0.996):
    """Luth ou harpe : corde pincée (Karplus-Strong), caisse boisée (passe-bande large), attaque adoucie."""
    period = max(2, int(RATE / freq))
    line = lowpass(white(period), 1500 + 6000 * bright)
    n = int(seconds * RATE)
    out = [0.0] * n
    j = 0
    for i in range(n):
        s = line[j]
        out[i] = s
        nxt = j + 1 if j + 1 < period else 0
        line[j] = decay * 0.5 * (s + line[nxt])
        j = nxt
    body = bandpass(out, lambda k: 380, 0.9)
    out = [a * 0.75 + b * 0.5 for a, b in zip(out, body)]
    fade = int(0.004 * RATE)
    for i in range(min(fade, n)):
        out[i] *= i / fade
    for i in range(min(int(0.3 * RATE), n)):
        out[n - 1 - i] *= i / (0.3 * RATE)
    return [s * vel for s in out]


def saw_pad(freqs, seconds, cutoff, detune=0.004, env=None, seed=0):
    """Cordes ou bourdon : scies désaccordées (deux par note), passe-bas, souffle lent de l'amplitude."""
    rnd = random.Random(seed)
    n = int(seconds * RATE)
    out = [0.0] * n
    for f in freqs:
        for d in (-detune, detune):
            phase = rnd.random()
            step = f * (1 + d) / RATE
            wob = rnd.uniform(0.05, 0.15)
            wob_phase = rnd.random() * 6.28
            for i in range(n):
                phase += step * (1 + 0.0015 * math.sin(wob * 6.283 * i / RATE + wob_phase))
                if phase >= 1.0:
                    phase -= 1.0
                out[i] += 2 * phase - 1
    out = lowpass(lowpass(out, cutoff), cutoff * 1.5)
    if env:
        out = envelope(out, env)
    return [s / (2 * len(freqs)) for s in out]


def drum(pitch=70, seconds=0.9, vel=1.0, skin=0.4):
    """Tambour sur cadre ou tambour de guerre : peau grave (hauteur qui chute), claquement de la frappe."""
    n = int(seconds * RATE)
    out = [0.0] * n
    phase = 0.0
    for i in range(n):
        t = i / RATE
        f = pitch * (1 + 0.6 * math.exp(-t * 30))
        phase += 2 * math.pi * f / RATE
        out[i] = math.sin(phase) * math.exp(-t * 5.5 / seconds)
    out = prev.saturate(out, 1.6)
    hit = bandpass(white(int(0.05 * RATE)), lambda k: 900, 0.8)
    mix(out, envelope(hit, lambda t, k: (1 - k) ** 3), 0.0, skin)
    return [s * vel for s in out]


def choir(freq, seconds, vowel="o", vel=1.0):
    """Voix grave tenue (chœur d'hommes lointain) : la voix synthétique des sons, souffle doux, entrée et sortie lentes."""
    v = v4.voice(lambda k: freq, lambda k: (vowel, "ou", 0.3 + 0.2 * math.sin(k * 3)), seconds,
                 breath=0.35, rough=0.05, jitter=0.006, vib=(4.8, 0.008))
    v = lowpass(v, 2200)
    return envelope(v, lambda t, k: vel * math.sin(math.pi * k) ** 1.5)


def drip():
    """Goutte d'eau dans une salle de pierre."""
    f = random.uniform(900, 1500)
    n = int(0.08 * RATE)
    out = [0.0] * n
    phase = 0.0
    for i in range(n):
        k = i / n
        phase += 2 * math.pi * f * (1 + 1.2 * k) / RATE
        out[i] = math.sin(phase) * math.sin(math.pi * min(1, k * 8)) * (1 - k) ** 2
    return out


# ------------------------------------------------------------------ assemblage

class Track:
    """Deux couches : `pad` (tenues, bouclées par fondu) et `events` (notes et coups, queue rajoutée au début)."""

    def __init__(self, seconds, tail=7.0, fade=4.0):
        self.length = seconds
        self.tail = tail
        self.fade = fade
        self.pad = [0.0] * int((seconds + fade) * RATE)
        self.events = [[0.0] * int((seconds + tail) * RATE) for _ in range(2)]  # gauche, droite

    def add(self, sig, at, gain=1.0, pan=0.0):
        """Evénement à `at` secondes, placé dans le champ stéréo (pan de -1 à 1)."""
        left = gain * math.cos((pan + 1) * math.pi / 4) * 1.41
        right = gain * math.sin((pan + 1) * math.pi / 4) * 1.41
        if at < 0:  # une note humanisée juste avant le début de la boucle se place à sa fin
            at += self.length
        start = int(at * RATE)
        for c, g in ((0, left), (1, right)):
            buf = self.events[c]
            for i, s in enumerate(sig):
                if start + i < len(buf):
                    buf[start + i] += s * g

    def render(self, room=1.4, wet=0.35, pad_wet=0.3):
        n = int(self.length * RATE)
        fade = int(self.fade * RATE)
        channels = []
        for c in range(2):
            size = room * (1.0 if c == 0 else 1.07)
            ev = reverb(self.events[c], size=size, wet=wet, tail=0.0)
            loop = ev[:n]
            for i in range(len(ev) - n):  # la queue revient au début
                loop[i % n] += ev[n + i]
            pad = reverb(self.pad, size=size * 1.1, wet=pad_wet, tail=0.0)
            pad = [pad[i] * (i / fade) + pad[n + i] * (1 - i / fade) if i < fade else pad[i] for i in range(n)]
            channels.append([a + b for a, b in zip(loop, pad)])
        return channels


def write_stereo(path, channels, peak=0.85):
    m = max(1e-9, max(abs(s) for ch in channels for s in ch))
    frames = bytearray()
    for l, r in zip(*channels):
        frames += struct.pack("<hh", int(l / m * peak * 32767), int(r / m * peak * 32767))
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(bytes(frames))
    print("ok", os.path.basename(path), round(len(channels[0]) / RATE, 1), "s (boucle stéréo)")


# ------------------------------------------------------------------ dehors, le jour

def village_day():
    """Village le jour : ré dorien, 3/4 lent, luth en arpèges, bourdon ré-la, tambour sur cadre, une mélodie de luth
    qui n'arrive qu'à la seconde moitié. Calme mais pas naïf : modal, grave, sans clochette."""
    random.seed(9001)
    beat = 60 / 76
    bar = 3 * beat
    bars = 32
    tr = Track(bars * bar)
    chords = [("D3", "A3", "D4", "F4"), ("C3", "G3", "C4", "E4"), ("G2", "D3", "G3", "B3"), ("D3", "A3", "D4", "F4"),
              ("F2", "C3", "F3", "A3"), ("C3", "G3", "C4", "E4"), ("A2", "E3", "A3", "C4"), ("D3", "A3", "D4", "F4")]
    # Bourdon ré-la sur toute la boucle, qui respire.
    tr.pad = [s * 0.55 for s in saw_pad([hz("D2"), hz("A2")], tr.length + tr.fade, 520,
                                       env=lambda t, k: 0.75 + 0.25 * math.sin(2 * math.pi * t / (8 * bar)), seed=1)]
    order = (0, 1, 2, 3, 2, 1)  # arpège sur six croches
    for b in range(bars):
        chord = chords[(b // 2) % len(chords)]
        for e, idx in enumerate(order):
            at = b * bar + e * beat / 2 + random.uniform(-0.008, 0.008)
            vel = (0.9 if e == 0 else 0.55) * random.uniform(0.85, 1.0)
            tr.add(lute(hz(chord[idx]), 2.2, vel, 0.35), at, 0.5, pan=-0.35 + 0.1 * idx)
        # Basse de luth sur le premier temps.
        tr.add(lute(hz(chord[0]) / 2, 3.0, 0.9, 0.25, 0.997), b * bar, 0.5, pan=0.0)
        # Tambour sur cadre à partir de la deuxième phrase.
        if b >= 8:
            tr.add(drum(62, 0.9, 0.9), b * bar, 0.35, pan=0.15)
            tr.add(drum(95, 0.5, 0.4, 0.6), b * bar + 2 * beat, 0.3, pan=0.2)
            if b % 2 == 1:
                tr.add(drum(95, 0.4, 0.3, 0.7), b * bar + 2.5 * beat, 0.25, pan=0.2)
    # Mélodie (mesures 17 à 32) : (mesure, temps, note, durée en temps).
    melody = [(16, 0, "A4", 2), (16, 2, "G4", 1), (17, 0, "F4", 2), (17, 2, "E4", 1), (18, 0, "D4", 3),
              (19, 1, "E4", 1), (19, 2, "F4", 1), (20, 0, "A4", 3), (21, 0, "G4", 1.5), (21, 1.5, "E4", 1.5),
              (22, 0, "C4", 2), (22, 2, "E4", 1), (23, 0, "D4", 3),
              (24, 0, "D5", 2), (24, 2, "C5", 1), (25, 0, "A4", 2), (25, 2, "G4", 1), (26, 0, "B4", 3),
              (27, 1, "A4", 1), (27, 2, "G4", 1), (28, 0, "F4", 2), (28, 2, "A4", 1), (29, 0, "G4", 1.5),
              (29, 1.5, "E4", 1.5), (30, 0, "E4", 2), (30, 2, "C4", 1), (31, 0, "D4", 3)]
    for m, t, note, d in melody:
        at = m * bar + t * beat + random.uniform(-0.01, 0.01)
        tr.add(lute(hz(note), d * beat + 1.8, 0.95, 0.55, 0.9975), at, 0.55, pan=0.3)
        # Doublure une octave plus bas, discrète, pour épaissir.
        tr.add(lute(hz(note) / 2, d * beat + 1.2, 0.5, 0.3), at + 0.012, 0.25, pan=-0.2)
    return tr.render(room=1.3, wet=0.3, pad_wet=0.25)


# ------------------------------------------------------------------ dehors, la nuit

def village_night():
    """Village la nuit, pendant la vague : ré phrygien, 4/4 à 96, tambours de guerre, ostinato grave pulsé, cordes
    dissonantes (demi-ton ré / mi bémol), chœur d'hommes lointain dans la seconde moitié."""
    random.seed(9002)
    beat = 60 / 96
    bar = 4 * beat
    bars = 28
    tr = Track(bars * bar)
    # Cordes graves : quatre mesures sur ré, quatre sur mi bémol, fondues entre elles.
    span = 4 * bar
    total = tr.length + tr.fade

    def on_d(t, k):
        x = (t % (2 * span)) / span
        return 0.5 + 0.5 * math.cos(math.pi * min(1.0, max(0.0, (x - 0.8) / 0.2)) if x < 1 else math.pi * (1 - min(1.0, max(0.0, (x - 1.8) / 0.2))))

    pad_d = saw_pad([hz("D2"), hz("A2"), hz("D3")], total, 650, 0.005, env=on_d, seed=2)
    pad_eb = saw_pad([hz("Eb2"), hz("Bb2"), hz("Eb3")], total, 650, 0.005, env=lambda t, k: 1 - on_d(t, k), seed=3)
    tr.pad = [(a + b) * 0.6 for a, b in zip(pad_d, pad_eb)]
    for b in range(bars):
        root = "D" if (b // 4) % 2 == 0 else "Eb"
        # Ostinato grave en croches, accent sur 1 et 3.
        for e in range(8):
            at = b * bar + e * beat / 2
            vel = 1.0 if e in (0, 4) else (0.7 if e in (3, 7) else 0.5)
            note = hz(root + "2") * (2 if e == 6 else 1)
            tr.add(lute(note, 0.5, vel, 0.2, 0.99), at, 0.45, pan=-0.2)
        # Tambours de guerre : grand coup sur 1, réponse sur le « et » de 2 et sur 3, roulement toutes les 4 mesures.
        tr.add(drum(52, 1.4, 1.0, 0.5), b * bar, 0.7, pan=0.0)
        tr.add(drum(58, 0.9, 0.6, 0.5), b * bar + 1.5 * beat, 0.5, pan=-0.3)
        tr.add(drum(52, 1.2, 0.85, 0.5), b * bar + 2 * beat, 0.6, pan=0.0)
        tr.add(drum(110, 0.4, 0.35, 0.8), b * bar + 3 * beat, 0.35, pan=0.35)
        tr.add(drum(110, 0.4, 0.3, 0.8), b * bar + 3.5 * beat, 0.3, pan=0.35)
        if b % 4 == 3:
            for s in range(4):
                tr.add(drum(80 + s * 6, 0.5, 0.45 + 0.1 * s, 0.7), b * bar + 3 * beat + s * beat / 4, 0.4, pan=0.25)
    # Chœur (mesures 13 à 28) : longues notes, une par phrase de deux mesures.
    line = ["D3", "F3", "Eb3", "D3", "A2", "C3", "Bb2", "A2"]
    for i, note in enumerate(line):
        at = (12 + 2 * i) * bar
        tr.add(choir(hz(note), 2 * bar + 1.0, "o", 1.0), at, 0.35, pan=-0.25)
        tr.add(choir(hz(note) / 2, 2 * bar + 1.0, "ou", 0.8), at + 0.05, 0.3, pan=0.25)
    return tr.render(room=1.5, wet=0.3, pad_wet=0.25)


# ------------------------------------------------------------------ dedans, le donjon

def dungeon():
    """Donjon : ambiance sombre et lente, bourdon très grave qui s'ouvre et se referme, harpe grave clairsemée en ré
    mineur, chœur lointain, gouttes, souffles qui passent. Presque sans pulsation."""
    random.seed(9003)
    length = 96.0
    tr = Track(length, tail=9.0, fade=6.0)
    total = tr.length + tr.fade
    cycle = length / 3  # le filtre du bourdon s'ouvre trois fois par boucle
    drone = saw_pad([hz("D1"), hz("A1"), hz("D2")], total, 260, 0.003,
                    env=lambda t, k: 0.7 + 0.3 * math.sin(2 * math.pi * t / cycle), seed=4)
    rumble = lowpass(v4.brown(int(total * RATE), 0.004), 120)
    tr.pad = [d * 0.7 + r * 0.25 * (0.6 + 0.4 * math.sin(2 * math.pi * i / RATE / (length / 2) + 1)) for i, (d, r) in enumerate(zip(drone, rumble))]
    # Harpe grave : notes isolées de la gamme de ré mineur, parfois en paires.
    scale = ["D2", "F2", "A2", "C3", "D3", "E3", "F3", "A3", "C4"]
    t = 1.0
    while t < length - 3:
        note = random.choice(scale)
        tr.add(lute(hz(note), 4.0, random.uniform(0.6, 1.0), 0.35, 0.998), t, 0.45, pan=random.uniform(-0.6, 0.6))
        if random.random() < 0.35:
            tr.add(lute(hz(random.choice(scale)), 3.5, 0.6, 0.35, 0.998), t + random.choice((0.4, 0.6, 0.8)), 0.35, pan=random.uniform(-0.6, 0.6))
        t += random.uniform(3.0, 6.5)
    # Chœur lointain : quatre longues notes par boucle.
    for at, note, vowel in ((6, "D3", "o"), (30, "F3", "ou"), (54, "E3", "o"), (78, "C3", "ou")):
        tr.add(choir(hz(note), 12.0, vowel, 0.9), at, 0.3, pan=-0.3)
        tr.add(choir(hz(note) * 1.5, 11.0, "ou", 0.6), at + 1.5, 0.18, pan=0.35)
    # Souffles qui passent et gouttes.
    for at in (14, 41, 67, 88):
        tr.add(v4.reverse_swell(2.5, 200, 700), at, 0.25, pan=random.uniform(-0.7, 0.7))
    for _ in range(26):
        tr.add(drip(), random.uniform(0, length - 1), random.uniform(0.05, 0.12), pan=random.uniform(-0.9, 0.9))
    return tr.render(room=2.0, wet=0.5, pad_wet=0.35)


TRACKS = {"jour": ("musique_dehors_jour.wav", village_day),
          "nuit": ("musique_dehors_nuit.wav", village_night),
          "donjon": ("musique_dedans_donjon.wav", dungeon)}


def run(job):
    name, out = job
    file, fn = TRACKS[name]
    write_stereo(os.path.join(out, file), fn())


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    names = sys.argv[2:] or list(TRACKS)
    os.makedirs(out, exist_ok=True)
    with Pool(len(names)) as pool:
        pool.map(run, [(n, out) for n in names])
