# -*- coding: utf-8 -*-
"""
Bavaroise v3 : chaîne reproductible du modèle Tripo vers Unity (script Blender, sans interface).

Lancement (Blender 5.2) :
    "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --python bavaroise_pipeline.py -- [options]
Options :
    --texture <fichier>   version texturée du même modèle Tripo (.fbx, .glb, .gltf, .obj) ; par défaut, recherche
                          automatique dans ArtSources/References/Personnages/ (voir TEXTURE_NOMS)
    --rendus <dossier>    rendus de vérification Blender (couleurs, poses KayKit Idle_A, Running_A, attaque)
    --sans-export         ne pas écrire les FBX (essais)

Entrée : ArtSources/References/Personnages/cute female character 3d model.fbx (Tripo, généré par Quentin depuis la
T-pose de la planche, « par parties ») : 12 maillages tripo_part_0..11, ~96 000 triangles, sans UV ni couleur,
0,98 unité de haut, Z vers le haut, face vers -Y (repère Blender après import).

Étapes :
 1. Import et nettoyage : transformations appliquées, sommets en double fusionnés, une pièce par partie Tripo.
 2. Mise à l'échelle sur le squelette KayKit Rig_Medium (Knight, Adventurers 2.0) : échelle uniforme qui met l'axe
    des bras à la hauteur des os upperarm, décalage pour centrer les bras sur Y = 0.
 3. Décimation par partie (budget BUDGET, ~6 000 triangles au total) : visage et mains plus détaillés, jupe et
    jambes moins ; normales plates (facettes nettes).
 4. Couleurs par face (attribut « Couleur », domaine coin, valeur uniforme par face) : transfert depuis la version
    texturée de Tripo si elle existe (plus proche surface, échantillons moyennés dans la face), sinon une couleur unie
    par partie (vérification du rig et des animations seulement).
 5. Ajustement par sections : bras allongés entre la manche et le poignet pour que le poignet tombe sur l'os wrist,
    main un peu grossie autour du poignet (les os ne sont jamais modifiés).
 6. Nœud du tablier dans le dos (deux boucles, deux pans, nœud central), facetté, bleu de la ceinture.
 7. Squelette : armature Rig_Medium du Knight importée telle quelle (hiérarchie, noms, orientation de repos intacts),
    poids automatiques (chaleur) puis corrections par partie : coiffure 100 % tête, jupe rigide sur bassin et
    cuisses, manches sur poitrine et bras, nœud rigide, os autorisés par partie, 4 influences au plus.
 8. Chope (bois à douelles, deux cerclages gris, anse, mousse facettée qui déborde), couleurs par face.
 9. Export FBX : Assets/Art/Bavaroise/Tripo/Bavaroise_Tripo.fbx (armature Rig_Medium + maillage skinné
    Bavaroise_Corps, mêmes axes que les FBX KayKit) et Chope_Tripo.fbx (repère du socket : anse à l'origine, haut de
    la chope selon +X Unity, corps selon +Z Unity, comme l'ancienne Chope_Bavaroise).
    Copie de travail : ArtSources/Personnages/Bavaroise/bavaroise.blend.

Côté Unity : menu Deathless > Personnages > Bavaroise (Tripo) (Assets/Editor/Personnages/BavaroiseTripo.cs).
"""
import bpy, bmesh, math, os, sys, glob, random
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

# ---------------------------------------------------------------------------------------------- chemins
RACINE = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", ".."))
REFS = os.path.join(RACINE, "ArtSources", "References", "Personnages")
SOURCE = os.path.join(REFS, "cute female character 3d model.fbx")
KNIGHT = os.path.join(RACINE, "Assets", "Art", "KayKit", "KayKit_Adventurers_2.0_FREE", "Characters", "fbx", "Knight.fbx")
ANIMS = os.path.join(RACINE, "Assets", "Art", "KayKit", "KayKit_Character_Animations_1.1", "Animations", "fbx", "Rig_Medium")
SORTIE = os.path.join(RACINE, "Assets", "Art", "Bavaroise", "Tripo")
BLEND = os.path.join(RACINE, "ArtSources", "Personnages", "Bavaroise", "bavaroise.blend")
# Version texturée attendue (même modèle passé par l'outil Texture de Tripo Studio) : premier fichier trouvé.
TEXTURE_NOMS = ["bavaroise_tripo_texture/*.fbx", "bavaroise_tripo_texture/*.glb", "bavaroise_tripo_texture.fbx", "bavaroise_tripo_texture.glb", "bavaroise_tripo_texture.gltf",
                "bavaroise_tripo_texture.obj", "*texture*.glb", "*texture*.fbx", "*textured*.*", "*tripo*.glb"]

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
def option(nom, defaut=None):
    return ARGS[ARGS.index(nom) + 1] if nom in ARGS and ARGS.index(nom) + 1 < len(ARGS) else defaut

# ---------------------------------------------------------------------------------------------- parties Tripo
# Numéro Tripo -> (nom, budget de triangles après décimation, symétrie X à la décimation).
# Côtés : le personnage regarde vers -Y, sa gauche est en +X (os *.l du Knight).
PARTIES = {
    0: ("Jupe", 1000, True),         # jupe, tablier, jupon, ceinture et petit nœud de devant
    1: ("Cheveux", 1500, True),      # coiffure (couronne de tresses)
    2: ("Corsage", 800, True),       # corsage, laçage
    3: ("JambeG", 280, False),       # jambe, chaussette, chaussure (gauche, +X)
    4: ("Peau", 1450, True),         # visage, oreilles, cou, décolleté (yeux, sourire, joues sculptés)
    5: ("BrasD", 260, False),        # bras et main droits (-X)
    6: ("MancheD", 120, False),
    7: ("MancheG", 120, False),
    8: ("JambeD", 280, False),
    9: ("BrasG", 260, False),
    10: ("Medaillon", 80, True),     # cadre du médaillon cœur
    11: ("MedaillonCoeur", 40, True),
}

