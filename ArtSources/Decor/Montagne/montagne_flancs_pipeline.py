# -*- coding: utf-8 -*-
"""
Montagne du nord, flancs ouest et est : chaîne reproductible des deux modèles Tripo de flanc vers Unity.
Réutilise les fonctions de montagne_pipeline.py (import, décimation, lissage, atlas cuit, export) ; la pièce héros
reste produite par montagne_pipeline.py seul.

Lancement (Blender 5.2, sans interface) :
    blender.exe -b --python ArtSources/Decor/Montagne/montagne_flancs_pipeline.py -- --piece est|ouest [options]
Options :
    --piece est|ouest   flanc à produire (obligatoire)
    --budget <n>        triangles du maillage rendu (défaut 25000, le budget demandé est de 20 000 à 30 000)
    --collision <n>     triangles du maillage de collision (défaut 2500)
    --texture <n>       côté de l'atlas (défaut 2048)
    --angle <deg>       angle de lissage des normales (défaut 50, comme la pièce héros)
    --rendus <dossier>  rendus de vérification Workbench (dessus, face, dos, trois-quarts)
    --sans-export       ne pas écrire le FBX ni la texture (essais)

Entrées (Tripo, 02/10/2026, un seul maillage chacune, face vers -Y) :
    ouest : ArtSources/References/Decor/montagne_gauche_tripo/low-poly+rock+model.fbx (96 488 triangles,
            0,98 × 0,46 × 0,36 unité ; point haut du côté gauche de la pièce)
    est   : ArtSources/References/Decor/montagne_droite_tripo/rock+formation+3d+model.fbx (96 869 triangles,
            0,98 × 0,30 × 0,32 unité ; pic plus haut sur la droite)

Traitement (par flanc) :
 1. Échelle d'après la boîte englobante (1 unité Tripo = ~115 m en largeur et profondeur : les blocs ont à peu près
    la taille de ceux de la pièce héros, 125 m par unité ; 85 m par unité en hauteur pour l'ouest, 92 pour l'est :
    le flanc est plus bas que la pièce héros, ~22 m au plus haut, et descend vers l'extérieur).
 2. Miroir en x : les deux modèles ont leur point haut côté extérieur, il faut le point haut vers le centre de la
    falaise, le bas vers l'extrémité (normales retournées avec les faces).
 3. Coupe côté centre (x) et côté nord (y, partie cachée par la falaise procédurale) ; trous rebouchés.
 4. Rampe d'enfouissement côté centre : le flanc sort du sol et monte jusqu'à pleine hauteur sur RAMPE m, pour se
    fondre derrière la fin de la pièce héros (pas de coupe visible). Ce qui passe sous le sol (z < -0,6 m) est supprimé.
 5. Décimation au budget, lissage, atlas cuit (Smart UV Project + Cycles), collision décimée, export FBX (deux objets :
    <nom> et <nom>_Collision) et texture PNG dans Assets/Art/Decor/Montagne/ (origine : centre de l'emprise, au sol).
Côté Unity : VillageV5Builder (V5Montagne, V5FlancsPose) pose les flancs.
"""
import bpy, bmesh, math, os, sys
from mathutils import Vector

ICI = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, ICI)
import montagne_pipeline as mp   # lit sys.argv ; ne lance rien

ARGS = mp.ARGS
RACINE = mp.RACINE
REF = os.path.join(RACINE, "ArtSources", "References", "Decor")
log = lambda *a: mp.log(*a)

RAMPE = 26.0        # m : longueur de la rampe d'enfouissement côté centre
SOUS_SOL = -0.6     # m : tout ce qui est plus bas est supprimé (le sol de la carte est à 0)

PIECES = {
    "ouest": dict(
        source=os.path.join(REF, "montagne_gauche_tripo", "low-poly+rock+model.fbx"),
        nom="Montagne_Flanc_Ouest", echelle=(115.0, 115.0, 85.0),
        cote=-1,           # le centre de la falaise est du côté +x : l'extérieur est du côté -x
        garde=-0.10,       # on garde la part de la pièce d'origine à x >= garde (unités Tripo) : le bas, à droite
        dos=12.0),         # m : on coupe tout ce qui est derrière y = dos (après centrage de la pièce Tripo)
    "est": dict(
        source=os.path.join(REF, "montagne_droite_tripo", "rock+formation+3d+model.fbx"),
        nom="Montagne_Flanc_Est", echelle=(115.0, 115.0, 92.0),
        cote=+1,           # le centre est du côté -x : l'extérieur est du côté +x
        garde=0.12,        # on garde la part d'origine à x <= garde : à gauche, avant le pic
        dos=15.0),
}


