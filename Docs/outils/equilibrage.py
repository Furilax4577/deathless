# -*- coding: utf-8 -*-
"""Audit d'équilibrage des cinq classes de Deathless (bibliothèque standard seulement).

Lit les chiffres de `Assets/Scripts/Jeu/GameBalance.cs` (valeurs par défaut des champs, par expressions régulières),
puis calcule le « modèle commun » décrit dans `Docs/equilibrage-classes.md` et l'imprime en Markdown.

    python Docs/outils/equilibrage.py            # tableaux Markdown sur la sortie standard
    python Docs/outils/equilibrage.py --json     # mêmes chiffres en JSON (pour comparer deux versions)

Hypothèses du modèle (les mêmes que dans le document) :
- cible mono : un guerrier de la nuit 5 (PV × multiplicateurPV[5]) ; groupe : 5 sbires serrés (dans 2,5 m) ;
- « soutenu » = cycle complet répété sans temps mort, compétences à recharge comptées au prorata (dégâts / recharge) ;
- « pic 3 s » = meilleure séquence sur 3 s, jauge et recharges pleines, en partant à zéro ;
- l'attaque tournante et le cône sont bornés par leur jauge ; la rage du viking part à `rageMin` (plancher, 30 depuis le
  27/09/2026 ; 0 avant) ;
- le tir à la tête et le coup dans le dos sont donnés à part (dépendent du joueur, pas des chiffres).
Le script ne lit pas l'asset `GameBalance.asset` : si l'asset diverge du code, c'est le code qui est audité.
"""
import json
import os
import re
import sys

RACINE = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
CHEMIN_BALANCE = os.path.join(RACINE, "Assets", "Scripts", "Jeu", "GameBalance.cs")

RE_CHAMP = re.compile(r"public\s+(float|int)\s+(\w+)\s*=\s*(-?[\d.]+)f?\s*;")
RE_TABLEAU = re.compile(r"public\s+(float|int)\[\]\s+(\w+)\s*=\s*\{([^}]*)\}\s*;")
RE_STATS = re.compile(r"public\s+StatsSquelette\s+(\w+)\s*=\s*new\s+StatsSquelette\s*\{([^}]*)\}")


def lire_balance(chemin=CHEMIN_BALANCE):
    """Champs numériques de GameBalance : nom → valeur (float), tableaux → liste, StatsSquelette → dict."""
    with open(chemin, encoding="utf-8") as f:
        texte = f.read()
    # La classe StatsSquelette (en bas du fichier) redéclare `vitesse`, `pv`… : on ne lit que GameBalance.
    coupe = texte.find("public class StatsSquelette")
    if coupe > 0:
        texte = texte[:coupe]
    b = {}
    for typ, nom, val in RE_CHAMP.findall(texte):
        b[nom] = float(val)
    for typ, nom, corps in RE_TABLEAU.findall(texte):
        b[nom] = [float(x.strip().rstrip("f")) for x in corps.split(",") if x.strip()]
    for nom, corps in RE_STATS.findall(texte):
        d = {}
        for paire in corps.split(","):
            if "=" in paire:
                k, v = paire.split("=")
                d[k.strip()] = float(v.strip().rstrip("f"))
        b[nom] = d
    return b


def f(x, dec=1):
    """Nombre en français (virgule), sans zéros inutiles."""
    s = ("%." + str(dec) + "f") % x
    if "." in s:
        s = s.rstrip("0").rstrip(".")
    return s.replace(".", ",")


