# -*- coding: utf-8 -*-
"""
Bavaroise v4 : chaîne reproductible du modèle Tripo (face + dos, 8 pièces texturées) vers Unity. Script Blender sans
interface, dérivé de bavaroise_pipeline.py (v3, gardé intact et reproductible).

Lancement (Blender 5.2 portable) :
    "C:/Users/Furilax/Tools/blender-5.2.2-windows-x64/blender.exe" -b --python bavaroise_v4_pipeline.py -- [options]
Options :
    --budget <n>          triangles visés pour le corps (défaut 12000 ; répartis par pièce, voir PARTIES)
    --texture <n>         côté de l'atlas (défaut 2048 ; 4096 = les textures Tripo sans perte, sans gain visible)
    --angle <deg>         angle de lissage des normales (défaut 60 : arêtes plus vives marquées dures)
    --rendus <dossier>    rendus de vérification Blender (T-pose face, 3/4, profil, dos, visage ; Idle_A, Running_A, attaques)
    --sans-export         ne pas écrire les FBX (essais)
    --correction          corriger les couleurs vers la palette de la planche (v3) ; par défaut la couleur Tripo v4 est
                          gardée telle quelle (c'est celle de l'image Grok validée ; la correction la rend orange)
    --sans-normales-ponderees   lissage simple au lieu des normales pondérées par l'aire des faces
    --sortie <dossier>    autre dossier de sortie pour les FBX et la texture (essais de budget)

Entrée : ArtSources/References/Personnages/bavaroise_v4_tripo/cute+girl+3d+model.fbx (Tripo, image Grok v2 en face + dos,
« par parties : Équilibré », texturé) : 8 maillages tripo_part_0..7, 291 000 triangles, UV et une texture basecolor par
pièce (dossier cute+girl+3d+model.fbm/), 0,98 unité de haut, Z vers le haut, face vers -Y (repère Blender après import).
Pièces : 0 = tête, cheveux, buste, corsage, médaillon ; 1 = jupe, tablier, jupon, ceinture, nœuds ; 2/3 = jambes,
chaussettes, chaussures (2 à gauche +X, 3 à droite) ; 4/5 = bras et mains (4 gauche) ; 6/7 = manches (7 gauche).

Ce qui change par rapport à la v3 :
 - plus de planche de 12 pièces : les règles de poids sont exprimées géométriquement (hauteur du cou pour la tête et les
   cheveux, répartition verticale du torse, jupe rigide, manches par distance à l'épaule) ;
 - lisse, pas facetté : faces lisses, arêtes marquées dures au-delà de --angle, normales pondérées par l'aire ;
 - budget de triangles plus élevé (10 à 14 k au lieu de 6,5 k) ;
 - couleurs : la décimation conserve le découpage UV Tripo (une tuile de l'atlas par pièce, pas de dépliage) et la
   couleur est cuite (Cycles, Selected to Active) depuis le modèle Tripo haute définition et ses 8 textures ;
 - le nœud du tablier dans le dos existe dans le maillage Tripo : plus de nœud modélisé à la main.
Inchangé : armature Rig_Medium du Knight importée telle quelle, chope procédurale (code repris de la v3), export FBX aux
mêmes chemins (Assets/Art/Bavaroise/Tripo/Bavaroise_Tripo.fbx, Chope_Tripo.fbx, Bavaroise_Texture.png), copie de
travail ArtSources/Personnages/Bavaroise/bavaroise.blend.

Côté Unity : menu Deathless > Personnages > Bavaroise (Tripo) (Assets/Editor/Personnages/BavaroiseTripo.cs).
"""
import bpy, bmesh, math, os, sys, random
from mathutils import Vector, Matrix

# ---------------------------------------------------------------------------------------------- chemins
RACINE = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
REFS = os.path.join(RACINE, "ArtSources", "References", "Personnages")
SOURCE = os.path.join(REFS, "bavaroise_v4_tripo", "cute+girl+3d+model.fbx")
KNIGHT = os.path.join(RACINE, "Assets", "Art", "KayKit", "KayKit_Adventurers_2.0_FREE", "Characters", "fbx", "Knight.fbx")
ANIMS = os.path.join(RACINE, "Assets", "Art", "KayKit", "KayKit_Character_Animations_1.1", "Animations", "fbx", "Rig_Medium")
BLEND = os.path.join(RACINE, "ArtSources", "Personnages", "Bavaroise", "bavaroise.blend")

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
def option(nom, defaut=None):
    return ARGS[ARGS.index(nom) + 1] if nom in ARGS and ARGS.index(nom) + 1 < len(ARGS) else defaut

SORTIE = option("--sortie", os.path.join(RACINE, "Assets", "Art", "Bavaroise", "Tripo"))
BUDGET = int(option("--budget", 12000))
TAILLE_TEXTURE = int(option("--texture", 2048))
ANGLE_LISSAGE = math.radians(float(option("--angle", 60)))
TEXTURE_PNG = os.path.join(SORTIE, "Bavaroise_Texture.png")

# ---------------------------------------------------------------------------------------------- parties Tripo
# Numéro Tripo -> (nom, part du budget de triangles, symétrie X à la décimation).
# Côtés : le personnage regarde vers -Y, sa gauche est en +X (os *.l du Knight).
PARTIES = {
    0: ("Haut", 0.44, True),       # tête, cheveux, visage, buste, corsage, médaillon (là où l'œil regarde)
    1: ("Jupe", 0.23, True),       # jupe, tablier, jupon, ceinture, nœud de devant et nœud du dos
    2: ("JambeG", 0.055, False),   # jambe, chaussette, chaussure (gauche, +X)
    3: ("JambeD", 0.055, False),
    4: ("BrasG", 0.065, False),    # bras et main (la main compte)
    5: ("BrasD", 0.065, False),
    6: ("MancheD", 0.035, False),  # manches bouffantes : rondes, elles réclament plus que leur surface
    7: ("MancheG", 0.035, False),
}

