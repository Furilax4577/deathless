"""Sons créés pour Deathless (25 septembre 2026, demande de Quentin : « créer tous les sons de la section À créer »).
Python pur, WAV 44,1 kHz 16 bits mono, aucun échantillon, graines fixes : relancer donne exactement les mêmes fichiers.

Direction sonore, calée sur les effets visuels (Docs/vfx.md) :
- Nyxessa et portail (gemmes vertes) : timbre cristallin, verre et carillons, résonances claires ; partiels
  inharmoniques d'une gemme ou d'un verre (GLASS), gammes de ré ; durées des effets (charge 0,7 s, anneaux de la
  goutte à 0,32 s d'écart amortis en 1,7 s, onde de la ceinture 1,2 s, passage 1,1 s) ;
- jour et nuit, interface : cloches douces, signaux courts et lisibles, jamais agressifs ;
- combat : critique métallique et doré, furtif feutré, alerte sèche, feu couleur feu.
Les armes de trait (arc, arbalète, bander, recharger) sont dans synth_physique.py, dont ce script reprend les briques.

Usage : python -B synth_deathless.py <dossier_sortie> [nom ...]   (sans nom : tout le catalogue ; variantes
<nom>_1.wav, <nom>_2.wav... ; boucles suffixées _loop, écrites sans coupure de fin).
"""
import math
import os
import random
import sys

import synth_sounds2 as base
import synth_sounds3 as prev
import synth_sounds4 as v4
from synth_sounds2 import RATE, zeros, mix, white, bandpass, lowpass, envelope, tone, reverb
from synth_physique import highpass, modal, strike, recede, rustle, level_peak, LOUDNESS_DB


# ------------------------------------------------------------------ briques

def hz(midi):
    """Fréquence d'une note MIDI (la 4 = 69 = 440 Hz)."""
    return 440.0 * 2 ** ((midi - 69) / 12)


D3, A3, D4, E4, FS4, GS4, A4, B4, C5, D5, E5, FS5, GS5, A5, B5, D6, FS6, A6, D7 = (
    50, 57, 62, 64, 66, 68, 69, 71, 72, 74, 76, 78, 80, 81, 83, 86, 90, 93, 98)
PENTA_HAUT = [D6, 88, FS6, A6, 95, D7]  # ré majeur pentatonique, octaves 6 et 7 : scintillement des gemmes

GLASS = ((1.0, 1.0), (2.32, 0.45), (4.25, 0.22), (6.63, 0.1))       # gemme, verre : partiels inharmoniques
BELL = ((0.5, 0.3), (1.0, 1.0), (2.0, 0.45), (3.0, 0.2), (4.2, 0.08))  # cloche douce, presque harmonique
GOLD = ((1.0, 1.0), (2.76, 0.6), (5.4, 0.45), (8.93, 0.25))         # plaque de métal clair (critique)


def ping(freq, t60, partials=GLASS, attack=0.0015, detune=0.0):
    """Tintement : partiels sinusoïdaux amortis (les aigus s'éteignent plus vite), attaque de `attack` s (dureté)."""
    n = int(min(t60 * 1.1, 8.0) * RATE)
    out = [0.0] * n
    for ratio, amp in partials:
        f = freq * ratio * (1 + random.uniform(-detune, detune))
        if f >= RATE * 0.45:
            continue
        tl = t60 / (1 + 0.6 * (ratio - 1)) if ratio > 1 else t60
        w = 2 * math.pi * f / RATE
        ph = random.random() * 2 * math.pi
        dec = math.exp(-6.91 / (tl * RATE))
        g = amp
        for i in range(n):
            out[i] += g * math.sin(w * i + ph)
            g *= dec
            if g < 1e-4:
                break
    a = max(1, int(attack * RATE))
    for i in range(min(a, n)):
        out[i] *= i / a
    return out


