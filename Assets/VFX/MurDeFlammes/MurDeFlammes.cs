using UnityEngine;
using UnityEngine.AI;

// Mur de flammes du mage (RB, créé dans Deathless le 01/10/2026). Ligne de feu posée au sol, perpendiculaire à la visée,
// dans le langage des gemmes : un seul maillage dynamique (GemmesVolantes, LowPolyGem, shader Relic/VertexColorUnlit via
// PortalVoxel.mat), pas d'alpha, tout apparaît et disparaît par la taille.
// - Embrasement (0,3 s) : le feu court du milieu vers les deux bouts.
// - Tenue : langues de flamme qui naissent au sol le long de la ligne et montent (les grosses et lentes rouge et orange,
//   les petites et vives jaunes), lit de braises au sol qui palpite, quelques étincelles blanc chaud ; une lumière Feu
//   moyenne au milieu.
// - Fin (0,5 s) : les flammes baissent et s'éteignent par la taille, des bouts vers le milieu.
// Couleurs : thème Feu (VfxPalette), jamais en dur ; le feu est couleur feu.
// API : MurDeFlammes.Jouer(centre, axe, longueur, duree, materiau). Purement visuel (dégâts et statuts : ClasseMage).
public class MurDeFlammes : MonoBehaviour
{
    private const float Pas = 0.35f;            // écart entre deux points d'émission le long de la ligne (m)
    private const float Embrasement = 0.3f;     // le feu court du milieu aux bouts (s)
    private const float Extinction = 0.5f;      // les flammes baissent avant la fin (s)
    private const float DebitParMetre = 48f;    // langues de flamme par seconde et par mètre

    private GemmesVolantes gemmes;
    private VfxLumiere lumiere;
    private Vector3[] points;
    private float[] distanceAuCentre;
    private Vector3 travers;
    private float debut, duree, demiLongueur;
    private float accFlammes, prochaineBraise;
    private bool fini;

    public static MurDeFlammes Jouer(Vector3 centre, Vector3 axe, float longueur, float duree, Material materiau)
    {
        if (materiau == null) return null;
        axe.y = 0f;
        if (axe.sqrMagnitude < 0.0001f) axe = Vector3.right;
        axe.Normalize();
        GameObject go = new GameObject("MurDeFlammes");
        go.transform.position = centre;
        MurDeFlammes m = go.AddComponent<MurDeFlammes>();
        m.duree = Mathf.Max(0.5f, duree);
        m.demiLongueur = Mathf.Max(0.5f, longueur * 0.5f);
        m.travers = Vector3.Cross(Vector3.up, axe);
        int n = Mathf.Max(3, Mathf.CeilToInt(longueur / Pas) + 1);
        m.points = new Vector3[n];
        m.distanceAuCentre = new float[n];
        for (int i = 0; i < n; i++)
        {
            float s = Mathf.Lerp(-m.demiLongueur, m.demiLongueur, i / (float)(n - 1));
            m.points[i] = Sol(centre + axe * s, centre.y);
            m.distanceAuCentre[i] = Mathf.Abs(s);
        }
        m.gemmes = GemmesVolantes.Creer("MurDeFlammes_Gemmes", materiau, Mathf.Clamp(Mathf.CeilToInt(longueur * DebitParMetre * 0.85f) + n * 3 + 60, 200, 1400));
        m.gemmes.transform.SetParent(go.transform, false);
        m.lumiere = VfxLumiere.Creer(go.transform, Vector3.up * 0.8f, VfxTheme.Feu, VfxTailleLumiere.Moyenne);
        m.debut = Time.time;
        m.prochaineBraise = m.debut;
        return m;
    }