# Couleurs unies par partie (sRGB), en attendant la version texturée : relevées sur la planche.
def hexa(h):
    return (int(h[0:2], 16) / 255.0, int(h[2:4], 16) / 255.0, int(h[4:6], 16) / 255.0, 1.0)
UNIES = {
    "Jupe": hexa("848BA8"), "Cheveux": hexa("E6A45C"), "Corsage": hexa("6E3B3D"), "JambeG": hexa("F1ECE7"),
    "JambeD": hexa("F1ECE7"), "Peau": hexa("E2A077"), "BrasD": hexa("E2A077"), "BrasG": hexa("E2A077"),
    "MancheD": hexa("F3EAE4"), "MancheG": hexa("F3EAE4"), "Medaillon": hexa("DCA650"), "MedaillonCoeur": hexa("DCA650"),
}
BLEU_NOEUD = hexa("7088BE")      # ceinture et nœud du dos (planche, vue de dos)
BLEU_NOEUD_OMBRE = hexa("6178AE")
BOIS = hexa("9E5A38"); BOIS_SOMBRE = hexa("8A4E31"); CERCLAGE = hexa("A9A6A2"); CERCLAGE_SOMBRE = hexa("8F8C89")
MOUSSE = hexa("F8F2EA"); MOUSSE_OMBRE = hexa("E9DFD3"); FOND_CHOPE = hexa("6E3F28")

ATTR = "Couleur"
JOURNAL = []
def log(*a):
    s = " ".join(str(x) for x in a)
    JOURNAL.append(s)
    print("[bavaroise]", s)

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

# ---------------------------------------------------------------------------------------------- 1. import
def importer_tripo():
    avant = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=SOURCE)
    parts = {}
    for o in set(bpy.data.objects) - avant:
        if o.type != "MESH":
            bpy.data.objects.remove(o)
            continue
        n = int(o.name.split("_")[-1])
        appliquer_transformations(o)
        bm = bmesh.new(); bm.from_mesh(o.data)
        avant_v = len(bm.verts)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bm.to_mesh(o.data); bm.free()
        nom = PARTIES[n][0]
        o.name = nom; o.data.name = nom
        o["tripo"] = n
        o.data.materials.clear()
        parts[nom] = o
        if avant_v != len(o.data.vertices):
            log("doublons fusionnés", nom, avant_v, "->", len(o.data.vertices))
    log("import Tripo :", len(parts), "parties,", sum(triangles(o) for o in parts.values()), "triangles")
    return parts

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

def mettre_a_l_echelle(parts, arm):
    # Axe des bras du modèle : centre des coupes du bras gauche entre la manche et le poignet.
    bras = parts["BrasG"]
    xm = max(v.co.x for v in parts["MancheG"].data.vertices)
    coupes = [c for c in sections_x(bras, xm + 0.01, xm + 0.10)]
    z_bras = sum(c[2] for c in coupes) / len(coupes)
    y_bras = sum(c[3] for c in coupes) / len(coupes)
    epaule, _ = os_monde(arm, "upperarm.l")
    s = epaule.z / z_bras
    m = Matrix.Translation((0, -y_bras * s, 0)) @ Matrix.Scale(s, 4)
    for o in parts.values():
        o.data.transform(m)
    log("échelle uniforme %.4f (axe des bras %.3f -> %.3f), décalage Y %.4f ; hauteur %.3f m" % (
        s, z_bras, epaule.z, -y_bras * s, max(v.co.z for o in parts.values() for v in o.data.vertices)))
    return s, m

# ---------------------------------------------------------------------------------------------- 3. décimation
def decimer(parts):
    dg = bpy.context.evaluated_depsgraph_get()
    for nom, o in parts.items():
        cible = next(b for (n, b, sym) in PARTIES.values() if n == nom)
        sym = next(sym for (n, b, sym) in PARTIES.values() if n == nom)
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
        o.data.shade_flat()
        log("décimation %-15s %6d -> %5d triangles" % (nom, avant, triangles(o)))

# ---------------------------------------------------------------------------------------------- 4. couleurs
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

def trouver_texture():
    f = option("--texture")
    if f:
        return f if os.path.isfile(f) else None
    for motif in TEXTURE_NOMS:
        for f in sorted(glob.glob(os.path.join(REFS, motif))):
            if os.path.abspath(f) != os.path.abspath(SOURCE):
                return f
    return None

