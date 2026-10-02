# -*- coding: utf-8 -*-
"""
Maisons du village : chaîne reproductible « maison Tripo (une seule image) -> Unity ».
Script Blender sans interface, sur le modèle de ArtSources/Decor/Montagne/montagne_pipeline.py (mêmes fonctions de base :
sélection, décimation, lissage des normales). Pièces du tableau PIECES : base (maison de base), forge, sorcier, druide, mecano, taverne ;
une pièce de plus = une entrée de plus (source, largeur, budget, graines de la palette). Les mesures propres à une pièce
(embrasure, lanterne, tour, foyer de la forge) sont faites sur le maillage (cf. ancres_maison, ancres_forge, collision_forge).

Lancement (Blender 5.2) :
    "C:/Users/Furilax/Tools/blender-5.2.2-windows-x64/blender.exe" -b --python ArtSources/Decor/Maisons/maison_pipeline.py -- --piece base [options]
Options :
    --piece <nom>       base | forge | sorcier | druide | mecano | taverne (obligatoire ; « base » = la maison de base)
    --largeur <m>       largeur hors tout (X, toit compris) ; défaut : celle du tableau (8,6 m pour la maison de base)
    --budget <n>        triangles du maillage rendu avant retrait des faces du dessous (défaut du tableau)
    --ey <f>, --ez <f>  échelles de la profondeur et de la hauteur relatives à la largeur (défaut : tableau ; 1 = uniforme)
    --collision <n>     triangles au plus du maillage de collision (défaut 200)
    --texture <n>       côté de l'atlas (défaut 2048)
    --angle <deg>       angle de lissage des normales (défaut 45 : arêtes plus vives marquées dures)
    --palette <0|1>     variante retenue pour <Nom>_Texture.png : 0 = atlas cuit tel quel, 1 = quantifié en 8 couleurs
                        (défaut 1, retenue le 02/10/2026). L'autre variante est écrite à côté : <Nom>_Texture_Variante.png
    --rendus <dossier>  rendus de vérification Workbench (face, dos, profil, trois-quarts, détails)
    --sans-export       ne pas écrire le FBX, les textures ni les ancres (essais)
    --sortie <dossier>  écrire ailleurs que dans Assets/Art/Decor/Maisons/ (essais hors du projet Unity)
    --diag              forge : journal de mesures seulement, rien d'autre

Entrées (Tripo, 02/10/2026) : un seul maillage de 260 000 à 285 000 triangles et une texture basecolor 4096² par pièce (maison de
base : 273 713 triangles, boîte 0,98 x 0,90 x 0,93 unité), Z haut, façade vers -Y, porte à gauche (x négatif vu de la façade).

Traitement :
 1. Échelle UNIFORME d'après la boîte englobante (Tripo n'a pas d'échelle métrique) : le toit (le point le plus large)
    fait PIECES[...]["largeur"] m. Origine au centre de l'emprise du soubassement, au sol ; façade vers -Y.
 2. Réparation du toit : le « trou » signalé près de la cheminée n'est pas un trou du maillage (le maillage est fermé, deux
    arêtes ouvertes seulement, au pied) mais une encoche dans la faîtière côté dos, par où la pierre de la cheminée se
    voit ; on la recouvre d'une faîtière copiée sur sa voisine (le maillage HD est réparé avant décimation, la cuisson
    reprend donc le bon rouge).
 3. Décimation (collapse) au budget avec un groupe de sommets qui protège ce qui se voit de près (soubassement, marches,
    arche et embrasure, lanterne, pan avant du toit, pignon droit) et laisse fondre le dos ; UV recréés (la décimation
    déchire les UV Tripo : leçon de la Bavaroise v4). Faces du dessous supprimées. Faces lisses, arêtes dures au-delà de
    --angle, normales pondérées par l'aire (Weighted Normal).
 4. Atlas unique --texture² : dépliage Smart UV Project, puis couleur cuite (Cycles, Selected to Active) depuis la
    haute définition et sa texture 4K.
 5. Recoloration en palette (variante 1) : chaque pixel de l'atlas est rangé dans l'une des huit classes (tuiles brique,
    plâtre crème, bois brun, pierre gris clair, vitre sombre, lanterne chaude, intérieur d'embrasure sombre, métal noir),
    puis peint de la couleur franche de la classe, en gardant 55 % du modelé d'origine (léger dégradé : jointures
    des tuiles, arêtes). Carte d'émission : vitres et verre de la lanterne (allumés la nuit par CycleJourNuit).
 6. Collision : enveloppes convexes (corps, toit, marches ; tour, foyer, poteaux selon la pièce) décimées, au plus --collision
    triangles ; jamais le maillage rendu ; pour la forge, zones libres de circulation vérifiées point par point. Export FBX (deux objets : <Nom> et <Nom>_Collision), textures PNG et ancres JSON dans
    Assets/Art/Decor/Maisons/. Le journal indique triangles, atlas et nombre de matériaux.
Côté Unity : Assets/Editor/Decor/MaisonTripo.cs (menus Deathless > Village > Maison Tripo (base), Forge Tripo, ...) fabrique les prefabs.
Origine : voir Docs/da/brief-maisons.md § Réception réalisée.
"""
import bpy, bmesh, math, os, sys, json
import numpy as np
from mathutils import Vector, Matrix

ICI = os.path.dirname(os.path.abspath(__file__))
RACINE = os.path.abspath(os.path.join(ICI, "..", "..", ".."))
sys.path.insert(0, os.path.join(RACINE, "ArtSources", "Decor", "Montagne"))
import montagne_pipeline as mp   # lit sys.argv ; ne lance rien (garde __main__)

REF = os.path.join(RACINE, "ArtSources", "References", "Decor")
SORTIE = os.path.join(RACINE, "Assets", "Art", "Decor", "Maisons")
if "--sortie" in mp.ARGS:   # dossier de sortie de remplacement (essais hors du projet Unity)
    SORTIE = os.path.abspath(mp.ARGS[mp.ARGS.index("--sortie") + 1])