def hexa(h):
    return (int(h[0:2], 16) / 255.0, int(h[2:4], 16) / 255.0, int(h[4:6], 16) / 255.0, 1.0)
# Couleurs unies de repli (sRGB, planche) : utilisées seulement si la cuisson échoue.
UNIES = {"Haut": hexa("E2A077"), "Jupe": hexa("848BA8"), "JambeG": hexa("F1ECE7"), "JambeD": hexa("F1ECE7"),
         "BrasG": hexa("E2A077"), "BrasD": hexa("E2A077"), "MancheD": hexa("F3EAE4"), "MancheG": hexa("F3EAE4")}
BOIS = hexa("9E5A38"); BOIS_SOMBRE = hexa("8A4E31"); CERCLAGE = hexa("A9A6A2"); CERCLAGE_SOMBRE = hexa("8F8C89")
MOUSSE = hexa("F8F2EA"); MOUSSE_OMBRE = hexa("E9DFD3"); FOND_CHOPE = hexa("6E3F28")

ATTR = "Couleur"
UV = "UV"
JOURNAL = []
def log(*a):
    s = " ".join(str(x) for x in a)
    JOURNAL.append(s)
    print("[bavaroise v4]", s)

# ---------------------------------------------------------------------------------------------- outils
def scene_vide():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def appliquer_transformations(o):
    o.data.transform(o.matrix_world)
    o.matrix_world = Matrix.Identity(4)

def triangles(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)

def remplacer_maillage(o, me):
    vieux = o.data
    o.data = me
    if vieux.users == 0:
        bpy.data.meshes.remove(vieux)

def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)

def lier(o):
    bpy.context.scene.collection.objects.link(o)

def selectionner(objs, actif=None):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = actif or objs[0]

# ---------------------------------------------------------------------------------------------- 1. import
def importer_tripo():
    """Les 8 pièces texturées. Retourne (parts, hd) : parts = copies de travail sans matériau (nommées, doublons fusionnés) ;
    hd = originaux Tripo intacts (UV, matériaux, images), gardés pour la cuisson de la texture."""
    avant = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=SOURCE)
    parts = {}; hd = []
    for o in sorted(set(bpy.data.objects) - avant, key=lambda o: o.name):
        if o.type != "MESH":
            bpy.data.objects.remove(o)
            continue
        n = int(o.name.split("_")[-1])
        appliquer_transformations(o)
        o.name = "HD_" + PARTIES[n][0]; o.hide_render = True
        hd.append(o)
        c = o.copy(); c.data = o.data.copy(); c.hide_render = False; lier(c)
        bm = bmesh.new(); bm.from_mesh(c.data)
        avant_v = len(bm.verts)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bm.to_mesh(c.data); bm.free()
        nom = PARTIES[n][0]
        c.name = nom; c.data.name = nom
        c["tripo"] = n
        c.data.materials.clear()
        parts[nom] = c
        if avant_v != len(c.data.vertices):
            log("doublons fusionnés", nom, avant_v, "->", len(c.data.vertices))
    log("import Tripo :", len(parts), "parties,", sum(triangles(o) for o in parts.values()), "triangles")
    return parts, hd

# ---------------------------------------------------------------------------------------------- squelette
def importer_squelette():
    """Armature Rig_Medium du Knight, sans ses maillages. Rien n'est modifié (hiérarchie, noms, repos)."""
    avant = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=KNIGHT)
    arm = None
    for o in set(bpy.data.objects) - avant:
        if o.type == "ARMATURE":
            arm = o
    for o in set(bpy.data.objects) - avant:
        if o is not arm:
            bpy.data.objects.remove(o)
    arm.name = "Rig_Medium"; arm.data.name = "Rig_Medium"
    for a in list(bpy.data.actions):
        bpy.data.actions.remove(a)
    return arm

def os_monde(arm, nom):
    b = arm.data.bones[nom]
    return arm.matrix_world @ b.head_local, arm.matrix_world @ b.tail_local

# ---------------------------------------------------------------------------------------------- 2. échelle
def sections_x(o, x0, x1, pas=0.01):
    """Coupes d'un bras le long de X : (x, épaisseur z + y, centre z, centre y)."""
    res = []
    vs = [v.co for v in o.data.vertices]
    x = x0
    while x < x1:
        s = [c for c in vs if x <= abs(c.x) < x + pas]
        if s:
            zmin = min(c.z for c in s); zmax = max(c.z for c in s); ymin = min(c.y for c in s); ymax = max(c.y for c in s)
            res.append((x + pas / 2, (zmax - zmin) + (ymax - ymin), (zmin + zmax) / 2, (ymin + ymax) / 2))
        x += pas
    return res

def mettre_a_l_echelle(parts, hd, arm):
    # Axe des bras du modèle : centre des coupes du bras gauche entre la manche et le poignet.
    bras = parts["BrasG"]
    xm = max(v.co.x for v in parts["MancheG"].data.vertices)
    coupes = [c for c in sections_x(bras, xm + 0.01, xm + 0.10)]
    z_bras = sum(c[2] for c in coupes) / len(coupes)
    y_bras = sum(c[3] for c in coupes) / len(coupes)
    epaule, _ = os_monde(arm, "upperarm.l")
    s = epaule.z / z_bras
    m = Matrix.Translation((0, -y_bras * s, 0)) @ Matrix.Scale(s, 4)
    for o in list(parts.values()) + hd:
        o.data.transform(m)
    log("échelle uniforme %.4f (axe des bras %.3f -> %.3f), décalage Y %.4f ; hauteur %.3f m" % (
        s, z_bras, epaule.z, -y_bras * s, max(v.co.z for o in parts.values() for v in o.data.vertices)))
    return s, m

