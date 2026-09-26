using System;
using System.Collections.Generic;
using System.IO;
using Deathless.Audio;
using UnityEngine;

namespace Deathless.Dev.Tournage
{
    /// Mixage hors temps réel de la bande-annonce (48 kHz stéréo), en pas avec l'encodage image par image.
    ///
    /// Deux familles de voix :
    /// - **Jeu** : les AudioSource de la scène sont sondées à chaque image (Sonder) ; une source qui démarre (ou change de
    ///   clip, ou repart de zéro) ouvre une voix qui relit le clip (AudioClip.GetData, clips « Decompress On Load »),
    ///   atténuée et placée en stéréo par rapport à la caméra de tournage (réglages 3D de la source). Les sources de
    ///   musique du jeu (groupe Musique) sont ignorées : la musique est posée ici. Les sons joués en PlayOneShot (sons 2D
    ///   d'AudioBank) ne sont pas visibles : le scénario les rejoue par le catalogue (JouerCatalogue).
    /// - **Habillage** : fichiers WAV préparés hors Unity (Builds/Trailer/audio, 48 kHz stéréo 16 bits : musique du jeu,
    ///   impacts et nappes Sonniss) posés par le scénario sur la ligne de temps de la vidéo.
    ///
    /// Le temps du jeu avance d'une image de capture à la fois (Time.captureFramerate) alors que le moteur audio de Unity
    /// tourne en temps réel : la position d'une voix de jeu est donc calculée en temps de jeu depuis son démarrage, pas
    /// lue dans AudioSource.timeSamples. Bus : Jeu, Musique, Habillage (gain avec rampe), maître avec limiteur doux.
    public class MixeurTrailer
    {
        public const int Taux = 48000;

        public enum Bus { Jeu, Musique, Habillage }

        sealed class Voix
        {
            public string nom;
            public Bus bus;
            public float[] donnees;
            public int canaux;
            public double pos;        // position dans la source (images de la source)
            public double pas;        // images de la source par échantillon de sortie
            public bool boucle;
            public float gainG, gainD, cibleG = -1f, cibleD = -1f;
            public long debut;        // échantillon de sortie où la voix commence (peut être dans le futur)
            public int fonduRestant = -1, fonduTotal;
            public bool finie;
            public int source;        // instance de l'AudioSource (0 : habillage)
        }

        sealed class Suivi
        {
            public AudioClip clip;
            public bool jouait;
            public int dernierEchantillon;
            public float debutJeu;
            public Voix voix;
            public bool vu;
        }

        readonly List<Voix> m_Voix = new List<Voix>();
        readonly Dictionary<int, Suivi> m_Suivis = new Dictionary<int, Suivi>();
        readonly Dictionary<AudioClip, float[]> m_Clips = new Dictionary<AudioClip, float[]>();
        readonly Dictionary<string, float[]> m_Wav = new Dictionary<string, float[]>();
        readonly float[] m_GainBus = { 1f, 1f, 1f };
        readonly float[] m_CibleBus = { 1f, 1f, 1f };
        readonly float[] m_PasBus = { 0f, 0f, 0f };
        float m_Maitre = 1f, m_CibleMaitre = 1f, m_PasMaitre;

        /// Échantillons de sortie déjà produits (la « tête de lecture » du mixage).
        public long Position { get; private set; }
        public double Temps => Position / (double)Taux;

        public float gainJeu = 1.3f;
        public string Dossier { get; private set; }
        public readonly List<string> Journal = new List<string>();
        public int VoixJeuCreees { get; private set; }
        public readonly Dictionary<string, int> VoixParClip = new Dictionary<string, int>();
        public float CreteMax { get; private set; }

        BinaryWriter m_Wav16;
        long m_Wav16Echantillons;

        // ------------------------------------------------------------------ Fichiers d'habillage

