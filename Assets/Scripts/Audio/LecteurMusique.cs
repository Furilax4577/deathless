using UnityEngine;

namespace Deathless.Audio
{
    /// Liste de morceaux : jour (2), nuit (2), taverne (1) ; les fichiers sont dans Assets/Audio/Deathless/Musique/
    /// (jour_1.ogg, jour_2.ogg, nuit_1.ogg, nuit_2.ogg, taverne.ogg) et branchés sur ReglagesAudio par l'éditeur.
    public enum ListeMusique { Aucune, Jour, Nuit, Taverne }

    /// Lecteur de musique du jeu (03/10/2026) : un objet persistant (créé au lancement avec les autres sons), donc la musique
    /// ne redémarre pas au changement de scène tant que la liste ne change pas. Chaque poste choisit sa liste d'après sa propre
    /// partie (rien n'est envoyé par le réseau) : phase jour / crépuscule / aube ou menu principal → jour ; nuit (ou nuit
    /// forcée de l'aperçu) → nuit ; héros local dans la pièce de la taverne → taverne. Les morceaux d'une même liste
    /// s'enchaînent en alternance (un seul morceau : il se rejoue sur lui-même) avec un fondu enchaîné de 2,5 s, le même
    /// fondu qu'au changement de liste. Listes vides (fichiers absents) : silence, sans aucun message. Sorties dans le
    /// groupe Musique du mixeur (volume des options).
    public class LecteurMusique : MonoBehaviour
    {
        public const float Fondu = 2.5f;
        const float PeriodeChoix = 0.25f;

        public static LecteurMusique Instance { get; private set; }

        /// Liste jouée en ce moment (ou en cours de fondu vers elle).
        public static ListeMusique Liste => Instance != null ? Instance.m_Liste : ListeMusique.Aucune;

        /// Nom du morceau actif (vide : silence), pour les vérifications.
        public static string Morceau => Instance != null && Instance.m_Actif >= 0 && Instance.m_Source[Instance.m_Actif].clip != null ? Instance.m_Source[Instance.m_Actif].clip.name : "";

        /// Force une liste (bancs et essais) ; null rend la main au choix automatique.
        public static ListeMusique? Forcee;

        internal static void Creer()
        {
            if (Instance != null) return;
            var go = new GameObject("Deathless.Musique") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<LecteurMusique>();
        }

        readonly AudioSource[] m_Source = new AudioSource[2];
        readonly float[] m_Gain = new float[2];
        readonly int[] m_Dernier = { -1, -1, -1, -1 };   // dernier morceau joué de chaque liste (alternance)
        int m_Actif = -1;
        ListeMusique m_Liste = ListeMusique.Aucune;
        bool m_Enchaine;       // le morceau suivant de la liste est déjà lancé
        float m_Prochain;

        void Awake()
        {
            for (int i = 0; i < 2; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.loop = false;
                s.spatialBlend = 0f;
                s.ignoreListenerPause = true;
                s.volume = 0f;
                VolumesAudio.Router(s, CanalAudio.Musique);
                m_Source[i] = s;
            }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        static AudioClip[] Clips(ListeMusique l)
        {
            var r = VolumesAudio.Reglages;
            if (r == null) return null;
            switch (l)
            {
                case ListeMusique.Jour: return r.musiquesJour;
                case ListeMusique.Nuit: return r.musiquesNuit;
                case ListeMusique.Taverne: return r.musiquesTaverne;
                default: return null;
            }
        }

        /// Liste voulue d'après la partie de ce poste.
        ListeMusique Voulue()
        {
            if (Forcee.HasValue) return Forcee.Value;
            var p = Deathless.Jeu.Partie.Instance;
            if (p == null) return ListeMusique.Aucune;
            if (Deathless.Jeu.PiecesBatiments.Instance != null)
            {
                var piece = Deathless.Jeu.PiecesBatiments.PieceLocale;
                if (piece != null && piece.role == "Taverne") return ListeMusique.Taverne;
            }
            var phase = p.Etat.phase;
            if (phase == Deathless.Jeu.Phase.Terminee) return m_Liste == ListeMusique.Aucune ? ListeMusique.Jour : m_Liste;
            if (phase == Deathless.Jeu.Phase.Nuit || Deathless.Jeu.Partie.NuitApercu) return ListeMusique.Nuit;
            return ListeMusique.Jour;   // jour, crépuscule, aube, menu principal
        }

        void Update()
        {
            if (Time.unscaledTime >= m_Prochain)
            {
                m_Prochain = Time.unscaledTime + PeriodeChoix;
                var v = Voulue();
                if (v != m_Liste) Changer(v);
                // Liste restée muette faute de morceaux (fichiers ajoutés depuis, réglages chargés plus tard) : on retente.
                else if (m_Actif < 0 && m_Liste != ListeMusique.Aucune) LancerSuivant();
            }

            // Enchaînement : le morceau actif touche à sa fin, le suivant de la liste démarre en fondu.
            if (m_Actif >= 0 && !m_Enchaine)
            {
                var s = m_Source[m_Actif];
                if (s.clip != null && (!s.isPlaying || s.time >= s.clip.length - Fondu))
                {
                    m_Enchaine = true;
                    LancerSuivant();
                }
            }

            for (int i = 0; i < 2; i++)
            {
                float cible = i == m_Actif ? 1f : 0f;
                m_Gain[i] = Mathf.MoveTowards(m_Gain[i], cible, Time.unscaledDeltaTime / Fondu);
                m_Source[i].volume = m_Gain[i];
                if (i != m_Actif && m_Gain[i] <= 0f && m_Source[i].isPlaying) m_Source[i].Stop();
            }
        }

        void Changer(ListeMusique l)
        {
            m_Liste = l;
            LancerSuivant();
        }

        void LancerSuivant()
        {
            var clips = Clips(m_Liste);
            int n = 0;
            if (clips != null) for (int i = 0; i < clips.Length; i++) if (clips[i] != null) n++;
            if (n == 0) { m_Actif = -1; return; }   // silence : le morceau en cours s'éteint en fondu
            int dernier = m_Dernier[(int)m_Liste];
            int k = dernier < 0 ? Random.Range(0, clips.Length) : (dernier + 1) % clips.Length;
            for (int essai = 0; essai < clips.Length && clips[k] == null; essai++) k = (k + 1) % clips.Length;
            m_Dernier[(int)m_Liste] = k;
            // La source la plus faible reçoit le nouveau morceau : un changement de liste en plein fondu ne coupe jamais
            // net le morceau encore audible (vérifié le 03/10/2026 : jour_1 à 91 % était coupé quand la nuit tombait).
            int cible = m_Gain[0] <= m_Gain[1] ? 0 : 1;
            var s = m_Source[cible];
            s.Stop();
            s.clip = clips[k];
            s.time = 0f;
            m_Gain[cible] = 0f;
            s.volume = 0f;
            s.Play();
            m_Actif = cible;
            m_Enchaine = false;
        }
    }
}