# ---------------------------------------------------------------------------------------------- 3. décimation
def decimer(parts):
    dg = bpy.context.evaluated_depsgraph_get()
    for nom, o in parts.items():
        part, sym = next((b, s) for (n, b, s) in PARTIES.values() if n == nom)
        cible = int(BUDGET * part)
        avant = triangles(o)
        if cible < avant:
            mod = o.modifiers.new("Decimation", "DECIMATE")
            mod.decimate_type = "COLLAPSE"
            mod.ratio = cible / avant
            mod.use_collapse_triangulate = True
            mod.use_symmetry = sym
            mod.symmetry_axis = "X"
            dg.update()
            me = bpy.data.meshes.new_from_object(o.evaluated_get(dg))
            o.modifiers.clear()
            remplacer_maillage(o, me)
            me.name = nom
        log("décimation %-8s %6d -> %5d triangles (cible %d)" % (nom, avant, triangles(o), cible))

def lisser(o, angle=None):
    """Faces lisses ; arêtes marquées dures au-delà de l'angle (ourlets, semelles, bords du tablier)."""
    me = o.data
    me.shade_smooth()
    bm = bmesh.new(); bm.from_mesh(me)
    dures = 0
    for e in bm.edges:
        if len(e.link_faces) == 2 and e.calc_face_angle(0.0) > (angle or ANGLE_LISSAGE):
            e.smooth = False; dures += 1
        else:
            e.smooth = True
    bm.to_mesh(me); bm.free()
    return dures

# ---------------------------------------------------------------------------------------------- 4. atlas de texture
# La décimation conserve le découpage UV Tripo de chaque pièce (coutures comprises) : pas de dépliage. Chaque pièce reçoit
# une tuile de l'atlas (pièces 0 et 1 en 2048², jambes et bras en 1024², manches en 512² dans un atlas 4096², ou moitié
# avec --texture 2048), et la couleur est cuite depuis le modèle Tripo haute définition (ses 8 textures) sur ces UV.
# Sur demande (--correction), correction vers la palette de la planche comme en v3 ; puis bouchage des pixels noirs.
PALETTE_PLANCHE = {
    "cheveux": "E6A45C", "cheveux_ombre": "C98A48", "peau": "E2A077", "joue": "F0806E", "corsage": "6E3B3D",
    "carreau_bleu": "848BA8", "carreau_blanc": "DAD5DA", "jupe": "4E597C", "ceinture": "7088BE", "blanc": "F3EAE4",
    "chaussure": "704630", "or": "DCA650", "sombre": "35251F",
}
# Tuiles (en fraction de l'atlas) : numéro Tripo -> (u0, v0, côté).
TUILES = {0: (0.0, 0.0, 0.5), 1: (0.5, 0.0, 0.5), 2: (0.0, 0.5, 0.25), 3: (0.25, 0.5, 0.25), 4: (0.5, 0.5, 0.25),
          5: (0.75, 0.5, 0.25), 6: (0.0, 0.75, 0.125), 7: (0.125, 0.75, 0.125)}

def image_de_materiau(mat):
    if mat is None or not mat.use_nodes:
        return None
    for n in mat.node_tree.nodes:
        if n.type == "BSDF_PRINCIPLED":
            entree = n.inputs.get("Base Color")
            if entree and entree.is_linked:
                src = entree.links[0].from_node
                if src.type == "TEX_IMAGE" and src.image:
                    return src.image
    for n in mat.node_tree.nodes:
        if n.type == "TEX_IMAGE" and n.image:
            return n.image
    return None

def pixels_np(img):
    import numpy as np
    w, h = img.size
    px = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(px)
    return px.reshape(h, w, 4)