        /// Charge tous les WAV du dossier (48 kHz, 16 bits PCM ; mono ou stéréo, rééchantillonnés sinon).
        public void Charger(string dossier)
        {
            Dossier = dossier;
            if (!Directory.Exists(dossier)) { Journal.Add("dossier audio absent : " + dossier); return; }
            foreach (var f in Directory.GetFiles(dossier, "*.wav"))
            {
                try
                {
                    var d = LireWav(f);
                    if (d != null) m_Wav[Path.GetFileNameWithoutExtension(f)] = d;
                }
                catch (Exception e) { Journal.Add("WAV illisible " + f + " : " + e.Message); }
            }
            Journal.Add(m_Wav.Count + " fichiers d'habillage chargés");
        }

        public bool Existe(string nom) => m_Wav.ContainsKey(nom);

        public float Duree(string nom) => m_Wav.TryGetValue(nom, out var d) ? d.Length / 2f / Taux : 0f;

        /// Instant (s) du premier échantillon à la moitié du maximum (attaque) d'un fichier d'habillage.
        public float Attaque(string nom)
        {
            if (!m_Wav.TryGetValue(nom, out var d)) return 0f;
            float max = 0f;
            for (int i = 0; i < d.Length; i++) max = Mathf.Max(max, Mathf.Abs(d[i]));
            for (int i = 0; i < d.Length; i += 2) if (Mathf.Abs(d[i]) + Mathf.Abs(d[i + 1]) >= max) return i / 2f / Taux;
            return 0f;
        }

        /// Instant (s) du maximum d'un fichier d'habillage.
        public float Pic(string nom)
        {
            if (!m_Wav.TryGetValue(nom, out var d)) return 0f;
            float max = 0f; int im = 0;
            for (int i = 0; i < d.Length; i += 2) { float v = Mathf.Abs(d[i]) + Mathf.Abs(d[i + 1]); if (v > max) { max = v; im = i; } }
            return im / 2f / Taux;
        }

        static float[] LireWav(string chemin)
        {
            var o = File.ReadAllBytes(chemin);
            if (o.Length < 44 || o[0] != 'R' || o[8] != 'W') return null;
            int pos = 12, canaux = 2, taux = Taux, bits = 16, debut = -1, taille = 0;
            while (pos + 8 <= o.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(o, pos, 4);
                int t = BitConverter.ToInt32(o, pos + 4);
                if (id == "fmt ")
                {
                    canaux = BitConverter.ToInt16(o, pos + 10);
                    taux = BitConverter.ToInt32(o, pos + 12);
                    bits = BitConverter.ToInt16(o, pos + 22);
                }
                else if (id == "data") { debut = pos + 8; taille = Mathf.Min(t, o.Length - debut); break; }
                pos += 8 + t + (t & 1);
            }
            if (debut < 0 || bits != 16) return null;
            int n = taille / (2 * canaux);
            var st = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                float g = BitConverter.ToInt16(o, debut + (i * canaux) * 2) / 32768f;
                float d = canaux > 1 ? BitConverter.ToInt16(o, debut + (i * canaux + 1) * 2) / 32768f : g;
                st[2 * i] = g; st[2 * i + 1] = d;
            }
            if (taux == Taux) return st;
            // Rééchantillonnage linéaire.
            int m = (int)((long)n * Taux / taux);
            var r = new float[m * 2];
            double k = taux / (double)Taux;
            for (int i = 0; i < m; i++)
            {
                double x = i * k; int a = (int)x; float f = (float)(x - a); int b = Mathf.Min(a + 1, n - 1); a = Mathf.Min(a, n - 1);
                r[2 * i] = st[2 * a] * (1 - f) + st[2 * b] * f;
                r[2 * i + 1] = st[2 * a + 1] * (1 - f) + st[2 * b + 1] * f;
            }
            return r;
        }

        // ------------------------------------------------------------------ Habillage : pose sur la ligne de temps

