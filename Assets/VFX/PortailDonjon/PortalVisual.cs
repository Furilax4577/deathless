// COPIE « Visual only » (bac à sable) de Relic/Assets/Scripts/PortalVisual.cs : la lecture de Portal.IsOpen est remplacée par
// le champ `ouvert` ; tout le reste (soupe de gemmes, ouverture, fermeture, onde) est identique.
using System.Collections.Generic;
using UnityEngine;

// Portail animé (étape 30F, refait à l'étape 68 d'après les portails de Rick et Morty) : une « soupe » de petites
// gemmes low poly dans un disque rond, construite par script au démarrage, en un seul maillage. Purement visuel et local.
// - Soupe : environ 1 700 octaèdres irréguliers (chacun ses proportions), chacun à sa profondeur (plus épais au centre,
//   comme une lentille), qui nagent en suivant le tourbillon (plus vite près du centre), tournent lentement sur
//   eux-mêmes, sont aspirés vers l'œil et renaissent au bord, en ondulant d'avant en arrière. Pas de fond : on voit à
//   travers les trous de la soupe (demande de Quentin).
// - Couleurs : chaque gemme prend l'une de quatre teintes de vert dans un champ de taches qui dérive et se déforme
//   (façon masque de Rorschach), plus clair au centre ; chaque facette est ombrée selon son orientation du moment,
//   donc les gemmes scintillent en tournant (shader Relic/VertexColorUnlit, relief peint dans les couleurs).
// - Bord : il bouillonne (les gemmes du pourtour vont et viennent) ; aucune pièce ne tourne d'un bloc.
// - Ouvert seulement pendant le répit (Portal.IsOpen) : ouverture (gerbe de gemmes GemBurst, puis la soupe jaillit du
//   centre vers le bord avec un rebond), attente, fermeture (des gemmes sont aspirées vers le centre, la soupe
//   s'emballe puis s'y résorbe).
// Les anciens quads (swirlFront, swirlBack, glow) sont masqués.
public class PortalVisual : MonoBehaviour
{
    [SerializeField] private Transform swirlFront;
    [SerializeField] private Transform swirlBack;
    [SerializeField] private Transform glow;
    [SerializeField] private Light portalLight;
    [SerializeField] private float frontSpeed = 70f;
    [SerializeField] private float backSpeed = -45f;
    [SerializeField] private float baseIntensity = 3f;
    [Header("Portail en voxels (étape 68)")]
    [Tooltip("Matériau à couleurs par sommet, opaque (shader Relic/VertexColorUnlit).")]
    [SerializeField] private Material voxelMaterial;
    [SerializeField] private Material lumpMaterial;   // plus utilisé (ancienne gerbe), gardé pour la scène
    [SerializeField] private Material glowMaterial;   // plus utilisé (ancienne version), gardé pour la scène
    [SerializeField] private Material depthMaterial;  // plus utilisé (ancienne version), gardé pour la scène
    [Tooltip("Rayon du disque (m). 1,35 dans Relic ; 1,6 depuis le 25/09/2026 (portail plus grand).")]
    [SerializeField] private float radius = 1.6f;
    [Tooltip("Profondeur visible au centre, en fraction du diamètre (disque épais à faces plates, bord arrondi).")]
    [Range(0.1f, 0.6f)] [SerializeField] private float epaisseur = 0.35f;
    [Tooltip("Prévenir la relique (Nyxessa) à l'ouverture et à la fermeture.")]
    [SerializeField] private bool reagirRelique = true;
    [Tooltip("Taille moyenne d'un cube de la soupe (m).")]
    [SerializeField] private float cell = 0.1f;
    [Tooltip("Nombre de cubes de la soupe.")]
    [SerializeField] private int particleCount = 2400;

    private const float OpenSeconds = 1.3f;
    private const float CloseSeconds = 0.9f;

    // Aperçu (outil d'enregistrement) : ferme tous les portails quel que soit l'état de la partie.
    public static bool PreviewClosed;
    [Tooltip("Bac à sable : remplace Portal.IsOpen (ouvert pendant le répit dans Relic).")]
    public bool ouvert = true;

    private enum State { Closed, Opening, Open, Closing }
    private State state = State.Closed;
    private float stateSince;

