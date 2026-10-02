using UnityEngine;

namespace Deathless.Jeu
{
    /// Brume de montagne de la carte v5 (02/10/2026, demande de Quentin : adoucir le passage entre les roches Tripo et la
    /// falaise procédurale). Nappes basses et douces posées sur les gradins, les flancs et les crêtes à partir de la hauteur
    /// du haut de la cascade (14 m) : le bas de la falaise reste net, la brume monte et s'épaissit vers le haut (trois bandes
    /// d'altitude, de plus en plus opaques). Même famille que GroundMist : domes facettés aplatis (FireballVisual.BodyMesh),
    /// un seul matériau URP Lit transparent par bande, instancié par le GPU (3 appels de dessin pour toute la brume), sans
    /// texture, sans ombre ; ni vert (blanc bleuté de jour, violet-gris de nuit comme la brume au sol). Les positions sont
    /// calculées par VillageBuilder (menu Deathless > Village > v5, étape Montagne) d'après la roche réelle de la scène ;
    /// au jeu, les nappes dérivent à peine et « respirent ». Purement visuel, rien sur le réseau, aucun lancer de rayon.
    public class BrumeMontagne : MonoBehaviour
    {
        [Tooltip("Matériau URP Lit transparent (sans texture) : copié à l'exécution par bande, l'asset n'est jamais modifié.")]
        public Material modele;
        public Color couleurJour = new Color(0.9f, 0.93f, 0.97f, 1f);
        public Color couleurNuit = new Color(0.45f, 0.42f, 0.66f, 1f);
        [Tooltip("Opacité de jour de chaque bande d'altitude (la brume s'épaissit vers le haut).")]
        public float[] alphaBandes = { 0.12f, 0.18f, 0.26f };
        [Tooltip("De nuit, l'opacité est multipliée par ce facteur et la teinte passe au violet-gris.")]
        public float nuitFacteur = 0.85f;
        public Vector3[] positions;
        [Tooltip("Largeur (m), hauteur (m) et bande d'altitude de chaque nappe.")]
        public float[] largeurs;
        public float[] hauteurs;
        public byte[] bandes;
        public Vector3 vent = new Vector3(0.25f, 0f, 0.1f);

        Material[] m_Materiaux;
        Transform[] m_Nappes;
        Vector3[] m_Base;
        float[] m_Phase;
        float m_NuitAppliquee = -1f;
        static readonly int s_Couleur = Shader.PropertyToID("_BaseColor"), s_Emission = Shader.PropertyToID("_EmissionColor");

        void Start()
        {
            if (modele == null || positions == null || positions.Length == 0) return;
            int nb = alphaBandes.Length;
            m_Materiaux = new Material[nb];
            for (int b = 0; b < nb; b++)
            {
                var m = new Material(modele);
                m.SetFloat("_SpecularHighlights", 0f); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                m.SetFloat("_EnvironmentReflections", 0f); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.EnableKeyword("_EMISSION");
                m.enableInstancing = true;
                m_Materiaux[b] = m;
            }
            Mesh mesh = FireballVisual.BodyMesh;
            m_Nappes = new Transform[positions.Length]; m_Base = new Vector3[positions.Length]; m_Phase = new float[positions.Length];
            var hasard = new System.Random(4242);
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject go = new GameObject("Nappe");
                go.transform.SetParent(transform, false);
                go.transform.position = positions[i];
                go.transform.rotation = Quaternion.Euler(0f, (float)hasard.NextDouble() * 360f, 0f);
                go.transform.localScale = new Vector3(largeurs[i], hauteurs[i], largeurs[i] * (0.6f + 0.4f * (float)hasard.NextDouble()));
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = m_Materiaux[Mathf.Clamp(bandes[i], 0, nb - 1)];
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                m_Nappes[i] = go.transform; m_Base[i] = positions[i]; m_Phase[i] = (float)hasard.NextDouble() * 100f;
            }
            Appliquer(DayCycle.Night);
        }

        void OnDestroy()
        {
            if (m_Materiaux != null) foreach (var m in m_Materiaux) if (m != null) Destroy(m);
        }

        void Appliquer(float nuit)
        {
            m_NuitAppliquee = nuit;
            Color c = Color.Lerp(couleurJour, couleurNuit, nuit);
            for (int b = 0; b < m_Materiaux.Length; b++)
            {
                Color cb = c; cb.a = alphaBandes[b] * Mathf.Lerp(1f, nuitFacteur, nuit);
                m_Materiaux[b].SetColor(s_Couleur, cb);
                // moitié lumière, moitié émission : les facettes ne se détachent pas en plaques claires et sombres, la nappe reste douce
                m_Materiaux[b].SetColor(s_Emission, new Color(c.r, c.g, c.b) * 0.5f);
            }
        }

        void Update()
        {
            if (m_Nappes == null) return;
            float nuit = DayCycle.Night;
            if (Mathf.Abs(nuit - m_NuitAppliquee) > 0.004f) Appliquer(nuit);
            float t = Time.time;
            for (int i = 0; i < m_Nappes.Length; i++)
            {
                // dérive lente sur place (les nappes ne quittent pas leur coin de montagne) et respiration
                float ph = t * 0.11f + m_Phase[i];
                m_Nappes[i].position = m_Base[i] + new Vector3(Mathf.Sin(ph) * 2.2f * (vent.x * 4f), 0f, Mathf.Cos(ph * 0.8f) * 1.6f * (vent.z * 8f)) + Vector3.up * (Mathf.Sin(ph * 1.7f) * 0.25f);
            }
        }
    }
}
