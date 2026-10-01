# -*- coding: utf-8 -*-
"""Simulateur de vagues de Deathless : compare les cinq classes face aux mêmes vagues (bibliothèque standard seulement).

Demande de Quentin (01/10/2026) : « un outil qui compare les dégâts de manière équivalente des personnages soumis à des
vagues d'ennemis ». Ce script est l'outil théorique ; un banc en jeu (Unity) doit confirmer ses ordres de grandeur.

    python Docs/outils/simulateur_vagues.py                       # nuits 3, 6, 9, 12, profils moyen et bon (Markdown)
    python Docs/outils/simulateur_vagues.py --nuit 6 --profil bon # une nuit, un profil
    python Docs/outils/simulateur_vagues.py --classe Rôdeur       # une classe (l'indice reste rapporté aux cinq)
    python Docs/outils/simulateur_vagues.py --json                # mêmes chiffres en JSON
    python Docs/outils/simulateur_vagues.py --seuil 15            # met en évidence les classes à plus de ±15 % de la moyenne
    python Docs/outils/simulateur_vagues.py --surcharge arcDegatsMax=45,arcTete=1.8   # « et si » sans toucher au code
    python Docs/outils/simulateur_vagues.py --nerf                # variantes du nerf du Rôdeur (indice avant / après)

Toutes les valeurs de jeu sont lues dans `Assets/Scripts/Jeu/GameBalance.cs` (initialiseurs des champs, par l'analyseur
de `equilibrage.py`) ; les multiplicateurs des élites et leur nombre par nuit sont lus dans `DirecteurVagues.cs`, le rayon
de la fumée dans `Fumigene.cs`. Une valeur absente ou illisible est signalée et arrête le script : rien n'est inventé.
Les quelques constantes qui ne sont écrites que dans le code des classes (durées d'animation, tics) sont regroupées dans
CODE ci-dessous, avec le fichier d'où elles viennent.

Modèle (détaillé dans Docs/equilibrage-classes.md, « Simulation de vagues ») : plan 2D, Nyxessa au centre ; les squelettes
sortent de terre dans les clairières actives (vagues à 0/40/80 s, ou 0/30/60/90 dès la nuit 9, étalées sur
`etalementVague`), marchent environ 20 s par un couloir (3 couloirs, flou) jusqu'à une place autour de Nyxessa, poursuivent
le héros qui passe à moins de 8 m, ripostent après deux coups au contact ; le héros est seul, devant Nyxessa, avec une
rotation scriptée et un profil de joueur (réaction, précision, tête, dos). Pas de temps fixe, graine fixe : déterministe.
"""
import argparse
import heapq
import json
import math
import os
import random
import re
import sys
import zlib

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from equilibrage import lire_balance, RACINE, CHEMIN_BALANCE  # noqa: E402  (même analyseur que l'audit)

CHEMIN_DIRECTEUR = os.path.join(RACINE, "Assets", "Scripts", "Jeu", "DirecteurVagues.cs")
CHEMIN_FUMIGENE = os.path.join(RACINE, "Assets", "VFX", "Fumigene", "Fumigene.cs")
CHEMIN_BRULURE = os.path.join(RACINE, "Assets", "Scripts", "Jeu", "Classes", "Brulure.cs")

CLASSES = ["Paladin", "Viking", "Mage", "Rôdeur", "Assassin"]

# Constantes écrites dans le code des classes, pas dans GameBalance (relevées le 01/10/2026).
CODE = {
    "rodeur_lacher": 0.3,          # ClasseRodeur.Maj : état Lacher 0,3 s avant de pouvoir rebander
    "nuee_lancer": 0.55,           # ClasseRodeur.Maj : la nuée part 0,55 s après l'appui
    "nuee_marqueur": 0.35,         # ClasseRodeur.Pluie : marqueur puis 0,35 s avant la 1re salve
    "nuee_duree_salves": 1.2,      # ClasseRodeur.Pluie : salves réparties sur 1,2 s
    "nuee_occupe": 1.0,            # ClasseRodeur.Maj : immobile 1 s
    "salve_delai": 0.08,           # ClasseRodeur.Salve
    "boule_occupe": 0.7,           # ClasseMage.Maj : min(bouleIntervalle, 0,7)
    "cone_tic": 0.25,              # ClasseMage.Maj : un tic toutes les 0,25 s, le premier après 0,25 s
    "saut_impact": 0.79 / 1.2,     # ClasseViking : ImpactClip / VitesseSaut
    "saut_fin": 0.79 / 1.2 + 0.35,
    "cri": 1.63 / 1.6,             # ClasseViking : CriClip / VitesseCri
    "cri_fin": 1.63 / 1.6 + 0.45,
    "arbalete_occupe": 0.4,        # ClasseAssassin.Maj (Tir)
    "grenade_lancer": 0.75,        # ClasseAssassin.Maj (Grenade)
    "grenade_occupe": 1.1,
    "soin_occupe": 0.9,            # ClassePaladin : incantation (soinIncantation) + fin du geste (estimée)
    "rayon_ennemi": 0.4,           # Combat.RayonDe (capsule d'un squelette, estimée)
    "rayon_ennemi_elite": 0.52,
    "sortie_de_terre": 1.0,        # clip de sortie de terre à vitesse 1,5 (estimé)
    "rotation_ennemi": 300.0,      # degrés par seconde (NavMeshAgent, estimé)
}

REQUIS = [
    "dureeNuit", "ennemisParNuit", "clairieresParNuit", "departsTroisVagues", "departsQuatreVagues", "nuitQuatreVagues",
    "partsTroisVagues", "partsQuatreVagues", "etalementVague", "partGuerriers", "partVoleurs", "partMages",
    "multiplicateurPV", "sbire", "guerrier", "voleur", "mage", "mageVitesseMissile", "detectionJoueur", "abandonPoursuite",
    "abandonApres", "rayonContactNyxessa", "rayonPlacesNyxessa", "trajetCouloirs", "trajetLargeur", "trajetFlou", "trajetEcartVitesse",
    "trajetAngleArrivee", "riposteCoups", "riposteDistance", "riposteDuree", "riposteAttaques",
    "nyxessaPV", "missilesStockPaliers", "missileRegenerationPaliers", "missileIntervallePaliers", "missileDegatsPaliers",
    "missilePortee", "missileVitesse", "groupeTaille", "groupeRayon", "frappeeRecente",
    "herosPV", "vitesse", "epeeDegats", "epeeIntervalle", "epeePortee", "epeeDemiAngle", "epeeInstant", "epeeCiblesParCoup",
    "epeeVitesse", "chargeDistance", "chargeDuree", "chargeAnticipation", "chargeLargeur", "chargeDegatsMin",
    "chargeDegatsMax", "chargeEtourdiCible", "chargeEtourdiRepousses", "chargeRecharge", "soinPart", "soinRecharge",
    "magePV", "bouleDegats", "bouleDegatsZone", "bouleRayon", "bouleIntervalle", "bouleVitesse", "boulePortee",
    "bouleInstant", "manaMax", "manaRegen", "manaParTouche", "coneMana", "coneDegats", "conePortee", "coneDemiAngle",
    "brulureDegats", "brulureDuree",
    "rodeurPV", "arcCharge", "arcDegatsMin", "arcDegatsMax", "arcTete", "arcEtourdiPleineCharge", "arcVitesseMin",
    "arcVitesseMax", "arcIntervalle", "nueeRayon", "nueeSalves", "nueeDegatsSalve", "nueePortee", "nueeRecharge",
    "nueeRalentiForce", "nueeRalentiDuree", "rouladeDistance", "salveFleches", "salveEcart", "salveDegats",
    "salveVitesse", "rouladeRecharge", "esquiveDuree",
    "assassinPV", "assassinVitesse", "marcheDiscrete", "dagueDegats", "dagueIntervalle", "daguePortee", "dagueDemiAngle",
    "dagueInstant", "critiqueFurtif", "critiqueDos", "critiqueFurtifDos", "angleDos", "assassinDetectionVue",
    "assassinDetectionAngle", "assassinDetectionDos", "horsCombat", "arbaleteDegats", "arbaleteTete", "arbaleteVitesse",
    "arbaletePortee", "arbaleteRecharge", "grenadeRecharge", "grenadeNuage", "grenadePortee", "pasOmbreDistance",
    "pasOmbreDuree", "pasOmbreArret", "pasOmbrePorteeCible", "pasOmbreRecharge", "executionSeuil", "executionElite",
    "vikingPV", "hacheDegats", "hacheIntervalle", "hachePortee", "hacheDemiAngle", "hacheInstant", "rageMax", "rageMin",
    "rageParTouche", "rageBaisse", "rageDelaiBaisse", "tournanteRage", "tournanteRageMin", "tournanteIntervalle",
    "tournanteRageParTic", "tournanteRayon", "tournanteDegats", "tournanteVitesse", "rugissementRage",
    "rugissementRecharge", "rugissementRayon", "rugissementProvocation", "peauDeFerReduction", "peauDeFerDuree",
    "sautRage", "sautRecharge", "sautDistance", "sautRayon", "sautDegats", "sautEtourdi",
]
# Brûlure en paliers (01/10/2026) : si ces champs manquent, la brûlure d'avant (brulureDegats pendant brulureDuree) fait foi.
BRULURE_PALIERS = ["brulureDegatsPaliers", "brulureRemplissageCone", "brulureRemplissageBoule", "brulureDelaiDescente",
                   "brulureVitesseDescente"]

# Profils de joueur : ce qui dépend des mains, pas des chiffres. Hypothèses, à confronter au banc en jeu.
PROFILS = {
    "moyen": dict(
        reaction=0.15,          # s perdues entre deux actions (viser, choisir, appuyer)
        arc_touche=0.80,        # part des flèches qui touchent (cible qui marche, manette)
        arc_tete=0.25,          # part des flèches qui touchent à la tête, parmi celles qui touchent
        arc_pleine=0.70,        # part des tirs lâchés à pleine charge (le reste vers 50 %)
        salve_touche=0.75, salve_tete=0.10,
        arbalete_touche=0.80, arbalete_tete=0.25,
        boule_directe=0.75,     # part des boules qui touchent la cible visée (sinon explosion à côté)
        nuee_optimale=False,    # la nuée vise le premier ennemi venu, pas le groupe le plus dense
        roulade_offensive=False,  # la roulade sert à fuir (ennemi à moins de 5 m), pas à placer la salve
        cone_seuil=3, cone_mana_depart=40.0,
        tournante_seuil=3, saut_seuil=3, rugir_seuil=4,
        charge_portee_min=2.5,
        dos_alterner=False,     # l'assassin ne change pas de cible pour garder le dos
        assassin_placement=False,  # il ne contourne pas sa cible pour la prendre de dos
        bond_usage=0.5, grenade_usage=0.5,
    ),
    "bon": dict(
        reaction=0.05,
        arc_touche=0.92, arc_tete=0.50, arc_pleine=0.95,
        salve_touche=0.90, salve_tete=0.30,
        arbalete_touche=0.92, arbalete_tete=0.50,
        boule_directe=0.92,
        nuee_optimale=True, roulade_offensive=True,
        cone_seuil=3, cone_mana_depart=20.0,
        tournante_seuil=3, saut_seuil=2, rugir_seuil=3,
        charge_portee_min=4.0,
        dos_alterner=True, assassin_placement=True,
        bond_usage=1.0, grenade_usage=1.0,
    ),
}

