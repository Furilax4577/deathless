// COPIE « Visual only » (bac à sable) de Relic/Assets/Scripts/GroundMist.cs : Relic.Instance est remplacé par le champ
// `centre` (à défaut, l'objet lui-même). Le reste est identique (DayCycle.Night vient de la copie locale de DayCycle).
using System.Collections.Generic;
using UnityEngine;

// Brume au sol la nuit (demande de Quentin) : des nappes basses faites de formes facettées aplaties (même famille que
// les nuages du ciel et la boule de feu), violet-gris translucides, qui dérivent lentement au ras du sol autour du
// village et « respirent » un peu. Elles apparaissent avec le crépuscule et se dissipent au jour, en suivant le fondu
// de DayCycle. Purement visuel et local, rien sur le réseau ; posé sur un objet de scène (GroundMist).
public class GroundMist : MonoBehaviour
{
    [Tooltip("Matériau URP Lit transparent (sans texture) : il est copié à l'exécution, l'asset n'est jamais modifié.")]
    [SerializeField] private Material template;
    [SerializeField] private Color color = new Color(0.45f, 0.38f, 0.62f, 1f);
    [Tooltip("Lueur propre de la brume (reflet de lune) : de nuit, la lumière seule ne suffit pas à la faire ressortir.")]
    [SerializeField] private Color glow = new Color(0.09f, 0.07f, 0.17f);
    [Tooltip("Opacité d'une nappe en pleine nuit.")]
    [SerializeField] private float nightAlpha = 0.3f;
    [Tooltip("Rayon (m) autour de la relique où la brume est posée.")]
    [SerializeField] private float radius = 62f;
    [Tooltip("Rayon (m) laissé libre autour de la relique (lisibilité de la relique et du terminal).")]
    [SerializeField] private float clearRadius = 6f;
    [Tooltip("Nombre de bancs de brume, et de nappes par banc (elles se chevauchent pour donner du volume).")]
    [SerializeField] private int banks = 90;
    [SerializeField] private int puffsPerBank = 5;
    [Tooltip("Largeur (m) d'une nappe, au plus bas et au plus haut.")]
    [SerializeField] private Vector2 width = new Vector2(2.5f, 6f);
    [Tooltip("Hauteur (m) d'une nappe (le dôme émerge de moitié) : la brume monte à peu près jusqu'aux genoux.")]
    [SerializeField] private Vector2 height = new Vector2(1f, 1.8f);
    [SerializeField] private Vector3 wind = new Vector3(0.35f, 0f, 0.15f);
    [Tooltip("Bac à sable : centre de la brume (Relic.Instance dans Relic).")]
    [SerializeField] private Transform centre;

    private readonly List<Transform> puffs = new List<Transform>();
    private readonly List<float> phases = new List<float>();
    private readonly List<Vector3> baseScales = new List<Vector3>();
    private readonly List<Renderer> renderers = new List<Renderer>();
    private Material material;
    private Vector3 center;
    private float shown = -1f;
    // Etat des rendus déjà appliqué : vrai au départ (un MeshRenderer neuf est actif), le premier Apply(0) les coupe.
    private bool shownVisible = true;
    private Vector3 bankCenter;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Start()
    {
        if (template == null)
            return;
        material = new Material(template);
        material.EnableKeyword("_EMISSION");
        // Brume visible en plein soleil : en alpha prémultiplié, reflets et spéculaire ne sont pas multipliés par
        // l'alpha, les dômes brillaient même presque transparents. On coupe les deux et on passe en mélange alpha
        // simple (sur la copie seulement).
        material.SetFloat("_SpecularHighlights", 0f);
        material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        material.SetFloat("_EnvironmentReflections", 0f);
        material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.SetFloat("_Blend", 0f);   // mode « Alpha » d'URP Lit (1 = prémultiplié)
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        // Des centaines de nappes, même maillage et même matériau : dessinées par instanciation GPU.
        material.enableInstancing = true;
        center = centre != null ? centre.position : transform.position;
        center.y = 0f;
        Mesh mesh = FireballVisual.BodyMesh;
        Random.State previous = Random.state;
        Random.InitState(1729);   // même disposition à chaque partie et chez chaque joueur
        for (int i = 0; i < banks * puffsPerBank; i++)
        {
            // Un banc = plusieurs nappes serrées autour d'un même point.
            if (i % puffsPerBank == 0)
                bankCenter = RandomPoint();
            Vector2 spread = Random.insideUnitCircle * 4f;
            Vector3 point = bankCenter + new Vector3(spread.x, 0f, spread.y);
            GameObject puff = new GameObject("MistPuff");
            puff.transform.SetParent(transform, false);
            float w = Random.Range(width.x, width.y);
            Vector3 scale = new Vector3(w, Random.Range(height.x, height.y), w * Random.Range(0.6f, 1f));
            puff.transform.localScale = scale;
            puff.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            puff.transform.position = OnGround(point);
            puff.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = puff.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            puffs.Add(puff.transform);
            phases.Add(Random.Range(0f, 100f));
            baseScales.Add(scale);
            renderers.Add(renderer);
        }
        Random.state = previous;
        Apply(0f);
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }

