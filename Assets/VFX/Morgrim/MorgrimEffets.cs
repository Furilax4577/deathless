using UnityEngine;

// Prototypes d'effets pour les deux versions de Morgrim (bac à sable, 26/09/2026), dans le langage gemmes du jeu
// (LowPolyGem, un seul maillage dynamique par effet, shader Relic/VertexColorUnlit, pas d'alpha : apparition et
// disparition par la taille). Inspiré de OndeGemmes et GemBurst (Assets/VFX/_RelicCommun et OndeDeChoc dans `main`),
// généralisé à n'importe quel thème de palette (VfxPalette, avec repli sur les couleurs par défaut si aucun registre
// n'est présent ici) au lieu d'être figé sur un seul thème. Pas de VfxLumiere ni de son : purement visuel, pour les
// captures. À reporter dans `main` sous une forme propre au thème si les compétences sont validées.
//
// - AnneauGemmes : anneau ou arc de gemmes au sol qui s'étend puis s'éteint (onde de choc, fissure). Reprend l'algorithme
//   d'OndeGemmes (suite dorée pour répartir les gemmes sur l'arc, éclats projetés en couronne).
// - EclatGemmes : gerbe de gemmes projetées depuis un point, en boule ou en cône dirigé (tourbillon, charge, coup
//   tranchant, coup de marteau). Reprend l'algorithme de GemBurst.Explode, généralisé à un thème et à une direction.
public static class MorgrimEffets
{
    public static Material MateriauGemmes;

    // ------------------------------------------------------------------- Anneau / arc (sol)

    public static GameObject AnneauGemmes(Vector3 position, VfxTheme theme, float rayonMax, float duree,
        float angleOuvertureDeg = 360f, Vector3? directionLocale = null, string nom = "AnneauGemmes")
    {
        var go = new GameObject(nom);
        go.transform.position = position;
        if (directionLocale.HasValue) go.transform.rotation = Quaternion.LookRotation(directionLocale.Value);
        var anneau = go.AddComponent<AnneauGemmesComp>();
        anneau.theme = theme;
        anneau.rayonMax = rayonMax;
        anneau.duree = duree;
        anneau.angleOuverture = Mathf.Clamp(angleOuvertureDeg, 10f, 360f);
        anneau.Construire();
        return go;
    }

    // ------------------------------------------------------------------- Gerbe (impact, tourbillon, tranche)

    public static GameObject EclatGemmes(Vector3 position, VfxTheme theme, float rayon, Vector3? directionMonde = null,
        float ouvertureConeDeg = 360f, string nom = "EclatGemmes")
    {
        var go = new GameObject(nom);
        go.transform.position = position;
        var eclat = go.AddComponent<EclatGemmesComp>();
        eclat.theme = theme;
        eclat.rayon = rayon;
        eclat.direction = directionMonde ?? Vector3.up;
        eclat.ouvertureCone = Mathf.Clamp(ouvertureConeDeg, 15f, 360f);
        eclat.Construire();
        return go;
    }

    internal static Color[] Teintes(VfxTheme theme)
    {
        return new[]
        {
            VfxPalette.Couleur(theme, VfxRole.Base, new Color(0.5f, 0.5f, 0.5f)),
            VfxPalette.Couleur(theme, VfxRole.Vif, new Color(0.65f, 0.65f, 0.65f)),
            VfxPalette.Couleur(theme, VfxRole.Coeur, new Color(0.8f, 0.8f, 0.8f)),
        };
    }
}