class Echantillonneur:
    """Image de texture lue une fois (pixels sRGB tels que stockés), échantillonnage bilinéaire en UV répétées."""
    def __init__(self, image):
        self.w, self.h = image.size
        import array
        self.px = array.array("f", [0.0]) * (self.w * self.h * 4)
        image.pixels.foreach_get(self.px)
        self.lineaire = image.is_float   # images flottantes (EXR) : valeurs linéaires

    def texel(self, x, y):
        x %= self.w; y %= self.h
        i = (y * self.w + x) * 4
        return self.px[i], self.px[i + 1], self.px[i + 2]

    def __call__(self, u, v):
        x = (u % 1.0) * self.w - 0.5; y = (v % 1.0) * self.h - 0.5
        x0 = math.floor(x); y0 = math.floor(y); fx = x - x0; fy = y - y0
        c00 = self.texel(x0, y0); c10 = self.texel(x0 + 1, y0); c01 = self.texel(x0, y0 + 1); c11 = self.texel(x0 + 1, y0 + 1)
        c = [(c00[k] * (1 - fx) + c10[k] * fx) * (1 - fy) + (c01[k] * (1 - fx) + c11[k] * fx) * fy for k in range(3)]
        if self.lineaire:
            c = [(1.055 * (x ** (1 / 2.4)) - 0.055) if x > 0.0031308 else 12.92 * x for x in c]
        return c

def image_de_materiau(mat):
    if mat is None or not mat.use_nodes:
        return None
    # Couleur de base du BSDF d'abord, sinon première image trouvée.
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

def image_a_part(fichier):
    """Texture fournie à part, dans le même dossier que le modèle texturé."""
    dossier = os.path.dirname(fichier)
    for motif in ["*basecolor*", "*base_color*", "*albedo*", "*diffuse*", "*texture*", "*tripo*"]:
        for f in sorted(glob.glob(os.path.join(dossier, motif))):
            if f.lower().endswith((".png", ".jpg", ".jpeg", ".tga", ".webp", ".exr")):
                return bpy.data.images.load(f)
    return None

def importer_texture(fichier):
    avant = set(bpy.data.objects)
    ext = os.path.splitext(fichier)[1].lower()
    if ext == ".fbx":
        bpy.ops.import_scene.fbx(filepath=fichier)
    elif ext in (".glb", ".gltf"):
        bpy.ops.import_scene.gltf(filepath=fichier)
    elif ext == ".obj":
        bpy.ops.wm.obj_import(filepath=fichier)
    else:
        raise RuntimeError("format non géré : " + fichier)
    objs = [o for o in set(bpy.data.objects) - avant]
    maillages = [o for o in objs if o.type == "MESH"]
    for o in objs:
        if o.type != "MESH":
            o.name = "_tex_" + o.name
    return objs, maillages

def transferer_couleurs(parts, fichier, transfo_tripo):
    """Couleurs par face prises sur la version texturée (même modèle Tripo) : pour chaque face décimée, 7 points (centre
    et points à mi-chemin des coins et des arêtes) projetés sur la plus proche surface texturée, UV interpolées,
    texture échantillonnée, moyenne. Le modèle texturé est recalé sur l'original Tripo (boîtes englobantes, puis demi-tour
    autour de Z si l'écart moyen est plus faible), puis passé par la même échelle que le modèle."""
    objs, maillages = importer_texture(fichier)
    if not maillages:
        raise RuntimeError("aucun maillage dans " + fichier)
    image_secours = None
    # Un seul maillage de travail : triangles, UV, image par triangle.
    tris = []      # (a, b, c, uva, uvb, uvc, indice d'échantillonneur)
    echs = []; index_image = {}
    for o in maillages:
        me = o.data
        me.calc_loop_triangles()
        uv = me.uv_layers.active
        if uv is None:
            log("transfert : maillage sans UV ignoré", o.name)
            continue
        mw = o.matrix_world
        mats = [s.material for s in o.material_slots]
        for t in me.loop_triangles:
            mat = mats[t.material_index] if t.material_index < len(mats) else None
            img = image_de_materiau(mat)
            if img is None:
                if image_secours is None:
                    image_secours = image_a_part(fichier)
                img = image_secours
            if img is None:
                continue
            if img.name not in index_image:
                index_image[img.name] = len(echs); echs.append(Echantillonneur(img))
            v = [mw @ me.vertices[i].co for i in t.vertices]
            u = [uv.data[li].uv.copy() for li in t.loops]
            tris.append((v[0], v[1], v[2], u[0], u[1], u[2], index_image[img.name]))
    if not tris:
        raise RuntimeError("aucune texture lisible dans " + fichier)
    # Recalage : boîte englobante du modèle texturé sur celle de l'original (même hauteur, mêmes pieds, même centre).
    pts = [p for t in tris for p in t[:3]]
    def boite(ps):
        return (Vector((min(p.x for p in ps), min(p.y for p in ps), min(p.z for p in ps))),
                Vector((max(p.x for p in ps), max(p.y for p in ps), max(p.z for p in ps))))
    tmin, tmax = boite(pts)
    omin, omax = transfo_tripo["boite_origine"]
    k = (omax.z - omin.z) / (tmax.z - tmin.z)
    centre_t = Vector(((tmin.x + tmax.x) / 2, (tmin.y + tmax.y) / 2, tmin.z))
    centre_o = Vector(((omin.x + omax.x) / 2, (omin.y + omax.y) / 2, omin.z))
    m_echelle = transfo_tripo["matrice"]
    def recaler(p, demi_tour):
        q = (p - centre_t) * k
        if demi_tour:
            q = Vector((-q.x, -q.y, q.z))
        return m_echelle @ (q + centre_o)
    # Choix du demi-tour : écart moyen des sommets décimés à la surface texturée.
    meilleurs = None
    for demi_tour in (False, True):
        vs = []; fs = []
        for t in tris:
            i = len(vs); vs += [recaler(t[0], demi_tour), recaler(t[1], demi_tour), recaler(t[2], demi_tour)]; fs.append((i, i + 1, i + 2))
        arbre = BVHTree.FromPolygons(vs, fs, all_triangles=True)
        ecart = 0.0; n = 0
        for o in parts.values():
            for v in list(o.data.vertices)[::7]:
                r = arbre.find_nearest(v.co)
                if r[0] is not None:
                    ecart += r[3]; n += 1
        ecart /= max(1, n)
        if meilleurs is None or ecart < meilleurs[0]:
            meilleurs = (ecart, demi_tour, arbre, vs)
    ecart, demi_tour, arbre, vs = meilleurs
    log("transfert de texture depuis %s : %d triangles, %d image(s), écart moyen %.4f m%s" % (
        os.path.basename(fichier), len(tris), len(echs), ecart, ", demi-tour" if demi_tour else ""))

    def couleur_au_point(p):
        loc, nrm, idx, dist = arbre.find_nearest(p)
        if idx is None:
            return None
        a, b, c = vs[3 * idx], vs[3 * idx + 1], vs[3 * idx + 2]
        # Coordonnées barycentriques du point projeté.
        v0 = b - a; v1 = c - a; v2 = loc - a
        d00 = v0.dot(v0); d01 = v0.dot(v1); d11 = v1.dot(v1); d20 = v2.dot(v0); d21 = v2.dot(v1)
        den = d00 * d11 - d01 * d01
        if abs(den) < 1e-14:
            wb, wc = 0.0, 0.0
        else:
            wb = (d11 * d20 - d01 * d21) / den; wc = (d00 * d21 - d01 * d20) / den
        wa = 1 - wb - wc
        t = tris[idx]
        uv = t[3] * wa + t[4] * wb + t[5] * wc
        return echs[t[6]](uv.x, uv.y)

    for nom, o in parts.items():
        me = o.data
        def couleur(p, me=me):
            coins = [me.vertices[i].co for i in p.vertices]
            c = p.center
            points = [c] + [c.lerp(q, 0.5) for q in coins] + [c.lerp((coins[i] + coins[(i + 1) % len(coins)]) / 2, 0.6) for i in range(len(coins))]
            acc = [0.0, 0.0, 0.0]; n = 0
            for q in points:
                col = couleur_au_point(q)
                if col:
                    acc = [acc[j] + col[j] for j in range(3)]; n += 1
            if n == 0:
                return UNIES[nom]
            return (acc[0] / n, acc[1] / n, acc[2] / n, 1.0)
        peindre_faces(o, couleur)
    for o in objs:
        bpy.data.objects.remove(o)
    return True

