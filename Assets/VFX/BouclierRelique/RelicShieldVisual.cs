// COPIE « Visual only » (bac à sable) de Relic/Assets/Scripts/RelicShieldVisual.cs : RelicShield (NetworkBehaviour) est
// remplacé par RelicShieldEtat (même API). Le reste est identique.
using UnityEngine;

// Visuel du bouclier de la relique (étape 73, cylindre depuis l'étape 75-B) : une paroi de gemmes low poly, dans
// l'esprit de la soupe du portail, qui tourne lentement autour de la relique : un cylindre ouvert (sans toit) de rayon
// `ShieldRadius` et de hauteur `ShieldHeight`, gemmes réparties uniformément sur la paroi, quelques-unes qui débordent
// du bord du haut pour un rebord irrégulier. Pendant l'incantation du mage, les gemmes montent du sol et se mettent
// en place ; levé, le cylindre scintille ; frappé, une onde claire part du point d'impact ; brisé, les gemmes tombent
// en pluie (GemBurst) ; à l'aube, il redescend dans le sol. La hauteur de la paroi suit ses points de vie et sa
// couleur aussi : bleu, puis orange, puis rouge (RelicShield.LifeTint, seuils dans GameBalance), la lumière suit.
// Purement visuel et local : lit les SyncVar de RelicShield (même objet). Matériau PortalVoxel (couleurs par sommet).
[RequireComponent(typeof(RelicShieldEtat))]
public class RelicShieldVisual : MonoBehaviour
{
    [SerializeField] private Material gemMaterial;
    [SerializeField] private int gems = 900;

    // Trois teintes par palier, du plus sombre au plus clair, plus un reflet (index 3).
    private static readonly Color[] BluePalette =
    {
        new Color(0.05f, 0.18f, 0.45f), new Color(0.1f, 0.4f, 0.85f), new Color(0.3f, 0.65f, 1f), new Color(0.7f, 0.9f, 1.2f),
    };
    private static readonly Color[] OrangePalette =
    {
        new Color(0.45f, 0.18f, 0.03f), new Color(0.9f, 0.45f, 0.08f), new Color(1f, 0.68f, 0.25f), new Color(1.2f, 0.95f, 0.65f),
    };
    private static readonly Color[] RedPalette =
    {
        new Color(0.4f, 0.04f, 0.04f), new Color(0.85f, 0.12f, 0.1f), new Color(1f, 0.35f, 0.3f), new Color(1.2f, 0.75f, 0.7f),
    };
    private static readonly Color BlueGlow = new Color(0.35f, 0.65f, 1f);
    private static readonly Color OrangeGlow = new Color(1f, 0.6f, 0.2f);
    private static readonly Color RedGlow = new Color(1f, 0.25f, 0.2f);

    private RelicShieldEtat shield;
    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private Vector3[] dir;          // direction horizontale de chaque gemme sur la paroi
    private float[] height;         // hauteur au-dessus du pied du cylindre (un peu au-delà de la hauteur : le rebord)
    private float[] radial;         // écart au rayon (épaisseur de la paroi)
    private float[] size;
    private float[] phase;
    private float[] order;          // ordre d'apparition pendant l'incantation (0 = première, en bas)
    private Quaternion[] rotation;
    private Vector3[] spinAxis;
    private GameObject holder;
    private Light glow;
    private float presence;         // 0 absent, 1 levé (lissé)
    private bool wasUp;
    private readonly Color[] palette = new Color[4];   // palette du moment, fondue selon la vie