# ------------------------------------------------------------------------------------------------ tableau des pièces
# nom : nom des objets, du FBX, des textures et du matériau ; source : FBX Tripo ; largeur : largeur hors tout (m, X) ;
# budget : triangles rendus ; atlas : côté de la texture cuite ; collision : triangles au plus ;
# protege : boîtes (xmin, xmax, ymin, ymax, zmin, zmax, poids) en mètres, repère final (origine au centre du soubassement,
# façade vers -Y), dont les sommets sont protégés de la décimation (poids de 0 à 1 ; le reste de la pièce est à 0,25-0,55).
PIECES = {
    "base": dict(
        nom="Maison_Base",
        source=os.path.join(REF, "maison_base_tripo", "medieval+cottage+3d+model.fbx"),
        largeur=8.6, budget=12500, atlas=2048, collision=200,
        # soubassement et marches, arche et embrasure, lanterne et sa potence, fenêtre avant, pignon droit (hublot)
        protege=[(-4.5, 4.5, -5.0, 5.0, -0.1, 1.15, 0.85),
                 (-3.3, 0.7, -5.0, -2.0, 0.0, 3.6, 1.0),
                 (0.5, 1.5, -4.0, -2.4, 1.9, 3.1, 1.0),
                 (1.1, 3.0, -4.0, -2.4, 1.1, 3.2, 0.9),
                 (3.0, 4.6, -3.5, 3.5, 2.9, 7.2, 0.7)],
        # couleurs de la palette (sRGB) : tuiles brique, plâtre crème, bois brun, pierre gris clair, vitre sombre,
        # lanterne chaude, intérieur d'embrasure, métal noir (proches de la taverne validée)
        palette=dict(brique="#C4512F", creme="#EBD3AB", bois="#6A4428", pierre="#A39D95", vitre="#2F3B4A",
                     lanterne="#F4A93C", embrasure="#2C1B12", metal="#232327"),
        # ancres Unity (m, repère final) : embrasure (centre au sol), seuil, fenêtre avant (charnières), lanterne
        porte=dict(x=-0.96, largeur=1.42, seuil=0.55, haut=2.94),
        fenetre=dict(x0=1.36, x1=2.81, z0=1.32, z1=2.91),
    ),
    # Forge (02/10/2026) : maison à gauche (pignon avant, arche vide à gauche, fenêtre à barreaux) + atelier ouvert à droite
    # (toit à une pente sur deux poteaux, foyer de pierre massif contre le mur, cheminée très haute, dalles à même le sol).
    # Échelle (compromis du 02/10/2026) : largeur hors tout 17,4 m (plan : maison 11,6 + atelier 5,8), profondeur x 0,87 par
    # rapport à l'échelle uniforme (maison 9,9 m de soubassement au lieu de 11,5 m ; plan : 9,6 m), hauteur gardée (même échelle
    # que la largeur). Repère de travail des boîtes « protege » : celui du modèle mis à l'échelle, origine au coin sud-ouest de la
    # boîte englobante (x est, y nord, z haut).
    "forge": dict(
        nom="Forge", type="forge",
        source=os.path.join(REF, "forge_tripo", "medieval+cottage+3d+model.fbx"),
        largeur=17.4, echelle_y=0.87, echelle_z=1.0, enfoncement=0.34, budget=16000, atlas=2048, collision=400,
        protege_pierre=0.9, repere_protege="travail",
        protege=[(0.0, 11.4, 0.0, 14.0, 6.2, 14.0, 0.8),     # toit de la maison (tuiles)
                 (0.0, 11.4, 0.0, 14.0, 0.0, 6.2, 0.55)],    # façades de la maison, soubassement, marches
        palette=dict(brique="#7A4A30", creme="#D9BF99", bois="#4B3022", pierre="#86817B", vitre="#2F3B4A",
                     lanterne="#F4A93C", embrasure="#2A1C14", metal="#232327"),
        porte=dict(x=0.0, largeur=1.8, seuil=0.0, haut=2.6), sans_emission=True,   # le feu et la lueur sont des effets moteur
        # tuiles et bois sont du même brun dans la texture Tripo de la forge : une seule classe (« bois »), le modelé fait
        # ressortir les tuiles ; pas de lanterne
        graines=dict(
            bois=[(79, 61, 46), (85, 69, 56), (69, 53, 40), (58, 45, 33), (54, 43, 34), (95, 74, 56), (72, 58, 46)],
            creme=[(205, 178, 147), (179, 155, 128), (220, 196, 165)],
            pierre=[(105, 95, 86), (134, 125, 113), (118, 107, 97), (127, 116, 103), (138, 128, 116), (88, 79, 70)],
            embrasure=[(47, 36, 26), (28, 20, 14)],
            vitre=[(24, 27, 32)], metal=[(14, 12, 11)]),
    ),
    # Maison du sorcier : maison basse à pignon avant, toit d'ardoise bleue, tour ronde à toit conique au coin arrière droit.
    # Largeur hors tout 12,4 m (maison 11,3 m de murs pour 11,6 m au plan), profondeur x 0,93 (murs 9,6 m comme au plan),
    # hauteur gardée (murs 4,6 m, faîtage 9,5 m, tour et croissant 16 m).
    "sorcier": dict(
        nom="Sorcier", type="maison",
        source=os.path.join(REF, "maison_sorcier_tripo", "fantasy+cottage+3d+model.fbx"),
        largeur=12.4, echelle_y=0.93, budget=15500, atlas=2048, collision=320, emprise_z=(0.9, 1.3),
        protege_pierre=0.8, tour=True, lanterne=True,
        protege=[(-9, 9, -9, 9, -0.1, 1.4, 0.7)],
        palette=dict(brique="#2B4189", creme="#E8CFA8", bois="#6A4428", pierre="#A09A98", vitre="#2F3E5E",
                     lanterne="#F4A93C", embrasure="#2C1B12", metal="#232327"),
        graines=dict(
            brique=[(33, 47, 77), (25, 38, 66), (40, 58, 98)],
            bois=[(65, 47, 35), (77, 57, 43), (51, 37, 27), (89, 73, 61)],
            pierre=[(90, 87, 87), (116, 109, 106), (106, 98, 95), (133, 123, 117)],
            creme=[(231, 195, 162), (221, 187, 154), (198, 168, 140), (169, 145, 122)],
            embrasure=[(33, 23, 17)], vitre=[(28, 32, 43)], metal=[(14, 12, 11)],
            lanterne=[(182, 134, 61), (230, 160, 60), (240, 190, 90)]),
    ),
    # Maison du druide : toit très pentu moussu, lierre, jardinières, auvent de séchage ouvert à droite (deux poteaux, bouquets
    # d'herbes suspendus). Largeur hors tout 14,6 m (maison 10,3 m + auvent 3,7 m ; plan : 11,6 + 2), profondeur x 0,88
    # (murs 8,5 m comme au plan), hauteur gardée. Lierre, fleurs et bouquets : fins, gardés dans la texture (cuisson depuis la haute
    # définition, couleurs hors palette conservées) ; bouquets d'herbes protégés de la décimation.
    "druide": dict(
        nom="Druide", type="maison",
        source=os.path.join(REF, "maison_druide_tripo", "mossy+cottage+3d+model.fbx"),
        largeur=14.6, echelle_y=0.88, budget=17000, atlas=2048, collision=320, emprise_z=(0.9, 1.3),
        protege_pierre=0.8, lanterne=False, garde_hors_palette=22.0, protege_herbes=0.95,
        protege=[(-9, 9, -9, 9, -0.1, 1.4, 0.7)],
        palette=dict(brique="#6E7A36", creme="#E6CBA2", bois="#5B3E27", pierre="#9A948C", vitre="#2F3B4A",
                     lanterne="#F4A93C", embrasure="#2A1C12", metal="#232327"),
        graines=dict(
            brique=[(92, 85, 42), (111, 103, 54), (147, 128, 79), (71, 59, 17)],
            bois=[(74, 58, 39), (66, 50, 34), (82, 68, 44), (58, 43, 29), (50, 36, 23)],
            creme=[(224, 185, 144), (193, 158, 121)],
            pierre=[(106, 97, 87), (92, 82, 71), (127, 117, 107)],
            embrasure=[(40, 29, 18), (29, 18, 8)], vitre=[(24, 27, 32)], metal=[(14, 12, 11)]),
    ),
    # Boutique du mécano : haut pignon avant (deux petites fenêtres), grande vitrine, arche vide, tuyau de poêle coudé, girouette
    # en engrenage. Largeur hors tout 12,6 m (plan 12,6 m, soubassement 11 m), profondeur x 1,0 (9,7 m, plan 9,6 m), hauteur
    # gardée (15 m : un étage et un haut toit ; plan : faîtage 9,5 m).
    "mecano": dict(
        nom="Mecano", type="maison",
        source=os.path.join(REF, "maison_mecano_tripo", "medieval+cottage+3d+model.fbx"),
        largeur=12.6, echelle_y=1.0, cadre=(4.9, 0.6), sans_emission=True, budget=15500, atlas=2048, collision=320, emprise_z=(0.9, 1.4),
        protege_pierre=0.8, lanterne=False,
        protege=[(-9, 9, -9, 9, -0.1, 1.5, 0.7)],
        palette=dict(brique="#C4512F", creme="#EBD3AB", bois="#6A4428", pierre="#A39D95", vitre="#2F3B4A",
                     lanterne="#F4A93C", embrasure="#2C1B12", metal="#2F2F33"),
        graines=dict(
            brique=[(138, 58, 41), (104, 34, 24), (120, 45, 32)],
            bois=[(74, 50, 35), (83, 57, 42), (64, 42, 29), (52, 28, 19)],
            pierre=[(112, 100, 90), (124, 110, 100), (102, 89, 79), (139, 124, 114), (88, 73, 62)],
            creme=[(227, 188, 152), (219, 180, 145), (206, 169, 136), (175, 144, 118)],
            embrasure=[(32, 21, 14)], metal=[(14, 12, 11)]),
    ),
    # Taverne « Le Tonneau Percé » (02/10/2026, une seule image Grok -> Tripo) : grande salle basse à toit de tuiles et demi-croupes,
    # haute cheminée de pierre à gauche, tonneau posé de travers sur le toit, enseigne (planche sans texte) suspendue à une potence de
    # fer à droite, une seule porte étroite en arche. Pas d'émission (les vitres sombres se confondent avec les ombres des tuiles dans la texture). Échelle : la largeur est celle du TOIT (l'enseigne dépasse de 2,3 m à droite et
    # n'entre pas dans la largeur : x_enseigne_tripo = abscisse Tripo où commence la potence). Profondeur étirée (echelle_y > 1 : le
    # modèle Tripo est beaucoup plus plat que le plan, 0,44 de profondeur sur largeur contre 0,70), hauteur gardée.
    "taverne": dict(
        nom="Taverne", type="taverne",
        source=os.path.join(REF, "taverne_tripo", "medieval+tavern+3d+model.fbx"),
        largeur=19.0, largeur_ref_tripo=0.882, echelle_y=1.25, budget=21000, atlas=2048, collision=420,
        sans_emission=True, inset_arche=0.177, x_enseigne_tripo=0.375, emprise_z_tripo=(0.041, 0.049), cadre=(4.4, 0.45), arche_x_tripo=(0.0, 0.24),
        protege_pierre=0.8, lanterne=False,
        protege=[],
        # en unités Tripo (x0, x1, y0, y1, z0, z1, poids) : soubassement et façade, enseigne et potence, tonneau du toit, cheminée
        protege_tripo=[(-0.50, 0.40, -0.23, 0.0, -0.01, 0.30, 0.55),
                       (0.36, 0.50, -0.16, -0.05, 0.03, 0.30, 0.95),
                       (0.06, 0.22, -0.21, -0.03, 0.25, 0.45, 0.9),
                       (-0.37, -0.28, -0.14, -0.04, 0.28, 0.55, 0.85)],
        palette=dict(brique="#C4512F", creme="#EBD3AB", bois="#6A4428", pierre="#A39D95", vitre="#2F3B4A",
                     lanterne="#F4A93C", embrasure="#2C1B12", metal="#232327"),
    ),
}

ARGS = mp.ARGS


def option(nom, defaut=None, conv=str):
    if nom in ARGS:
        return conv(ARGS[ARGS.index(nom) + 1])
    return defaut


PIECE = option("--piece")
if PIECE not in PIECES:
    raise SystemExit("--piece <%s> obligatoire" % "|".join(PIECES))
CFG = PIECES[PIECE]
NOM = CFG["nom"]
LARGEUR = option("--largeur", CFG["largeur"], float)
BUDGET = option("--budget", CFG["budget"], int)
CFG = dict(CFG)
CFG["echelle_y"] = option("--ey", CFG.get("echelle_y", 1.0), float)   # échelle de la profondeur relative à la largeur
CFG["echelle_z"] = option("--ez", CFG.get("echelle_z", 1.0), float)   # échelle de la hauteur relative à la largeur
BUDGET_COLLISION = option("--collision", CFG["collision"], int)
TAILLE = option("--texture", CFG["atlas"], int)
ANGLE = math.radians(option("--angle", 45.0, float))
PALETTE_RETENUE = option("--palette", 1, int)
W_FOND = option("--wfond", 0.0, float)    # protection (0 à 1) du dos et des côtés
W_AVANT = option("--wavant", 0.4, float)   # protection du pan avant
mp.ANGLE_LISSAGE = ANGLE   # lisser() de montagne_pipeline lit cette valeur

log = lambda *a: (print("[maison]", *a), sys.stdout.flush())
triangles = mp.triangles
selectionner = mp.selectionner


def hexa(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], dtype=np.float32) / 255.0


