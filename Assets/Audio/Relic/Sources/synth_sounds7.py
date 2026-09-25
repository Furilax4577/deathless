"""Sons manquants de Relic (plan des sons, 23 septembre 2026). Même direction que les sons validés : sombre et
organique, à base de bruits filtrés, de grondements, de craquements d'os et de bois, de souffles et de voix
synthétisées ; très peu de hauteurs tenues (ce sont elles qui rendaient les essais précédents « enfantins »), jamais
de clochette ni de mélodie. Python pur, WAV 44,1 kHz 16 bits mono.

Usage : python synth_sounds7.py <dossier_sortie> [nom ...]   (sans nom : tout le catalogue)
Chaque son porte sa graine : relancer donne exactement le même fichier.
"""
import math
import os
import random
import sys
from multiprocessing import Pool

import synth_sounds2 as base
import synth_sounds3 as prev
import synth_sounds4 as v4
import synth_sounds6 as v6
from synth_sounds2 import RATE, zeros, mix, white, bandpass, lowpass, envelope, tone, reverb, grains


# ------------------------------------------------------------------ briques

def highpass(sig, cutoff):
    low = lowpass(sig, cutoff)
    return [s - l for s, l in zip(sig, low)]


def noise(seconds, freq_of, q, env):
    """Bruit blanc filtré (passe-bande qui suit freq_of) sous une enveloppe env(t, k)."""
    return envelope(bandpass(white(int(seconds * RATE)), freq_of, q), env)


def rumble(seconds, cutoff, env, leak=0.01):
    """Grondement : bruit brun passé dans un passe-bas."""
    return envelope(lowpass(v4.brown(int(seconds * RATE), leak), cutoff), env)


def thump(f0, f1, seconds, drive=1.5):
    """Coup sourd : sinus dont la hauteur chute de f0 à f1, saturé, décroissance rapide (corps d'un impact)."""
    sig = tone(lambda k: f0 + (f1 - f0) * k ** 0.5, seconds, lambda t, k: min(1.0, t * 900) * math.exp(-7 * k))
    return prev.saturate(sig, drive)


def knock(freq, seconds=0.12, q=12):
    """Coup de bois ou d'os : bruit très résonant (q élevé) qui s'éteint vite."""
    return noise(seconds, lambda k: freq, q, lambda t, k: min(1.0, t * 3000) * math.exp(-9 * k))


def click(freq, q=4, seconds=0.012):
    return noise(seconds, lambda k: freq, q, lambda t, k: (1 - k) ** 2)


def whoosh(seconds, f0, f1, q=1.2, shape=1.0):
    return noise(seconds, lambda k: f0 + (f1 - f0) * k, q, lambda t, k: math.sin(math.pi * k) ** shape)


def pluck(freq, seconds, decay=0.992, bright=0.5):
    """Corde pincée (Karplus-Strong) : la corde d'un arc ou d'une arbalète qui claque."""
    period = max(2, int(RATE / freq))
    line = lowpass(white(period), 2000 + 8000 * bright)
    out = []
    for i in range(int(seconds * RATE)):
        j = i % period
        s = line[j]
        out.append(s)
        line[j] = decay * 0.5 * (s + line[(j + 1) % period])
    return out


def creak(seconds, rate_of, freq, q=6):
    """Grincement (bois qui se tend, charnière) : train d'impulsions irrégulier dans un résonateur."""
    n = int(seconds * RATE)
    sig = [0.0] * n
    t = 0.0
    while t < seconds:
        k = t / seconds
        i = int(t * RATE)
        if i < n:
            sig[i] = random.uniform(0.6, 1.0)
        t += 1.0 / max(5.0, rate_of(k)) * random.uniform(0.7, 1.3)
    return bandpass(bandpass(sig, lambda k: freq, q), lambda k: freq * 1.9, q * 0.7)


def bones(seconds, count, spread=None, low=600, high=2400, gain=(0.2, 0.6)):
    """Os qui s'entrechoquent : clics creux et résonants."""
    return grains(seconds, count, (low, high), (0.008, 0.03), gain, q=11, spread=spread)


def gravel(seconds, count, spread=None, gain=(0.1, 0.4)):
    """Terre et gravats : grains sourds et larges."""
    return grains(seconds, count, (180, 1400), (0.01, 0.05), gain, q=2.5, spread=spread)


def fire_roar(seconds, cutoff=900):
    n = int(seconds * RATE)
    return lowpass(v4.brown(n, 0.012), cutoff)


def lin_fade(sig, fade_in=0.005, fade_out=0.05):
    n = len(sig)
    a, b = int(fade_in * RATE), int(fade_out * RATE)
    for i in range(min(a, n)):
        sig[i] *= i / a
    for i in range(min(b, n)):
        sig[n - 1 - i] *= i / b
    return sig


def loop_cycles(seconds, hz):
    """Fréquence de modulation arrondie pour tomber juste sur la longueur de la boucle."""
    return max(1, round(hz * seconds)) / seconds


# ------------------------------------------------------------------ tirs et impacts

def bow_shot():
    buf = zeros(0.8)
    mix(buf, click(2200, 1.5, 0.015), 0.0, 0.6)
    string = prev.saturate(pluck(random.uniform(88, 104), 0.45, 0.975, 0.35), 1.6)
    mix(buf, envelope(lowpass(string, 2600), lambda t, k: (1 - k) ** 2), 0.0, 0.9)
    mix(buf, thump(140, 70, 0.08), 0.0, 0.35)
    mix(buf, whoosh(0.28, 900, 2600, 1.3, 0.6), 0.01, 0.35)
    return reverb(buf, 0.8, 0.12, 0.3)


def bow_shot_v2():
    # Arc du rôdeur, deuxième version (retour de Quentin : « refaire le son de l'arc et le baisser ») : plus de claquement
    # sec ; une corde grave et douce qui vibre, le bois de l'arc qui répond, et le souffle de la flèche qui part.
    buf = zeros(0.9)
    string = pluck(random.uniform(70, 82), 0.55, 0.982, 0.18)
    mix(buf, envelope(lowpass(string, 1400), lambda t, k: (1 - k) ** 1.6), 0.0, 0.9)
    mix(buf, knock(random.uniform(180, 230), 0.16, 8), 0.0, 0.35)
    mix(buf, thump(120, 65, 0.09, 1.3), 0.0, 0.25)
    mix(buf, whoosh(0.32, 600, 1900, 1.0, 0.7), 0.015, 0.45)
    return reverb(buf, 0.9, 0.14, 0.35)