def sparkle(seconds, count, notes=PENTA_HAUT, t60=(0.08, 0.3), amp=(0.05, 0.2), when=None, pitch_of=None):
    """Scintillement de gemmes : `count` petits tintements ; when(r) place chacun dans le temps (r au hasard de 0 à 1),
    pitch_of(k) multiplie la hauteur selon l'instant k (0 à 1)."""
    buf = zeros(seconds)
    for _ in range(count):
        r = random.random()
        k = when(r) if when else r
        f = hz(random.choice(notes)) * (pitch_of(k) if pitch_of else 1.0) * random.uniform(0.995, 1.005)
        mix(buf, ping(f, random.uniform(*t60), GLASS, 0.0008), k * seconds * 0.97, random.uniform(*amp))
    return buf


def air(seconds, f_of, q, env):
    """Souffle : bruit filtré dont la bande suit f_of(k), sous l'enveloppe env(k)."""
    sig = bandpass(white(int(seconds * RATE)), f_of, q)
    n = len(sig)
    return [s * env(i / max(1, n - 1)) for i, s in enumerate(sig)]


def sub(f0, f1, seconds, drive=1.2):
    """Poussée grave : sinus qui glisse de f0 à f1, attaque douce, saturé légèrement."""
    sig = tone(lambda k: f0 + (f1 - f0) * k, seconds, lambda t, k: min(1.0, t * 150) * (1 - k) ** 2)
    return prev.saturate(sig, drive)


def bubble(f0, rise, seconds=0.08):
    """Goutte d'eau : bulle dont la hauteur monte vite (elle se referme), décroissance rapide."""
    return tone(lambda k: f0 * (1 + rise * k ** 0.7), seconds, lambda t, k: min(1.0, t * 1500) * math.exp(-5 * k))


def trimmed(buf, floor=0.03):
    """Retire la fin sous `floor` × la crête (-30 dB par défaut) avant de retourner un son : sinon le son retourné
    commencerait par une longue montée inaudible."""
    m = max(1e-9, max(abs(s) for s in buf))
    end = len(buf)
    while end > 0 and abs(buf[end - 1]) < m * floor:
        end -= 1
    return buf[:end]


def master(buf, seconds, drive=1.0, verb=None):
    """Mastering commun (même principe que synth_physique.finish) : grave < 40 Hz coupé, crêtes arrondies, réverbération
    légère facultative (verb = (taille, mouillé, queue)), fondu de sortie ; normalisation par base.write."""
    buf = buf[:int(seconds * RATE)] + [0.0] * max(0, int(seconds * RATE) - len(buf))
    if verb:
        buf = reverb(buf, *verb)
    buf = highpass(buf, 40)
    peak = max(1e-9, max(abs(s) for s in buf))
    buf = [math.tanh(drive * s / peak) for s in buf]
    fade = int(0.05 * RATE)
    for i in range(min(fade, len(buf))):
        buf[-1 - i] *= i / fade
    return buf


def loop_master(buf, seconds, fade=0.4):
    """Boucle : longueur `seconds` + fondu, fin fondue dans le début (synth_sounds3.loopable), crêtes arrondies."""
    buf = highpass(buf[:int((seconds + fade) * RATE)], 40)
    buf = prev.loopable(buf, fade)
    peak = max(1e-9, max(abs(s) for s in buf))
    return [math.tanh(s / peak) for s in buf]


def cycles(seconds, f):
    """Fréquence arrondie pour tomber juste sur la longueur d'une boucle."""
    return max(1, round(f * seconds)) / seconds


# ------------------------------------------------------------------ Nyxessa et portail

def _charge_body(travel=0.7, d=1.6):
    buf = zeros(d)
    mix(buf, ping(hz(D5), 0.5, GLASS, 0.001), 0.0, 0.3)
    mix(buf, air(travel, lambda k: 500 * 5 ** k, 1.6, lambda k: (0.25 + 0.75 * k) * min(1, k * 20)), 0.0, 0.55)
    mix(buf, tone(lambda k: hz(D4) * 2 ** k, travel, lambda t, k: math.sin(math.pi * min(1, k * 0.6)) ** 2 * 0.6,
                  ((1.0, 1.0), (2.0, 0.3), (3.0, 0.1))), 0.0, 0.35)
    mix(buf, sparkle(travel, 45, when=lambda r: r ** 0.6, pitch_of=lambda k: 0.7 + 0.6 * k, amp=(0.04, 0.14)), 0.0, 1.0)
    arrive = travel
    for note, g in ((D3, 0.8), (A3, 0.6), (D4, 0.4)):
        mix(buf, ping(hz(note), 1.3, GLASS, 0.008), arrive, g)
    mix(buf, sub(90, 50, 0.3), arrive, 0.45)
    mix(buf, air(0.5, lambda k: 1800 - 1200 * k, 0.8, lambda k: (1 - k) ** 2 * min(1, k * 30)), arrive, 0.3)
    mix(buf, sparkle(0.7, 25, when=lambda r: r ** 2, amp=(0.03, 0.1)), arrive, 1.0)
    return buf