def lisse(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


# ---------------------------------------------------------------------------------------------- 1. import et échelle
def importer():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=CFG["source"])
    maillages = [o for o in bpy.data.objects if o.type == "MESH"]
    selectionner(maillages)
    if len(maillages) > 1:
        bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    for x in [x for x in bpy.data.objects if x.type != "MESH"]:
        bpy.data.objects.remove(x)
    o.parent = None
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    me = o.data
    n = len(me.vertices)
    co = np.empty(n * 3, dtype=np.float64); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
    largeur_tripo = CFG.get("largeur_ref_tripo", co[:, 0].max() - co[:, 0].min())   # taverne : le toit, sans l'enseigne
    s = LARGEUR / largeur_tripo
    co *= s
    co[:, 1] *= CFG.get("echelle_y", 1.0)
    co[:, 2] *= CFG.get("echelle_z", 1.0)
    global ORIGINE
    if CFG.get("type") == "forge":
        # origine : centre de l'emprise du sol et du soubassement (z < 1,4 m, hors marches de la porte), au sol
        co[:, 0] -= co[:, 0].min(); co[:, 1] -= co[:, 1].min(); co[:, 2] -= co[:, 2].min()
        tr = (co[:, 2] < 1.4) & (co[:, 1] > 1.2)
        # plan : origine au coin sud-ouest du soubassement de la maison (x est, y nord)
        house = (co[:, 2] > 1.2) & (co[:, 2] < 1.6) & (co[:, 0] < 0.55 * LARGEUR)
        plan = (co[house, 0].min(), co[house, 1].min())
        cx = (co[tr, 0].min() + co[tr, 0].max()) / 2
        cy = (co[tr, 1].min() + co[tr, 1].max()) / 2
        ORIGINE = dict(cx=float(cx), cy=float(cy), plan_x=float(plan[0] - cx), plan_y=float(plan[1] - cy))
    else:
        # origine : centre de l'emprise du soubassement (tranche z 0,6-0,95 m, hors cadre d'arche et marches), au sol
        ez = CFG.get("emprise_z", (0.6 * LARGEUR / 8.6, 0.95 * LARGEUR / 8.6))
        if "emprise_z_tripo" in CFG:
            ez = tuple(v * s * CFG.get("echelle_z", 1.0) for v in CFG["emprise_z_tripo"])
        tr = (co[:, 2] > ez[0]) & (co[:, 2] < ez[1])
        if "x_enseigne_tripo" in CFG:   # l'enseigne et sa potence ne comptent pas dans l'emprise
            tr &= co[:, 0] < CFG["x_enseigne_tripo"] * s
        cx = (co[tr, 0].min() + co[tr, 0].max()) / 2
        cy = (co[tr, 1].min() + co[tr, 1].max()) / 2
        ORIGINE = dict(cx=float(cx), cy=float(cy), plan_x=0.0, plan_y=0.0)
    ORIGINE.update(s=float(s), ey=float(CFG.get("echelle_y", 1.0)), ez=float(CFG.get("echelle_z", 1.0)),
                   x_ens=float(CFG["x_enseigne_tripo"] * s - cx) if "x_enseigne_tripo" in CFG else 1e9)
    co[:, 0] -= cx; co[:, 1] -= cy; co[:, 2] -= co[:, 2].min()
    # forge : le sol de l'atelier (dalles de 0,36 m) est « à même le sol » : on enfonce tout pour que son dessus soit au terrain
    co[:, 2] -= CFG.get("enfoncement", 0.0)
    me.vertices.foreach_set("co", co.reshape(-1)); me.update()
    o.name = "HD_Tripo"
    log("import : %d triangles ; échelle %.4f m par unité Tripo ; hors tout %.2f x %.2f x %.2f m (x, y, z) ; soubassement %.2f x %.2f m ; "
        "centre Tripo déplacé de (%.2f, %.2f) m"
        % (triangles(o), s, np.ptp(co[:, 0]), np.ptp(co[:, 1]), np.ptp(co[:, 2]),
           np.ptp(co[tr, 0]), np.ptp(co[tr, 1]), cx, cy))
    return o


def tx(X):
    """Abscisse du repère final d'une abscisse Tripo (unités du fichier source) ; ty, tz : de même (taverne)."""
    return X * ORIGINE["s"] - ORIGINE["cx"]


def ty(Y):
    return Y * ORIGINE["s"] * ORIGINE["ey"] - ORIGINE["cy"]


def tz(Z):
    return Z * ORIGINE["s"] * ORIGINE["ez"]


ORIGINE = dict(cx=0.0, cy=0.0, plan_x=0.0, plan_y=0.0)   # décalage appliqué à l'import (m) ; origine du plan de la forge


def coords(o):
    me = o.data
    co = np.empty(len(me.vertices) * 3, dtype=np.float64); me.vertices.foreach_get("co", co)
    return co.reshape(-1, 3)


def couleur_aux_sommets(o):
    """Couleur de la texture 4K de Tripo au premier coin de chaque sommet (pour repérer la lanterne, les vitres)."""
    img = mp.image_source(o)
    w, h = img.size
    px = np.empty(w * h * 4, dtype=np.float32); img.pixels.foreach_get(px); px = px.reshape(h, w, 4)[:, :, :3]
    me = o.data
    uv = np.empty(len(me.loops) * 2, dtype=np.float32); me.uv_layers[0].data.foreach_get("uv", uv); uv = uv.reshape(-1, 2)
    vi = np.empty(len(me.loops), dtype=np.int32); me.loops.foreach_get("vertex_index", vi)
    col = np.zeros((len(me.vertices), 3), dtype=np.float32)
    ix = np.clip((uv[:, 0] * w).astype(int), 0, w - 1); iy = np.clip((uv[:, 1] * h).astype(int), 0, h - 1)
    col[vi] = px[iy, ix]
    return col


def plan(x, y):
    """Coordonnées du plan (origine au coin sud-ouest du soubassement de la maison) d'un point du repère final."""
    return x - ORIGINE["plan_x"], y - ORIGINE["plan_y"]


def diag_forge(o):
    """Journal de mesures de la forge (repère du plan) : sol, foyer, poteaux, toit de l'atelier, carte d'occupation."""
    co = coords(o); col = couleur_aux_sommets(o)
    px, py = plan(co[:, 0], co[:, 1])
    z = co[:, 2]
    v = col.max(1); sat = (v - col.min(1)) / np.maximum(v, 1e-4)
    pierre = (sat < 0.25) & (v > 0.30)
    log("plan : origine du plan = coin sud-ouest du soubassement ; hors tout x %.1f à %.1f, y %.1f à %.1f, z jusqu'à %.1f m"
        % (px.min(), px.max(), py.min(), py.max(), z.max()))
    sol = (z < 0.9) & (px > 11.0) & (py > 3.0) & (py < 5.0)
    log("sol de l'atelier : dessus à %.2f m (sommets z < 0,9 m sur x > 11, y 3 à 5)" % z[sol].max())
    for (nom, m) in (("foyer (pierre, z 0,6 à 5, x > 10)", pierre & (z > 0.6) & (z < 5.0) & (px > 10.0) & (px < 17.0) & (py > 4.0)),
                     ("poteaux (x > 16)", (px > 16.0) & (z > 1.0) & (z < 3.5) & (~pierre))):
        if m.any():
            log("%s : x %.2f à %.2f, y %.2f à %.2f" % (nom, px[m].min(), px[m].max(), py[m].min(), py[m].max()))
    sud = (z < 1.2) & (px > 14.0) & (px < 17.0) & (py > 0.5) & (py < 2.6)
    log("sol de l'atelier (bande sud) : dessus à %.2f m, dessous %.2f m" % (z[sud].max(), z[sud].min()))
    sombre = (v < 0.22) & (sat < 0.6)
    b = sombre & (px > 10.5) & (px < 14.5) & (py > 3.2) & (py < 4.8) & (z > 1.2) & (z < 5.0)
    if b.any():
        log("bouche du foyer (sommets sombres sur la face sud) : x %.2f à %.2f, z %.2f à %.2f, y %.2f à %.2f ; centre (%.2f, %.2f, %.2f)"
            % (px[b].min(), px[b].max(), z[b].min(), z[b].max(), py[b].min(), py[b].max(), px[b].mean(), py[b].mean(), z[b].mean()))
    d = sombre & (px > 2.0) & (px < 7.0) & (py < 2.6) & (z > 0.8) & (z < 5.0)
    if d.any():
        log("embrasure de la porte (sommets sombres sur la façade) : x %.2f à %.2f, z %.2f à %.2f, y %.2f à %.2f"
            % (px[d].min(), px[d].max(), z[d].min(), z[d].max(), py[d].min(), py[d].max()))
    w = (px > 6.0) & (px < 10.5) & (py < 2.6) & (z > 2.0) & (z < 5.5) & sombre
    if w.any():
        log("fenêtre à barreaux (sombre) : x %.2f à %.2f, z %.2f à %.2f" % (px[w].min(), px[w].max(), z[w].min(), z[w].max()))
    toit = (px > 10.5) & (z > 3.0) & (z < 9.0) & (~pierre)
    for xs in (11.0, 12.5, 14.0, 15.5, 17.0):
        mm = toit & (np.abs(px - xs) < 0.3) & (py > 2.0)
        if mm.any():
            log("toit de l'atelier au-dessus de x = %.1f : z %.2f à %.2f (y %.1f à %.1f)" % (xs, z[mm].min(), z[mm].max(), py[mm].min(), py[mm].max()))
    # carte d'occupation du volume 0,7 à 2,6 m (le corps du forgeron), cellules de 0,5 m
    m = (z > 0.7) & (z < 2.6) & (px > 8.0)
    cell = 0.5; x0, y0 = 8.0, -2.0
    nx = int((px.max() - x0) / cell) + 1; ny = int((py.max() - y0) / cell) + 1
    g = np.zeros((ny, nx), int)
    ix = ((px[m] - x0) / cell).astype(int); iy = ((py[m] - y0) / cell).astype(int)
    ok = (ix >= 0) & (iy >= 0)
    np.add.at(g, (iy[ok], ix[ok]), 1)
    log("occupation 0,7 à 2,6 m, x de %.1f (pas 0,5), y du nord au sud :" % x0)
    for r in range(ny - 1, -1, -1):
        log("%6.1f " % (y0 + r * cell) + "".join("#" if c > 40 else ("+" if c > 4 else ".") for c in g[r]))


