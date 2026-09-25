using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;

namespace Deathless.Jeu
{
    /// Aperçu 3D des classes pour l'écran de choix (IApercuClasse, Docs/ui-v01.md) : petite scène de présentation loin
    /// sous le village (couche 30, exclue de la caméra de jeu), une caméra dédiée qui rend dans une RenderTexture à fond
    /// transparent, un socle hexagonal discret et le modèle équipé de la classe (enfant « Modele » du prefab Heros_<Classe>
    /// de ClassesJeu : modèle, arme, contrôleur d'animation au repos, sans le gameplay). La caméra ne tourne que pendant
    /// Montrer … Cacher. Changement de classe immédiat, apparition par la taille (pas d'alpha) ; rotation lente continue.
    public class ApercuClasse : MonoBehaviour, IApercuClasse
    {
        public const int Couche = 30;
        public static readonly Vector3 Position = new Vector3(0f, -300f, 0f);

        [Header("Rendu")]
        public int largeur = 720, hauteur = 920;
        public float champ = 26f;
        [Header("Présentation")]
        [Tooltip("Rotation lente continue (degrés par seconde).")]
        public float vitesseRotation = 18f;
        [Tooltip("Durée de l'apparition par la taille (s).")]
        public float dureeApparition = 0.28f;
        [Tooltip("Lumières ponctuelles proches (pas de directionnelle : elle pourrait devenir la lumière principale du village).")]
        public Color lumiereCle = new Color(1f, 0.93f, 0.86f);
        public float intensiteCle = 6f;
        public Color lumiereContre = new Color(0.55f, 0.45f, 1f);
        public float intensiteContre = 9f;
        public Color lumiereNyxessa = new Color(0.62f, 0.91f, 0.44f);
        public float intensiteNyxessa = 2f;

        Camera m_Camera;
        RenderTexture m_Rendu;
        Transform m_Plateau;
        readonly Dictionary<string, GameObject> m_Modeles = new Dictionary<string, GameObject>();
        readonly List<PlayableGraph> m_Graphes = new List<PlayableGraph>();
        [Tooltip("Classe à venir (verrouillée) : teinte multipliée sur ses matériaux (un peu assombrie et désaturée, sans alpha).")]
        public Color teinteVerrouillee = new Color(0.62f, 0.62f, 0.68f);
        GameObject m_Actif;
        float m_Apparition = 1f;
        float m_Angle = 200f;

        public Texture Rendu => m_Rendu;

        /// Crée l'aperçu (une fois par scène) et l'enregistre dans DonneesUI.
        public static ApercuClasse Creer()
        {
            var go = new GameObject("ApercuClasse");
            go.transform.position = Position;
            var a = go.AddComponent<ApercuClasse>();
            DonneesUI.ApercuClasse = a;
            return a;
        }

        void Awake()
        {
            m_Rendu = new RenderTexture(largeur, hauteur, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            { name = "ApercuClasse", antiAliasing = 4 };

            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.25f, -6.6f);
            camGo.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);
            m_Camera = camGo.AddComponent<Camera>();
            m_Camera.cullingMask = 1 << Couche;
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            m_Camera.fieldOfView = champ;
            m_Camera.nearClipPlane = 0.1f;
            m_Camera.farClipPlane = 20f;
            m_Camera.targetTexture = m_Rendu;
            m_Camera.enabled = false;
            var urp = m_Camera.GetUniversalAdditionalCameraData();
            urp.renderPostProcessing = false;   // garde l'alpha du fond
            urp.renderShadows = false;
            urp.requiresDepthTexture = false;
            urp.requiresColorTexture = false;

            // La caméra de jeu ne voit pas la couche de présentation.
            foreach (var c in Camera.allCameras) if (c != m_Camera) c.cullingMask &= ~(1 << Couche);
            var principale = Camera.main;
            if (principale != null) principale.cullingMask &= ~(1 << Couche);

            Lumiere("Cle", lumiereCle, intensiteCle, new Vector3(-2.2f, 2.9f, -2.6f));
            Lumiere("Contre", lumiereContre, intensiteContre, new Vector3(1.6f, 2.3f, 2.2f));
            Lumiere("Nyxessa", lumiereNyxessa, intensiteNyxessa, new Vector3(2.6f, 0.7f, -0.8f));

            m_Plateau = new GameObject("Plateau").transform;
            m_Plateau.SetParent(transform, false);
            Socle();
        }

        void Lumiere(string nom, Color couleur, float intensite, Vector3 position)
        {
            var go = new GameObject("Lumiere_" + nom);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 9f;
            l.color = couleur;
            l.intensity = intensite;
            l.shadows = LightShadows.None;
            l.cullingMask = 1 << Couche;
        }

