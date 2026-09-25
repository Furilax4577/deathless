using System.Collections.Generic;
using Deathless.Audio;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Lecture des sons du catalogue : effets 3D (réserve de sources), sons 2D, boucles attachées, musique en fondu
    /// enchaîné. Une instance par scène (AudioBank.Instance) ; les appels statiques ne font rien sans elle.
    /// Mixer : effets, sons 2D et boucles dans le groupe Effets, musique dans le groupe Musique (VolumesAudio).
    public class AudioBank : MonoBehaviour
    {
        public static AudioBank Instance { get; private set; }

        public SonsCatalogue catalogue;
        [Range(0f, 1f)] public float volumeEffets = 0.9f;
        [Range(0f, 1f)] public float volumeMusique = 0.45f;
        public int sources = 24;
        public float fonduMusique = 3f;

        readonly List<AudioSource> m_Sources = new List<AudioSource>();
        AudioSource m_Source2D;
        AudioSource[] m_Musique = new AudioSource[2];
        int m_MusiqueActive;
        float m_Fondu = 1f;
        readonly Dictionary<string, float> m_Dernier = new Dictionary<string, float>();

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < sources; i++)
            {
                var go = new GameObject("Son3D_" + i);
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 3f;
                s.maxDistance = 60f;
                s.dopplerLevel = 0f;
                VolumesAudio.Router(s, CanalAudio.Effets);
                m_Sources.Add(s);
            }
            m_Source2D = gameObject.AddComponent<AudioSource>();
            m_Source2D.playOnAwake = false;
            VolumesAudio.Router(m_Source2D, CanalAudio.Effets);
            for (int i = 0; i < 2; i++)
            {
                m_Musique[i] = gameObject.AddComponent<AudioSource>();
                m_Musique[i].loop = true;
                m_Musique[i].playOnAwake = false;
                m_Musique[i].volume = 0f;
                VolumesAudio.Router(m_Musique[i], CanalAudio.Musique);
            }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        static AudioClip Tirer(SonsCatalogue.Entree e) => e.clips[Random.Range(0, e.clips.Length)];

        SonsCatalogue.Entree Entree(string[] ids) => catalogue != null ? catalogue.Premiere(ids) : null;

        /// Son 3D au point donné. `anti` : intervalle minimal entre deux lectures du même son (s).
        public static void Jouer(string[] ids, Vector3 point, float volume = 1f, float anti = 0f)
        {
            if (Instance != null) Instance.JouerIci(ids, point, volume, anti);
        }

        public static void Jouer2D(string[] ids, float volume = 1f)
        {
            if (Instance == null) return;
            var e = Instance.Entree(ids);
            if (e == null) return;
            Instance.m_Source2D.PlayOneShot(Tirer(e), volume * Instance.volumeEffets);
        }

        void JouerIci(string[] ids, Vector3 point, float volume, float anti)
        {
            var e = Entree(ids);
            if (e == null) return;
            if (anti > 0f)
            {
                if (m_Dernier.TryGetValue(e.id, out var t) && Time.time - t < anti) return;
                m_Dernier[e.id] = Time.time;
            }
            AudioSource libre = null;
            foreach (var s in m_Sources) if (!s.isPlaying) { libre = s; break; }
            if (libre == null) libre = m_Sources[Random.Range(0, m_Sources.Count)];
            libre.transform.position = point;
            libre.clip = Tirer(e);
            libre.volume = volume * volumeEffets;
            libre.pitch = Random.Range(0.95f, 1.05f);
            libre.loop = false;
            libre.Play();
        }

        /// Boucle attachée à un objet (vol du missile, bourdonnement du portail) ; renvoie la source (à arrêter).
        public static AudioSource Boucle(string[] ids, Transform parent, float volume = 1f)
        {
            if (Instance == null || parent == null) return null;
            var e = Instance.Entree(ids);
            if (e == null) return null;
            var s = parent.gameObject.AddComponent<AudioSource>();
            s.clip = Tirer(e);
            s.loop = true;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 3f;
            s.maxDistance = 45f;
            s.dopplerLevel = 0f;
            s.volume = volume * Instance.volumeEffets;
            VolumesAudio.Router(s, CanalAudio.Effets);
            s.Play();
            return s;
        }

        /// Musique de fond (fondu enchaîné).
        public static void Musique(string[] ids)
        {
            if (Instance == null) return;
            var e = Instance.Entree(ids);
            if (e == null) return;
            var clip = e.clips[0];
            var active = Instance.m_Musique[Instance.m_MusiqueActive];
            if (active.clip == clip && active.isPlaying) return;
            Instance.m_MusiqueActive = 1 - Instance.m_MusiqueActive;
            var nouvelle = Instance.m_Musique[Instance.m_MusiqueActive];
            nouvelle.clip = clip;
            nouvelle.volume = 0f;
            nouvelle.Play();
            Instance.m_Fondu = 0f;
        }

        void Update()
        {
            if (m_Fondu < 1f)
            {
                m_Fondu = Mathf.Min(1f, m_Fondu + Time.unscaledDeltaTime / Mathf.Max(0.01f, fonduMusique));
                var a = m_Musique[m_MusiqueActive];
                var b = m_Musique[1 - m_MusiqueActive];
                a.volume = volumeMusique * m_Fondu;
                b.volume = volumeMusique * (1f - m_Fondu);
                if (m_Fondu >= 1f) b.Stop();
            }
        }
    }
}