LAISSE = 10.0          # m : un héros de mêlée défend Nyxessa, il ne s'en éloigne pas plus
POSTE = 4.5            # m : place du héros à distance (devant Nyxessa, du côté le plus pressé)
PORTEE_UTILE_ARC = 40.0  # m : au-delà, la flèche (balistique, cible qui marche) n'est plus tirée


# ======================================================================== Lecture des valeurs

def lire_valeurs(surcharges=None):
    """GameBalance.cs + DirecteurVagues.cs + Fumigene.cs → dictionnaire ; arrête le script si une valeur manque."""
    b = lire_balance(CHEMIN_BALANCE)
    with open(CHEMIN_BALANCE, encoding="utf-8") as fh:
        texte = fh.read()
    for nom, x, y in re.findall(r"public\s+Vector2\s+(\w+)\s*=\s*new\s+Vector2\(\s*(-?[\d.]+)f?\s*,\s*(-?[\d.]+)f?\s*\)", texte):
        b[nom] = (float(x), float(y))
    manquants = [k for k in REQUIS if k not in b]
    for k in ("sbire", "guerrier", "voleur", "mage"):
        if k in b:
            manquants += ["%s.%s" % (k, c) for c in ("pv", "vitesse", "degatsJoueur", "degatsNyxessa", "intervalle", "preparation", "portee") if c not in b[k]]
    if "mageDistanceTir" not in b:
        manquants.append("mageDistanceTir")
    # Élites : DirecteurVagues.Preparer / Poser (pas dans GameBalance).
    with open(CHEMIN_DIRECTEUR, encoding="utf-8") as fh:
        dv = fh.read()
    m = re.search(r"int\s+elites\s*=\s*nuit\s*>=\s*(\d+)\s*\?\s*(\d+)\s*:\s*nuit\s*>=\s*(\d+)\s*\?\s*(\d+)\s*:\s*(\d+)\s*;", dv)
    m_pv = re.search(r"pv\s*=\s*stats\.pv\s*\*\s*([\d.]+)f", dv)
    m_dg = re.search(r"degatsJoueur\s*=\s*stats\.degatsJoueur\s*\*\s*([\d.]+)f", dv)
    m_dn = re.search(r"degatsNyxessa\s*=\s*stats\.degatsNyxessa\s*\*\s*([\d.]+)f", dv)
    m_po = re.search(r"portee\s*=\s*stats\.portee\s*\+\s*([\d.]+)f", dv)
    if m:
        b["_elites"] = (int(m.group(1)), int(m.group(2)), int(m.group(3)), int(m.group(4)), int(m.group(5)))
    else:
        manquants.append("DirecteurVagues.cs : nombre d'élites par nuit")
    for cle, mm, nom in (("_elitePV", m_pv, "PV des élites"), ("_eliteDegats", m_dg, "dégâts des élites aux joueurs"),
                         ("_eliteDegatsNyx", m_dn, "dégâts des élites à Nyxessa"), ("_elitePortee", m_po, "portée des élites")):
        if mm:
            b[cle] = float(mm.group(1))
        else:
            manquants.append("DirecteurVagues.cs : " + nom)
    try:
        with open(CHEMIN_FUMIGENE, encoding="utf-8") as fh:
            mf = re.search(r"float\s+rayon\s*=\s*([\d.]+)f", fh.read())
        b["_fumeeRayon"] = float(mf.group(1)) * 0.9 if mf else None  # Contient : distance < rayon × 0,9
    except OSError:
        b["_fumeeRayon"] = None
    if b["_fumeeRayon"] is None:
        manquants.append("Fumigene.cs : rayon du nuage")
    if manquants:
        sys.stderr.write("Valeurs absentes ou illisibles (rien n'est inventé, le script s'arrête) :\n  - " + "\n  - ".join(manquants) + "\n")
        sys.exit(2)
    b["_paliers"] = all(k in b for k in BRULURE_PALIERS) and len(b["brulureDegatsPaliers"]) > 0
    if not b["_paliers"]:
        sys.stderr.write("Brûlure en paliers absente de GameBalance.cs : modèle d'avant (%s/s pendant %s s).\n" % (b["brulureDegats"], b["brulureDuree"]))
    for k, v in (surcharges or {}).items():
        if k not in b:
            sys.stderr.write("Surcharge inconnue : %s (pas un champ lu dans GameBalance.cs)\n" % k)
            sys.exit(2)
        b[k] = v
    return b


# ======================================================================== Géométrie

def dist(a, b):
    return math.hypot(a[0] - b[0], a[1] - b[1])


def norme(v):
    n = math.hypot(v[0], v[1])
    return (v[0] / n, v[1] / n) if n > 1e-9 else (1.0, 0.0)


def angle_entre(u, v):
    """Angle (degrés) entre deux vecteurs du plan."""
    nu, nv = math.hypot(*u), math.hypot(*v)
    if nu < 1e-9 or nv < 1e-9:
        return 0.0
    c = max(-1.0, min(1.0, (u[0] * v[0] + u[1] * v[1]) / (nu * nv)))
    return math.degrees(math.acos(c))


def tourner_vers(face, voulu, max_deg):
    """Tourne `face` vers `voulu` d'au plus `max_deg` degrés."""
    a0 = math.atan2(face[1], face[0])
    a1 = math.atan2(voulu[1], voulu[0])
    d = (a1 - a0 + math.pi) % (2 * math.pi) - math.pi
    lim = math.radians(max_deg)
    d = max(-lim, min(lim, d))
    return (math.cos(a0 + d), math.sin(a0 + d))


def graine_de(*parts):
    return zlib.crc32("|".join(str(p) for p in parts).encode("utf-8"))


# ======================================================================== Ennemis

class Ennemi:
    __slots__ = ("id", "type", "elite", "pv", "pvmax", "pos", "face", "vit", "degJ", "degN", "intervalle", "prep",
                 "portee", "rayon", "vague", "clair", "apparition", "actif", "phase", "waypoint", "place", "cible",
                 "pret_coup", "impact", "cible_coup", "etourdi", "ralenti", "ralenti_force", "provoque", "riposte",
                 "riposte_coups", "riposte_attaques", "dernier_coup_heros", "vivant", "mort_t", "a_frappe_nyx",
                 "brulure", "mannequin", "sans_frapper")

    def __init__(self):
        self.vivant = True
        self.actif = False
        self.mort_t = None
        self.etourdi = -1.0
        self.ralenti = -1.0
        self.ralenti_force = 0.0
        self.provoque = -1.0
        self.riposte = -1.0
        self.riposte_coups = 0
        self.riposte_attaques = 0
        self.dernier_coup_heros = -99.0
        self.impact = None
        self.cible_coup = None
        self.pret_coup = 0.0
        self.cible = "route"
        self.a_frappe_nyx = False
        self.brulure = None
        self.mannequin = False
        self.sans_frapper = 0.0


# ======================================================================== Simulation

