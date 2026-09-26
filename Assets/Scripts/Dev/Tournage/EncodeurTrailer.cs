#if UNITY_EDITOR
using System;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;

namespace Deathless.Dev.Tournage
{
    /// Encodage continu de la bande-annonce (même méthode que EnregistreurBoucle / ClipsWiki : MediaEncoder, H.264) avec
    /// une piste audio AAC 48 kHz stéréo mixée par MixeurTrailer. Une image = rendu de la caméra de tournage dans une
    /// RenderTexture, puis composition sur le processeur de la couche d'interface (textes, HUD : panneau UI Toolkit rendu
    /// dans sa propre RenderTexture, alpha prémultiplié ou non, détecté), voile noir (fondus), zoom numérique facultatif.
    /// Les plans sont enchaînés dans le même fichier : pas de concaténation après coup.
    public class EncodeurTrailer : IDisposable
    {
        public readonly int largeur, hauteur, ips;
        public string Chemin { get; private set; }
        public int Images { get; private set; }
        public double Temps => Images / (double)ips;
        public MixeurTrailer Mixeur { get; }
        public RenderTexture RtInterface { get; private set; }
        /// Dernière image composée (pour les vignettes de contrôle).
        public Texture2D Image => m_Tex;
        public string Erreur { get; private set; } = "";
        public bool AlphaPremultiplie { get; private set; } = true;

        MediaEncoder m_Enc;
        RenderTexture m_RtCam, m_RtZoom;
        Texture2D m_Tex, m_TexUi;
        float[] m_Audio;
        long m_AudioEcrit;
        int m_TestsAlpha;

        public EncodeurTrailer(int largeur, int hauteur, int ips, MixeurTrailer mixeur)
        {
            this.largeur = largeur; this.hauteur = hauteur; this.ips = ips;
            Mixeur = mixeur;
            m_RtCam = new RenderTexture(largeur, hauteur, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4, name = "TournageCamera" };
            m_RtCam.Create();
            RtInterface = new RenderTexture(largeur, hauteur, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "TournageInterface" };
            RtInterface.Create();
            m_Tex = new Texture2D(largeur, hauteur, TextureFormat.RGBA32, false);
            m_TexUi = new Texture2D(largeur, hauteur, TextureFormat.RGBA32, false);
            m_Audio = new float[(MixeurTrailer.Taux / Mathf.Max(1, ips) + 4) * 2];
        }

