# -*- coding: utf-8 -*-
"""
Montagne du nord, flancs ouest et est : chaîne reproductible des deux modèles Tripo de flanc vers Unity.
Réutilise les fonctions de montagne_pipeline.py (import, décimation, lissage, atlas cuit, export) ; la pièce héros
reste produite par montagne_pipeline.py seul.

Intégration FIDÈLE (02/10/2026, retour de Quentin : « ce ne sont pas les modèles que je t'ai donnés ») : la pièce
Tripo entière, telle qu'elle a été choisie : ni miroir, ni recadrage, ni enfouissement, ni étirement non uniforme.
Seule l'échelle (uniforme, --echelle) et le lacet côté Unity (VillageV5Builder.V5FlancsPose) sont réglables.

Lancement (Blender 5.2, sans interface) :
    blender.exe -b --python ArtSources/Decor/Montagne/montagne_flancs_pipeline.py -- --piece est|ouest [options]
Options :
    --piece est|ouest   flanc à produire (obligatoire)
    --echelle <m>       mètres par unité Tripo, uniforme (défaut 62 : la pièce fait ~61 m de large ; la pièce héros est à
                        125 m par unité, mais 61 m x 2 ne tiennent pas entre la pièce héros et la limite du sol)
    --budget <n>        triangles du maillage rendu (défaut 28000, le budget demandé est de 25 000 à 30 000)
    --collision <n>     triangles du maillage de collision (défaut 2800)
    --texture <n>       côté de l'atlas (défaut 2048)
    --angle <deg>       angle de lissage des normales (défaut 50, comme la pièce héros)
    --rendus <dossier>  rendus de vérification Workbench (dessus, face, dos, trois-quarts)
    --sans-export       ne pas écrire le FBX ni la texture (essais)

Entrées (Tripo, 02/10/2026, un seul maillage chacune, face vers -Y, x de -0,49 à +0,49 unité) :
    ouest : ArtSources/References/Decor/montagne_droite_tripo/rock+formation+3d+model.fbx (96 869 triangles,
            0,98 x 0,30 x 0,32 unité ; mur de colonnes arrondies, deux grands pics sur la DROITE de la pièce)
    est   : ArtSources/References/Decor/montagne_gauche_tripo/low-poly+rock+model.fbx (96 488 triangles,
            0,98 x 0,46 x 0,35 unité ; point haut à GAUCHE de la pièce, gros rocher en surplomb tout à gauche,
            la pente descend vers la droite)
ÉCHANGE du 02/10/2026 (retour de Quentin : « on ne voit pas assez le pic », « inverse gauche et droite comme Tripo ») : les
deux pièces ont changé de côté, toujours SANS miroir. La pièce « droite » de Tripo (deux pics à droite) est posée à l'OUEST :
ses pics sont sur sa droite, donc vers le centre du village, bien visibles depuis la place de Nyxessa ; la pièce « gauche »
(point haut et rocher en surplomb à gauche) est posée à l'EST : son point haut est sur sa gauche, donc vers le centre.
Les noms de sortie suivent le côté : Montagne_Flanc_Ouest = pièce droite de Tripo, Montagne_Flanc_Est = pièce gauche.

Traitement (par flanc) :
 1. Échelle uniforme (s m par unité Tripo), origine au centre de l'emprise, au sol (le point le plus bas à z = 0).
 2. Décimation au budget, lissage, atlas cuit (Smart UV Project + Cycles), collision décimée, export FBX (deux objets :
    <nom> et <nom>_Collision) et texture PNG dans Assets/Art/Decor/Montagne/.
Côté Unity : VillageV5Builder (V5Montagne, V5FlancsPose, V5FlancsEchelle) pose les flancs.
"""
import bpy, math, os, sys

ICI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, ICI)
import montagne_pipeline as mp   # lit sys.argv ; ne lance rien

ARGS = mp.ARGS
RACINE = mp.RACINE
REF = os.path.join(RACINE, "ArtSources", "References", "Decor")
log = lambda *a: mp.log(*a)

ECHELLE_DEFAUT = 62.0   # m par unité Tripo (uniforme)

PIECES = {
    "ouest": dict(source=os.path.join(REF, "montagne_droite_tripo", "rock+formation+3d+model.fbx"), nom="Montagne_Flanc_Ouest"),
    "est": dict(source=os.path.join(REF, "montagne_gauche_tripo", "low-poly+rock+model.fbx"), nom="Montagne_Flanc_Est"),
}