def corriger_couleurs(px):
    """Correction vers la palette de la planche sans perdre les détails (v3) : les teintes dominantes de l'atlas sont
    trouvées par k-moyennes (initialisées sur la palette), puis chaque pixel est décalé du mélange des écarts
    (cible - source) de ses teintes proches (poids gaussiens) : nuances, carreaux et traits restent."""
    import numpy as np
    T = px.shape[0]
    rgb = px[:, :, :3].reshape(-1, 3).copy()
    n = len(rgb)
    utiles = rgb.sum(axis=1) > 0.02
    noms = list(PALETTE_PLANCHE)
    cibles = np.array([hexa(PALETTE_PLANCHE[k])[:3] for k in noms], dtype=np.float32)
    ech = rgb[utiles][:: max(1, int(utiles.sum() // 200000))]
    sources = cibles.copy()
    for _ in range(12):
        d = ((ech[:, None, :] - sources[None, :, :]) ** 2).sum(axis=2)
        lab = d.argmin(axis=1)
        for i in range(len(sources)):
            sel = ech[lab == i]
            if len(sel) > 50:
                sources[i] = sel.mean(axis=0)
    part = np.bincount(lab, minlength=len(sources)) / len(lab)
    for i, k in enumerate(noms):
        log("correction %-14s source %s -> cible %s (%.1f %% des pixels)" % (
            k, "".join("%02X" % int(round(c * 255)) for c in sources[i]), PALETTE_PLANCHE[k], 100 * part[i]))
    delta = cibles - sources
    sigma2 = 2 * 0.085 ** 2
    sortie = rgb.copy()
    pas = 1 << 20
    for a in range(0, n, pas):
        bloc = rgb[a:a + pas]
        d = ((bloc[:, None, :] - sources[None, :, :]) ** 2).sum(axis=2)
        w = np.exp(-(d - d.min(axis=1, keepdims=True)) / sigma2)
        w /= w.sum(axis=1, keepdims=True)
        sortie[a:a + pas] = bloc + w @ delta
    sortie = np.clip(sortie, 0, 1)
    sortie[~utiles] = 0.0
    px[:, :, :3] = sortie.reshape(T, T, 3)

def boucher_trous(px):
    """Pixels restés noirs (fond des tuiles, hors des îles) : remplis de proche en proche par la moyenne des voisins, pour
    que le filtrage et les mips ne tirent pas vers le noir aux bords des îles."""
    import numpy as np
    im = px[:, :, :3]
    ok = (im.sum(axis=2) > 0.02).astype(np.float32)
    trous = int((ok == 0).sum())
    for _ in range(64):
        acc = np.zeros_like(im); nb = np.zeros_like(ok)
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            acc += np.roll(np.roll(im * ok[..., None], dx, 0), dy, 1)
            nb += np.roll(np.roll(ok, dx, 0), dy, 1)
        nouveaux = (ok == 0) & (nb > 0)
        if not nouveaux.any():
            break
        im[nouveaux] = acc[nouveaux] / nb[nouveaux][:, None]
        ok[nouveaux] = 1.0
    px[:, :, :3] = im
    log("pixels noirs bouchés (fond des tuiles compris) : %d" % trous)

def atlas_texture(parts, hd):
    """Les UV Tripo de chaque pièce décimée sont ramenées dans leur tuile de l'atlas, puis la couleur est cuite (Cycles,
    « Selected to Active ») depuis les 8 pièces Tripo haute définition jointes (chacune avec sa texture) : le découpage UV
    reste celui de Tripo (grandes îles, coutures propres), mais la couleur est reprojetée sur la géométrie décimée (la
    décimation déforme les UV sur le damier du tablier ; la cuisson corrige ça)."""
    import numpy as np
    T = TAILLE_TEXTURE
    for o in hd:
        nom = o.name[3:]
        n = next(k for k, (nn, _, _) in PARTIES.items() if nn == nom)
        img = image_de_materiau(o.active_material)
        if img is None:
            raise RuntimeError("texture introuvable pour " + o.name)
        u0, v0, cote = TUILES[n]
        me = parts[nom].data
        uv = me.uv_layers[0]
        uv.name = UV
        for d in uv.data:
            d.uv = (u0 + (d.uv.x % 1.0) * cote, v0 + (d.uv.y % 1.0) * cote)
        log("atlas : %-8s texture %d² -> tuile %d² en (%.3f, %.3f)" % (nom, img.size[0], int(T * cote), u0, v0))
    # Source : les 8 pièces Tripo jointes (chacune garde son matériau et son image).
    selectionner(hd)
    bpy.ops.object.join()
    src = bpy.context.view_layer.objects.active
    src.name = "HD_Tripo"; src.hide_render = False
    # Cible de cuisson : copie jointe des parties (mêmes UV), supprimée ensuite.
    copies = []
    for o in parts.values():
        c = o.copy(); c.data = o.data.copy(); lier(c); copies.append(c)
    selectionner(copies)
    bpy.ops.object.join()
    cible = bpy.context.view_layer.objects.active
    img = bpy.data.images.new("Bavaroise_Texture", T, T, alpha=False)
    mat = bpy.data.materials.new("Cuisson"); mat.use_nodes = True
    noeud = mat.node_tree.nodes.new("ShaderNodeTexImage"); noeud.image = img
    mat.node_tree.nodes.active = noeud
    cible.data.materials.clear(); cible.data.materials.append(mat)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = 4
    selectionner([src, cible], cible)
    bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"}, use_selected_to_active=True, cage_extrusion=0.02,
                        max_ray_distance=0.12, margin=12, margin_type="EXTEND", use_clear=True, target="IMAGE_TEXTURES")
    px = pixels_np(img)
    if "--correction" in ARGS:
        corriger_couleurs(px)
    boucher_trous(px)
    img.pixels.foreach_set(px.reshape(-1))
    os.makedirs(SORTIE, exist_ok=True)
    img.filepath_raw = TEXTURE_PNG
    img.file_format = "PNG"
    img.save()
    bpy.data.objects.remove(cible); bpy.data.objects.remove(src)
    log("atlas cuit %d x %d -> %s" % (T, T, os.path.relpath(TEXTURE_PNG, RACINE)))
    return img

def attribut_couleur(o):
    me = o.data
    a = me.color_attributes.get(ATTR)
    if a is None:
        a = me.color_attributes.new(ATTR, "FLOAT_COLOR", "CORNER")
    me.color_attributes.active_color = a
    me.color_attributes.render_color_index = me.color_attributes.find(ATTR)
    return a

def peindre_faces(o, couleur_de_face):
    """couleur_de_face(polygone) -> (r, g, b, a) sRGB ; même valeur sur tous les coins de la face."""
    a = attribut_couleur(o)
    for p in o.data.polygons:
        c = couleur_de_face(p)
        for li in p.loop_indices:
            a.data[li].color_srgb = c

# ---------------------------------------------------------------------------------------------- 5. bras
def poignet(o, manche):
    """Position du poignet (coupe la plus fine entre 55 % et 85 % de la longueur visible du bras) : (x, z, y)."""
    e = max(abs(v.co.x) for v in manche.data.vertices)
    xmax = max(abs(v.co.x) for v in o.data.vertices)
    coupes = sections_x(o, e + (xmax - e) * 0.55, e + (xmax - e) * 0.85, 0.008)
    w, _, zw, yw = min(coupes, key=lambda c: c[1])
    return e, w, zw, yw, xmax

def ajuster_bras(parts, arm):
    """Allonge l'avant-bras visible (de la sortie de manche au poignet) pour que le poignet du modèle tombe sur l'os
    wrist, puis grossit un peu la main autour du poignet. Le reste (manche, épaule) ne bouge pas."""
    poignet_os, _ = os_monde(arm, "wrist.l")
    for cote, bras, manche in ((1, "BrasG", "MancheG"), (-1, "BrasD", "MancheD")):
        o = parts[bras]
        e, w, zw, yw, xmax = poignet(o, parts[manche])
        W = abs(poignet_os.x)
        k = (W - e) / (w - e)
        h = 1.10                                                           # main un peu plus forte (planche)
        centre = Vector((cote * w, yw, zw)); centre2 = Vector((cote * W, yw, zw))
        for v in o.data.vertices:
            x = abs(v.co.x)
            if x <= e:
                continue
            if x <= w:
                v.co.x = cote * (e + (x - e) * k)
            else:
                v.co = centre2 + (v.co - centre) * h
        log("bras %s : sortie de manche %.3f, poignet %.3f -> %.3f (avant-bras x%.2f), main x%.2f, bout des doigts %.3f" % (
            bras, e, w, W, k, h, max(abs(v.co.x) for v in o.data.vertices)))

# ---------------------------------------------------------------------------------------------- 6. poids
def poids_torse(z):
    """Répartition verticale bassin / colonne / poitrine (os du Knight : hips 0,41, spine 0,60, chest 0,97)."""
    if z >= 1.02:
        return {"chest": 1.0}
    if z >= 0.80:
        t = smoothstep(0.80, 1.02, z); return {"spine": 1 - t, "chest": t}
    if z >= 0.62:
        t = smoothstep(0.62, 0.80, z); return {"hips": 1 - t, "spine": t}
    return {"hips": 1.0}

def melanger(a, b, t):
    r = {}
    for k, w in a.items():
        r[k] = r.get(k, 0) + w * (1 - t)
    for k, w in b.items():
        r[k] = r.get(k, 0) + w * t
    return r

def distance_segment(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.dot(ab), 1e-12)))
    return (a + ab * t - p).length