def bow_shot_charged():
    # Tir chargé du rôdeur : la corde tendue plus fort claque plus grave et plus long, la flèche part plus vite.
    buf = zeros(1.1)
    mix(buf, creak(0.12, lambda k: 80, 560, 5), 0.0, 0.25)
    string = pluck(random.uniform(52, 60), 0.7, 0.985, 0.22)
    mix(buf, envelope(lowpass(string, 1300), lambda t, k: (1 - k) ** 1.4), 0.1, 1.0)
    mix(buf, knock(random.uniform(150, 190), 0.2, 8), 0.1, 0.45)
    mix(buf, thump(95, 50, 0.14, 1.6), 0.1, 0.45)
    mix(buf, whoosh(0.4, 900, 2600, 1.2, 0.6), 0.11, 0.55)
    return reverb(buf, 1.0, 0.16, 0.4)


def crossbow_shot():
    buf = zeros(0.8)
    mix(buf, click(3600, 8, 0.01), 0.0, 0.7)
    mix(buf, click(1700, 8, 0.015), 0.012, 0.6)
    string = prev.saturate(pluck(random.uniform(125, 150), 0.3, 0.96, 0.5), 2.0)
    mix(buf, envelope(lowpass(string, 3000), lambda t, k: (1 - k) ** 2.5), 0.015, 1.0)
    mix(buf, thump(110, 60, 0.1), 0.015, 0.5)
    mix(buf, knock(420, 0.08), 0.02, 0.4)
    mix(buf, whoosh(0.22, 1200, 3000, 1.5, 0.5), 0.02, 0.3)
    return reverb(buf, 0.8, 0.12, 0.3)


def skeleton_bow_shot():
    # Arc des squelettes : plus sec, corde plus lâche, et le cliquetis des os du bras qui se détend.
    buf = zeros(0.8)
    string = prev.saturate(pluck(random.uniform(72, 84), 0.4, 0.97, 0.25), 1.8)
    mix(buf, envelope(lowpass(string, 2000), lambda t, k: (1 - k) ** 2), 0.0, 0.9)
    mix(buf, bones(0.25, 4, spread=lambda: random.random() ** 2, gain=(0.15, 0.4)), 0.0, 1.0)
    mix(buf, whoosh(0.26, 700, 2200, 1.2, 0.6), 0.01, 0.3)
    return reverb(buf, 0.8, 0.15, 0.3)


def tower_shot():
    # Baliste de la tour : mécanisme de bois, corde épaisse, recul sourd.
    buf = zeros(1.0)
    mix(buf, creak(0.12, lambda k: 90, 500, 5), 0.0, 0.25)
    mix(buf, click(2800, 6, 0.012), 0.1, 0.7)
    string = prev.saturate(pluck(random.uniform(62, 72), 0.5, 0.97, 0.3), 2.2)
    mix(buf, envelope(lowpass(string, 1800), lambda t, k: (1 - k) ** 2), 0.1, 1.0)
    mix(buf, thump(90, 45, 0.18, 2.0), 0.1, 0.8)
    mix(buf, knock(310, 0.14, 10), 0.11, 0.5)
    mix(buf, knock(520, 0.1, 10), 0.13, 0.3)
    mix(buf, whoosh(0.3, 600, 1800, 1.1, 0.6), 0.11, 0.3)
    return reverb(buf, 1.0, 0.2, 0.4)


def arrow_impact():
    buf = zeros(0.5)
    mix(buf, thump(random.uniform(170, 210), 80, 0.07, 2.0), 0.0, 0.8)
    mix(buf, click(random.uniform(900, 1400), 2, 0.02), 0.0, 0.6)
    # La hampe qui vibre après s'être fichée.
    wobble = pluck(random.uniform(170, 220), 0.25, 0.9, 0.2)
    mix(buf, envelope(wobble, lambda t, k: 0.5 + 0.5 * math.sin(2 * math.pi * 38 * t)), 0.005, 0.25)
    return reverb(buf, 0.7, 0.1, 0.2)


# ------------------------------------------------------------------ mêlée et défense

def skeleton_hit():
    buf = zeros(0.6)
    mix(buf, noise(0.05, lambda k: 2200 - 900 * k, 1.4, lambda t, k: math.exp(-5 * k)), 0.0, 0.9)
    mix(buf, thump(random.uniform(110, 140), 60, 0.1), 0.0, 0.6)
    mix(buf, bones(0.35, random.randint(4, 7), spread=lambda: random.random() ** 2.5), 0.0, 1.0)
    return reverb(buf, 0.8, 0.12, 0.3)


def skeleton_death():
    # Le squelette s'effondre : le corps touche le sol, puis les os rebondissent et s'éparpillent.
    buf = zeros(1.8)
    mix(buf, noise(0.08, lambda k: 1800 - 1000 * k, 1.2, lambda t, k: math.exp(-4 * k)), 0.0, 0.7)
    mix(buf, bones(0.4, 6, spread=lambda: random.random() ** 2), 0.0, 0.8)
    fall = random.uniform(0.25, 0.35)
    mix(buf, thump(95, 45, 0.2, 2.2), fall, 0.9)
    mix(buf, gravel(0.3, 10, spread=lambda: random.random() ** 2), fall, 0.8)
    mix(buf, bones(1.1, 26, spread=lambda: random.random() ** 1.8, gain=(0.1, 0.5)), fall, 1.0)
    # Dernier souffle : l'air qui quitte la cage thoracique vide.
    mix(buf, noise(0.9, lambda k: 700 - 350 * k, 2.0, lambda t, k: math.sin(math.pi * k) * (1 - k)), 0.05, 0.25)
    return reverb(buf, 1.0, 0.2, 0.6)


def skeleton_spawn():
    # La terre se fend, gronde, puis le squelette s'en extrait dans un cliquetis d'os.
    d = 2.2
    buf = zeros(d)
    mix(buf, rumble(1.6, 260, lambda t, k: math.sin(math.pi * min(1.0, k * 1.2)) ** 1.5, 0.006), 0.0, 1.6)
    mix(buf, gravel(1.6, 40, spread=lambda: math.sqrt(random.random())), 0.1, 1.0)
    mix(buf, noise(0.07, lambda k: 1500 - 900 * k, 1.0, lambda t, k: math.exp(-4 * k)), 0.7, 0.9)
    mix(buf, thump(80, 40, 0.2), 0.7, 0.7)
    mix(buf, bones(1.0, 16, spread=lambda: random.random() ** 1.3, gain=(0.1, 0.35)), 0.8, 1.0)
    return reverb(buf, 1.1, 0.25, 0.6)


