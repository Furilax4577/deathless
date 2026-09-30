using Deathless.Jeu;
using UnityEditor;
using UnityEngine;

namespace Deathless.EditeurJeu
{
    /// Boss (30/09/2026) : grimoire de Nyxar. Le prefab Squelette_Necromancien est construit par Deathless > Jeu > 4 ;
    /// ce menu y ajoute la référence au modèle du grimoire porté à la ceinture (KayKit spellbook_closed), instancié à
    /// l'apparition par Necromancien.CreerEclats avec son éclat de Nyx. Relançable ; à relancer après le menu 4.
    public static class BossBuilder
    {
        const string PrefabNyxar = "Assets/Jeu/Prefabs/Squelette_Necromancien.prefab";
        const string Grimoire = "Assets/Art/KayKit/KayKit_Adventurers_2.0_FREE/Assets/fbx(unity)/spellbook_closed.fbx";

        [MenuItem("Deathless/Jeu/14. Boss (grimoire de Nyxar)")]
        public static string Construire()
        {
            var livre = AssetDatabase.LoadAssetAtPath<GameObject>(Grimoire);
            if (livre == null) return "Grimoire introuvable : " + Grimoire;
            var racine = PrefabUtility.LoadPrefabContents(PrefabNyxar);
            try
            {
                var n = racine.GetComponent<Necromancien>();
                if (n == null) return "Pas de Necromancien sur " + PrefabNyxar;
                n.modeleGrimoire = livre;
                PrefabUtility.SaveAsPrefabAsset(racine, PrefabNyxar);
            }
            finally { PrefabUtility.UnloadPrefabContents(racine); }
            string r = "Nyxar : grimoire posé sur " + PrefabNyxar;
            Debug.Log(r);
            return r;
        }
    }
}