def nyxessa_charge():
    # Charge vers le portail (0,7 s) : éclat au cristal, souffle et gemmes qui montent en voyageant, puis impact doux
    # à l'arrivée (accord grave de verre, poussée d'air, gerbe de gemmes).
    return master(_charge_body(), 1.6, verb=(1.0, 0.12, 0.6))


def nyxessa_charge_return():
    # Retour de l'énergie vers Nyxessa : la charge jouée à l'envers (aspirée au portail, descend en voyageant), et une
    # note grave qui se pose doucement au cristal.
    buf = trimmed(_charge_body())
    buf.reverse()
    mix(buf, ping(hz(D4), 0.9, GLASS, 0.02), len(buf) / RATE - 0.02, 0.35)
    return master(buf, len(buf) / RATE + 0.9, verb=(1.0, 0.12, 0.6))


def _drop_body():
    d = 1.9
    buf = zeros(d)
    mix(buf, bubble(random.uniform(210, 240), 1.8, 0.07), 0.0, 0.9)
    mix(buf, sub(95, 60, 0.25, 1.0), 0.0, 0.4)
    for n, (note, g) in enumerate(((A4, 1.0), (FS4, 0.7), (D4, 0.5))):
        at = 0.08 + n * 0.32
        mix(buf, ping(hz(note), 0.95, GLASS, 0.012, 0.002), at, 0.45 * g)
        mix(buf, ping(hz(note + 12), 0.5, GLASS, 0.008, 0.002), at + 0.01, 0.12 * g)
        mix(buf, air(0.45, lambda k, n=n: (1600 - 350 * n) * (1 - 0.5 * k), 1.1,
                     lambda k: math.sin(math.pi * min(1, k * 1.2)) * (1 - k)), at, 0.3 * g)
    return buf


def portal_drop_in():
    # Entrée dans le portail : une goutte grave, puis trois anneaux qui s'élargissent (0,32 s d'écart, de plus en plus
    # graves et faibles) avec un clapotis qui s'étale.
    return master(_drop_body(), 1.9, verb=(0.9, 0.1, 0.5))


def portal_drop_out():
    # Sortie du portail : l'inverse aspiré (les anneaux convergent et se résorbent au centre), puis une petite bosse.
    buf = trimmed(_drop_body())
    buf.reverse()
    mix(buf, bubble(300, 0.8, 0.06), len(buf) / RATE - 0.01, 0.35)
    return master(buf, len(buf) / RATE + 0.3, verb=(0.9, 0.1, 0.5))


def portal_arrive():
    # Arrivée d'un joueur (1,1 s) : bouffée d'air et gerbe de gemmes qui jaillissent, puis gemmes qui convergent en
    # montant, et un accord chaud quand le corps est reconstitué.
    d = 1.6
    buf = zeros(d)
    mix(buf, air(0.4, lambda k: 2600 - 1400 * k, 0.9, lambda k: min(1, k * 25) * (1 - k) ** 2), 0.0, 0.5)
    mix(buf, sparkle(0.35, 35, when=lambda r: r ** 2, amp=(0.05, 0.16)), 0.0, 1.0)
    mix(buf, sparkle(0.6, 40, when=lambda r: r, pitch_of=lambda k: 0.75 + 0.45 * k, amp=(0.03, 0.12)), 0.3, 1.0)
    mix(buf, tone(lambda k: hz(D3), 0.9, lambda t, k: math.sin(math.pi * k) ** 2, ((1.0, 1.0), (2.0, 0.3))), 0.1, 0.25)
    for note, g in ((D4, 0.6), (A4, 0.45), (D5, 0.35)):
        mix(buf, ping(hz(note), 0.9, GLASS, 0.015), 0.9, g)
    return master(buf, d, verb=(1.0, 0.12, 0.5))


