using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Deathless.Audio
{
    public enum CanalAudio { Principal, Musique, Effets, Interface }

    public enum SonInterface { Survol, Clic, Retour, Refus }

    /// Volumes du jeu (options, onglet Audio) : quatre réglages de 0 à 1, enregistrés dans les PlayerPrefs et appliqués au
    /// mixer Deathless (paramètres exposés en dB, 0 = coupé). Chargés au lancement, avant la première scène, donc avant le
    /// premier son. Toute source audio du jeu passe par un groupe du mixer : `Groupe(canal)`.
    public static class VolumesAudio
    {
        /// Valeurs par défaut (demande de Quentin, 25/09/2026) : principal 100, musique 10, effets 15, interface 15.
        public static readonly float[] ParDefaut = { 1f, 0.1f, 0.15f, 0.15f };
        static readonly string[] s_Parametres = { "VolumePrincipal", "VolumeMusique", "VolumeEffets", "VolumeInterface" };
        const string PrefixeCle = "Deathless.Volume.";

        static readonly float[] s_Volumes = (float[])ParDefaut.Clone();
        static ReglagesAudio s_Reglages;
        static bool s_Charge;

        /// Un volume a changé (canal, nouvelle valeur 0-1).
        public static event Action<CanalAudio, float> Changed;

        public static ReglagesAudio Reglages
        {
            get
            {
                if (s_Reglages == null) s_Reglages = Resources.Load<ReglagesAudio>("DeathlessAudio");
                return s_Reglages;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AuLancement()
        {
            Charger();
            Appliquer();
            LecteurAudio.Creer();
        }

        static void Charger()
        {
            for (var i = 0; i < s_Volumes.Length; i++)
                s_Volumes[i] = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefixeCle + (CanalAudio)i, ParDefaut[i]));
            s_Charge = true;
        }

        public static float Volume(CanalAudio canal)
        {
            if (!s_Charge) Charger();
            return s_Volumes[(int)canal];
        }

        /// Règle un volume (0-1), l'applique au mixer tout de suite et l'enregistre.
        public static void DefinirVolume(CanalAudio canal, float valeur)
        {
            if (!s_Charge) Charger();
            valeur = Mathf.Clamp01(valeur);
            if (Mathf.Approximately(s_Volumes[(int)canal], valeur)) return;
            s_Volumes[(int)canal] = valeur;
            PlayerPrefs.SetFloat(PrefixeCle + canal, valeur);
            Appliquer(canal);
            Changed?.Invoke(canal, valeur);
        }

        /// Écrit les PlayerPrefs sur le disque (à la sortie de l'écran d'options).
        public static void Enregistrer() => PlayerPrefs.Save();

        public static void Reinitialiser()
        {
            for (var i = 0; i < ParDefaut.Length; i++) DefinirVolume((CanalAudio)i, ParDefaut[i]);
        }

        /// Conversion linéaire → dB (0 = coupé, -80 dB).
        public static float EnDecibels(float v) => v <= 0.0001f ? -80f : Mathf.Max(-80f, 20f * Mathf.Log10(v));

        public static void Appliquer()
        {
            for (var i = 0; i < s_Volumes.Length; i++) Appliquer((CanalAudio)i);
        }

        static void Appliquer(CanalAudio canal)
        {
            var r = Reglages;
            if (r == null || r.mixer == null) return;
            r.mixer.SetFloat(s_Parametres[(int)canal], EnDecibels(Volume(canal)));
        }

        /// Groupe du mixer d'un canal (Principal : null, les sources vont dans un sous-groupe).
        public static AudioMixerGroup Groupe(CanalAudio canal)
        {
            var r = Reglages;
            if (r == null) return null;
            switch (canal)
            {
                case CanalAudio.Musique: return r.groupeMusique;
                case CanalAudio.Effets: return r.groupeEffets;
                case CanalAudio.Interface: return r.groupeInterface;
                default: return null;
            }
        }

        /// Branche une source sur le groupe d'un canal.
        public static AudioSource Router(AudioSource source, CanalAudio canal)
        {
            if (source != null) source.outputAudioMixerGroup = Groupe(canal);
            return source;
        }

        /// Son court des menus (groupe Interface).
        public static void JouerInterface(SonInterface son, float volume = 1f)
        {
            var r = Reglages;
            if (r == null) return;
            var clip = son == SonInterface.Survol ? r.survol : son == SonInterface.Clic ? r.clic : son == SonInterface.Refus ? r.refus : r.retour;
            LecteurAudio.Jouer(CanalAudio.Interface, clip, volume);
        }

        /// Aperçu d'un réglage : son court de la catégorie (Effets : un coup ; Interface : un clic).
        public static void JouerApercu(CanalAudio canal)
        {
            var r = Reglages;
            if (r == null) return;
            if (canal == CanalAudio.Effets) LecteurAudio.Jouer(CanalAudio.Effets, r.apercuEffets, 0.8f);
            else if (canal == CanalAudio.Interface) LecteurAudio.Jouer(CanalAudio.Interface, r.clic, 1f);
        }

        /// Remplace AudioSource.PlayClipAtPoint (dont la source ne passe par aucun groupe) : son 3D ponctuel, groupe Effets.
        public static void JouerAuPoint(AudioClip clip, Vector3 point, float volume = 1f)
        {
            if (clip == null) return;
            var go = new GameObject("Son ponctuel");
            go.transform.position = point;
            var s = go.AddComponent<AudioSource>();
            Router(s, CanalAudio.Effets);
            s.clip = clip;
            s.volume = volume;
            s.spatialBlend = 1f;
            s.Play();
            UnityEngine.Object.Destroy(go, clip.length + 0.1f);
        }
    }

    /// Objet persistant (créé au lancement) : sources 2D des sons d'interface et des aperçus, et nouvelle application des
    /// volumes à la première image (AudioMixer.SetFloat peut être ignoré avant la première image).
    public class LecteurAudio : MonoBehaviour
    {
        static LecteurAudio s_Instance;
        AudioSource m_Interface, m_Effets;

        internal static void Creer()
        {
            if (s_Instance != null) return;
            var go = new GameObject("Deathless.Audio") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<LecteurAudio>();
        }

        void Awake()
        {
            m_Interface = VolumesAudio.Router(Source(), CanalAudio.Interface);
            m_Effets = VolumesAudio.Router(Source(), CanalAudio.Effets);
        }

        AudioSource Source()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.ignoreListenerPause = true;
            return s;
        }

        void Start() => VolumesAudio.Appliquer();

        internal static void Jouer(CanalAudio canal, AudioClip clip, float volume)
        {
            if (clip == null) return;
            if (s_Instance == null) Creer();
            var s = canal == CanalAudio.Interface ? s_Instance.m_Interface : s_Instance.m_Effets;
            if (s != null) s.PlayOneShot(clip, volume);
        }

        void OnDestroy() { if (s_Instance == this) s_Instance = null; }
    }
}