    private void Start()
    {
        shield = GetComponent<RelicShieldEtat>();
        if (gemMaterial == null)
        {
            enabled = false;
            return;
        }
        float wallHeight = shield.Height;
        System.Random random = new System.Random(3131);
        float R() => (float)random.NextDouble();
        dir = new Vector3[gems];
        height = new float[gems];
        radial = new float[gems];
        size = new float[gems];
        phase = new float[gems];
        order = new float[gems];
        rotation = new Quaternion[gems];
        spinAxis = new Vector3[gems];
        for (int i = 0; i < gems; i++)
        {
            // Paroi du cylindre : azimut et hauteur uniformes, donc densité homogène sur la surface.
            float azimuth = R() * Mathf.PI * 2f;
            dir[i] = new Vector3(Mathf.Cos(azimuth), 0f, Mathf.Sin(azimuth));
            bool rim = R() < 0.07f;
            // Rebord : quelques gemmes débordent du haut, de plus en plus rares en montant.
            height[i] = rim ? wallHeight + R() * R() * 0.8f : R() * wallHeight;
            radial[i] = (R() - 0.5f) * (rim ? 0.6f : 0.35f);
            size[i] = 0.05f + 0.06f * R();
            phase[i] = R() * 10f;
            order[i] = Mathf.Clamp01(height[i] / wallHeight);   // du bas vers le haut
            rotation[i] = Quaternion.Euler(R() * 360f, R() * 360f, R() * 360f);
            spinAxis[i] = new Vector3(R() - 0.5f, R() - 0.5f, R() - 0.5f).normalized;
        }
        vertices = new Vector3[gems * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        mesh = new Mesh { name = "RelicShield" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = LowPolyGem.Triangles(gems);
        // Les sommets sont écrits en coordonnées du monde (Draw) : le porteur reste à l'origine, sinon le cylindre serait
        // décalé de la position de la relique une deuxième fois.
        holder = new GameObject("RelicShieldGems");
        holder.transform.position = Vector3.zero;
        holder.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer meshRenderer = holder.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = gemMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        glow = new GameObject("Glow").AddComponent<Light>();
        glow.transform.SetParent(holder.transform, false);
        glow.transform.localPosition = shield.BasePosition + Vector3.up * (wallHeight * 0.5f);
        glow.type = LightType.Point;
        glow.color = BlueGlow;
        glow.range = 12f;
        glow.intensity = 0f;
        glow.shadows = LightShadows.None;
        holder.SetActive(false);
    }

    private void OnDestroy()
    {
        if (holder != null)
            Destroy(holder);
        if (mesh != null)
            Destroy(mesh);
    }

    // Palette du moment : chaque teinte fondue entre bleu, orange et rouge selon la vie du bouclier.
    private void UpdatePalette()
    {
        for (int k = 0; k < 4; k++)
            palette[k] = shield.LifeTint(BluePalette[k], OrangePalette[k], RedPalette[k]);
    }

    // Brisé : les gemmes présentes tombent en pluie (gerbe partant de leur place, poussée vers l'extérieur), de la
    // couleur du bouclier au moment de la rupture (rouge, en principe).
    public void Shatter()
    {
        if (mesh == null || presence <= 0.05f)
            return;
        UpdatePalette();
        Vector3[] world = new Vector3[gems];
        Color[] tints = new Color[gems];
        Vector3 basePosition = shield.BasePosition;
        float radius = shield.Radius;
        for (int i = 0; i < gems; i++)
        {
            world[i] = basePosition + dir[i] * (radius + radial[i]) + Vector3.up * height[i];
            tints[i] = palette[1 + i % 3];
        }
        Vector3 center = basePosition + Vector3.up * (shield.Height * 0.5f);
        GemBurst.Shatter(center, world, tints, 0.06f, 2.5f, Vector3.down * 2f, gemMaterial);
        presence = 0f;
        holder.SetActive(false);
    }

    private void LateUpdate()
    {
        if (mesh == null || shield == null)
            return;
        // Présence : monte d'une traite pendant l'incantation, de zéro à la hauteur finale en exactement la durée de
        // l'incantation (demande de Quentin : « fais-le se lever d'une traite »), puis reste ; retombe à l'aube.
        float wanted = shield.IsUp || shield.IsCasting ? 1f : 0f;
        float speed = wanted > 0f ? 1f / shield.CastSeconds : 1.4f;
        presence = Mathf.MoveTowards(presence, wanted, speed * Time.deltaTime);
        wasUp = shield.IsUp;
        if (presence <= 0.01f)
        {
            if (holder.activeSelf)
                holder.SetActive(false);
            return;
        }
        if (!holder.activeSelf)
            holder.SetActive(true);
        Draw(Time.time);
    }

    private void Draw(float t)
    {
        Vector3 basePosition = shield.BasePosition;
        float radius = shield.Radius;
        float wallHeight = shield.Height;
        float life = shield.LifeRatio;
        UpdatePalette();
        float hitAge = t - shield.LastHitTime;
        // Point d'impact ramené sur la paroi : direction horizontale et hauteur.
        Vector3 hitDir = Vector3.zero;
        float hitHeight = 0f;
        if (hitAge >= 0f && hitAge < 1f)
        {
            Vector3 toHit = shield.LastHitPoint - basePosition;
            hitHeight = Mathf.Clamp(toHit.y, 0f, wallHeight);
            toHit.y = 0f;
            hitDir = toHit.sqrMagnitude > 1e-4f ? toHit.normalized : Vector3.zero;
        }
        // Rotation lente du cylindre entier, dans un sens, et une deuxième couche (une gemme sur trois) dans l'autre.
        float turnA = t * 0.18f, turnB = -t * 0.26f;
        for (int i = 0; i < gems; i++)
        {
            // Pendant l'incantation, les gemmes montent du sol, du bas vers le haut, en une seule montée continue (et y
            // redescendent à l'aube) ; blessé, les plus hautes disparaissent : la paroi baisse avec la vie.
            float rise = Mathf.Clamp01((presence - order[i] * 0.6f) * 2.5f);
            float shown = rise * Mathf.Clamp01((life - order[i] + 0.3f) * 4f);
            if (shown <= 0.01f)
            {
                LowPolyGem.Write(vertices, colors, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight);
                continue;
            }
            Vector3 d = Quaternion.Euler(0f, (i % 3 == 0 ? turnB : turnA) * Mathf.Rad2Deg, 0f) * dir[i];
            float breathe = Mathf.Sin(t * 1.5f + phase[i]) * 0.06f;
            float r = radius + radial[i] + breathe;
            float y = height[i] * Mathf.SmoothStep(0f, 1f, rise);
            // Onde depuis le point d'impact : un anneau qui s'élargit le long de la paroi (distance sur le cylindre :
            // arc horizontal et écart de hauteur) ; les gemmes proches du front sont poussées et éclaircies.
            float ripple = 0f;
            if (hitDir != Vector3.zero)
            {
                float arc = Vector3.Angle(d, hitDir) * Mathf.Deg2Rad * radius;
                float dy = y - hitHeight;
                float distance = Mathf.Sqrt(arc * arc + dy * dy);
                ripple = Mathf.Clamp01(1f - Mathf.Abs(distance - hitAge * 8f) / 0.7f) * (1f - hitAge);
                r += ripple * 0.35f;
            }
            Vector3 position = basePosition + d * r + Vector3.up * y;
            float glint = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(t * 2f + phase[i] * 3f), 8f);
            int k = i % 3;
            Color color = Color.Lerp(palette[k], palette[3], Mathf.Max(glint * 0.6f, ripple));
            Quaternion spin = Quaternion.AngleAxis(t * (40f + 30f * (i % 5)), spinAxis[i]) * rotation[i];
            LowPolyGem.Write(vertices, colors, i, position, size[i] * shown, new Vector3(0.7f, 1.3f, 0.7f), spin, color, LowPolyGem.DefaultLight);
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.bounds = new Bounds(basePosition + Vector3.up * (wallHeight * 0.5f), new Vector3((radius + 1f) * 2f, wallHeight + 3f, (radius + 1f) * 2f));
        if (glow != null)
        {
            glow.color = shield.LifeTint(BlueGlow, OrangeGlow, RedGlow);
            glow.intensity = presence * (1.2f + 0.3f * Mathf.Sin(t * 3f)) + (hitAge < 0.3f ? 2f : 0f);
        }
    }
}