def nyxessa_belt_wave():
    # Onde de la ceinture (1,2 s) : une vague de petits tintements fait le tour des gemmes en orbite, sa hauteur suit
    # le tour (monte puis redescend), plus fort quand elle passe de notre côté.
    d = 1.6
    buf = zeros(d)
    notes = [D5, E5, FS5, A5, B5, D6, 88, D6, B5, A5, FS5, E5, D5, B4, A4, B4, D5, E5]
    for n, note in enumerate(notes):
        k = n / (len(notes) - 1)
        mix(buf, ping(hz(note), 0.4, GLASS, 0.002, 0.003), k * 1.2, 0.12 + 0.2 * math.sin(math.pi * k))
    mix(buf, sparkle(1.2, 30, amp=(0.02, 0.07)), 0.0, 1.0)
    mix(buf, air(1.2, lambda k: 2500 + 800 * math.sin(2 * math.pi * k), 1.4, lambda k: math.sin(math.pi * k) ** 2), 0.0, 0.1)
    return master(buf, d, verb=(0.9, 0.1, 0.4))


def nyxessa_recall():
    # Rappel forcé (plus urgent) : trois pulsations dissonantes (triton), aspiration qui monte, puis la traction :
    # accord de verre plus dur, poussée grave et gerbe de gemmes.
    d = 1.8
    buf = zeros(d)
    for n in range(3):
        at = n * 0.17
        mix(buf, ping(hz(D5), 0.3, GLASS, 0.001), at, 0.5)
        mix(buf, ping(hz(GS5), 0.3, GLASS, 0.001), at + 0.004, 0.4)
    mix(buf, v4.reverse_swell(0.9, 400, 2200), 0.12, 0.9)
    mix(buf, tone(lambda k: hz(A3) * 2 ** (k * 1.5), 0.9, lambda t, k: k ** 2, ((1.0, 1.0), (1.5, 0.4))), 0.12, 0.25)
    pull = 1.02
    for note, g in ((D4, 0.7), (GS4, 0.45), (D5, 0.4)):
        mix(buf, ping(hz(note), 0.8, GLASS, 0.002), pull, g)
    mix(buf, sub(110, 45, 0.35, 1.8), pull, 0.6)
    mix(buf, sparkle(0.5, 25, when=lambda r: r ** 2, amp=(0.04, 0.12)), pull, 1.0)
    return master(buf, d, drive=1.3, verb=(1.0, 0.1, 0.4))


def nyxessa_upgrade():
    # Palier amélioré : arpège qui monte en souffle, accord clair de ré majeur appuyé d'un grave, pluie de gemmes.
    d = 2.2
    buf = zeros(d)
    mix(buf, air(0.4, lambda k: 600 * 5 ** k, 1.3, lambda k: k ** 1.5), 0.0, 0.4)
    for n, note in enumerate((D5, FS5, A5, D6)):
        mix(buf, ping(hz(note), 1.0, GLASS, 0.0015), n * 0.085, 0.35)
    top = 0.36
    for note, g in ((D5, 0.5), (FS5, 0.4), (A5, 0.4), (D6, 0.35), (FS6, 0.2)):
        mix(buf, ping(hz(note), 1.6, GLASS, 0.003, 0.002), top, g)
    mix(buf, ping(hz(D3), 1.6, BELL, 0.01), top, 0.5)
    mix(buf, sparkle(1.4, 55, when=lambda r: r ** 1.5, amp=(0.03, 0.1)), top, 1.0)
    return master(buf, d, verb=(1.1, 0.15, 0.6))


