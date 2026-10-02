# -*- coding: utf-8 -*-
"""
Montagne du nord, pièce héros : chaîne reproductible du modèle Tripo (grotte du portail, ravine de la cascade) vers Unity.
Script Blender sans interface, sur le modèle de ArtSources/Personnages/Bavaroise/bavaroise_v4_pipeline.py.

Lancement (Blender 5.2) :
    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --python ArtSources/Decor/Montagne/montagne_pipeline.py -- [options]
Options :
    --budget <n>      triangles du maillage rendu (défaut 40000 ; Tripo : 280 752)
    --collision <n>   triangles du maillage de collision (défaut 3000)
    --texture <n>     côté de l'atlas (défaut 2048)
    --angle <deg>     angle de lissage des normales (défaut 50 : arêtes plus vives marquées dures)
    --rendus <dossier>  rendus de vérification Workbench (face, dessus, trois-quarts)
    --sans-export     ne pas écrire le FBX ni la texture (essais)

Entrée : ArtSources/References/Decor/montagne_heros_tripo/low-poly+rock+cave+3d+model.fbx (Tripo, 01/10/2026) : un seul
maillage de 140 376 sommets, 280 752 triangles, une texture basecolor 4096², 0,98 × 0,35 × 0,19 unité (largeur,
profondeur, hauteur), face vers -Y. La grotte est à gauche (x de -0,145 à -0,095 unité, profonde de 0,09), la ravine
de la cascade au centre (x de -0,03 à +0,02).

Traitement :
 1. Échelle d'après la boîte englobante (Tripo n'a pas d'échelle métrique) : ECHELLE = (125, 115, 110) ; la pièce fait
    alors 122 × 40 × 20,5 m (brief : 80 à 100 m de large, 15 à 20 m de haut ; un peu plus large pour que la grotte
    ait 6,3 m d'ouverture et 7 m de haut). Origine au centre du dessous ; face avant vers -Y (le sud du village une fois
    dans Unity, voir VillageV5Builder).
 2. Décimation (collapse) au budget, UV Tripo conservées ; faces lisses, arêtes dures au-delà de --angle, normales
    pondérées par l'aire (Weighted Normal).
 3. Atlas unique --texture² : nouveau dépliage (Smart UV Project ; la décimation déchire les petites îles UV de Tripo),
    puis couleur cuite (Cycles, Selected to Active) depuis le Tripo haute définition et sa texture 4K.
 4. Collision : copie décimée à --collision triangles (maillage concave immobile côté Unity, jamais le maillage rendu).
 5. Export FBX (deux objets : Montagne_Heros, Montagne_Heros_Collision) et texture PNG dans Assets/Art/Decor/Montagne/.
Côté Unity : menu Deathless > Village > v5 (Assets/Editor/VillageV5Builder.cs) pose la pièce, la grotte et le portail.
"""
import bpy, bmesh, math, os, sys
from mathutils import Vector

RACINE = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
SOURCE = os.path.join(RACINE, "ArtSources", "References", "Decor", "montagne_heros_tripo", "low-poly+rock+cave+3d+model.fbx")
SORTIE = os.path.join(RACINE, "Assets", "Art", "Decor", "Montagne")
FBX = os.path.join(SORTIE, "Montagne_Heros.fbx")
TEXTURE_PNG = os.path.join(SORTIE, "Montagne_Heros_Texture.png")

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def arg(nom, defaut, conv=float):
    if nom in ARGS:
        return conv(ARGS[ARGS.index(nom) + 1])
    return defaut


BUDGET = arg("--budget", 40000, int)
BUDGET_COLLISION = arg("--collision", 3000, int)
TAILLE_TEXTURE = arg("--texture", 2048, int)
ANGLE_LISSAGE = math.radians(arg("--angle", 50.0))
ECHELLE = (125.0, 115.0, 110.0)
NOM = "Montagne_Heros"   # nom de la pièce (objets, maillage, matériau, texture) ; montagne_flancs_pipeline.py le change


def log(*a):
    print("[montagne]", *a)
    sys.stdout.flush()


def triangles(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)


def selectionner(objs, actif=None):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = actif or objs[0]


def remplacer_maillage(o, me):
    vieux = o.data
    o.data = me
    if vieux.users == 0:
        bpy.data.meshes.remove(vieux)


# ---------------------------------------------------------------------------------------------- 1. import et échelle
def importer():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=SOURCE)
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
    me.transform(__import__("mathutils").Matrix.Diagonal((ECHELLE[0], ECHELLE[1], ECHELLE[2], 1.0)))
    xs = [v.co.x for v in me.vertices]; ys = [v.co.y for v in me.vertices]; zs = [v.co.z for v in me.vertices]
    centre = Vector(((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, min(zs)))
    me.transform(__import__("mathutils").Matrix.Translation(-centre))
    me.update()
    o.name = "HD_Tripo"
    log("import : %d triangles, %.1f × %.1f × %.1f m (largeur, profondeur, hauteur), centre Tripo déplacé de %s"
        % (triangles(o), max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs), tuple(round(c, 2) for c in centre)))
    return o


# ---------------------------------------------------------------------------------------------- 2. décimation, normales
def decimer(src, budget, nom):
    o = src.copy(); o.data = src.data.copy(); bpy.context.collection.objects.link(o)
    o.name = nom; o.data.name = nom
    dg = bpy.context.evaluated_depsgraph_get()
    avant = triangles(o)
    mod = o.modifiers.new("Decimation", "DECIMATE")
    mod.decimate_type = "COLLAPSE"
    mod.ratio = min(1.0, budget / avant)
    mod.use_collapse_triangulate = True
    dg.update()
    me = bpy.data.meshes.new_from_object(o.evaluated_get(dg))
    o.modifiers.clear()
    remplacer_maillage(o, me)
    me.name = nom
    log("décimation %-26s %6d -> %6d triangles" % (nom, avant, triangles(o)))
    return o