        /// Pose le fichier `nom` à l'instant `temps` (s, temps de la vidéo). Un instant passé entre au milieu du fichier.
        /// `pan` : -1 gauche, 1 droite. Renvoie faux si le fichier manque.
        public bool Poser(string nom, double temps, float gain = 1f, Bus bus = Bus.Habillage, float pan = 0f, bool boucle = false)
        {
            if (!m_Wav.TryGetValue(nom, out var d)) { Journal.Add("habillage absent : " + nom); return false; }
            long debut = (long)Math.Round(temps * Taux);
            var v = new Voix { nom = nom, bus = bus, donnees = d, canaux = 2, pas = 1.0, boucle = boucle, debut = debut };
            if (debut < Position) { v.pos = Position - debut; v.debut = Position; }
            Pan(gain * 1.4142f, pan, out v.gainG, out v.gainD);
            m_Voix.Add(v);
            return true;
        }

        /// Pose le fichier pour que son attaque (ou son pic) tombe à l'instant `temps`.
        public bool PoserSur(string nom, double temps, float gain = 1f, bool pic = false, Bus bus = Bus.Habillage)
            => Poser(nom, temps - (pic ? Pic(nom) : Attaque(nom)), gain, bus);

        /// Fondu de sortie des voix d'habillage portant ce nom (toutes si null).
        public void Couper(string nom, float fondu, Bus? bus = null)
        {
            foreach (var v in m_Voix)
            {
                if (v.finie || v.source != 0) continue;
                if (nom != null && v.nom != nom) continue;
                if (bus.HasValue && v.bus != bus.Value) continue;
                Fondre(v, fondu);
            }
        }

        /// Gain d'une voix d'habillage (rampe de `duree` s).
        public void Gain(string nom, float gain, float duree)
        {
            foreach (var v in m_Voix)
            {
                if (v.finie || v.nom != nom) continue;
                v.cibleG = v.cibleD = gain;
                v.fonduTotal = Mathf.Max(1, (int)(duree * Taux));
            }
        }

        /// Gain d'un bus (rampe de `duree` s).
        public void GainBus(Bus bus, float gain, float duree)
        {
            int b = (int)bus;
            m_CibleBus[b] = gain;
            m_PasBus[b] = duree <= 0f ? float.MaxValue : Mathf.Abs(gain - m_GainBus[b]) / (duree * Taux);
        }

        /// Gain maître (silences voulus : rampe de `duree` s).
        public void GainMaitre(float gain, float duree)
        {
            m_CibleMaitre = gain;
            m_PasMaitre = duree <= 0f ? float.MaxValue : Mathf.Abs(gain - m_Maitre) / (duree * Taux);
        }

        static void Pan(float gain, float pan, out float g, out float d)
        {
            float a = (Mathf.Clamp(pan, -1f, 1f) + 1f) * Mathf.PI * 0.25f;
            g = gain * Mathf.Cos(a); d = gain * Mathf.Sin(a);
        }

        static void Fondre(Voix v, float duree)
        {
            if (v.finie) return;
            int n = Mathf.Max(1, (int)(duree * Taux));
            if (v.fonduRestant >= 0 && v.fonduRestant <= n) return;
            v.fonduRestant = n; v.fonduTotal = n;
            v.cibleG = -1f;
        }

        // ------------------------------------------------------------------ Sons du jeu

        /// Clip du catalogue du jeu (premier id présent), ou null.
        public static AudioClip ClipCatalogue(string[] ids)
        {
            var ab = Deathless.Jeu.AudioBank.Instance;
            var e = ab != null && ab.catalogue != null ? ab.catalogue.Premiere(ids) : null;
            return e != null && e.clips.Length > 0 ? e.clips[UnityEngine.Random.Range(0, e.clips.Length)] : null;
        }

        /// Rejoue un son du catalogue du jeu (sons 2D d'AudioBank, invisibles au sondage) à l'instant `temps`.
        public void JouerCatalogue(string[] ids, double temps, float gain = 1f)
        {
            var c = ClipCatalogue(ids);
            if (c != null) JouerClip(c, temps, gain, Bus.Jeu);
        }