def sorcerer_cast_loop():
    # Incantation du bouclier par le sorcier (boucle 3 s, pendant les 3 s de la levée) : bourdon grave qui bat
    # lentement, murmure de voix sans paroles (formants filtrés), éclats de gemmes bleues qui montent.
    d, fade = 3.0, 0.5
    total = d + fade
    n = int(total * RATE)
    trem = cycles(d, 1.5)
    buf = tone(lambda k: hz(D3), total, lambda t, k: 0.7 + 0.3 * math.sin(2 * math.pi * trem * t),
               ((1.0, 1.0), (2.0, 0.35), (3.0, 0.15)))
    mix(buf, tone(lambda k: hz(A3) * 1.004, total, lambda t, k: 0.6 + 0.4 * math.sin(2 * math.pi * trem * t + 2),
                  ((1.0, 1.0), (2.0, 0.25))), 0.0, 0.5)
    syll = lowpass([random.uniform(0, 1) ** 2 for _ in range(n)], 5)
    m = max(syll) or 1
    for f, q, g in ((520, 3.0, 0.9), (1150, 4.0, 0.6), (2400, 5.0, 0.25)):
        voice = bandpass(white(n), lambda k, f=f: f * (1 + 0.1 * math.sin(2 * math.pi * 0.4 * k * total)), q)
        mix(buf, [v * s / m for v, s in zip(voice, syll)], 0.0, g * 0.6)
    mix(buf, sparkle(total, 22, notes=[A5, D6, 88, A6], amp=(0.03, 0.09), t60=(0.2, 0.5)), 0.0, 1.0)
    return loop_master(buf, d, fade)


def nyxessa_destroyed():
    # Destruction de Nyxessa (défaite) : craquement franc, le cristal se brise en éclats qui retombent, accord de verre
    # grave et dissonant qui s'éteint longuement, bourdon qui descend.
    d = 5.5
    buf = zeros(d)
    mix(buf, highpass(air(0.05, lambda k: 3000, 0.5, lambda k: (1 - k) ** 2), 800), 0.0, 1.0)
    mix(buf, sub(60, 32, 1.2, 2.2), 0.0, 1.0)
    for note, g in ((D4, 0.6), (GS4, 0.5), (C5, 0.4), (D3, 0.7)):
        mix(buf, ping(hz(note), 3.2, GLASS, 0.001, 0.004), 0.0, g)
    shards = zeros(2.5)
    for _ in range(160):
        at = random.expovariate(2.2)
        if at < 2.4:
            f = random.uniform(1800, 8500)
            mix(shards, ping(f, random.uniform(0.04, 0.3), GLASS, 0.0003), at, random.uniform(0.03, 0.2) * math.exp(-at))
    mix(buf, shards, 0.02, 1.0)
    mix(buf, tone(lambda k: 73.4 * 2 ** (-k * 0.9), 5.0, lambda t, k: min(1.0, t * 4) * (1 - k) ** 1.5,
                  ((1.0, 1.0), (2.0, 0.4), (3.0, 0.2), (1.01, 0.6))), 0.1, 0.55)
    mix(buf, ping(hz(D3), 4.5, GLASS, 0.3, 0.003), 0.3, 0.4)
    mix(buf, ping(hz(D3) * 1.012, 4.5, GLASS, 0.3, 0.003), 0.3, 0.35)
    return master(buf, d, drive=1.2, verb=(1.4, 0.25, 1.2))


# ------------------------------------------------------------------ jour et nuit, interface

def night_warning():
    # Alerte avant la nuit : deux notes de cloche douce qui descendent (la, ré) et un grave, lisible et calme ;
    # courte et régulière pour pouvoir être répétée.
    d = 1.5
    buf = zeros(d)
    mix(buf, ping(hz(A4), 1.1, BELL, 0.004), 0.0, 0.6)
    mix(buf, ping(hz(D4), 1.2, BELL, 0.004), 0.3, 0.6)
    mix(buf, ping(hz(D3), 1.0, BELL, 0.01), 0.3, 0.3)
    return master(buf, d)


