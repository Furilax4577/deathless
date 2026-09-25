using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Registre des classes jouables (Assets/Jeu/Resources/ClassesJeu.asset, rempli par Deathless > Jeu > Classes) :
    /// identité, modèle, style d'arme, contrôleur et prefab du héros de chaque classe. Les valeurs chiffrées sont dans
    /// GameBalance ; les textes de l'écran de choix dans ClassesJouables.Catalogue (agent ui).
    [CreateAssetMenu(menuName = "Deathless/Classes jouables", fileName = "ClassesJeu")]
    public class ClassesJeu : ScriptableObject
    {
        [Serializable]
        public class ClasseDef
        {
            public string id;
            public string nom;
            [Tooltip("Style de magie ou élément de la classe (Mage : « feu », le seul pour l'instant) ; vide pour les autres.")]
            public string element;
            public GameObject modele;
            public WeaponStyle style;
            public RuntimeAnimatorController controleur;
            public GameObject prefab;
        }

        public List<ClasseDef> classes = new List<ClasseDef>();

        public ClasseDef Trouver(string id)
        {
            foreach (var c in classes) if (c != null && c.id == id) return c;
            return null;
        }

        static ClassesJeu s_Courant;
        public static ClassesJeu Courant => s_Courant != null ? s_Courant : (s_Courant = Resources.Load<ClassesJeu>("ClassesJeu"));
    }
}
