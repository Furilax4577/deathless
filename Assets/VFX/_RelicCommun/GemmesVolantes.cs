using UnityEngine;

// Gemmes volantes (25/09/2026) : réserve de gemmes LowPolyGem en repère monde, un seul maillage, pour les effets qui
// sèment des gemmes (éclats, traînées, gerbes, fumée). Chaque gemme a sa position, sa vitesse, sa gravité, son
// freinage, sa couleur (couleurs par sommet, HDR permis) et une enveloppe de taille : éclosion (`eclosion` s), maintien
// jusqu'à `maintien` (fraction de la vie), puis extinction par la taille. Pas d'alpha. Shader Relic/VertexColorUnlit.
public class GemmesVolantes : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private Vector3[] pos, vit, etir, axe;
    private Quaternion[] rot;
    private float[] ne, vie, taille, gravite, frein, eclosion, maintien, spin;
    private Color[] teinte;
    private int prochain;
    private int capacite;
    private bool vivant;
    [Tooltip("Détruire l'objet quand plus aucune gemme n'est vivante (réserves ponctuelles).")]
    public bool detruireVide;

    public static GemmesVolantes Creer(string nom, Material materiau, int capacite, bool detruireVide = false)
    {
        GameObject go = new GameObject(nom);
        GemmesVolantes g = go.AddComponent<GemmesVolantes>();
        g.detruireVide = detruireVide;
        g.Construire(materiau, capacite);
        return g;
    }

    private void Construire(Material materiau, int n)
    {
        capacite = Mathf.Max(8, n);
        pos = new Vector3[capacite]; vit = new Vector3[capacite]; etir = new Vector3[capacite]; axe = new Vector3[capacite];
        rot = new Quaternion[capacite];
        ne = new float[capacite]; vie = new float[capacite]; taille = new float[capacite]; gravite = new float[capacite];
        frein = new float[capacite]; eclosion = new float[capacite]; maintien = new float[capacite]; spin = new float[capacite];
        teinte = new Color[capacite];
        for (int i = 0; i < capacite; i++) ne[i] = -1000f;
        vertices = new Vector3[capacite * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        mesh = new Mesh { name = name };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = LowPolyGem.Triangles(capacite);
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = materiau;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }

    // Émet une gemme ; `retard` (s) décale sa naissance.
    public void Emettre(Vector3 position, Vector3 vitesse, float t, float duree, Color couleur, float g = 0f, float freinage = 0f,
        float eclot = 0.05f, float tenue = 0.3f, Vector3 etirement = default(Vector3), float retard = 0f)
    {
        if (mesh == null) return;
        int i = prochain;
        prochain = (prochain + 1) % capacite;
        pos[i] = position; vit[i] = vitesse; taille[i] = t; vie[i] = Mathf.Max(0.02f, duree); teinte[i] = couleur;
        gravite[i] = g; frein[i] = freinage; eclosion[i] = Mathf.Max(0.001f, eclot); maintien[i] = Mathf.Clamp01(tenue);
        etir[i] = etirement == default(Vector3) ? new Vector3(Random.Range(0.7f, 1.1f), Random.Range(0.9f, 1.4f), Random.Range(0.7f, 1.1f)) : etirement;
        rot[i] = Random.rotation; axe[i] = Random.onUnitSphere; spin[i] = Random.Range(90f, 360f);
        ne[i] = Time.time + retard;
        vivant = true;
    }

    private void LateUpdate()
    {
        if (mesh == null || !vivant) return;
        // Sommets en repère monde : l'objet reste à l'origine même s'il est rangé sous un autre objet.
        if (transform.parent != null) transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        float t = Time.time, dt = Time.deltaTime;
        bool encore = false;
        for (int i = 0; i < capacite; i++)
        {
            float age = t - ne[i];
            if (age < 0f || age >= vie[i])
            {
                if (age < 0f && age > -10f) encore = true;
                LowPolyGem.Write(vertices, colors, i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, Color.black, LowPolyGem.DefaultLight);
                continue;
            }
            encore = true;
            vit[i] += Vector3.down * gravite[i] * dt;
            if (frein[i] > 0f) vit[i] *= Mathf.Exp(-frein[i] * dt);
            pos[i] += vit[i] * dt;
            float a = age / vie[i];
            float s = Mathf.Clamp01(age / eclosion[i]);
            if (a > maintien[i]) s *= 1f - (a - maintien[i]) / Mathf.Max(0.001f, 1f - maintien[i]);
            Quaternion r = Quaternion.AngleAxis(age * spin[i], axe[i]) * rot[i];
            LowPolyGem.Write(vertices, colors, i, pos[i], taille[i] * s, etir[i], r, teinte[i], LowPolyGem.DefaultLight);
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.RecalculateBounds();
        vivant = encore;
        if (!vivant && detruireVide) Destroy(gameObject);
    }
}