# ---------------------------------------------------------------------------------------------- 2. toit
def reparer_toit(o):
    """Encoche de la faîtière autour de la cheminée (côté dos) : la pierre de la souche affleure entre deux faîtières.
    Recouverte d'une faîtière neuve (boîte chanfreinée) dont les UV pointent sur un texel de tuile de la texture 4K
    (relevé sur une faîtière voisine) : la cuisson reprend donc le rouge des tuiles. Retourne le nombre de faces ajoutées."""
    co = coords(o)
    col = couleur_aux_sommets(o)
    x, y, z = co[:, 0], co[:, 1], co[:, 2]
    k = LARGEUR / 8.6
    rouge = (col[:, 0] > col[:, 1] * 1.8) & (col[:, 0] > 0.25)
    # texel de tuile : un sommet rouge de la faîtière à droite de la cheminée
    cand = np.nonzero(rouge & (z > 7.15 * k) & (x > -2.0 * k) & (x < 0.0) & (np.abs(y) < 0.3 * k))[0]
    if len(cand) == 0:
        log("réparation du toit : texel de tuile introuvable, rien ajouté")
        return 0
    me = o.data
    uvl = me.uv_layers[0].data
    vi = np.empty(len(me.loops), dtype=np.int32); me.loops.foreach_get("vertex_index", vi)
    meilleur = cand[np.argmax(col[cand, 0])]   # le rouge le plus clair des faîtières voisines
    boucle = int(np.nonzero(vi == meilleur)[0][0])
    uv = tuple(uvl[boucle].uv)
    # boîte sur le côté dos de la souche, à hauteur de faîtière (cotes relevées sur le HD : souche x -3,54 à -2,47, y -0,84 à
    # 0,35 ; faîtières jusqu'à y = 0,42 et z = 7,25 m)
    bm = bmesh.new(); bm.from_mesh(me)
    uvlay = bm.loops.layers.uv.active
    g = bmesh.ops.create_cube(bm, size=1.0)
    verts = g["verts"]
    cx, cy, cz = -3.0 * k, 0.36 * k, 6.98 * k
    for v in verts:
        v.co = Vector((cx + v.co.x * 1.0 * k, cy + v.co.y * 0.24 * k, cz + v.co.z * 0.50 * k))
    bmesh.ops.bevel(bm, geom=[e for e in bm.edges if all(v in verts for v in e.verts)] + verts, offset=0.05 * k, segments=2,
                    affect="EDGES")
    nf = 0
    for f in bm.faces:
        if any(l[uvlay].uv.length == 0 for l in f.loops) and all(v.co.z > 6.5 * k for v in f.verts):
            for l in f.loops:
                l[uvlay].uv = uv
            nf += 1
    bm.to_mesh(me); bm.free(); me.update()
    log("réparation du toit : faîtière neuve ajoutée contre la cheminée côté dos (%d faces, UV sur un texel de tuile)" % nf)
    return nf


# ---------------------------------------------------------------------------------------------- 3. décimation
POIDS_COULEURS = None   # couleurs Tripo aux sommets du maillage HD (pour protéger les pierres)


def poids_sommets(co):
    """Poids de 0 à 1 par sommet (protection contre la décimation)."""
    x, y, z = co[:, 0], co[:, 1], co[:, 2]
    # fond : le dos et les côtés fondent (0,25), le devant et le dessus du pan avant tiennent mieux (jusqu'à 0,6)
    w = W_FOND + (W_AVANT - W_FOND) * (1 - lisse(-0.5, 3.0, y))
    dx = dy = 0.0
    dz = 0.0
    if CFG.get("repere_protege") == "travail":
        # repère de travail : origine au coin sud-ouest de la boîte englobante, avant recentrage et enfoncement
        dx, dy, dz = -ORIGINE["cx"], -ORIGINE["cy"], -CFG.get("enfoncement", 0.0)
    for (x0, x1, y0, y1, z0, z1, p) in CFG["protege"]:
        m = ((x - dx) >= x0) & ((x - dx) <= x1) & ((y - dy) >= y0) & ((y - dy) <= y1) & ((z - dz) >= z0) & ((z - dz) <= z1)
        w = np.where(m, np.maximum(w, p), w)
    for (x0, x1, y0, y1, z0, z1, p) in CFG.get("protege_tripo", []):   # boîtes en unités Tripo (taverne)
        m = (x >= tx(x0)) & (x <= tx(x1)) & (y >= ty(y0)) & (y <= ty(y1)) & (z >= tz(z0)) & (z <= tz(z1))
        w = np.where(m, np.maximum(w, p), w)
    if CFG.get("protege_herbes") and POIDS_COULEURS is not None:
        # bouquets d'herbes suspendus sous l'auvent : vert-olive, dans le dernier quart de la largeur, à hauteur des poutres
        hx = x - (-ORIGINE["cx"]); kk = LARGEUR / 13.6
        hue, sat_h, vh = teinte(POIDS_COULEURS)
        herbe = (hx > 0.74 * LARGEUR) & (z > 2.8 * kk) & (z < 7.0 * kk) & (hue > 38) & (hue < 110) & (sat_h > 0.3) & (vh > 0.25)
        w = np.where(herbe, np.maximum(w, CFG["protege_herbes"]), w)
    if CFG.get("protege_pierre"):
        col = POIDS_COULEURS
        v = col.max(1); sat = (v - col.min(1)) / np.maximum(v, 1e-4)
        w = np.where((sat < 0.25) & (v > 0.30), np.maximum(w, CFG["protege_pierre"]), w)
    return (1.0 - 0.7 * w).astype(np.float32)   # Blender : un poids bas protège (moins de fusions), 1 = fond


def decimer(src, budget, nom):
    o = src.copy(); o.data = src.data.copy(); bpy.context.collection.objects.link(o)
    o.name = nom; o.data.name = nom
    selectionner([o])
    # triangulation préalable (la décimation travaille en triangles)
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bm.to_mesh(o.data); bm.free()
    co = coords(o)
    global POIDS_COULEURS
    POIDS_COULEURS = couleur_aux_sommets(src) if (CFG.get("protege_pierre") or CFG.get("protege_herbes")) else None
    vg = o.vertex_groups.new(name="protege")
    w = poids_sommets(co)
    for pas in np.unique(np.round(w, 2)):
        idx = np.nonzero(np.round(w, 2) == pas)[0].tolist()
        vg.add(idx, float(pas), "REPLACE")
    avant = triangles(o)
    dg = bpy.context.evaluated_depsgraph_get()
    mod = o.modifiers.new("Decimation", "DECIMATE")
    mod.decimate_type = "COLLAPSE"
    mod.ratio = min(1.0, budget / avant)
    mod.use_collapse_triangulate = True
    mod.vertex_group = "protege"
    mod.vertex_group_factor = 1.0
    dg.update()
    me = bpy.data.meshes.new_from_object(o.evaluated_get(dg))
    o.modifiers.clear()
    o.vertex_groups.clear()
    mp.remplacer_maillage(o, me)
    me.name = nom
    log("décimation %-18s %6d -> %6d triangles" % (nom, avant, triangles(o)))
    c = coords(o); t = np.array([[v for v in p.vertices] for p in o.data.polygons if len(p.vertices) == 3]) if False else None
    ys = np.array([c[list(p.vertices), 1].mean() for p in o.data.polygons])
    log("  répartition : %d triangles côté façade (y < 0), %d côté dos" % ((ys < 0).sum(), (ys >= 0).sum()))
    return o


def sans_dessous(o):
    """Supprime les faces du dessous (invisibles : posées sur le sol)."""
    bm = bmesh.new(); bm.from_mesh(o.data)
    bm.faces.ensure_lookup_table()
    sup = [f for f in bm.faces if f.normal.z < -0.5 and f.calc_center_median().z < 0.25]
    bmesh.ops.delete(bm, geom=sup, context="FACES")
    bm.to_mesh(o.data); bm.free()
    o.data.update()
    log("faces du dessous retirées : %d" % len(sup))


# ---------------------------------------------------------------------------------------------- 4. atlas
def cuire(src, cible):
    """Dépliage neuf de la pièce décimée, puis cuisson de la couleur Tripo dessus (Cycles, Selected to Active)."""
    if mp.image_source(src) is None:
        raise RuntimeError("texture Tripo introuvable")
    me = cible.data
    while len(me.uv_layers) > 0:
        me.uv_layers.remove(me.uv_layers[0])
    me.uv_layers.new(name="UVMap")
    selectionner([cible])
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.003, area_weight=0.0, scale_to_bounds=True)
    bpy.ops.object.mode_set(mode="OBJECT")
    img = bpy.data.images.new(NOM + "_Texture", TAILLE, TAILLE, alpha=False)
    mat = bpy.data.materials.new(NOM); mat.use_nodes = True
    noeud = mat.node_tree.nodes.new("ShaderNodeTexImage"); noeud.image = img
    mat.node_tree.nodes.active = noeud
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    cible.data.materials.clear(); cible.data.materials.append(mat)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = 4
    selectionner([src, cible], cible)
    bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"}, use_selected_to_active=True, cage_extrusion=0.2,
                        max_ray_distance=0.6, margin=16, margin_type="EXTEND", use_clear=True, target="IMAGE_TEXTURES")
    if bsdf is not None:
        mat.node_tree.links.new(noeud.outputs["Color"], bsdf.inputs["Base Color"])
    log("atlas cuit %d²" % TAILLE)
    return img


# ---------------------------------------------------------------------------------------------- 5. palette
def srgb_vers_lab(c):
    c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    m = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]], dtype=np.float32)
    xyz = c @ m.T / np.array([0.95047, 1.0, 1.08883], dtype=np.float32)
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)


# graines de classification : couleurs (sRGB 0-255) relevées par k-means sur la texture Tripo 4K, rangées par classe
GRAINES = {
    "brique": [(141, 48, 31), (121, 38, 25), (102, 31, 20), (91, 26, 17), (170, 62, 40)],
    "creme": [(230, 195, 157), (235, 200, 161), (222, 187, 151), (176, 150, 125)],
    "bois": [(73, 49, 32), (62, 40, 26), (54, 34, 22)],
    "pierre": [(98, 90, 83), (118, 105, 96), (137, 122, 111), (110, 106, 102)],
    "embrasure": [(31, 21, 13)],
    "vitre": [(33, 36, 40), (45, 52, 60)],
    "metal": [(17, 17, 18), (12, 11, 11)],
    "lanterne": [(182, 134, 61), (230, 160, 60), (240, 190, 90)],
}
CLASSES = ["brique", "creme", "bois", "pierre", "vitre", "lanterne", "embrasure", "metal"]
GARDE_MODELE = 0.55   # part du modelé d'origine conservée dans la variante 1


def classer(px):
    """px : (n, 3) sRGB 0-1 -> indice de classe (0..7) par plus proche graine en Lab."""
    graines = []; cl = []
    table = dict(GRAINES); table.update(CFG.get("graines", {}))
    for i, nom in enumerate(CLASSES):
        for g in table.get(nom, []):
            graines.append(np.array(g, dtype=np.float32) / 255.0); cl.append(i)
    gl = srgb_vers_lab(np.array(graines, dtype=np.float32))
    cl = np.array(cl)
    res = np.empty(len(px), dtype=np.int8)
    pas = 400000
    for a in range(0, len(px), pas):
        lab = srgb_vers_lab(px[a:a + pas])
        d = ((lab[:, None, :] - gl[None]) ** 2).sum(2)
        res[a:a + pas] = cl[d.argmin(1)]
    # règle sur les petites zones chaudes : le verre de la lanterne (orange vif, saturé) prime sur la brique et la crème
    v = px.max(1); mn = px.min(1); sat = (v - mn) / np.maximum(v, 1e-4)
    r, g, b = px[:, 0], px[:, 1], px[:, 2]
    dd = np.maximum(v - mn, 1e-5)
    hue = np.where(v == r, ((g - b) / dd) % 6, np.where(v == g, (b - r) / dd + 2, (r - g) / dd + 4)) * 60
    chaud = (sat > 0.55) & (v > 0.55) & (hue > 24) & (hue < 58)
    if "lanterne" in table or "graines" not in CFG:
        res[chaud] = CLASSES.index("lanterne")
    return res