def option(nom, defaut, conv=str):
    if nom in ARGS:
        return conv(ARGS[ARGS.index(nom) + 1])
    return defaut


PIECE = option("--piece", None)
if PIECE not in PIECES:
    raise SystemExit("--piece est|ouest obligatoire")
CFG = PIECES[PIECE]
BUDGET = option("--budget", 25000, int)
COLLISION = option("--collision", 2500, int)
SORTIE = mp.SORTIE


def couper(me, co, no, supprimer_devant=True):
    """Supprime la géométrie du côté de la normale `no` du plan (co, no), rebouche les trous en triangles."""
    bm = bmesh.new(); bm.from_mesh(me)
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no, dist=1e-5,
                           clear_outer=supprimer_devant, clear_inner=not supprimer_devant)
    bords = [e for e in bm.edges if e.is_boundary]
    if bords:
        res = bmesh.ops.holes_fill(bm, edges=bords, sides=1000000)
        bmesh.ops.triangulate(bm, faces=res["faces"], quad_method="BEAUTY", ngon_method="BEAUTY")
    bm.to_mesh(me); bm.free()


def preparer(o):
    """Échelle déjà appliquée par mp.importer() : miroir, coupes, rampe d'enfouissement, recentrage."""
    me = o.data
    sx = CFG["echelle"][0]; cote = CFG["cote"]
    # miroir en x (les deux pièces ont leur point haut côté extérieur) ; les faces suivent
    me.transform(__import__("mathutils").Matrix.Scale(-1.0, 4, Vector((1, 0, 0))))
    me.flip_normals()
    # coupe côté centre : x' = -x_origine
    xc = -CFG["garde"] * sx
    couper(me, Vector((xc, 0, 0)), Vector((-cote, 0, 0)))
    # coupe côté nord
    couper(me, Vector((0, CFG["dos"], 0)), Vector((0, 1, 0)))
    # rampe d'enfouissement
    zmax = max(v.co.z for v in me.vertices)
    D = zmax + 2.0
    for v in me.vertices:
        u = (v.co.x - xc) * cote
        t = min(1.0, max(0.0, u / RAMPE))
        v.co.z -= D * (1.0 - t) ** 1.6
    me.update()
    # ce qui est sous le sol est supprimé
    couper(me, Vector((0, 0, SOUS_SOL)), Vector((0, 0, -1)))
    xs = [v.co.x for v in me.vertices]; ys = [v.co.y for v in me.vertices]; zs = [v.co.z for v in me.vertices]
    cx = (min(xs) + max(xs)) / 2; cy = (min(ys) + max(ys)) / 2
    me.transform(__import__("mathutils").Matrix.Translation((-cx, -cy, 0.0)))
    me.update()
    sens = "le centre de la falaise est à x = %+.1f (repère du flanc), l'extrémité à x = %+.1f" % (
        (xc - cx), ((max(xs) if cote > 0 else min(xs)) - cx))
    log("préparation : emprise %.1f × %.1f m, hauteur %.1f m, centre déplacé de (%.1f, %.1f) ; %s ; %d triangles HD"
        % (max(xs) - min(xs), max(ys) - min(ys), max(zs), cx, cy, sens, mp.triangles(o)))
    return dict(xmin=min(xs) - cx, xmax=max(xs) - cx, ymin=min(ys) - cy, ymax=max(ys) - cy, zmax=max(zs))


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
    co.location = (-60 * CFG["cote"], -110, 45); co.rotation_euler = tuple(math.radians(a) for a in (66, 0, -28 * CFG["cote"]))
    sc.render.filepath = os.path.join(dossier, "flanc_%s_trois_quarts.png" % PIECE)
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(co)


def main():
    mp.SOURCE = CFG["source"]
    mp.ECHELLE = CFG["echelle"]
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