def victory():
    # Victoire (jingle ~3 s) : arpège montant de cloches et de verre en ré majeur, accord final large sur une nappe
    # qui gonfle, timbale douce, pluie de gemmes.
    d = 3.6
    buf = zeros(d)
    steps = ((D4, A4), (FS4, D5), (A4, FS5), (D5, A5))
    for n, (lo, hi) in enumerate(steps):
        mix(buf, ping(hz(lo), 0.9, BELL, 0.004), n * 0.17, 0.45)
        mix(buf, ping(hz(hi), 0.8, GLASS, 0.002), n * 0.17, 0.35)
        mix(buf, sub(80, 60, 0.15, 1.0), n * 0.17, 0.12)
    top = 0.72
    for note, g in ((D3, 0.6), (D4, 0.5), (FS4, 0.4), (A4, 0.4), (D5, 0.4), (FS5, 0.3), (A5, 0.25)):
        mix(buf, ping(hz(note), 2.4, BELL if note < D5 else GLASS, 0.004), top, g)
    pad = zeros(2.8)
    for note in (D4, FS4, A4, D5):
        mix(pad, tone(lambda k, f=hz(note): f, 2.8, lambda t, k: min(1.0, t / 0.35) * (1 - k) ** 1.2,
                      ((1.0, 1.0), (2.0, 0.2), (1.003, 0.5))), 0.0, 0.18)
    mix(buf, pad, top - 0.1, 1.0)
    mix(buf, sub(73, 55, 0.5, 1.3), top, 0.5)
    mix(buf, sparkle(2.0, 45, when=lambda r: r ** 1.3, amp=(0.02, 0.08)), top, 1.0)
    return master(buf, d, verb=(1.1, 0.15, 0.6))


def vote_ready():
    # Vote « prêt » : petit signal positif, deux notes qui montent (ré, la).
    buf = zeros(0.6)
    mix(buf, ping(hz(D5), 0.4, GLASS, 0.001), 0.0, 0.5)
    mix(buf, ping(hz(A5), 0.45, GLASS, 0.001), 0.07, 0.5)
    return master(buf, 0.6)


def vote_all_ready():
    # Tout le monde est prêt : trois notes qui montent puis un accord ouvert, avec quelques gemmes.
    buf = zeros(1.1)
    for n, note in enumerate((D5, FS5, A5)):
        mix(buf, ping(hz(note), 0.5, GLASS, 0.001), n * 0.08, 0.45)
    for note, g in ((D6, 0.45), (A5, 0.35), (D5, 0.3)):
        mix(buf, ping(hz(note), 0.8, GLASS, 0.0015), 0.26, g)
    mix(buf, sparkle(0.6, 12, amp=(0.02, 0.06)), 0.26, 1.0)
    return master(buf, 1.1)


def vote_cancel():
    # Vote annulé : deux notes qui descendent (la, mi), plus douces.
    buf = zeros(0.5)
    mix(buf, ping(hz(A5), 0.3, GLASS, 0.003), 0.0, 0.45)
    mix(buf, ping(hz(E5), 0.35, GLASS, 0.003), 0.08, 0.45)
    return master(buf, 0.5)


# ------------------------------------------------------------------ combat

def _crit(base_f, power=1.0):
    buf = zeros(0.4)
    mix(buf, modal(strike(0.0002, 0.35, 0.3), tuple((base_f * r, 0.26 / (1 + 0.4 * r), a) for r, a in GOLD),
                   spread=0.01), 0.0, 1.0 * power)
    mix(buf, highpass(air(0.004, lambda k: 5000, 0.7, lambda k: 1 - k), 3000), 0.0, 0.8 * power)
    mix(buf, ping(base_f * 3.1, 0.18, ((1.0, 1.0),), 0.0005), 0.004, 0.35 * power)
    mix(buf, sparkle(0.25, 4, notes=[A6, D7], t60=(0.05, 0.12), amp=(0.05, 0.1)), 0.02, 1.0)
    return buf


def critical_hit():
    # Coup critique : impact brillant, métallique et doré, très court (étoile de 0,34 s).
    return master(_crit(random.uniform(1250, 1500)), 0.38)


def critical_best():
    # Meilleur critique : deux couches (impact doré plus grave et plus fort, puis un second plus aigu 45 ms après),
    # poussée grave, anneau de gemmes qui s'élargit.
    d = 0.6
    buf = zeros(d)
    mix(buf, _crit(950, 1.2), 0.0, 1.0)
    mix(buf, sub(95, 50, 0.14, 2.0), 0.0, 0.6)
    mix(buf, _crit(1900, 0.8), 0.045, 1.0)
    mix(buf, sparkle(0.4, 12, pitch_of=lambda k: 1 - 0.3 * k, amp=(0.03, 0.08)), 0.06, 1.0)
    return master(buf, d, drive=1.3)


