using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Types de statut (wiki : statuts.md). La valeur sert aussi d'identifiant réseau (octet) : ne pas renuméroter.
    /// Renverse (26/09/2026) : chute à la renverse, sans contrôle (Heros.Etat.Renverse porte le déroulé — chute, au sol,
    /// relevé — ; ce statut n'est que l'affichage réseau/HUD, comme Etourdi).
    /// PeauDeFer (27/09/2026) : bienfait du rugissement du viking, part des dégâts subis retirée (intensité 0,35 = −35 %),
    /// appliquée par Sante.absorbeur du héros (Heros.Initialiser).
    /// Galvanise (30/09/2026) : bienfait du cri de Morgrim sur les squelettes proches, dégâts portés +intensité (0,3 = +30 %,
    /// appliqué par Sante.Encaisser d'après la source du coup) et vitesse +GameBalance.morgrimCriBonusVitesse (FacteurVitesse).
    public enum TypeStatut : byte { Aucun = 0, Brulure = 1, Ralenti = 2, Etourdi = 3, Ivresse = 4, Provoque = 5, Renverse = 6, PeauDeFer = 7, Galvanise = 8 }

    /// D'où vient un statut (infobulle du menu du personnage : « Source »).
    public enum OrigineStatut : byte { Inconnue = 0, Joueur = 1, Ennemi = 2, Chute = 3, Taverne = 4, Eau = 5 }

    /// Ce que fait un nouveau statut quand la cible a déjà le même.
    /// Rafraichir : la durée repart de zéro. Prolonger : la fin la plus lointaine l'emporte (étourdissement,
    /// ralenti, ivresse). Additionner : les durées s'ajoutent. Remplacer : le nouveau prend toute la place (provocation).
    /// L'intensité retenue est toujours la plus forte, sauf pour Remplacer.
    public enum RegleCumul : byte { Rafraichir = 0, Prolonger = 1, Additionner = 2, Remplacer = 3 }

    /// Un statut posé sur un personnage (héros ou ennemi).
    [Serializable]
    public struct Statut
    {
        public TypeStatut type;
        /// Brûlure : dégâts par seconde du palier au dernier coup de feu (le palier courant fait foi : Brulure.Degats) ; Ralenti : part de vitesse retirée (0,4 = −40 %) ; autres : 1.
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
        /// Brûlure en paliers (01/10/2026) : instantané au dernier coup de feu (palier 1 au plafond, jauge 0 à 1) et instant
        /// (Time.time de ce poste) où commence la redescente. Valeurs courantes : Brulure.Etat (ou PalierCourant/JaugeCourante).
        public byte palier;
        public float jauge;
        public float descente;

        public bool Permanent => float.IsPositiveInfinity(fin);
        /// Secondes restantes, ou -1 sans durée.
        public float Restant => Permanent ? -1f : Mathf.Max(0f, fin - Time.time);
        /// Palier courant d'un statut qui se cumule (Brûlure), 0 pour les autres.
        public int PalierCourant { get { if (type != TypeStatut.Brulure) return 0; Brulure.Etat(this, Time.time, out int p, out _); return p; } }
        /// Jauge courante (0 à 1) d'un statut qui se cumule (Brûlure), -1 pour les autres.
        public float JaugeCourante { get { if (type != TypeStatut.Brulure) return -1f; Brulure.Etat(this, Time.time, out _, out float j); return j; } }
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

        /// Délai minimal entre deux demandes du même type à l'hôte (le cône de flammes attise la brûlure 4 fois par seconde :
        /// ses remplissages sont cumulés entre deux demandes, AttiserBrulure).
        public const float IntervalleDemandes = 0.4f;
        /// Délai de grâce d'une prédiction qui n'est pas encore revenue de l'hôte.
        public const float GracePrediction = 1.5f;

        readonly List<Statut> m_Liste = new List<Statut>();
        readonly Dictionary<TypeStatut, float> m_DernieresDemandes = new Dictionary<TypeStatut, float>();
        Sante m_Sante;
        float m_AccuBrulure;
        bool m_FlammesVisibles;
        int m_PalierVisible;
        /// Client : remplissage de brûlure pas encore envoyé à l'hôte (cumulé entre deux demandes), et sa source.
        float m_BrulureEnAttente, m_DerniereDemandeBrulure;
        OrigineStatut m_BrulureOrigine;
        int m_BrulureSource;

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
                float f = r > 0f ? Mathf.Clamp(1f - r, 0.1f, 1f) : 1f;
                // Galvanisé (cri de Morgrim) : vitesse accrue.
                if (A(TypeStatut.Galvanise)) f *= 1f + GameBalance.Courant.morgrimCriBonusVitesse;
                return f;
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

        /// Autorité : demande reçue d'un client (déjà validée par le réseau). Brûlure : `intensite` porte le remplissage.
        public void AjouterDemande(Statut s)
        {
            if (s.type == TypeStatut.Brulure) AttiserBrulure(s.intensite, s.origine, s.sourceId);
            else Ajouter(s.type, s.duree, s.intensite, s.origine, s.sourceId);
        }

        /// Brûlure en paliers (Brulure.Attiser) : un coup de feu remplit la jauge de `remplissage`. Chez un client, les
        /// remplissages sont cumulés et envoyés à l'hôte au plus tous les IntervalleDemandes (rien n'est perdu entre deux).
        public void AttiserBrulure(float remplissage, OrigineStatut origine, int sourceId)
        {
            if (remplissage <= 0f) return;
            if (Sante != null && Sante.Mort) return;
            if (!Autorite)
            {
                if (relais == null) return;
                m_BrulureEnAttente += remplissage;
                m_BrulureOrigine = origine;
                m_BrulureSource = sourceId;
                if (Time.time - m_DerniereDemandeBrulure >= IntervalleDemandes) EnvoyerBrulure();
                else enabled = true;   // Update envoie le reste
                return;
            }
            float t = Time.time;
            int i = -1;
            for (int j = 0; j < m_Liste.Count; j++)
                if (m_Liste[j].type == TypeStatut.Brulure && !m_Liste[j].Permanent) { i = j; break; }
            var s = Brulure.Attiser(i >= 0 ? m_Liste[i] : default(Statut), i >= 0, remplissage, t);
            s.origine = origine;
            s.sourceId = sourceId;
            s.predit = false;
            if (i >= 0) m_Liste[i] = s;
            else m_Liste.Add(s);
            enabled = true;
            Change?.Invoke();
        }

        void EnvoyerBrulure()
        {
            if (relais == null || m_BrulureEnAttente <= 0f) { m_BrulureEnAttente = 0f; return; }
            m_DerniereDemandeBrulure = Time.time;
            DemandesEnvoyees++;
            relais(new Statut
            {
                type = TypeStatut.Brulure, duree = GameBalance.Courant.brulureDuree, intensite = m_BrulureEnAttente,
                origine = m_BrulureOrigine, sourceId = m_BrulureSource,
            });
            m_BrulureEnAttente = 0f;
        }

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
            // Boucle plutôt que FindIndex(lambda) : la lambda capture `s` et allouait une fermeture à chaque appel.
            int i = -1;
            for (int j = 0; j < m_Liste.Count; j++)
                if (m_Liste[j].type == s.type && !m_Liste[j].Permanent) { i = j; break; }
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
            if (m_Liste.Count == 0 && !m_FlammesVisibles && m_BrulureEnAttente <= 0f) enabled = false;
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
            // Client : remplissage de brûlure en attente, envoyé dès que l'intervalle des demandes est passé.
            if (!Autorite && m_BrulureEnAttente > 0f && t - m_DerniereDemandeBrulure >= IntervalleDemandes) EnvoyerBrulure();

            // Brûlure : dégâts continus du palier courant, comptés au joueur qui l'a posée (le poste qui fait foi seulement).
            int ib = -1;
            for (int i = 0; i < m_Liste.Count; i++) if (m_Liste[i].type == TypeStatut.Brulure) { ib = i; break; }
            bool brule = ib >= 0;
            int palier = brule ? m_Liste[ib].PalierCourant : 0;
            if (brule && Autorite && sante != null && !sante.Mort && sante.isActiveAndEnabled) Bruler(sante, m_Liste[ib], palier);
            else m_AccuBrulure = 0f;
            // Flammèches : allumées, éteintes, ou plus fournies quand le palier change.
            if (brule != m_FlammesVisibles || (brule && palier != m_PalierVisible))
            {
                m_FlammesVisibles = brule;
                m_PalierVisible = palier;
                Brulure.Montrer(this, brule, palier);
            }

            if (change && Autorite) Change?.Invoke();
            if (m_Liste.Count == 0 && !m_FlammesVisibles && m_BrulureEnAttente <= 0f) enabled = false;
        }

        void Bruler(Sante sante, Statut brulure, int palier)
        {
            m_AccuBrulure += Brulure.Degats(palier) * Time.deltaTime;
            if (m_AccuBrulure < 1f) return;
            float d = Mathf.Floor(m_AccuBrulure);
            m_AccuBrulure -= d;
            int id = brulure.sourceId;
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