        /// Socle hexagonal bas, facettes nettes (les dalles du village), ardoise sombre.
        void Socle()
        {
            const float rayon = 0.78f, haut = 0.1f;
            var v = new List<Vector3>();
            var t = new List<int>();
            var coins = new Vector3[6];
            for (var i = 0; i < 6; i++)
            {
                var a = Mathf.Deg2Rad * (60f * i + 30f);
                coins[i] = new Vector3(Mathf.Cos(a) * rayon, 0f, Mathf.Sin(a) * rayon);
            }
            // Dessus (triangles en éventail, sommets propres : normales plates).
            for (var i = 0; i < 6; i++)
            {
                var n = v.Count;
                v.Add(Vector3.up * haut); v.Add(coins[(i + 1) % 6] + Vector3.up * haut); v.Add(coins[i] + Vector3.up * haut);
                t.Add(n); t.Add(n + 1); t.Add(n + 2);
            }
            // Côtés.
            for (var i = 0; i < 6; i++)
            {
                var n = v.Count;
                Vector3 a = coins[i], b = coins[(i + 1) % 6];
                v.Add(a); v.Add(a + Vector3.up * haut); v.Add(b + Vector3.up * haut); v.Add(b);
                t.Add(n); t.Add(n + 1); t.Add(n + 2); t.Add(n); t.Add(n + 2); t.Add(n + 3);
            }
            var mesh = new Mesh { name = "SocleApercu" };
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            var go = new GameObject("Socle");
            go.layer = Couche;
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader) { name = "SocleApercu" };
            mat.SetColor("_BaseColor", new Color(0.17f, 0.19f, 0.26f));
            mat.SetFloat("_Smoothness", 0.25f);
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        GameObject Modele(string id)
        {
            if (m_Modeles.TryGetValue(id, out var m) && m != null) return m;
            var def = ClassesJeu.Courant != null ? ClassesJeu.Courant.Trouver(id) : null;
            var source = def != null && def.prefab != null ? def.prefab.transform.Find("Modele") : null;
            if (source == null) return ModeleVerrouille(id);
            m = Instantiate(source.gameObject, m_Plateau, false);
            m.name = "Apercu_" + id;
            m.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            m.transform.localRotation = Quaternion.identity;
            foreach (var t in m.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Couche;
            foreach (var r in m.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var anim = m.GetComponent<Animator>();
            if (anim != null)
            {
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (anim.runtimeAnimatorController == null && def.controleur != null) anim.runtimeAnimatorController = def.controleur;
            }
            m.SetActive(false);
            m_Modeles[id] = m;
            return m;
        }

        /// Classe à venir : modèle KayKit, arme sous la main droite, pose de repos (Playables, sans contrôleur), matériaux
        /// assombris par un MaterialPropertyBlock.
        GameObject ModeleVerrouille(string id)
        {
            var e = ApercusVerrouilles.Courant != null ? ApercusVerrouilles.Courant.Trouver(id) : null;
            if (e == null || e.modele == null) return null;
            var m = Instantiate(e.modele, m_Plateau, false);
            m.name = "Apercu_" + id;
            m.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            m.transform.localRotation = Quaternion.identity;
            if (e.arme != null)
            {
                var os = MannequinEquip.Trouver(m.transform, e.os);
                if (os != null) Instantiate(e.arme, os, false);
            }
            foreach (var t in m.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Couche;
            var bloc = new MaterialPropertyBlock();
            foreach (var r in m.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.GetPropertyBlock(bloc);
                bloc.SetColor("_BaseColor", teinteVerrouillee);
                r.SetPropertyBlock(bloc);
            }
            var anim = m.GetComponent<Animator>();
            if (anim == null) anim = m.AddComponent<Animator>();
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (e.repos != null)
            {
                AnimationPlayableUtilities.PlayClip(anim, e.repos, out var graphe);
                m_Graphes.Add(graphe);
            }
            m.SetActive(false);
            m_Modeles[id] = m;
            return m;
        }

        public void Montrer(string classeId)
        {
            m_Camera.enabled = true;
            var m = Modele(classeId);
            if (m == m_Actif && m != null) return;
            if (m_Actif != null) m_Actif.SetActive(false);
            m_Actif = m;
            if (m == null) return;
            m.SetActive(true);
            m_Apparition = 0f;
            m.transform.localScale = Vector3.one * 0.001f;
        }

        public void Cacher()
        {
            if (m_Camera != null) m_Camera.enabled = false;
        }

        public void Tourner(float degres) => m_Angle += degres;

        void Update()
        {
            if (m_Camera == null || !m_Camera.enabled) return;
            var dt = Time.unscaledDeltaTime;
            m_Angle += vitesseRotation * dt;
            m_Plateau.localRotation = Quaternion.Euler(0f, m_Angle, 0f);
            if (m_Actif != null && m_Apparition < 1f)
            {
                m_Apparition = Mathf.Min(1f, m_Apparition + dt / Mathf.Max(0.01f, dureeApparition));
                // Apparition par la taille, léger dépassement (sans alpha).
                var k = m_Apparition;
                var s = 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f);
                m_Actif.transform.localScale = Vector3.one * (GameBalance.Courant != null ? GameBalance.Courant.echellePersonnages : 0.8f) * Mathf.Max(0.001f, s);
            }
        }

        void OnDestroy()
        {
            if (ReferenceEquals(DonneesUI.ApercuClasse, this)) DonneesUI.ApercuClasse = null;
            if (m_Rendu != null) m_Rendu.Release();
            foreach (var g in m_Graphes) if (g.IsValid()) g.Destroy();
        }
    }
}