// Anneau ou arc de gemmes qui s'étend au sol puis s'éteint (adapté d'OndeGemmes, généralisé au thème).
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AnneauGemmesComp : MonoBehaviour
{
    public VfxTheme theme = VfxTheme.Terre;
    public float rayonDepart = 0.4f;
    public float rayonMax = 4f;
    public float largeurDepart = 0.6f;
    public float largeurFin = 0.15f;
    public float duree = 0.8f;
    public float angleOuverture = 360f;
    public int capaciteAnneau = 260;
    public int eclats = 26;

    Mesh mesh; MeshRenderer rendu; Vector3[] vertices; Color[] colors;
    float[] fraction, radial, hauteur, tailleFacteur, phase; Color[] teinte; Quaternion[] rot; Vector3[] axe;
    Vector3[] eDepart, eVitesse; float[] eVie, eTaille, eSpin; Color[] eTeinte; Quaternion[] eRot; Vector3[] eAxe;
    float debut = -100f; int total;

    public void Construire()
    {
        rendu = GetComponent<MeshRenderer>();
        rendu.sharedMaterial = MorgrimEffets.MateriauGemmes;
        rendu.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendu.receiveShadows = false;
        total = capaciteAnneau + eclats;
        vertices = new Vector3[total * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        mesh = new Mesh { name = "AnneauGemmes" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.MarkDynamic();
        mesh.vertices = vertices; mesh.colors = colors; mesh.triangles = LowPolyGem.Triangles(total);
        GetComponent<MeshFilter>().sharedMesh = mesh;

        fraction = new float[capaciteAnneau]; radial = new float[capaciteAnneau]; hauteur = new float[capaciteAnneau];
        tailleFacteur = new float[capaciteAnneau]; phase = new float[capaciteAnneau]; teinte = new Color[capaciteAnneau];
        rot = new Quaternion[capaciteAnneau]; axe = new Vector3[capaciteAnneau];
        var couleurs = MorgrimEffets.Teintes(theme);
        var rnd = new System.Random(2626);
        for (int i = 0; i < capaciteAnneau; i++)
        {
            fraction[i] = (float)((i * 0.6180339887) % 1.0);
            radial[i] = (float)rnd.NextDouble() - 0.5f;
            hauteur[i] = (float)rnd.NextDouble() * 0.06f;
            tailleFacteur[i] = 0.75f + 0.5f * (float)rnd.NextDouble();
            phase[i] = (float)rnd.NextDouble() * 10f;
            teinte[i] = rnd.NextDouble() < 0.35 ? couleurs[1] : couleurs[0];
            rot[i] = Quaternion.Euler((float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 360f);
            axe[i] = new Vector3((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f).normalized;
        }
        eDepart = new Vector3[eclats]; eVitesse = new Vector3[eclats]; eVie = new float[eclats]; eTaille = new float[eclats];
        eTeinte = new Color[eclats]; eRot = new Quaternion[eclats]; eAxe = new Vector3[eclats]; eSpin = new float[eclats];
        float ouverture = angleOuverture * Mathf.Deg2Rad;
        float debutArc = Mathf.PI / 2f - ouverture / 2f;
        float vRadiale = rayonMax / Mathf.Max(0.1f, duree);
        for (int i = 0; i < eclats; i++)
        {
            float a = debutArc + (float)rnd.NextDouble() * ouverture;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            eDepart[i] = dir * 0.5f + Vector3.up * 0.05f;
            eVitesse[i] = dir * vRadiale * (0.55f + (float)rnd.NextDouble() * 0.4f) + Vector3.up * 3.5f;
            eVie[i] = 0.7f + (float)rnd.NextDouble() * 0.3f;
            eTaille[i] = 0.06f + (float)rnd.NextDouble() * 0.07f;
            eTeinte[i] = couleurs[i % 2 == 0 ? 0 : 1];
            eRot[i] = Random.rotation;
            eAxe[i] = Random.onUnitSphere;
            eSpin[i] = Random.Range(-230f, 230f);
        }
        Jouer();
    }

    public void Jouer() { debut = Time.time; if (rendu != null) rendu.enabled = true; Appliquer(0f); }

    void Update() { if (debut < 0f) return; float t = Time.time - debut; if (t >= Mathf.Max(duree, 1f)) { if (rendu != null) rendu.enabled = false; return; } Appliquer(t); }

    // État à t secondes après le départ (Update et captures).
    public void Appliquer(float t)
    {
        if (mesh == null) return;
        float ouverture = angleOuverture * Mathf.Deg2Rad;
        float debutArc = Mathf.PI / 2f - ouverture / 2f;
        float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duree));
        float rExt = Mathf.Lerp(rayonDepart, rayonMax, p);
        float largeur = Mathf.Lerp(largeurDepart, largeurFin, p);
        float extinction = t < duree ? 1f : 0f;
        float perimetre = rExt * ouverture;
        int visibles = t < duree ? Mathf.Min(capaciteAnneau, Mathf.CeilToInt(perimetre * 14f)) : 0;
        for (int i = 0; i < capaciteAnneau; i++)
        {
            if (i >= visibles) { LowPolyGem.Write(vertices, colors, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight); continue; }
            float a = debutArc + fraction[i] * ouverture;
            float r = rExt - largeur * 0.5f + radial[i] * largeur;
            Vector3 pos = new Vector3(Mathf.Cos(a) * r, hauteur[i] + 0.05f, Mathf.Sin(a) * r);
            Quaternion spin = Quaternion.AngleAxis(t * 90f + phase[i] * 36f, axe[i]) * rot[i];
            LowPolyGem.Write(vertices, colors, i, pos, 0.11f * tailleFacteur[i] * extinction, new Vector3(0.8f, 1.2f, 0.8f), spin, teinte[i], LowPolyGem.DefaultLight);
        }
        for (int j = 0; j < eclats; j++)
        {
            int i = capaciteAnneau + j;
            float k = t / eVie[j];
            if (t < 0f || k >= 1f) { LowPolyGem.Write(vertices, colors, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight); continue; }
            Vector3 v = eVitesse[j];
            float y = eDepart[j].y + v.y * t * Mathf.Exp(-1.2f * t) - 0.5f * 6.87f * t * t;
            Vector3 pos = new Vector3(eDepart[j].x + v.x * t, Mathf.Max(0.04f, y), eDepart[j].z + v.z * t);
            float size = eTaille[j] * (k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f);
            Quaternion spin = Quaternion.AngleAxis(t * eSpin[j], eAxe[j]) * eRot[j];
            LowPolyGem.Write(vertices, colors, i, pos, size, new Vector3(1f, 0.8f, 1.15f), spin, eTeinte[j], LowPolyGem.DefaultLight);
        }
        mesh.vertices = vertices; mesh.colors = colors;
        mesh.bounds = new Bounds(Vector3.zero, new Vector3((rayonMax + 1f) * 2f, 3f, (rayonMax + 1f) * 2f));
    }

    void OnDestroy() { if (mesh != null) Object.DestroyImmediate(mesh); }
}

// Gerbe de gemmes projetée depuis un point, en boule (ouvertureCone = 360) ou en cône dirigé (adapté de GemBurst.Explode,
// généralisé au thème et à une direction).
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EclatGemmesComp : MonoBehaviour
{
    public VfxTheme theme = VfxTheme.Rage;
    public float rayon = 2f;
    public Vector3 direction = Vector3.up;
    public float ouvertureCone = 360f;
    public float duree = 0.6f;
    public int nombre = 48;

    Mesh mesh; Vector3[] vertices; Color[] colors;
    Vector3[] positions, velocities, stretch; Quaternion[] rotation; Vector3[] spinAxis; float[] spinSpeed, size; Color[] color;
    float debut = -100f;

    public void Construire()
    {
        var rendu = GetComponent<MeshRenderer>();
        rendu.sharedMaterial = MorgrimEffets.MateriauGemmes;
        rendu.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendu.receiveShadows = false;

        positions = new Vector3[nombre]; velocities = new Vector3[nombre]; stretch = new Vector3[nombre];
        rotation = new Quaternion[nombre]; spinAxis = new Vector3[nombre]; spinSpeed = new float[nombre];
        size = new float[nombre]; color = new Color[nombre];
        var couleurs = MorgrimEffets.Teintes(theme);
        Vector3 axeCone = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.up;
        float demiAngle = ouvertureCone * 0.5f * Mathf.Deg2Rad;
        for (int i = 0; i < nombre; i++)
        {
            Vector3 dir;
            if (ouvertureCone >= 359f) dir = Random.onUnitSphere;
            else
            {
                // Direction aléatoire dans un cône autour de axeCone.
                float t = Random.value * demiAngle;
                float phi = Random.value * Mathf.PI * 2f;
                Vector3 perp = Vector3.Cross(axeCone, Mathf.Abs(Vector3.Dot(axeCone, Vector3.up)) < 0.9f ? Vector3.up : Vector3.right).normalized;
                Vector3 perp2 = Vector3.Cross(axeCone, perp);
                dir = (Mathf.Cos(t) * axeCone + Mathf.Sin(t) * (Mathf.Cos(phi) * perp + Mathf.Sin(phi) * perp2)).normalized;
            }
            float speed = Random.Range(0.7f, 1.3f) * rayon / duree * 1.4f;
            positions[i] = dir * rayon * 0.06f;
            velocities[i] = dir * speed;
            stretch[i] = new Vector3(Random.Range(0.6f, 1.2f), Random.Range(0.8f, 1.5f), Random.Range(0.6f, 1.2f));
            rotation[i] = Random.rotation;
            spinAxis[i] = Random.onUnitSphere;
            spinSpeed[i] = Random.Range(200f, 600f);
            size[i] = Random.Range(0.05f, 0.11f) * Mathf.Clamp(rayon, 0.5f, 2f);
            color[i] = couleurs[Random.Range(0, couleurs.Length)];
        }
        vertices = new Vector3[nombre * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        mesh = new Mesh { name = "EclatGemmes" };
        mesh.MarkDynamic();
        mesh.vertices = vertices; mesh.colors = colors; mesh.triangles = LowPolyGem.Triangles(nombre);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * rayon * 3f);
        GetComponent<MeshFilter>().sharedMesh = mesh;
        Jouer();
    }

    public void Jouer() { debut = Time.time; Appliquer(0f); }
    void Update() { if (debut < 0f) return; float t = Time.time - debut; if (t >= duree) return; Appliquer(t); }

    public void Appliquer(float t)
    {
        if (mesh == null) return;
        float k = Mathf.Clamp01(t / duree);
        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 v = velocities[i] * (1f - Mathf.Min(1f, 3.2f * t)) ;
            Vector3 pos = positions[i] + velocities[i] * t * (1f - 0.5f * Mathf.Min(1f, t)) + Vector3.down * (2.5f * 0.5f * t * t);
            float grow = Mathf.Clamp01(t / 0.06f) * (1f - k * k);
            Quaternion spin = Quaternion.AngleAxis(t * spinSpeed[i], spinAxis[i]) * rotation[i];
            LowPolyGem.Write(vertices, colors, i, pos, size[i] * grow, stretch[i], spin, color[i], LowPolyGem.DefaultLight);
        }
        mesh.vertices = vertices; mesh.colors = colors;
    }

    void OnDestroy() { if (mesh != null) Object.DestroyImmediate(mesh); }
}