def couleurs(parts, transfo_tripo):
    fichier = trouver_texture()
    # Couleurs de sommet unies dans tous les cas (repli, lisibles par le shader Deathless/VertexColorLit).
    for nom, o in parts.items():
        peindre_faces(o, lambda p, c=UNIES[nom]: c)
    if fichier and "--couleurs-par-face" not in ARGS:
        cuire_texture(parts, fichier, transfo_tripo)
        return "texture cuite depuis " + os.path.basename(fichier)
    if fichier:
        try:
            transferer_couleurs(parts, fichier, transfo_tripo)
            return "texture Tripo (" + os.path.basename(fichier) + ")"
        except Exception as e:
            log("transfert impossible (%s) : couleurs unies par partie" % e)
    for nom, o in parts.items():
        peindre_faces(o, lambda p, c=UNIES[nom]: c)
    return "unies par partie (" + ("transfert en échec" if fichier else "version texturée absente") + ")"

# ---------------------------------------------------------------------------------------------- 4 bis. texture cuite
# Méthode retenue (01/10/2026, demande de Quentin) : la couleur par face efface tout ce qui est plus petit qu'une face
# (damier, médaillon, cils). On garde donc une texture : dépliage UV du modèle décimé (un atlas pour toutes les parties,
# visage agrandi), cuisson Cycles « Selected to Active » de la couleur de base du modèle texturé Tripo (8192², haute
# définition) vers une image TAILLE_TEXTURE², puis correction de couleurs vers la palette de la planche. Les facettes
# restent données par les normales plates. Une bande à droite de l'atlas (u > 0,97) est réservée aux couleurs unies
# (nœud du dos). L'ancienne méthode (couleurs par face) reste disponible : option --couleurs-par-face.
UV = "UV"
TAILLE_TEXTURE = 2048
TEXTURE_PNG = os.path.join(SORTIE, "Bavaroise_Texture.png")
ECHELLE_UV = {"Peau": 2.2, "MedaillonCoeur": 2.0, "Medaillon": 2.0, "BrasG": 1.3, "BrasD": 1.3}   # densité relative
RESERVE = {"noeud": (0.9875, 0.62), "noeud_ombre": (0.9875, 0.56)}                                  # centres en UV
# Palette de la planche (sRGB, relevée à la pipette sur bavaroise_planche.jpg) : cible de la correction.
PALETTE_PLANCHE = {
    "cheveux": "E6A45C", "cheveux_ombre": "C98A48", "peau": "E2A077", "joue": "F0806E", "corsage": "6E3B3D",
    "carreau_bleu": "848BA8", "carreau_blanc": "DAD5DA", "jupe": "4E597C", "ceinture": "7088BE", "blanc": "F3EAE4",
    "chaussure": "704630", "or": "DCA650", "sombre": "35251F",
}