def lisser_classes(cls, n=8):
    """Filtre modal 3 x 3 sur la carte des classes (retire le sel et poivre de la texture)."""
    h, w = cls.shape
    comp = np.zeros((n, h, w), dtype=np.uint8)
    p = np.pad(cls, 1, mode="edge")
    for k in range(n):
        m = (p == k).astype(np.uint8)
        s = np.zeros((h, w), dtype=np.uint8)
        for dy in range(3):
            for dx in range(3):
                s += m[dy:dy + h, dx:dx + w]
        comp[k] = s
    return comp.argmax(0).astype(np.int8)


def quantifier(img):
    """Variante 1 : classes franches, modelé conservé à GARDE_MODELE. Retourne (pixels (h, w, 4), classes (h, w))."""
    w, h = img.size
    px = np.empty(w * h * 4, dtype=np.float32); img.pixels.foreach_get(px)
    px = px.reshape(h, w, 4)[:, :, :3]
    cls = classer(px.reshape(-1, 3)).reshape(h, w)
    cls = lisser_classes(cls)
    pal = {n: hexa(CFG["palette"][n]) for n in CLASSES}
    hors = None
    if CFG.get("garde_hors_palette"):
        # pixels éloignés de toutes les graines (fleurs, lierre vif...) : couleur d'origine gardée
        table = dict(GRAINES); table.update(CFG.get("graines", {}))
        gl = srgb_vers_lab(np.array([np.array(g, dtype=np.float32) / 255.0 for k in table for g in table[k]], dtype=np.float32))
        labp = srgb_vers_lab(px.reshape(-1, 3))
        dmin = np.empty(len(labp), dtype=np.float32)
        for a in range(0, len(labp), 400000):
            dmin[a:a + 400000] = np.sqrt(((labp[a:a + 400000, None, :] - gl[None]) ** 2).sum(2)).min(1)
        hors = (dmin > CFG["garde_hors_palette"]).reshape(cls.shape)
    lum = (px * np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)).sum(2)
    # luminance moyenne de chaque classe sur les pixels réellement peints (le fond noir hors îlots est écarté)
    utile = lum > 0.02
    sortie = np.zeros((h, w, 3), dtype=np.float32)
    for i, nom in enumerate(CLASSES):
        m = (cls == i)
        if not m.any():
            continue
        mu = lum[m & utile].mean() if (m & utile).any() else lum[m].mean()
        f = np.clip((lum[m] / max(mu, 1e-3)), 0.35, 1.9) ** 0.8
        f = 1.0 + GARDE_MODELE * (f - 1.0)
        sortie[m] = np.clip(pal[nom][None, :] * f[:, None], 0.0, 1.0)
    if hors is not None:
        hors &= utile
        sortie[hors] = px[hors]
        log("couleurs hors palette gardées : %.2f %% des pixels peints" % (100.0 * hors.sum() / max(utile.sum(), 1)))
    sortie[~utile] = 0.0
    log("palette : " + ", ".join("%s %.1f %%" % (n, 100.0 * (cls[utile] == i).mean()) for i, n in enumerate(CLASSES)))
    return sortie, cls, utile


def image_depuis(nom, rgb):
    h, w, _ = rgb.shape
    img = bpy.data.images.new(nom, w, h, alpha=False)
    out = np.ones((h, w, 4), dtype=np.float32); out[:, :, :3] = rgb
    img.pixels.foreach_set(out.reshape(-1)); img.update()
    return img


def carte_emission(cls, utile):
    """Blanc là où la classe est vitre ou verre de la lanterne, noir ailleurs (allumé la nuit par CycleJourNuit)."""
    m = ((cls == CLASSES.index("vitre")) | (cls == CLASSES.index("lanterne"))) & utile
    if CFG.get("sans_emission"):
        m = np.zeros_like(m)
    e = np.zeros(cls.shape + (3,), dtype=np.float32)
    e[m] = 1.0
    # dilatation d'un pixel (marge d'UV) pour que le filtrage ne ramène pas du noir sur les bords
    return e


def enregistrer(img, chemin):
    img.filepath_raw = chemin
    img.file_format = "PNG"
    img.save()
    log("texture", os.path.relpath(chemin, RACINE))


# ---------------------------------------------------------------------------------------------- 6. collision
def enveloppe(pts, nom, tris):
    """Enveloppe convexe de pts (n, 3), décimée à environ tris triangles ; retourne un objet."""
    bm = bmesh.new()
    for p in pts:
        bm.verts.new(p)
    bmesh.ops.convex_hull(bm, input=bm.verts[:])
    # sommets laissés isolés par l'enveloppe
    iso = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=iso, context="VERTS")
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(4), verts=bm.verts[:], edges=bm.edges[:])
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    me = bpy.data.meshes.new(nom); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(nom, me); bpy.context.collection.objects.link(o)
    n = len(me.polygons)
    if n > tris:
        selectionner([o])
        mod = o.modifiers.new("d", "DECIMATE"); mod.decimate_type = "COLLAPSE"; mod.ratio = tris / n
        mod.use_collapse_triangulate = True
        dg = bpy.context.evaluated_depsgraph_get(); dg.update()
        me2 = bpy.data.meshes.new_from_object(o.evaluated_get(dg)); o.modifiers.clear()
        mp.remplacer_maillage(o, me2); me2.name = nom
    return o


def joindre_enveloppes(parts):
    objs = []
    for nom, pts, tris in parts:
        if len(pts) < 8:
            continue
        # sous-échantillon (les enveloppes convexes ne dépendent que des points extrêmes)
        if len(pts) > 20000:
            pts = pts[np.random.default_rng(3).choice(len(pts), 20000, replace=False)]
        objs.append(enveloppe(pts, "c_" + nom, tris))
    selectionner(objs)
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = NOM + "_Collision"; o.data.name = NOM + "_Collision"
    n = triangles(o)
    log("collision : %d triangles (%s)" % (n, ", ".join(p[0] for p in parts)))
    if n > BUDGET_COLLISION:
        log("ATTENTION : collision au-dessus du plafond de %d triangles" % BUDGET_COLLISION)
    return o


def collision(hd):
    """Enveloppes convexes réunies en un seul maillage (jamais le maillage rendu)."""
    if CFG.get("type") == "forge":
        return collision_forge(hd)
    if CFG.get("type") == "maison":
        return collision_maison(hd)
    if CFG.get("type") == "taverne":
        return collision_taverne(hd)
    co = coords(hd)
    x, y, z = co[:, 0], co[:, 1], co[:, 2]
    zegout = 3.0 * LARGEUR / 8.6   # le toit commence à l'égout (sablière haute)
    marches = (y < -3.3) & (z < 0.7) & (x > -2.5) & (x < 0.6)
    lanterne = (y < -3.0) & (z > 1.3) & (z < 3.2) & (x > 0.5) & (x < 1.6)
    corps = (z < zegout) & ~marches & ~lanterne
    toit = (z >= zegout) & ~((x < -2.3) & (z > 7.0 * LARGEUR / 8.6))   # souche de cheminée au-dessus du faîtage : omise
    return joindre_enveloppes([("corps", co[corps], 60), ("toit", co[toit], 70), ("marches", co[marches], 36)])


# --- forge : zones de circulation de l'atelier (repère du plan : x est, y nord, m) -------------------------------------
# Rectangles qui doivent rester libres de collision de 0 à 3 m de haut (le sol de l'atelier reste marchable).
ZONES_LIBRES = {
    "bande sud de l'atelier (devant le foyer, hors poteaux)": (10.6, 15.8, 0.6, 3.7),
    "bande est de l'atelier (face est du foyer, jusqu'aux poteaux)": (14.1, 15.8, 3.7, 9.0),
}
ZEGOUT_MAISON = 6.2   # hauteur de l'égout de la maison (m)


def collision_forge(hd):
    """Maison (corps, toit), marches, foyer, souche de la cheminée, toit de l'atelier et deux poteaux, en enveloppes convexes.
    Rien sur le sol de l'atelier ni au-dessus à moins de 3 m (le toit de l'atelier est à 4,4 m au plus bas)."""
    co = coords(hd); col = couleur_aux_sommets(hd)
    px, py = plan(co[:, 0], co[:, 1]); z = co[:, 2]
    v = col.max(1); sat = (v - col.min(1)) / np.maximum(v, 1e-4)
    pierre = (sat < 0.25) & (v > 0.30)
    zeg = ZEGOUT_MAISON
    maison = px < 10.6
    corps = maison & (z < zeg) & (py > 0.2)
    marches = maison & (py <= 0.2) & (z < 1.6)
    toit_m = maison & (z >= zeg - 0.2)
    foyer = (~maison) & pierre & (px < 14.6) & (py > 3.9) & (z > 0.1) & (z < 5.4)
    souche = (~maison) & (px < 13.2) & (z > 5.4) & pierre
    toit_a = (~maison) & (px > 9.4) & (z > 4.4) & (~pierre)
    pot_s = (px > 16.0) & (py < 6.0) & (z < 4.6) & (~pierre) & (z > 0.3)
    pot_n = (px > 16.0) & (py >= 6.0) & (z < 4.6) & (~pierre) & (z > 0.3)
    parts = [("corps", co[corps], 60), ("toit_maison", co[toit_m], 60), ("marches", co[marches], 40), ("foyer", co[foyer], 60),
             ("souche", co[souche], 40), ("toit_atelier", co[toit_a], 40), ("poteau_s", co[pot_s], 24), ("poteau_n", co[pot_n], 24)]
    return joindre_enveloppes(parts)


