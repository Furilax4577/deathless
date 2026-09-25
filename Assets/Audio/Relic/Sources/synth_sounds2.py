"""Sons de Relic, deuxième passe (retour de Quentin : « trop enfantin »). Plus sombre et organique : graves, souffles
filtrés, craquements, résonances métalliques inharmoniques et dissonantes, réverbération ; aucune clochette
mélodique. Python pur, WAV 44,1 kHz 16 bits mono. Usage : python synth_sounds2.py <dossier_sortie>
"""
import math
import os
import random
import struct
import sys
import wave

RATE = 44100


def zeros(seconds):
    return [0.0] * int(seconds * RATE)


def mix(buf, sig, at=0.0, gain=1.0):
    start = int(at * RATE)
    if start + len(sig) > len(buf):
        buf.extend([0.0] * (start + len(sig) - len(buf)))
    for i, s in enumerate(sig):
        buf[start + i] += s * gain
    return buf


def white(n):
    return [random.uniform(-1.0, 1.0) for _ in range(n)]


def bandpass(sig, freq_of, q):
    """Passe-bande biquad (RBJ) dont la fréquence centrale suit freq_of(k), k de 0 à 1 le long du signal."""
    out = [0.0] * len(sig)
    x1 = x2 = y1 = y2 = 0.0
    n = len(sig)
    for i, x in enumerate(sig):
        if i % 32 == 0:
            f = max(20.0, min(RATE * 0.45, freq_of(i / max(1, n - 1))))
            w = 2 * math.pi * f / RATE
            alpha = math.sin(w) / (2 * q)
            a0 = 1 + alpha
            b0, b2 = alpha / a0, -alpha / a0
            a1, a2 = -2 * math.cos(w) / a0, (1 - alpha) / a0
        y = b0 * x + b2 * x2 - a1 * y1 - a2 * y2
        x2, x1, y2, y1 = x1, x, y1, y
        out[i] = y
    return out


def lowpass(sig, cutoff):
    a = 1.0 - math.exp(-2 * math.pi * cutoff / RATE)
    y = 0.0
    out = []
    for x in sig:
        y += a * (x - y)
        out.append(y)
    return out


def envelope(sig, env):
    n = len(sig)
    return [s * env(i / RATE, i / max(1, n - 1)) for i, s in enumerate(sig)]


def tone(freq_of, seconds, env, harmonics=((1.0, 1.0),)):
    """Oscillateur dont la fréquence suit freq_of(k) ; harmonics : (rapport, amplitude), inharmoniques permis."""
    n = int(seconds * RATE)
    phases = [random.random() * 6.28 for _ in harmonics]
    out = [0.0] * n
    for i in range(n):
        k = i / max(1, n - 1)
        f = freq_of(k)
        s = 0.0
        for j, (ratio, amp) in enumerate(harmonics):
            phases[j] += 2 * math.pi * f * ratio / RATE
            s += amp * math.sin(phases[j])
        out[i] = s * env(i / RATE, k)
    return out


def reverb(sig, size=1.0, wet=0.35, tail=1.5):
    """Réverbération de Schroeder (4 filtres en peigne parallèles, 2 passe-tout en série) : une pièce de pierre."""
    sig = sig + [0.0] * int(tail * RATE)
    combs = [(int(d * size), g) for d, g in ((1557, 0.84), (1617, 0.83), (1491, 0.82), (1422, 0.81))]
    total = [0.0] * len(sig)
    for delay, g in combs:
        line = [0.0] * delay
        idx = 0
        damp = 0.0
        for i, x in enumerate(sig):
            y = line[idx]
            damp = y * 0.6 + damp * 0.4  # amortissement des aigus dans la queue
            line[idx] = x + damp * g
            idx = (idx + 1) % delay
            total[i] += y
    for delay, g in ((225, 0.5), (556, 0.5)):
        line = [0.0] * delay
        idx = 0
        out = []
        for x in total:
            b = line[idx]
            y = -x + b
            line[idx] = x + b * g
            idx = (idx + 1) % delay
            out.append(y)
        total = out
    return [d * (1 - wet) + w * wet * 0.25 for d, w in zip(sig, total)]