    private Transform root;
    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private Vector3[] vertices;
    private Color[] colors;
    // Cubes de la soupe : rayon relatif, angle, profondeur de base (fraction de l'épaisseur locale), taille, phase.
    private float[] pRadius;
    private float[] pAngle;
    private float[] pDepth;
    private float[] pSize;
    private float[] pPhase;
    private Vector3[] pStretch;      // proportions de la gemme (irrégulière)
    private Quaternion[] pRotation;  // orientation de départ
    private Vector3[] pSpinAxis;     // axe et vitesse de rotation propre
    private float[] pSpinSpeed;
    private float flowTime;
    private float twist;
    private float meanRotation;
    // Goutte d'eau (passage d'un joueur, 25/09/2026) : instant de départ (-100 = aucune) et sens (entrée : anneaux
    // du centre vers le bord ; sortie : du bord vers le centre).
    private float goutteStart = -100f;
    private bool goutteSortie;
    private const float GoutteSecondes = 1.5f;
    private VfxLumiere lumiere;

    // Centre de la soupe dans le monde (où convergent les gemmes d'un joueur qui entre).
    public Vector3 Center => root != null ? root.position : transform.position;

    // Entrée d'un joueur : goutte d'eau, des anneaux partent du centre vers le bord et s'amortissent, avec un creux
    // qui rebondit au centre.
    public void Entrer()
    {
        goutteStart = Time.time;
        goutteSortie = false;
    }

    // Sortie d'un joueur : l'inverse, les anneaux partent du bord, convergent vers le centre et s'y résorbent (petite
    // bosse au centre à la fin).
    public void Sortir()
    {
        goutteStart = Time.time;
        goutteSortie = true;
    }

    // Ancienne onde de Relic : remplacée par la goutte d'eau d'entrée.
    public void Ripple()
    {
        Entrer();
    }