def shield_block():
    buf = zeros(0.8)
    mix(buf, knock(random.uniform(330, 380), 0.15, 12), 0.0, 0.9)
    mix(buf, knock(random.uniform(520, 560), 0.12, 10), 0.0, 0.5)
    mix(buf, thump(130, 65, 0.1, 2.0), 0.0, 0.8)
    mix(buf, click(3000, 2, 0.01), 0.0, 0.5)
    # Cerclage de fer : partiels inharmoniques graves, très amortis (pas une cloche).
    mix(buf, tone(lambda k: random.uniform(185, 200), 0.4, lambda t, k: math.exp(-14 * t), base.DARK_METAL), 0.0, 0.25)
    return reverb(buf, 0.9, 0.15, 0.3)


def parry():
    # Parade parfaite : joue par-dessus le blocage (shield_block, lancé en même temps par le jeu) : impact plus
    # lourd et onde grave qui fait vaciller l'attaquant.
    buf = zeros(1.4)
    mix(buf, click(2400, 1.5, 0.015), 0.0, 0.5)
    mix(buf, thump(70, 35, 0.35, 2.5), 0.0, 1.0)
    mix(buf, tone(lambda k: 150 * (1 - 0.15 * k), 0.9, lambda t, k: min(1.0, t * 400) * math.exp(-5 * t), base.DARK_METAL), 0.0, 0.3)
    mix(buf, noise(0.5, lambda k: 500 + 1200 * k, 1.2, lambda t, k: math.sin(math.pi * k) * (1 - k)), 0.03, 0.35)
    return reverb(buf, 1.2, 0.3, 0.8)


# ------------------------------------------------------------------ joueur

def player_hurt():
    buf = zeros(0.6)
    mix(buf, thump(random.uniform(120, 150), 70, 0.1, 2.0), 0.0, 0.8)
    mix(buf, noise(0.12, lambda k: 1300, 1.0, lambda t, k: math.exp(-5 * k)), 0.0, 0.4)
    # Souffle coupé : grognement bref, très soufflé, sans hauteur marquée.
    f0 = random.uniform(115, 140)
    grunt = v4.voice(lambda k: f0 * (1 - 0.2 * k), lambda k: ("e", "a", k), 0.2, breath=0.8, rough=0.4, jitter=0.03)
    mix(buf, envelope(prev.saturate(grunt, 1.5), lambda t, k: min(1.0, t * 60) * (1 - k) ** 1.5), 0.02, 0.35)
    return reverb(buf, 0.8, 0.12, 0.3)


def player_death():
    buf = zeros(2.2)
    exhale = v4.voice(lambda k: 105 * (1 - 0.35 * k), lambda k: ("a", "o", k), 1.0, breath=0.85, rough=0.5, jitter=0.03)
    mix(buf, envelope(exhale, lambda t, k: min(1.0, t * 20) * (1 - k) ** 1.3), 0.0, 0.4)
    mix(buf, thump(85, 40, 0.25, 2.2), 0.45, 1.0)
    mix(buf, noise(0.25, lambda k: 1100, 0.9, lambda t, k: math.exp(-4 * k)), 0.45, 0.4)
    mix(buf, grains(0.5, 8, (2000, 4200), (0.005, 0.015), (0.1, 0.3), q=10, spread=lambda: random.random() ** 2), 0.46, 1.0)
    # Le monde s'éloigne : grave qui se retire.
    mix(buf, rumble(1.4, 180, lambda t, k: math.sin(math.pi * k) * (1 - k), 0.005), 0.4, 1.0)
    return reverb(buf, 1.4, 0.35, 1.0)


def respawn():
    # Nyxessa rend la vie : aspiration inversée, grave qui monte, éclat sourd à l'arrivée.
    buf = zeros(2.0)
    mix(buf, v4.reverse_swell(1.1, 300, 1200), 0.0, 0.8)
    mix(buf, noise(1.1, lambda k: 90 + 260 * k ** 2, 1.5, lambda t, k: k ** 2), 0.0, 1.2)
    mix(buf, thump(110, 55, 0.25, 2.0), 1.1, 0.9)
    mix(buf, grains(0.6, 14, (1800, 4200), (0.004, 0.012), (0.05, 0.15), q=5, spread=lambda: random.random() ** 2), 1.1, 1.0)
    mix(buf, noise(0.6, lambda k: 800 - 400 * k, 1.2, lambda t, k: (1 - k) ** 2), 1.1, 0.3)
    return reverb(buf, 1.3, 0.35, 0.8)


def dodge():
    buf = zeros(0.5)
    mix(buf, whoosh(0.32, 500, 1600, 0.9, 0.7), 0.0, 1.0)
    mix(buf, noise(0.2, lambda k: 2500, 0.8, lambda t, k: math.sin(math.pi * k) ** 2), 0.05, 0.3)
    return reverb(buf, 0.7, 0.08, 0.2)


def land():
    buf = zeros(0.5)
    mix(buf, thump(random.uniform(85, 105), 45, 0.12, 2.0), 0.0, 1.0)
    mix(buf, gravel(0.25, 8, spread=lambda: random.random() ** 2), 0.0, 0.8)
    return reverb(buf, 0.7, 0.08, 0.2)


def water_step():
    buf = zeros(0.6)
    mix(buf, noise(0.15, lambda k: random.uniform(700, 1100), 1.2, lambda t, k: math.exp(-5 * k)), 0.0, 0.8)
    mix(buf, noise(0.3, lambda k: 1800, 0.8, lambda t, k: math.sin(math.pi * k) * (1 - k)), 0.02, 0.35)
    # Gouttes : petits claquements graves (glouglou retenu, sans chant).
    for _ in range(random.randint(2, 4)):
        f = random.uniform(300, 520)
        drop = tone(lambda k, f=f: f * (1 + 0.6 * k), 0.035, lambda t, k: math.sin(math.pi * k) * (1 - k))
        mix(buf, drop, random.uniform(0.03, 0.25), 0.18)
    return reverb(buf, 0.8, 0.15, 0.2)


# ------------------------------------------------------------------ compétences

def knight_bash():
    buf = zeros(1.0)
    mix(buf, whoosh(0.15, 400, 1200, 1.0, 0.5), 0.0, 0.4)
    mix(buf, thump(80, 38, 0.25, 2.5), 0.12, 1.0)
    mix(buf, knock(300, 0.18, 10), 0.12, 0.8)
    mix(buf, knock(470, 0.14, 9), 0.12, 0.5)
    mix(buf, tone(lambda k: 170, 0.4, lambda t, k: math.exp(-12 * t), base.DARK_METAL), 0.12, 0.25)
    mix(buf, bones(0.4, 8, spread=lambda: random.random() ** 2), 0.14, 0.8)
    return reverb(buf, 1.0, 0.2, 0.5)