def verifier_zones(col_obj):
    """Aucun point de la collision dans les zones libres, de 0,05 à 3 m de haut (test point par point, enveloppes convexes)."""
    me = col_obj.data
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
    vus = set(); morceaux = []
    for f in bm.faces:
        if f.index in vus:
            continue
        pile = [f]; vus.add(f.index); groupe = []
        while pile:
            g = pile.pop(); groupe.append(g)
            for e in g.edges:
                for h in e.link_faces:
                    if h.index not in vus:
                        vus.add(h.index); pile.append(h)
        morceaux.append([(np.array(g.normal), float(np.array(g.normal) @ np.array(g.calc_center_median()))) for g in groupe])
    bm.free()
    resultat = {}
    for nom, (x0, x1, y0, y1) in ZONES_LIBRES.items():
        xs = np.arange(x0, x1 + 1e-6, 0.25); ys = np.arange(y0, y1 + 1e-6, 0.25); zs = np.arange(0.05, 3.01, 0.25)
        pts = np.array([[ORIGINE["plan_x"] + a, ORIGINE["plan_y"] + b, c] for a in xs for b in ys for c in zs])
        dedans = np.zeros(len(pts), bool)
        for pl in morceaux:
            ok = np.ones(len(pts), bool)
            for n, d in pl:
                ok &= (pts @ n - d) <= 1e-4
            dedans |= ok
        resultat[nom] = int(dedans.sum())
        log("zone libre « %s » (x %.1f à %.1f, y %.1f à %.1f, z 0 à 3 m) : %d points sur %d dans la collision"
            % (nom, x0, x1, y0, y1, dedans.sum(), len(pts)))
    return resultat


# ---------------------------------------------------------------------------------------------- ancres
def ancres(hd):
    """Ancres au repère final de Blender (x droite vue de la façade, y vers le fond, z haut), en m ; Unity les convertit."""
    if CFG.get("type") == "forge":
        return ancres_forge(hd)
    if CFG.get("type") == "maison":
        return ancres_maison(hd)
    if CFG.get("type") == "taverne":
        return ancres_taverne(hd)
    p = CFG["porte"]; f = CFG["fenetre"]
    co = coords(hd)
    col = couleur_aux_sommets(hd)
    v = col.max(1); mn = col.min(1); sat = (v - mn) / np.maximum(v, 1e-4)
    r, g, b = col[:, 0], col[:, 1], col[:, 2]
    dd = np.maximum(v - mn, 1e-5)
    hue = np.where(v == r, ((g - b) / dd) % 6, np.where(v == g, (b - r) / dd + 2, (r - g) / dd + 4)) * 60
    verre = (sat > 0.5) & (v > 0.5) & (hue > 24) & (hue < 58) & (co[:, 1] < -2.5) & (co[:, 2] > 1.5) & (co[:, 2] < 3.4) & (co[:, 0] > 0.3)
    if verre.sum() > 5:
        lan = co[verre].mean(0)
    else:
        lan = np.array([1.0, -3.35, 2.45])
    log("lanterne : %d sommets de verre, centre (%.2f, %.2f, %.2f)" % (verre.sum(), lan[0], lan[1], lan[2]))
    # plan de façade : le nu du mur avant (mode de l'histogramme des y des sommets de la façade, par classes de 1 cm)
    zmur = (co[:, 2] > 1.5) & (co[:, 2] < 2.8) & (np.abs(co[:, 0]) < 3.5) & (co[:, 1] < 0.0) & (co[:, 1] > -4.5)
    h, bords = np.histogram(co[zmur, 1], bins=np.arange(-4.5, 0.0, 0.01))
    ymur = float(bords[h.argmax()] + 0.005)
    log("plan de façade : y = %.2f m" % ymur)
    face = [0.0, -1.0, 0.0]
    return dict(
        largeur=LARGEUR, facade_y=ymur,
        entree=dict(centre=[p["x"], ymur, 0.0], largeur=p["largeur"], haut=p["haut"]),
        lanterne=[float(lan[0]), float(lan[1]), float(lan[2])],
        ancres=[dict(nom="Porte_Pivot", pos=[p["x"] - p["largeur"] / 2, ymur, p["seuil"]], dir=face),
                dict(nom="Volet_G", pos=[f["x0"], ymur, (f["z0"] + f["z1"]) / 2], dir=face),
                dict(nom="Volet_D", pos=[f["x1"], ymur, (f["z0"] + f["z1"]) / 2], dir=face)],
        convention="Blender : x vers la droite vue de la façade, y vers le fond, z vers le haut ; origine au centre de l'emprise du soubassement, au sol.")


# Postes de la forge (repère du plan, m) : adaptation de l'implantation du brief au modèle, dont le foyer est deux fois plus
# massif que celui du plan (4,3 x 6,3 m au lieu de 2,2 x 2,4 m) et dont la bouche regarde le sud (voir brief-forge.md § Réception).
POSTES_FORGE = dict(enclume=(14.5, 1.7), frappe=(14.5, 2.9), bac=(14.8, 5.6), trempe=(15.8, 5.6))


def plan_vers_final(x, y):
    return float(x + ORIGINE["plan_x"]), float(y + ORIGINE["plan_y"])


def ancres_forge(hd):
    co = coords(hd); col = couleur_aux_sommets(hd)
    px, py = plan(co[:, 0], co[:, 1]); z = co[:, 2]
    v = col.max(1); sat = (v - col.min(1)) / np.maximum(v, 1e-4)
    sombre = (v < 0.22) & (sat < 0.6)
    pierre_f = (sat < 0.25) & (v > 0.30) & (px > 10.4) & (px < 14.0) & (py > 3.0) & (py < 5.5) & (z > 0.5) & (z < 5.0)
    # façade de la maison : nu du mur (mode des y des sommets de la façade entre les poteaux, hors marches)
    m = (z > 2.4) & (z < 4.5) & (px > 1.0) & (px < 9.5) & (py < 3.0) & (py > -1.5)
    h, bords = np.histogram(py[m], bins=np.arange(-1.5, 3.0, 0.01))
    ymur = float(bords[h.argmax()] + 0.005)
    marche = (px > 2.0) & (px < 6.0) & (z < 1.6) & (py < 1.5)
    ymarche = float(py[marche].min())
    # embrasure : cadre d'arche en pierre de la façade (detecter_arche), converti en repère du plan
    mf = dict(ymur=float(plan_vers_final(0, ymur)[1]), xmin=float(co[:, 0].min()), xmax=float(plan_vers_final(9.8, 0)[0]))
    ax0, ax1, azb, azh = detecter_arche(co, col, mf)
    xp0, xp1 = ax0 - ORIGINE["plan_x"], ax1 - ORIGINE["plan_x"]
    d = np.zeros(len(co), bool); d[np.argmin(np.abs(px - xp0) + np.abs(py - ymur) + np.abs(z - 2.4))] = True   # sommet de repère pour z du seuil
    log("façade : nu du mur à y = %.2f, marches jusqu'à y = %.2f ; ouverture x %.2f à %.2f (largeur %.2f)" % (ymur, ymarche, xp0, xp1, xp1 - xp0))
    xc = (xp0 + xp1) / 2
    b = sombre & (px > 10.8) & (px < 14.0) & (py > 3.2) & (py < 4.8) & (z > 1.6) & (z < 4.4)
    xb = float(px[b].mean())
    yb = float(py[pierre_f].min())   # face sud du foyer (pierre)
    log("bouche du foyer : centre x = %.2f, face sud y = %.2f, z %.2f à %.2f" % (xb, yb, z[b].min(), z[b].max()))
    sol = float(z[(z < 1.2) & (px > 14.0) & (px < 17.0) & (py > 0.5) & (py < 2.6)].max())

    def A(nom, xy, zz, vers):
        dx, dy = vers[0] - xy[0], vers[1] - xy[1]
        n = math.hypot(dx, dy)
        gx, gy = plan_vers_final(xy[0], xy[1])
        return dict(nom=nom, pos=[gx, gy, float(zz)], dir=[dx / n, dy / n, 0.0])

    P = POSTES_FORGE
    xpx, ypy = plan_vers_final(xp0, ymur)
    ancs = [
        dict(nom="Porte_Pivot", pos=[xpx, ypy, float(azb + 0.5)], dir=[0.0, -1.0, 0.0]),
        A("Foyer_Feu", (xb, yb + 0.6), sol + 1.7, (xb, yb - 5.0)),
        A("Enclume_Ancre", P["enclume"], sol, (P["enclume"][0], P["enclume"][1] + 5.0)),
        A("Bac_Ancre", P["bac"], sol, (P["bac"][0], P["bac"][1] + 5.0)),
        A("Poste_Chauffe", (xb, yb - 1.1), sol, (xb, yb)),
        A("Poste_Frappe", P["frappe"], sol, P["enclume"]),
        A("Poste_Trempe", P["trempe"], sol, P["bac"]),
    ]
    gx, gy = plan_vers_final(xc, ymarche)
    return dict(
        largeur=LARGEUR, facade_y=float(plan_vers_final(0, ymur)[1]),
        entree=dict(centre=[gx, gy, 0.0], largeur=1.8, haut=2.6),
        lanterne=None, ancres=ancs, origine_plan=[ORIGINE["plan_x"], ORIGINE["plan_y"]],
        convention="Blender : x vers la droite vue de la façade, y vers le fond, z vers le haut ; origine au centre de l'emprise "
                   "(maison + atelier), au sol. Repère du plan : x_b = x_plan + %.3f, y_b = y_plan + %.3f."
                   % (ORIGINE["plan_x"], ORIGINE["plan_y"]),
        plan=dict(x_bouche=xb, y_bouche=yb, sol=sol, **{k: list(v_) for k, v_ in P.items()}))



# ---------------------------------------------------------------------------------------------- pièces génériques (sorcier, druide, mécano)
def teinte(col):
    v = col.max(1); mn = col.min(1); sat = (v - mn) / np.maximum(v, 1e-4)
    r, g, b = col[:, 0], col[:, 1], col[:, 2]
    dd = np.maximum(v - mn, 1e-5)
    hue = np.where(v == r, ((g - b) / dd) % 6, np.where(v == g, (b - r) / dd + 2, (r - g) / dd + 4)) * 60
    return hue, sat, v


