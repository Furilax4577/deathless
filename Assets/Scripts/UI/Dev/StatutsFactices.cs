using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.UI.Dev
{
    /// Statuts factices du banc UIv01 (IEtatStatuts, posés par EtatFactice) : le joueur brûle, est ralenti et ivre ; deux
    /// ennemis fictifs, devant la caméra, brûlent ou sont étourdis. Les durées tournent en boucle (temps réel), pour voir
    /// les jauges et les secondes descendre. `Actif` faux : rien n'est affiché.
    public class StatutsFactices : IEtatStatuts
    {
        sealed class Statut : IStatutAffiche
        {
            public string Nom { get; set; }
            public string Icone { get; set; }
            public string Effet { get; set; }
            public string Source { get; set; }
            public float Duree { get; set; }
            public bool Nefaste { get; set; } = true;
            /// Décalage dans la boucle ; Duree <= 0 : sans durée.
            public float decalage;
            public float Restant => Duree > 0f ? Duree - Mathf.Repeat(Time.unscaledTime + decalage, Duree) : -1f;
        }

        sealed class Ennemi : IEnnemiAffecte
        {
            public Vector3 devant;   // position relative à la caméra : x à droite, y en haut, z devant
            public readonly List<IStatutAffiche> liste = new List<IStatutAffiche>();
            public Vector3 PositionTete
            {
                get
                {
                    var cam = Camera.main;
                    if (cam == null) return devant;
                    var t = cam.transform;
                    return t.position + t.right * devant.x + t.up * devant.y + t.forward * devant.z;
                }
            }
            public bool Visible => true;
            public IReadOnlyList<IStatutAffiche> Statuts => liste;
        }

        public bool Actif = true;

        readonly List<IStatutAffiche> m_Joueur = new List<IStatutAffiche>();
        readonly List<IEnnemiAffecte> m_Ennemis = new List<IEnnemiAffecte>();
        static readonly List<IStatutAffiche> s_Vide = new List<IStatutAffiche>();
        static readonly List<IEnnemiAffecte> s_Aucun = new List<IEnnemiAffecte>();

        public StatutsFactices()
        {
            var brulure = new Statut { Nom = "Brûlure", Icone = "statut_brulure", Effet = "5 dégâts par seconde. Chaque nouveau coup de feu relance la durée.", Source = "Morgane (Mage)", Duree = 3f };
            var ralenti = new Statut { Nom = "Ralenti", Icone = "statut_ralenti", Effet = "Déplacements ralentis de 40 %.", Source = "Chute", Duree = 6f, decalage = 1.3f };
            var ivresse = new Statut { Nom = "Ivresse", Icone = "statut_ivresse", Effet = "La tête tourne : la vue tangue et la démarche hésite. Attaques et visée inchangées.", Source = "Taverne", Duree = 15f, Nefaste = false, decalage = 4f };
            m_Joueur.Add(brulure);
            m_Joueur.Add(ralenti);
            m_Joueur.Add(ivresse);

            var e1 = new Ennemi { devant = new Vector3(-2.5f, -0.2f, 9f) };
            e1.liste.Add(new Statut { Nom = "Brûlure", Icone = "statut_brulure", Effet = "", Source = "Morgane (Mage)", Duree = 3f, decalage = 0.7f });
            e1.liste.Add(new Statut { Nom = "Ralenti", Icone = "statut_ralenti", Effet = "", Source = "", Duree = 5f, decalage = 2f });
            var e2 = new Ennemi { devant = new Vector3(3f, 0.3f, 12f) };
            e2.liste.Add(new Statut { Nom = "Étourdi", Icone = "statut_etourdi", Effet = "", Source = "Quentin (Paladin)", Duree = 2.5f });
            e2.liste.Add(new Statut { Nom = "Provoqué", Icone = "statut_provoque", Effet = "", Source = "Bjorn le Rouge (Viking)", Duree = 5f, decalage = 1f });
            m_Ennemis.Add(e1);
            m_Ennemis.Add(e2);
        }

        public IReadOnlyList<IStatutAffiche> StatutsJoueur => Actif ? m_Joueur : s_Vide;
        public IReadOnlyList<IEnnemiAffecte> EnnemisAffectes => Actif ? m_Ennemis : s_Aucun;
    }
}
