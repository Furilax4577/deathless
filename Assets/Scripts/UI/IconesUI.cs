using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI
{
    /// Table des icônes vectorielles de l'interface (Assets/UI/Resources/DeathlessIcones.asset) : identifiant (nom du
    /// SVG sans extension, ex. « paladin_epee ») → VectorImage importée par le module Vector Graphics (Assets/UI/Icones/).
    /// Remplie par Deathless > UI > 6. Table des icônes (SVG) ; les actions des classes pointent leur icône par son
    /// identifiant (ClassesJouables.Catalogue). Une VectorImage reste nette à toutes les tailles d'interface.
    [CreateAssetMenu(menuName = "Deathless/UI/Table des icônes", fileName = "DeathlessIcones")]
    public class IconesUI : ScriptableObject
    {
        // Icônes du HUD qui ne sont pas des actions de classe (identifiants des SVG).
        public const string Mana = "jauge_mana";
        public const string Rage = "jauge_rage";
        public const string Furtif = "assassin_furtif";
        public const string Potion = "commun_potion_soin";
        public const string Esquive = "commun_esquive";
        public const string CoupCritique = "commun_coup_critique";
        /// Emblème de repli d'une classe dont le SVG n'est pas encore là (hexagone vide, Assets/UI/Icones/Repli/).
        public const string RepliClasse = "repli_classe";

        [Serializable]
        public class Entree
        {
            public string id;
            public VectorImage image;
        }

        public List<Entree> icones = new List<Entree>();

        Dictionary<string, VectorImage> m_Index;

        static IconesUI s_Defaut;
        public static IconesUI Defaut => s_Defaut != null ? s_Defaut : (s_Defaut = Resources.Load<IconesUI>("DeathlessIcones"));

        /// Icône par identifiant (null si absente ou identifiant vide). Un emblème de classe manquant (« classe_… »)
        /// prend l'hexagone vide RepliClasse, jusqu'à l'arrivée de son SVG.
        public static VectorImage Trouver(string id)
        {
            var v = TrouverExact(id);
            if (v == null && id != null && id.StartsWith("classe_")) v = TrouverExact(RepliClasse);
            return v;
        }

        static VectorImage TrouverExact(string id)
        {
            var t = Defaut;
            if (t == null || string.IsNullOrEmpty(id)) return null;
            if (t.m_Index == null || t.m_Index.Count != t.icones.Count)
            {
                t.m_Index = new Dictionary<string, VectorImage>();
                foreach (var e in t.icones) if (e != null && !string.IsNullOrEmpty(e.id)) t.m_Index[e.id] = e.image;
            }
            return t.m_Index.TryGetValue(id, out var v) ? v : null;
        }

        /// Pose l'icône en fond d'un élément (masqué si l'icône manque). Renvoie vrai si elle existe.
        public static bool Poser(VisualElement element, string id)
        {
            var image = Trouver(id);
            element.style.backgroundImage = image != null ? new StyleBackground(image) : new StyleBackground(StyleKeyword.None);
            element.style.display = image != null ? DisplayStyle.Flex : DisplayStyle.None;
            return image != null;
        }

        /// Nouvel élément d'icône (classe USS « dl-icone » : l'image est ajustée à la boîte, sans déformation).
        public static VisualElement Creer(string id, string classe = null)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList("dl-icone");
            if (!string.IsNullOrEmpty(classe)) e.AddToClassList(classe);
            Poser(e, id);
            return e;
        }
    }
}