def bow_full_charge():
    # Arc chargé à fond : petit tintement bref qui accompagne l'éclat de la flèche (0,25 s).
    buf = zeros(0.35)
    mix(buf, ping(hz(D7), 0.25, GLASS, 0.001), 0.0, 0.5)
    mix(buf, ping(hz(A6) * 2, 0.18, GLASS, 0.001), 0.03, 0.25)
    return master(buf, 0.35)


def arrow_rain_marker():
    # Marqueur de la nuée de flèches : le cercle apparaît au sol en 0,3 s (tapotements sourds qui font le tour, souffle
    # qui tourne, petit coup au sol).
    d = 0.55
    buf = zeros(d)
    mix(buf, sub(130, 70, 0.12, 1.2), 0.0, 0.35)
    for n in range(8):
        mix(buf, ping(random.uniform(650, 1100), 0.08, GLASS, 0.001), n * 0.038, 0.25 + 0.1 * math.sin(math.pi * n / 7))
    mix(buf, air(0.35, lambda k: 800 + 1400 * k, 1.5, lambda k: math.sin(math.pi * k)), 0.0, 0.3)
    return master(buf, d)


def stealth_enter():
    # Passage en mode furtif (transition 0,35 s) : souffle feutré qui se referme (bande qui descend), tenue grave
    # sombre, petit « fwoump » étouffé à la fin.
    d = 0.55
    buf = zeros(d)
    n = int(0.4 * RATE)
    sig = white(n)
    y, out = 0.0, []
    for i, s in enumerate(sig):
        k = i / n
        y += (1 - math.exp(-2 * math.pi * (3000 * (0.1 ** k)) / RATE)) * (s - y)
        out.append(y * math.sin(math.pi * min(1, k * 1.3)) ** 1.2)
    mix(buf, out, 0.0, 0.9)
    mix(buf, tone(lambda k: 98 * (1 - 0.1 * k), 0.4, lambda t, k: math.sin(math.pi * k), ((1.0, 1.0), (1.06, 0.7))), 0.0, 0.3)
    mix(buf, sub(90, 55, 0.15, 1.0), 0.32, 0.35)
    return master(buf, d)


def stealth_exit():
    # Sortie du mode furtif : l'inverse, le souffle s'ouvre (bande qui monte) et s'efface.
    d = 0.4
    buf = zeros(d)
    n = int(0.3 * RATE)
    y, out = 0.0, []
    for i, s in enumerate(white(n)):
        k = i / n
        y += (1 - math.exp(-2 * math.pi * (300 * 10 ** k) / RATE)) * (s - y)
        out.append(y * min(1, k * 8) * (1 - k) ** 1.5)
    mix(buf, out, 0.0, 0.9)
    mix(buf, tone(lambda k: 98 * (1 + 0.15 * k), 0.25, lambda t, k: (1 - k) ** 2, ((1.0, 1.0), (1.06, 0.7))), 0.0, 0.25)
    return master(buf, d)


def stealth_spotted():
    # Assassin repéré : alerte sèche (claquement de bois dur et deux notes brèves qui sautent vers l'aigu).
    d = 0.4
    buf = zeros(d)
    mix(buf, modal(strike(0.0002, 0.1, 0.3), ((1800, 0.04, 0.8), (3100, 0.03, 0.5), (4700, 0.02, 0.3))), 0.0, 1.0)
    mix(buf, tone(lambda k: 880 if k < 0.35 else 1320, 0.16, lambda t, k: min(1, t * 800) * (1 - k) ** 1.5,
                  ((1.0, 1.0), (2.0, 0.3), (3.0, 0.1))), 0.01, 0.45)
    return master(buf, d)


def smoke_bomb_throw():
    # Lancer de la grenade fumigène : froissement, bras qui fend l'air, puis la grenade qui tournoie en s'éloignant.
    d = 0.95
    buf = zeros(d)
    mix(buf, rustle(0.2, 1800, lambda k: math.sin(math.pi * k)), 0.0, 0.25)
    mix(buf, air(0.28, lambda k: 400 + 1000 * math.sin(math.pi * k), 1.0, lambda k: math.sin(math.pi * k) ** 1.5), 0.05, 0.9)
    spin = recede(0.65, 1400, 900, 2.0, 12, attack=0.03, air=5000, flutter=(0.7, 13))
    mix(buf, spin, 0.25, 0.8)
    return master(buf, d)