    private static Color Dark => VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Base, new Color(0.07f, 0.38f, 0.05f));
    private static Color Mid => VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Vif, new Color(0.25f, 0.7f, 0.08f));
    private static Color Light => VfxPalette.Couleur(VfxTheme.Nyxessa, VfxRole.Coeur, new Color(0.55f, 0.95f, 0.2f));
    private static Color Pale => VfxPalette.Accent(VfxTheme.Nyxessa, "Éclat", new Color(0.76f, 1f, 0.44f)) * 1.25f;


    private void Start()
    {
        if (voxelMaterial == null)
            return;
        foreach (Transform old in new[] { swirlFront, swirlBack, glow })
            if (old != null)
                foreach (Renderer r in old.GetComponentsInChildren<Renderer>())
                    r.enabled = false;
        // Lumière commune des effets (thème Nyxessa, grande classe) sur la lumière du prefab.
        if (portalLight != null)
        {
            lumiere = portalLight.GetComponent<VfxLumiere>();
            if (lumiere == null) lumiere = portalLight.gameObject.AddComponent<VfxLumiere>();
            lumiere.theme = VfxTheme.Nyxessa;
            lumiere.taille = VfxTailleLumiere.Grande;
        }

        // Racine d'échelle 1 dans le monde, dans le plan de l'ancien tourbillon (un quad : face selon son z local).
        Transform parent = swirlFront != null ? swirlFront.parent : transform;
        root = new GameObject("VoxelPortal").transform;
        root.SetParent(parent, false);
        root.localPosition = Vector3.zero;
        root.localRotation = swirlFront != null ? swirlFront.localRotation : Quaternion.identity;
        Vector3 lossy = parent.lossyScale;
        root.localScale = new Vector3(1f / Mathf.Max(0.001f, lossy.x), 1f / Mathf.Max(0.001f, lossy.y), 1f / Mathf.Max(0.001f, lossy.z));

        BuildCells();
        BuildMesh();

        bool open = ouvert;
        SetState(open ? State.Open : State.Closed);
        if (open)
        {
            UpdateVoxels(1f, 1f);
            if (lumiere != null) lumiere.Allumer();
        }
        else
            ApplyClosed();
    }

    // Cubes répartis uniformément dans le disque, chacun à sa profondeur, sa taille et sa phase.
    private void BuildCells()
    {
        int n = Mathf.Max(100, particleCount);
        pRadius = new float[n];
        pAngle = new float[n];
        pDepth = new float[n];
        pSize = new float[n];
        pPhase = new float[n];
        pStretch = new Vector3[n];
        pRotation = new Quaternion[n];
        pSpinAxis = new Vector3[n];
        pSpinSpeed = new float[n];
        for (int i = 0; i < n; i++)
            Respawn(i, Mathf.Sqrt(Random.value));
    }

    // (Re)naissance d'un cube au rayon relatif `r` : profondeur, taille et phase tirées au hasard.
    private void Respawn(int i, float r)
    {
        pRadius[i] = r;
        pAngle[i] = Random.Range(0f, Mathf.PI * 2f);
        // Profondeur : répartie des deux côtés, plus serrée près du plan (un cœur dense, des cubes plus rares loin).
        float d = Random.Range(-1f, 1f);
        pDepth[i] = d * Mathf.Abs(d);
        pSize[i] = Random.Range(0.6f, 1.05f);
        pPhase[i] = Random.Range(0f, 100f);
        pStretch[i] = new Vector3(Random.Range(0.6f, 1.2f), Random.Range(0.8f, 1.5f), Random.Range(0.6f, 1.2f));
        pRotation[i] = Random.rotation;
        pSpinAxis[i] = Random.onUnitSphere;
        pSpinSpeed[i] = Random.Range(30f, 110f);
    }

    // Un seul maillage : 24 sommets (8 faces de 3 sommets, propres à chaque face) et 24 indices par gemme.
    private void BuildMesh()
    {
        int gems = pRadius.Length;
        vertices = new Vector3[gems * LowPolyGem.VerticesPerGem];
        colors = new Color[vertices.Length];
        int[] triangles = LowPolyGem.Triangles(gems);
        mesh = new Mesh { name = "VoxelPortal" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(radius * 2.4f, radius * 2.4f, radius * 2f));
        root.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        meshRenderer = root.gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = voxelMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    // Ecrit une gemme de la soupe (LowPolyGem : octaèdre irrégulier, facettes ombrées selon leur orientation).
    private void WriteGem(int index, Vector3 center, float size, Vector3 stretch, Quaternion rotation, Color color)
    {
        LowPolyGem.Write(vertices, colors, index, center, size, stretch, rotation, color, LowPolyGem.DefaultLight);
    }

    private void SetState(State next)
    {
        state = next;
        stateSince = Time.time;
    }

    private void ApplyClosed()
    {
        root.gameObject.SetActive(false);
        if (lumiere != null)
            lumiere.Eteindre();
        else if (portalLight != null)
            portalLight.enabled = false;
    }

    // Gerbe de gemmes au centre du portail : elles jaillissent à l'ouverture, elles sont aspirées à la fermeture.
    private void Pop(float size, bool implode)
    {
        if (voxelMaterial == null)
            return;
        if (implode)
            GemBurst.Implode(root.position, size, voxelMaterial);
        else
            GemBurst.Explode(root.position, size, voxelMaterial);
    }

    // Rebond élastique : dépasse un peu 1 puis s'y pose.
    private static float ElasticOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(2f, -9f * t) * Mathf.Cos(t * Mathf.PI * 2.4f);
    }

    private void Update()
    {
        float t = Time.time;
        if (root == null)
        {
            // Sans le matériau des voxels : l'ancien portail tourne comme avant.
            if (portalLight != null)
                portalLight.intensity = baseIntensity * (1f + 0.15f * Mathf.Sin(t * 5.3f) + 0.08f * Mathf.Sin(t * 13.1f));
            if (swirlFront != null) swirlFront.Rotate(0f, 0f, frontSpeed * Time.deltaTime, Space.Self);
            if (swirlBack != null) swirlBack.Rotate(0f, 0f, backSpeed * Time.deltaTime, Space.Self);
            if (glow != null) glow.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * 1.7f));
            return;
        }

        bool wantOpen = !PreviewClosed && ouvert;
        if (wantOpen && (state == State.Closed || state == State.Closing))
        {
            SetState(State.Opening);
            root.gameObject.SetActive(true);
            if (lumiere != null) lumiere.Allumer();
            else if (portalLight != null) portalLight.enabled = true;
            Pop(1f, false);
            if (reagirRelique) Nyxessa.Signaler(ReactionNyxessa.OuverturePortail);
        }
        else if (!wantOpen && (state == State.Open || state == State.Opening))
        {
            SetState(State.Closing);
            Pop(1.3f, true);
            if (reagirRelique) Nyxessa.Signaler(ReactionNyxessa.FermeturePortail);
        }
        if (state == State.Closed)
            return;

        float since = Time.time - stateSince;
        float reveal = 1f;   // rayon (relatif) jusqu'où les cubes sont sortis
        float speed = 1f;    // vitesse des taches et de la houle
        float light = 1f;
        if (state == State.Opening)
        {
            float k = since / OpenSeconds;
            reveal = ElasticOut(k * 1.1f) * 1.1f;
            speed = Mathf.Lerp(4f, 1f, k);
            light = Mathf.Clamp01(k * 2f);
            if (k >= 1f)
                SetState(State.Open);
        }
        else if (state == State.Closing)
        {
            float k = since / CloseSeconds;
            reveal = k < 0.25f ? 1.1f : Mathf.Lerp(1.1f, -0.1f, Mathf.Pow((k - 0.25f) / 0.75f, 1.4f));
            speed = Mathf.Lerp(1f, 5f, k);
            light = 1f - k;
            if (k >= 1f)
            {
                SetState(State.Closed);
                ApplyClosed();
                return;
            }
        }
        if (lumiere != null)
            lumiere.facteur = light;
        else if (portalLight != null)
            portalLight.intensity = baseIntensity * light * (1f + 0.15f * Mathf.Sin(t * 5.3f) + 0.08f * Mathf.Sin(t * 13.1f));

        flowTime += Time.deltaTime * speed;
        twist += Time.deltaTime * 0.55f * speed;
        // Invisible (hors du champ de la caméra) : on ne recalcule pas les cubes.
        if (state == State.Open && meshRenderer != null && !meshRenderer.isVisible)
            return;
        UpdateVoxels(reveal, speed);
    }

    // Fait nager la soupe et recalcule chaque cube : position (tourbillon, aspiration, ondulation en profondeur),
    // couleur (taches), présence (ouverture et fermeture, bord bouillonnant).
    private void UpdateVoxels(float reveal, float speed)
    {
        float dt = Time.deltaTime * speed;
        float drift = flowTime * 0.12f;
        float t = flowTime;
        // Rotation moyenne des cubes (celle d'un cube à mi-rayon) : le champ de couleurs la suit.
        meanRotation += dt * (0.35f + 1.6f * 0.25f);
        for (int i = 0; i < pRadius.Length; i++)
        {
            // Tourbillon (plus rapide près de l'œil) et lente aspiration ; arrivé au centre, le cube renaît au bord.
            float r = pRadius[i];
            pAngle[i] -= dt * (0.35f + 1.6f * (1f - r) * (1f - r));
            r -= dt * (0.025f + 0.05f * (1f - r));
            if (r <= 0.04f)
            {
                Respawn(i, 1.02f);
                r = pRadius[i];
            }
            pRadius[i] = r;
            float a = pAngle[i];
            Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;

            // Couleur : le champ de taches tourne avec la soupe (angle `fieldAngle` : même rotation moyenne que les cubes),
            // pour qu'un cube garde à peu près sa couleur en nageant (sinon la soupe scintille comme un buisson), avec un
            // enroulement borné et trois bras en spirale qui gardent le tourbillon lisible ; plus clair au centre.
            float fa = a + meanRotation + (1.4f - r) * (1.6f + 0.6f * Mathf.Sin(twist * 0.6f));
            Vector2 q = new Vector2(Mathf.Cos(fa), Mathf.Sin(fa)) * r;
            float value = Mathf.PerlinNoise(q.x * 1.5f + 10f + drift, q.y * 1.5f + 20f - drift * 0.7f) * 0.6f
                + Mathf.PerlinNoise(q.x * 3.2f + 40f - drift, q.y * 3.2f + 60f + drift) * 0.15f
                + 0.25f * (0.5f + 0.5f * Mathf.Sin(3f * fa + 7f * r - t * 0.8f));
            value += (0.55f - r) * 0.35f;
            Color color = value < 0.42f ? Dark : value < 0.53f ? Mid : value < 0.63f ? Light : Pale;

            // Présence : ouverture et fermeture (le rayon `reveal` avance ou recule), bord qui bouillonne, et le cube qui
            // renaît au bord grandit au lieu d'apparaître d'un coup.
            float edge = 0.95f + 0.08f * Mathf.PerlinNoise(a * 1.6f + 5f, t * 0.9f);
            float presence = Mathf.Clamp01((reveal - r) * 6f) * Mathf.Clamp01((edge - r) * 10f);
            if (presence <= 0.01f)
            {
                WriteGem(i, Vector3.zero, 0f, Vector3.one, Quaternion.identity, color);
                continue;
            }
            // Profondeur : disque épais à faces presque plates et bord arrondi (profil (1 - r^4)^0,5), demi-épaisseur
            // calculée pour que la profondeur visible au centre (gemmes et houle comprises) fasse `epaisseur` × diamètre ;
            // chaque cube ondule d'avant en arrière à son rythme, et une houle partie du centre soulève la soupe.
            float demi = Mathf.Max(0.05f, (epaisseur * 2f * radius - 0.2f) / 2.3f);
            float thickness = demi * Mathf.Sqrt(Mathf.Max(0f, 1f - r * r * r * r)) + 0.03f;
            float wave = 0.25f * Mathf.Sin(t * 1.7f + pPhase[i]) + 0.2f * Mathf.Sin(r * 8f - t * 3f);
            float z = (pDepth[i] + wave * 0.3f) * thickness;
            // Goutte d'eau : trois anneaux (0,16 s d'écart) qui parcourent la surface, crête suivie d'un creux, amortis ;
            // entrée : du centre vers le bord, creux au centre qui rebondit ; sortie : du bord vers le centre, petite
            // bosse au centre quand ils s'y résorbent. Les crêtes s'éclaircissent.
            float goutte = Time.time - goutteStart;
            if (goutte >= 0f && goutte < GoutteSecondes)
            {
                float amort = 1f - goutte / GoutteSecondes;
                // Pendant la goutte, le cœur clair de la soupe est atténué pour que les crêtes se détachent.
                if (color == Pale && amort > 0.25f)
                    color = Light;
                for (int k = 0; k < 3; k++)
                {
                    float ak = goutte - k * 0.16f;
                    if (ak < 0f) continue;
                    float ring = goutteSortie ? 1.1f - ak * 1.2f : ak * 1.2f;
                    if (ring < -0.1f || ring > 1.2f) continue;
                    float amp = 0.34f * (1f - k * 0.25f) * amort * (goutteSortie ? Mathf.Clamp01(ak / 0.25f) : Mathf.Exp(-ak * 1.2f));
                    float d1 = (r - ring) / 0.09f;
                    float creux = Mathf.Exp(-((r - ring + (goutteSortie ? -0.16f : 0.16f)) / 0.09f) * ((r - ring + (goutteSortie ? -0.16f : 0.16f)) / 0.09f));
                    float crete = Mathf.Exp(-d1 * d1);
                    z += amp * (crete - 0.55f * creux);
                    // Crête claire, creux sombre : les anneaux se lisent aussi de face.
                    if (crete * amp > 0.1f)
                        color = Pale;
                    else if (creux * amp > 0.12f)
                        color = Dark;
                }
                float centre = Mathf.Exp(-r * r / 0.02f);
                if (!goutteSortie)
                    z -= 0.28f * centre * Mathf.Cos(goutte * 14f) * Mathf.Exp(-goutte * 3.5f);
                else
                    z += 0.22f * centre * Mathf.Sin(Mathf.PI * Mathf.Clamp01((goutte - 0.8f) / 0.5f));
            }
            float size = cell * pSize[i] * presence * (0.85f + 0.15f * Mathf.Sin(t * 3.1f + pPhase[i] * 2f));
            // Chaque gemme tourne lentement sur elle-même : ses facettes changent de teinte, la soupe scintille.
            Quaternion spin = Quaternion.AngleAxis(t * pSpinSpeed[i], pSpinAxis[i]) * pRotation[i];
            WriteGem(i, new Vector3(p.x * radius, p.y * radius, z), size, pStretch[i], spin, color);
        }
        mesh.vertices = vertices;
        mesh.colors = colors;
    }
}
