using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Types de statut (wiki : statuts.md). La valeur sert aussi d'identifiant réseau (octet) : ne pas renuméroter.
    public enum TypeStatut : byte { Aucun = 0, Brulure = 1, Ralenti = 2, Etourdi = 3, Ivresse = 4, Provoque = 5 }

    /// D'où vient un statut (infobulle du menu du personnage : « Source »).
    public enum OrigineStatut : byte { Inconnue = 0, Joueur = 1, Ennemi = 2, Chute = 3, Taverne = 4, Eau = 5 }

    /// Ce que fait un nouveau statut quand la cible a déjà le même.
    /// Rafraichir : la durée repart de zéro (brûlure). Prolonger : la fin la plus lointaine l'emporte (étourdissement,
    /// ralenti, ivresse). Additionner : les durées s'ajoutent. Remplacer : le nouveau prend toute la place (provocation).
    /// L'intensité retenue est toujours la plus forte, sauf pour Remplacer.
    public enum RegleCumul : byte { Rafraichir = 0, Prolonger = 1, Additionner = 2, Remplacer = 3 }

    /// Un statut posé sur un personnage (héros ou ennemi).
    [Serializable]
    public struct Statut
    {
        public TypeStatut type;
        /// Brûlure : dégâts par seconde ; Ralenti : part de vitesse retirée (0,4 = −40 %) ; autres : 1.
        public float intensite;
        /// Durée posée lors du dernier (re)lancement : sert à la jauge (restant / durée).
        public float duree;
        /// Instant de fin (Time.time de ce poste) ; +∞ : sans durée (statut de zone).
        public float fin;
        public OrigineStatut origine;
        /// Joueur à l'origine (origine Joueur), sinon 0.
        public int sourceId;
        /// Propriétaire d'un héros client : appliqué ici avant la réponse de l'hôte (prédiction).
        public bool predit;
        public float preditDepuis;

        public bool Permanent => float.IsPositiveInfinity(fin);
        /// Secondes restantes, ou -1 sans durée.
        public float Restant => Permanent ? -1f : Mathf.Max(0f, fin - Time.time);
    }

    /// Statuts d'un personnage (héros ou ennemi) : liste, règles de cumul, expiration, effets communs (dégâts de la
    /// brûlure, flammèches, facteur de vitesse du ralenti). Le poste qui fait foi (l'hôte, ou ce poste en solo) décide :
    /// un client envoie ses demandes par `relais` et reçoit la liste de l'hôte par `Recevoir` (EnnemiReseau, HerosReseau,
    /// voir Docs/reseau.md, « Statuts »). Le reste du jeu garde sa logique propre (étourdissement des squelettes et du
    /// héros, ivresse de la caméra) et pose ici le statut qui la décrit, pour l'affichage et le réseau.
    [DisallowMultipleComponent]
    public class Statuts : MonoBehaviour
    {
        /// Statuts ayant au moins une entrée (ou un effet encore visible), tous postes : pour l'affichage au-dessus des ennemis.
        static readonly List<Statuts> s_Actifs = new List<Statuts>();
        public static IReadOnlyList<Statuts> Actifs => s_Actifs;

        /// Tests réseau : listes reçues de l'hôte par ce poste (changements seulement).
        public static int ListesRecues { get; private set; }
        /// Tests réseau : demandes envoyées à l'hôte par ce poste.
        public static int DemandesEnvoyees { get; private set; }

        /// Délai minimal entre deux demandes du même type à l'hôte (un cône de flammes rafraîchit la brûlure 4 fois par seconde).
        public const float IntervalleDemandes = 0.4f;
        /// Délai de grâce d'une prédiction qui n'est pas encore revenue de l'hôte.
        public const float GracePrediction = 1.5f;

        readonly List<Statut> m_Liste = new List<Statut>();
        readonly Dictionary<TypeStatut, float> m_DernieresDemandes = new Dictionary<TypeStatut, float>();
        Sante m_Sante;
        float m_AccuBrulure;
        bool m_FlammesVisibles;

        /// Faux chez un client réseau (l'hôte fait foi) : `Ajouter` passe par `relais`, s'il y en a un.
        public bool Autorite { get; set; } = true;
        /// Client : envoie une demande à l'hôte (type, durée, intensité, origine, joueur source).
        public Action<Statut> relais;
        /// Client propriétaire de ce héros : les statuts qu'il demande s'appliquent aussitôt ici (prédiction).
        public bool predire;

        /// Autorité : la liste vient de changer (ajout, rafraîchissement, retrait, fin). Le réseau l'écrit alors.
        public event Action Change;

        public IReadOnlyList<Statut> Liste => m_Liste;
        public int Nombre => m_Liste.Count;
        public Sante Sante => m_Sante != null ? m_Sante : (m_Sante = GetComponent<Sante>());

        /// Le composant Statuts du personnage (ajouté s'il manque).
        public static Statuts De(Component c)
        {
            if (c == null) return null;
            var s = c.GetComponent<Statuts>();
            if (s == null) s = c.gameObject.AddComponent<Statuts>();
            return s;
        }

        void Awake()
        {
            m_Sante = GetComponent<Sante>();
            enabled = false;   // réveillé par le premier statut
        }

        void OnEnable() { if (!s_Actifs.Contains(this)) s_Actifs.Add(this); }
        void OnDisable() { s_Actifs.Remove(this); }
        void OnDestroy() { s_Actifs.Remove(this); if (m_FlammesVisibles) Brulure.Montrer(this, false); }

        // ----------------------------------------------------------------- Lecture

        public bool A(TypeStatut type)
        {
            for (int i = 0; i < m_Liste.Count; i++) if (m_Liste[i].type == type) return true;
            return false;
        }

        /// Intensité la plus forte de ce type (0 s'il est absent).
        public float Intensite(TypeStatut type)
        {
            float v = 0f;
            for (int i = 0; i < m_Liste.Count; i++) if (m_Liste[i].type == type) v = Mathf.Max(v, m_Liste[i].intensite);
            return v;
        }

        /// Facteur de vitesse des statuts (ralenti) : 1 sans ralentissement. L'eau du donjon est à part (ZoneEau).
        public float FacteurVitesse
        {
            get
            {
                float r = Intensite(TypeStatut.Ralenti);
                return r > 0f ? Mathf.Clamp(1f - r, 0.1f, 1f) : 1f;
            }
        }

        /// Facteur de l'eau du donjon aux pieds de ce personnage (1 hors de l'eau) : affiché comme un ralenti sans durée.
        public float FacteurEau => Deathless.Donjon.ZoneEau.FacteurEn(transform.position + Vector3.up * 0.2f);

        // ----------------------------------------------------------------- Écriture

        /// Pose (ou cumule selon la règle du type) un statut. Chez un client, c'est une demande à l'hôte.
        public void Ajouter(TypeStatut type, float duree, float intensite, OrigineStatut origine, int sourceId = 0)
        {
            if (type == TypeStatut.Aucun || duree <= 0f) return;
            if (Sante != null && Sante.Mort) return;
            var s = new Statut { type = type, duree = duree, intensite = intensite, origine = origine, sourceId = sourceId, fin = Time.time + duree };
            if (!Autorite)
            {
                if (relais == null) return;   // copie d'un personnage tenu ailleurs, sans droit de demande
                if (predire) { s.predit = true; s.preditDepuis = Time.time; Appliquer(s); }
                m_DernieresDemandes.TryGetValue(type, out float derniere);
                if (Time.time - derniere < IntervalleDemandes && derniere > 0f) return;
                m_DernieresDemandes[type] = Time.time;
                DemandesEnvoyees++;
                relais(s);
                return;
            }
            if (Appliquer(s)) Change?.Invoke();
        }

        /// Autorité : demande reçue d'un client (déjà validée par le réseau).
        public void AjouterDemande(Statut s) => Ajouter(s.type, s.duree, s.intensite, s.origine, s.sourceId);

        /// Retire tous les statuts de ce type (autorité ; chez un client, retrait local d'une prédiction seulement).
        public void Retirer(TypeStatut type)
        {
            bool change = false;
            for (int i = m_Liste.Count - 1; i >= 0; i--)
                if (m_Liste[i].type == type && (Autorite || m_Liste[i].predit)) { m_Liste.RemoveAt(i); change = true; }
            if (change) Apres(Autorite);
        }

        /// Retire tout (mort, fin de partie).
        public void Vider()
        {
            if (m_Liste.Count == 0) return;
            m_Liste.Clear();
            Apres(Autorite);
        }

        /// Règles de cumul. Renvoie vrai si la liste a changé.
        bool Appliquer(Statut s)
        {
            var def = CatalogueStatuts.De(s.type);
            var regle = def != null ? def.regle : RegleCumul.Prolonger;
            int i = m_Liste.FindIndex(x => x.type == s.type && !x.Permanent);
            if (i < 0)
            {
                m_Liste.Add(s);
                enabled = true;
                return true;
            }
            var e = m_Liste[i];
            switch (regle)
            {
                case RegleCumul.Rafraichir:
                    e.fin = s.fin; e.duree = s.duree;
                    e.intensite = Mathf.Max(e.intensite, s.intensite);
                    e.origine = s.origine; e.sourceId = s.sourceId;
                    break;
                case RegleCumul.Prolonger:
                    if (s.fin > e.fin) { e.fin = s.fin; e.duree = s.duree; e.origine = s.origine; e.sourceId = s.sourceId; }
                    e.intensite = Mathf.Max(e.intensite, s.intensite);
                    break;
                case RegleCumul.Additionner:
                    e.fin += s.duree; e.duree = Mathf.Max(0.01f, e.fin - Time.time);
                    e.intensite = Mathf.Max(e.intensite, s.intensite);
                    break;
                default:
                    e = s;
                    break;
            }
            e.predit = s.predit; e.preditDepuis = s.preditDepuis;
            m_Liste[i] = e;
            return true;
        }

        /// Client : liste de l'hôte (fins déjà converties en Time.time de ce poste). Les prédictions encore en attente
        /// (type absent de la liste de l'hôte, moins de GracePrediction secondes) sont gardées.
        public void Recevoir(List<Statut> hote)
        {
            ListesRecues++;
            float t = Time.time;
            for (int i = 0; i < m_Liste.Count; i++)
            {
                var l = m_Liste[i];
                if (!l.predit || t - l.preditDepuis > GracePrediction) continue;
                bool present = false;
                for (int k = 0; k < hote.Count; k++) if (hote[k].type == l.type) { present = true; break; }
                if (!present) hote.Add(l);
            }
            m_Liste.Clear();
            m_Liste.AddRange(hote);
            if (m_Liste.Count > 0 || m_FlammesVisibles) enabled = true;   // réveillé aussi pour éteindre les flammèches
        }

        void Apres(bool signaler)
        {
            if (signaler) Change?.Invoke();
            if (m_Liste.Count == 0 && !m_FlammesVisibles) enabled = false;
        }

        // ----------------------------------------------------------------- Boucle

        void Update()
        {
            float t = Time.time;
            bool change = false;
            for (int i = m_Liste.Count - 1; i >= 0; i--)
            {
                var s = m_Liste[i];
                if (!s.Permanent && s.fin <= t) { m_Liste.RemoveAt(i); change = true; }
            }
            var sante = Sante;
            if (sante != null && sante.Mort && m_Liste.Count > 0) { m_Liste.Clear(); change = true; }

            // Brûlure : dégâts continus, comptés au joueur qui l'a posée (le poste qui fait foi seulement).
            bool brule = A(TypeStatut.Brulure);
            if (brule && Autorite && sante != null && !sante.Mort && sante.isActiveAndEnabled) Bruler(sante);
            else m_AccuBrulure = 0f;
            if (brule != m_FlammesVisibles) { m_FlammesVisibles = brule; Brulure.Montrer(this, brule); }

            if (change && Autorite) Change?.Invoke();
            if (m_Liste.Count == 0 && !m_FlammesVisibles) enabled = false;
        }

        void Bruler(Sante sante)
        {
            m_AccuBrulure += Intensite(TypeStatut.Brulure) * Time.deltaTime;
            if (m_AccuBrulure < 1f) return;
            float d = Mathf.Floor(m_AccuBrulure);
            m_AccuBrulure -= d;
            int id = 0;
            for (int i = 0; i < m_Liste.Count; i++) if (m_Liste[i].type == TypeStatut.Brulure) { id = m_Liste[i].sourceId; break; }
            var p = Partie.Instance;
            var source = p != null && id > 0 ? p.HerosDe(id) : null;
            float reel = sante.Encaisser(new InfoDegats
            {
                montant = d, sourceId = id, equipeSource = Equipe.Heros, source = source != null ? source.gameObject : null,
                point = sante.transform.position + Vector3.up, continu = true
            });
            if (reel > 0f && id > 0 && p != null) p.CompterDegats(id, reel, false);
        }
    }
}