def deplier(parts):
    """Atlas unique : Smart UV Project sur toutes les parties ensemble, îles du visage, du médaillon et des mains
    agrandies, puis rangement (pack) avec marges, et bande droite libérée."""
    objs = list(parts.values())
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        if UV not in o.data.uv_layers:
            o.data.uv_layers.new(name=UV)
        o.data.uv_layers.active = o.data.uv_layers[UV]
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.003, area_weight=0.0, correct_aspect=True, scale_to_bounds=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    for nom, k in ECHELLE_UV.items():
        if nom in parts:
            for d in parts[nom].data.uv_layers[UV].data:
                d.uv = d.uv * k
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.select_all(action="SELECT")
    bpy.ops.uv.pack_islands(rotate=True, margin=0.004)
    bpy.ops.object.mode_set(mode="OBJECT")
    for o in objs:
        for d in o.data.uv_layers[UV].data:
            d.uv = d.uv * 0.97

def recaler_haute_definition(hd, parts, transfo_tripo):
    """Place le modèle texturé sur le modèle décimé : boîte englobante sur celle de l'original Tripo (même hauteur, mêmes
    pieds, même centre), demi-tour autour de Z si l'écart est plus faible, puis la même échelle que le modèle."""
    ps = [v.co.copy() for v in hd.data.vertices]
    tmin = Vector((min(p.x for p in ps), min(p.y for p in ps), min(p.z for p in ps)))
    tmax = Vector((max(p.x for p in ps), max(p.y for p in ps), max(p.z for p in ps)))
    omin, omax = transfo_tripo["boite_origine"]
    k = (omax.z - omin.z) / (tmax.z - tmin.z)
    ct = Vector(((tmin.x + tmax.x) / 2, (tmin.y + tmax.y) / 2, tmin.z))
    co = Vector(((omin.x + omax.x) / 2, (omin.y + omax.y) / 2, omin.z))
    echantillon = [v.co.copy() for o in parts.values() for v in list(o.data.vertices)[::9]]
    faces = [tuple(p.vertices) for p in hd.data.polygons]
    meilleur = None
    for demi_tour in (False, True):
        M = transfo_tripo["matrice"] @ Matrix.Translation(co) @ (Matrix.Rotation(math.pi, 4, "Z") if demi_tour else Matrix.Identity(4)) @ Matrix.Scale(k, 4) @ Matrix.Translation(-ct)
        arbre = BVHTree.FromPolygons([M @ p for p in ps], faces)
        ecart = sum(arbre.find_nearest(q)[3] or 0 for q in echantillon) / len(echantillon)
        if meilleur is None or ecart < meilleur[0]:
            meilleur = (ecart, demi_tour, M)
    hd.data.transform(meilleur[2])
    log("texture : modèle haute définition recalé (écart moyen %.4f m%s)" % (meilleur[0], ", demi-tour" if meilleur[1] else ""))

def corriger_couleurs(img):
    """Correction vers la palette de la planche sans perdre les détails : les teintes dominantes de la texture cuite sont
    trouvées par k-moyennes (initialisées sur la palette), puis chaque pixel est décalé du mélange des écarts
    (cible - source) de ses teintes proches (poids gaussiens) : nuances, carreaux et traits restent."""
    import numpy as np
    n = img.size[0] * img.size[1]
    px = np.empty(n * 4, dtype=np.float32)
    img.pixels.foreach_get(px)
    px = px.reshape(n, 4)
    rgb = px[:, :3].copy()
    noms = list(PALETTE_PLANCHE)
    cibles = np.array([hexa(PALETTE_PLANCHE[k])[:3] for k in noms], dtype=np.float32)
    utiles = rgb.sum(axis=1) > 0.02          # pixels couverts par la cuisson (le fond reste noir)
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
    # Trous de cuisson (rayons sans impact, entre les tresses surtout : pixels restés au noir exact) : bouchés de proche
    # en proche par la moyenne des voisins remplis (le fond hors des îles se remplit aussi, sans effet).
    T0 = img.size[0]
    im = sortie.reshape(T0, T0, 3); ok = utiles.reshape(T0, T0).astype(np.float32)
    trous = int((ok == 0).sum())
    for _ in range(40):
        acc = np.zeros_like(im); n = np.zeros_like(ok)
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            acc += np.roll(np.roll(im * ok[..., None], dx, 0), dy, 1)
            n += np.roll(np.roll(ok, dx, 0), dy, 1)
        nouveaux = (ok == 0) & (n > 0)
        if not nouveaux.any():
            break
        im[nouveaux] = acc[nouveaux] / n[nouveaux][:, None]
        ok[nouveaux] = 1.0
    sortie = im.reshape(-1, 3)
    log("trous de cuisson bouchés (pixels noirs, fond compris) : %d" % trous)
    px[:, :3] = sortie
    # Bande réservée : couleurs unies du nœud du dos.
    T = img.size[0]
    px = px.reshape(T, T, 4)
    for cle, coul in (("noeud", BLEU_NOEUD), ("noeud_ombre", BLEU_NOEUD_OMBRE)):
        u, v = RESERVE[cle]
        x0 = int((u - 0.008) * T); x1 = int((u + 0.008) * T); y0 = int((v - 0.025) * T); y1 = int((v + 0.025) * T)
        px[y0:y1, x0:x1, :3] = coul[:3]
    img.pixels.foreach_set(px.reshape(-1))

