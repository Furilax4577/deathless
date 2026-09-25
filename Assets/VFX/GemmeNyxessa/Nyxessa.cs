using UnityEngine;

// Événements auxquels la relique Nyxessa réagit.
public enum ReactionNyxessa { OuverturePortail, FermeturePortail, TirMissile, PassageJoueur }

// Réactions de la relique Nyxessa (25/09/2026), à poser sur la racine du prefab GemmeNyxessa. Chaque événement a une
// réponse courte et distincte, dans le langage existant (ceinture RelicBelt, rotation CrystalSpin, lumière VfxLumiere,
// gerbes GemBurst) :
// - OuverturePortail : la ceinture s'élargit (RelicBelt.Pulse) et accélère (× 3, retour en 1,5 s), éclat de lumière
//   (× 2,5), gerbe de gemmes au cristal ;
// - FermeturePortail : la ceinture se resserre (RelicBelt.Resserrer) et ralentit (× 0,25, retour en 2 s), la lumière
//   baisse (jusqu'à × 0,35) puis revient, implosion de gemmes au cristal ;
// - TirMissile : le cristal pulse (× 1,15) et recule à l'opposé du tir, éclat bref au point de départ du missile ;
// - PassageJoueur : une petite onde fait le tour de la ceinture (RelicBelt.Parcourir) avec un scintillement.
// Accès sans dépendance dure : Nyxessa.Signaler(type, point) ne fait rien s'il n'y a pas de relique dans la scène.
public class Nyxessa : MonoBehaviour
{
    public static Nyxessa Instance { get; private set; }

    [SerializeField] private RelicBelt ceinture;
    [SerializeField] private Transform cristal;
    [SerializeField] private CrystalSpin rotation;
    [SerializeField] private VfxLumiere lumiere;
    [Tooltip("Matériau des gerbes de gemmes (PortalVoxel).")]
    [SerializeField] private Material gemmes;

    private float ouverture = -100f, fermeture = -100f, tir = -100f;
    private Vector3 directionTir = Vector3.forward;
    private Vector3 echelleCristal = Vector3.one;

    // Signale un événement à la relique de la scène (sans effet s'il n'y en a pas).
    public static void Signaler(ReactionNyxessa type, Vector3 point = default(Vector3))
    {
        if (Instance != null) Instance.Reagir(type, point);
    }

    private void Awake()
    {
        Instance = this;
        if (ceinture == null) ceinture = GetComponentInChildren<RelicBelt>();
        if (rotation == null) rotation = GetComponentInChildren<CrystalSpin>();
        if (cristal == null && rotation != null) cristal = rotation.transform;
        if (lumiere == null) lumiere = GetComponentInChildren<VfxLumiere>();
        if (cristal != null) echelleCristal = cristal.localScale;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public Vector3 CentreCristal { get { return cristal != null ? cristal.position : transform.position; } }

    // Réaction à un événement ; `point` : départ du missile pour TirMissile (ignoré sinon).
    public void Reagir(ReactionNyxessa type, Vector3 point = default(Vector3))
    {
        switch (type)
        {
            case ReactionNyxessa.OuverturePortail:
                ouverture = Time.time;
                if (ceinture != null) ceinture.Pulse();
                if (gemmes != null) GemBurst.Explode(CentreCristal, 1.3f, gemmes);
                break;
            case ReactionNyxessa.FermeturePortail:
                fermeture = Time.time;
                if (ceinture != null) ceinture.Resserrer();
                if (gemmes != null) GemBurst.Implode(CentreCristal, 1.3f, gemmes);
                break;
            case ReactionNyxessa.TirMissile:
                tir = Time.time;
                Vector3 d = point - CentreCristal;
                directionTir = d.sqrMagnitude > 1e-4f ? d.normalized : transform.forward;
                VfxLumiere.Eclat(point, VfxTheme.Nyxessa, VfxTailleLumiere.Petite, 0.05f);
                break;
            case ReactionNyxessa.PassageJoueur:
                if (ceinture != null) ceinture.Parcourir();
                break;
        }
    }

    // Tir d'un missile magique par la relique : éclat au départ, réaction, et crâne en gemmes à l'échelle `echelle`
    // (1,5 par défaut pour la relique). Le projectile (vide) est déplacé par l'appelant.
    public SkullMissileVisual TirerMissile(Transform projectile, GemShape forme, Material materiau, float echelle = 1.5f)
    {
        Vector3 depart = projectile.position;
        GemBurst.Explode(depart + projectile.forward * 0.3f * echelle, 0.5f * echelle, materiau);
        Reagir(ReactionNyxessa.TirMissile, depart);
        return SkullMissileVisual.Attach(projectile, forme, Vector3.zero, 0.55f, materiau, echelle);
    }

    private void Update()
    {
        float t = Time.time;
        float ao = t - ouverture, af = t - fermeture, at = t - tir;
        // Vitesse de la ceinture et du cristal.
        float vitesse = 1f;
        if (ao >= 0f && ao < 1.5f) vitesse *= 1f + 2f * (1f - ao / 1.5f) * (1f - ao / 1.5f);
        if (af >= 0f && af < 2f) vitesse *= Mathf.Lerp(0.25f, 1f, Mathf.SmoothStep(0f, 1f, af / 2f));
        if (ceinture != null) ceinture.multiplicateurVitesse = vitesse;
        if (rotation != null) rotation.multiplicateur = vitesse;
        // Lumière : éclat à l'ouverture, baisse à la fermeture, pulsation au tir.
        float f = 1f;
        if (ao >= 0f && ao < 1f) f += 1.5f * Mathf.Exp(-ao * 3f);
        if (af >= 0f && af < 1.8f) f *= 1f - 0.65f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(af / 1.8f));
        if (at >= 0f && at < 0.4f) f += 0.6f * Mathf.Exp(-at * 8f);
        if (lumiere != null) lumiere.facteur = f;
        // Cristal : pulsation et recul au tir.
        if (cristal != null)
        {
            float p = at >= 0f && at < 0.3f ? Mathf.Sin(Mathf.PI * at / 0.3f) : 0f;
            cristal.localScale = echelleCristal * (1f + 0.15f * p);
            if (rotation != null)
                rotation.decalage = cristal.parent != null ? cristal.parent.InverseTransformVector(-directionTir * 0.18f * p) : -directionTir * 0.18f * p;
        }
    }
}