    /// Point au sol : le NavMesh (là où marchent les squelettes : pont, gué), sinon le décor sous le point, sinon la
    /// hauteur donnée.
    public static Vector3 Sol(Vector3 p, float hauteurRepli)
    {
        NavMeshHit nh;
        if (NavMesh.SamplePosition(p, out nh, 2.5f, NavMesh.AllAreas)) return nh.position;
        RaycastHit h;
        if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out h, 8f, ~0, QueryTriggerInteraction.Ignore)) return h.point;
        p.y = hauteurRepli;
        return p;
    }

    private static Color F(VfxRole r, Color d) { return VfxPalette.Couleur(VfxTheme.Feu, r, d); }

    // Hauteur du feu (0 à 1) au point situé à `d` m du milieu, au temps `t` depuis la pose.
    private float Hauteur(float d, float t)
    {
        float front = demiLongueur * Mathf.Clamp01(t / Embrasement);
        float k = Mathf.Clamp01((front - d) / 0.6f + 0.5f);
        float reste = duree - t;
        if (reste < Extinction)
        {
            // Extinction des bouts vers le milieu : les points loin du centre s'éteignent un peu plus tôt.
            float x = reste / Extinction - 0.3f * (d / demiLongueur);
            k *= Mathf.Clamp01(x / 0.7f);
        }
        return k;
    }

    private void Update()
    {
        if (fini || gemmes == null) return;
        float t = Time.time - debut;
        if (t >= duree)
        {
            fini = true;
            if (lumiere != null) lumiere.Eteindre();
            Destroy(gameObject, 1.2f);   // les dernières gemmes finissent de rapetisser
            return;
        }
        Color coeur = F(VfxRole.Coeur, new Color(1f, 0.9f, 0.4f));
        Color vif = F(VfxRole.Vif, new Color(1f, 0.38f, 0.04f));
        Color baseC = F(VfxRole.Base, new Color(0.8f, 0.12f, 0.03f));
        Color ombre = F(VfxRole.Ombre, new Color(0.29f, 0.07f, 0.02f));
        Color blanc = VfxPalette.Accent(VfxTheme.Feu, "Blanc chaud", new Color(1f, 0.96f, 0.84f));

        // Langues de flamme.
        accFlammes += Time.deltaTime * DebitParMetre * demiLongueur * 2f;
        int nb = Mathf.FloorToInt(accFlammes);
        accFlammes -= nb;
        for (int i = 0; i < nb; i++)
        {
            int k = Random.Range(0, points.Length);
            float h = Hauteur(distanceAuCentre[k], t);
            if (h <= 0.02f) continue;
            Vector3 p = points[k] + travers * Random.Range(-0.28f, 0.28f) + Vector3.up * 0.05f;
            float v = Random.value;   // 0 : grosse et lente (extérieur, rouge) ; 1 : petite et vive (cœur, jaune)
            Color c = v < 0.35f ? Color.Lerp(baseC, vif, v / 0.35f)
                : v < 0.75f ? Color.Lerp(vif, coeur, (v - 0.35f) / 0.4f) * 1.25f
                : Color.Lerp(coeur, blanc, (v - 0.75f) / 0.25f) * 1.5f;
            float taille = Mathf.Lerp(0.42f, 0.2f, v) * Mathf.Lerp(0.55f, 1f, h);
            Vector3 vit = Vector3.up * Mathf.Lerp(1.8f, 3.4f, v) * Mathf.Lerp(0.6f, 1f, h) + travers * Random.Range(-0.15f, 0.15f);
            gemmes.Emettre(p, vit, taille, Random.Range(0.5f, 0.8f) * Mathf.Lerp(0.6f, 1f, h), c, -0.4f, 0.6f, 0.07f, 0.35f,
                new Vector3(Random.Range(0.6f, 0.85f), Random.Range(1.5f, 2.2f), Random.Range(0.6f, 0.85f)));
        }
        // Étincelles : rares, blanc chaud, vives.
        if (Random.value < Time.deltaTime * 12f)
        {
            int k = Random.Range(0, points.Length);
            if (Hauteur(distanceAuCentre[k], t) > 0.3f)
                gemmes.Emettre(points[k] + Vector3.up * 0.3f, new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(3f, 4.5f), Random.Range(-0.5f, 0.5f)),
                    Random.Range(0.03f, 0.05f), Random.Range(0.5f, 0.8f), blanc * 1.8f, 2.5f, 0.5f, 0.02f, 0.5f, new Vector3(0.6f, 0.6f, 1.4f));
        }
        // Braises au sol : un lit de gemmes sombres qui palpite, renouvelé par paquets.
        if (Time.time >= prochaineBraise)
        {
            prochaineBraise = Time.time + 0.16f;
            for (int k = 0; k < points.Length; k++)
            {
                float h = Hauteur(distanceAuCentre[k], t);
                if (h <= 0.05f) continue;
                Vector3 p = points[k] + travers * Random.Range(-0.35f, 0.35f) + Vector3.up * 0.03f;
                Color c = Random.value < 0.5f ? Color.Lerp(ombre, baseC, Random.value) : baseC * 1.3f;
                gemmes.Emettre(p, Vector3.zero, Random.Range(0.08f, 0.13f) * h, Random.Range(0.3f, 0.4f), c, 0f, 0f, 0.08f, 0.6f,
                    new Vector3(Random.Range(1f, 1.4f), Random.Range(0.4f, 0.6f), Random.Range(1f, 1.4f)));
            }
        }
    }
}
