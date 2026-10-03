"""Jauge Kenney : inventaire et usage de tout ce qui vient des packs Kenney (CC0) dans Deathless, pour les dégager
comme KayKit (décision de Quentin, 03/10/2026). Même style que jauge_kaykit.py : rejouable, bibliothèque standard
seulement. Écrit Wiki/data/kenney.json, lu par build.py pour la balise {jauge-kenney}.

Ce que le script mesure
- Inventaire : fichiers sous Assets/Audio/Kenney/ (RPG Audio, Interface Sounds) et Assets/Art/UI/KenneyInputPrompts/
  (icônes de boutons), taille, pack. Un fichier est « Kenney » s'il est dans un dossier dont le nom contient « kenney ».
- Usage d'un son (quatre états, du plus fort au plus faible) :
    joué      une entrée du catalogue qui le contient est le premier id présent d'un tableau de SonsDuJeu / SonsPortes
              (« le premier id présent dans le catalogue joue »), ou le fichier est référencé par GUID dans un prefab,
              une scène ou un asset ;
    repli     une entrée qui le contient figure dans un tableau, mais derrière un id présent : il ne joue qu'en secours ;
    catalogue présent dans SonsCatalogue.asset, jamais appelé par le code ;
    disque    seulement sur le disque.
  Le statut « utilise » de sons.json n'est pas fiable pour cela (le catalogue embarque aussi les « a_ecouter » et les
  « disponible » référencés par le code) : le script lit le code (Assets/Scripts/**/*.cs) et le catalogue importé.
- Icônes : utilisée si l'asset InputGlyphs (ou un prefab, une scène) la référence par GUID, ou le lanceur (csproj).
- Dérivés : sons du jeu qui reprennent des échantillons Kenney retravaillés (source de sons.json qui cite Kenney).
Usage : python Wiki/jauge_kenney.py"""
import json, os, re, datetime, io

ICI = os.path.dirname(os.path.abspath(__file__))
RACINE = os.path.dirname(ICI)
ASSETS = os.path.join(RACINE, "Assets")
EXT_SONS = (".wav", ".ogg", ".mp3")
EXT_YAML = (".unity", ".prefab", ".asset", ".mat", ".controller", ".uxml", ".uss", ".tss", ".anim", ".inputactions")

