using System.Collections.Generic;
using UnityEngine;

// Traînée d'air des projectiles non magiques (flèches, carreaux ; 25/09/2026) : un filet fin et clair derrière le
// projectile, sans lueur, sans gemme, sans lumière. Tube à trois pans (lisible sous tous les angles) en couleurs par
// sommet blanc cassé, pans ombrés différemment (volume), rendu opaque par le shader Relic/VertexColorUnlit (couleurs non
// HDR : aucune émission). Il s'amincit vers l'arrière et avec l'âge et disparaît par la taille, jamais par transparence.
// Quand le projectile s'arrête (planté) ou disparaît, la traînée se résorbe en `vie` s puis l'objet se détruit.
// API : TraineeAir.Attacher(projectile, décalageLocal, matériau, longueur, épaisseur, vie) ; Detacher().
public class TraineeAir : MonoBehaviour
{
    private const int Pans = 3;
    private const int MaxPoints = 40;

    private Transform cible;
    private Vector3 decalage;
    private float longueur, epaisseur, vie;
    private Color couleur;
    private bool attache;

    private readonly List<Vector3> points = new List<Vector3>();
    private readonly List<float> ages = new List<float>();
    private Mesh mesh;
    private Vector3[] v;
    private Color[] c;

    // `decalageLocal` : point d'attache dans le repère du projectile (l'arrière de la flèche) ; `longueur` (m) : longueur
    // maximale ; `epaisseur` (m) : rayon à l'avant ; `vie` (s) : temps de résorption d'un point.
    public static TraineeAir Attacher(Transform projectile, Vector3 decalageLocal, Material materiau, float longueur = 2f,
        float epaisseur = 0.012f, float vie = 0.22f)
    {
        if (projectile == null || materiau == null) return null;
        GameObject go = new GameObject("TraineeAir");
        TraineeAir t = go.AddComponent<TraineeAir>();
        t.cible = projectile;
        t.decalage = decalageLocal;
        t.longueur = longueur;
        t.epaisseur = epaisseur;
        t.vie = Mathf.Max(0.05f, vie);
        t.couleur = new Color(0.9f, 0.89f, 0.85f);   // blanc cassé, non HDR
        t.attache = true;
        t.Construire(materiau);
        return t;
    }

    public void Detacher() { attache = false; }

    private void Construire(Material materiau)
    {
        mesh = new Mesh { name = "TraineeAir" };
        mesh.MarkDynamic();
        v = new Vector3[MaxPoints * Pans];
        c = new Color[v.Length];
        int[] tri = new int[(MaxPoints - 1) * Pans * 6];
        int k = 0;
        for (int i = 0; i < MaxPoints - 1; i++)
            for (int p = 0; p < Pans; p++)
            {
                int a = i * Pans + p, b = i * Pans + (p + 1) % Pans, a2 = a + Pans, b2 = b + Pans;
                tri[k++] = a; tri[k++] = a2; tri[k++] = b;
                tri[k++] = b; tri[k++] = a2; tri[k++] = b2;
            }
        mesh.vertices = v;
        mesh.colors = c;
        mesh.triangles = tri;
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

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < ages.Count; i++) ages[i] += dt;
        if (attache && cible != null)
        {
            Vector3 tete = cible.TransformPoint(decalage);
            // Un nouveau point quand le projectile a avancé ; arrêté (planté), plus de point : la traînée se résorbe.
            if (points.Count == 0 || (tete - points[0]).sqrMagnitude > 0.0225f)
            {
                points.Insert(0, tete);
                ages.Insert(0, 0f);
            }
            else points[0] = tete;
        }
        else attache = false;
        // Retire les points trop vieux ou au-delà de la longueur.
        float cumul = 0f;
        int garder = 0;
        for (int i = 0; i < points.Count; i++)
        {
            if (i > 0) cumul += Vector3.Distance(points[i - 1], points[i]);
            if (ages[i] >= vie || cumul > longueur || i >= MaxPoints) break;
            garder = i + 1;
        }
        if (garder < points.Count)
        {
            points.RemoveRange(garder, points.Count - garder);
            ages.RemoveRange(garder, ages.Count - garder);
        }
        if (!attache && points.Count <= 1)
        {
            Destroy(gameObject);
            return;
        }
        Ecrire();
    }

    private void Ecrire()
    {
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        int n = points.Count;
        float cumul = 0f;
        Vector3 haut = Vector3.up;
        for (int i = 0; i < MaxPoints; i++)
        {
            if (i >= n)
            {
                Vector3 fin = n > 0 ? points[n - 1] : Vector3.zero;
                for (int p = 0; p < Pans; p++) { v[i * Pans + p] = fin; c[i * Pans + p] = couleur; }
                continue;
            }
            if (i > 0) cumul += Vector3.Distance(points[i - 1], points[i]);
            Vector3 dir = i < n - 1 ? points[i] - points[i + 1] : (i > 0 ? points[i - 1] - points[i] : Vector3.forward);
            if (dir.sqrMagnitude < 1e-8f) dir = Vector3.forward;
            dir.Normalize();
            Vector3 x = Vector3.Cross(dir, Mathf.Abs(Vector3.Dot(dir, haut)) > 0.95f ? Vector3.right : haut).normalized;
            Vector3 y = Vector3.Cross(dir, x);
            // Mince à l'avant (sort de l'empennage), plus large au premier tiers, puis s'amincit vers l'arrière et avec l'âge.
            float k = Mathf.Clamp01(cumul / Mathf.Max(0.01f, longueur));
            float profil = Mathf.Sin(Mathf.PI * Mathf.Pow(Mathf.Clamp01(k * 0.85f + 0.08f), 0.7f));
            float r = epaisseur * profil * (1f - ages[i] / vie);
            for (int p = 0; p < Pans; p++)
            {
                float a = p * Mathf.PI * 2f / Pans;
                v[i * Pans + p] = points[i] + (x * Mathf.Cos(a) + y * Mathf.Sin(a)) * r;
                c[i * Pans + p] = couleur * (p == 0 ? 1f : p == 1 ? 0.88f : 0.76f);
            }
        }
        mesh.vertices = v;
        mesh.colors = c;
        mesh.RecalculateBounds();
    }
}
