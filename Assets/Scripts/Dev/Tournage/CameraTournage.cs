using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Deathless.Dev.Tournage
{
    /// Caméra de tournage de la bande-annonce, indépendante de CameraEpaule : une Camera à elle (rendue à la main dans
    /// une RenderTexture par EncodeurTrailer, jamais à l'écran), réglages URP recopiés de la caméra du jeu (post-traitement,
    /// volumes), et un **mouvement** : fonction du temps du plan (secondes de vidéo, pas de jeu) vers une pose (position,
    /// rotation, champ). Constructeurs : Rail (positions clés interpolées en Catmull-Rom, regard interpolé), Orbite,
    /// Suivre (cible mobile, lissée), Fixe, CameraJeu (recopie la caméra du jeu, ivresse comprise). Secousses ajoutées par
    /// Secouer (impact), roulis supplémentaire par Roulis.
    public class CameraTournage : MonoBehaviour
    {
        public struct Pose
        {
            public Vector3 position;
            public Quaternion rotation;
            public float champ;
            public Pose(Vector3 p, Quaternion r, float c) { position = p; rotation = r; champ = c; }
            public static Pose Regard(Vector3 p, Vector3 cible, float champ, float roulis = 0f)
            {
                Vector3 d = cible - p;
                var r = d.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(d) : Quaternion.identity;
                if (roulis != 0f) r *= Quaternion.Euler(0f, 0f, roulis);
                return new Pose(p, r, champ);
            }
        }

        /// Position clé d'un rail : instant (s du plan), position, point regardé, champ vertical (degrés).
        public struct Cle
        {
            public float t;
            public Vector3 position, regard;
            public float champ;
            public Cle(float t, Vector3 position, Vector3 regard, float champ) { this.t = t; this.position = position; this.regard = regard; this.champ = champ; }
        }

        public Camera Cam { get; private set; }
        public Camera Modele { get; private set; }
        public Func<float, Pose> Mouvement;
        public Pose Derniere { get; private set; }

        struct Choc { public float t, amplitude, duree; }
        readonly List<Choc> m_Chocs = new List<Choc>();
        /// Roulis ajouté (degrés) en fonction du temps du plan (ivresse appuyée) ; null : aucun.
        public Func<float, float> Roulis;

        public static CameraTournage Creer(Transform parent)
        {
            var go = new GameObject("CameraTournage");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<CameraTournage>();
            c.Cam = go.AddComponent<Camera>();
            c.Cam.enabled = false;   // rendue à la main (EncodeurTrailer), jamais à l'écran
            return c;
        }

        /// Recopie les réglages de la caméra du jeu (scène courante) : à rappeler après chaque chargement de scène.
        public void Preparer(Camera modele)
        {
            Modele = modele;
            if (modele == null) return;
            Cam.CopyFrom(modele);
            Cam.enabled = false;
            Cam.targetTexture = null;
            var src = modele.GetUniversalAdditionalCameraData();
            var dst = Cam.GetUniversalAdditionalCameraData();
            if (src != null && dst != null)
            {
                dst.renderPostProcessing = src.renderPostProcessing;
                dst.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                dst.antialiasingQuality = AntialiasingQuality.High;
                dst.volumeLayerMask = src.volumeLayerMask;
                dst.volumeTrigger = transform;
                dst.renderShadows = src.renderShadows;
                dst.stopNaN = src.stopNaN;
                dst.dithering = true;
                dst.requiresColorOption = src.requiresColorOption;
                dst.requiresDepthOption = src.requiresDepthOption;
                dst.renderType = CameraRenderType.Base;
            }
            Cam.nearClipPlane = 0.08f;
            Cam.farClipPlane = Mathf.Max(600f, modele.farClipPlane);
        }

        public void Secouer(float t, float amplitude, float duree) => m_Chocs.Add(new Choc { t = t, amplitude = amplitude, duree = duree });

        public void NouveauPlan()
        {
            m_Chocs.Clear();
            Roulis = null;
            Mouvement = null;
        }

        /// Pose la caméra pour l'instant `t` du plan (juste avant le rendu).
        public void Appliquer(float t)
        {
            if (Modele != null)
            {
                // Le donjon change le fond de la caméra du jeu (ambiance) : on suit.
                Cam.clearFlags = Modele.clearFlags;
                Cam.backgroundColor = Modele.backgroundColor;
                Cam.cullingMask = Modele.cullingMask;
            }
            Pose p = Mouvement != null ? Mouvement(t) : (Modele != null ? new Pose(Modele.transform.position, Modele.transform.rotation, Modele.fieldOfView) : Derniere);
            foreach (var c in m_Chocs)
            {
                float u = t - c.t;
                if (u < 0f || u > c.duree) continue;
                float a = c.amplitude * (1f - u / c.duree) * (1f - u / c.duree);
                p.position += p.rotation * new Vector3(Mathf.Sin(u * 71f) * a, Mathf.Sin(u * 53f + 1.3f) * a * 0.8f, 0f);
                p.rotation *= Quaternion.Euler(Mathf.Sin(u * 61f + 0.7f) * a * 12f, 0f, Mathf.Sin(u * 47f) * a * 20f);
            }
            if (Roulis != null) p.rotation *= Quaternion.Euler(0f, 0f, Roulis(t));
            transform.SetPositionAndRotation(p.position, p.rotation);
            Cam.fieldOfView = p.champ;
            Derniere = p;
        }

        // ------------------------------------------------------------------ Mouvements

        public static Func<float, Pose> Fixe(Vector3 position, Vector3 regard, float champ) => t => Pose.Regard(position, regard, champ);

        /// Rail : positions et regards interpolés (Catmull-Rom), champ interpolé ; `lisse` : entrée et sortie adoucies.
        public static Func<float, Pose> Rail(bool lisse, params Cle[] cles)
        {
            if (cles == null || cles.Length == 0) return null;
            return t =>
            {
                if (cles.Length == 1 || t <= cles[0].t) return Pose.Regard(cles[0].position, cles[0].regard, cles[0].champ);
                var der = cles[cles.Length - 1];
                if (t >= der.t) return Pose.Regard(der.position, der.regard, der.champ);
                int i = 0;
                while (i < cles.Length - 2 && t > cles[i + 1].t) i++;
                float u = Mathf.InverseLerp(cles[i].t, cles[i + 1].t, t);
                if (lisse) u = u * u * (3f - 2f * u);
                var a = cles[Mathf.Max(0, i - 1)]; var b = cles[i]; var c = cles[i + 1]; var d = cles[Mathf.Min(cles.Length - 1, i + 2)];
                Vector3 pos = CatmullRom(a.position, b.position, c.position, d.position, u);
                Vector3 reg = CatmullRom(a.regard, b.regard, c.regard, d.regard, u);
                return Pose.Regard(pos, reg, Mathf.Lerp(b.champ, c.champ, u));
            };
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// Orbite autour d'un centre (éventuellement mobile) : rayon, hauteur, angle de départ (degrés, 0 = -Z du centre),
        /// vitesse angulaire (degrés/s), point regardé = centre + decalageRegard.
        public static Func<float, Pose> Orbite(Func<Vector3> centre, float rayon, float hauteur, float angle0, float vitesse, Vector3 decalageRegard, float champ, float rayonFin = -1f)
        {
            return t =>
            {
                Vector3 c = centre();
                float a = (angle0 + vitesse * t) * Mathf.Deg2Rad;
                float r = rayonFin > 0f ? Mathf.Lerp(rayon, rayonFin, Mathf.Clamp01(t / 4f)) : rayon;
                Vector3 p = c + new Vector3(Mathf.Sin(a) * r, hauteur, -Mathf.Cos(a) * r);
                return Pose.Regard(p, c + decalageRegard, champ);
            };
        }

        /// Suit une cible : position = cible + decalage exprimé dans le repère de la cible (lacet seul), regard = cible +
        /// regardLocal ; lissage exponentiel (0 : sec). L'état du lissage vit dans la fonction.
        public static Func<float, Pose> Suivre(Transform cible, Vector3 decalage, Vector3 regardLocal, float champ, float lissage = 6f)
        {
            bool init = false; Vector3 pos = Vector3.zero, reg = Vector3.zero; float dernier = -1f;
            return t =>
            {
                if (cible == null) return Pose.Regard(pos, reg, champ);
                var lacet = Quaternion.Euler(0f, cible.eulerAngles.y, 0f);
                Vector3 p = cible.position + lacet * decalage;
                Vector3 r = cible.position + lacet * regardLocal;
                if (!init || lissage <= 0f) { pos = p; reg = r; init = true; }
                else
                {
                    float dt = Mathf.Max(0f, t - dernier);
                    float k = 1f - Mathf.Exp(-lissage * dt);
                    pos = Vector3.Lerp(pos, p, k); reg = Vector3.Lerp(reg, r, k);
                }
                dernier = t;
                return Pose.Regard(pos, reg, champ);
            };
        }

        /// Recopie la caméra du jeu (CameraEpaule : épaule, ivresse).
        public static Func<float, Pose> CameraJeu(Camera jeu, float champ = -1f)
            => t => jeu != null ? new Pose(jeu.transform.position, jeu.transform.rotation, champ > 0f ? champ : jeu.fieldOfView) : default;
    }
}
