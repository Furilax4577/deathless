using UnityEngine;

// Charge de Nyxessa (25/09/2026) : un flux de gemmes Nyxessa qui suit un arc (Bézier cubique qui monte de `hauteur` m
// au-dessus de la corde en son milieu) de `depart` à `arrivee` en `duree` s. Tête dense et claire (rôle cœur, éclat),
// queue qui s'éteint par la taille (vif, base) ; une VfxLumiere (Nyxessa, moyenne) voyage avec la tête et éclaire le
// sol le long du trajet. `aLArrivee` est appelé quand la tête arrive ; la queue finit de converger puis l'objet se
// détruit. Utilisé par Nyxessa.EnvoyerCharge (ouverture du portail) et Nyxessa.ReprendreCharge (fermeture).
// `echelle` (25/09/2026, agent gameplay) : ampleur du flux, 1 = charge du portail (480 gemmes, lumière grande). En dessous
// de 1, moins de gemmes (480 × échelle, 40 au moins), gemmes et écart à l'arc réduits (× √échelle), lumière moyenne sous
// 0,6 : MortAllie l'utilise à 0,35 pour l'énergie d'un allié mort, bien plus discrète que la charge du portail.
public class ChargeNyxessa : MonoBehaviour
{
    private const int GemmesMax = 480;
    private int Gemmes = GemmesMax;
    private float echelle = 1f;
    private const float Queue = 0.45f;       // longueur de la queue en fraction du trajet
    private const float Resorption = 0.35f;  // la queue finit d'arriver après la tête (s)

    private Vector3 p0, p1, p2, p3;
    private float duree;
    private float age;
    private bool arrive;
    private System.Action aLArrivee;
    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private float[] retard;
    private Vector3[] ecart;
    private Quaternion[] rotation;
    private Vector3[] axe;
    private float[] taille;
    private VfxLumiere lumiere;

    private static Color Teinte(VfxRole role, Color defaut) { return VfxPalette.Couleur(VfxTheme.Nyxessa, role, defaut); }

    public static ChargeNyxessa Lancer(Vector3 depart, Vector3 arrivee, float duree, Material materiau, System.Action aLArrivee, float hauteur = 2.5f, float echelle = 1f)
    {
        if (materiau == null)
        {
            if (aLArrivee != null) aLArrivee();
            return null;
        }
        GameObject go = new GameObject("ChargeNyxessa");
        ChargeNyxessa c = go.AddComponent<ChargeNyxessa>();
        c.echelle = Mathf.Clamp(echelle, 0.05f, 1f);
        c.Gemmes = Mathf.Max(40, Mathf.RoundToInt(GemmesMax * c.echelle));
        c.Construire(depart, arrivee, Mathf.Max(0.1f, duree), materiau, aLArrivee, hauteur);
        return c;
    }

    // Point de l'arc (u de 0 à 1).
    public Vector3 Arc(float u)
    {
        u = Mathf.Clamp01(u);
        float w = 1f - u;
        return w * w * w * p0 + 3f * w * w * u * p1 + 3f * w * u * u * p2 + u * u * u * p3;
    }

    private void Construire(Vector3 depart, Vector3 arrivee, float d, Material materiau, System.Action rappel, float hauteur)
    {
        p0 = depart;
        p3 = arrivee;
        // Milieu de la cubique = milieu de la corde + (3/4) × soulèvement des points de contrôle.
        Vector3 leve = Vector3.up * hauteur * 4f / 3f;
        p1 = Vector3.Lerp(depart, arrivee, 0.25f) + leve;
        p2 = Vector3.Lerp(depart, arrivee, 0.75f) + leve;
        duree = d;
        aLArrivee = rappel;
        retard = new float[Gemmes]; ecart = new Vector3[Gemmes]; rotation = new Quaternion[Gemmes];
        axe = new Vector3[Gemmes]; taille = new float[Gemmes];
        float reduc = Mathf.Sqrt(echelle);
        for (int i = 0; i < Gemmes; i++)
        {
            float r = Random.value;
            retard[i] = r * r * Queue;              // plus de gemmes près de la tête
            ecart[i] = Random.insideUnitSphere * Mathf.Lerp(0.14f, 0.7f, r) * reduc;
            rotation[i] = Random.rotation;
            axe[i] = Random.onUnitSphere;
            taille[i] = Random.Range(0.08f, 0.14f) * reduc;
        }
        vertices = new Vector3[Gemmes * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        mesh = new Mesh { name = "ChargeNyxessa" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = LowPolyGem.Triangles(Gemmes);
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materiau;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        lumiere = VfxLumiere.Creer(transform, Vector3.zero, VfxTheme.Nyxessa, echelle < 0.6f ? VfxTailleLumiere.Moyenne : VfxTailleLumiere.Grande);
        Dessiner();
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (!arrive && age >= duree)
        {
            arrive = true;
            if (lumiere != null) lumiere.Eteindre();
            if (aLArrivee != null) aLArrivee();
        }
        Dessiner();
        if (age >= duree + Resorption + VfxLumiere.Extinction)
            Destroy(gameObject);
    }

    private void Dessiner()
    {
        // Tête : départ franc, léger freinage à l'arrivée.
        float k = Mathf.Clamp01(age / duree);
        float tete = 1f - (1f - k) * (1f - k) * (1f - 0.35f * k);
        // Après l'arrivée, la queue continue d'avancer et se résorbe dans la cible.
        float surplus = Mathf.Max(0f, age - duree) / Resorption * Queue;
        Color coeur = Teinte(VfxRole.Coeur, new Color(0.62f, 0.91f, 0.44f));
        Color eclat = VfxPalette.Accent(VfxTheme.Nyxessa, "Éclat", new Color(0.91f, 1f, 0.78f));
        Color vif = Teinte(VfxRole.Vif, new Color(0.25f, 0.68f, 0.35f));
        Color basse = Teinte(VfxRole.Base, new Color(0.12f, 0.35f, 0.2f));
        float t = Time.time;
        for (int i = 0; i < Gemmes; i++)
        {
            float u = tete + surplus - retard[i];
            float q = retard[i] / Queue;                          // 0 tête, 1 bout de la queue
            float presence = u <= 0f ? 0f : Mathf.Clamp01(u / 0.05f);
            if (u >= 1f) presence *= Mathf.Clamp01(1f - (u - 1f) / 0.12f);
            float s = taille[i] * Mathf.Lerp(2.6f, 0.35f, q) * presence;
            if (s <= 0.002f)
            {
                LowPolyGem.Write(vertices, colors, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight);
                continue;
            }
            // La queue ondule un peu autour de l'arc ; la tête reste serrée.
            Vector3 p = Arc(u) + ecart[i] * (0.4f + q) + Vector3.up * Mathf.Sin(t * 9f + i) * 0.04f * q;
            Color c = q < 0.15f ? Color.Lerp(eclat * 1.35f, coeur, q / 0.15f) : q < 0.55f ? Color.Lerp(coeur, vif, (q - 0.15f) / 0.4f) : Color.Lerp(vif, basse, (q - 0.55f) / 0.45f);
            Quaternion r = Quaternion.AngleAxis(age * 420f + i * 17f, axe[i]) * rotation[i];
            LowPolyGem.Write(vertices, colors, i, p, s, new Vector3(0.7f, 1.3f, 0.7f), r, c, LowPolyGem.DefaultLight);
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.RecalculateBounds();
        if (lumiere != null) lumiere.transform.position = Arc(Mathf.Min(1f, tete));
    }
}