def knight_charge():
    # Elan : piétinement lourd qui accélère, cliquetis d'armure, souffle qui monte.
    d = 1.3
    buf = zeros(d)
    t = 0.0
    step = 0.28
    while t < 1.0:
        mix(buf, thump(90, 50, 0.1, 2.0), t, 0.7)
        mix(buf, grains(0.12, 3, (2200, 4200), (0.004, 0.01), (0.1, 0.25), q=10), t, 1.0)
        t += step
        step = max(0.13, step * 0.82)
    mix(buf, noise(1.1, lambda k: 300 + 900 * k, 1.1, lambda t, k: k ** 1.5), 0.0, 0.6)
    return reverb(buf, 1.0, 0.15, 0.4)


def knight_heal():
    # Soin : inspiration profonde, chaleur grave qui monte et bat doucement, sans note.
    d = 1.8
    buf = zeros(d)
    mix(buf, noise(0.6, lambda k: 1100 + 400 * k, 1.0, lambda t, k: math.sin(math.pi * k) ** 1.5), 0.0, 0.35)
    hum = prev.saturate([a + b for a, b in zip(
        tone(lambda k: 82, 1.5, lambda t, k: math.sin(math.pi * k) ** 1.2),
        tone(lambda k: 83.3, 1.5, lambda t, k: math.sin(math.pi * k) ** 1.2))], 1.4)
    mix(buf, lowpass(hum, 600), 0.2, 0.4)
    mix(buf, rumble(1.5, 400, lambda t, k: math.sin(math.pi * k) ** 2, 0.008), 0.2, 0.7)
    mix(buf, v4.reverse_swell(0.7, 500, 1400), 0.2, 0.3)
    return reverb(buf, 1.3, 0.35, 0.8)


def mage_ignite():
    # Embrasement : le feu prend d'un coup (aspiration, souffle qui s'enflamme, braises).
    d = 1.4
    buf = zeros(d)
    mix(buf, v4.reverse_swell(0.3, 400, 1200), 0.0, 0.5)
    roar = fire_roar(1.1, 1100)
    mix(buf, envelope(roar, lambda t, k: min(1.0, t * 25) * (1 - k) ** 1.2), 0.28, 1.5)
    mix(buf, noise(1.0, lambda k: 600 + 1800 * math.sin(math.pi * min(1, k * 1.8)), 1.0, lambda t, k: (1 - k) ** 2), 0.28, 0.35)
    mix(buf, thump(70, 40, 0.2), 0.28, 0.5)
    mix(buf, v4.fire_crackle(1.1, 40), 0.28, 0.6)
    return reverb(buf, 1.0, 0.2, 0.5)


def mage_flame_cone_loop():
    d = 3.0
    n = int(d * RATE)
    jet = bandpass(white(n), lambda k: 1600, 0.45)
    flutter = loop_cycles(d, 17)
    slow = loop_cycles(d, 1.3)
    buf = envelope(jet, lambda t, k: 0.55 + 0.25 * math.sin(2 * math.pi * flutter * t) + 0.2 * math.sin(2 * math.pi * slow * t))
    buf = [s * 0.5 for s in buf]
    mix(buf, [s * 1.4 for s in fire_roar(d, 1000)], 0.0, 1.0)
    mix(buf, v4.fire_crackle(d, 45, (0.1, 0.45)), 0.0, 1.0)
    return prev.loopable(buf[:n], 0.5)


def mage_avatar():
    # Avatar du feu : grondement qui enfle, colonne de flammes qui s'élève, souffle d'embrasement.
    d = 2.8
    buf = zeros(d)
    mix(buf, rumble(2.0, 220, lambda t, k: k ** 1.5, 0.006), 0.0, 1.6)
    mix(buf, noise(2.0, lambda k: 200 + 1400 * k ** 2, 0.9, lambda t, k: k ** 2), 0.0, 0.6)
    mix(buf, envelope(fire_roar(1.6, 1300), lambda t, k: min(1.0, t * 20) * (1 - k) ** 1.1), 1.6, 1.8)
    mix(buf, thump(60, 32, 0.4, 2.5), 1.6, 1.0)
    mix(buf, v4.fire_crackle(1.4, 60, (0.15, 0.6)), 1.6, 0.8)
    return reverb(buf, 1.5, 0.35, 1.0)


def ranger_focus():
    # Concentration : inspiration retenue, l'arc qui se tend en grinçant.
    d = 1.0
    buf = zeros(d)
    mix(buf, noise(0.5, lambda k: 1300 + 300 * k, 1.1, lambda t, k: math.sin(math.pi * k) ** 1.2), 0.0, 0.4)
    mix(buf, creak(0.6, lambda k: 25 + 60 * k, 620, 6), 0.15, 0.9)
    mix(buf, noise(0.4, lambda k: 180, 1.2, lambda t, k: math.sin(math.pi * k)), 0.3, 0.4)
    return reverb(buf, 0.9, 0.15, 0.4)


def arrow_rain():
    # Nuée de flèches : sifflements qui descendent et se rapprochent, puis impacts en pluie.
    d = 3.0
    buf = zeros(d)
    for _ in range(16):
        at = random.uniform(0.0, 0.5)
        f = random.uniform(1800, 2800)
        w = noise(0.7, lambda k, f=f: f * (1 - 0.35 * k), 5, lambda t, k: k ** 1.5)
        mix(buf, w, at, random.uniform(0.15, 0.3))
    for _ in range(22):
        at = random.uniform(0.65, 1.8)
        mix(buf, thump(random.uniform(160, 230), 80, 0.05, 2.0), at, random.uniform(0.2, 0.5))
        mix(buf, click(random.uniform(800, 1500), 2, 0.015), at, random.uniform(0.15, 0.35))
    mix(buf, gravel(1.2, 20, gain=(0.05, 0.2)), 0.7, 1.0)
    return reverb(buf, 1.1, 0.25, 0.5)


def smoke_bomb():
    # Bombe fumigène : pot d'argile qui éclate, souffle qui s'échappe, nuage qui s'étale.
    d = 2.6
    buf = zeros(d)
    mix(buf, thump(140, 60, 0.1, 2.0), 0.0, 0.8)
    mix(buf, grains(0.25, 14, (1500, 4500), (0.004, 0.015), (0.2, 0.5), q=6, spread=lambda: random.random() ** 2), 0.0, 1.0)
    mix(buf, noise(2.3, lambda k: 2200 - 1200 * k, 0.6, lambda t, k: min(1.0, t * 30) * (1 - k) ** 1.8), 0.02, 0.8)
    mix(buf, rumble(1.8, 250, lambda t, k: min(1.0, t * 20) * (1 - k) ** 1.5), 0.02, 0.8)
    return reverb(buf, 1.1, 0.25, 0.6)