def burn_loop():
    # Brûlure (boucle 2 s) : petites flammes qui lèchent (souffle grave qui palpite) et crépitements de braises.
    d, fade = 2.0, 0.4
    total = d + fade
    n = int(total * RATE)
    flick = cycles(d, 9)
    slow = cycles(d, 1.5)
    buf = lowpass(v4.brown(n, 0.012), 700)
    buf = [s * (0.6 + 0.25 * math.sin(2 * math.pi * flick * i / RATE) + 0.15 * math.sin(2 * math.pi * slow * i / RATE))
           for i, s in enumerate(buf)]
    lick = bandpass(white(n), lambda k: 1300, 0.7)
    mix(buf, [s * (0.5 + 0.5 * math.sin(2 * math.pi * flick * i / RATE + 1)) for i, s in enumerate(lick)], 0.0, 0.25)
    mix(buf, v4.fire_crackle(total, 30, (0.1, 0.5)), 0.0, 1.0)
    return loop_master(buf, d, fade)


# ------------------------------------------------------------------ catalogue

CATALOG = {
    # nom : (fonction, boucle, nombre de variantes, graine) — graines à la suite de synth_physique.py (772 à 776)
    "nyxessa_charge": (nyxessa_charge, False, 1, 780),
    "nyxessa_charge_return": (nyxessa_charge_return, False, 1, 781),
    "portal_drop_in": (portal_drop_in, False, 1, 782),
    "portal_drop_out": (portal_drop_out, False, 1, 783),
    "portal_arrive": (portal_arrive, False, 1, 784),
    "nyxessa_belt_wave": (nyxessa_belt_wave, False, 1, 785),
    "nyxessa_recall": (nyxessa_recall, False, 1, 786),
    "nyxessa_upgrade": (nyxessa_upgrade, False, 1, 787),
    "sorcerer_cast_loop": (sorcerer_cast_loop, True, 1, 788),
    "nyxessa_destroyed": (nyxessa_destroyed, False, 1, 789),
    "night_warning": (night_warning, False, 1, 790),
    "victory": (victory, False, 1, 791),
    "vote_ready": (vote_ready, False, 1, 792),
    "vote_all_ready": (vote_all_ready, False, 1, 793),
    "vote_cancel": (vote_cancel, False, 1, 794),
    "critical_hit": (critical_hit, False, 3, 795),
    "critical_best": (critical_best, False, 1, 796),
    "bow_full_charge": (bow_full_charge, False, 1, 797),
    "arrow_rain_marker": (arrow_rain_marker, False, 1, 798),
    "stealth_enter": (stealth_enter, False, 1, 799),
    "stealth_exit": (stealth_exit, False, 1, 800),
    "stealth_spotted": (stealth_spotted, False, 1, 801),
    "smoke_bomb_throw": (smoke_bomb_throw, False, 1, 802),
    "burn_loop": (burn_loop, True, 1, 803),
}
# Volume perçu visé quand il diffère de LOUDNESS_DB (-14 dB ; boucles : -15 dB) : événements majeurs plus forts.
LOUD = {"critical_best": -12.5, "nyxessa_destroyed": -12.5, "victory": -12.5, "nyxessa_recall": -13.0}


def render(name, variant, out):
    fn, is_loop, count, seed = CATALOG[name]
    random.seed(seed * 10 + variant)
    file = name if count == 1 else "%s_%d" % (name, variant + 1)
    path = os.path.join(out, file + ".wav")
    buf = fn()
    target = LOUD.get(name, -15.0 if is_loop else LOUDNESS_DB)
    if is_loop:
        prev.write_raw(path, buf, level_peak(buf, target))
    else:
        base.write(path, buf, level_peak(buf, target))
    return file


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    names = sys.argv[2:] or list(CATALOG)
    os.makedirs(out, exist_ok=True)
    for n in names:
        for v in range(CATALOG[n][2]):
            render(n, v, out)