        /// Pose un AudioClip du projet (2D) sur la ligne de temps.
        public void JouerClip(AudioClip clip, double temps, float gain = 1f, Bus bus = Bus.Jeu)
        {
            var d = Echantillons(clip);
            if (d == null) return;
            var v = new Voix { nom = clip.name, bus = bus, donnees = d, canaux = clip.channels, pas = clip.frequency / (double)Taux, debut = (long)Math.Round(temps * Taux) };
            if (v.debut < Position) { v.pos = (Position - v.debut) * v.pas; v.debut = Position; }
            Pan(gain * 1.4142f, 0f, out v.gainG, out v.gainD);
            m_Voix.Add(v);
        }

        float[] Echantillons(AudioClip c)
        {
            if (c == null) return null;
            if (m_Clips.TryGetValue(c, out var d)) return d;
            d = null;
            try
            {
                if (c.loadState != AudioDataLoadState.Loaded) c.LoadAudioData();
                if (c.loadState == AudioDataLoadState.Loaded && c.samples > 0)
                {
                    var t = new float[c.samples * c.channels];
                    if (c.GetData(t, 0)) d = t;
                }
            }
            catch (Exception e) { Journal.Add("clip illisible " + c.name + " : " + e.Message); }
            if (d != null) m_Clips[c] = d;
            else if (c.loadState == AudioDataLoadState.Loaded) m_Clips[c] = null;   // flux (musique) : jamais lisible
            return d;
        }

        static bool EstMusique(AudioSource s)
        {
            var g = VolumesAudio.Groupe(CanalAudio.Musique);
            return g != null && s.outputAudioMixerGroup == g;
        }

        /// Sonde les AudioSource de la scène (à chaque image, enregistrée ou non). `enregistre` : les voix ne sont créées
        /// que pendant l'enregistrement ; une source déjà en cours reprend là où elle en est (en temps de jeu).
        public void Sonder(Transform auditeur, bool enregistre)
        {
            foreach (var su in m_Suivis.Values) su.vu = false;
            var sources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            float maintenant = Time.time;
            foreach (var s in sources)
            {
                if (s == null || EstMusique(s)) continue;
                int id = s.GetInstanceID();
                if (!m_Suivis.TryGetValue(id, out var su)) { su = new Suivi(); m_Suivis[id] = su; }
                su.vu = true;
                var clip = s.clip;
                bool joue = s.isActiveAndEnabled && s.isPlaying && clip != null && !s.mute;
                int ts = joue ? s.timeSamples : 0;
                bool nouveau = joue && (!su.jouait || su.clip != clip || (!s.loop && ts + 4000 < su.dernierEchantillon));
                if (nouveau)
                {
                    if (su.voix != null && su.voix.boucle) Fondre(su.voix, 0.03f);
                    su.clip = clip; su.debutJeu = maintenant; su.voix = null;
                }
                if (joue && enregistre && su.voix == null)
                {
                    var d = Echantillons(clip);
                    if (d != null)
                    {
                        double pas = clip.frequency * Mathf.Clamp(s.pitch, 0.1f, 3f) / (double)Taux;
                        double decale = (maintenant - su.debutJeu) * clip.frequency * s.pitch;
                        int longueur = d.Length / Mathf.Max(1, clip.channels);
                        if (s.loop || decale < longueur - 1)
                        {
                            var v = new Voix { nom = clip.name, bus = Bus.Jeu, donnees = d, canaux = clip.channels, pas = pas, boucle = s.loop, debut = Position, source = id };
                            v.pos = s.loop ? decale % longueur : decale;
                            Spatial(s, auditeur, out v.gainG, out v.gainD);
                            m_Voix.Add(v);
                            su.voix = v;
                            VoixJeuCreees++;
                            VoixParClip.TryGetValue(clip.name, out int nb);
                            VoixParClip[clip.name] = nb + 1;
                        }
                    }
                }
                if (su.voix != null)
                {
                    if (!joue)
                    {
                        if (su.voix.boucle) Fondre(su.voix, 0.04f);
                        su.voix = null;
                    }
                    else
                    {
                        Spatial(s, auditeur, out su.voix.cibleG, out su.voix.cibleD);
                        su.voix.fonduTotal = Taux / 30;
                    }
                }
                su.jouait = joue; su.dernierEchantillon = ts;
            }
            // Sources détruites : les boucles s'arrêtent.
            List<int> parties = null;
            foreach (var kv in m_Suivis)
            {
                if (kv.Value.vu) continue;
                if (kv.Value.voix != null && kv.Value.voix.boucle) Fondre(kv.Value.voix, 0.04f);
                (parties ??= new List<int>()).Add(kv.Key);
            }
            if (parties != null) foreach (var k in parties) m_Suivis.Remove(k);
        }