# Tableaux de SonsDuJeu / SonsPortes -> (catégorie d'usage, évènement, fréquence 1..5).
# Fréquence : 5 en continu (pas), 4 à chaque combat, 3 régulier, 2 occasionnel (boutique, repas, portes), 1 rare.
EVENEMENTS = {
    "Pas": ("Pas", "Pas du joueur, repli sans matière", 5),
    "PasHerbe": ("Pas", "Pas sur l'herbe", 5), "PasTerre": ("Pas", "Pas sur la terre", 5),
    "PasPierre": ("Pas", "Pas sur la pierre", 5), "PasBois": ("Pas", "Pas sur le bois", 5),
    "PasMetal": ("Pas", "Pas sur le métal", 5), "PasSable": ("Pas", "Pas sur le sable", 5),
    "PasEau": ("Pas", "Pas dans l'eau", 5),
    "EpeeElan": ("Combat", "Coup d'épée (élan)", 4), "Dague": ("Combat", "Coup de dague", 4), "Hache": ("Combat", "Coup de hache", 4),
    "NyxarFaux": ("Combat", "Coup de faux de Nyxar", 2), "SquelettePreparation": ("Combat", "Squelette : préparation d'un coup", 4),
    "Saut": ("Joueur", "Saut", 3),
    "Or": ("Objets, or et taverne", "Or qui entre ou sort de la caisse", 3), "AchatBoutique": ("Objets, or et taverne", "Achat à la boutique", 2),
    "AchatCle": ("Objets, or et taverne", "Achat d'une clé", 2), "Repas": ("Objets, or et taverne", "Repas à la taverne", 2),
    "Biere": ("Objets, or et taverne", "Bière à la taverne", 2), "PotionBue": ("Objets, or et taverne", "Potion bue", 2),
    "CoffreCadenas": ("Coffres et cadenas", "Cadenas d'un coffre", 2), "CoffreOuvert": ("Coffres et cadenas", "Coffre qui s'ouvre", 2),
    "PorteOuvre": ("Portes", "Porte d'un bâtiment qui s'ouvre", 2), "PorteFerme": ("Portes", "Porte d'un bâtiment qui se ferme", 2),
    "Vague": ("Jour et nuit", "Nouvelle vague lancée", 2), "NyxessaAlerte": ("Interface", "Alerte de Nyxessa", 3),
    "PalierAchete": ("Interface", "Palier acheté", 2), "AchatRefuse": ("Interface", "Achat refusé", 3),
    "PointDepense": ("Interface", "Point dépensé", 3), "PointGagne": ("Interface", "Point gagné", 2),
    "AlerteNuit": ("Interface", "Alerte de la nuit", 2), "Pret": ("Interface", "Joueur prêt", 2),
    "PretAnnule": ("Interface", "Prêt annulé", 2), "TousPrets": ("Interface", "Tous prêts", 2),
    "ViseeAnnule": ("Interface", "Visée annulée", 3),
}
# Proposition de remplaçant par entrée du catalogue Kenney utilisée par le jeu : (remplaçant proposé, ids du catalogue candidats).
REMPLACANTS = {
    "kenney_rpg_footstep": ("Pas propres par matière (herbe, terre, sable, pierre, bois, métal) : en cours de refonte ; retirer le repli et refaire terre_6 et pierre_4 à 6 (échantillons Kenney) par synthèse", ["pas_herbe", "pas_terre", "pas_pierre", "pas_sable_v2", "pas_bois_v2", "pas_metal_v2"]),
    "kenney_rpg_dooropen": ("Porte de bois propre : gonds, loquet, bois qui cède (à créer, en 2 variantes, tonalité sombre)", []),
    "kenney_rpg_doorclose": ("Porte de bois propre : battant qui se referme, loquet, écho de la pièce (à créer, 4 variantes)", []),
    "kenney_rpg_metallatch": ("Cadenas et loquet propres : clé qui tourne, pêne de fer (à créer, coffre et boutique)", []),
    "kenney_rpg_metalclick": ("Cadenas qui claque propre ; bière : voir dl_taverne_biere (à écouter)", ["dl_taverne_biere"]),
    "kenney_rpg_metalpot": ("Repas : couverts et chope en étain, propres (à créer)", []),
    "kenney_rpg_handlecoins": ("Pièces dans une bourse de cuir : dl_or_caisse (à écouter) ; achat de boutique : à créer", ["dl_or_caisse"]),
    "kenney_rpg_knifeslice": ("Souffle de lame : dl_epee_elan et dl_dague_elan (à écouter, pas encore câblés dans SonsDuJeu)", ["dl_epee_elan", "dl_dague_elan"]),
    "kenney_rpg_chop": ("Souffle de hache : dl_hache_elan (à écouter, pas encore câblé) ; faux de Nyxar : à créer", ["dl_hache_elan"]),
    "kenney_rpg_cloth": ("Saut : souffle et tissu synthétisés (id jump, à créer)", []),
    "kenney_rpg_drawknife": ("Crécelle d'os du squelette : dl_squelette_preparation (à écouter)", ["dl_squelette_preparation"]),
    "nuit_vague": ("Trois coups de peau de guerre : dl_vague (à écouter)", ["dl_vague"]),
    "ui_retour": ("Visée annulée : ajouter dl_interface_retour au tableau ViseeAnnule, ou créer dl_visee_annule", ["dl_interface_retour"]),
    "ui_confirmation": ("Repli seulement : dl_interface_confirmation (à écouter) joue déjà ; retirer le repli à la validation", ["dl_interface_confirmation"]),
    "ui_refus": ("Repli seulement : dl_interface_refus (à écouter) joue déjà ; retirer le repli à la validation", ["dl_interface_refus"]),
    "ui_decompte": ("Repli seulement : dl_interface_decompte (à écouter, pas branché) ; retirer le repli", ["dl_interface_decompte"]),
    "nyxessa_alerte": ("Repli seulement : dl_nyxessa_alerte (à écouter) joue déjà ; retirer le repli", ["dl_nyxessa_alerte"]),
}
# Tableaux joués seulement en secours (aucun id présent devant, mais le code ne les appelle qu'après le tableau par matière).
SECOURS = {"Pas"}
PACKS = {"RPGAudio": "RPG Audio", "InterfaceSounds": "Interface Sounds"}