def viking_roar():
    d = 2.0
    buf = zeros(d)

    def contour(k):
        if k < 0.15:
            return 95 + 45 * (k / 0.15) ** 0.6
        if k < 0.6:
            return 140 * (1 + 0.03 * math.sin(k * 45))
        return 140 * (1 - 0.35 * ((k - 0.6) / 0.4) ** 1.2)

    length = 1.4
    for octave, gain, sub, rough in ((1.0, 1.0, 0.35, 0.6), (0.5, 0.9, 0.6, 0.8), (0.25, 0.35, 0.5, 0.9)):
        v = v4.voice(lambda k, o=octave: contour(k) * o,
                     lambda k: ("a", "o", max(0.0, (k - 0.5) * 2)) if k > 0.5 else ("o", "a", min(1.0, k * 4)),
                     length, breath=0.4, rough=rough, jitter=0.035, vib=(5.5, 0.02), subharmonic=sub)
        v = prev.saturate(v, 3.0)
        mix(buf, envelope(v, lambda t, k: min(1.0, t * 12) * (1 - k) ** 0.8), 0.0, gain)
    mix(buf, rumble(1.4, 200, lambda t, k: math.sin(math.pi * k)), 0.0, 0.8)
    return reverb(buf, 1.5, 0.4, 1.0)


def viking_leap_land():
    d = 1.8
    buf = zeros(d)
    mix(buf, thump(55, 28, 0.45, 3.0), 0.0, 1.2)
    mix(buf, noise(0.08, lambda k: 1400 - 800 * k, 1.0, lambda t, k: math.exp(-4 * k)), 0.0, 1.0)
    mix(buf, gravel(1.0, 40, spread=lambda: random.random() ** 2, gain=(0.15, 0.5)), 0.0, 1.0)
    mix(buf, rumble(1.2, 200, lambda t, k: (1 - k) ** 1.5, 0.006), 0.0, 1.2)
    mix(buf, noise(0.8, lambda k: 900 - 500 * k, 1.0, lambda t, k: math.sin(math.pi * k) * (1 - k)), 0.02, 0.4)
    return reverb(buf, 1.3, 0.3, 0.8)


def whirlwind_loop():
    d = 2.4
    n = int(d * RATE)
    spin = loop_cycles(d, 2.5)
    air = bandpass(white(n), lambda k: 900 + 700 * math.sin(2 * math.pi * spin * k * d), 1.0)
    buf = envelope(air, lambda t, k: 0.25 + 0.75 * max(0.0, math.sin(2 * math.pi * spin * t)) ** 2)
    mix(buf, envelope(lowpass(v4.brown(n, 0.02), 350), lambda t, k: 0.6 + 0.4 * math.sin(2 * math.pi * spin * t + 1.0)), 0.0, 0.6)
    return prev.loopable(buf[:n], 0.3)


# ------------------------------------------------------------------ nécromancien

def necro_summon():
    # Invocation (3 s d'incantation) : chuchotements qui montent, grave qui enfle, la terre qui cède à la fin.
    d = 3.6
    buf = zeros(d)
    random_state = random.getstate()
    whisp = v6.whisperer(2.8, 0.3)
    random.setstate(random_state)
    mix(buf, envelope(whisp, lambda t, k: k ** 0.7), 0.0, 0.8)
    mix(buf, envelope(v6.whisperer(2.8, 0.6), lambda t, k: k), 0.1, 0.6)
    mix(buf, rumble(3.0, 200, lambda t, k: k ** 2, 0.006), 0.0, 1.4)
    mix(buf, noise(2.8, lambda k: 150 + 250 * k, 2.0, lambda t, k: k ** 2), 0.0, 0.5)
    mix(buf, noise(0.08, lambda k: 1500 - 900 * k, 1.0, lambda t, k: math.exp(-4 * k)), 2.9, 0.8)
    mix(buf, thump(70, 35, 0.3, 2.5), 2.9, 1.0)
    mix(buf, gravel(0.7, 24, spread=lambda: random.random() ** 2), 2.9, 1.0)
    return reverb(buf, 1.4, 0.35, 0.8)


def necro_heal_loop():
    # Soin en canal : souffle sombre qui respire, pulsé, avec un murmure lointain.
    d = 4.0
    n = int(d * RATE)
    breath = loop_cycles(d, 0.75)
    buf = envelope(bandpass(v4.brown(n, 0.02), lambda k: 220, 1.4),
                   lambda t, k: 0.45 + 0.55 * math.sin(math.pi * breath * t) ** 2)
    buf = [s * 1.2 for s in buf]
    mix(buf, envelope(noise(d, lambda k: 800, 1.6, lambda t, k: 1.0),
                      lambda t, k: 0.2 + 0.8 * math.sin(math.pi * breath * t + 0.8) ** 4), 0.0, 0.15)
    mix(buf, v6.whisperer(d, 0.8), 0.0, 0.35)
    buf = reverb(buf[:n], 1.2, 0.35, 0.0)
    return prev.loopable(buf, 0.6)


# ------------------------------------------------------------------ monde

def portal_open():
    # Ouverture : la pression monte (aspiration inversée, grave qui s'élève), puis le passage se déchire.
    d = 3.2
    buf = zeros(d)
    mix(buf, v4.reverse_swell(1.3, 250, 900), 0.0, 1.0)
    mix(buf, noise(1.3, lambda k: 60 + 120 * k ** 2, 2.5, lambda t, k: k ** 2), 0.0, 1.5)
    mix(buf, noise(0.1, lambda k: 1800 - 1200 * k, 0.9, lambda t, k: math.exp(-3 * k)), 1.3, 1.0)
    mix(buf, thump(65, 32, 0.45, 2.5), 1.3, 1.1)
    mix(buf, grains(1.2, 40, (1200, 3800), (0.004, 0.014), (0.05, 0.25), q=5, spread=lambda: random.random() ** 1.8), 1.3, 1.0)
    mix(buf, rumble(1.6, 300, lambda t, k: (1 - k) ** 1.2, 0.006), 1.3, 1.2)
    mix(buf, noise(1.5, lambda k: 700 - 300 * k, 1.2, lambda t, k: (1 - k) ** 1.5), 1.35, 0.4)
    return reverb(buf, 1.6, 0.4, 1.2)


def portal_close():
    # Fermeture : le tourbillon s'emballe et s'effondre sur lui-même, dernier claquement sourd.
    d = 2.8
    buf = zeros(d)
    mix(buf, noise(1.2, lambda k: 300 + 1500 * k ** 2, 1.5, lambda t, k: k ** 1.5), 0.0, 0.6)
    mix(buf, rumble(1.2, 250, lambda t, k: k, 0.008), 0.0, 1.0)
    mix(buf, v4.reverse_swell(0.5, 600, 2000), 0.75, 0.9)
    mix(buf, thump(55, 28, 0.4, 3.0), 1.2, 1.2)
    mix(buf, grains(0.6, 16, (1000, 3000), (0.004, 0.012), (0.05, 0.2), q=5, spread=lambda: random.random() ** 2), 1.2, 1.0)
    return reverb(buf, 1.5, 0.35, 1.2)