def cuire_texture(parts, fichier, transfo_tripo):
    objs, maillages = importer_texture(fichier)
    if not maillages:
        raise RuntimeError("aucun maillage dans " + fichier)
    for o in maillages:
        appliquer_transformations(o)
    for o in objs:
        if o not in maillages:
            bpy.data.objects.remove(o)
    bpy.ops.object.select_all(action="DESELECT")
    for o in maillages:
        o.select_set(True)
    bpy.context.view_layer.objects.active = maillages[0]
    if len(maillages) > 1:
        bpy.ops.object.join()
    hd = bpy.context.view_layer.objects.active
    if image_de_materiau(hd.active_material) is None:
        img = image_a_part(fichier)
        if img is None:
            raise RuntimeError("texture introuvable pour " + fichier)
        mat = bpy.data.materials.new("TripoTexture"); mat.use_nodes = True
        t = mat.node_tree.nodes.new("ShaderNodeTexImage"); t.image = img
        mat.node_tree.links.new(t.outputs[0], mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
        hd.data.materials.clear(); hd.data.materials.append(mat)
    recaler_haute_definition(hd, parts, transfo_tripo)
    deplier(parts)
    # Cible de cuisson : copie jointe des parties (mêmes UV), supprimée ensuite.
    bpy.ops.object.select_all(action="DESELECT")
    copies = []
    for o in parts.values():
        c = o.copy(); c.data = o.data.copy(); lier(c); c.select_set(True); copies.append(c)
    bpy.context.view_layer.objects.active = copies[0]
    bpy.ops.object.join()
    cible = bpy.context.view_layer.objects.active
    img = bpy.data.images.new("Bavaroise_Texture", TAILLE_TEXTURE, TAILLE_TEXTURE, alpha=False)
    mat = bpy.data.materials.new("Cuisson"); mat.use_nodes = True
    noeud = mat.node_tree.nodes.new("ShaderNodeTexImage"); noeud.image = img
    mat.node_tree.nodes.active = noeud
    cible.data.materials.clear(); cible.data.materials.append(mat)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = 4
    bpy.ops.object.select_all(action="DESELECT")
    hd.select_set(True); cible.select_set(True)
    bpy.context.view_layer.objects.active = cible
    bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"}, use_selected_to_active=True, cage_extrusion=0.02,
                        max_ray_distance=0.15, margin=6, margin_type="EXTEND", use_clear=True, target="IMAGE_TEXTURES")
    corriger_couleurs(img)
    os.makedirs(SORTIE, exist_ok=True)
    img.filepath_raw = TEXTURE_PNG
    img.file_format = "PNG"
    img.save()
    bpy.data.objects.remove(cible); bpy.data.objects.remove(hd)
    log("texture cuite %d x %d -> %s" % (TAILLE_TEXTURE, TAILLE_TEXTURE, os.path.relpath(TEXTURE_PNG, RACINE)))
    return img

# ---------------------------------------------------------------------------------------------- 5. bras
def ajuster_bras(parts, arm):
    """Allonge l'avant-bras visible (de la sortie de manche au poignet) pour que le poignet du modèle tombe sur l'os
    wrist, puis grossit un peu la main autour du poignet. Le reste (manche, épaule) ne bouge pas."""
    poignet_os, _ = os_monde(arm, "wrist.l")
    for cote, bras, manche in ((1, "BrasG", "MancheG"), (-1, "BrasD", "MancheD")):
        o = parts[bras]
        e = max(abs(v.co.x) for v in parts[manche].data.vertices)        # sortie de la manche
        xmax = max(abs(v.co.x) for v in o.data.vertices)
        # Poignet : coupe la plus fine entre 55 % et 85 % de la longueur visible.
        coupes = sections_x(o, e + (xmax - e) * 0.55, e + (xmax - e) * 0.85, 0.008)
        w, _, zw, yw = min(coupes, key=lambda c: c[1])
        W = abs(poignet_os.x)
        k = (W - e) / (w - e)
        h = 1.12                                                           # main un peu plus forte (planche)
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

# ---------------------------------------------------------------------------------------------- 6. nœud du dos
def ruban(bm, chemin, largeurs, dir_largeur, epaisseur, ferme, couleurs, encoche=0.0):
    """Ruban plat balayé le long d'un chemin : section rectangle (largeur selon dir_largeur(i), épaisseur selon la
    normale). couleurs = (dessus, dessous). Encoche en V au bout d'un ruban ouvert (pans du nœud)."""
    n = len(chemin)
    anneaux = []
    for i in range(n):
        p = chemin[i]
        if ferme:
            t = (chemin[(i + 1) % n] - chemin[(i - 1) % n]).normalized()
        else:
            t = (chemin[min(i + 1, n - 1)] - chemin[max(i - 1, 0)]).normalized()
        l = dir_largeur(i, p, t)
        l = (l - t * l.dot(t)).normalized()
        nn = t.cross(l).normalized()
        demi = largeurs[i] / 2
        ep = epaisseur / 2
        milieu = p
        if not ferme and i == n - 1 and encoche:
            milieu = p - t * encoche
        coins = [p - l * demi - nn * ep, milieu - nn * ep, p + l * demi - nn * ep,
                 p + l * demi + nn * ep, milieu + nn * ep, p - l * demi + nn * ep]
        anneaux.append([bm.verts.new(c) for c in coins])
    faces = []
    segs = n if ferme else n - 1
    for i in range(segs):
        a = anneaux[i]; b = anneaux[(i + 1) % n]
        for j in range(6):
            f = bm.faces.new((a[j], a[(j + 1) % 6], b[(j + 1) % 6], b[j]))
            faces.append((f, couleurs[0] if j in (3, 4) else couleurs[1]))
    if not ferme:
        for anneau, inverse in ((anneaux[0], True), (anneaux[-1], False)):
            vs = list(reversed(anneau)) if inverse else anneau
            f = bm.faces.new(vs)
            faces.append((f, couleurs[1]))
    return faces