def lire(chemin):
    try:
        with io.open(chemin, encoding="utf-8", errors="ignore") as f:
            return f.read()
    except OSError:
        return ""


def guid_de(chemin):
    m = re.search(r"guid: (\w{32})", lire(chemin + ".meta"))
    return m.group(1) if m else None


def inventaire():
    sons, icones = {}, {}
    for racine, _, fichiers in os.walk(ASSETS):
        chemin = racine.replace("\\", "/")
        if "kenney" not in chemin.lower():
            continue
        for f in fichiers:
            if f.endswith(".meta"):
                continue
            p = os.path.join(racine, f)
            rel = os.path.relpath(p, ASSETS).replace("\\", "/")
            g = guid_de(p)
            fiche = {"fichier": rel, "octets": os.path.getsize(p), "guid": g}
            if f.lower().endswith(EXT_SONS):
                fiche["pack"] = next((n for d, n in PACKS.items() if "/" + d + "/" in "/" + rel), "Kenney")
                sons[g] = fiche
            elif f.lower().endswith((".png", ".jpg", ".svg")):
                fiche["pack"] = "Input Prompts"
                fiche["famille"] = rel.split("/")[3] if len(rel.split("/")) > 4 else ""
                icones[g] = fiche
    return sons, icones


def lire_catalogue():
    t = lire(os.path.join(ASSETS, "Jeu", "Audio", "SonsCatalogue.asset"))
    entrees = {}
    for m in re.finditer(r"  - id: (\S+)\n(.*?)(?=\n  - id: |\Z)", t, re.S):
        st = re.search(r"statut: (\w+)", m.group(2))
        entrees[m.group(1)] = {"statut": st.group(1) if st else "", "guids": re.findall(r"guid: (\w{32})", m.group(2))}
    return entrees


def lire_code():
    """Tableaux d'ids (SonsDuJeu, SonsPortes…) et nombre d'appels de chacun dans le code."""
    tableaux, sources = [], ""
    for dossier in ("Scripts", "Editor"):
        for racine, _, fichiers in os.walk(os.path.join(ASSETS, dossier)):
            for f in fichiers:
                if f.endswith(".cs"):
                    t = lire(os.path.join(racine, f))
                    sources += t + "\n"
                    for m in re.finditer(r"string\[\]\s+(\w+)\s*=\s*\{([^}]*)\}", t):
                        tableaux.append((f, m.group(1), re.findall(r'"([^"]+)"', m.group(2))))
    return tableaux, sources


def references_yaml(guids):
    refs = {}
    for racine, _, fichiers in os.walk(ASSETS):
        for f in fichiers:
            if f.endswith(EXT_YAML) and f != "SonsCatalogue.asset":
                p = os.path.join(racine, f)
                for g in set(re.findall(r"guid: (\w{32})", lire(p))):
                    if g in guids:
                        refs.setdefault(g, set()).add(os.path.relpath(p, ASSETS).replace("\\", "/"))
    return refs