def mesures_maison(co):
    """Plan de façade (mode des y des sommets du mur avant), égout et faîtage (par les étendues des tranches de 25 cm)."""
    x, y, z = co[:, 0], co[:, 1], co[:, 2]
    ymin = float(y.min())
    mur = (z > 1.5) & (z < 3.5) & (y < ymin + 3.5)
    h, bords = np.histogram(y[mur], bins=np.arange(ymin, ymin + 3.5, 0.01))
    ymur = float(bords[h.argmax()] + 0.005)
    zs = np.arange(0.0, z.max(), 0.25)
    larg = np.array([np.ptp(x[(z >= a) & (z < a + 0.25)]) if ((z >= a) & (z < a + 0.25)).sum() > 20 else 0.0 for a in zs])
    prof = np.array([np.ptp(y[(z >= a) & (z < a + 0.25)]) if ((z >= a) & (z < a + 0.25)).sum() > 20 else 0.0 for a in zs])
    mur_l = np.median(larg[(zs > 2.0) & (zs < 3.0)])
    egout = float(zs[(zs > 2.5) & (larg > mur_l + 0.4)][0])
    faitage = float(zs[(prof > 0.6 * prof.max())].max()) + 0.25
    return dict(ymur=ymur, egout=egout, faitage=faitage, xmin=float(x.min()), xmax=float(x.max()), zmax=float(z.max()))


def collision_maison(hd):
    """Marches et débord du soubassement, corps, toit (jusqu'au faîtage) et, pour le sorcier, la tour : enveloppes convexes."""
    co = coords(hd)
    x, y, z = co[:, 0], co[:, 1], co[:, 2]
    m = mesures_maison(co)
    log("mesures : nu du mur y = %.2f, égout z = %.2f, faîtage z = %.2f" % (m["ymur"], m["egout"], m["faitage"]))
    parts = []
    tour = np.zeros(len(co), bool)
    if CFG.get("tour"):
        haut = z > 0.85 * z.max()
        cx, cy = float(x[haut].mean()), float(y[haut].mean())
        d = np.hypot(x - cx, y - cy)
        stem = (z > 6.0) & (z < 0.58 * z.max()) & (d < 2.4)
        rt = float(np.percentile(d[stem], 95))
        tour = (d < rt + 0.1) & (z > 1.0)
        log("tour : axe (%.2f, %.2f), rayon %.2f m" % (cx, cy, rt))
        parts += [("tour_fut", co[tour & (z < 0.62 * z.max())], 48), ("tour_cone", co[tour & (z >= 0.6 * z.max())], 40)]
    marches = (y < m["ymur"] - 0.1) & (z < 1.6) & ~tour
    lanterne = (y < m["ymur"] - 0.05) & (z > 1.6) & (z < m["egout"])
    corps = (z < m["egout"]) & ~marches & ~lanterne & ~tour
    toit = (z >= m["egout"]) & (z <= m["faitage"]) & ~tour
    parts = [("marches", co[marches], 40), ("corps", co[corps], 60), ("toit", co[toit], 70)] + parts
    return joindre_enveloppes(parts)


def detecter_arche(co, col, m):
    """Cadre d'arche en pierre de la façade : retourne (x0, x1, zb, zh) de l'OUVERTURE (le cadre diminué de 23,5 % de sa largeur de
    chaque côté, rapport mesuré sur les cadres de la maison de base et de la forge), au repère de co."""
    hue, sat, v = teinte(col)
    x, y, z = co[:, 0], co[:, 1], co[:, 2]
    ymur = m["ymur"]
    pierre = (sat < 0.17) & (v > 0.28) & (v < 0.7)
    ymin_f = float(y[(z > 1.6) & (z < 4.8)].min())
    zc, gap = CFG.get("cadre", (4.5, 0.45))   # hauteur maximale et écart de coupure (m) pour isoler l'arche
    cadre = pierre & (z > 1.8) & (z < zc) & (y < ymin_f + 1.6) & (x > m["xmin"] + 0.8) & (x < m["xmax"] - 0.8)
    if cadre.sum() < 30:
        raise RuntimeError("cadre d'arche introuvable")
    # amas contigus en x (écart > 0,6 m = autre ensemble de pierres) : le plus fourni est l'arche
    xs = np.sort(x[cadre]); coupures = np.nonzero(np.diff(xs) > gap)[0]
    bornes = [0] + (coupures + 1).tolist() + [len(xs)]
    amas = max(((bornes[i + 1] - bornes[i], xs[bornes[i]], xs[bornes[i + 1] - 1]) for i in range(len(bornes) - 1)))
    xa, xb = float(amas[1]), float(amas[2])
    wf = xb - xa
    ins = CFG.get("inset_arche", 0.235)   # taverne : 0,177 (ouverture de 2,0 m relevée sur la capture de détail)
    x0, x1 = xa + ins * wf, xb - ins * wf
    zb = 1.2
    zh = float(z[cadre].max()) - 0.5
    log("embrasure : cadre de pierre x %.2f à %.2f (largeur %.2f), ouverture x %.2f à %.2f (%.2f), nu du mur y = %.2f"
        % (xa, xb, wf, x0, x1, x1 - x0, ymur))
    return x0, x1, zb, zh


def ancres_maison(hd):
    """Embrasure, lanterne éventuelle et charnière de la porte, détectées sur le maillage (sommets sombres du renfoncement,
    verre orange de la lanterne), au repère final de Blender."""
    co = coords(hd); col = couleur_aux_sommets(hd)
    hue, sat, v = teinte(col)
    m = mesures_maison(co)
    ymur = m["ymur"]
    x, y, z = co[:, 0], co[:, 1], co[:, 2]
    x0, x1, zb, zh = detecter_arche(co, col, m)
    xc = (x0 + x1) / 2
    lan = None
    if CFG.get("lanterne"):
        amb = (sat > 0.5) & (v > 0.5) & (hue > 24) & (hue < 58) & (y < ymur + 0.3) & (z > 1.5) & (z < 5.0)
        if amb.sum() > 5:
            lan = co[amb].mean(0).tolist()
            log("lanterne : %d sommets de verre, centre (%.2f, %.2f, %.2f)" % (amb.sum(), lan[0], lan[1], lan[2]))
    face = [0.0, -1.0, 0.0]
    return dict(
        largeur=LARGEUR, facade_y=ymur,
        entree=dict(centre=[xc, ymur, 0.0], largeur=1.8, haut=2.6),
        lanterne=lan,
        ancres=[dict(nom="Porte_Pivot", pos=[x0, ymur, zb], dir=face)],
        mesures=dict(embrasure_largeur=x1 - x0, embrasure_haut=zh - zb, egout=m["egout"], faitage=m["faitage"], hauteur=m["zmax"]),
        convention="Blender : x vers la droite vue de la façade, y vers le fond, z vers le haut ; origine au centre de l'emprise, au sol.")


# ---------------------------------------------------------------------------------------------- taverne
def masques_taverne(co, col):
    """Parties repérées sur la taverne (repère final) : enseigne (planche), tonneau du toit, souche de cheminée. Retourne un dict
    de masques et de mesures."""
    hue, sat, v = teinte(col)
    x, y, z = co[:, 0], co[:, 1], co[:, 2]
    r, g = col[:, 0], col[:, 1]
    xs = ORIGINE["x_ens"]
    # planche de l'enseigne : sommets clairs (bois, pas le fer) au-delà de la potence, sous les crochets
    planche = (x > xs) & (z < tz(0.168)) & (v > 0.15)   # le bord haut de la planche est à z = 0,167 (unités Tripo) ; au-dessus : crochets
    sign = (x > xs)
    # tonneau : sommets qui dépassent du pan avant du toit (plan ajusté sur les tuiles hors tonneau et hors cheminée)
    tuile = (r > g * 1.8) & (r > 0.25)
    fit = tuile & (y < ty(-0.03)) & (y > ty(-0.19)) & (z > tz(0.26)) & (((x > tx(-0.25)) & (x < tx(0.0))) | ((x > tx(0.22)) & (x < tx(0.26))))
    A = np.c_[np.ones(fit.sum()), y[fit]]
    c, _, _, _ = np.linalg.lstsq(A, z[fit], rcond=None)
    d = z - (c[0] + c[1] * y)
    reg = (x > tx(-0.15)) & (x < tx(0.32)) & (y < ty(0.0)) & (y > ty(-0.2)) & (z > tz(0.25))
    tonneau = reg & (d > 0.02 * ORIGINE["s"] * ORIGINE["ez"])
    pierre = (sat < 0.17) & (v > 0.28) & (v < 0.7)
    souche = pierre & (x < tx(-0.29)) & (x > tx(-0.40)) & (z > tz(0.30))
    return dict(planche=planche, sign=sign, tonneau=tonneau, souche=souche, pente=float(c[1]), pierre=pierre)


def collision_taverne(hd):
    """Corps (soubassement, murs, jusqu'à l'égout), toit (égout au faîtage), marches et rebord du soubassement, souche de la
    cheminée au-dessus du toit, planche de l'enseigne : enveloppes convexes (jamais le maillage rendu). L'auvent de l'arche et le
    tonneau du toit ne comptent pas (hors de portée, et l'auvent barrerait l'accès à la porte)."""
    co = coords(hd); col = couleur_aux_sommets(hd)
    x, y, z = co[:, 0], co[:, 1], co[:, 2]
    k = masques_taverne(co, col)
    m = mesures_maison(co[~k["sign"]])
    log("mesures : nu du mur y = %.2f, égout z = %.2f, faîtage z = %.2f" % (m["ymur"], m["egout"], m["faitage"]))
    corps_x = ~k["sign"]
    marches = corps_x & (y < m["ymur"] - 0.1) & (z < 1.6)
    corps = corps_x & (z < m["egout"]) & ~marches & (y > m["ymur"] - 0.2)
    toit = corps_x & (z >= m["egout"]) & (z <= m["faitage"]) & ~k["souche"] & ~k["tonneau"]
    souche_h = k["souche"] & (z >= m["egout"] - 0.5)
    parts = [("marches", co[marches], 40), ("corps", co[corps], 60), ("toit", co[toit], 70), ("souche", co[souche_h], 30),
             ("enseigne", co[k["planche"]], 20)]
    return joindre_enveloppes(parts)


