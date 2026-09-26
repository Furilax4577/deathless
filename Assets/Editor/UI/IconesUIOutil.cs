using System.IO;
using Deathless.UI;
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.EditorUI
{
    /// Icônes SVG de l'interface (Assets/UI/Icones/, sources dans ArtSources/Icones/) :
    /// - à l'import, chaque SVG devient une VectorImage pour UI Toolkit (module Vector Graphics intégré à Unity 6), avec
    ///   le cadre 128 × 128 du fichier conservé (PreserveViewport) : toutes les icônes ont la même échelle ;
    /// - la table Assets/UI/Resources/DeathlessIcones.asset (IconesUI) est reconstruite quand un SVG est ajouté,
    ///   supprimé ou déplacé dans ce dossier, ou par le menu Deathless > UI > 6. Table des icônes (SVG).
    public class IconesUIOutil : AssetPostprocessor
    {
        public const string Dossier = "Assets/UI/Icones";
        public const string CheminTable = "Assets/UI/Resources/DeathlessIcones.asset";

        static bool EstIcone(string chemin) => chemin.StartsWith(Dossier + "/") && chemin.EndsWith(".svg");

        void OnPreprocessAsset()
        {
            if (!EstIcone(assetPath) || !(assetImporter is SVGImporter svg)) return;
            svg.SvgType = SVGType.VectorImage;
            svg.ViewportOptions = Unity.VectorGraphics.ViewportOptions.PreserveViewport;
        }

        static void OnPostprocessAllAssets(string[] importes, string[] supprimes, string[] deplaces, string[] anciens)
        {
            var change = false;
            foreach (var c in supprimes) change |= EstIcone(c);
            foreach (var c in deplaces) change |= EstIcone(c);
            foreach (var c in anciens) change |= EstIcone(c);
            foreach (var c in importes)
                if (EstIcone(c) && (IconesUI.Defaut == null || IconesUI.Trouver(Path.GetFileNameWithoutExtension(c)) == null)) change = true;
            if (change) EditorApplication.delayCall += Reconstruire;
        }

        /// Copie dans Assets/UI/Icones/ les SVG nouveaux ou modifiés de ArtSources/Icones/ (Classes : sauf les variantes
        /// « classe_x_a.svg » ; Competences : sauf les icônes provisoires du druide « druide_* » ; Emotes : icônes de la
        /// roue à emotes, generer_emotes.py). Faite au chargement de
        /// l'éditeur et par le menu : un emblème qui arrive dans ArtSources (ex. classe_mecanicien) remplace le repli.
        [InitializeOnLoadMethod]
        static void SynchroniserAuChargement() => EditorApplication.delayCall += () => { if (Synchroniser() > 0) AssetDatabase.Refresh(); };

        public static int Synchroniser()
        {
            var racine = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ArtSources", "Icones"));
            var copies = 0;
            foreach (var sous in new[] { "Classes", "Competences", "Emotes" })
            {
                var source = Path.Combine(racine, sous);
                if (!Directory.Exists(source)) continue;
                var cible = Path.Combine(Application.dataPath, "UI", "Icones", sous);
                Directory.CreateDirectory(cible);
                foreach (var fichier in Directory.GetFiles(source, "*.svg"))
                {
                    var nom = Path.GetFileNameWithoutExtension(fichier);
                    if (sous == "Classes" && System.Text.RegularExpressions.Regex.IsMatch(nom, "^classe_.+_[a-z]$")) continue;
                    if (sous == "Competences" && nom.StartsWith("druide_")) continue;
                    var dest = Path.Combine(cible, nom + ".svg");
                    if (File.Exists(dest) && File.ReadAllText(dest) == File.ReadAllText(fichier)) continue;
                    File.Copy(fichier, dest, true);
                    copies++;
                }
            }
            if (copies > 0) Debug.Log("[Icônes] " + copies + " SVG copiés depuis ArtSources/Icones.");
            return copies;
        }

        [MenuItem("Deathless/UI/6. Table des icônes (SVG)")]
        public static void Menu()
        {
            if (Synchroniser() > 0) AssetDatabase.Refresh();
            Reconstruire();
        }

        public static void Reconstruire()
        {
            var table = AssetDatabase.LoadAssetAtPath<IconesUI>(CheminTable);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<IconesUI>();
                AssetDatabase.CreateAsset(table, CheminTable);
            }
            table.icones.Clear();
            foreach (var guid in AssetDatabase.FindAssets("", new[] { Dossier }))
            {
                var chemin = AssetDatabase.GUIDToAssetPath(guid);
                if (!chemin.EndsWith(".svg")) continue;
                var image = AssetDatabase.LoadAssetAtPath<VectorImage>(chemin);
                if (image == null) continue;
                table.icones.Add(new IconesUI.Entree { id = Path.GetFileNameWithoutExtension(chemin), image = image });
            }
            table.icones.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log("[Icônes] Table reconstruite : " + table.icones.Count + " icônes (" + CheminTable + ").");
        }
    }
}