def libelles(evs, ident):
    """Libellés lisibles : les usages joués en entier, les usages de secours regroupés en une mention."""
    joues = [e["libelle"] for e in evs if e["effectif"] == ident]
    secours = [e for e in evs if e["effectif"] != ident]
    if secours:
        pas = [e for e in secours if e["categorie"] == "Pas"]
        autres = [e["libelle"] for e in secours if e["categorie"] != "Pas"]
        if pas:
            autres.insert(0, "pas du joueur (%d tableaux)" % len(pas))
        joues.append("secours de : " + ", ".join(autres[:3]) + (" et %d autres" % (len(autres) - 3) if len(autres) > 3 else ""))
    return joues


def mesurer():
    sons, icones = inventaire()
    catalogue = lire_catalogue()
    tableaux, sources = lire_code()
    refs = references_yaml(set(sons) | set(icones))
    csproj = lire(os.path.join(RACINE, "Launcher", "DeathlessLauncher.csproj"))

    id_kenney = {i for i, e in catalogue.items() if any(g in sons for g in e["guids"])}
    # Évènements du jeu : un tableau avec au moins un id du catalogue ; id effectif = premier présent.
    evenements = []
    for fichier, nom, ids in tableaux:
        presents = [i for i in ids if i in catalogue]
        if not presents:
            continue
        appels = len(re.findall(r"\b(?:SonsDuJeu|SonsPortes)\.%s\b" % nom, sources))
        if nom.startswith("Pas") and nom != "Pas":
            appels += 1 if "PasParMatiere[" in sources else 0
        cat, libelle, freq = EVENEMENTS.get(nom, ("Autre", nom, 1))
        evenements.append({"tableau": nom, "ids": ids, "effectif": None if nom in SECOURS else presents[0], "secours": nom in SECOURS, "appels": appels,
                           "categorie": cat, "libelle": libelle, "frequence": freq})
    effectifs = {e["effectif"] for e in evenements if e["effectif"]}
    nommes = {i for e in evenements for i in e["ids"]}

    # Etat de chaque son Kenney.
    etat_fichier = {}
    for g, fiche in sons.items():
        entrees = [i for i, e in catalogue.items() if g in e["guids"]]
        etat = "disque"
        if entrees:
            etat = "catalogue"
        if any(i in nommes for i in entrees):
            etat = "repli"
        if any(i in effectifs for i in entrees) or g in refs:
            etat = "joue"
        fiche.update({"etat": etat, "entrees": entrees, "refs": sorted(refs.get(g, []))})
        etat_fichier[g] = etat
    nb = {k: sum(1 for v in etat_fichier.values() if v == k) for k in ("joue", "repli", "catalogue", "disque")}
    octets = {k: sum(f["octets"] for f in sons.values() if f["etat"] == k) for k in nb}

    # Entrées du catalogue Kenney par état (une entrée = un « son du jeu » regroupant ses variantes).
    entrees_k = []
    for i in sorted(id_kenney):
        e = catalogue[i]
        fich = [sons[g] for g in e["guids"] if g in sons]
        etats = [f["etat"] for f in fich]
        etat = "joue" if "joue" in etats else "repli" if "repli" in etats else "catalogue"
        entrees_k.append({"id": i, "etat": etat, "variantes": len(fich), "octets": sum(f["octets"] for f in fich),
                          "pack": fich[0]["pack"], "fichiers": [f["fichier"] for f in fich]})

    # Part des sons du jeu (évènements câblés : id effectif de chaque tableau, ids distincts).
    jeu_k = sorted({e["effectif"] for e in evenements if e["effectif"] in id_kenney})
    jeu_tous = sorted(effectifs)
    tous_sons = [os.path.join(r, f) for r, _, fs in os.walk(os.path.join(ASSETS, "Audio")) for f in fs if f.lower().endswith(EXT_SONS)]
    octets_audio = sum(os.path.getsize(p) for p in tous_sons)

    # Catégories d'usage (évènements dont l'id effectif est Kenney ; repli ailleurs).
    categories = {}
    for e in evenements:
        c = categories.setdefault(e["categorie"], {"nom": e["categorie"], "evenements": 0, "kenney_joue": 0, "kenney_repli": 0})
        c["evenements"] += 1
        if e["effectif"] in id_kenney:
            c["kenney_joue"] += 1
        elif any(i in id_kenney for i in e["ids"]):
            c["kenney_repli"] += 1
    categories = sorted(categories.values(), key=lambda c: (-c["kenney_joue"], -c["kenney_repli"], c["nom"]))

    # Tableau « à remplacer » : entrées Kenney jouées ou en repli, triées par fréquence d'usage puis nombre d'appels.
    a_remplacer = []
    for k in entrees_k:
        if k["etat"] == "catalogue":
            continue
        evs = [e for e in evenements if k["id"] in e["ids"]]
        joues = [e for e in evs if e["effectif"] == k["id"]]
        frequence = max([e["frequence"] for e in (joues or evs)] or [1])
        appels = sum(e["appels"] for e in joues) if joues else 0
        prop, candidats = REMPLACANTS.get(k["id"], ("À définir : son propre (généré) pour cet usage", []))
        a_remplacer.append({
            "id": k["id"], "etat": k["etat"], "variantes": k["variantes"], "octets": k["octets"], "pack": k["pack"],
            "categorie": (joues or evs)[0]["categorie"] if evs else "Autre",
            "evenements": libelles(evs, k["id"]),
            "frequence": frequence, "appels": appels, "remplacant": prop, "candidats": candidats, "avancement": "à faire",
        })
    a_remplacer.sort(key=lambda r: (-r["frequence"], r["etat"] != "joue", -r["appels"], r["id"]))

    # Dérivés : sons propres bâtis sur des échantillons Kenney (source citée dans sons.json).
    derives = []
    try:
        cat_sons = json.load(io.open(os.path.join(ICI, "data", "sons.json"), encoding="utf-8"))
    except (OSError, ValueError):
        cat_sons = []
    for s in cat_sons:
        if "kenney" in (s.get("source") or "").lower() and "kenney" not in s["id"] and s["id"] not in ("nyxessa_alerte", "nuit_vague") and not s["id"].startswith("ui_"):
            derives.append({"id": s["id"], "nom": s["nom"], "statut": s["statut"]})

    # Icônes.
    icones_utilisees = 0
    octets_icones_utilises = 0
    for g, f in icones.items():
        nom = os.path.basename(f["fichier"])
        utilisee = g in refs or ("\\" + nom) in csproj.replace("/", "\\")
        f["utilisee"] = utilisee
        if utilisee:
            icones_utilisees += 1
            octets_icones_utilises += f["octets"]
    familles = {}
    for f in icones.values():
        d = familles.setdefault(f["famille"], {"nom": f["famille"], "fichiers": 0, "utilisees": 0, "octets": 0})
        d["fichiers"] += 1
        d["utilisees"] += 1 if f["utilisee"] else 0
        d["octets"] += f["octets"]

    return {
        "date": datetime.date.today().isoformat(),
        "sons": {
            "fichiers": len(sons), "octets": sum(f["octets"] for f in sons.values()),
            "par_pack": [{"pack": p, "fichiers": sum(1 for f in sons.values() if f["pack"] == p),
                          "octets": sum(f["octets"] for f in sons.values() if f["pack"] == p)} for p in sorted(set(f["pack"] for f in sons.values()))],
            "etats": nb, "octets_par_etat": octets,
            "utilises": nb["joue"], "pourcent_utilises": round(100.0 * nb["joue"] / len(sons), 1) if sons else 0.0,
            "total_sons_projet": len(tous_sons), "octets_sons_projet": octets_audio,
            "pourcent_fichiers_projet": round(100.0 * len(sons) / len(tous_sons), 1) if tous_sons else 0.0,
            "catalogue_entrees": len(catalogue), "catalogue_entrees_kenney": len(id_kenney),
            "pourcent_catalogue": round(100.0 * len(id_kenney) / len(catalogue), 1) if catalogue else 0.0,
            "jeu_evenements": len(evenements), "jeu_ids_effectifs": len(jeu_tous), "jeu_ids_kenney": len(jeu_k),
            "pourcent_jeu": round(100.0 * len(jeu_k) / len(jeu_tous), 1) if jeu_tous else 0.0,
            "ids_kenney_joues": jeu_k,
        },
        "icones": {"fichiers": len(icones), "octets": sum(f["octets"] for f in icones.values()), "utilisees": icones_utilisees,
                   "octets_utilises": octets_icones_utilises, "familles": sorted(familles.values(), key=lambda d: d["nom"])},
        "categories": categories,
        "a_remplacer": a_remplacer,
        "non_branches": {"entrees": sum(1 for k in entrees_k if k["etat"] == "catalogue"),
                         "fichiers": nb["catalogue"], "octets": octets["catalogue"]},
        "derives": derives,
        "total_fichiers": len(sons) + len(icones), "total_octets": sum(f["octets"] for f in sons.values()) + sum(f["octets"] for f in icones.values()),
        "autres_sources": autres_sources(catalogue),
    }