        /// Coupe franche : les voix du jeu en cours s'éteignent en `fondu` s ; celles des sources encore actives sont
        /// recréées à l'image enregistrée suivante (à leur position en temps de jeu, placées par la nouvelle caméra).
        public void CouperJeu(float fondu = 0.04f)
        {
            foreach (var v in m_Voix) if (v.bus == Bus.Jeu && v.source != 0) Fondre(v, fondu);
            foreach (var su in m_Suivis.Values) su.voix = null;
        }

        void Spatial(AudioSource s, Transform auditeur, out float g, out float d)
        {
            float vol = s.volume * gainJeu;
            float blend = Mathf.Clamp01(s.spatialBlend);
            float att = 1f, pan = 0f;
            if (blend > 0.001f && auditeur != null)
            {
                Vector3 v = s.transform.position - auditeur.position;
                float dist = v.magnitude;
                float min = Mathf.Max(0.01f, s.minDistance), max = Mathf.Max(min + 0.01f, s.maxDistance);
                float a;
                switch (s.rolloffMode)
                {
                    case AudioRolloffMode.Linear: a = Mathf.Clamp01(1f - (dist - min) / (max - min)); break;
                    case AudioRolloffMode.Custom:
                        var courbe = s.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
                        a = courbe != null ? Mathf.Clamp01(courbe.Evaluate(dist / max)) : 1f; break;
                    default: a = dist <= min ? 1f : Mathf.Clamp01(min / dist); if (dist > max) a = 0f; break;
                }
                att = Mathf.Lerp(1f, a, blend);
                if (dist > 0.01f) pan = Vector3.Dot(v / dist, auditeur.right) * blend * 0.75f;
            }
            Pan(vol * att * 1.4142f, pan, out g, out d);
        }

        // ------------------------------------------------------------------ Rendu