def lire_poids(o):
    noms = {g.index: g.name for g in o.vertex_groups}
    res = []
    for v in o.data.vertices:
        res.append({noms[g.group]: g.weight for g in v.groups if g.weight > 0})
    return res

def ecrire_poids(o, poids, max_influences=4):
    for g in list(o.vertex_groups):
        o.vertex_groups.remove(g)
    groupes = {}
    for i, d in enumerate(poids):
        d = {k: w for k, w in d.items() if w > 1e-4}
        d = dict(sorted(d.items(), key=lambda kv: -kv[1])[:max_influences])
        tot = sum(d.values())
        for k, w in d.items():
            if k not in groupes:
                groupes[k] = o.vertex_groups.new(name=k)
            groupes[k].add([i], w / tot, "REPLACE")

def poids(parts, arm):
    """Poids automatiques (chaleur) puis règles géométriques par pièce, 4 influences au plus :
    - Haut : au-dessus du cou (os head), tête 100 % (visage, oreilles, cheveux, tresses) ; cou en fondu ; bas du corsage
      sur la répartition verticale du torse (comme le haut de la jupe : pas de jour à la taille) ;
    - Jupe : rigide, bassin, puis cuisses jusqu'à 45 % vers l'ourlet (elle suit les jambes sans s'étirer) ; le haut (ceinture,
      nœud du dos) sur la répartition du torse ;
    - manches : poitrine près de l'épaule, bras ensuite (pas d'étirement du ballon) ;
    - bras, jambes : chaleur limitée aux os du membre."""
    for b in arm.data.bones:
        b.use_deform = b.name not in ("root", "handslot.l", "handslot.r")
    seg = {b.name: os_monde(arm, b.name) for b in arm.data.bones}
    chaleur = ["Haut", "BrasG", "BrasD", "JambeG", "JambeD", "MancheG", "MancheD"]
    selectionner([parts[n] for n in chaleur] + [arm], arm)
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    autorises = {
        "Haut": ["hips", "spine", "chest", "head", "upperarm.l", "upperarm.r"],
        "BrasG": ["chest", "upperarm.l", "lowerarm.l", "wrist.l", "hand.l"],
        "BrasD": ["chest", "upperarm.r", "lowerarm.r", "wrist.r", "hand.r"],
        "JambeG": ["hips", "upperleg.l", "lowerleg.l", "foot.l", "toes.l"],
        "JambeD": ["hips", "upperleg.r", "lowerleg.r", "foot.r", "toes.r"],
        "MancheG": ["chest", "upperarm.l"],
        "MancheD": ["chest", "upperarm.r"],
    }
    tete_z = seg["head"][0].z
    bilan = {}
    for nom, o in parts.items():
        vs = o.data.vertices
        if nom in autorises:
            brut = lire_poids(o)
            res = []
            vides = 0
            for v, d in zip(vs, brut):
                d = {k: w for k, w in d.items() if k in autorises[nom]}
                if sum(d.values()) < 1e-3:
                    vides += 1
                    k = min(autorises[nom], key=lambda b: distance_segment(v.co, *seg[b]))
                    d = {k: 1.0}
                p = v.co
                if nom == "Haut":
                    # Tête, cheveux et oreilles entièrement sur la tête ; le cou passe de la poitrine à la tête. Les tresses
                    # du chignon descendent derrière la nuque (y > 0) : elles restent sur la tête plus bas que le cou.
                    bas = tete_z - (0.12 if p.y > 0.03 else 0.06)
                    t = smoothstep(bas, bas + 0.07, p.z)
                    d = melanger(d, {"head": 1.0}, t)
                    if p.z < 1.0:
                        d = melanger(d, poids_torse(p.z), smoothstep(1.0, 0.92, p.z))
                if nom.startswith("Manche"):
                    cote = "l" if nom.endswith("G") else "r"
                    t = smoothstep(0.17, 0.26, abs(p.x))
                    d = {"chest": 1 - t, "upperarm." + cote: t}
                res.append(d)
            bilan[nom] = "chaleur" + (" (%d sommets complétés au plus proche)" % vides if vides else "")
        elif nom == "Jupe":
            zs = [v.co.z for v in vs]
            z_haut = max(zs); z_ourlet = min(zs)
            res = []
            for v in vs:
                p = v.co
                u = max(0.0, min(1.0, (z_haut - 0.06 - p.z) / (z_haut - 0.06 - z_ourlet)))
                part = 0.45 * u ** 1.6
                g = 1 / (1 + math.exp(-p.x / 0.035))
                bas = {"hips": 1 - part, "upperleg.l": part * g, "upperleg.r": part * (1 - g)}
                d = melanger(bas, poids_torse(p.z), smoothstep(z_haut - 0.10, z_haut - 0.01, p.z))
                res.append(d)
            bilan[nom] = "rigide (bassin + cuisses jusqu'à 45 %)"
        else:
            continue
        ecrire_poids(o, res)
        if not any(m.type == "ARMATURE" for m in o.modifiers):
            m = o.modifiers.new("Armature", "ARMATURE"); m.object = arm
    log("hauteur du cou (os head) : %.3f m" % tete_z)
    for nom in parts:
        log("poids %-8s %s" % (nom, bilan.get(nom, "?")))

