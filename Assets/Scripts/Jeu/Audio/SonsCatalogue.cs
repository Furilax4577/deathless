using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Catalogue des sons, tiré de Wiki/data/sons.json par le menu Deathless > Jeu > Importer le catalogue des sons
    /// (tous les statuts sauf « a_creer »). Le jeu ne lit que cet asset ; relancer l'import quand le catalogue change.
    [CreateAssetMenu(menuName = "Deathless/Catalogue des sons", fileName = "SonsCatalogue")]
    public class SonsCatalogue : ScriptableObject
    {
        [Serializable]
        public class Entree
        {
            public string id;
            public string nom;
            public string statut;
            public bool boucle;
            public AudioClip[] clips;

            /// Portée en mètres (distance au-delà de laquelle on n'entend plus rien, § 3 du cahier des charges son) ;
            /// 0 = non précisée dans le catalogue, AudioBank applique alors sa valeur par défaut (60 m pour un effet,
            /// 45 m pour une boucle).
            public float portee;
        }

        public List<Entree> entrees = new List<Entree>();

        Dictionary<string, Entree> m_Index;

        public Entree Trouver(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (m_Index == null || m_Index.Count != entrees.Count)
            {
                m_Index = new Dictionary<string, Entree>();
                foreach (var e in entrees) if (e != null && !string.IsNullOrEmpty(e.id)) m_Index[e.id] = e;
            }
            return m_Index.TryGetValue(id, out var r) && r.clips != null && r.clips.Length > 0 ? r : null;
        }

        /// Première entrée présente parmi les ids donnés par ordre de préférence (son « à créer » puis repli).
        public Entree Premiere(string[] ids)
        {
            if (ids == null) return null;
            foreach (var id in ids)
            {
                var e = Trouver(id);
                if (e != null) return e;
            }
            return null;
        }
    }
}