class Simulation:
    def __init__(self, b, classe, nuit, profil, graine, palier_missiles=1, mono=False, duree_mono=180.0):
        self.b = b
        self.classe = classe
        self.nuit = nuit
        self.p = PROFILS[profil]
        self.rng = random.Random(graine_de(classe, nuit, profil, graine, "jeu"))
        self.rng_vague = random.Random(graine_de(nuit, graine, "vague"))   # mêmes vagues pour toutes les classes
        self.dt = 0.05
        self.t = 0.0
        self.mono = mono
        # Nuit : prolongée de 60 s au-delà de l'aube pour mesurer le vidage de la dernière vague (les survivants à l'aube
        # sont comptés à part : en jeu, l'aube les désintègre).
        self.duree = duree_mono if mono else b["dureeNuit"] + 60.0
        self.survivants_aube = None
        self.evts = []
        self.seq = 0
        self.ennemis = []
        self.stats = dict(utiles=0.0, crit=0.0, brut=0.0, tues=0, nyx_degats=0.0, nyx_atteinte=0, recu=0.0,
                          engage=0.0, missiles=0.0, chutes=0, tues_missiles=0, brulure=0.0, par_source={})
        self.vague_fin = {}
        self.vague_n = {}
        self.palier = palier_missiles
        self.nyx_stock = int(b["missilesStockPaliers"][min(palier_missiles, 5) - 1])
        self.nyx_regen = 0.0
        self.nyx_prochain = 0.0
        self.nyx_frappee = -99.0
        self.fumee = None   # (centre, fin)
        self.directions = [90.0, -30.0, 210.0]
        if mono:
            self._poser_mannequin()
        else:
            self._planifier()
        self.heros = HEROS[classe](self)

    # ------------------------------------------------------------ Plan de la nuit (DirecteurVagues.Preparer)
    def _planifier(self):
        b, n = self.b, self.nuit
        par = lambda tab: tab[max(0, min(n - 1, len(tab) - 1))]
        total = int(round(par(b["ennemisParNuit"])))
        nb_cl = max(1, min(3, int(par(b["clairieresParNuit"]))))
        actives = list(range(nb_cl))
        quatre = n >= b["nuitQuatreVagues"]
        departs = b["departsQuatreVagues"] if quatre else b["departsTroisVagues"]
        parts = b["partsQuatreVagues"] if quatre else b["partsTroisVagues"]
        pg, pv_, pm = par(b["partGuerriers"]), par(b["partVoleurs"]), par(b["partMages"])
        e1, n1, e2, n2, n0 = b["_elites"]
        elites = n1 if n >= e1 else (n2 if n >= e2 else n0)
        sorties = []
        pose = 0
        for w in range(len(departs)):
            nw = total - pose if w == len(departs) - 1 else int(round(total * parts[min(w, len(parts) - 1)]))
            pose += nw
            for i in range(nw):
                r = self.rng_vague.random()
                pM = min(max(pm, 0), 1)
                pV = min(max(pv_, 0), 1 - pM)
                pG = min(max(pg, 0), 1 - pM - pV)
                typ = "mage" if r < pM else "voleur" if r < pM + pV else "guerrier" if r < pM + pV + pG else "sbire"
                sorties.append([departs[w] + b["etalementVague"] * i / max(1, nw), typ, actives[i % nb_cl], False, w])
        for e in range(elites):
            if not sorties:
                break
            idx = max(0, min(len(sorties) - 1, len(sorties) // 2 + e * 3))
            sorties[idx][1] = "guerrier"
            sorties[idx][3] = True
        mult = par(b["multiplicateurPV"])
        marche = 20.0 * b["sbire"]["vitesse"]   # ~20 s de marche d'un sbire
        for k, (instant, typ, cl, elite, w) in enumerate(sorties):
            st = b[typ]
            e = Ennemi()
            e.id = k
            e.type = typ
            e.elite = elite
            e.pvmax = e.pv = st["pv"] * mult * (b["_elitePV"] if elite else 1.0)
            e.degJ = st["degatsJoueur"] * (b["_eliteDegats"] if elite else 1.0)
            e.degN = st["degatsNyxessa"] * (b["_eliteDegatsNyx"] if elite else 1.0)
            e.intervalle, e.prep = st["intervalle"], st["preparation"]
            e.portee = st["portee"] + (b["_elitePortee"] if elite else 0.0)
            e.vit = st["vitesse"] * (1 + self.rng_vague.uniform(-1, 1) * b["trajetEcartVitesse"])
            e.rayon = CODE["rayon_ennemi_elite"] if elite else CODE["rayon_ennemi"]
            e.vague, e.clair, e.apparition = w, cl, instant
            a = math.radians(self.directions[cl])
            axe = (math.cos(a), math.sin(a))
            lat = (-axe[1], axe[0])
            ox, oy = self.rng_vague.uniform(-4, 4), self.rng_vague.uniform(-4, 4)
            e.pos = (axe[0] * marche + ox, axe[1] * marche + oy)
            e.face = (-axe[0], -axe[1])
            couloirs = max(1, int(b["trajetCouloirs"]))
            kc = self.rng_vague.randrange(couloirs)
            decal = 0.0 if couloirs == 1 else (kc / (couloirs - 1) - 0.5) * b["trajetLargeur"]
            fr_ = self.rng_vague.uniform(0.4, 0.6)
            decal += self.rng_vague.uniform(-1, 1) * b["trajetFlou"]
            le = marche * (1 - fr_) + self.rng_vague.uniform(-1, 1) * b["trajetFlou"]
            e.waypoint = (axe[0] * le + lat[0] * decal, axe[1] * le + lat[1] * decal)
            cote = 1 if decal >= 0 else -1
            aa = a + math.radians(cote * self.rng_vague.uniform(0, b["trajetAngleArrivee"]))
            e.place = (math.cos(aa) * b["rayonPlacesNyxessa"], math.sin(aa) * b["rayonPlacesNyxessa"])
            e.phase = 0
            self.ennemis.append(e)
            self.vague_n[w] = self.vague_n.get(w, 0) + 1

    def _poser_mannequin(self):
        """Mono-cible : un guerrier de la nuit, PV infinis, au contact de Nyxessa (il la frappe, riposte comprise)."""
        b = self.b
        e = Ennemi()
        st = b["guerrier"]
        e.id, e.type, e.elite = 0, "guerrier", False
        e.pvmax = e.pv = 1e9
        e.degJ, e.degN, e.intervalle, e.prep, e.portee = st["degatsJoueur"], st["degatsNyxessa"], st["intervalle"], st["preparation"], st["portee"]
        e.vit, e.rayon = st["vitesse"], CODE["rayon_ennemi"]
        e.vague, e.clair, e.apparition = 0, 0, 0.0
        e.pos = (0.0, b["rayonContactNyxessa"] - 0.1)
        e.face = (0.0, -1.0)
        e.waypoint = e.place = (0.0, b["rayonPlacesNyxessa"])
        e.phase = 1
        e.mannequin = True
        self.ennemis.append(e)
        self.vague_n[0] = 1

    # ------------------------------------------------------------ Outils
    def plus_tard(self, delai, fn):
        self.seq += 1
        heapq.heappush(self.evts, (self.t + delai, self.seq, fn))

    def presents(self):
        return [e for e in self.ennemis if e.vivant and e.actif]

    def dans_secteur(self, origine, face, portee, demi_angle, liste=None):
        """Combat.Ennemis : distance au bord de la capsule ≤ portée, angle au centre ≤ demi-angle ; triés par distance."""
        res = []
        for e in (liste if liste is not None else self.presents()):
            d = dist(e.pos, origine)
            dd = max(0.0, d - e.rayon)
            if dd > portee:
                continue
            if demi_angle < 180 and d > 0.2 and angle_entre(face, (e.pos[0] - origine[0], e.pos[1] - origine[1])) > demi_angle:
                continue
            res.append((dd, e.id, e))
        res.sort(key=lambda x: (x[0], x[1]))
        return [x[2] for x in res]

    def frapper(self, e, montant, critique=False, melee=False, continu=False, source="?"):
        """Héros → ennemi. Compte les dégâts utiles (sans le surplus sur un ennemi déjà achevé) et la riposte."""
        if not e.vivant:
            return 0.0
        utile = min(montant, e.pv)
        e.pv -= montant
        self.stats["brut"] += montant
        self.stats["utiles"] += utile
        if critique:
            self.stats["crit"] += utile
        if continu == "brulure":
            self.stats["brulure"] += utile
            source = "brûlure"
        ps = self.stats["par_source"]
        ps[source] = ps.get(source, 0.0) + utile
        h = self.heros
        # Riposte (Squelette.CompterRiposte) : au contact de Nyxessa, frappé riposteCoups fois de suite à moins de riposteDistance.
        if e.type != "mage" and e.cible == "N" and dist(h.pos, e.pos) <= self.b["riposteDistance"] and continu is False:
            if self.t - e.dernier_coup_heros > 3.0:
                e.riposte_coups = 0
            e.riposte_coups += 1
            e.dernier_coup_heros = self.t
            if e.riposte_coups >= self.b["riposteCoups"]:
                e.riposte = self.t + self.b["riposteDuree"]
                e.riposte_attaques = int(self.b["riposteAttaques"])
                e.riposte_coups = 0
                e.cible = "H"
        if e.pv <= 0:
            self.tuer(e)
        return utile

    def tuer(self, e, par_missile=False):
        e.vivant = False
        e.mort_t = self.t
        if par_missile:
            self.stats["tues_missiles"] += 1
        else:
            self.stats["tues"] += 1
        if all((not x.vivant) for x in self.ennemis if x.vague == e.vague):
            self.vague_fin[e.vague] = self.t

    def etourdir(self, e, duree):
        if e.vivant:
            e.etourdi = max(e.etourdi, self.t + duree)
            e.impact = None

    def ralentir(self, e, duree, force):
        if e.vivant:
            e.ralenti = max(e.ralenti, self.t + duree)
            e.ralenti_force = force

    # ------------------------------------------------------------ Brûlure (Brulure.Attiser / Etat, ou modèle d'avant)
    def attiser(self, e, remplissage):
        b = self.b
        if not e.vivant:
            return
        if not b["_paliers"]:
            e.brulure = dict(palier=1, jauge=0.0, descente=self.t, fin=self.t + b["brulureDuree"])
            return
        vit = max(0.01, b["brulureVitesseDescente"])
        pmax = len(b["brulureDegatsPaliers"])
        p, j = 1, 0.0
        if e.brulure is not None and self.t < e.brulure["fin"]:
            p, j = self.etat_brulure(e.brulure)
        j += max(0.0, remplissage)
        while j > 1.0 and p < pmax:
            j -= 1.0
            p += 1
        j = min(j, 1.0)
        duree = max(b["brulureDuree"], b["brulureDelaiDescente"] + (p - 1 + j) / vit)
        e.brulure = dict(palier=p, jauge=j, descente=self.t + b["brulureDelaiDescente"], fin=self.t + duree)

    def etat_brulure(self, s):
        b = self.b
        if not b["_paliers"]:
            return 1, 0.0
        vit = max(0.01, b["brulureVitesseDescente"])
        total = max(1, s["palier"]) - 1 + s["jauge"] - max(0.0, self.t - s["descente"]) * vit
        if total <= 0:
            return 1, 0.0
        p = max(1, min(len(b["brulureDegatsPaliers"]), int(math.ceil(total - 0.0001))))
        return p, max(0.0, min(1.0, total - (p - 1)))

    def dps_brulure(self, palier):
        b = self.b
        if not b["_paliers"]:
            return b["brulureDegats"]
        l = b["brulureDegatsPaliers"]
        return l[max(1, min(len(l), palier)) - 1]

    # ------------------------------------------------------------ Boucle
    def lancer(self):
        b = self.b
        pas = int(round(self.duree / self.dt))
        for _ in range(pas):
            self.t = round(self.t + self.dt, 6)
            t = self.t
            while self.evts and self.evts[0][0] <= t + 1e-9:
                _, _, fn = heapq.heappop(self.evts)
                fn()
            for e in self.ennemis:
                if not e.actif and e.vivant and t >= e.apparition + CODE["sortie_de_terre"]:
                    e.actif = True
            self._brulures()
            self.heros.pas(t, self.dt)
            self._ennemis()
            self._missiles()
            if not self.mono and self.survivants_aube is None and t >= b["dureeNuit"] - 1e-6:
                self.survivants_aube = sum(1 for e in self.ennemis if e.vivant)
                self.stats["utiles_aube"] = self.stats["utiles"]
            if self.heros.engage():
                self.stats["engage"] += self.dt
            if not self.mono and all(not e.vivant for e in self.ennemis):
                if self.survivants_aube is None:
                    self.survivants_aube = 0
                    self.stats["utiles_aube"] = self.stats["utiles"]
                break
        return self

    def _brulures(self):
        for e in self.ennemis:
            s = e.brulure
            if s is None or not e.vivant:
                continue
            if self.t >= s["fin"]:
                e.brulure = None
                continue
            p, _ = self.etat_brulure(s)
            self.frapper(e, self.dps_brulure(p) * self.dt, continu="brulure")

    def _ennemis(self):
        b, t, dt, h = self.b, self.t, self.dt, self.heros
        contact = b["rayonContactNyxessa"]
        vivants = self.presents()
        for e in vivants:
            if t < e.etourdi:
                continue
            # Cible (Squelette.MajMarche / Poursuite)
            dn = math.hypot(*e.pos)
            dh = dist(e.pos, h.pos)
            visible = h.visible_par(e, dh)
            if e.riposte > 0 and (t >= e.riposte or e.riposte_attaques <= 0):
                e.riposte = -1.0           # fin de la riposte : il revient à Nyxessa
                e.cible = "route"
            if t < e.provoque:
                e.cible = "H"
            elif t < e.riposte and e.riposte_attaques > 0:
                e.cible = "H"
            elif e.cible == "H" and dh <= b["abandonPoursuite"] and visible and e.sans_frapper <= b["abandonApres"]:
                pass   # poursuite en cours (Squelette.MajPoursuite)
            elif e.type != "mage" and dn <= contact + 0.05:
                e.cible = "N"
                e.sans_frapper = 0.0
            elif e.type != "mage" and visible and dh < b["detectionJoueur"] and dn > contact:
                e.cible = "H"
                e.sans_frapper = 0.0
                h.repere()
            elif e.cible == "H":
                e.cible = "route"
                e.sans_frapper = 0.0
            if e.mannequin and e.cible == "route":
                e.cible = "N"
            # Déplacement
            v = e.vit * (1 - e.ralenti_force if t < e.ralenti else 1.0)
            voulu = None
            portee_ok = False
            if e.type == "mage":
                mn = b["mageDistanceTir"][1]
                if dn > mn:
                    voulu = (0.0, 0.0)
                else:
                    portee_ok = True
            elif e.cible == "H":
                if dh > e.portee + 0.3:
                    voulu = h.pos
                    e.sans_frapper += dt
                else:
                    portee_ok = True
                    e.sans_frapper = 0.0
            elif e.cible == "N":
                if dn > contact + 0.05:
                    voulu = e.place
                else:
                    portee_ok = True
            else:
                if e.phase == 0 and dist(e.pos, e.waypoint) < 1.5:
                    e.phase = 1
                if dn <= contact + 0.05:
                    e.cible = "N"
                    portee_ok = True
                else:
                    voulu = e.waypoint if e.phase == 0 else e.place
            if voulu is not None and e.impact is None and not e.mannequin:
                d = (voulu[0] - e.pos[0], voulu[1] - e.pos[1])
                ld = math.hypot(*d)
                if ld > 1e-6:
                    pasm = min(ld, v * dt)
                    e.pos = (e.pos[0] + d[0] / ld * pasm, e.pos[1] + d[1] / ld * pasm)
                    e.face = tourner_vers(e.face, d, CODE["rotation_ennemi"] * dt)
            # Orientation pendant l'attaque
            if e.cible == "H" or (e.type == "mage" and dh <= e.portee and visible):
                e.face = tourner_vers(e.face, (h.pos[0] - e.pos[0], h.pos[1] - e.pos[1]), CODE["rotation_ennemi"] * dt)
            elif portee_ok:
                e.face = tourner_vers(e.face, (-e.pos[0], -e.pos[1]), CODE["rotation_ennemi"] * dt)
            # Attaque
            if e.impact is not None and t >= e.impact:
                e.impact = None
                if e.cible_coup == "H":
                    if (e.type == "mage" and dist(e.pos, h.pos) <= e.portee + 1) or dist(e.pos, h.pos) <= e.portee + 0.8:
                        h.encaisser(e.degJ)
                    if t < e.riposte:
                        e.riposte_attaques -= 1
                else:
                    self.stats["nyx_degats"] += e.degN
                    self.nyx_frappee = t
                    if not e.a_frappe_nyx:
                        e.a_frappe_nyx = True
                        self.stats["nyx_atteinte"] += 1
            if portee_ok and e.impact is None and t >= e.pret_coup:
                if e.type == "mage":
                    e.cible_coup = "H" if (dh <= e.portee and visible) else "N"
                    vol = (dh if e.cible_coup == "H" else dn) / b["mageVitesseMissile"]
                    e.impact = t + e.prep + vol
                else:
                    e.cible_coup = "H" if e.cible == "H" else "N"
                    e.impact = t + e.prep
                e.pret_coup = t + e.intervalle
        # Séparation (évitement des agents) près de Nyxessa : pas d'empilement au même point.
        proches = [e for e in vivants if math.hypot(*e.pos) < 14 and not e.mannequin]
        for i in range(len(proches)):
            a = proches[i]
            for j in range(i + 1, len(proches)):
                c = proches[j]
                dx, dy = c.pos[0] - a.pos[0], c.pos[1] - a.pos[1]
                d2 = dx * dx + dy * dy
                mini = a.rayon + c.rayon + 0.1
                if d2 < mini * mini:
                    d = math.sqrt(d2) if d2 > 1e-9 else 1e-3
                    if d2 <= 1e-9:
                        dx, dy = (1.0, 0.0) if a.id < c.id else (-1.0, 0.0)
                    pousse = (mini - d) * 0.5
                    a.pos = (a.pos[0] - dx / d * pousse, a.pos[1] - dy / d * pousse)
                    c.pos = (c.pos[0] + dx / d * pousse, c.pos[1] + dy / d * pousse)
            r = math.hypot(*a.pos)
            if r < b["rayonPlacesNyxessa"]:
                k = b["rayonPlacesNyxessa"] / max(r, 1e-6)
                a.pos = (a.pos[0] * k, a.pos[1] * k)

    def _missiles(self):
        """Missiles de Nyxessa (DefenseNyxessa) au palier choisi, simplifiés : cible, réserve, salve sur groupe ou élite."""
        if self.palier <= 0 or self.mono:
            return
        b, t = self.b, self.t
        i = min(self.palier, 5) - 1
        stock_max = int(b["missilesStockPaliers"][i])
        if self.nyx_stock < stock_max:
            self.nyx_regen += self.dt
            if self.nyx_regen >= b["missileRegenerationPaliers"][i]:
                self.nyx_regen = 0.0
                self.nyx_stock += 1
        else:
            self.nyx_regen = 0.0
        if self.nyx_stock <= 0 or t < self.nyx_prochain:
            return
        cands = [e for e in self.presents() if math.hypot(*e.pos) <= b["missilePortee"]]
        if not cands:
            return
        frappent = [e for e in cands if e.a_frappe_nyx and math.hypot(*e.pos) <= b["rayonContactNyxessa"] + 0.5]
        elites = [e for e in cands if e.elite]
        cible = min(frappent or elites or cands, key=lambda e: (math.hypot(*e.pos), e.id))
        groupe = sum(1 for e in cands if dist(e.pos, cible.pos) <= b["groupeRayon"]) >= b["groupeTaille"]
        recente = t - self.nyx_frappee < b["frappeeRecente"]
        reserve = 0 if recente else 1
        if self.nyx_stock <= reserve and not (groupe or cible.elite) and not recente:
            return
        if self.nyx_stock <= 0:
            return
        self.nyx_stock -= 1
        self.nyx_prochain = t + b["missileIntervallePaliers"][i]
        degats = b["missileDegatsPaliers"][i]

        def impact(e=cible, d=degats):
            if e.vivant:
                u = min(d, e.pv)
                e.pv -= d
                self.stats["missiles"] += u
                if e.pv <= 0:
                    self.tuer(e, par_missile=True)
        self.plus_tard(math.hypot(*cible.pos) / b["missileVitesse"], impact)


# ======================================================================== Héros (rotations)

class Heros:
    pv_max_cle = "herosPV"
    melee = True

    def __init__(self, sim):
        self.sim = sim
        self.b = sim.b
        self.p = sim.p
        self.rng = sim.rng
        self.pv = self.b[self.pv_max_cle]
        self.pos = (0.0, POSTE)
        self.face = (0.0, 1.0)
        self.libre = 0.0          # instant où il peut agir à nouveau
        self.facteur_vitesse = 1.0
        self.invulnerable = -1.0
        self.reduction = (0.0, -1.0)
        self.cible = None
        self.poste_cl = 0
        self.poste_maj = -99.0

    # -- utilitaires communs
    def vitesse(self):
        return self.b["vitesse"]

    def visible_par(self, e, dh):
        return True

    def repere(self):
        pass

    def encaisser(self, degats):
        t = self.sim.t
        if t < self.invulnerable:
            return
        red, fin = self.reduction
        if t < fin:
            degats *= (1 - red)
        self.sim.stats["recu"] += degats
        self.pv -= degats
        self.sur_touche()
        if self.pv <= 0:
            self.sim.stats["chutes"] += 1   # indicatif : le héros ne meurt pas dans le simulateur
            self.pv = self.b[self.pv_max_cle]

    def sur_touche(self):
        pass

    def occupe(self):
        return self.sim.t < self.libre

    def finir_dans(self, duree, reaction=True):
        self.libre = self.sim.t + duree + (self.p["reaction"] if reaction else 0.0)

    def aller_vers(self, point, dt, facteur=1.0, arret=0.0):
        d = (point[0] - self.pos[0], point[1] - self.pos[1])
        ld = math.hypot(*d)
        if ld <= arret + 1e-6:
            return True
        pas = min(ld - arret, self.vitesse() * facteur * dt)
        np_ = (self.pos[0] + d[0] / ld * pas, self.pos[1] + d[1] / ld * pas)
        r = math.hypot(*np_)
        if self.melee and r > LAISSE:
            np_ = (np_[0] * LAISSE / r, np_[1] * LAISSE / r)
        if r < 2.0:   # le plateau de Nyxessa
            np_ = (np_[0] * 2.0 / max(r, 1e-6), np_[1] * 2.0 / max(r, 1e-6))
        self.pos = np_
        self.face = norme(d)
        return False

    def viser(self, e):
        self.face = norme((e.pos[0] - self.pos[0], e.pos[1] - self.pos[1]))

    def candidats_melee(self):
        """Ennemis qu'un héros de mêlée va chercher : ceux qui frappent (Nyxessa ou lui) d'abord, puis ceux qui entrent dans sa laisse."""
        res = [e for e in self.sim.presents() if math.hypot(*e.pos) <= LAISSE + 2.5]
        return res

    def choisir_melee(self):
        c = self.candidats_melee()
        if not c:
            return None
        if self.cible is not None and self.cible.vivant and self.cible in c and dist(self.cible.pos, self.pos) < 4.0:
            return self.cible
        frappent = [e for e in c if e.cible in ("N", "H") and math.hypot(*e.pos) <= LAISSE]
        pool = frappent or c
        return min(pool, key=lambda e: (dist(e.pos, self.pos), e.id))

    def poste(self):
        """Héros à distance : place devant Nyxessa, du côté de la clairière la plus pressée (revue toutes les 2 s)."""
        sim = self.sim
        if sim.t - self.poste_maj > 2.0:
            self.poste_maj = sim.t
            compte = {}
            for e in sim.presents():
                if math.hypot(*e.pos) < 25:
                    compte[e.clair] = compte.get(e.clair, 0) + (2 if math.hypot(*e.pos) < 6 else 1)
            if compte:
                self.poste_cl = max(compte, key=lambda k: (compte[k], -k))
        a = math.radians(sim.directions[self.poste_cl])
        return (math.cos(a) * POSTE, math.sin(a) * POSTE)

    def engage(self):
        """Fenêtre de combat de l'arme principale : un ennemi à portée (mêlée : dans la laisse ; distance : à portée)."""
        return any(math.hypot(*e.pos) <= LAISSE + 2.5 for e in self.sim.presents())

    def pas(self, t, dt):
        raise NotImplementedError


class Paladin(Heros):
    pv_max_cle = "herosPV"

    def __init__(self, sim):
        super().__init__(sim)
        self.rech_charge = 0.0
        self.rech_soin = 0.0
        self.charge = None

    def pas(self, t, dt):
        b, sim = self.b, self.sim
        self.rech_charge = max(0.0, self.rech_charge - dt)
        self.rech_soin = max(0.0, self.rech_soin - dt)
        if self.charge is not None:
            self._ruee(dt)
            return
        if self.occupe():
            return
        if self.rech_soin <= 0 and self.pv <= b["herosPV"] * (1 - b["soinPart"]):
            self.pv = min(b["herosPV"], self.pv + b["herosPV"] * b["soinPart"])
            self.rech_soin = b["soinRecharge"]
            self.finir_dans(CODE["soin_occupe"])
            return
        e = self.choisir_melee()
        if e is None:
            self.aller_vers((0.0, POSTE), dt)
            return
        de = dist(e.pos, self.pos)
        # Charge bélier : cible à au moins charge_portee_min m (dégâts selon la distance parcourue, 15 à 60).
        if self.rech_charge <= 0:
            cands = [x for x in self.candidats_melee() if self.p["charge_portee_min"] <= dist(x.pos, self.pos) <= b["chargeDistance"] + 1.2]
            if cands:
                c = max(cands, key=lambda x: (x.elite, dist(x.pos, self.pos), -x.id))
                self.viser(c)
                le = dist(c.pos, self.pos)
                self.charge = dict(cible=c, dir=self.face, dist=max(0.3, min(le - 1.1, b["chargeDistance"])), fait=0.0,
                                   debut=t + b["chargeAnticipation"], depart=self.pos, repousses=set())
                self.rech_charge = b["chargeRecharge"]
                return
        if de - e.rayon <= b["epeePortee"]:
            self.viser(e)
            self.cible = e
            self.finir_dans(b["epeeIntervalle"])
            sim.plus_tard(b["epeeInstant"], self._coup)
        else:
            self.aller_vers(e.pos, dt, arret=b["epeePortee"] * 0.8)

    def _coup(self):
        b = self.b
        for e in self.sim.dans_secteur(self.pos, self.face, b["epeePortee"], b["epeeDemiAngle"])[:int(b["epeeCiblesParCoup"])]:
            self.sim.frapper(e, b["epeeDegats"], melee=True, source="épée")

    def _ruee(self, dt):
        b, sim, c = self.b, self.sim, self.charge
        if sim.t < c["debut"]:
            return
        v = b["chargeDistance"] / b["chargeDuree"]
        pas = min(v * dt, c["dist"] - c["fait"])
        self.pos = (self.pos[0] + c["dir"][0] * pas, self.pos[1] + c["dir"][1] * pas)
        c["fait"] += pas
        for e in sim.presents():
            if e is c["cible"] or e.id in c["repousses"]:
                continue
            rel = (e.pos[0] - self.pos[0], e.pos[1] - self.pos[1])
            av = rel[0] * c["dir"][0] + rel[1] * c["dir"][1]
            lat = abs(rel[0] * c["dir"][1] - rel[1] * c["dir"][0])
            if -0.5 <= av <= 1.0 and lat <= b["chargeLargeur"] / 2 + e.rayon:
                c["repousses"].add(e.id)
                sim.etourdir(e, b["chargeEtourdiRepousses"])
        if c["fait"] >= c["dist"] - 1e-6:
            force = max(0.0, min(1.0, c["fait"] / b["chargeDistance"]))
            deg = b["chargeDegatsMin"] + (b["chargeDegatsMax"] - b["chargeDegatsMin"]) * force
            e = c["cible"]
            if e.vivant and dist(e.pos, self.pos) < 2.6 + e.rayon:
                sim.frapper(e, deg, melee=True, source="charge")
                sim.etourdir(e, b["chargeEtourdiCible"])
            self.charge = None
            self.finir_dans(0.0)


class Viking(Heros):
    pv_max_cle = "vikingPV"

    def __init__(self, sim):
        super().__init__(sim)
        self.rage = self.b["rageMin"]
        self.dernier_coup = -99.0
        self.rech_rugir = 0.0
        self.rech_saut = 0.0
        self.tournante = False
        self.prochain_tic = 0.0

    def gagner(self, n, continu=False):
        if n > 0:
            self.dernier_coup = self.sim.t
            self.rage = min(self.b["rageMax"], self.rage + n * (self.b["tournanteRageParTic"] if continu else self.b["rageParTouche"]))

    def pas(self, t, dt):
        b, sim = self.b, self.sim
        self.rech_rugir = max(0.0, self.rech_rugir - dt)
        self.rech_saut = max(0.0, self.rech_saut - dt)
        if t - self.dernier_coup > b["rageDelaiBaisse"] and not self.tournante:
            if self.rage > b["rageMin"]:
                self.rage = max(b["rageMin"], self.rage - b["rageBaisse"] * dt)
            else:
                self.rage = min(b["rageMin"], self.rage + b["rageBaisse"] * dt)
        autour = lambda r: [e for e in sim.presents() if dist(e.pos, self.pos) - e.rayon <= r]
        if self.tournante:
            self.rage -= b["tournanteRage"] * dt
            e = self.choisir_melee()
            if e is not None and dist(e.pos, self.pos) > 1.2:
                self.aller_vers(e.pos, dt, b["tournanteVitesse"], arret=1.0)
            if t >= self.prochain_tic:
                self.prochain_tic = t + b["tournanteIntervalle"]
                touches = autour(b["tournanteRayon"])
                for x in touches:
                    sim.frapper(x, b["tournanteDegats"], melee=True, continu=True, source="tournante")
                self.gagner(len(touches), continu=True)
            if self.rage <= 0 or len(autour(b["tournanteRayon"] + 0.5)) < 2:
                self.tournante = False
                self.rage = max(0.0, self.rage)
                self.finir_dans(0.0)
            return
        if self.occupe():
            return
        # Rugissement : provoque ce qui est à rugissementRayon m ; Peau de fer.
        if self.rech_rugir <= 0 and self.rage >= b["rugissementRage"] and len(autour(b["rugissementRayon"])) >= self.p["rugir_seuil"]:
            self.rage -= b["rugissementRage"]
            self.rech_rugir = b["rugissementRecharge"]
            self.finir_dans(CODE["cri_fin"])
            sim.plus_tard(CODE["cri"], self._cri)
            return
        # Saut percutant : vers le groupe le plus dense à portée de bond.
        if self.rech_saut <= 0 and self.rage >= b["sautRage"]:
            meilleur = None
            for c in sim.presents():
                dc = dist(c.pos, self.pos)
                if dc > b["sautDistance"] + 1.0 or math.hypot(*c.pos) > LAISSE + 2:
                    continue
                n = sum(1 for x in sim.presents() if dist(x.pos, c.pos) - x.rayon <= b["sautRayon"] - 0.3)
                if meilleur is None or n > meilleur[0]:
                    meilleur = (n, c)
            proches = sum(1 for x in sim.presents() if math.hypot(*x.pos) <= LAISSE + 2)
            if meilleur and meilleur[0] >= min(self.p["saut_seuil"], max(1, proches)):
                self.rage -= b["sautRage"]
                self.rech_saut = b["sautRecharge"]
                c = meilleur[1]
                self.viser(c)
                d = min(b["sautDistance"], max(0.0, dist(c.pos, self.pos) - 1.0))
                arrivee = (self.pos[0] + self.face[0] * d, self.pos[1] + self.face[1] * d)
                self.finir_dans(CODE["saut_fin"])
                sim.plus_tard(CODE["saut_impact"], lambda a=arrivee: self._atterrir(a))
                return
        if self.rage >= b["tournanteRageMin"] and len(autour(b["tournanteRayon"])) >= self.p["tournante_seuil"]:
            self.tournante = True
            self.prochain_tic = t
            return
        e = self.choisir_melee()
        if e is None:
            self.aller_vers((0.0, POSTE), dt)
            return
        if dist(e.pos, self.pos) - e.rayon <= b["hachePortee"]:
            self.viser(e)
            self.cible = e
            self.finir_dans(b["hacheIntervalle"])
            sim.plus_tard(b["hacheInstant"], self._coup)
        else:
            self.aller_vers(e.pos, dt, arret=b["hachePortee"] * 0.8)

    def _coup(self):
        b = self.b
        touches = self.sim.dans_secteur(self.pos, self.face, b["hachePortee"], b["hacheDemiAngle"])
        for e in touches:
            self.sim.frapper(e, b["hacheDegats"], melee=True, source="hache")
        self.gagner(len(touches))

    def _cri(self):
        b, sim = self.b, self.sim
        self.reduction = (b["peauDeFerReduction"], sim.t + b["peauDeFerDuree"])
        for e in sim.presents():
            if dist(e.pos, self.pos) <= b["rugissementRayon"]:
                e.provoque = sim.t + b["rugissementProvocation"]
                e.cible = "H"

    def _atterrir(self, arrivee):
        b, sim = self.b, self.sim
        self.pos = arrivee
        point = (arrivee[0] + self.face[0], arrivee[1] + self.face[1])
        n = 0
        for e in sim.presents():
            if dist(e.pos, point) - e.rayon <= b["sautRayon"]:
                sim.frapper(e, b["sautDegats"], melee=True, source="saut")
                sim.etourdir(e, b["sautEtourdi"])
                n += 1
        self.gagner(n)

class Mage(Heros):
    pv_max_cle = "magePV"
    melee = False

    def __init__(self, sim):
        super().__init__(sim)
        self.mana = self.b["manaMax"]
        self.cone = False
        self.prochain_tic = 0.0
        self.derniere_boule = -99.0
        # Refonte du kit (GameBalance du 01/10/2026) : grande boule (LB) et mur de flammes (RB), si les champs existent.
        b = self.b
        self.grande = all(k in b for k in ("grandeBouleDegats", "grandeBouleDegatsZone", "grandeBouleRayon", "grandeBouleMana",
                                           "grandeBouleRecharge", "grandeBouleInstant", "grandeBouleDuree", "grandeBouleVitesse"))
        self.mur_ok = all(k in b for k in ("murLongueur", "murDistance", "murEpaisseur", "murDuree", "murMana", "murRecharge",
                                           "murInstant", "murGeste", "murRalenti", "murRalentiDuree", "murIntervallePalier"))
        self.rech_grande = 0.0
        self.rech_mur = 0.0
        self.mur = None   # (centre, direction de la ligne, fin)
        self.mur_dans = {}

    def engage(self):
        return any(dist(e.pos, self.pos) <= self.b["boulePortee"] for e in self.sim.presents())

    def _maj_mur(self, t):
        """Mur de flammes : qui y entre monte d'un palier de brûlure, puis d'un palier toutes les murIntervallePalier s ; Ralenti."""
        b, sim = self.b, self.sim
        if self.mur is None:
            return
        centre, ligne, fin = self.mur
        if t >= fin:
            self.mur = None
            self.mur_dans = {}
            return
        for e in sim.presents():
            rel = (e.pos[0] - centre[0], e.pos[1] - centre[1])
            le = rel[0] * ligne[0] + rel[1] * ligne[1]
            lat = abs(rel[0] * ligne[1] - rel[1] * ligne[0])
            if abs(le) <= b["murLongueur"] / 2 and lat <= b["murEpaisseur"] / 2 + e.rayon:
                prochain = self.mur_dans.get(e.id)
                if prochain is None or t >= prochain:
                    sim.attiser(e, 1.0 if b["_paliers"] else 0)
                    self.mur_dans[e.id] = t + b["murIntervallePalier"]
                sim.ralentir(e, b["murRalentiDuree"], b["murRalenti"])
            elif e.id in self.mur_dans:
                del self.mur_dans[e.id]

    def pas(self, t, dt):
        b, sim = self.b, self.sim
        self.rech_grande = max(0.0, self.rech_grande - dt)
        self.rech_mur = max(0.0, self.rech_mur - dt)
        self._maj_mur(t)
        if not self.cone:
            self.mana = min(b["manaMax"], self.mana + b["manaRegen"] * dt)
        poste = self.poste()
        if self.cone:
            self.mana -= b["coneMana"] * dt
            dans = sim.dans_secteur(self.pos, self.face, b["conePortee"], b["coneDemiAngle"])
            if t >= self.prochain_tic:
                self.prochain_tic = t + CODE["cone_tic"]
                for e in dans:
                    sim.frapper(e, b["coneDegats"] * 0.25, continu=True, source="cône")
                    if b.get("coneRalenti", 0) > 0:
                        sim.ralentir(e, b.get("coneRalentiDuree", 0.5), b["coneRalenti"])
                    if b["_paliers"]:
                        sim.attiser(e, b["brulureRemplissageCone"])
                    else:
                        sim.attiser(e, 0)
            if self.mana <= 0 or len(dans) < 2:
                self.cone = False
                self.mana = max(0.0, self.mana)
                self.finir_dans(0.0)
            return
        if self.occupe():
            return
        if dist(self.pos, poste) > 0.5:
            self.aller_vers(poste, dt)
        seuil_zone = self.p["saut_seuil"]   # même lecture du groupe que le saut du viking : 3 (moyen), 2 (bon)
        # Mur de flammes : en travers du chemin du groupe qui approche (à murDistance m devant le mage).
        if self.mur_ok and self.rech_mur <= 0 and self.mana >= b["murMana"]:
            venant = [e for e in sim.presents() if 4.0 < dist(e.pos, self.pos) <= 14.0 and e.cible != "N"]
            if len(venant) >= seuil_zone + 1:
                gx = sum(e.pos[0] for e in venant) / len(venant)
                gy = sum(e.pos[1] for e in venant) / len(venant)
                axe = norme((gx - self.pos[0], gy - self.pos[1]))
                self.face = axe
                self.mana -= b["murMana"]
                self.rech_mur = b["murRecharge"]
                self.finir_dans(b["murGeste"])
                centre = (self.pos[0] + axe[0] * b["murDistance"], self.pos[1] + axe[1] * b["murDistance"])
                ligne = (-axe[1], axe[0])
                sim.plus_tard(b["murInstant"], lambda c=centre, l=ligne: setattr(self, "mur", (c, l, sim.t + b["murDuree"])))
                return
        # Grande boule de feu : sur le groupe le plus fourni dans son grand rayon.
        if self.grande and self.rech_grande <= 0 and self.mana >= b["grandeBouleMana"]:
            cands = [e for e in sim.presents() if dist(e.pos, self.pos) <= b["boulePortee"]]
            if cands:
                c = max(cands, key=lambda e: (sum(1 for x in cands if dist(x.pos, e.pos) - x.rayon <= b["grandeBouleRayon"]), -e.id))
                n = sum(1 for x in cands if dist(x.pos, c.pos) - x.rayon <= b["grandeBouleRayon"])
                if n >= seuil_zone or len(cands) <= 1:
                    self.viser(c)
                    self.mana -= b["grandeBouleMana"]
                    self.rech_grande = b["grandeBouleRecharge"]
                    self.finir_dans(b["grandeBouleDuree"])
                    directe = self.rng.random() < self.p["boule_directe"]
                    ang = self.rng.uniform(0, 2 * math.pi)
                    vol = dist(c.pos, self.pos) / b["grandeBouleVitesse"]
                    sim.plus_tard(b["grandeBouleInstant"] + vol, lambda e=c, d=directe, a=ang: self._exploser(e, d, a, grande=True))
                    return
        # Cône : groupe au contact du bon côté, mana suffisant.
        if self.mana >= max(b["coneMana"] * 0.25, self.p["cone_mana_depart"]):
            proches = [e for e in sim.presents() if dist(e.pos, self.pos) - e.rayon <= b["conePortee"]]
            meilleur = None
            for c in proches:
                face = norme((c.pos[0] - self.pos[0], c.pos[1] - self.pos[1]))
                n = len(sim.dans_secteur(self.pos, face, b["conePortee"], b["coneDemiAngle"], proches))
                if meilleur is None or n > meilleur[0]:
                    meilleur = (n, face)
            if meilleur and meilleur[0] >= self.p["cone_seuil"]:
                self.face = meilleur[1]
                self.cone = True
                self.prochain_tic = t + CODE["cone_tic"]
                return
        if t - self.derniere_boule < b["bouleIntervalle"]:
            return
        cands = [e for e in sim.presents() if dist(e.pos, self.pos) <= b["boulePortee"]]
        if not cands:
            return
        # Cible : le groupe le plus fourni dans le rayon de l'explosion, à égalité le plus proche de Nyxessa.
        def score(e):
            n = sum(1 for x in cands if dist(x.pos, e.pos) - x.rayon <= b["bouleRayon"])
            return (-n, round(math.hypot(*e.pos), 1), e.id)
        e = min(cands, key=score)
        self.viser(e)
        self.derniere_boule = t
        self.finir_dans(CODE["boule_occupe"])
        vol = dist(e.pos, self.pos) / b["bouleVitesse"]
        directe = self.rng.random() < self.p["boule_directe"]
        ang = self.rng.uniform(0, 2 * math.pi)
        sim.plus_tard(b["bouleInstant"] + vol, lambda e=e, d=directe, a=ang: self._exploser(e, d, a))

    def _exploser(self, e, directe, ang, grande=False):
        b, sim = self.b, self.sim
        point = e.pos if directe else (e.pos[0] + math.cos(ang) * 1.2, e.pos[1] + math.sin(ang) * 1.2)
        if grande:
            direct, zone, rayon = b["grandeBouleDegats"], b["grandeBouleDegatsZone"], b["grandeBouleRayon"]
        else:
            direct, zone, rayon = b["bouleDegats"], b["bouleDegatsZone"], b["bouleRayon"]
        touches = []
        if directe and e.vivant:
            touches.append((e, direct))
        for x in sim.presents():
            if x is e and directe:
                continue
            if dist(x.pos, point) - x.rayon <= rayon:
                touches.append((x, zone))
        for x, d in touches:
            # Hypothèse : la grande boule allume la brûlure comme la boule (son code n'est pas encore écrit le 01/10/2026).
            if sim.frapper(x, d, source="grande boule" if grande else "boule") > 0:
                self.mana = min(b["manaMax"], self.mana + b["manaParTouche"])
            sim.attiser(x, b["brulureRemplissageBoule"] if b["_paliers"] else 0)


class Rodeur(Heros):
    pv_max_cle = "rodeurPV"
    melee = False

    def __init__(self, sim):
        super().__init__(sim)
        self.rech_nuee = 0.0
        self.rech_roulade = 0.0

    def engage(self):
        return any(dist(e.pos, self.pos) <= PORTEE_UTILE_ARC for e in self.sim.presents())

    def choisir(self):
        cands = [e for e in self.sim.presents() if dist(e.pos, self.pos) <= PORTEE_UTILE_ARC]
        if not cands:
            return None
        frappent = [e for e in cands if e.cible in ("N", "H") and (math.hypot(*e.pos) <= 4 or dist(e.pos, self.pos) <= 4)]
        pool = frappent or cands
        return min(pool, key=lambda e: (math.hypot(*e.pos), e.id))

    def pas(self, t, dt):
        b, sim = self.b, self.sim
        self.rech_nuee = max(0.0, self.rech_nuee - dt)
        self.rech_roulade = max(0.0, self.rech_roulade - dt)
        if self.occupe():
            return
        poste = self.poste()
        if dist(self.pos, poste) > 0.5:
            self.aller_vers(poste, dt)
        presents = sim.presents()
        # Nuée de flèches.
        if self.rech_nuee <= 0:
            cands = [e for e in presents if dist(e.pos, self.pos) <= b["nueePortee"]]
            if cands:
                if self.p["nuee_optimale"]:
                    centre = max(cands, key=lambda c: (sum(1 for x in cands if dist(x.pos, c.pos) - x.rayon <= b["nueeRayon"] - 0.5), -c.id)).pos
                else:
                    centre = min(cands, key=lambda c: (math.hypot(*c.pos), c.id)).pos
                n = sum(1 for x in cands if dist(x.pos, centre) - x.rayon <= b["nueeRayon"])
                if n >= 2 or (n >= 1 and len(cands) <= 2):
                    self.rech_nuee = b["nueeRecharge"]
                    self.finir_dans(CODE["nuee_occupe"])
                    debut = CODE["nuee_lancer"] + CODE["nuee_marqueur"]
                    pas_ = CODE["nuee_duree_salves"] / b["nueeSalves"]
                    for i in range(int(b["nueeSalves"])):
                        sim.plus_tard(debut + i * pas_, lambda c=centre, i=i: self._salve_nuee(c, i))
                    return
        # Roulade arrière + salve en éventail.
        if self.rech_roulade <= 0:
            e = self.choisir()
            danger = any(dist(x.pos, self.pos) <= 5 for x in presents)
            if e is not None and (danger or (self.p["roulade_offensive"] and dist(e.pos, self.pos) <= 20)):
                self.rech_roulade = b["rouladeRecharge"]
                self.viser(e)
                self.finir_dans(b["esquiveDuree"])
                sim.plus_tard(CODE["salve_delai"], lambda f=self.face, o=self.pos: self._salve(o, f))
                return
        e = self.choisir()
        if e is None:
            return
        self.viser(e)
        pleine = self.rng.random() < self.p["arc_pleine"]
        charge = 1.0 if pleine else 0.5
        tenue = charge * b["arcCharge"]
        # RT maintenu : il rebande seul après le lâcher (arcIntervalle + 0,15 = 0,3 s, autant que l'état Lacher).
        self.finir_dans(tenue + max(CODE["rodeur_lacher"], b["arcIntervalle"] + 0.15))
        sim.plus_tard(tenue, lambda c=charge, e=e: self._lacher(c, e))

    def _lacher(self, charge, e):
        b, sim = self.b, self.sim
        if not e.vivant:
            e = self.choisir()
            if e is None:
                return
        self.viser(e)
        degats = b["arcDegatsMin"] + (b["arcDegatsMax"] - b["arcDegatsMin"]) * charge
        vitesse = b["arcVitesseMin"] + (b["arcVitesseMax"] - b["arcVitesseMin"]) * charge
        touche = self.rng.random() < self.p["arc_touche"]
        tete = touche and self.rng.random() < self.p["arc_tete"]
        etourdi = b["arcEtourdiPleineCharge"] if charge >= 0.999 else 0.0

        def impact():
            if touche and e.vivant:
                sim.frapper(e, degats * (b["arcTete"] if tete else 1.0), critique=tete, source="arc")
                if etourdi > 0:
                    sim.etourdir(e, etourdi)
        sim.plus_tard(dist(e.pos, self.pos) / vitesse, impact)

    def _salve_nuee(self, centre, i):
        b, sim = self.b, self.sim
        for e in sim.presents():
            if dist(e.pos, centre) - e.rayon <= b["nueeRayon"]:
                sim.frapper(e, b["nueeDegatsSalve"], source="nuée")
                if b["nueeRalentiForce"] > 0 and i % 2 == 0:
                    sim.ralentir(e, b["nueeRalentiDuree"], b["nueeRalentiForce"])

    def _salve(self, origine, face):
        b, sim = self.b, self.sim
        n = int(b["salveFleches"])
        # La roulade l'emporte de rouladeDistance m en arrière.
        self.pos = (self.pos[0] - face[0] * b["rouladeDistance"], self.pos[1] - face[1] * b["rouladeDistance"])
        r = math.hypot(*self.pos)
        if r < 2.0:
            self.pos = (self.pos[0] * 2.0 / max(r, 1e-6), self.pos[1] * 2.0 / max(r, 1e-6))
        deja = set()
        for k in range(n):
            a = math.radians(-b["salveEcart"] + 2 * b["salveEcart"] * k / max(1, n - 1)) if n > 1 else 0.0
            d = (face[0] * math.cos(a) - face[1] * math.sin(a), face[0] * math.sin(a) + face[1] * math.cos(a))
            meilleur = None
            for e in sim.presents():
                rel = (e.pos[0] - origine[0], e.pos[1] - origine[1])
                av = rel[0] * d[0] + rel[1] * d[1]
                lat = abs(rel[0] * d[1] - rel[1] * d[0])
                if 0 < av <= PORTEE_UTILE_ARC and lat <= e.rayon + 0.1:
                    if meilleur is None or av < meilleur[0]:
                        meilleur = (av, e)
            if meilleur is None:
                continue
            e = meilleur[1]
            if self.rng.random() >= self.p["salve_touche"]:
                continue
            tete = self.rng.random() < self.p["salve_tete"]
            deja.add(e.id)
            sim.plus_tard(meilleur[0] / b["salveVitesse"],
                          lambda e=e, tt=tete: e.vivant and sim.frapper(e, b["salveDegats"] * (b["arcTete"] if tt else 1.0), critique=tt, source="salve"))


class Assassin(Heros):
    pv_max_cle = "assassinPV"

    def __init__(self, sim):
        super().__init__(sim)
        self.rech_arbalete = 0.0
        self.rech_grenade = 0.0
        self.rech_bond = 0.0
        self.dernier_combat = -99.0
        self.furtif = True
        self.facteur = 1.0

    def vitesse(self):
        return self.b["marcheDiscrete"] if self.furtif else self.b["assassinVitesse"]

    def dans_fumee(self):
        f = self.sim.fumee
        return f is not None and self.sim.t < f[1] and dist(self.pos, f[0]) < self.b["_fumeeRayon"]

    def visible_par(self, e, dh):
        if self.dans_fumee():
            return False
        if not self.furtif:
            return True
        b = self.b
        if dh <= b["assassinDetectionDos"]:
            return True
        return dh <= b["assassinDetectionVue"] and angle_entre(e.face, (self.pos[0] - e.pos[0], self.pos[1] - e.pos[1])) <= b["assassinDetectionAngle"]

    def repere(self):
        self.dernier_combat = self.sim.t
        self.furtif = False

    def sur_touche(self):
        self.repere()

    def pas(self, t, dt):
        b, sim = self.b, self.sim
        self.rech_arbalete = max(0.0, self.rech_arbalete - dt)
        self.rech_grenade = max(0.0, self.rech_grenade - dt)
        self.rech_bond = max(0.0, self.rech_bond - dt)
        fumee = self.dans_fumee()
        if fumee:
            self.dernier_combat = -99.0
        if fumee or t - self.dernier_combat > b["horsCombat"]:
            self.furtif = True
        if self.occupe():
            return
        presents = sim.presents()
        cands = self.candidats_melee()
        # Grenade fumigène : dans la mêlée (3 ennemis à 5 m), selon le profil.
        if self.rech_grenade <= 0 and sum(1 for e in presents if dist(e.pos, self.pos) <= 5) >= 3:
            self.rech_grenade = b["grenadeRecharge"]
            if self.rng.random() < self.p["grenade_usage"]:
                self.finir_dans(CODE["grenade_occupe"])
                centre = (self.pos[0] + self.face[0] * 1.0, self.pos[1] + self.face[1] * 1.0)
                sim.plus_tard(CODE["grenade_lancer"], lambda c=centre: self._fumee(c))
                return
        if not cands:
            # Rien dans la laisse : arbalète sur ce qui approche.
            loin = [e for e in presents if dist(e.pos, self.pos) <= b["arbaletePortee"]]
            if loin and self.rech_arbalete <= 0:
                self._tirer(min(loin, key=lambda e: (math.hypot(*e.pos), e.id)))
                return
            self.aller_vers((0.0, POSTE), dt)
            return
        # Pas de l'ombre : derrière une cible à portée de réticule.
        if self.rech_bond <= 0:
            pool = [e for e in cands if 2.0 < dist(e.pos, self.pos) <= b["pasOmbrePorteeCible"]]
            if pool:
                self.rech_bond = b["pasOmbreRecharge"]
                if self.rng.random() < self.p["bond_usage"]:
                    c = max(pool, key=lambda e: (e.elite, e.pv, -e.id))
                    arrivee = (c.pos[0] - c.face[0] * b["pasOmbreArret"], c.pos[1] - c.face[1] * b["pasOmbreArret"])
                    if dist(arrivee, self.pos) <= b["pasOmbreDistance"] + b["pasOmbreArret"]:
                        self.pos = arrivee
                        self.viser(c)
                        self.cible = c
                        self.invulnerable = t + b["pasOmbreDuree"] + 0.05
                        self.finir_dans(b["pasOmbreDuree"], reaction=False)
                        return
        e = self._choisir(cands)
        if e is None:
            return
        de = dist(e.pos, self.pos) - e.rayon
        if self.p["assassin_placement"] and e.cible == "N" and not self._dans_le_dos(e):
            derriere = (e.pos[0] - e.face[0] * 1.2, e.pos[1] - e.face[1] * 1.2)
            self.aller_vers(derriere, dt, arret=0.2)
            return
        if de <= b["daguePortee"]:
            self.viser(e)
            self.cible = e
            furtif = self.furtif
            self.finir_dans(b["dagueIntervalle"])
            sim.plus_tard(b["dagueInstant"], lambda e=e, f=furtif: self._dague(e, f))
        else:
            self.aller_vers(e.pos, dt, arret=b["daguePortee"] * 0.7)
            if self.rech_arbalete <= 0 and de > 6:
                self._tirer(e)

    def _dans_le_dos(self, e):
        return angle_entre(e.face, (self.pos[0] - e.pos[0], self.pos[1] - e.pos[1])) > self.b["angleDos"]

    def _choisir(self, cands):
        b = self.b
        if not self.p["dos_alterner"]:
            if self.cible is not None and self.cible.vivant and self.cible in cands:
                return self.cible
            return min(cands, key=lambda e: (dist(e.pos, self.pos), e.id))
        proches = [e for e in cands if dist(e.pos, self.pos) - e.rayon <= b["daguePortee"] + 1.5]
        execs = [e for e in proches if not e.elite and e.pv / e.pvmax < b["executionSeuil"]]
        if execs:
            return min(execs, key=lambda e: (dist(e.pos, self.pos), e.id))
        # Garder le dos : une cible qui frappe Nyxessa et n'a pas encore pris deux coups de suite (riposte).
        dos = [e for e in cands if e.cible == "N" and e.riposte_coups < b["riposteCoups"] - 1]
        if dos:
            return min(dos, key=lambda e: (dist(e.pos, self.pos), e.id))
        return min(cands, key=lambda e: (dist(e.pos, self.pos), e.id))

    def _dague(self, visee, furtif):
        b, sim = self.b, self.sim
        dans = sim.dans_secteur(self.pos, self.face, b["daguePortee"], b["dagueDemiAngle"])
        if not dans:
            return
        e = visee if visee in dans else dans[0]
        dos = self._dans_le_dos(e)
        mult = b["critiqueFurtifDos"] if (furtif and dos) else b["critiqueDos"] if dos else b["critiqueFurtif"] if furtif else 1.0
        crit = furtif or dos
        deg = b["dagueDegats"] * mult
        if e.vivant and e.pv / e.pvmax < b["executionSeuil"] and not e.mannequin:
            if e.elite:
                deg = b["dagueDegats"] * max(mult, b["executionElite"])
            else:
                deg = max(deg, e.pv + 1)
            crit = True
            self.rech_bond = 0.0
        sim.frapper(e, deg, critique=crit, melee=True, source="dague")
        self.dernier_combat = sim.t
        if not self.dans_fumee():
            self.furtif = False

    def _tirer(self, e):
        b, sim = self.b, self.sim
        self.rech_arbalete = b["arbaleteRecharge"]
        self.viser(e)
        self.repere()
        self.finir_dans(CODE["arbalete_occupe"] + 0.3)   # + 0,3 s : LT pour sortir l'arbalète
        touche = self.rng.random() < self.p["arbalete_touche"]
        tete = touche and self.rng.random() < self.p["arbalete_tete"]
        sim.plus_tard(dist(e.pos, self.pos) / b["arbaleteVitesse"],
                      lambda: touche and e.vivant and sim.frapper(e, b["arbaleteDegats"] * (b["arbaleteTete"] if tete else 1.0), critique=tete, source="arbalète"))

    def _fumee(self, centre):
        sim = self.sim
        sim.fumee = (centre, sim.t + self.b["grenadeNuage"])
        for e in sim.presents():
            if e.cible == "H" and e.provoque < sim.t:
                e.cible = "route"


HEROS = {"Paladin": Paladin, "Viking": Viking, "Mage": Mage, "Rôdeur": Rodeur, "Assassin": Assassin}


# ======================================================================== Mesures

def mesurer(b, classe, nuit, profil, graines=5, palier=1):
    """Moyenne sur `graines` tirages : vague complète de la nuit + mannequin mono-cible (180 s)."""
    acc = []
    for g in range(graines):
        s = Simulation(b, classe, nuit, profil, g, palier_missiles=palier).lancer()
        m = Simulation(b, classe, nuit, profil, g, palier_missiles=0, mono=True).lancer()
        st = s.stats
        vagues = []
        for w in sorted(s.vague_n):
            fin = s.vague_fin.get(w)
            depart = min(e.apparition for e in s.ennemis if e.vague == w)
            vagues.append(None if fin is None else fin - depart)
        acc.append(dict(
            dps_mono=m.stats["utiles"] / m.duree,
            dps_combat=st["utiles"] / max(1e-6, st["engage"]),
            dps_nuit=st.get("utiles_aube", st["utiles"]) / s.b["dureeNuit"],
            part_crit=st["crit"] / max(1e-6, st["utiles"]),
            part_brulure=st["brulure"] / max(1e-6, st["utiles"]),
            vagues=vagues,
            nyx_degats=st["nyx_degats"], nyx_atteinte=st["nyx_atteinte"], recu=st["recu"],
            survivants=s.survivants_aube if s.survivants_aube is not None else sum(1 for e in s.ennemis if e.vivant),
            total=len(s.ennemis),
            sources={k: v / max(1e-6, st["utiles"]) for k, v in st["par_source"].items()},
        ))
    moy = {}
    for k in acc[0]:
        if k == "vagues":
            moy[k] = []
            for i in range(len(acc[0]["vagues"])):
                vals = [a["vagues"][i] for a in acc if i < len(a["vagues"])]
                moy[k].append(None if any(v is None for v in vals) else sum(vals) / len(vals))
        elif k == "sources":
            cles = sorted({c for a in acc for c in a["sources"]})
            moy[k] = {c: sum(a["sources"].get(c, 0.0) for a in acc) / len(acc) for c in cles}
        else:
            moy[k] = sum(a[k] for a in acc) / len(acc)
    return moy


def indices(lignes):
    """Indice vague (dégâts utiles avant l'aube) et indice mono, rapportés à la moyenne des classes présentes."""
    mv = sum(l["dps_nuit"] for l in lignes.values()) / len(lignes)
    mm = sum(l["dps_mono"] for l in lignes.values()) / len(lignes)
    for l in lignes.values():
        l["indice"] = l["dps_nuit"] / mv
        l["indice_mono"] = l["dps_mono"] / mm
    return lignes


def tableau(b, nuits, profils, classes, graines, palier):
    res = {}
    for pr in profils:
        for n in nuits:
            lignes = indices({c: mesurer(b, c, n, pr, graines, palier) for c in CLASSES})
            res[(pr, n)] = {c: lignes[c] for c in classes}
    return res


def fr(x, dec=1):
    if x is None:
        return "—"
    s = ("%." + str(dec) + "f") % x
    if "." in s:
        s = s.rstrip("0").rstrip(".")
    return s.replace(".", ",").replace("-", "−")


def marque(ind, seuil):
    c = fr(ind, 2)
    if seuil and abs(ind - 1) * 100 > seuil:
        return "**%s** %s" % (c, "▲" if ind > 1 else "▼")
    return c


def markdown(res, seuil, palier, b):
    out = []
    for (pr, n), lignes in res.items():
        total = next(iter(lignes.values()))["total"]
        out.append("#### Nuit %d, profil %s (%d squelettes, PV ×%s ; missiles de Nyxessa : %s)" % (
            n, pr, total, fr(b["multiplicateurPV"][n - 1], 2), "palier %d" % palier if palier else "aucun"))
        out.append("")
        out.append("| Classe | DPS mono | Indice mono | DPS en combat | DPS de nuit | **Indice vague** | Part critiques | Vidage des vagues (s) | Ennemis à Nyxessa | Dégâts à Nyxessa | Survivants à l'aube | Dégâts reçus |")
        out.append("|---|---|---|---|---|---|---|---|---|---|---|---|")
        for c, l in lignes.items():
            out.append("| %s | %s | %s | %s | %s | %s | %s %% | %s | %s | %s | %s | %s |" % (
                c, fr(l["dps_mono"]), marque(l["indice_mono"], seuil), fr(l["dps_combat"]), fr(l["dps_nuit"]),
                marque(l["indice"], seuil), fr(100 * l["part_crit"], 0),
                " / ".join(fr(v, 0) for v in l["vagues"]), fr(l["nyx_atteinte"], 1), fr(l["nyx_degats"], 0),
                fr(l["survivants"], 1), fr(l["recu"], 0)))
        out.append("")
        out.append("Origine des dégâts : " + " ; ".join(
            "%s %s" % (c, ", ".join("%s %s %%" % (k, fr(100 * v, 0)) for k, v in sorted(l["sources"].items(), key=lambda x: -x[1]) if v >= 0.01))
            for c, l in lignes.items()) + ".")
        out.append("")
    if seuil:
        out.append("▲ / ▼ : plus de ±%s %% de la moyenne des cinq classes." % fr(seuil, 0))
    return "\n".join(out)


VARIANTES = [
    ("Actuel (50, tête ×2, tir rapide 10)", {}),
    ("A : pleine charge 45", {"arcDegatsMax": 45.0}),
    ("B : tête ×1,75", {"arcTete": 1.75}),
    ("C : pleine charge 45, tête ×1,8", {"arcDegatsMax": 45.0, "arcTete": 1.8}),
    ("D : pleine charge 44, tête ×1,75, tir rapide 9", {"arcDegatsMax": 44.0, "arcTete": 1.75, "arcDegatsMin": 9.0}),
    ("E : pleine charge 42, tête ×1,8", {"arcDegatsMax": 42.0, "arcTete": 1.8}),
    ("F : pleine charge 48, tête ×1,8", {"arcDegatsMax": 48.0, "arcTete": 1.8}),
    ("G : pleine charge 47, tête ×1,85", {"arcDegatsMax": 47.0, "arcTete": 1.85}),
    ("H : C + nuée 16 par salve (compensation de zone)", {"arcDegatsMax": 45.0, "arcTete": 1.8, "nueeDegatsSalve": 16.0}),
]


def coups_pour_tuer(b, pv):
    """Flèches à pleine charge pour abattre `pv` : corps seul, tête seule."""
    corps = b["arcDegatsMax"]
    tete = b["arcDegatsMax"] * b["arcTete"]
    return int(math.ceil(pv / corps - 1e-9)), int(math.ceil(pv / tete - 1e-9))


def nerf(surcharges, nuits, profils, graines, palier):
    """Rôdeur avec chaque variante ; les autres classes ne bougent pas, les moyennes sont recalculées."""
    base = lire_valeurs(surcharges)
    autres = {}
    for pr in profils:
        for n in nuits:
            autres[(pr, n)] = {c: mesurer(base, c, n, pr, graines, palier) for c in CLASSES if c != "Rôdeur"}
    lignes = []
    for nom, sur in VARIANTES:
        bv = lire_valeurs(dict(surcharges or {}, **sur))
        ligne = dict(nom=nom, surcharges=sur, valeurs={}, coups={})
        for typ in ("sbire", "voleur", "guerrier", "mage"):
            ligne["coups"][typ] = coups_pour_tuer(bv, bv[typ]["pv"])
        ligne["coups"]["sbire_n11"] = coups_pour_tuer(bv, bv["sbire"]["pv"] * bv["multiplicateurPV"][10])
        for pr in profils:
            for n in nuits:
                r = mesurer(bv, "Rôdeur", n, pr, graines, palier)
                lot = dict(autres[(pr, n)])
                lot["Rôdeur"] = r
                indices(lot)
                ligne["valeurs"][(pr, n)] = dict(
                    indice=r["indice"], indice_mono=r["indice_mono"], dps_nuit=r["dps_nuit"], dps_mono=r["dps_mono"],
                    part_crit=r["part_crit"],
                    sous=[c for c in ("Assassin", "Paladin") if r["dps_nuit"] < lot[c]["dps_nuit"] - 1e-9],
                    sous_mono=[c for c in ("Assassin", "Paladin") if r["dps_mono"] < lot[c]["dps_mono"] - 1e-9],
                    autres={c: (lot[c]["indice"], lot[c]["indice_mono"]) for c in CLASSES if c != "Rôdeur"})
        v = ligne["valeurs"].values()
        ligne["dans_bande"] = all(0.9 <= x["indice"] <= 1.1 for x in v) and all(x["indice_mono"] <= 1.1 for x in v)
        ligne["jamais_sous"] = not any(x["sous"] for x in v)
        lignes.append(ligne)
    return lignes


def markdown_nerf(lignes, nuits, profils):
    cles = [(pr, n) for pr in profils for n in nuits]
    out = ["Indice vague / indice mono du Rôdeur (1 = moyenne des cinq classes ; « < X » : sous X en dégâts de nuit) :", "",
           "| Variante | " + " | ".join("N%d %s" % (n, pr) for pr, n in cles) + " | Dans ±10 % | Jamais sous Assassin/Paladin |",
           "|---|" + "---|" * (len(cles) + 2)]
    for l in lignes:
        cells = []
        for k in cles:
            v = l["valeurs"][k]
            c = "%s / %s" % (fr(v["indice"], 2), fr(v["indice_mono"], 2))
            if v["sous"]:
                c += " < " + ", ".join(v["sous"])
            cells.append(c)
        out.append("| %s | %s | %s | %s |" % (l["nom"], " | ".join(cells), "oui" if l["dans_bande"] else "non",
                                              "oui" if l["jamais_sous"] else "non"))
    out.append("")
    out.append("DPS mono du Rôdeur (moyen / bon) et flèches à pleine charge pour abattre (corps / tête) :")
    out.append("")
    out.append("| Variante | DPS mono moyen | DPS mono bon | Part critiques (bon) | Sbire 100 PV | Sbire nuit 11 (120) | Voleur 115 | Guerrier 160 | Mage 70 |")
    out.append("|---|---|---|---|---|---|---|---|---|")
    for l in lignes:
        vm = [l["valeurs"][k] for k in cles if k[0] == "moyen"]
        vb = [l["valeurs"][k] for k in cles if k[0] == "bon"]
        moy = lambda vs, key: sum(x[key] for x in vs) / len(vs) if vs else None
        cp = lambda t: "%d / %d" % l["coups"][t]
        out.append("| %s | %s | %s | %s %% | %s | %s | %s | %s | %s |" % (
            l["nom"], fr(moy(vm, "dps_mono")), fr(moy(vb, "dps_mono")), fr(100 * (moy(vb, "part_crit") or 0), 0),
            cp("sbire"), cp("sbire_n11"), cp("voleur"), cp("guerrier"), cp("mage")))
    out.append("")
    ref = lignes[0]["valeurs"]
    out.append("Autres classes, indice vague / mono (Rôdeur actuel) : " + " ; ".join(
        "N%d %s : %s" % (n, pr, ", ".join("%s %s / %s" % (c, fr(x[0], 2), fr(x[1], 2)) for c, x in ref[(pr, n)]["autres"].items()))
        for pr, n in cles) + ".")
    return "\n".join(out)


def main():
    ap = argparse.ArgumentParser(description="Simulateur de vagues : les cinq classes face aux mêmes vagues.")
    ap.add_argument("--nuit", type=int, action="append", help="nuit 1 à 12 (répétable) ; défaut 3, 6, 9, 12")
    ap.add_argument("--profil", choices=sorted(PROFILS), action="append", help="moyen ou bon (défaut : les deux)")
    ap.add_argument("--classe", action="append", help="classe à afficher (les indices restent rapportés aux cinq)")
    ap.add_argument("--json", action="store_true")
    ap.add_argument("--seuil", type=float, default=0.0, help="signale les classes à plus de ±seuil %% de la moyenne")
    ap.add_argument("--graines", type=int, default=5, help="tirages moyennés (défaut 5)")
    ap.add_argument("--palier", type=int, default=1, help="palier des missiles de Nyxessa, 0 = aucun (défaut 1)")
    ap.add_argument("--surcharge", default="", help="champ=valeur,… (essai sans toucher à GameBalance.cs)")
    ap.add_argument("--tete", type=float, help="part des tirs à la tête (arc et arbalète ; salve ×0,6) pour tous les profils")
    ap.add_argument("--nerf", action="store_true", help="variantes du nerf du Rôdeur")
    a = ap.parse_args()
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    sur = {}
    for paire in filter(None, a.surcharge.split(",")):
        k, v = paire.split("=")
        sur[k.strip()] = float(v)
    if a.tete is not None:
        for p in PROFILS.values():
            p["arc_tete"] = p["arbalete_tete"] = a.tete
            p["salve_tete"] = a.tete * 0.6
    nuits = a.nuit or [3, 6, 9, 12]
    for n in nuits:
        if not 1 <= n <= 12:
            sys.stderr.write("Nuit hors de 1 à 12 : %d\n" % n)
            sys.exit(2)
    profils = a.profil or ["moyen", "bon"]
    noms = {c.lower().replace("ô", "o"): c for c in CLASSES}
    classes = CLASSES
    if a.classe:
        classes = []
        for c in a.classe:
            k = c.lower().replace("ô", "o")
            if k not in noms:
                sys.stderr.write("Classe inconnue : %s (%s)\n" % (c, ", ".join(CLASSES)))
                sys.exit(2)
            classes.append(noms[k])
    if a.nerf:
        lignes = nerf(sur, nuits, profils, a.graines, a.palier)
        if a.json:
            print(json.dumps([dict(nom=l["nom"], surcharges=l["surcharges"], dans_bande=l["dans_bande"],
                                   jamais_sous=l["jamais_sous"], coups=l["coups"],
                                   valeurs={"%s_N%d" % k: v for k, v in l["valeurs"].items()}) for l in lignes],
                             ensure_ascii=False, indent=1))
        else:
            print(markdown_nerf(lignes, nuits, profils))
        return
    b = lire_valeurs(sur)
    res = tableau(b, nuits, profils, classes, a.graines, a.palier)
    if a.json:
        print(json.dumps({"%s_N%d" % k: v for k, v in res.items()}, ensure_ascii=False, indent=1))
    else:
        print(markdown(res, a.seuil, a.palier, b))


if __name__ == "__main__":
    main()