def portal_pass():
    # Passage d'un joueur (PlayerZone.DepartRpc, 1,1 s) : le corps se défait en gemmes qui crépitent de plus en plus
    # serré, aspirées vers le portail dans un souffle qui monte, puis le portail les avale d'un coup sourd.
    d = 2.4
    buf = zeros(d)
    swallow = 1.1
    mix(buf, grains(swallow, 70, (1400, 4200), (0.003, 0.009), (0.04, 0.16), q=4,
                    spread=lambda: math.sqrt(random.random())), 0.0, 1.0)
    mix(buf, noise(swallow, lambda k: 300 + 1500 * k ** 2, 1.3, lambda t, k: k ** 1.6), 0.0, 0.9)
    mix(buf, noise(swallow, lambda k: 70 + 110 * k, 2.0, lambda t, k: k ** 2), 0.0, 1.0)
    mix(buf, v4.reverse_swell(0.45, 500, 1600), swallow - 0.45, 0.8)
    mix(buf, thump(80, 38, 0.35, 2.4), swallow, 1.0)
    mix(buf, noise(0.6, lambda k: 600 - 350 * k, 1.2, lambda t, k: (1 - k) ** 2), swallow, 0.35)
    return reverb(buf, 1.4, 0.35, 0.9)


def shield_raise():
    # Bouclier de la relique (étape 73) : le dôme de gemmes se lève après l'incantation. Souffle qui monte et s'élargit,
    # grave qui s'installe, crépitement des gemmes qui se figent, puis une tenue sourde qui s'éteint.
    d = 3.2
    buf = zeros(d)
    mix(buf, v4.reverse_swell(1.0, 250, 900), 0.0, 0.8)
    mix(buf, noise(1.4, lambda k: 150 + 700 * k ** 1.5, 1.2, lambda t, k: k ** 1.5), 0.0, 0.9)
    mix(buf, thump(70, 42, 0.5, 2.0), 1.0, 0.9)
    mix(buf, grains(1.4, 60, (1200, 3600), (0.003, 0.01), (0.04, 0.16), q=5, spread=lambda: random.random() ** 1.5), 1.0, 1.0)
    hum = prev.saturate([a + b for a, b in zip(
        tone(lambda k: 65, 1.8, lambda t, k: math.sin(math.pi * k) ** 1.3),
        tone(lambda k: 66.2, 1.8, lambda t, k: math.sin(math.pi * k) ** 1.3))], 1.3)
    mix(buf, lowpass(hum, 500), 1.1, 0.35)
    return reverb(buf, 1.6, 0.4, 1.0)


def shield_hit():
    # Coup sur le dôme : impact vitreux étouffé, onde grave courte, quelques éclats de gemmes.
    buf = zeros(0.9)
    mix(buf, noise(0.05, lambda k: 3200 - 1600 * k, 1.2, lambda t, k: math.exp(-5 * k)), 0.0, 0.7)
    mix(buf, thump(random.uniform(95, 115), 55, 0.18, 2.0), 0.0, 0.9)
    mix(buf, tone(lambda k: random.uniform(200, 230), 0.35, lambda t, k: math.exp(-11 * t), ((1.0, 1.0), (2.41, 0.35), (3.83, 0.12))), 0.0, 0.2)
    mix(buf, grains(0.35, 8, (2000, 4500), (0.003, 0.009), (0.08, 0.25), q=6, spread=lambda: random.random() ** 2), 0.01, 1.0)
    mix(buf, noise(0.5, lambda k: 500 - 200 * k, 1.4, lambda t, k: (1 - k) ** 2), 0.0, 0.3)
    return reverb(buf, 1.2, 0.3, 0.6)


def shield_break():
    # Rupture du dôme : craquement qui court, éclatement, pluie de gemmes qui retombent, grave qui s'effondre.
    d = 2.8
    buf = zeros(d)
    mix(buf, grains(0.5, 18, (1500, 4000), (0.004, 0.012), (0.15, 0.4), q=8, spread=lambda: random.random()), 0.0, 1.0)
    mix(buf, noise(0.12, lambda k: 2200 - 1400 * k, 0.9, lambda t, k: math.exp(-3 * k)), 0.5, 1.0)
    mix(buf, thump(60, 30, 0.5, 2.8), 0.5, 1.2)
    mix(buf, grains(1.8, 120, (1200, 4200), (0.003, 0.01), (0.05, 0.2), q=5, spread=lambda: random.random() ** 0.8), 0.55, 1.0)
    mix(buf, rumble(1.5, 220, lambda t, k: (1 - k) ** 1.5, 0.006), 0.5, 1.1)
    mix(buf, noise(1.2, lambda k: 900 - 500 * k, 1.1, lambda t, k: (1 - k) ** 1.8), 0.55, 0.4)
    return reverb(buf, 1.5, 0.4, 1.2)


def turret_turn_loop():
    # Tête de tourelle qui pivote : grincement de bois et d'engrenages, discret, en boucle tant qu'elle tourne.
    d = 1.6
    n = int(d * RATE)
    rate = loop_cycles(d, 2.0)
    buf = creak(d, lambda k: 34 + 8 * math.sin(2 * math.pi * rate * k * d), 420, 6)
    mix(buf, envelope(bandpass(white(n), lambda k: 900, 2.0), lambda t, k: 0.08 + 0.05 * math.sin(2 * math.pi * rate * t)), 0.0, 1.0)
    mix(buf, envelope(lowpass(v4.brown(n, 0.02), 250), lambda t, k: 0.5), 0.0, 0.5)
    return prev.loopable(buf[:n], 0.25)


def portal_hum_loop():
    # Présence du portail ouvert (3D, entendue de près seulement) : tourbillon grave et grésillement lointain.
    d = 4.0
    n = int(d * RATE)
    swirl = loop_cycles(d, 0.5)
    buf = envelope(bandpass(v4.brown(n, 0.02), lambda k: 140 + 60 * math.sin(2 * math.pi * swirl * k * d), 1.6),
                   lambda t, k: 0.7 + 0.3 * math.sin(2 * math.pi * swirl * t))
    mix(buf, envelope(noise(d, lambda k: 1400 + 500 * math.sin(2 * math.pi * swirl * k * d + 2), 2.5, lambda t, k: 1.0),
                      lambda t, k: 0.5 + 0.5 * math.sin(2 * math.pi * swirl * t + 2)), 0.0, 0.12)
    mix(buf, grains(d, 30, (1500, 3500), (0.003, 0.01), (0.02, 0.07), q=5), 0.0, 1.0)
    return prev.loopable(buf[:n], 0.8)


