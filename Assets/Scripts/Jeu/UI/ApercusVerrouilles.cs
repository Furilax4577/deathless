using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Modèles des classes à venir (Druide, Mécanicien) pour l'aperçu 3D de l'écran de choix : elles n'ont pas encore de
    /// prefab de héros dans ClassesJeu. Asset : Assets/Jeu/Resources/ApercusVerrouilles.asset (lu par ApercuClasse).
    [CreateAssetMenu(menuName = "Deathless/Aperçus des classes verrouillées", fileName = "ApercusVerrouilles")]
    public class ApercusVerrouilles : ScriptableObject
    {
        [Serializable]
        public class Entree
        {
            public string id;
            [Tooltip("Personnage KayKit (squelette Rig_Medium).")]
            public GameObject modele;
            [Tooltip("Arme posée sous l'os `os` (facultatif).")]
            public GameObject arme;
            public string os = "handslot.r";
            [Tooltip("Pose de repos jouée en boucle (Idle_A de Rig_Medium_General).")]
            public AnimationClip repos;
        }

        public List<Entree> entrees = new List<Entree>();

        public Entree Trouver(string id)
        {
            foreach (var e in entrees) if (e != null && e.id == id) return e;
            return null;
        }

        static ApercusVerrouilles s_Courant;
        public static ApercusVerrouilles Courant => s_Courant != null ? s_Courant : (s_Courant = Resources.Load<ApercusVerrouilles>("ApercusVerrouilles"));
    }
}
