#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace Deathless.Dev.Tournage
{
    /// Tournage de la bande-annonce Steam (Docs/trailer-storyboard.md, structure recommandée, 12 plans, 39 s), en Play dans
    /// la scène Village, lancé par execute_code :
    ///   Deathless.Dev.Tournage.Tournage.Lancer();                         // les 12 plans, encodés
    ///   Deathless.Dev.Tournage.Tournage.Lancer("1,2", apercu: true);      // essai : vignettes PNG, sans encodage
    /// État : Tournage.Etat (texte), Tournage.Termine, Tournage.Journal. Sortie : Builds/Trailer/Deathless-trailer-v1.mp4
    /// (+ copie WAV du mixage), vignettes d'essai dans Builds/Trailer/apercu/, planche de contrôle (images extraites du
    /// MP4 toutes les 3 s à partir de 1,5 s, par un VideoPlayer) dans Assets/Screenshots/trailer_planche.png.
    ///
    /// Déroulé : chaque plan prépare son décor sans enregistrer (chargement de scène avec la classe voulue, placement,
    /// ennemis posés), puis « Prise » enregistre un nombre exact d'images : temps de jeu figé sur la cadence de capture
    /// (Time.captureFramerate), ralentis par Time.timeScale. Un seul encodage continu : les plans se suivent dans le fichier.
    public class Tournage : MonoBehaviour
    {
        public static Tournage Instance { get; private set; }
        static string s_Etat = "inactif";
        public static string Etat => s_Etat;
        public static bool Termine { get; private set; }
        public static readonly List<string> Journal = new List<string>();

        public string plans = "1-12";
        public bool apercu;
        public int ips = 60, largeur = 1920, hauteur = 1080;
        public uint debitKbps = 16000;
        public string sortie = "Builds/Trailer/Deathless-trailer-v1.mp4";
        public string dossierAudio = "Builds/Trailer/audio";
        public string planche = "Assets/Screenshots/trailer_planche.png";
        /// Vignettes PNG toutes les `pasVignettes` s de plan (0 : aucune ; en aperçu : 0,5 s par défaut).
        public float pasVignettes;

        public EncodeurTrailer Enc { get; private set; }
        public CameraTournage Cam { get; private set; }
        public HabillageTrailer Hab { get; private set; }
        public MixeurTrailer Mix { get; private set; }
        public ScenarioTrailer Sc { get; private set; }

        /// Temps du plan en cours (s de vidéo) et numéro du plan.
        public float TempsPlan { get; private set; }
        public int Plan { get; private set; }
        /// Images enregistrées depuis le début (toutes prises confondues).
        public int ImagesTotal { get; private set; }
        public bool Enregistre { get; private set; }
        /// Voile noir appliqué après l'interface (fondu final) : fonction du temps du plan.
        public Func<float, float> NoirTotal;
        public float Zoom = 1f;
        public Vector2 CentreZoom = new Vector2(0.5f, 0.5f);
        public readonly Dictionary<int, float> DureesPlans = new Dictionary<int, float>();

        float m_AncienVolume = 1f;
        int m_ImagesDebutPrise;
        float m_DepuisPrise;
        bool m_Arret;

        // ------------------------------------------------------------------ Entrée

        public static void Lancer(string plans = "1-12", bool apercu = false, int ips = 60, float pasVignettes = -1f, string sortie = null)
        {
            if (!Application.isPlaying) { s_Etat = "pas en Play"; return; }
            if (Instance != null) { s_Etat = "déjà en cours"; return; }
            Termine = false;
            Journal.Clear();
            var go = new GameObject("Tournage");
            DontDestroyOnLoad(go);
            var t = go.AddComponent<Tournage>();
            t.plans = plans; t.apercu = apercu; t.ips = ips;
            t.pasVignettes = pasVignettes >= 0f ? pasVignettes : (apercu ? 0.5f : 0f);
            if (apercu) { t.largeur = 1920; t.hauteur = 1080; }
            if (!string.IsNullOrEmpty(sortie)) t.sortie = sortie;
            Instance = t;
        }

        public static void Arreter()
        {
            if (Instance != null) Instance.m_Arret = true;
        }

        public static void Log(string texte)
        {
            string l = "[" + (Instance != null ? "plan " + Instance.Plan + " t=" + Instance.TempsPlan.ToString("0.00") : "-") + "] " + texte;
            Journal.Add(l);
            Debug.Log("[Tournage] " + l);
        }

        static IEnumerable<int> Liste(string s)
        {
            foreach (var morceau in s.Split(','))
            {
                var m = morceau.Trim();
                if (m.Length == 0) continue;
                int tiret = m.IndexOf('-');
                if (tiret > 0)
                {
                    int a = int.Parse(m.Substring(0, tiret)), b = int.Parse(m.Substring(tiret + 1));
                    for (int i = a; i <= b; i++) yield return i;
                }
                else yield return int.Parse(m);
            }
        }

        // ------------------------------------------------------------------ Déroulé

        IEnumerator Start()
        {
            s_Etat = "préparation";
            string racine = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/') + "/";
            Mix = new MixeurTrailer();
            Mix.Charger(racine + dossierAudio);
            foreach (var l in Mix.Journal) Log(l);
            Enc = new EncodeurTrailer(largeur, hauteur, ips, Mix);
            Cam = CameraTournage.Creer(transform);
            Hab = HabillageTrailer.Creer(transform, Enc.RtInterface);
            Sc = new ScenarioTrailer(this);
            m_AncienVolume = AudioListener.volume;
            AudioListener.volume = 0f;   // le son vient du mixage ; l'éditeur reste muet
            Time.captureFramerate = ips;
            if (!apercu)
            {
                Enc.Ouvrir(racine + sortie, debitKbps);
                Mix.OuvrirCopie(racine + Path.ChangeExtension(sortie, null) + "_mixage.wav");
                Log("encodage : " + sortie + " (" + largeur + "x" + hauteur + ", " + ips + " i/s, " + debitKbps + " kb/s, AAC 48 kHz stéréo)");
            }
            StartCoroutine(Sondage());
            var liste = new List<int>(Liste(plans));
            foreach (int p in liste)
            {
                if (m_Arret) break;
                Plan = p;
                TempsPlan = 0f;
                s_Etat = "plan " + p + " (préparation)";
                int avant = ImagesTotal;
                Mix.CouperJeu();
                Mix.GainMaitre(1f, 0.01f);
                Time.timeScale = 1f;
                NoirTotal = null; Zoom = 1f;
                Cam.NouveauPlan();
                Hab.Effacer();
                yield return Sc.Jouer(p);
                Time.timeScale = 1f;
                DureesPlans[p] = (ImagesTotal - avant) / (float)ips;
                Log("plan " + p + " : " + DureesPlans[p].ToString("0.00") + " s enregistrées");
            }
            s_Etat = "fermeture";
            Time.timeScale = 1f;
            Enc.Fermer();
            Mix.FermerCopie();
            Log("fin : " + ImagesTotal + " images (" + (ImagesTotal / (float)ips).ToString("0.00") + " s), voix de jeu " + Mix.VoixJeuCreees + ", crête " + Mix.CreteMax.ToString("0.00")
                + ", alpha interface " + (Enc.AlphaPremultiplie ? "prémultiplié" : "droit") + (Enc.Erreur.Length > 0 ? ", erreur " + Enc.Erreur : ""));
            var clips = new List<KeyValuePair<string, int>>(Mix.VoixParClip);
            clips.Sort((a, b) => b.Value.CompareTo(a.Value));
            string sons = "";
            for (int i = 0; i < clips.Count && i < 25; i++) sons += clips[i].Key + "×" + clips[i].Value + " ";
            Log("sons du jeu : " + sons);
            Restaurer();
            if (!apercu && !m_Arret)
            {
                s_Etat = "planche";
                yield return Planche(racine + sortie, racine + planche);
            }
            s_Etat = m_Arret ? "arrêté" : "terminé";
            Termine = true;
            Destroy(gameObject);
        }

        /// Sonde les sources audio à chaque image hors prise (départs des sons, pour les reprendre au bon endroit).
        IEnumerator Sondage()
        {
            var fin = new WaitForEndOfFrame();
            while (true)
            {
                yield return fin;
                if (!Enregistre && Mix != null && Cam != null) Mix.Sonder(Cam.transform, false);
            }
        }

        void Restaurer()
        {
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            AudioListener.volume = m_AncienVolume;
            if (Sc != null) Sc.Nettoyer();
            if (Hab != null) Hab.Debrancher();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (!Termine)
            {
                Restaurer();
                Enc?.Fermer();
                Mix?.FermerCopie();
                s_Etat = "interrompu";
                Termine = true;
            }
            Enc?.Dispose();
        }

        // ------------------------------------------------------------------ Prises

        /// Instant vidéo (s depuis le début du film) d'un instant `t` du plan en cours.
        public double Video(float t) => m_ImagesDebutPrise / (double)ips + (t - m_DepuisPrise);

        /// Enregistre `duree` s de plan (à partir de l'instant `depuis` du plan). `image` est appelée à chaque image,
        /// avant le rendu, avec le temps du plan ; le rendu, la composition et l'encodage suivent en fin d'image.
        public IEnumerator Prise(float duree, Action<float> image, float depuis = 0f)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(duree * ips));
            m_ImagesDebutPrise = ImagesTotal;
            m_DepuisPrise = depuis;
            Enregistre = true;
            s_Etat = "plan " + Plan + " (prise)";
            float prochaineVignette = depuis;
            var fin = new WaitForEndOfFrame();
            for (int f = 0; f < n && !m_Arret; f++)
            {
                float t = depuis + f / (float)ips;
                TempsPlan = t;
                try { image?.Invoke(t); }
                catch (Exception e) { Log("erreur dans l'image du plan : " + e.Message); Debug.LogException(e); }
                Hab.Maj(t);
                yield return fin;
                Cam.Appliquer(t);
                Mix.Sonder(Cam.transform, true);
                float noirTotal = NoirTotal != null ? Mathf.Clamp01(NoirTotal(t)) : 0f;
                bool vignette = pasVignettes > 0f && t + 1e-4f >= prochaineVignette;
                if (!apercu)
                {
                    Enc.Ajouter(Cam.Cam, true, noirTotal, false, Zoom, CentreZoom);
                }
                else if (vignette) Enc.Composer(Cam.Cam, true, noirTotal, false, Zoom, CentreZoom);
                if (vignette)
                {
                    prochaineVignette += pasVignettes;
                    Enc.Vignette(Directory.GetParent(Application.dataPath).FullName + "/Builds/Trailer/apercu/plan" + Plan.ToString("00") + "_" + t.ToString("00.00").Replace(',', '.') + ".png", 2);
                }
                ImagesTotal++;
                yield return null;
            }
            Enregistre = false;
        }

        /// Ralenti (Time.timeScale) ; la cadence de capture reste la même : le jeu avance moins par image.
        public void Ralenti(float facteur) => Time.timeScale = Mathf.Clamp(facteur, 0.05f, 1f);

        /// Pose un son d'habillage sur la vidéo à l'instant `t` du plan (attaque du son sur cet instant si `surAttaque`).
        public void Son(string nom, float t, float gain = 1f, bool surAttaque = false, MixeurTrailer.Bus bus = MixeurTrailer.Bus.Habillage)
        {
            if (apercu) return;
            if (surAttaque) Mix.PoserSur(nom, Video(t), gain, false, bus);
            else Mix.Poser(nom, Video(t), gain, bus);
        }

        /// Pose un son pour que son maximum tombe à l'instant `t` du plan.
        public void SonPic(string nom, float t, float gain = 1f) { if (!apercu) Mix.PoserSur(nom, Video(t), gain, true); }

        // ------------------------------------------------------------------ Planche de contrôle

        /// Relit le MP4 (VideoPlayer) : durée, images, pistes ; une image toutes les 3 s, en planche 5 colonnes.
        IEnumerator Planche(string mp4, string png)
        {
            if (!File.Exists(mp4)) { Log("planche : fichier absent " + mp4); yield break; }
            Log("fichier : " + (new FileInfo(mp4).Length / 1048576.0).ToString("0.0") + " Mo");
            var go = new GameObject("LecteurPlanche");
            var vp = go.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.source = VideoSource.Url;
            vp.url = "file://" + mp4.Replace('\\', '/');
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.renderMode = VideoRenderMode.RenderTexture;
            var rt = new RenderTexture(largeur, hauteur, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.Create();
            vp.targetTexture = rt;
            vp.skipOnDrop = false;
            bool pret = false, cherche = false;
            vp.prepareCompleted += _ => pret = true;
            vp.seekCompleted += _ => cherche = true;
            vp.Prepare();
            float t0 = Time.realtimeSinceStartup;
            while (!pret && Time.realtimeSinceStartup - t0 < 20f) yield return null;
            if (!pret) { Log("planche : lecture impossible"); Destroy(go); rt.Release(); yield break; }
            Log("relecture : durée " + vp.length.ToString("0.000") + " s, " + vp.frameCount + " images à " + vp.frameRate.ToString("0.##") + " i/s, " + vp.width + "x" + vp.height + ", pistes audio " + vp.audioTrackCount);
            int colonnes = 5, w = 384, h = 216, marge = 6;
            var instants = new List<float>();
            for (float t = 1.5f; t < (float)vp.length - 0.05f; t += 3f) instants.Add(t);
            int lignes = Mathf.CeilToInt(instants.Count / (float)colonnes);
            int W = colonnes * w + (colonnes + 1) * marge, H = lignes * h + (lignes + 1) * marge;
            var sortiePx = new Color32[W * H];
            for (int i = 0; i < sortiePx.Length; i++) sortiePx[i] = new Color32(22, 26, 36, 255);
            var lu = new Texture2D(largeur, hauteur, TextureFormat.RGBA32, false);
            vp.Play(); vp.Pause();
            for (int k = 0; k < instants.Count; k++)
            {
                cherche = false;
                vp.frame = (long)Mathf.Round(instants[k] * (float)vp.frameRate);
                t0 = Time.realtimeSinceStartup;
                while (!cherche && Time.realtimeSinceStartup - t0 < 5f) yield return null;
                for (int a = 0; a < 3; a++) yield return null;
                var actif = RenderTexture.active;
                RenderTexture.active = rt;
                lu.ReadPixels(new Rect(0, 0, largeur, hauteur), 0, 0, false);
                RenderTexture.active = actif;
                var px = lu.GetPixels32();
                int cx = k % colonnes, cy = lignes - 1 - k / colonnes;
                int ox = marge + cx * (w + marge), oy = marge + cy * (h + marge);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        sortiePx[(oy + y) * W + ox + x] = px[(y * hauteur / h) * largeur + x * largeur / w];
            }
            var img = new Texture2D(W, H, TextureFormat.RGBA32, false);
            img.SetPixels32(sortiePx); img.Apply(false);
            Directory.CreateDirectory(Path.GetDirectoryName(png));
            File.WriteAllBytes(png, img.EncodeToPNG());
            Log("planche : " + instants.Count + " images → " + png);
            Destroy(img); Destroy(lu);
            vp.Stop();
            Destroy(go);
            rt.Release(); Destroy(rt);
        }
    }
}
#endif