def grains(seconds, count, freq_range, dur_range, gain_range, q=6.0, start=0.0, spread=None):
    """Craquements : courtes rafales de bruit filtré à des instants aléatoires (glace, os, braise)."""
    buf = zeros(seconds)
    for _ in range(count):
        d = random.uniform(*dur_range)
        f = random.uniform(*freq_range)
        g = bandpass(white(int(d * RATE)), lambda k, f=f: f, q)
        g = envelope(g, lambda t, k: math.exp(-6 * k) * min(1.0, t * 800))
        at = start + (random.random() if spread is None else spread()) * (seconds - start - d)
        mix(buf, g, at, random.uniform(*gain_range))
    return buf


def write(path, buf, peak=0.8):
    m = max(1e-9, max(abs(s) for s in buf))
    # Fin coupée dès que le son passe sous -54 dB (la queue de réverbération inaudible ne sert à rien).
    end = len(buf)
    while end > 0 and abs(buf[end - 1]) < m * 0.002:
        end -= 1
    buf = buf[:min(len(buf), end + int(0.05 * RATE))]
    fade = int(0.05 * RATE)
    for i in range(min(fade, len(buf))):
        buf[-1 - i] *= i / fade
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s / m * peak)) * 32767)) for s in buf))
    print("ok", os.path.basename(path), round(len(buf) / RATE, 2), "s")


# Résonance métallique sombre (pierre, cristal massif) : partiels inharmoniques graves.
DARK_METAL = ((1.0, 1.0), (1.414, 0.55), (2.31, 0.3), (3.07, 0.18))


def portal_open():
    d = 2.6
    buf = zeros(d)
    # Souffle d'air aspiré : bruit dont la bande monte, gonfle puis retombe.
    whoosh = bandpass(white(int(d * RATE)), lambda k: 180 + 1400 * math.sin(math.pi * min(1, k * 1.2)) ** 2, 1.4)
    mix(buf, envelope(whoosh, lambda t, k: math.sin(math.pi * min(1, k * 1.15)) ** 1.5), 0.0, 0.9)
    # Grave qui enfle (le vide qui s'ouvre), avec un léger battement.
    mix(buf, tone(lambda k: 42 + 14 * k, d, lambda t, k: math.sin(math.pi * k) ** 2, ((1.0, 1.0), (1.02, 0.6), (2.0, 0.2))), 0.0, 0.8)
    # Résonance dissonante qui s'installe quand le portail est ouvert.
    mix(buf, tone(lambda k: 146, 2.0, lambda t, k: min(1, t * 3) * math.exp(-1.6 * t), DARK_METAL), 0.6, 0.22)
    mix(buf, tone(lambda k: 155, 2.0, lambda t, k: min(1, t * 3) * math.exp(-1.8 * t), DARK_METAL), 0.62, 0.16)
    # Crépitement de gemmes, discret, dans le haut.
    mix(buf, grains(d, 45, (2500, 6000), (0.004, 0.012), (0.05, 0.14), q=8, start=0.4), 0.0, 1.0)
    return reverb(buf, size=1.3, wet=0.4, tail=1.6)


def relic_pulse():
    d = 2.2
    buf = zeros(d)
    # Coup sourd : sinus grave qui chute, comme une onde de choc.
    mix(buf, tone(lambda k: 70 * (1 - 0.55 * k), 0.9, lambda t, k: math.exp(-5.5 * t) * min(1, t * 400), ((1.0, 1.0), (2.0, 0.25))), 0.0, 1.0)
    # Pression d'air : bruit grave filtré, bref.
    mix(buf, envelope(lowpass(white(int(0.5 * RATE)), 400), lambda t, k: math.exp(-9 * t)), 0.0, 2.2)
    # Bourdon du cristal qui résonne après le coup, deux notes proches qui battent (tension).
    hum = ((1.0, 1.0), (2.0, 0.35), (2.76, 0.12))
    mix(buf, tone(lambda k: 98, 2.0, lambda t, k: min(1, t * 8) * math.exp(-1.4 * t), hum), 0.02, 0.35)
    mix(buf, tone(lambda k: 103.5, 2.0, lambda t, k: min(1, t * 8) * math.exp(-1.5 * t), hum), 0.02, 0.3)
    # L'onde qui s'éloigne : souffle qui s'ouvre et s'éteint.
    wave_ = bandpass(white(int(1.6 * RATE)), lambda k: 300 + 2200 * k, 1.0)
    mix(buf, envelope(wave_, lambda t, k: math.sin(math.pi * k) * (1 - k)), 0.08, 0.35)
    return reverb(buf, size=1.4, wet=0.45, tail=1.8)