# ---------------------------------------------------------------------------------------------- jointure
def joindre(parts, arm):
    objs = list(parts.values())
    for o in objs:
        o.parent = None          # frère de l'armature, comme les maillages du Knight
    corps = parts["Haut"]
    selectionner(objs, corps)
    bpy.ops.object.join()
    corps.name = "Bavaroise_Corps"; corps.data.name = "Bavaroise_Corps"
    for m in list(corps.modifiers):
        corps.modifiers.remove(m)
    me = corps.data
    # Lissage : faces lisses, arêtes dures au-delà de l'angle, puis normales pondérées par l'aire des faces (figées
    # en normales personnalisées, exportées dans le FBX).
    dures = lisser(corps)
    if "--sans-normales-ponderees" not in ARGS:
        wn = corps.modifiers.new("NormalesPonderees", "WEIGHTED_NORMAL")
        wn.mode = "FACE_AREA"; wn.keep_sharp = True; wn.weight = 50
        selectionner([corps])
        bpy.ops.object.modifier_apply(modifier=wn.name)
    m = corps.modifiers.new("Armature", "ARMATURE"); m.object = arm
    img = bpy.data.images.get("Bavaroise_Texture")
    if img is not None:
        mat = bpy.data.materials.new("Bavaroise_Texture"); mat.use_nodes = True
        t = mat.node_tree.nodes.new("ShaderNodeTexImage"); t.image = img
        mat.node_tree.links.new(t.outputs[0], mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
        mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
        if UV in me.uv_layers:
            me.uv_layers.active = me.uv_layers[UV]
    else:
        mat = bpy.data.materials.get("Bavaroise") or bpy.data.materials.new("Bavaroise")
    me.materials.clear(); me.materials.append(mat)
    log("corps joint : %d triangles, %d sommets, %d groupes d'os, %d arêtes dures (angle > %.0f°)" % (
        triangles(corps), len(me.vertices), len(corps.vertex_groups), dures, math.degrees(ANGLE_LISSAGE)))
    return corps

# ---------------------------------------------------------------------------------------------- 7. chope (code v3, inchangé)
def chope():
    """Chope de la planche : 12 douelles brunes (deux teintes alternées), légère panse, deux cerclages gris épais
    biseautés, anse grise, mousse blanche facettée qui déborde avec quatre coulures. Modélisée axe selon +Z, anse côté
    +Y, puis passée dans le repère du socket (voir l'en-tête)."""
    bm = bmesh.new()
    cf = []   # (face, couleur)
    n = 12
    R = 0.135
    def anneau(rayon, z, decal=0.5):
        return [bm.verts.new((rayon * math.sin(2 * math.pi * (j + decal) / n), -rayon * math.cos(2 * math.pi * (j + decal) / n), z)) for j in range(n)]
    def bande(a, b, coul):
        for j in range(n):
            f = bm.faces.new((a[j], a[(j + 1) % n], b[(j + 1) % n], b[j]))
            cf.append((f, coul(j) if callable(coul) else coul))
    # Corps (douelles), panse légère.
    zs = [-0.2, -0.13, 0.0, 0.13, 0.19]
    rs = [R * 0.96, R * 1.0, R * 1.04, R * 1.0, R * 0.98]
    anneaux = [anneau(r, z) for r, z in zip(rs, zs)]
    for i in range(len(anneaux) - 1):
        bande(anneaux[i], anneaux[i + 1], lambda j: BOIS if j % 2 == 0 else BOIS_SOMBRE)
    f = bm.faces.new(list(reversed(anneaux[0]))); cf.append((f, FOND_CHOPE))
    f = bm.faces.new(anneaux[-1]); cf.append((f, BOIS_SOMBRE))
    # Cerclages : bandes épaisses biseautées (4 anneaux chacune).
    for (z0, z1) in ((-0.155, -0.085), (0.075, 0.145)):
        def rayon(z):
            # rayon du corps à cette hauteur (interpolé), plus l'épaisseur du cerclage
            for i in range(len(zs) - 1):
                if zs[i] <= z <= zs[i + 1]:
                    t = (z - zs[i]) / (zs[i + 1] - zs[i]); return rs[i] * (1 - t) + rs[i + 1] * t
            return R
        a = [anneau(rayon(z0) + 0.002, z0), anneau(rayon(z0) + 0.016, z0 + 0.010), anneau(rayon(z1) + 0.016, z1 - 0.010), anneau(rayon(z1) + 0.002, z1)]
        bande(a[0], a[1], CERCLAGE_SOMBRE); bande(a[1], a[2], CERCLAGE); bande(a[2], a[3], CERCLAGE_SOMBRE)
    # Anse : tube carré le long d'un arc, côté +Y.
    arc = [(0.112, 0.125), (0.172, 0.125), (0.214, 0.085), (0.222, 0.0), (0.214, -0.085), (0.172, -0.125), (0.112, -0.125)]
    sections = []
    for i, (y, z) in enumerate(arc):
        p = Vector((0, y, z))
        a0 = Vector((0, *arc[max(i - 1, 0)])); a1 = Vector((0, *arc[min(i + 1, len(arc) - 1)]))
        t = (a1 - a0).normalized()
        nrm = Vector((1, 0, 0)).cross(t).normalized()
        L, E = 0.020, 0.014
        sections.append([bm.verts.new(p + Vector((sx * L, 0, 0)) + nrm * sy * E) for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))])
    for i in range(len(sections) - 1):
        a, b = sections[i], sections[i + 1]
        for j in range(4):
            f = bm.faces.new((a[j], a[(j + 1) % 4], b[(j + 1) % 4], b[j])); cf.append((f, CERCLAGE))
    # Mousse : dôme facetté (9 x 4) qui déborde du bord, plus quatre coulures.
    def ellipsoide(c, r, nu, nv, coul, graine):
        rnd = random.Random(graine)
        pole_bas = bm.verts.new(c + Vector((0, 0, -r.z)))
        pole_haut = bm.verts.new(c + Vector((0, 0, r.z * (1 + rnd.uniform(-0.05, 0.05)))))
        rangs = []
        for k in range(1, nv):
            phi = math.pi * k / nv - math.pi / 2
            rang = []
            for j in range(nu):
                th = 2 * math.pi * (j + 0.5 * (k % 2)) / nu
                bruit = 1 + rnd.uniform(-0.07, 0.07)
                rang.append(bm.verts.new(c + Vector((r.x * math.cos(phi) * math.cos(th) * bruit, r.y * math.cos(phi) * math.sin(th) * bruit, r.z * math.sin(phi)))))
            rangs.append(rang)
        for j in range(nu):
            f = bm.faces.new((pole_bas, rangs[0][(j + 1) % nu], rangs[0][j])); cf.append((f, coul(f)))
            f = bm.faces.new((pole_haut, rangs[-1][j], rangs[-1][(j + 1) % nu])); cf.append((f, coul(f)))
        for k in range(len(rangs) - 1):
            for j in range(nu):
                f = bm.faces.new((rangs[k][j], rangs[k][(j + 1) % nu], rangs[k + 1][(j + 1) % nu], rangs[k + 1][j])); cf.append((f, coul(f)))
    def teinte_mousse(f):
        f.normal_update()
        return MOUSSE if f.normal.z > 0.35 else MOUSSE_OMBRE
    ellipsoide(Vector((0, 0, 0.198)), Vector((0.148, 0.148, 0.092)), 9, 4, teinte_mousse, 51)
    for k, ang in enumerate((25, 120, 210, 300)):
        a = math.radians(ang)
        ellipsoide(Vector((0.13 * math.sin(a), -0.13 * math.cos(a), 0.172)), Vector((0.042, 0.042, 0.04)), 5, 3, lambda f: MOUSSE, 60 + k)
    # Repère du socket : anse (prise à mi-hauteur un peu sous le centre) à l'origine ; haut de la chope selon -X Blender
    # (= +X Unity), corps selon -Y Blender (= +Z Unity).
    prise = Vector((0, 0.222, -0.055))
    rot = Matrix.Rotation(math.radians(-90), 4, "Y")
    for v in bm.verts:
        v.co = rot @ (v.co - prise)
    bm.faces.index_update()
    teintes = {f.index: c for f, c in cf}
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new("Chope_Tripo")
    bm.to_mesh(me); bm.free()
    me.shade_flat()
    o = bpy.data.objects.new("Chope_Tripo", me); lier(o)
    peindre_faces(o, lambda p: teintes[p.index])
    mat = bpy.data.materials.get("Bavaroise") or bpy.data.materials.new("Bavaroise")
    me.materials.append(mat)
    log("chope : %d triangles" % triangles(o))
    return o

