using UnityEngine;
using UnityEngine.Rendering;

// Lanternes du village (KayKit Halloween Bits : lantern_standing au sol, post_lantern suspendue à un poteau), bac à
// sable sandbox-vfx, 25/09/2026, agent « lanternes ».
//
// Demande de Quentin : la lumière vient du centre de la cage ; le modèle (cadre, vitres) reste affiché avec sa texture
// mais ne bloque pas la lumière. Les deux modèles partagent l'atlas halloweenbits_texture (8 × 4 cases) ; les vitres
// sont des faces pleines et opaques tournées vers l'extérieur, habillées par la case jaune-orangé (3e colonne, rangée
// du bas). Une lumière posée dans la cage les éclaire par l'arrière : elles restent sombres, d'où l'émission. Avant ce
// correctif, la lumière était posée à la main trop haut, dans le toit (0,62 × 1,3 m + 0,15 m devant pour
// lantern_standing, au lieu de 0,437 m ; 2,4 m pour post_lantern dans Relic, au lieu de 2,027 m).
//
// Configurer (appelé à la construction, par VillageBuilder) :
//  - calcule le centre de la cage à partir des bornes des triangles des vitres (UV dans UvVitres), sur tous les
//    maillages du modèle, et y place la flamme puis la lumière ;
//  - ombre portée des parties du modèle coupée (lanterne au sol) ou gardée pour le soleil (poteau), réception gardée ;
//  - la lumière reste sans ombre (LightShadows.None) : rien ne peut boucher la lumière, pas de carte d'ombre cubique ;
//  - pose le matériau des lanternes (Lanterne_HalloweenBits : carte d'émission limitée aux vitres, en niveaux de gris).
//
// À chaque image, le composant pilote la lumière et les vitres :
//  - allumage (0 éteinte, 1 allumée) : donné par CycleJourNuit (éteinte le jour, fondu au crépuscule et à l'aube) ;
//  - couleur : celle des réglages communs (LanternesReglages), ou la sienne si couleurPropre ; appliquée à la lumière
//    et à l'émission des vitres (même teinte pour la cage et la tache au sol) ;
//  - scintillement doux : ±amplitude (10 % par défaut) par deux ondulations lentes décalées (0,45 et 1,1 Hz), phase et
//    fréquences propres à chaque lanterne (tirées de sa position) ; les vitres suivent. Aucune allocation par image.
[ExecuteAlways]
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]   // après CycleJourNuit (allumage) et FireEffect (qui écrit aussi l'intensité)
public class LanterneLumiere : MonoBehaviour
{
    // Case de l'atlas halloweenbits_texture qui habille les vitres (u 0,25 à 0,375 ; v 0 à 0,25).
    public static readonly Rect UvVitres = new Rect(0.25f, 0f, 0.125f, 0.25f);
    public static readonly Color CouleurDefaut = new Color(1f, 0.64f, 0.34f);   // lumière des lanternes de VillageBuilder

    [Tooltip("Réglages communs (couleur par défaut, scintillement, éclat des vitres).")]
    public LanternesReglages reglages;
    [Tooltip("Garder la couleur de cette lanterne au lieu de la couleur commune.")]
    public bool couleurPropre;
    public Color couleur = CouleurDefaut;
    [Tooltip("Lumière de la lanterne, au centre de la cage.")]
    public Light lumiere;
    [Tooltip("Intensité de la lumière allumée, hors scintillement (CycleJourNuit.intensiteLanterne).")]
    public float intensite = 2.2f;
    [Tooltip("0 éteinte, 1 allumée (piloté par CycleJourNuit).")]
    [Range(0f, 1f)] public float allumage = 1f;
    [Tooltip("Rendus de la lanterne (matériau Lanterne_HalloweenBits).")]
    public Renderer[] rendus;

    private MaterialPropertyBlock bloc;
    private float phase1, phase2, freq1 = 1f, freq2 = 1f;
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    public Color CouleurEffective { get { return couleurPropre || reglages == null ? couleur : reglages.couleur; } }

    private void OnEnable()
    {
        // Graine tirée de la position : chaque lanterne a sa phase et ses fréquences (±15 %), stables d'une partie à l'autre.
        Vector3 p = transform.position;
        float h1 = Hash(p.x * 12.9898f + p.z * 78.233f), h2 = Hash(p.x * 39.346f + p.z * 11.135f + p.y * 3.7f);
        phase1 = h1 * 6.2832f; phase2 = h2 * 6.2832f;
        freq1 = 0.85f + 0.3f * h2; freq2 = 0.85f + 0.3f * h1;
        Rafraichir();
    }

    private void LateUpdate() { Rafraichir(); }

    private static float Hash(float x) { float s = Mathf.Sin(x) * 43758.5453f; return s - Mathf.Floor(s); }

    // Facteur de scintillement à l'instant t, dans [1 - amplitude ; 1 + amplitude] : somme de deux sinus lents
    // (poids 0,62 et 0,38, donc bornée par 1), continue et sans à-coup.
    public float Scintillement(float t)
    {
        float amp = reglages != null ? reglages.amplitude : 0.1f;
        float v = reglages != null ? reglages.vitesse : 1f;
        float s = 0.62f * Mathf.Sin(6.2832f * 0.45f * freq1 * v * t + phase1) + 0.38f * Mathf.Sin(6.2832f * 1.1f * freq2 * v * t + phase2);
        return 1f + amp * s;
    }

    public void Rafraichir() { Rafraichir(Application.isPlaying ? Time.time : Time.realtimeSinceStartup); }

