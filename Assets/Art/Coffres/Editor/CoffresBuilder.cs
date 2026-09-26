using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Deathless.Coffres
{
    /// Coffres du donjon dérivés des modèles KayKit Dungeon `chest` et `chest_gold` (les FBX ne sont jamais modifiés).
    ///
    /// - **Sans serrure** (`Coffre`, `GrandCoffre`) : ouverture gratuite, cas de tous les coffres du jeu pour l'instant
    ///   (décision de Quentin, 26/09/2026). Copies des maillages du corps et du couvercle sans les deux pièces de serrure :
    ///   la gâche sur le corps et le moraillon à trou de serrure sur le couvercle. Ce sont des îlots de triangles
    ///   (composantes connexes) centrés en x = 0 sur la face avant ; même méthode que ForgeronBuilder.RetirerEcharpe (main).
    /// - **À clé** (`Coffre_Acier`, `_Cuivre`, `_Or`, et `GrandCoffre_*`) : maillages d'origine, serrure comprise, pas de
    ///   cadenas. Tout le métal du coffre (ferrures, coins, clous, serrure) utilise une seule case de l'atlas
    ///   `dungeon_texture` (colonne 1, ligne 0) : une copie de l'atlas où seule cette case est teintée de la couleur de la
    ///   clé (matériau des prefabs `Cle_*` de Assets/Art/Cadenas), et un matériau par métal. Le bois garde l'atlas KayKit.
    ///   Le couvercle porte un repère `Serrure` au trou de serrure (Z = sens d'insertion de la clé, Y = côté du panneton),
    ///   même convention que CadenasOuverture.
    ///
    /// Tout est sous Assets/Art/Coffres (maillages, textures, matériaux, prefabs, ce script), prévu pour être copié tel
    /// quel dans main (mêmes chemins et GUID). Dépendances : FBX et atlas KayKit Dungeon, Assets/Art/Materials/
    /// KayKit_Dungeon.mat (mêmes GUID dans main), prefabs Cle_* (couleurs lues à la génération seulement).
    public static class CoffresBuilder
    {
        public const string Dossier = "Assets/Art/Coffres";
        const string Kit = "Assets/Art/KayKit/KayKit_Dungeon_Pack_1.1_FREE/Assets/fbx(unity)/";
        const string Atlas = Kit + "dungeon_texture.png";
        const string MateriauKayKit = "Assets/Art/Materials/KayKit_Dungeon.mat";
        static readonly string[] Metaux = { "Acier", "Cuivre", "Or" };

        /// Case du métal dans l'atlas 1024 x 1024 (8 x 4 cases de 128 x 256 px) : colonne 1, ligne du haut.
        static readonly RectInt CaseMetal = new RectInt(128, 768, 128, 256);
        /// Trou de serrure du moraillon, dans le repère du couvercle (face avant à z = 1,265 ; creux jusqu'à 1,185).
        public static readonly Vector3 TrouSerrure = new Vector3(0f, 0.06f, 1.265f);

        [MenuItem("Deathless/Coffres/Générer")]
        static void Menu() { Debug.Log(Generer()); }

        public static string Generer()
        {
            var sb = new StringBuilder();
            foreach (var d in new[] { "Maillages", "Textures", "Materiaux", "Prefabs" })
                if (!AssetDatabase.IsValidFolder(Dossier + "/" + d)) AssetDatabase.CreateFolder(Dossier, d);

            var materiaux = new Dictionary<string, Material>();
            foreach (var metal in Metaux) materiaux[metal] = MateriauMetal(metal, sb);
            var kaykit = AssetDatabase.LoadAssetAtPath<Material>(MateriauKayKit);

            foreach (var (modele, nom) in new[] { ("chest", "Coffre"), ("chest_gold", "GrandCoffre") })
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + modele + ".fbx");
                var corps = fbx.GetComponent<MeshFilter>().sharedMesh; // corps sur la racine du FBX
                var couvercleT = fbx.transform.Find(modele + "_lid");
                var couvercle = couvercleT.GetComponent<MeshFilter>().sharedMesh;
                var corpsSans = SansSerrure(corps, Dossier + "/Maillages/" + nom + "_Corps_SansSerrure.asset", sb);
                var couvercleSans = SansSerrure(couvercle, Dossier + "/Maillages/" + nom + "_Couvercle_SansSerrure.asset", sb);
                Prefab(nom, modele, corpsSans, couvercleSans, couvercleT, kaykit, false);
                foreach (var metal in Metaux) Prefab(nom + "_" + metal, modele, corps, couvercle, couvercleT, materiaux[metal], true);
                sb.AppendLine("Prefabs : " + nom + ", " + nom + "_Acier, _Cuivre, _Or");
            }
            AssetDatabase.SaveAssets();
            return sb.ToString();
        }

        // ------------------------------------------------------------------ serrure retirée

        /// Copie du maillage sans les îlots de serrure : composantes (sommets soudés) autres que la plus grande, centrées
        /// en x (|x| < 5 cm), étroites (< 0,6 m) et sur la face avant (z du centre > 80 % du z maximal du maillage).
        static Mesh SansSerrure(Mesh src, string chemin, StringBuilder sb)
        {
            var tris = src.triangles;
            var v = src.vertices;
            int n = v.Length;
            var parent = new int[n];
            for (int i = 0; i < n; i++) parent[i] = i;
            int Racine(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
            void Unir(int a, int b) { a = Racine(a); b = Racine(b); if (a != b) parent[a] = b; }
            var soude = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < n; i++)
            {
                var k = new Vector3Int(Mathf.RoundToInt(v[i].x * 10000f), Mathf.RoundToInt(v[i].y * 10000f), Mathf.RoundToInt(v[i].z * 10000f));
                if (soude.TryGetValue(k, out int j)) Unir(i, j); else soude[k] = i;
            }
            for (int t = 0; t < tris.Length; t += 3) { Unir(tris[t], tris[t + 1]); Unir(tris[t + 1], tris[t + 2]); }
            var boites = new Dictionary<int, Bounds>();
            var nb = new Dictionary<int, int>();
            for (int t = 0; t < tris.Length; t += 3)
            {
                int r = Racine(tris[t]);
                var c = (v[tris[t]] + v[tris[t + 1]] + v[tris[t + 2]]) / 3f;
                if (boites.TryGetValue(r, out var b)) { b.Encapsulate(c); boites[r] = b; nb[r]++; }
                else { boites[r] = new Bounds(c, Vector3.zero); nb[r] = 1; }
            }
            int plus = -1;
            foreach (var kv in nb) if (plus < 0 || kv.Value > nb[plus]) plus = kv.Key;
            float zMax = src.bounds.max.z;
            var retirees = new HashSet<int>();
            foreach (var kv in boites)
            {
                var b = kv.Value;
                if (kv.Key != plus && Mathf.Abs(b.center.x) < 0.05f && b.size.x < 0.6f && b.center.z > 0.8f * zMax) retirees.Add(kv.Key);
            }
            var garde = new List<int>(tris.Length);
            int enleves = 0;
            for (int t = 0; t < tris.Length; t += 3)
            {
                if (retirees.Contains(Racine(tris[t]))) { enleves++; continue; }
                garde.Add(tris[t]); garde.Add(tris[t + 1]); garde.Add(tris[t + 2]);
            }
            var copie = AssetDatabase.LoadAssetAtPath<Mesh>(chemin);
            if (copie == null) { copie = Object.Instantiate(src); AssetDatabase.CreateAsset(copie, chemin); }
            else EditorUtility.CopySerialized(src, copie);
            copie.name = Path.GetFileNameWithoutExtension(chemin);
            copie.subMeshCount = 1;
            copie.SetTriangles(garde, 0);
            copie.RecalculateBounds();
            EditorUtility.SetDirty(copie);
            sb.AppendLine(copie.name + " : " + enleves + " triangles de serrure retirés sur " + tris.Length / 3 + " (" + retirees.Count + " îlots)");
            return copie;
        }

        // ------------------------------------------------------------------ métal teinté

        static Color CouleurCle(string metal)
        {
            var cle = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Cadenas/Prefabs/Cle_" + metal + ".prefab");
            var r = cle != null ? cle.GetComponentInChildren<Renderer>() : null;
            if (r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor")) return r.sharedMaterial.GetColor("_BaseColor");
            return metal == "Or" ? new Color(1f, 0.737f, 0.314f) : metal == "Cuivre" ? new Color(0.886f, 0.596f, 0.471f) : new Color(0.541f, 0.635f, 0.702f);
        }

        /// Copie de l'atlas dont la case du métal reprend la couleur de la clé, en gardant le dégradé KayKit (clair en
        /// haut, sombre en bas, liseré de la case) : luminance d'origine normalisée → 0,62 à 1,12 fois la couleur de la clé.
        static Material MateriauMetal(string metal, StringBuilder sb)
        {
            string cheminTex = Dossier + "/Textures/Coffre_Atlas_" + metal + ".png";
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(Atlas));
            var px = tex.GetPixels32();
            int w = tex.width;
            float lmin = 1f, lmax = 0f;
            for (int y = CaseMetal.yMin; y < CaseMetal.yMax; y++)
                for (int x = CaseMetal.xMin; x < CaseMetal.xMax; x++)
                {
                    float l = ((Color)px[y * w + x]).grayscale;
                    lmin = Mathf.Min(lmin, l); lmax = Mathf.Max(lmax, l);
                }
            var cle = CouleurCle(metal);
            for (int y = CaseMetal.yMin; y < CaseMetal.yMax; y++)
                for (int x = CaseMetal.xMin; x < CaseMetal.xMax; x++)
                {
                    float l = (((Color)px[y * w + x]).grayscale - lmin) / Mathf.Max(1e-4f, lmax - lmin);
                    var c = cle * Mathf.Lerp(0.62f, 1.12f, l);
                    c.a = 1f;
                    px[y * w + x] = (Color32)new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
                }
            tex.SetPixels32(px);
            File.WriteAllBytes(cheminTex, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(cheminTex);
            var src = (TextureImporter)AssetImporter.GetAtPath(Atlas);
            var imp = (TextureImporter)AssetImporter.GetAtPath(cheminTex);
            imp.filterMode = src.filterMode;
            imp.mipmapEnabled = src.mipmapEnabled;
            imp.textureCompression = src.textureCompression;
            imp.sRGBTexture = src.sRGBTexture;
            imp.maxTextureSize = src.maxTextureSize;
            imp.wrapMode = src.wrapMode;
            imp.SaveAndReimport();

            string cheminMat = Dossier + "/Materiaux/Coffre_" + metal + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(cheminMat);
            if (mat == null)
            {
                mat = new Material(AssetDatabase.LoadAssetAtPath<Material>(MateriauKayKit));
                AssetDatabase.CreateAsset(mat, cheminMat);
            }
            else mat.CopyPropertiesFromMaterial(AssetDatabase.LoadAssetAtPath<Material>(MateriauKayKit));
            var t2 = AssetDatabase.LoadAssetAtPath<Texture2D>(cheminTex);
            mat.mainTexture = t2;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", t2);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", metal == "Or" ? 0.3f : 0.2f);
            EditorUtility.SetDirty(mat);
            sb.AppendLine("Coffre_" + metal + " : case métal teintée " + ColorUtility.ToHtmlStringRGB(cle));
            return mat;
        }

        // ------------------------------------------------------------------ prefabs

        /// Même hiérarchie que le FBX (corps `chest`, couvercle `chest_lid` : DonjonJeu.CoffreDe cherche un enfant « _lid »).
        static void Prefab(string nom, string modele, Mesh corps, Mesh couvercle, Transform couvercleFbx, Material mat, bool serrure)
        {
            var racine = new GameObject(nom);
            try
            {
                var c = new GameObject(modele);
                c.transform.SetParent(racine.transform, false);
                c.AddComponent<MeshFilter>().sharedMesh = corps;
                c.AddComponent<MeshRenderer>().sharedMaterial = mat;
                var l = new GameObject(modele + "_lid");
                l.transform.SetParent(racine.transform, false);
                l.transform.localPosition = couvercleFbx.localPosition;
                l.transform.localRotation = couvercleFbx.localRotation;
                l.transform.localScale = couvercleFbx.localScale;
                l.AddComponent<MeshFilter>().sharedMesh = couvercle;
                l.AddComponent<MeshRenderer>().sharedMaterial = mat;
                if (serrure)
                {
                    var s = new GameObject("Serrure").transform;
                    s.SetParent(l.transform, false);
                    s.localPosition = TrouSerrure;
                    s.localRotation = Quaternion.LookRotation(Vector3.back, Vector3.down);
                }
                PrefabUtility.SaveAsPrefabAsset(racine, Dossier + "/Prefabs/" + nom + ".prefab");
            }
            finally { Object.DestroyImmediate(racine); }
        }
    }
}