def autres_sources(catalogue):
    """Une ligne par autre source tierce repérée (poids et usage global, sans détail)."""
    def poids(chemin, exts=None):
        n = o = 0
        for r, _, fs in os.walk(chemin):
            for f in fs:
                if not f.endswith(".meta") and (exts is None or f.lower().endswith(exts)):
                    n += 1
                    o += os.path.getsize(os.path.join(r, f))
        return n, o
    sources = []
    for nom, rel, licence, note in (
        ("KayKit (modèles, animations, textures)", "Art/KayKit", "CC0", "voir la jauge KayKit ci-dessus"),
        ("Sonniss GDC (hache dans le vent)", "Audio/Sonniss", "libre de droits pour les jeux", "3 fichiers du catalogue"),
        ("Relic (sons générés en Python, propres au projet)", "Audio/Relic", "propre", "pas une source tierce : sons de Relic recopiés"),
        ("Forge (enclume générée en Python)", "Audio/Forge", "propre", "pas une source tierce"),
        ("Fredoka (police)", "Art/Fonts/Fredoka", "SIL OFL 1.1", "polices de l'interface"),
    ):
        n, o = poids(os.path.join(ASSETS, rel.replace("/", os.sep)))
        sources.append({"nom": nom, "chemin": "Assets/" + rel, "fichiers": n, "octets": o, "licence": licence, "note": note})
    return sources