def noeud_dos(parts, s):
    """Gros nœud bleu du tablier, au dos de la taille (planche, vue de dos) : nœud central, deux boucles aplaties qui
    remontent vers l'extérieur, deux pans qui tombent sur la jupe, finis en V."""
    # Taille : coupe la plus fine du corsage et de la jupe réunis, dans le dos.
    vs = [v.co for nom in ("Corsage", "Jupe") for v in parts[nom].data.vertices]
    zc = 0.482 * s
    arbre = BVHTree.FromObject(parts["Jupe"], bpy.context.evaluated_depsgraph_get())
    arbre2 = BVHTree.FromObject(parts["Corsage"], bpy.context.evaluated_depsgraph_get())
    y_dos = -1.0
    for a in (arbre, arbre2):
        r = a.ray_cast(Vector((0, 2.0, zc)), Vector((0, -1, 0)))
        if r[0] is not None:
            y_dos = max(y_dos, r[0].y)
    origine = Vector((0, y_dos + 0.022, zc))
    bm = bmesh.new()
    faces = []
    Y = Vector((0, 1, 0)); Z = Vector((0, 0, 1))
    for cote in (1, -1):
        # Boucle : ellipse dans le plan XY (couche avant contre le dos, couche arrière vers l'extérieur), largeur du
        # ruban selon Z, pincée au nœud ; relevée de ~18° vers l'extérieur.
        a, b, nseg = 0.118, 0.030, 10
        chemin = []; larg = []
        for i in range(nseg):
            th = 2 * math.pi * i / nseg
            x = cote * a * (1 - math.cos(th))
            y = b * math.sin(th) + b * 0.6
            z = abs(x) * 0.48
            y -= 0.3 * z        # les boucles remontent contre le dos du corsage
            chemin.append(origine + Vector((x, y, z)))
            larg.append(0.045 + 0.105 * math.sin(th / 2) ** 2)
        faces += ruban(bm, chemin, larg, lambda i, p, t: Z, 0.010, True, (BLEU_NOEUD, BLEU_NOEUD_OMBRE))
        # Pan : descend en s'écartant, légère ondulation, fin en V.
        pts = [(0.014, 0.014, -0.014), (0.046, 0.030, -0.095), (0.072, 0.024, -0.185), (0.092, 0.032, -0.270), (0.108, 0.028, -0.345)]
        chemin = []
        for (x, y, z) in pts:
            # Les pans suivent la jupe (qui s'évase vers l'arrière) : posés sur sa surface, rayon lancé depuis le dos.
            p = origine + Vector((cote * x, y, z))
            for a in (arbre, arbre2):
                r = a.ray_cast(Vector((p.x, 2.0, p.z)), Vector((0, -1, 0)))
                if r[0] is not None:
                    p.y = max(p.y, r[0].y + y * 0.5 + 0.008)
            chemin.append(p)
        larg = [0.046, 0.058, 0.066, 0.072, 0.078]
        faces += ruban(bm, chemin, larg, lambda i, p, t: Vector((1, 0, 0)), 0.009, False, (BLEU_NOEUD, BLEU_NOEUD_OMBRE), encoche=0.028)
    # Nœud central : icosphère aplatie (facettes).
    geo = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=1.0)
    for v in geo["verts"]:
        v.co = origine + Vector((v.co.x * 0.040, 0.028 + v.co.y * 0.026, v.co.z * 0.038))
    nouv = set(f for v in geo["verts"] for f in v.link_faces)
    faces += [(f, BLEU_NOEUD) for f in nouv]
    me = bpy.data.meshes.new("Noeud")
    # Couleurs rangées par indice de face (to_mesh garde l'ordre des faces).
    bm.faces.index_update()
    teintes = {f.index: c for f, c in faces}
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new("Noeud", me); lier(o)
    me.shade_flat()
    peindre_faces(o, lambda p: teintes[p.index])
    if any(UV in x.data.uv_layers for x in parts.values()):
        # Atlas : toutes les faces du nœud lisent la bande réservée (bleu, ombre pour le dessous des rubans).
        uv = me.uv_layers.new(name=UV)
        for p in me.polygons:
            u, v = RESERVE["noeud" if teintes[p.index] == BLEU_NOEUD else "noeud_ombre"]
            for li in p.loop_indices:
                uv.data[li].uv = (u, v)
    parts["Noeud"] = o
    log("nœud du dos : %d triangles, centre (0, %.3f, %.3f)" % (triangles(o), origine.y, origine.z))
    return o