    // Point au hasard dans l'anneau entre clearRadius et radius.
    private Vector3 RandomPoint()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Mathf.Sqrt(Random.Range(clearRadius * clearRadius / (radius * radius), 1f)) * radius;
        return center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
    }

    // Posée sur le sol (la moitié de la nappe sous terre : on ne voit que le dôme bas). Le point le plus bas touché par
    // le rayon : le sol, pas un toit ni la cime d'un arbre.
    private static Vector3 OnGround(Vector3 point)
    {
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(point.x, 60f, point.z), Vector3.down, 120f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float lowest = float.PositiveInfinity;
        foreach (RaycastHit hit in hits)
            lowest = Mathf.Min(lowest, hit.point.y);
        if (!float.IsPositiveInfinity(lowest))
            point.y = lowest;
        return point;
    }

    private void Update()
    {
        if (material == null)
            return;
        float night = DayCycle.Night;
        Apply(night);
        if (night <= 0.001f)
            return;
        float time = Time.time;
        for (int i = 0; i < puffs.Count; i++)
        {
            Transform puff = puffs[i];
            // Dérive au vent ; sortie de l'anneau, la nappe revient de l'autre côté (et se recale sur le sol).
            Vector3 position = puff.position + wind * Time.deltaTime;
            Vector3 offset = position - center;
            offset.y = 0f;
            if (offset.magnitude > radius)
                position = OnGround(center - offset.normalized * (radius * 0.98f));
            else if (Mathf.Repeat(time + phases[i], 3f) < Time.deltaTime)
                position = OnGround(position);   // de temps en temps, suit le relief
            puff.position = position;
            // Respiration lente : la nappe gonfle et s'étale un peu, et tourne à peine.
            float breath = 1f + 0.12f * Mathf.Sin(time * 0.4f + phases[i]);
            Vector3 scale = baseScales[i];
            puff.localScale = new Vector3(scale.x * breath, scale.y * (2f - breath), scale.z * breath);
            puff.Rotate(0f, 2f * Time.deltaTime, 0f, Space.World);
        }
    }

    // Opacité selon la nuit ; de jour, les nappes sont coupées (aucun coût de rendu).
    private void Apply(float night)
    {
        bool visible = night > 0.001f;
        if (!visible)
            night = 0f;   // sous le seuil : coupure franche, pas d'opacité résiduelle
        // Le filtre n'ignore que les petits pas intermédiaires : un passage visible / invisible est toujours appliqué
        // (sinon les derniers pas du fondu, trop petits, laissaient la brume allumée toute la journée).
        if (visible == shownVisible && Mathf.Abs(night - shown) < 0.005f)
            return;
        shown = night;
        Color c = color;
        c.a = nightAlpha * night;
        material.SetColor(BaseColorId, c);
        material.SetColor(EmissionColorId, glow * night);
        if (visible != shownVisible)
        {
            shownVisible = visible;
            foreach (Renderer r in renderers)
                r.enabled = visible;
        }
    }
}