def ecrire():
    donnees = mesurer()
    os.makedirs(os.path.join(ICI, "data"), exist_ok=True)
    with io.open(os.path.join(ICI, "data", "kenney.json"), "w", encoding="utf-8") as f:
        json.dump(donnees, f, ensure_ascii=False, indent=2)
    return donnees


if __name__ == "__main__":
    d = ecrire()
    s, i = d["sons"], d["icones"]
    print("Kenney : %d fichiers, %.1f Mo (sons %d, icônes %d)" % (d["total_fichiers"], d["total_octets"] / 1048576.0, s["fichiers"], i["fichiers"]))
    print("  sons : joués %(joue)d, repli %(repli)d, catalogue sans appel %(catalogue)d, disque seul %(disque)d" % s["etats"])
    print("  part des sons du jeu : %d / %d événements effectifs (%.1f %%) ; catalogue %d / %d (%.1f %%) ; fichiers audio %d / %d (%.1f %%)" % (
        s["jeu_ids_kenney"], s["jeu_ids_effectifs"], s["pourcent_jeu"], s["catalogue_entrees_kenney"], s["catalogue_entrees"], s["pourcent_catalogue"],
        s["fichiers"], s["total_sons_projet"], s["pourcent_fichiers_projet"]))
    print("  icônes : %d utilisées sur %d" % (i["utilisees"], i["fichiers"]))
    for r in d["a_remplacer"]:
        print("  %-8s F%d %-24s %s" % (r["etat"], r["frequence"], r["id"], "; ".join(r["evenements"])))