def skull_shatter():
    d = 1.1
    buf = zeros(d)
    # Craquement net (os, verre épais).
    crack = bandpass(white(int(0.06 * RATE)), lambda k: 3200 - 1800 * k, 1.2)
    mix(buf, envelope(crack, lambda t, k: math.exp(-5 * k)), 0.0, 1.3)
    mix(buf, tone(lambda k: 120 * (1 - 0.5 * k), 0.25, lambda t, k: math.exp(-14 * t), ((1.0, 1.0), (1.7, 0.4))), 0.0, 0.7)
    # Débris d'os qui s'entrechoquent : clics résonants dans le médium, serrés au début.
    mix(buf, grains(d, 26, (700, 2200), (0.01, 0.03), (0.15, 0.45), q=9,
                    spread=lambda: random.random() ** 2.2 * 0.55), 0.0, 1.0)
    # Soupir spectral qui s'échappe (l'âme du missile).
    exhale = bandpass(white(int(0.9 * RATE)), lambda k: 900 - 500 * k, 2.5)
    mix(buf, envelope(exhale, lambda t, k: math.sin(math.pi * k) * (1 - k) ** 0.5), 0.05, 0.35)
    return reverb(buf, size=1.0, wet=0.3, tail=1.0)


def dawn_vaporize():
    d = 2.2
    buf = zeros(d)
    # Grésillement (os qui se dissout au soleil) : bruit aigu en crépitements, qui monte puis s'éteint.
    mix(buf, grains(d, 160, (3500, 9000), (0.002, 0.006), (0.04, 0.12), q=4,
                    spread=lambda: math.sqrt(random.random()) * 0.8), 0.0, 1.0)
    sizzle = bandpass(white(int(d * RATE)), lambda k: 5000 + 2000 * k, 0.8)
    mix(buf, envelope(sizzle, lambda t, k: math.sin(math.pi * min(1, k * 1.3)) ** 2), 0.0, 0.25)
    # Souffle qui s'élève et s'emporte (la poussière part avec le vent).
    rise = bandpass(white(int(d * RATE)), lambda k: 350 + 1300 * k, 1.2)
    mix(buf, envelope(rise, lambda t, k: math.sin(math.pi * k) ** 1.2), 0.0, 0.55)
    # Grave qui se retire : la magie qui quitte le corps.
    mix(buf, tone(lambda k: 65 - 20 * k, 1.6, lambda t, k: math.exp(-2.5 * t) * min(1, t * 30), ((1.0, 1.0), (1.5, 0.3))), 0.0, 0.45)
    return reverb(buf, size=1.2, wet=0.35, tail=1.2)


def crystal_light():
    d = 1.6
    buf = zeros(d)
    # Cristal frotté (pas frappé) : attaque lente, résonance grave et inharmonique qui bat doucement.
    body = ((1.0, 1.0), (2.41, 0.35), (3.83, 0.12))
    mix(buf, tone(lambda k: 233, 1.4, lambda t, k: math.sin(math.pi * min(1, t / 0.25) * 0.5) * math.exp(-2.2 * max(0, t - 0.2)), body), 0.0, 0.45)
    mix(buf, tone(lambda k: 236.5, 1.4, lambda t, k: math.sin(math.pi * min(1, t / 0.3) * 0.5) * math.exp(-2.4 * max(0, t - 0.25)), body), 0.0, 0.3)
    # Petit souffle d'énergie à l'allumage et quelques grains.
    air = bandpass(white(int(0.6 * RATE)), lambda k: 1200 + 800 * k, 1.5)
    mix(buf, envelope(air, lambda t, k: math.sin(math.pi * k) * (1 - k)), 0.0, 0.25)
    mix(buf, grains(0.8, 10, (2500, 5000), (0.003, 0.008), (0.03, 0.08), q=8), 0.05, 1.0)
    return reverb(buf, size=1.1, wet=0.4, tail=1.2)


if __name__ == "__main__":
    random.seed(11)
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(out, exist_ok=True)
    write(os.path.join(out, "portail_ouverture_v2.wav"), portal_open())
    write(os.path.join(out, "relique_impulsion_v2.wav"), relic_pulse())
    write(os.path.join(out, "crane_eclat_v2.wav"), skull_shatter())
    write(os.path.join(out, "aube_vaporisation_v2.wav"), dawn_vaporize())
    write(os.path.join(out, "cristal_allume_v2.wav"), crystal_light())