    // État à l'instant t (captures et mesures du banc).
    public void Rafraichir(float t)
    {
        float k = Mathf.Clamp01(allumage) * Scintillement(t);
        Color c = CouleurEffective;
        if (lumiere != null)
        {
            lumiere.color = c;
            lumiere.enabled = allumage > 0.01f;
            lumiere.intensity = intensite * k;
        }
        if (rendus == null) return;
        if (bloc == null) bloc = new MaterialPropertyBlock();
        float eclat = reglages != null ? reglages.eclatVitres : 1.1f;
        Color e = c * (eclat * k);
        foreach (Renderer r in rendus)
        {
            if (r == null) continue;
            r.GetPropertyBlock(bloc);
            bloc.SetColor(EmissionId, e);
            r.SetPropertyBlock(bloc);
        }
    }

    // Bornes, dans le repère du maillage, des triangles dont le centre UV tombe dans la zone. Faux si aucun.
    public static bool BornesZoneUV(Mesh m, Rect zone, out Bounds bornes)
    {
        bornes = new Bounds();
        if (m == null) return false;
        Vector3[] v = m.vertices; Vector2[] uv = m.uv; int[] tr = m.triangles;
        if (uv == null || uv.Length != v.Length) return false;
        bool trouve = false;
        for (int i = 0; i < tr.Length; i += 3)
        {
            Vector2 c = (uv[tr[i]] + uv[tr[i + 1]] + uv[tr[i + 2]]) / 3f;
            if (!zone.Contains(c)) continue;
            for (int k = 0; k < 3; k++)
            {
                Vector3 p = v[tr[i + k]];
                if (!trouve) { bornes = new Bounds(p, Vector3.zero); trouve = true; }
                else bornes.Encapsulate(p);
            }
        }
        return trouve;
    }

    // Centre de la cage, en coordonnées monde : centre des bornes des vitres, sur tous les maillages du modèle (post_lantern
    // a deux maillages : le poteau, et la lanterne en enfant) ; à défaut, centre des bornes des rendus.
    // `ignorer` : sous-arbre à écarter (flamme ajoutée en enfant : sa sphère a aussi des UV dans la case des vitres).
    // Lit les sommets des maillages : à appeler dans l'éditeur (construction), les maillages importés n'étant pas lisibles en jeu.
    public static Vector3 CentreCage(GameObject lanterne, Transform ignorer = null)
    {
        bool trouve = false; Bounds monde = new Bounds();
        foreach (MeshFilter mf in lanterne.GetComponentsInChildren<MeshFilter>(true))
        {
            if (ignorer != null && mf.transform.IsChildOf(ignorer)) continue;
            Bounds b;
            if (!BornesZoneUV(mf.sharedMesh, UvVitres, out b)) continue;
            for (int i = 0; i < 8; i++)   // coins des bornes locales, passés en monde
            {
                Vector3 coin = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 p = mf.transform.TransformPoint(coin);
                if (!trouve) { monde = new Bounds(p, Vector3.zero); trouve = true; } else monde.Encapsulate(p);
            }
        }
        if (trouve) return monde.center;
        foreach (Renderer r in lanterne.GetComponentsInChildren<Renderer>(true))
        {
            if (ignorer != null && r.transform.IsChildOf(ignorer)) continue;
            if (!trouve) { monde = r.bounds; trouve = true; } else monde.Encapsulate(r.bounds);
        }
        return trouve ? monde.center : lanterne.transform.position;
    }

    // Règle une lanterne posée (lantern_standing au sol, ou post_lantern : lanterne suspendue à un poteau, même atlas) :
    // flamme puis lumière au centre de la cage, matériau à vitres émissives, composant qui pilote lumière et vitres.
    // `flamme` : objet à recentrer avec la lumière (sphère de VillageBuilder, ou racine LanternFire de FireEffect avec
    // son halo) ; peut être nul, comme `materiau` et `reglages`. `intensite` : intensité allumée (2,2 pour les lanternes
    // de CycleJourNuit, 8 pour FireEffect.CreateLantern). `allumee` : état de départ (faux au village : CycleJourNuit
    // allume). `ombreSoleil` : faux = aucune partie du modèle ne projette d'ombre (lanterne au sol) ; vrai = le modèle
    // garde son ombre du soleil et de la lune (poteau de 3 m). Dans les deux cas la lumière de la lanterne est sans
    // ombre : rien ne peut la masquer.
    public static LanterneLumiere Configurer(GameObject lanterne, Light lumiere, Transform flamme, Material materiau,
        LanternesReglages reglages, float intensite, bool allumee, bool ombreSoleil)
    {
        Vector3 centre = CentreCage(lanterne, flamme);
        if (flamme != null) flamme.position = centre;            // d'abord la flamme : la lumière peut en être l'enfant
        if (lumiere != null)
        {
            lumiere.transform.position = centre;
            lumiere.shadows = LightShadows.None;
        }

        var liste = new System.Collections.Generic.List<Renderer>();
        foreach (MeshRenderer r in lanterne.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (flamme != null && r.transform.IsChildOf(flamme)) continue;
            r.shadowCastingMode = ombreSoleil ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = true;
            if (materiau != null) r.sharedMaterial = materiau;
            liste.Add(r);
        }
        LanterneLumiere ll = lanterne.GetComponent<LanterneLumiere>();
        if (ll == null) ll = lanterne.AddComponent<LanterneLumiere>();
        ll.reglages = reglages;
        ll.lumiere = lumiere;
        ll.intensite = intensite;
        ll.allumage = allumee ? 1f : 0f;
        ll.rendus = liste.ToArray();
        ll.Rafraichir();
        return ll;
    }
}