        public void Ouvrir(string chemin, uint debitKbps)
        {
            Chemin = chemin;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(chemin)));
            var h264 = new H264EncoderAttributes { gopSize = (uint)ips, numConsecutiveBFrames = 2, profile = VideoEncodingProfile.H264High };
            var video = new VideoTrackEncoderAttributes(h264)
            {
                frameRate = new MediaRational(ips),
                width = (uint)largeur,
                height = (uint)hauteur,
                targetBitRate = debitKbps * 1000,
                includeAlpha = false,
            };
            var audio = new AudioTrackAttributes { sampleRate = new MediaRational(MixeurTrailer.Taux), channelCount = 2, language = "fr" };
            m_Enc = new MediaEncoder(chemin, video, audio);
            Images = 0; m_AudioEcrit = 0;
        }

        /// Rend, compose et encode une image, puis la tranche d'audio correspondante.
        /// `noir` : 0 image normale, 1 noir complet (sous l'interface si `noirSousTexte`) ; `zoom` ≥ 1 vers `centreZoom`
        /// (0-1, origine en bas à gauche).
        public void Ajouter(Camera cam, bool interfaceVisible, float noir, bool noirSousTexte, float zoom, Vector2 centreZoom)
        {
            Composer(cam, interfaceVisible, noir, noirSousTexte, zoom, centreZoom);
            if (m_Enc != null) m_Enc.AddFrame(m_Tex);
            Images++;
            // Audio : autant d'échantillons que la vidéo en demande jusqu'à la fin de cette image (compte exact).
            long cible = (long)Images * MixeurTrailer.Taux / ips;
            int n = (int)(cible - m_AudioEcrit);
            if (n > 0)
            {
                if (m_Audio.Length < n * 2) m_Audio = new float[n * 2];
                Mixeur.Mixer(m_Audio, n);
                if (m_Enc != null)
                {
                    var na = new NativeArray<float>(n * 2, Allocator.Temp);
                    NativeArray<float>.Copy(m_Audio, na, n * 2);
                    m_Enc.AddSamples(na);
                    na.Dispose();
                }
                m_AudioEcrit += n;
            }
        }

        /// Composition seule (vignettes d'essai, sans encodage).
        public Texture2D Composer(Camera cam, bool interfaceVisible, float noir, bool noirSousTexte, float zoom, Vector2 centreZoom)
        {
            var actif = RenderTexture.active;
            if (cam != null)
            {
                var ancienne = cam.targetTexture;
                cam.targetTexture = m_RtCam;
                cam.Render();
                cam.targetTexture = ancienne;
            }
            RenderTexture.active = m_RtCam;
            m_Tex.ReadPixels(new Rect(0, 0, largeur, hauteur), 0, 0, false);
            Color32[] px = m_Tex.GetPixels32();
            byte n8 = (byte)Mathf.RoundToInt(255f * (1f - Mathf.Clamp01(noir)));
            if (noirSousTexte && noir > 0f) Assombrir(px, n8);
            if (interfaceVisible)
            {
                RenderTexture.active = RtInterface;
                m_TexUi.ReadPixels(new Rect(0, 0, largeur, hauteur), 0, 0, false);
                var ui = m_TexUi.GetPixels32();
                if (!m_AlphaDecide) DetecterAlpha(ui);
                // Composition en lumière linéaire (projet en espace Linear, RenderTextures sRGB) : les deux couches sont
                // décodées par table, mélangées, puis réencodées en sRGB.
                bool pm = AlphaPremultiplie;
                for (int i = 0; i < px.Length; i++)
                {
                    var u = ui[i];
                    if (u.a == 0) continue;
                    var c = px[i];
                    if (u.a == 255 && pm) { c.r = u.r; c.g = u.g; c.b = u.b; px[i] = c; continue; }
                    float a = u.a / 255f, ia = 1f - a;
                    float ur = s_Lin[u.r], ug = s_Lin[u.g], ub = s_Lin[u.b];
                    if (!pm) { ur *= a; ug *= a; ub *= a; }
                    c.r = s_Srgb[(int)(Mathf.Min(1f, ur + s_Lin[c.r] * ia) * 4095f)];
                    c.g = s_Srgb[(int)(Mathf.Min(1f, ug + s_Lin[c.g] * ia) * 4095f)];
                    c.b = s_Srgb[(int)(Mathf.Min(1f, ub + s_Lin[c.b] * ia) * 4095f)];
                    px[i] = c;
                }
            }
            if (!noirSousTexte && noir > 0f) Assombrir(px, n8);
            for (int i = 0; i < px.Length; i++) px[i].a = 255;
            m_Tex.SetPixels32(px);
            if (zoom > 1.001f)
            {
                m_Tex.Apply(false);
                if (m_RtZoom == null) { m_RtZoom = new RenderTexture(largeur, hauteur, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); m_RtZoom.Create(); }
                Vector2 echelle = new Vector2(1f / zoom, 1f / zoom);
                Vector2 decalage = new Vector2(Mathf.Clamp(centreZoom.x - 0.5f / zoom, 0f, 1f - 1f / zoom), Mathf.Clamp(centreZoom.y - 0.5f / zoom, 0f, 1f - 1f / zoom));
                Graphics.Blit(m_Tex, m_RtZoom, echelle, decalage);
                RenderTexture.active = m_RtZoom;
                m_Tex.ReadPixels(new Rect(0, 0, largeur, hauteur), 0, 0, false);
            }
            RenderTexture.active = actif;
            return m_Tex;
        }

        static void Assombrir(Color32[] px, byte f)
        {
            if (f >= 255) return;
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                c.r = (byte)(c.r * f / 255); c.g = (byte)(c.g * f / 255); c.b = (byte)(c.b * f / 255);
                px[i] = c;
            }
        }

        static readonly float[] s_Lin = TableLineaire();
        static readonly byte[] s_Srgb = TableSrgb();
        bool m_AlphaDecide;

        static float[] TableLineaire()
        {
            var t = new float[256];
            for (int i = 0; i < 256; i++) t[i] = Mathf.GammaToLinearSpace(i / 255f);
            return t;
        }

        static byte[] TableSrgb()
        {
            var t = new byte[4096];
            for (int i = 0; i < 4096; i++) t[i] = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.LinearToGammaSpace(i / 4095f) * 255f), 0, 255);
            return t;
        }

        /// Alpha prémultiplié (sortie habituelle d'UI Toolkit) si, en linéaire, aucune couleur ne dépasse son alpha ;
        /// décidé une fois pour toutes sur les premières images assez fournies en bords semi-transparents.
        void DetecterAlpha(Color32[] ui)
        {
            int droits = 0, vus = 0;
            for (int i = 0; i < ui.Length; i += 5)
            {
                var u = ui[i];
                if (u.a < 24 || u.a > 230) continue;
                vus++;
                float m = Mathf.Max(s_Lin[u.r], Mathf.Max(s_Lin[u.g], s_Lin[u.b]));
                if (m > u.a / 255f + 0.04f) droits++;
            }
            if (vus < 400) return;
            m_TestsAlpha++;
            AlphaPremultiplie = droits < vus / 10;
            if (m_TestsAlpha >= 3) m_AlphaDecide = true;
        }

        public void Fermer()
        {
            try { m_Enc?.Dispose(); }
            catch (Exception e) { Erreur = e.Message; }
            m_Enc = null;
        }

        public void Dispose()
        {
            Fermer();
            if (m_RtCam != null) { m_RtCam.Release(); UnityEngine.Object.Destroy(m_RtCam); }
            if (RtInterface != null) { RtInterface.Release(); UnityEngine.Object.Destroy(RtInterface); }
            if (m_RtZoom != null) { m_RtZoom.Release(); UnityEngine.Object.Destroy(m_RtZoom); }
            if (m_Tex != null) UnityEngine.Object.Destroy(m_Tex);
            if (m_TexUi != null) UnityEngine.Object.Destroy(m_TexUi);
        }

        /// Enregistre l'image composée courante en PNG (réduite d'un facteur `reduction`).
        public void Vignette(string chemin, int reduction = 2)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(chemin)));
            if (reduction <= 1) { File.WriteAllBytes(chemin, m_Tex.EncodeToPNG()); return; }
            int w = largeur / reduction, h = hauteur / reduction;
            var src = m_Tex.GetPixels32();
            var petit = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = src[(y * reduction) * largeur + x * reduction];
            petit.SetPixels32(px); petit.Apply(false);
            File.WriteAllBytes(chemin, petit.EncodeToPNG());
            UnityEngine.Object.Destroy(petit);
        }
    }
}
#endif