def relic_hit():
    # Nyxessa frappée : choc sourd dans une pierre massive, craquelure, onde grave. Court, pas de résonance tenue.
    buf = zeros(1.2)
    mix(buf, thump(90, 45, 0.25, 2.5), 0.0, 1.0)
    mix(buf, noise(0.05, lambda k: 2600 - 1200 * k, 1.3, lambda t, k: math.exp(-5 * k)), 0.0, 0.7)
    mix(buf, grains(0.3, 6, (2500, 5000), (0.003, 0.008), (0.1, 0.3), q=7, spread=lambda: random.random() ** 2), 0.01, 1.0)
    mix(buf, tone(lambda k: 118, 0.5, lambda t, k: math.exp(-9 * t), ((1.0, 1.0), (2.76, 0.3), (5.4, 0.1))), 0.0, 0.2)
    mix(buf, noise(0.6, lambda k: 400 - 150 * k, 1.5, lambda t, k: (1 - k) ** 2), 0.0, 0.35)
    return reverb(buf, 1.2, 0.3, 0.6)


def relic_pulse():
    # Impulsion de la relique (ouverture du portail) : souffle profond qui part du cœur et s'élargit.
    d = 2.8
    buf = zeros(d)
    mix(buf, v4.reverse_swell(0.5, 200, 700), 0.0, 0.7)
    mix(buf, thump(50, 30, 0.8, 1.8), 0.5, 1.2)
    mix(buf, noise(1.8, lambda k: 250 + 900 * k, 1.1, lambda t, k: min(1.0, t * 15) * (1 - k) ** 1.4), 0.5, 0.7)
    mix(buf, grains(1.5, 24, (1500, 3500), (0.003, 0.01), (0.03, 0.12), q=5, spread=lambda: math.sqrt(random.random())), 0.55, 1.0)
    return reverb(buf, 1.6, 0.4, 1.2)


def nightfall():
    # Tombée de la nuit : vent qui se lève, cor de corne lointain et grave, la brume qui arrive.
    d = 5.0
    buf = zeros(d)
    mix(buf, envelope(bandpass(v4.brown(int(4.5 * RATE), 0.02), lambda k: 300 + 250 * math.sin(k * 5), 1.2),
                      lambda t, k: math.sin(math.pi * k) ** 1.2), 0.0, 1.3)
    horn = v4.voice(lambda k: 73 * (1 + 0.012 * math.sin(k * 7)) * (1 - 0.06 * max(0.0, k - 0.75) * 4), lambda k: ("o", "ou", k),
                    2.6, breath=0.3, rough=0.25, jitter=0.01, vib=(4.0, 0.006))
    horn = prev.saturate(horn, 2.2)
    horn = lowpass(lowpass(horn, 1200), 1500)
    mix(buf, envelope(horn, lambda t, k: min(1.0, t / 0.5) * (1 - k) ** 0.8), 0.6, 0.55)
    buf = reverb(buf, 2.0, 0.55, 2.0)
    return buf


def dawn():
    # L'aube : le vent retombe, un souffle clair et large se lève, grave chaud qui s'ouvre.
    d = 4.0
    buf = zeros(d)
    mix(buf, noise(3.5, lambda k: 400 + 1200 * k, 0.8, lambda t, k: math.sin(math.pi * k) ** 1.5), 0.0, 0.6)
    mix(buf, rumble(3.2, 300, lambda t, k: math.sin(math.pi * k) ** 2, 0.008), 0.0, 0.8)
    mix(buf, v4.reverse_swell(1.4, 600, 1800), 0.3, 0.5)
    mix(buf, grains(2.5, 30, (2000, 4500), (0.003, 0.01), (0.02, 0.08), q=5, spread=lambda: math.sqrt(random.random())), 1.2, 1.0)
    return reverb(buf, 1.8, 0.45, 1.5)


def dawn_vaporize():
    # Un squelette de vague se vaporise : les os se désagrègent en poussière, souffle qui s'élève.
    d = 2.0
    buf = zeros(d)
    mix(buf, bones(0.6, 10, spread=lambda: random.random() ** 1.5, gain=(0.1, 0.35)), 0.0, 1.0)
    mix(buf, grains(1.6, 90, (900, 3500), (0.003, 0.01), (0.03, 0.1), q=2, spread=lambda: math.sqrt(random.random()) * 0.9), 0.0, 1.0)
    mix(buf, noise(1.6, lambda k: 300 + 900 * k, 1.1, lambda t, k: math.sin(math.pi * k) ** 1.2), 0.1, 0.6)
    mix(buf, rumble(1.0, 200, lambda t, k: (1 - k) ** 2), 0.0, 0.5)
    return reverb(buf, 1.2, 0.3, 0.8)


def crystal_light():
    # Cristal de passage qui s'allume : souffle d'énergie bref, craquement de givre, grave étouffé.
    d = 1.2
    buf = zeros(d)
    mix(buf, v4.reverse_swell(0.25, 800, 2200), 0.0, 0.5)
    mix(buf, grains(0.5, 12, (1800, 4000), (0.003, 0.01), (0.1, 0.3), q=4, spread=lambda: random.random() ** 2), 0.22, 1.0)
    mix(buf, thump(120, 70, 0.12, 1.5), 0.22, 0.35)
    mix(buf, noise(0.6, lambda k: 900 - 300 * k, 1.5, lambda t, k: (1 - k) ** 2), 0.22, 0.3)
    return reverb(buf, 1.1, 0.35, 0.8)


def trap_spikes():
    buf = zeros(0.9)
    mix(buf, noise(0.12, lambda k: 2400 + 1500 * k, 3, lambda t, k: math.sin(math.pi * k)), 0.0, 0.7)
    mix(buf, thump(100, 50, 0.12, 2.2), 0.1, 1.0)
    mix(buf, grains(0.2, 6, (1800, 3500), (0.004, 0.012), (0.2, 0.4), q=10), 0.1, 1.0)
    mix(buf, gravel(0.2, 6), 0.1, 0.6)
    return reverb(buf, 0.9, 0.2, 0.4)


def trap_flames():
    d = 1.8
    buf = zeros(d)
    mix(buf, noise(0.15, lambda k: 800, 1.0, lambda t, k: math.sin(math.pi * k)), 0.0, 0.5)
    mix(buf, envelope(fire_roar(1.5, 1200), lambda t, k: min(1.0, t * 30) * (1 - k) ** 1.3), 0.08, 1.6)
    mix(buf, noise(1.4, lambda k: 1500, 0.5, lambda t, k: min(1.0, t * 30) * (1 - k) ** 2), 0.08, 0.35)
    mix(buf, v4.fire_crackle(1.4, 45), 0.1, 0.6)
    return reverb(buf, 1.0, 0.25, 0.5)