def ancres_taverne(hd):
    """Embrasure (porte étroite), plan de façade, enseigne (planche), tonneau du toit et cheminée, détectés sur le maillage ;
    au repère final de Blender."""
    co = coords(hd); col = couleur_aux_sommets(hd)
    hue, sat, v = teinte(col)
    x, y, z = co[:, 0], co[:, 1], co[:, 2]
    k = masques_taverne(co, col)
    m = mesures_maison(co[~k["sign"]])
    ymur = m["ymur"]
    # embrasure : cadre de pierre dans la fenêtre d'abscisses arche_x_tripo (ni cheminée ni soubassement)
    ax0, ax1 = (tx(a) for a in CFG["arche_x_tripo"])
    sel = (x > ax0 - 0.8) & (x < ax1 + 0.8)
    x0, x1, zb, zh = detecter_arche(co[sel], col[sel], dict(ymur=ymur, xmin=ax0 - 0.8, xmax=ax1 + 0.8))
    xc = (x0 + x1) / 2
    sombre = (v < 0.12) & (x > x0) & (x < x1) & (y < ymur + 0.6) & (z > 0.5) & (z < zh)
    if sombre.sum() > 20:
        zb = float(np.percentile(z[sombre], 3))
    log("porte : ouverture x %.2f à %.2f (largeur %.2f), seuil z = %.2f, haut z = %.2f" % (x0, x1, x1 - x0, zb, zh))
    # enseigne
    p = k["planche"]
    if p.sum() < 50:
        raise RuntimeError("planche de l'enseigne introuvable")
    px0, px1 = float(np.percentile(x[p], 0.5)), float(np.percentile(x[p], 99.5))
    pz0, pz1 = float(np.percentile(z[p], 0.5)), float(np.percentile(z[p], 99.5))
    yf = float(y[p].min()); yb = float(y[p].max())
    ens = dict(centre=[(px0 + px1) / 2, yf, (pz0 + pz1) / 2], largeur=px1 - px0, haut=pz1 - pz0, epaisseur=yb - yf)
    log("enseigne : planche x %.2f à %.2f (%.2f m), z %.2f à %.2f (%.2f m), face avant y = %.2f, épaisseur %.2f m"
        % (px0, px1, px1 - px0, pz0, pz1, pz1 - pz0, yf, yb - yf))
    # tonneau
    t = k["tonneau"]
    if t.sum() < 200:
        raise RuntimeError("tonneau du toit introuvable")
    zt = z[t]
    bas = t & (z < zt.min() + 0.2 * np.ptp(zt))
    ton = co[t].mean(0)
    sortie = co[bas].mean(0)
    pente = k["pente"]   # dz/dy du pan avant ; la bière descend vers -y
    n = math.hypot(1.0, pente)
    log("tonneau : %d sommets, centre (%.2f, %.2f, %.2f), x %.2f à %.2f, z %.2f à %.2f ; pente du pan avant %.2f (%.0f degrés)"
        % (t.sum(), ton[0], ton[1], ton[2], co[t, 0].min(), co[t, 0].max(), zt.min(), zt.max(), pente, math.degrees(math.atan(pente))))
    # cheminée : sommet de la souche
    so = k["souche"]
    zh_c = float(z[so].max())
    haut = so & (z > zh_c - 0.6)
    ch = [float(x[haut].mean()), float(y[haut].mean()), zh_c]
    log("cheminée : sommet (%.2f, %.2f, %.2f), %.1f x %.1f m en haut" % (ch[0], ch[1], ch[2], np.ptp(x[haut]), np.ptp(y[haut])))
    face = [0.0, -1.0, 0.0]
    return dict(
        largeur=LARGEUR, facade_y=ymur,
        entree=dict(centre=[xc, ymur, 0.0], largeur=1.8, haut=2.6),
        lanterne=None,
        ancres=[dict(nom="Porte_Pivot", pos=[x0, ymur, zb], dir=face),
                dict(nom="Enseigne", pos=ens["centre"], dir=face),
                dict(nom="Tonneau_Fuite", pos=[float(sortie[0]), float(sortie[1]), float(sortie[2])], dir=[0.0, -1.0 / n, -pente / n]),
                dict(nom="Cheminee", pos=ch, dir=[0.0, 0.0, 0.0])],
        enseigne=ens,
        mesures=dict(embrasure_largeur=x1 - x0, embrasure_haut=zh - zb, egout=m["egout"], faitage=m["faitage"],
                     hauteur=float(z.max()), tonneau_centre=[float(a) for a in ton], pente_toit=pente),
        convention="Blender : x vers la droite vue de la façade, y vers le fond, z vers le haut ; origine au centre de l'emprise "
                   "du soubassement (sans l'enseigne), au sol.")


# ---------------------------------------------------------------------------------------------- rendus
DONNEES_RENDU = {}   # ancres calculées (vues de détail de la taverne)


def rendus(dossier, objs, mat_img=None):
    os.makedirs(dossier, exist_ok=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "STUDIO"
    sc.display.shading.color_type = "TEXTURE"
    sc.display.shading.show_object_outline = False
    sc.render.resolution_x, sc.render.resolution_y = 1280, 960
    sc.world = bpy.data.worlds.new("w"); sc.world.color = (0.17, 0.17, 0.18)
    cam = bpy.data.cameras.new("Cam"); co = bpy.data.objects.new("Cam", cam); bpy.context.collection.objects.link(co)
    sc.camera = co
    cam.type = "ORTHO"
    for o in bpy.data.objects:
        if o.type == "MESH":
            o.hide_render = o not in objs
    if CFG.get("type") == "forge":
        centre = Vector((0, 0, 6.0)); o = 24.0
        vues = (("face", 0, 8, o, centre), ("dos", 180, 8, o, centre), ("profil", 90, 8, o, centre),
                ("trois_quarts", 35, 28, o, centre), ("dessus", 0, 89.9, 22.0, Vector((0, 0, 0))),
                ("atelier", 20, 35, 13.0, Vector((4.0, 2.0, 2.0))))
    elif CFG.get("type") == "taverne":
        co_r = coords(objs[0]); zm = float(co_r[:, 2].max())
        centre = Vector((1.0, 0, zm * 0.42)); o = max(zm * 1.45, LARGEUR * 1.3)
        vues = [("face", 0, 8, o, centre), ("dos", 180, 8, o, centre), ("profil", 90, 8, o, centre),
                ("trois_quarts", 35, 28, o, centre), ("dessus", 0, 89.9, o, Vector((0, 0, 0)))]
        if DONNEES_RENDU:
            e = DONNEES_RENDU["enseigne"]["centre"]; tc = DONNEES_RENDU["mesures"]["tonneau_centre"]
            vues += [("enseigne", 12, 8, 7.0, Vector((e[0], e[1], e[2]))), ("tonneau", 20, 22, 9.0, Vector(tc)),
                     ("porte", 12, 12, 9.0, Vector((tx(0.10), -4.0, 2.4)))]
        vues = tuple(vues)
    elif CFG["nom"] != "Maison_Base":
        co_r = coords(objs[0]); zm = float(co_r[:, 2].max())
        centre = Vector((0, 0, zm * 0.46)); o = max(zm * 1.45, 16.0)
        vues = (("face", 0, 8, o, centre), ("dos", 180, 8, o, centre), ("profil", 90, 8, o, centre),
                ("trois_quarts", 35, 28, o, centre), ("dessus", 0, 89.9, 18.0, Vector((0, 0, 0))),
                ("arche", 15, 12, 7.0, Vector((float(co_r[:, 0].min()) * 0.5, float(co_r[:, 1].min()), 2.2))))
    else:
        centre = Vector((0, 0, 4.0))
        vues = (("face", 0, 8, 12.5, centre), ("dos", 180, 8, 12.5, centre), ("profil", 90, 8, 12.5, centre),
                ("trois_quarts", 35, 28, 12.5, centre), ("arche", 12, 10, 4.5, Vector((-0.96, -3.0, 1.9))),
                ("toit_dos", 180, 30, 5.0, Vector((-3.0, 1.0, 6.8))))
    for nom, lacet, tangage, ortho, c in vues:
        a = math.radians(lacet); t = math.radians(tangage)
        off = Vector((math.sin(a) * math.cos(t), -math.cos(a) * math.cos(t), math.sin(t))) * 40.0
        co.location = c + off
        co.rotation_euler = (-off).normalized().to_track_quat("-Z", "Y").to_euler()
        cam.ortho_scale = ortho
        sc.render.filepath = os.path.join(dossier, "maison_" + nom + ".png")
        bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(co)


# ---------------------------------------------------------------------------------------------- export
def exporter(objs, chemin):
    selectionner(objs)
    bpy.ops.export_scene.fbx(
        filepath=chemin, use_selection=True, object_types={"MESH"}, apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", use_mesh_modifiers=False,
        mesh_smooth_type="FACE", use_tspace=False, add_leaf_bones=False, bake_anim=False, path_mode="AUTO",
        embed_textures=False)
    log("export", os.path.relpath(chemin, RACINE))


def main():
    hd = importer()
    if CFG.get("type") == "forge":
        diag_forge(hd)
        if "--diag" in ARGS:
            return
    elif CFG["nom"] == "Maison_Base":
        reparer_toit(hd)
    rendu = decimer(hd, BUDGET, NOM)
    sans_dessous(rendu)
    mp.lisser(rendu)
    col = collision(hd)
    if CFG.get("type") == "forge":
        verifier_zones(col)
    try:
        dat = ancres(hd)
    except Exception as e:   # essais d'échelle (--sans-export) : les mesures des ancres supposent l'échelle retenue
        if "--sans-export" not in ARGS:
            raise
        log("ancres non calculées (essai d'échelle) :", e)
        dat = {}
    img0 = cuire(hd, rendu)
    px1, cls, utile = quantifier(img0)
    img1 = image_depuis(NOM + "_Texture_P1", px1)
    emi = image_depuis(NOM + "_Emission", carte_emission(cls, utile))
    if "--rendus" in ARGS:
        DONNEES_RENDU.update(dat)
        rendus(ARGS[ARGS.index("--rendus") + 1], [rendu])
    bpy.data.objects.remove(hd)
    ntri = triangles(rendu)
    if "--sans-export" not in ARGS:
        os.makedirs(SORTIE, exist_ok=True)
        retenue, autre = (img1, img0) if PALETTE_RETENUE == 1 else (img0, img1)
        enregistrer(retenue, os.path.join(SORTIE, NOM + "_Texture.png"))
        enregistrer(autre, os.path.join(SORTIE, NOM + "_Texture_Variante.png"))
        enregistrer(emi, os.path.join(SORTIE, NOM + "_Emission.png"))
        with open(os.path.join(SORTIE, NOM + "_Ancres.json"), "w", encoding="utf-8") as fh:
            json.dump(dat, fh, indent=1)
        exporter([rendu, col], os.path.join(SORTIE, NOM + ".fbx"))
    log("terminé : rendu %d triangles, collision %d triangles, atlas %d², 1 matériau, palette %d retenue"
        % (ntri, triangles(col), TAILLE, PALETTE_RETENUE))


if __name__ == "__main__":
    main()