def option(nom, defaut, conv=str):
    if nom in ARGS:
        return conv(ARGS[ARGS.index(nom) + 1])
    return defaut


PIECE = option("--piece", None)
if PIECE not in PIECES:
    raise SystemExit("--piece est|ouest obligatoire")
CFG = PIECES[PIECE]
BUDGET = option("--budget", 28000, int)
COLLISION = option("--collision", 2800, int)
ECHELLE = option("--echelle", ECHELLE_DEFAUT, float)
SORTIE = mp.SORTIE


def preparer(o):
    """Échelle déjà appliquée par mp.importer() (uniforme) ; rien d'autre : pas de miroir, de coupe ni d'enfouissement."""
    me = o.data
    xs = [v.co.x for v in me.vertices]; ys = [v.co.y for v in me.vertices]; zs = [v.co.z for v in me.vertices]
    log("pièce entière : emprise %.1f x %.1f m (x, y), hauteur %.1f m, échelle uniforme %.1f m par unité Tripo ; %d triangles HD"
        % (max(xs) - min(xs), max(ys) - min(ys), max(zs), ECHELLE, mp.triangles(o)))
    return dict(xmin=min(xs), xmax=max(xs), ymin=min(ys), ymax=max(ys), zmax=max(zs))


def rendus(dossier, o, bb):
    os.makedirs(dossier, exist_ok=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "STUDIO"
    sc.display.shading.color_type = "TEXTURE"
    sc.render.resolution_x, sc.render.resolution_y = 1800, 800
    sc.world = bpy.data.worlds.new("w"); sc.world.color = (0.17, 0.17, 0.18)
    cam = bpy.data.cameras.new("Cam"); co = bpy.data.objects.new("Cam", cam); bpy.context.collection.objects.link(co)
    sc.camera = co
    cam.type = "ORTHO"; cam.ortho_scale = 110
    vues = (("dessus", (0, 0, 200), (0, 0, 0), 110), ("face", (0, -200, 12), (90, 0, 0), 110),
            ("dos", (0, 200, 12), (90, 0, 180), 110))
    for nom, loc, rot, ortho in vues:
        co.location = loc; co.rotation_euler = tuple(math.radians(a) for a in rot); cam.ortho_scale = ortho
        sc.render.filepath = os.path.join(dossier, "flanc_%s_%s.png" % (PIECE, nom))
        bpy.ops.render.render(write_still=True)
    cam.type = "PERSP"; cam.lens = 28
    co.location = (-45, -110, 45); co.rotation_euler = tuple(math.radians(a) for a in (66, 0, -22))
    sc.render.filepath = os.path.join(dossier, "flanc_%s_trois_quarts.png" % PIECE)
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(co)


def main():
    mp.SOURCE = CFG["source"]
    mp.ECHELLE = (ECHELLE, ECHELLE, ECHELLE)
    mp.NOM = CFG["nom"]
    mp.TAILLE_TEXTURE = option("--texture", 2048, int)
    mp.ANGLE_LISSAGE = math.radians(option("--angle", 50.0, float))
    fbx = os.path.join(SORTIE, CFG["nom"] + ".fbx")
    png = os.path.join(SORTIE, CFG["nom"] + "_Texture.png")
    mp.FBX = fbx
    hd = mp.importer()
    bb = preparer(hd)
    if "--rendus-hd" in ARGS:
        rendus(ARGS[ARGS.index("--rendus-hd") + 1], hd, bb)
    rendu = mp.decimer(hd, BUDGET, CFG["nom"])
    mp.lisser(rendu)
    collision = mp.decimer(hd, COLLISION, CFG["nom"] + "_Collision")
    collision.data.materials.clear()
    img = mp.cuire(hd, rendu)
    if "--rendus" in ARGS:
        rendus(ARGS[ARGS.index("--rendus") + 1], rendu, bb)
    bpy.data.objects.remove(hd)
    if "--sans-export" not in ARGS:
        os.makedirs(SORTIE, exist_ok=True)
        img.filepath_raw = png
        img.file_format = "PNG"
        img.save()
        log("texture", os.path.relpath(png, RACINE))
        mp.exporter([rendu, collision])
    log("terminé (%s) : rendu %d triangles, collision %d triangles, emprise x %.1f..%.1f, y %.1f..%.1f, hauteur %.1f"
        % (PIECE, mp.triangles(rendu), mp.triangles(collision), bb["xmin"], bb["xmax"], bb["ymin"], bb["ymax"], bb["zmax"]))


main()