# ---------------------------------------------------------------------------------------------- 8. export
def exporter(objs, chemin):
    selectionner(objs)
    os.makedirs(os.path.dirname(chemin), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=chemin, use_selection=True, object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        use_mesh_modifiers=False, mesh_smooth_type="FACE", colors_type="SRGB", use_tspace=False,
        add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X", use_armature_deform_only=False,
        armature_nodetype="NULL", bake_anim=False, path_mode="AUTO", embed_textures=False)
    log("export", os.path.relpath(chemin, RACINE))

# ---------------------------------------------------------------------------------------------- rendus Blender
def rendus(dossier, corps, arm):
    """Rendus Workbench (atlas de texture, lumière studio) : face, 3/4, profil, dos et visage en T-pose, puis Idle_A, Running_A
    et deux attaques appliquées depuis les FBX d'animation KayKit (mêmes noms d'os)."""
    os.makedirs(dossier, exist_ok=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "STUDIO"
    sc.display.shading.color_type = "TEXTURE" if bpy.data.images.get("Bavaroise_Texture") else "VERTEX"
    sc.display.shading.show_object_outline = False
    sc.render.film_transparent = False
    sc.world = bpy.data.worlds.new("Fond") if sc.world is None else sc.world
    sc.world.color = (0.17, 0.17, 0.18)
    cam = bpy.data.cameras.new("CamRendu"); cam.type = "ORTHO"
    co = bpy.data.objects.new("CamRendu", cam); lier(co); sc.camera = co
    def cliche(nom, lacet, zc=0.95, echelle=2.1, w=640, h=760, tangage=0.0):
        cam.ortho_scale = echelle; sc.render.resolution_x = w; sc.render.resolution_y = h
        a = math.radians(lacet); d = 6
        co.location = Vector((d * math.sin(a), -d * math.cos(a), zc + d * math.sin(math.radians(tangage))))
        co.rotation_euler = (math.radians(90 - tangage), 0, a)
        sc.render.filepath = os.path.join(dossier, nom + ".png")
        bpy.ops.render.render(write_still=True)
    for nom, lacet in (("tpose_face", 0), ("tpose_34", -35), ("tpose_profil", -90), ("tpose_dos", 180)):
        cliche(nom, lacet)
    cliche("tpose_visage", 0, 1.45, 0.7, 700, 700)
    cliche("tpose_tablier", 0, 0.75, 0.8, 700, 700)
    actions = {}
    for f, noms in (("Rig_Medium_General.fbx", ["Idle_A"]), ("Rig_Medium_MovementBasic.fbx", ["Running_A"]),
                    ("Rig_Medium_CombatMelee.fbx", ["Melee_Dualwield_Attack_Slice", "Melee_2H_Attack_Chop"])):
        avant = set(bpy.data.objects); avant_a = set(bpy.data.actions)
        bpy.ops.import_scene.fbx(filepath=os.path.join(ANIMS, f))
        for o in set(bpy.data.objects) - avant:
            bpy.data.objects.remove(o)
        for a in set(bpy.data.actions) - avant_a:
            for n in noms:
                if a.name.endswith(n):
                    a.use_fake_user = True; actions[n] = a
    arm.animation_data_create()
    for n, instants in (("Idle_A", [0.5]), ("Running_A", [0.2, 0.6]), ("Melee_Dualwield_Attack_Slice", [0.25, 0.5, 0.75]), ("Melee_2H_Attack_Chop", [0.3, 0.55])):
        a = actions.get(n)
        if a is None:
            log("rendu : action introuvable", n); continue
        arm.animation_data.action = a
        if hasattr(arm.animation_data, "action_slot") and len(getattr(a, "slots", [])) and arm.animation_data.action_slot is None:
            arm.animation_data.action_slot = a.slots[0]
        f0, f1 = a.frame_range
        for t in instants:
            sc.frame_set(int(round(f0 + (f1 - f0) * t)))
            cliche("%s_%02d" % (n, int(t * 100)), -35, 0.95, 2.3)
            cliche("%s_%02d_profil" % (n, int(t * 100)), -90, 0.95, 2.3)
    arm.animation_data.action = None
    for pb in arm.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
    bpy.data.objects.remove(co)

# ---------------------------------------------------------------------------------------------- principal
def principal():
    scene_vide()
    parts, hd = importer_tripo()
    arm = importer_squelette()
    s, m = mettre_a_l_echelle(parts, hd, arm)
    decimer(parts)
    try:
        atlas_texture(parts, hd)
        log("couleurs : atlas cuit depuis les 8 textures Tripo sur les UV Tripo tuilées")
    except Exception as ex:
        log("atlas impossible (%s) : couleurs unies par partie" % ex)
        for o in list(bpy.data.objects):
            if o.type == "MESH" and o.name.startswith("HD_"):
                bpy.data.objects.remove(o)
        for nom, o in parts.items():
            peindre_faces(o, lambda p, c=UNIES[nom]: c)
    ajuster_bras(parts, arm)
    poids(parts, arm)
    corps = joindre(parts, arm)
    ch = chope()
    if "--sans-export" not in ARGS:
        exporter([arm, corps], os.path.join(SORTIE, "Bavaroise_Tripo.fbx"))
        exporter([ch], os.path.join(SORTIE, "Chope_Tripo.fbx"))
    ch.hide_render = True
    if option("--rendus"):
        rendus(option("--rendus"), corps, arm)
    for a in bpy.data.actions:
        a.use_fake_user = False
    if "--sortie" not in ARGS:
        os.makedirs(os.path.dirname(BLEND), exist_ok=True)
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=BLEND, compress=True)
    log("total : %d triangles (corps) + chope %d" % (triangles(corps), triangles(ch)))
    with open(os.path.join(os.path.dirname(BLEND), "bavaroise_v4_pipeline.log"), "w", encoding="utf-8") as f:
        f.write("\n".join(JOURNAL) + "\n")

principal()