def lisser(o):
    me = o.data
    me.shade_smooth()
    bm = bmesh.new(); bm.from_mesh(me)
    dures = 0
    for e in bm.edges:
        if len(e.link_faces) == 2 and e.calc_face_angle(0.0) > ANGLE_LISSAGE:
            e.smooth = False; dures += 1
        else:
            e.smooth = True
    bm.to_mesh(me); bm.free()
    selectionner([o])
    mod = o.modifiers.new("Normales", "WEIGHTED_NORMAL")
    mod.keep_sharp = True
    mod.weight = 50
    bpy.ops.object.modifier_apply(modifier=mod.name)
    log("lissage : %d arêtes dures (> %.0f°)" % (dures, math.degrees(ANGLE_LISSAGE)))


# ---------------------------------------------------------------------------------------------- 3. atlas
def image_source(o):
    for m in o.data.materials:
        if m and m.use_nodes:
            for n in m.node_tree.nodes:
                if n.type == "TEX_IMAGE" and n.image:
                    return n.image
    return None


def cuire(src, cible):
    T = TAILLE_TEXTURE
    if "--sans-cuisson" in ARGS:
        # texture Tripo réduite, UV Tripo gardées par la décimation
        img = image_source(src).copy(); img.name = NOM + "_Texture"; img.scale(T, T)
        mat = bpy.data.materials.new(NOM); mat.use_nodes = True
        noeud = mat.node_tree.nodes.new("ShaderNodeTexImage"); noeud.image = img
        mat.node_tree.links.new(noeud.outputs["Color"], mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
        cible.data.materials.clear(); cible.data.materials.append(mat)
        log("texture Tripo réduite à %d² (sans cuisson)" % T)
        return img
    if image_source(src) is None:
        raise RuntimeError("texture Tripo introuvable")
    # La décimation déchire les petites îles UV de Tripo (taches claires et sombres) : nouveau dépliage de la pièce
    # décimée, puis cuisson de la couleur Tripo dessus.
    me = cible.data
    while len(me.uv_layers) > 0:
        me.uv_layers.remove(me.uv_layers[0])
    me.uv_layers.new(name="UVMap")
    selectionner([cible])
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.002, area_weight=0.0, scale_to_bounds=True)
    bpy.ops.object.mode_set(mode="OBJECT")
    img = bpy.data.images.new(NOM + "_Texture", T, T, alpha=False)
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
    bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"}, use_selected_to_active=True, cage_extrusion=0.25,
                        max_ray_distance=1.5, margin=16, margin_type="EXTEND", use_clear=True, target="IMAGE_TEXTURES")
    # relie la texture au BSDF pour l'export (Unity crée le matériau ; VillageV5Builder le remplace par le sien)
    if bsdf is not None:
        mat.node_tree.links.new(noeud.outputs["Color"], bsdf.inputs["Base Color"])
    log("atlas cuit %d²" % T)
    return img


# ---------------------------------------------------------------------------------------------- rendus
def rendus(dossier, o):
    os.makedirs(dossier, exist_ok=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "FLAT" if "--plat" in ARGS else "STUDIO"
    sc.display.shading.color_type = "TEXTURE"
    sc.render.resolution_x, sc.render.resolution_y = 1600, 800
    cam = bpy.data.cameras.new("Cam"); co = bpy.data.objects.new("Cam", cam); bpy.context.collection.objects.link(co)
    sc.camera = co
    for nom, loc, rot, ortho in (("face", (0, -200, 12), (90, 0, 0), 128), ("dessus", (0, 0, 200), (0, 0, 0), 128),
                                 ("trois_quarts", (-60, -110, 60), (62, 0, -28), None),
                                 ("grotte", (-15, -69, 27.5), (70, 0, 0), None)):
        co.location = loc; co.rotation_euler = tuple(math.radians(a) for a in rot)
        if ortho:
            cam.type = "ORTHO"; cam.ortho_scale = ortho
        else:
            cam.type = "PERSP"; cam.lens = 30 if nom != "grotte" else 50
        sc.render.filepath = os.path.join(dossier, "montagne_" + nom + ".png")
        bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(co)


# ---------------------------------------------------------------------------------------------- export
def exporter(objs):
    os.makedirs(SORTIE, exist_ok=True)
    selectionner(objs)
    bpy.ops.export_scene.fbx(
        filepath=FBX, use_selection=True, object_types={"MESH"}, apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", use_mesh_modifiers=False,
        mesh_smooth_type="FACE", use_tspace=False, add_leaf_bones=False, bake_anim=False, path_mode="AUTO",
        embed_textures=False)
    log("export", os.path.relpath(FBX, RACINE))


def main():
    hd = importer()
    rendu = decimer(hd, BUDGET, NOM)
    lisser(rendu)
    collision = decimer(hd, BUDGET_COLLISION, NOM + "_Collision")
    collision.data.materials.clear()
    img = cuire(hd, rendu)
    if "--rendus" in ARGS:
        rendus(ARGS[ARGS.index("--rendus") + 1], rendu)
    bpy.data.objects.remove(hd)
    if "--sans-export" not in ARGS:
        os.makedirs(SORTIE, exist_ok=True)
        img.filepath_raw = TEXTURE_PNG
        img.file_format = "PNG"
        img.save()
        log("texture", os.path.relpath(TEXTURE_PNG, RACINE))
        exporter([rendu, collision])
    log("terminé : rendu %d triangles, collision %d triangles" % (triangles(rendu), triangles(collision)))


if __name__ == "__main__":   # importé par montagne_flancs_pipeline.py : ne rien lancer
    main()