# ---------------------------------------------------------------------------------------------- 7. poids
TORSE = ("hips", "spine", "chest")
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
    # Os sans déformation : racine et sockets (aucun sommet ne doit les suivre).
    for b in arm.data.bones:
        b.use_deform = b.name not in ("root", "handslot.l", "handslot.r")
    seg = {b.name: os_monde(arm, b.name) for b in arm.data.bones}
    chaleur = ["Peau", "Corsage", "BrasG", "BrasD", "JambeG", "JambeD", "MancheG", "MancheD"]
    bpy.ops.object.select_all(action="DESELECT")
    for nom in chaleur:
        parts[nom].select_set(True)
    arm.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    autorises = {
        "Peau": ["hips", "spine", "chest", "head", "upperarm.l", "upperarm.r"],
        "Corsage": ["hips", "spine", "chest", "upperarm.l", "upperarm.r"],
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
                if nom == "Peau":
                    # Visage et oreilles entièrement sur la tête ; le cou passe de la poitrine à la tête.
                    t = smoothstep(tete_z - 0.06, tete_z + 0.03, p.z)
                    d = melanger(d, {"head": 1.0}, t)
                if nom == "Corsage" and p.z < 1.0:
                    # Bas du corsage : même répartition verticale que le haut de la jupe (pas de jour à la taille).
                    d = melanger(d, poids_torse(p.z), smoothstep(1.0, 0.92, p.z))
                if nom.startswith("Manche"):
                    # Manche bouffante : poitrine près de l'épaule, bras ensuite, sans étirement du ballon.
                    cote = "l" if nom.endswith("G") else "r"
                    t = smoothstep(0.17, 0.26, abs(p.x))
                    d = {"chest": 1 - t, "upperarm." + cote: t}
                res.append(d)
            bilan[nom] = "chaleur" + (" (%d sommets complétés au plus proche)" % vides if vides else "")
        elif nom in ("Cheveux",):
            res = [{"head": 1.0} for v in vs]; bilan[nom] = "tête 100 %"
        elif nom.startswith("Medaillon"):
            res = [{"chest": 1.0} for v in vs]; bilan[nom] = "poitrine 100 %"
        elif nom == "Noeud":
            zc = sum(v.co.z for v in vs) / len(vs)
            res = [poids_torse(zc + 0.03) for v in vs]; bilan[nom] = "rigide (taille)"
        elif nom == "Jupe":
            # Jupe rigide : haut = répartition du torse à la taille, puis bassin, et vers l'ourlet jusqu'à 45 % sur la
            # cuisse du côté (moitié-moitié au milieu, devant et derrière) : elle suit les jambes sans s'étirer.
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
        # Modificateur d'armature (les parties sans chaleur n'en ont pas encore).
        if not any(m.type == "ARMATURE" for m in o.modifiers):
            m = o.modifiers.new("Armature", "ARMATURE"); m.object = arm
    for nom in parts:
        log("poids %-15s %s" % (nom, bilan.get(nom, "?")))

# ---------------------------------------------------------------------------------------------- jointure
def joindre(parts, arm):
    bpy.ops.object.select_all(action="DESELECT")
    objs = list(parts.values())
    for o in objs:
        o.parent = None          # frère de l'armature, comme les maillages du Knight
        o.select_set(True)
    corps = parts["Peau"]
    bpy.context.view_layer.objects.active = corps
    bpy.ops.object.join()
    corps.name = "Bavaroise_Corps"; corps.data.name = "Bavaroise_Corps"
    for m in list(corps.modifiers):
        corps.modifiers.remove(m)
    m = corps.modifiers.new("Armature", "ARMATURE"); m.object = arm
    me = corps.data
    me.shade_flat()
    img = bpy.data.images.get("Bavaroise_Texture")
    if img is not None:
        # Matériau texturé (couleur de base = texture cuite) : exporté dans le FBX, refait côté Unity.
        mat = bpy.data.materials.new("Bavaroise_Texture"); mat.use_nodes = True
        t = mat.node_tree.nodes.new("ShaderNodeTexImage"); t.image = img
        mat.node_tree.links.new(t.outputs[0], mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
        mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
        if UV in me.uv_layers:
            me.uv_layers.active = me.uv_layers[UV]
    else:
        mat = bpy.data.materials.get("Bavaroise") or bpy.data.materials.new("Bavaroise")
    me.materials.clear(); me.materials.append(mat)
    log("corps joint : %d triangles, %d sommets, %d groupes d'os" % (triangles(corps), len(me.vertices), len(corps.vertex_groups)))
    return corps

# ---------------------------------------------------------------------------------------------- 8. chope
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

# ---------------------------------------------------------------------------------------------- 9. export
def exporter(objs, chemin):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
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
    """Rendus Workbench (couleurs de sommet) : face, profil, dos en T-pose, puis Idle_A, Running_A et l'attaque à deux
    mains appliqués depuis les FBX d'animation KayKit (mêmes noms d'os)."""
    os.makedirs(dossier, exist_ok=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "STUDIO"
    sc.display.shading.color_type = "TEXTURE" if bpy.data.images.get("Bavaroise_Texture") else "VERTEX"
    sc.render.film_transparent = False
    sc.world = bpy.data.worlds.new("Fond") if sc.world is None else sc.world
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
    cliche("tpose_visage", 0, 1.45, 0.6, 600, 600)
    # Actions KayKit.
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
    parts = importer_tripo()
    boite = (Vector((min(v.co.x for o in parts.values() for v in o.data.vertices), min(v.co.y for o in parts.values() for v in o.data.vertices), min(v.co.z for o in parts.values() for v in o.data.vertices))),
             Vector((max(v.co.x for o in parts.values() for v in o.data.vertices), max(v.co.y for o in parts.values() for v in o.data.vertices), max(v.co.z for o in parts.values() for v in o.data.vertices))))
    arm = importer_squelette()
    s, m = mettre_a_l_echelle(parts, arm)
    decimer(parts)
    methode = couleurs(parts, {"boite_origine": boite, "matrice": m})
    log("couleurs :", methode)
    ajuster_bras(parts, arm)
    noeud_dos(parts, s)
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
    os.makedirs(os.path.dirname(BLEND), exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=BLEND, compress=True)
    log("total : %d triangles (corps) + chope %d" % (triangles(corps), triangles(ch)))
    with open(os.path.join(os.path.dirname(BLEND), "bavaroise_pipeline.log"), "w", encoding="utf-8") as f:
        f.write("\n".join(JOURNAL) + "\n")

principal()