def modele(b):
    """Modèle commun : un dictionnaire par classe, mêmes clés partout."""
    nuit = 5
    mult = b["multiplicateurPV"][nuit - 1]
    guerrier_pv = b["guerrier"]["pv"] * mult
    sbire_pv = b["sbire"]["pv"] * mult
    groupe = 5  # sbires serrés
    classes = {}

    # ---------------------------------------------------------------- Paladin
    epee_dps = b["epeeDegats"] / b["epeeIntervalle"]
    epee_cibles = min(groupe, int(b["epeeCiblesParCoup"]))
    charge_cycle = b["chargeAnticipation"] + b["chargeDuree"]
    # Pic 3 s : charge complète (60) puis autant de coups d'épée que le temps restant le permet.
    coups = int((3.0 - charge_cycle - b["epeeInstant"]) // b["epeeIntervalle"]) + 1
    classes["Paladin"] = dict(
        pv=b["herosPV"], vitesse=b["vitesse"],
        dps_mono=epee_dps + b["chargeDegatsMax"] / b["chargeRecharge"],
        dps_mono_base=epee_dps,
        dps_zone=epee_dps * epee_cibles + b["chargeDegatsMax"] / b["chargeRecharge"],
        pic3=b["chargeDegatsMax"] + coups * b["epeeDegats"],
        pic3_zone=b["chargeDegatsMax"] + coups * b["epeeDegats"] * epee_cibles,
        temps_guerrier=guerrier_pv / epee_dps,
        competences=[
            ("Épée", "aucun", "%s s entre deux coups" % f(b["epeeIntervalle"], 2),
             "%s dégâts, %s cibles, %s m, ±%s°" % (f(b["epeeDegats"]), int(b["epeeCiblesParCoup"]), f(b["epeePortee"]), int(b["epeeDemiAngle"]))),
            ("Garde", "%s endurance par dégât bloqué" % f(b["gardeCoutParDegat"]), "—",
             "±%s° ; parade %s s → étourdi %s s ; parfaite %s s → repousse %s m, étourdi %s s" % (
                 int(b["gardeDemiAngle"]), f(b["paradeFenetre"], 2), f(b["paradeEtourdi"]), f(b["paradeParfaiteFenetre"], 2),
                 f(b["paradeParfaiteRepousse"]), f(b["paradeParfaiteEtourdi"]))),
            ("Charge bélier", "aucun", "%s s" % f(b["chargeRecharge"]),
             "%s à %s dégâts, %s m, étourdi %s s (cible) / %s s (traversés)" % (
                 f(b["chargeDegatsMin"]), f(b["chargeDegatsMax"]), f(b["chargeDistance"]), f(b["chargeEtourdiCible"]), f(b["chargeEtourdiRepousses"]))),
            ("Soin d'aura", "aucun", "%s s" % f(b["soinRecharge"]),
             "+%s %% de la vie (%s PV), %s PV/s en moyenne, pour lui et chaque allié à moins de %s m" % (
                 int(b["soinPart"] * 100), f(b["herosPV"] * b["soinPart"]), f(b["herosPV"] * b["soinPart"] / b["soinRecharge"], 2), f(b.get("soinRayonAura", 0)))),
        ],
        survie="%s PV ; garde (%s dégâts bloqués par jauge, +%s/s) ; parade ; soin %s PV / %s s (aura %s m) ; esquive" % (
            f(b["herosPV"]), f(b["endurance"] / b["gardeCoutParDegat"]), f(b["enduranceRegen"]), f(b["herosPV"] * b["soinPart"]), f(b["soinRecharge"]), f(b.get("soinRayonAura", 0))),
        controle="étourdi %s s (charge), %s s (parade), %s s + repousse (parfaite)" % (f(b["chargeEtourdiCible"]), f(b["paradeEtourdi"]), f(b["paradeParfaiteEtourdi"])),
        mobilite="%s m/s ; charge %s m / %s s ; ×%s en garde" % (f(b["vitesse"]), f(b["chargeDistance"]), f(b["chargeRecharge"]), f(b["gardeVitesse"])),
        dependance="aucune",
    )

    # ---------------------------------------------------------------- Viking
    hache_dps = b["hacheDegats"] / b["hacheIntervalle"]
    tourn_dps = b["tournanteDegats"] / b["tournanteIntervalle"]
    tourn_rage_par_cible = b["tournanteRageParTic"] / b["tournanteIntervalle"]
    tourn_seuil = 0.0   # tournante gratuite depuis le 03/10/2026 (plus de seuil de rentabilité en rage)
    rage_par_s_mono = b["rageParTouche"] / b["hacheIntervalle"]
    # Pic 3 s (rage pleine) : saut (impact à 0,66 s) puis coups de hache.
    saut_impact = 0.79 / 1.2 + 0.35
    coups_v = int((3.0 - saut_impact - b["hacheInstant"]) // b["hacheIntervalle"]) + 1
    classes["Viking"] = dict(
        pv=b["vikingPV"], vitesse=b["vikingVitesse"],
        dps_mono=hache_dps + b["sautDegats"] / b["sautRecharge"],
        dps_mono_base=hache_dps,
        dps_zone=hache_dps * groupe + b["sautDegats"] * groupe / b["sautRecharge"],
        pic3=b["sautDegats"] + coups_v * b["hacheDegats"],
        pic3_zone=(b["sautDegats"] + coups_v * b["hacheDegats"]) * groupe,
        temps_guerrier=guerrier_pv / hache_dps,
        competences=[
            ("Hache", "aucun (+%s rage par cible)" % f(b["rageParTouche"]), "%s s entre deux coups" % f(b["hacheIntervalle"], 2),
             "%s dégâts, toutes les cibles, %s m, ±%s°" % (f(b["hacheDegats"]), f(b["hachePortee"]), int(b["hacheDemiAngle"]))),
            ("Attaque tournante", "gratuite (+%s rage par cible et par seconde)" % f(tourn_rage_par_cible, 1), "%s s de recharge, %s s de maintien au plus" % (f(b["tournanteRecharge"]), f(b["tournanteDureeMax"])),
             "%s dégâts / %s s = %s DPS par cible, 360°, %s m" % (
                 f(b["tournanteDegats"]), f(b["tournanteIntervalle"], 2), f(tourn_dps), f(b["tournanteRayon"]))),
            ("Rugissement", "gratuit", "%s s" % f(b["rugissementRecharge"]),
             "provoque %s s à %s m ; Peau de fer −%s %% pendant %s s ; crié en marchant" % (
                 f(b["rugissementProvocation"]), f(b["rugissementRayon"]), int(b.get("peauDeFerReduction", 0) * 100), f(b.get("peauDeFerDuree", 0)))),
            ("Saut percutant", "gratuit", "%s s" % f(b["sautRecharge"]),
             "%s dégâts, %s m de bond, rayon %s m, étourdi %s s" % (f(b["sautDegats"]), f(b["sautDistance"]), f(b["sautRayon"]), f(b["sautEtourdi"]))),
        ],
        survie="%s PV ; Peau de fer −%s %% %s s / %s s (rugissement) ; esquive ; saut = 5 m de fuite (recharge %s s)" % (
            f(b["vikingPV"]), int(b.get("peauDeFerReduction", 0) * 100), f(b.get("peauDeFerDuree", 0)), f(b["rugissementRecharge"]), f(b["sautRecharge"])),
        controle="provocation %s s à %s m ; étourdi %s s en zone (saut)" % (f(b["rugissementProvocation"]), f(b["rugissementRayon"]), f(b["sautEtourdi"])),
        mobilite="%s m/s ; saut %s m / %s s ; ×%s en tournante, ×0,25 pendant le coup" % (f(b["vikingVitesse"]), f(b["sautDistance"]), f(b["sautRecharge"]), f(b["tournanteVitesse"])),
        dependance="compétences gratuites (recharge seule) ; rage = Furie (ultime, non modélisée) : %s s de hache sur une cible pour la remplir" % (
            f((b["rageMax"] - b.get("rageMin", 0)) / rage_par_s_mono)),
        rage_par_s_mono=rage_par_s_mono, tourn_dps=tourn_dps, tourn_seuil=tourn_seuil,
        rage_vide_en=b["rageDelaiBaisse"] + b["rageMax"] / b["rageBaisse"],
    )

    # ---------------------------------------------------------------- Mage
    boule_dps = b["bouleDegats"] / b["bouleIntervalle"]
    brulure = b["brulureDegats"]  # entretenue par les coups : 5 PV/s tant qu'on tape
    cone_dps = b["coneDegats"]
    cone_duree = b["manaMax"] / b["coneMana"]
    zone_boule = (b["bouleDegats"] + (groupe - 1) * b["bouleDegatsZone"]) / b["bouleIntervalle"]
    boules3 = int((3.0 - b["bouleInstant"]) // b["bouleIntervalle"]) + 1  # boules qui partent en 3 s
    classes["Mage"] = dict(
        pv=b["magePV"], vitesse=b["mageVitesse"],
        dps_mono=boule_dps + brulure,
        dps_mono_base=boule_dps,
        dps_zone=zone_boule + brulure * groupe,
        pic3=max(boules3 * b["bouleDegats"], 3 * cone_dps) + 3 * brulure,
        pic3_zone=boules3 * (b["bouleDegats"] + (groupe - 1) * b["bouleDegatsZone"]) + 3 * brulure * groupe,
        temps_guerrier=guerrier_pv / (boule_dps + brulure),
        competences=[
            ("Boule de feu", "aucun (+%s mana par cible touchée)" % f(b["manaParTouche"]), "%s s entre deux" % f(b["bouleIntervalle"], 2),
             "%s dégâts + %s en zone (%s m) ; brûlure %s/s pendant %s s ; portée %s m" % (
                 f(b["bouleDegats"]), f(b["bouleDegatsZone"]), f(b["bouleRayon"]), f(b["brulureDegats"]), f(b["brulureDuree"]), f(b["boulePortee"]))),
            ("Cône de flammes", "%s mana/s (jauge %s : %s s au plus) ; régénération %s/s hors cône" % (
                f(b["coneMana"]), f(b["manaMax"]), f(cone_duree), f(b["manaRegen"])), "—",
             "%s DPS par cible, %s m, ±%s°, vitesse ×%s ; brûlure" % (f(b["coneDegats"]), f(b["conePortee"]), int(b["coneDemiAngle"]), f(b["coneVitesse"]))),
            ("LB", "—", "—", "vide"),
            ("RB", "—", "—", "vide"),
        ],
        survie="%s PV ; portée %s m ; aucune mitigation ; esquive" % (f(b["magePV"]), f(b["boulePortee"])),
        controle="aucun (brûlure = dégâts seulement)",
        mobilite="%s m/s ; ×0,6 en lançant, ×%s pendant le cône" % (f(b["mageVitesse"]), f(b["coneVitesse"])),
        dependance="aucune ; mais rien ne le protège au contact",
        cone_duree=cone_duree, cone_dps=cone_dps,
        mana_plein_en=b["manaMax"] / b["manaRegen"],
    )

    # ---------------------------------------------------------------- Rôdeur
    cycle_charge = b["arcCharge"] + b["arcIntervalle"] + 0.15  # rebander après lâcher (ClasseRodeur.Maj)
    arc_dps = b["arcDegatsMax"] / cycle_charge
    cycle_rapide = 0.05 + b["arcIntervalle"] + 0.15
    arc_rapide_dps = b["arcDegatsMin"] / cycle_rapide
    nuee_total = b["nueeSalves"] * b["nueeDegatsSalve"]
    salve_mono = 2 * b["salveDegats"]  # 1 à 2 flèches sur une cible (éventail ±20°)
    salve_zone = b["salveFleches"] * b["salveDegats"]
    classes["Rôdeur"] = dict(
        pv=b["rodeurPV"], vitesse=b["rodeurVitesse"],
        dps_mono=arc_dps + nuee_total / b["nueeRecharge"] + salve_mono / b["rouladeRecharge"],
        dps_mono_base=arc_dps,
        dps_zone=arc_dps + nuee_total * groupe / b["nueeRecharge"] + salve_zone / b["rouladeRecharge"],
        pic3=nuee_total + salve_mono + b["arcDegatsMax"],
        pic3_zone=nuee_total * groupe + salve_zone + b["arcDegatsMax"],
        temps_guerrier=guerrier_pv / arc_dps,
        competences=[
            ("Arc (charge complète)", "aucun", "%s s de charge + %s s" % (f(b["arcCharge"]), f(b["arcIntervalle"] + 0.15, 2)),
             "%s dégâts (%s DPS), tête ×%s (%s DPS), étourdi %s s ; tir rapide %s dégâts (%s DPS)" % (
                 f(b["arcDegatsMax"]), f(arc_dps), f(b["arcTete"]), f(arc_dps * b["arcTete"]), f(b.get("arcEtourdiPleineCharge", 0)), f(b["arcDegatsMin"]), f(arc_rapide_dps))),
            ("Nuée de flèches", "aucun", "%s s" % f(b["nueeRecharge"]),
             "%s salves × %s = %s dégâts par cible restée dans %s m, sur 1,2 s, ralenti −%s %% tant qu'on y reste ; portée %s m ; immobile 1 s" % (
                 int(b["nueeSalves"]), f(b["nueeDegatsSalve"]), f(nuee_total), f(b["nueeRayon"]), int(b.get("nueeRalentiForce", 0) * 100), f(b["nueePortee"]))),
            ("Roulade + salve", "%s endurance" % f(b["rouladeCout"]), "%s s" % f(b["rouladeRecharge"]),
             "%s m en arrière, invulnérable %s s ; %s flèches × %s sur ±%s° (tête ×2)" % (
                 f(b["rouladeDistance"]), f(b["esquiveInvulnerable"], 2), int(b["salveFleches"]), f(b["salveDegats"]), int(b["salveEcart"]))),
            ("Visée", "aucun", "—", "zoom ; vitesse ×%s" % f(b["viseeVitesse"])),
        ],
        survie="%s PV ; portée %s m ; roulade arrière 4 m (+ esquive) ; plus rapide que tout squelette (%s contre %s m/s)" % (
            f(b["rodeurPV"]), f(b["arcPortee"]), f(b["rodeurVitesse"]), f(b["sbire"]["vitesse"])),
        controle="étourdi %s s par flèche à pleine charge (toutes les %s s) ; ralenti −%s %% dans la nuée / %s s" % (
            f(b.get("arcEtourdiPleineCharge", 0)), f(cycle_charge, 2), int(b.get("nueeRalentiForce", 0) * 100), f(b["nueeRecharge"])),
        mobilite="%s m/s ; roulade 4 m / %s s ; ×%s en bandant" % (f(b["rodeurVitesse"]), f(b["rouladeRecharge"]), f(b["arcVitesseBander"])),
        dependance="aucune ; sa valeur dépend de la précision (tête ×2)",
        arc_dps=arc_dps, arc_rapide_dps=arc_rapide_dps,
    )

    # ---------------------------------------------------------------- Assassin
    dague_dps = b["dagueDegats"] / b["dagueIntervalle"]
    dos_dps = dague_dps * b["critiqueDos"]
    ouverture = b["dagueDegats"] * b["critiqueFurtifDos"]
    coups_a = int(3.0 // b["dagueIntervalle"])  # coups en 3 s (le premier à 0,25 s)
    classes["Assassin"] = dict(
        pv=b["assassinPV"], vitesse=b["assassinVitesse"],
        dps_mono=dague_dps + b["arbaleteDegats"] / b["arbaleteRecharge"],
        dps_mono_base=dague_dps,
        dps_zone=dague_dps + b["arbaleteDegats"] / b["arbaleteRecharge"],  # une cible à la fois
        pic3=ouverture + (coups_a - 1) * b["dagueDegats"] * b["critiqueDos"],
        pic3_zone=ouverture + (coups_a - 1) * b["dagueDegats"] * b["critiqueDos"],
        temps_guerrier=guerrier_pv / dague_dps,
        competences=[
            ("Dague", "aucun", "%s s entre deux coups" % f(b["dagueIntervalle"], 2),
             "%s dégâts, 1 cible, %s m ; furtif ×%s (1 coup), dos ×%s (%s DPS), les deux ×%s (%s)" % (
                 f(b["dagueDegats"]), f(b["daguePortee"]), f(b["critiqueFurtif"]), f(b["critiqueDos"]), f(dos_dps), f(b["critiqueFurtifDos"]), f(ouverture))),
            ("Arbalète", "aucun", "%s s" % f(b["arbaleteRecharge"]),
             "%s dégâts (tête ×%s = %s), %s m/s, portée %s m ; vitesse ×0,5 en main" % (
                 f(b["arbaleteDegats"]), f(b["arbaleteTete"]), f(b["arbaleteDegats"] * b["arbaleteTete"]), f(b["arbaleteVitesse"]), f(b["arbaletePortee"]))),
            ("Grenade fumigène", "aucun", "%s s" % f(b["grenadeRecharge"]),
             "nuage %s s, portée %s m : personne n'est vu dedans (alliés compris) ; l'assassin y redevient furtif" % (f(b["grenadeNuage"]), f(b["grenadePortee"]))),
            ("Pas de l'ombre", "aucun", "%s s (remise à zéro par une exécution)" % f(b.get("pasOmbreRecharge", 0)),
             "bond de %s m en %s s vers la visée, invulnérable, à travers les ennemis ; arrêt %s m derrière l'ennemi visé, face à son dos ; ne sort pas du furtif" % (
                 f(b.get("pasOmbreDistance", 0)), f(b.get("pasOmbreDuree", 0), 2), f(b.get("pasOmbreArret", 0)))),
            ("Exécution (passif)", "—", "—",
             "dague sur un ennemi commun sous %s %% de vie : achevé net ; élite ou boss : ×%s (le meilleur des facteurs, pas le produit)" % (
                 int(b.get("executionSeuil", 0) * 100), f(b.get("executionElite", 0)))),
            ("Furtif (passif)", "—", "%s s hors combat" % f(b["horsCombat"]),
             "marche à %s m/s ; repéré à %s m devant (±%s°) ou %s m derrière" % (
                 f(b["marcheDiscrete"]), f(b["assassinDetectionVue"]), int(b["assassinDetectionAngle"]), f(b["assassinDetectionDos"]))),
        ],
        survie="%s PV ; aucune mitigation ; furtivité (pas ciblé hors combat) ; fumée = sortie de combat ; bond invulnérable %s s / %s s ; esquive" % (
            f(b["assassinPV"]), f(b.get("pasOmbreDuree", 0), 2), f(b.get("pasOmbreRecharge", 0))),
        controle="aucun ; fumée = les squelettes perdent leur cible (et repartent vers Nyxessa) ; exécution = un blessé sous %s %% meurt net" % int(b.get("executionSeuil", 0) * 100),
        mobilite="%s m/s (%s furtif) ; bond de %s m / %s s ; ×0,4 pendant le coup, ×0,5 arbalète en main" % (
            f(b["assassinVitesse"]), f(b["marcheDiscrete"]), f(b.get("pasOmbreDistance", 0)), f(b.get("pasOmbreRecharge", 0))),
        dependance="le dos ×%s exige un ennemi occupé ailleurs (Nyxessa, tank) ; le Pas de l'ombre l'y porte ; depuis le 27/09/2026 un squelette sur Nyxessa frappé %s fois à moins de %s m se retourne (riposte %s s)" % (
            f(b["critiqueDos"]), int(b.get("riposteCoups", 0)), f(b.get("riposteDistance", 0)), f(b.get("riposteDuree", 0))),
        dos_dps=dos_dps, ouverture=ouverture,
    )

    # ---------------------------------------------------------------- Contexte : ce que demandent les nuits
    contexte = {}
    for n in (5, 8, 10, 12):
        m = b["multiplicateurPV"][n - 1]
        total = b["ennemisParNuit"][n - 1]
        pg = b["partGuerriers"][n - 1]
        elites = 2 if n >= 7 else (1 if n >= 5 else 0)
        pv_solo = total * (pg * b["guerrier"]["pv"] + (1 - pg) * b["sbire"]["pv"]) * m + elites * 2 * b["guerrier"]["pv"] * m
        pv_4 = pv_solo * (1 + b["ennemisParJoueurEnPlus"] * 3)
        contexte[n] = dict(ennemis_solo=total, pv_solo=pv_solo, pv_4=pv_4,
                           dps_par_joueur_4=pv_4 / 4 / 90.0, dps_solo=pv_solo / 90.0)
    contexte["morgrim_pv"] = b["golemPV"]
    contexte["nyxar_pv"] = b["necroPV"]
    contexte["guerrier_pv"] = guerrier_pv
    contexte["sbire_pv"] = sbire_pv
    contexte["guerrier_degats"] = b["guerrier"]["degatsJoueur"]
    contexte["sbire_degats"] = b["sbire"]["degatsJoueur"]
    return classes, contexte


def markdown(classes, contexte, b):
    out = []
    p = out.append
    p("### Tableau du modèle commun (généré par `Docs/outils/equilibrage.py`)")
    p("")
    p("Cible mono : guerrier de la nuit 5 (%s PV, %s dégâts par coup) ; groupe : 5 sbires serrés (%s PV chacun)." % (
        f(contexte["guerrier_pv"]), f(contexte["guerrier_degats"]), f(contexte["sbire_pv"])))
    p("")
    p("| Classe | PV | DPS mono soutenu (base seule) | DPS groupe (5 sbires) | Pic 3 s mono | Pic 3 s groupe | Guerrier N5 tué en | Contrôle | Survie |")
    p("|---|---|---|---|---|---|---|---|---|")
    for nom, c in classes.items():
        p("| %s | %s | **%s** (%s) | %s | %s | %s | %s s | %s | %s |" % (
            nom, f(c["pv"]), f(c["dps_mono"]), f(c["dps_mono_base"]), f(c["dps_zone"]), f(c["pic3"]), f(c["pic3_zone"]),
            f(c["temps_guerrier"]), c["controle"], c["survie"]))
    p("")
    p("Lignes à part (dépendent du joueur, pas des chiffres) : Rôdeur tête ×2 → %s DPS soutenu ; Assassin dans le dos ×%s → %s DPS soutenu, ouverture furtif + dos %s ; Viking attaque tournante %s DPS par cible (plus que la hache en mono : %s) tant qu'il a de la rage." % (
        f(classes["Rôdeur"]["arc_dps"] * b["arcTete"]), f(b["critiqueDos"]), f(classes["Assassin"]["dos_dps"]), f(classes["Assassin"]["ouverture"]),
        f(classes["Viking"]["tourn_dps"]), f(classes["Viking"]["dps_mono_base"])))
    p("")
    p("### Compétences : coût, recharge, effet")
    p("")
    for nom, c in classes.items():
        p("**%s** — mobilité : %s ; dépendance : %s." % (nom, c["mobilite"], c["dependance"]))
        p("")
        p("| Compétence | Coût | Recharge | Effet |")
        p("|---|---|---|---|")
        for k in c["competences"]:
            p("| %s | %s | %s | %s |" % k)
        p("")
    p("### Ce que demandent les nuits (PV ennemis à abattre, 90 s de combat utile par nuit)")
    p("")
    p("| Nuit | Ennemis (solo) | PV ennemis solo | DPS requis solo | PV ennemis à 4 | DPS requis par joueur à 4 |")
    p("|---|---|---|---|---|---|")
    for n in (5, 8, 10, 12):
        c = contexte[n]
        p("| %d | %d | %s | %s | %s | %s |" % (n, c["ennemis_solo"], f(c["pv_solo"], 0), f(c["dps_solo"]), f(c["pv_4"], 0), f(c["dps_par_joueur_4"])))
    p("")
    p("Morgrim : %s PV (étourdissements ×0,5, non repoussable) ; Nyxar : %s PV, reste à 12–18 m. Nyxessa au palier 5 ajoute environ %s DPS sur une nuit (4 160 dégâts / 120 s)." % (
        f(contexte["morgrim_pv"], 0), f(contexte["nyxar_pv"], 0), f(4160 / 120.0)))
    p("")
    p("Temps pour abattre Morgrim seul, DPS mono soutenu : " + " ; ".join(
        "%s %s s" % (nom, f(contexte["morgrim_pv"] / c["dps_mono"], 0)) for nom, c in classes.items())
      + " ; Assassin dans le dos %s s ; Rôdeur à la tête %s s." % (
          f(contexte["morgrim_pv"] / classes["Assassin"]["dos_dps"], 0), f(contexte["morgrim_pv"] / (classes["Rôdeur"]["arc_dps"] * b["arcTete"]), 0)))
    return "\n".join(out)


def main():
    b = lire_balance()
    classes, contexte = modele(b)
    if "--json" in sys.argv:
        propre = {n: {k: v for k, v in c.items() if k != "competences"} for n, c in classes.items()}
        print(json.dumps({"classes": propre, "contexte": contexte}, ensure_ascii=False, indent=1))
        return
    sys.stdout.reconfigure(encoding="utf-8")
    print(markdown(classes, contexte, b))


if __name__ == "__main__":
    main()