def structure_place():
    # Pose d'une tour ou d'un mur : coups de maillet sur du bois, planche qui se cale.
    buf = zeros(1.0)
    for i, at in enumerate((0.0, 0.22)):
        mix(buf, knock(random.uniform(260, 320), 0.15, 11), at, 0.9)
        mix(buf, thump(110, 60, 0.1, 2.0), at, 0.6)
    mix(buf, creak(0.2, lambda k: 70, 480, 5), 0.4, 0.3)
    mix(buf, gravel(0.2, 5), 0.25, 0.5)
    return reverb(buf, 0.9, 0.15, 0.4)


def structure_hit():
    buf = zeros(0.6)
    mix(buf, knock(random.uniform(240, 330), 0.18, 10), 0.0, 1.0)
    mix(buf, knock(random.uniform(500, 650), 0.1, 9), 0.0, 0.4)
    mix(buf, thump(120, 60, 0.1, 2.0), 0.0, 0.7)
    mix(buf, grains(0.25, 5, (1500, 3000), (0.004, 0.012), (0.1, 0.25), q=6, spread=lambda: random.random() ** 2), 0.0, 1.0)
    return reverb(buf, 0.8, 0.15, 0.3)


def structure_destroyed():
    d = 2.2
    buf = zeros(d)
    mix(buf, noise(0.12, lambda k: 1200 - 700 * k, 0.9, lambda t, k: math.exp(-3 * k)), 0.0, 1.0)
    mix(buf, thump(60, 30, 0.4, 3.0), 0.0, 1.1)
    mix(buf, creak(0.3, lambda k: 40 - 25 * k, 420, 5), 0.02, 0.5)
    for _ in range(14):
        at = random.random() ** 1.6 * 1.3
        mix(buf, knock(random.uniform(220, 700), 0.12, 9), at, random.uniform(0.2, 0.6))
    mix(buf, gravel(1.5, 30, spread=lambda: random.random() ** 1.8), 0.0, 1.0)
    mix(buf, rumble(1.2, 220, lambda t, k: (1 - k) ** 1.5), 0.0, 0.9)
    return reverb(buf, 1.2, 0.3, 0.7)


def chest_open():
    buf = zeros(1.2)
    mix(buf, click(2600, 5, 0.012), 0.0, 0.5)
    mix(buf, creak(0.55, lambda k: 30 + 25 * math.sin(math.pi * k), 560, 6), 0.05, 0.8)
    mix(buf, knock(230, 0.2, 10), 0.62, 0.8)
    mix(buf, thump(95, 50, 0.12, 2.0), 0.62, 0.6)
    return reverb(buf, 1.0, 0.25, 0.5)


# ------------------------------------------------------------------ catalogue
# nom -> (fonction, boucle ?, nombre de variantes, graine)

CATALOG = {
    "bow_shot": (bow_shot, False, 3, 701),
    "crossbow_shot": (crossbow_shot, False, 3, 702),
    "skeleton_bow_shot": (skeleton_bow_shot, False, 3, 703),
    "tower_shot": (tower_shot, False, 2, 704),
    "arrow_impact": (arrow_impact, False, 3, 705),
    "skeleton_hit": (skeleton_hit, False, 3, 706),
    "skeleton_death": (skeleton_death, False, 3, 707),
    "skeleton_spawn": (skeleton_spawn, False, 2, 708),
    "shield_block": (shield_block, False, 3, 709),
    "parry": (parry, False, 1, 710),
    "player_hurt": (player_hurt, False, 3, 711),
    "player_death": (player_death, False, 1, 712),
    "respawn": (respawn, False, 1, 713),
    "dodge": (dodge, False, 2, 714),
    "land": (land, False, 3, 715),
    "water_step": (water_step, False, 3, 716),
    "knight_bash": (knight_bash, False, 1, 720),
    "knight_charge": (knight_charge, False, 1, 721),
    "knight_heal": (knight_heal, False, 1, 722),
    "mage_ignite": (mage_ignite, False, 1, 723),
    "mage_flame_cone_loop": (mage_flame_cone_loop, True, 1, 724),
    "mage_avatar": (mage_avatar, False, 1, 725),
    "ranger_focus": (ranger_focus, False, 1, 726),
    "arrow_rain": (arrow_rain, False, 1, 727),
    "smoke_bomb": (smoke_bomb, False, 1, 728),
    "viking_roar": (viking_roar, False, 2, 729),
    "viking_leap_land": (viking_leap_land, False, 1, 730),
    "whirlwind_loop": (whirlwind_loop, True, 1, 731),
    "necro_summon": (necro_summon, False, 1, 740),
    "necro_heal_loop": (necro_heal_loop, True, 1, 741),
    "portal_open": (portal_open, False, 1, 750),
    "portal_close": (portal_close, False, 1, 751),
    "portal_hum_loop": (portal_hum_loop, True, 1, 752),
    "portal_pass": (portal_pass, False, 1, 765),
    "bow_shot_v2": (bow_shot_v2, False, 3, 766),
    "bow_shot_charged": (bow_shot_charged, False, 2, 767),
    "shield_raise": (shield_raise, False, 1, 768),
    "shield_hit": (shield_hit, False, 3, 769),
    "shield_break": (shield_break, False, 1, 770),
    "turret_turn_loop": (turret_turn_loop, True, 1, 771),
    "relic_hit": (relic_hit, False, 2, 753),
    "relic_pulse": (relic_pulse, False, 1, 754),
    "nightfall": (nightfall, False, 1, 755),
    "dawn": (dawn, False, 1, 756),
    "dawn_vaporize": (dawn_vaporize, False, 2, 757),
    "crystal_light": (crystal_light, False, 2, 758),
    "trap_spikes": (trap_spikes, False, 1, 759),
    "trap_flames": (trap_flames, False, 1, 760),
    "structure_place": (structure_place, False, 1, 761),
    "structure_hit": (structure_hit, False, 2, 762),
    "structure_destroyed": (structure_destroyed, False, 1, 763),
    "chest_open": (chest_open, False, 1, 764),
}


def render(job):
    name, variant, out = job
    fn, is_loop, count, seed = CATALOG[name]
    random.seed(seed * 10 + variant)
    buf = fn()
    file = name if count == 1 else "%s_%d" % (name, variant + 1)
    path = os.path.join(out, file + ".wav")
    if is_loop:
        prev.write_raw(path, buf)
    else:
        base.write(path, lin_fade(buf, 0.002, 0.0))
    return file


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    names = sys.argv[2:] or list(CATALOG)
    os.makedirs(out, exist_ok=True)
    jobs = [(n, v, out) for n in names for v in range(CATALOG[n][2])]
    with Pool() as pool:
        pool.map(render, jobs)