        /// Mixe les `n` échantillons suivants (entrelacés gauche/droite dans `sortie`, longueur 2n) et avance la tête.
        public void Mixer(float[] sortie, int n)
        {
            Array.Clear(sortie, 0, n * 2);
            long t0 = Position;
            for (int vi = 0; vi < m_Voix.Count; vi++)
            {
                var v = m_Voix[vi];
                if (v.finie) continue;
                int b = (int)v.bus;
                int i0 = (int)Math.Max(0, v.debut - t0);
                if (i0 >= n) continue;
                int canaux = Mathf.Max(1, v.canaux);
                int longueur = v.donnees.Length / canaux;
                for (int i = i0; i < n; i++)
                {
                    // Rampe de gain (suivi spatial ou Gain) et fondu de sortie.
                    if (v.cibleG >= 0f && v.fonduRestant < 0)
                    {
                        float k = 1f / Mathf.Max(1, v.fonduTotal);
                        v.gainG += (v.cibleG - v.gainG) * k * 3f; v.gainD += (v.cibleD - v.gainD) * k * 3f;
                    }
                    float f = 1f;
                    if (v.fonduRestant >= 0)
                    {
                        if (v.fonduRestant == 0) { v.finie = true; break; }
                        f = v.fonduRestant / (float)Mathf.Max(1, v.fonduTotal);
                        v.fonduRestant--;
                    }
                    int a = (int)v.pos;
                    if (a >= longueur)
                    {
                        if (!v.boucle) { v.finie = true; break; }
                        v.pos -= longueur; a = (int)v.pos;
                    }
                    float fr = (float)(v.pos - a);
                    int c = a + 1 < longueur ? a + 1 : (v.boucle ? 0 : a);
                    float sg, sd;
                    if (canaux == 1)
                    {
                        sg = v.donnees[a] * (1 - fr) + v.donnees[c] * fr; sd = sg;
                    }
                    else
                    {
                        sg = v.donnees[a * canaux] * (1 - fr) + v.donnees[c * canaux] * fr;
                        sd = v.donnees[a * canaux + 1] * (1 - fr) + v.donnees[c * canaux + 1] * fr;
                    }
                    float gb = m_GainBus[b] * f;
                    sortie[2 * i] += sg * v.gainG * gb;
                    sortie[2 * i + 1] += sd * v.gainD * gb;
                    v.pos += v.pas;
                }
            }
            // Bus et maître : rampes appliquées échantillon par échantillon (approximation : le gain de bus est lu par voix
            // plus haut ; on avance ici la rampe d'une image).
            for (int b = 0; b < 3; b++)
            {
                if (m_GainBus[b] == m_CibleBus[b]) continue;
                float pas = m_PasBus[b] * n;
                m_GainBus[b] = Mathf.MoveTowards(m_GainBus[b], m_CibleBus[b], pas);
            }
            for (int i = 0; i < n; i++)
            {
                if (m_Maitre != m_CibleMaitre) m_Maitre = Mathf.MoveTowards(m_Maitre, m_CibleMaitre, m_PasMaitre);
                sortie[2 * i] = Limiter(sortie[2 * i] * m_Maitre);
                sortie[2 * i + 1] = Limiter(sortie[2 * i + 1] * m_Maitre);
            }
            m_Voix.RemoveAll(v => v.finie);
            Position += n;
            if (m_Wav16 != null)
            {
                for (int i = 0; i < n * 2; i++) m_Wav16.Write((short)Mathf.Clamp(Mathf.RoundToInt(sortie[i] * 32767f), -32767, 32767));
                m_Wav16Echantillons += n;
            }
        }

        float Limiter(float x)
        {
            float a = Mathf.Abs(x);
            if (a > CreteMax) CreteMax = a;
            if (a <= 0.8f) return x;
            float y = 0.8f + 0.2f * (float)Math.Tanh((a - 0.8f) / 0.2f);
            return x < 0f ? -y : y;
        }

        // ------------------------------------------------------------------ Copie WAV du mixage (vérification)

        public void OuvrirCopie(string chemin)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(chemin)));
            m_Wav16 = new BinaryWriter(File.Create(chemin));
            m_Wav16.Write(new byte[44]);
            m_Wav16Echantillons = 0;
        }

        public void FermerCopie()
        {
            if (m_Wav16 == null) return;
            long octets = m_Wav16Echantillons * 4;
            var f = m_Wav16.BaseStream;
            f.Seek(0, SeekOrigin.Begin);
            m_Wav16.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); m_Wav16.Write((int)(36 + octets));
            m_Wav16.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); m_Wav16.Write(16); m_Wav16.Write((short)1); m_Wav16.Write((short)2);
            m_Wav16.Write(Taux); m_Wav16.Write(Taux * 4); m_Wav16.Write((short)4); m_Wav16.Write((short)16);
            m_Wav16.Write(System.Text.Encoding.ASCII.GetBytes("data")); m_Wav16.Write((int)octets);
            m_Wav16.Close();
            m_Wav16 = null;
        }
    }
}
