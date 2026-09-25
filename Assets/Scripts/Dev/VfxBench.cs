using System.Collections;
using UnityEngine;

// Banc de vérification des effets visuels (scène Assets/Scenes/VfxBench.unity). Outil de développement, pas du gameplay :
// en Play mode, chaque poste rejoue son effet en boucle par son API publique (voir Docs/vfx.md). Les séquences qui, dans
// Relic, vivaient dans du code réseau ou de gameplay (SkillEffects, EnemyVisual, PlayerZone, RelicTurret) sont rejouées
// ici avec les mêmes appels statiques (FireballVisual, LowPolyBlast, SkullMissileVisual, GemBurst, DirtBurst,
// PortalTransit). ToutJouer() relance tous les postes en même temps (captures).
public class VfxBench : MonoBehaviour
{
    [Header("Matériaux communs")]
    [Tooltip("PortalVoxel (shader Relic/VertexColorUnlit) : gemmes des effets Relic.")]
    public Material gemmes;
    [Tooltip("FireBurst : boule de feu, explosion, gerbe de terre.")]
    public Material feu;
    public Material flamme;
    public Material fumee;

    [Header("Figurants (pose de repos échantillonnée au démarrage)")]
    public AnimationClip poseRepos;
    public GameObject[] figurants;

    [Header("Gemme Nyxessa")]
    public RelicBelt anneau;
    public float intervalleAnneau = 6f;

    [Header("Portail de donjon")]
    public PortalVisual portail;
    public float portailFerme = 2.5f;
    public float intervallePortail = 9f;

    [Header("Téléportation")]
    public PortalVisual portailTransit;
    public Transform corps;
    public Vector3 pointA = new Vector3(-3f, 0f, 0f);
    public Vector3 pointB = new Vector3(3f, 0f, 0f);
    public float intervalleTeleportation = 6f;

    [Header("Boule de feu (Relic)")]
    public Transform departBoule;
    public float intervalleBoule = 4f;

    [Header("Missile magique")]
    public Transform departMissile;
    public GemShape formeCrane;
    public float intervalleMissile = 4f;

    [Header("Bouclier de la relique")]
    public RelicShieldEtat bouclier;
    public float tenueBouclier = 3f;
    public float intervalleBouclier = 14f;

    [Header("Squelettes (désintégration, sortie de terre)")]
    public GameObject squeletteDesintegration;
    public GameObject squeletteSortie;
    public AnimationClip clipSortie;
    public float vitesseClipSortie = 1.5f;
    public float profondeurSortie = 1.9f;
    public float intervalleSquelettes = 6f;

    [Header("Effets du bac à sable")]
    public GameObject burn;
    public GameObject cone;
    public float dureeFlammes = 4f;
    public float pauseFlammes = 1.5f;
    public AuraSoin aura;
    public float intervalleAura = 2.5f;
    public Rugissement rugissement;
    public float intervalleRugissement = 3.5f;
    public OndeDeChoc ondeSaut;
    public OndeDeChoc ondeCharge;
    public float intervalleOndes = 2.5f;

    private Vector3 corpsOrigine;
    private bool versB = true;
    private GameObject enVolBoule, enVolMissile;

    private void Start()
    {
        if (poseRepos != null && figurants != null)
            foreach (GameObject f in figurants)
                if (f != null)
                {
                    Animator a = f.GetComponentInChildren<Animator>();
                    if (a != null) a.enabled = false;
                    poseRepos.SampleAnimation(f, 0f);
                }
        foreach (GameObject s in new[] { squeletteDesintegration, squeletteSortie })
            if (s != null && clipSortie != null)
            {
                Animator a = s.GetComponentInChildren<Animator>();
                if (a != null) a.enabled = false;
                clipSortie.SampleAnimation(s, clipSortie.length);
            }
        if (corps != null) corpsOrigine = corps.parent != null ? corps.parent.position : corps.position;
        ToutJouer();
    }

    // Relance tous les postes au même instant, puis chacun reboucle à son intervalle.
    public void ToutJouer()
    {
        StopAllCoroutines();
        if (enVolBoule != null) Destroy(enVolBoule);
        if (enVolMissile != null) Destroy(enVolMissile);
        Boucle(intervalleAnneau, () => { if (anneau != null) anneau.Pulse(); });
        Boucle(intervallePortail, () => StartCoroutine(CyclePortail()));
        Boucle(intervalleTeleportation, () => StartCoroutine(Passage()));
        Boucle(intervalleBoule, () => StartCoroutine(VolBoule()));
        Boucle(intervalleMissile, () => StartCoroutine(VolMissile()));
        Boucle(intervalleBouclier, () => StartCoroutine(CycleBouclier()));
        Boucle(intervalleSquelettes, () => StartCoroutine(Vaporisation()));
        Boucle(intervalleSquelettes, () => StartCoroutine(Sortie()));
        Boucle(dureeFlammes + pauseFlammes, () => StartCoroutine(Flammes()));
        Boucle(intervalleAura, () => { if (aura != null) aura.Jouer(); });
        Boucle(intervalleRugissement, () => { if (rugissement != null) rugissement.Jouer(); });
        Boucle(intervalleOndes, () => { if (ondeSaut != null) ondeSaut.Jouer(); if (ondeCharge != null) ondeCharge.Jouer(); });
    }

    private void Boucle(float intervalle, System.Action action)
    {
        StartCoroutine(BoucleRoutine(Mathf.Max(0.5f, intervalle), action));
    }

    private IEnumerator BoucleRoutine(float intervalle, System.Action action)
    {
        while (true)
        {
            action();
            yield return new WaitForSeconds(intervalle);
        }
    }

    // Portail : ouvert la plus grande partie du cycle, puis fermeture (implosion) et réouverture (gerbe) en fin de
    // cycle — PortalVisual.ouvert (dans Relic : Portal.IsOpen, fermé la nuit).
    private IEnumerator CyclePortail()
    {
        if (portail == null) yield break;
        portail.ouvert = true;
        yield return new WaitForSeconds(Mathf.Max(0f, intervallePortail - portailFerme - 1.5f));
        portail.ouvert = false;
        yield return new WaitForSeconds(portailFerme);
        portail.ouvert = true;
    }

    // Téléportation (Relic : PlayerZone.Transit) : corps masqué, PortalTransit.Depart + Ripple, puis Arrive + Ripple.
    private IEnumerator Passage()
    {
        if (portailTransit == null || corps == null || gemmes == null) yield break;
        const float depart = 1.1f, arrivee = 1.0f;
        Vector3 centre = portailTransit.Center;
        Vector3 cible = corpsOrigine + (versB ? pointB : pointA);
        Visible(corps, false);
        PortalTransit.Depart(Corps(corps.position), centre, gemmes, depart);
        portailTransit.Ripple();
        yield return new WaitForSeconds(depart);
        corps.position = cible;
        yield return null;
        PortalTransit.Arrive(Corps(cible), centre, gemmes, arrivee);
        portailTransit.Ripple();
        yield return new WaitForSeconds(arrivee);
        Visible(corps, true);
        versB = !versB;
    }

    // Volume du joueur de Relic (0,8 x 1,9 x 0,8), le pivot du figurant étant aux pieds.
    private static Bounds Corps(Vector3 pieds)
    {
        return new Bounds(pieds + Vector3.up * 0.95f, new Vector3(0.8f, 1.9f, 0.8f));
    }

    // Boule de feu (Relic : SkillEffects.AttachFireball puis Burst(point, 2,5, feu)) : vol en cloche 18 m/s, chute 5.
    private IEnumerator VolBoule()
    {
        if (departBoule == null || feu == null) yield break;
        if (enVolBoule != null) Destroy(enVolBoule);
        Vector3 velocity = departBoule.forward * 18f + Vector3.up * 1.5f;
        GameObject projectile = new GameObject("VfxBench_Boule");
        enVolBoule = projectile;
        projectile.transform.position = departBoule.position;
        projectile.transform.rotation = Quaternion.LookRotation(velocity);
        FireballVisual.Attach(projectile.transform, feu);
        float t = 0f;
        while (t < 1.3f && projectile != null)
        {
            t += Time.deltaTime;
            velocity += Vector3.down * 5f * Time.deltaTime;
            projectile.transform.position += velocity * Time.deltaTime;
            projectile.transform.rotation = Quaternion.LookRotation(velocity);
            if (projectile.transform.position.y <= 0.15f) break;
            yield return null;
        }
        if (projectile == null) yield break;
        Vector3 point = projectile.transform.position;
        Destroy(projectile);
        LowPolyBlast.Fire(point, 2.5f, feu);
        if (fumee != null)
        {
            FireEffect smoke = FireEffect.Create(null, point, 1f, 0.8f, flamme, fumee, false, true, false);
            yield return new WaitForSeconds(0.4f);
            if (smoke != null) smoke.SetEmitting(false);
            yield return new WaitForSeconds(2.6f);
            if (smoke != null) Destroy(smoke.gameObject);
        }
    }

    // Missile magique (Relic : RelicTurret.FireRpc + SkullMissileVisual) : éclat au départ, crâne 12 m/s, Shatter à l'arrivée.
    private IEnumerator VolMissile()
    {
        if (departMissile == null || formeCrane == null || gemmes == null) yield break;
        if (enVolMissile != null) Destroy(enVolMissile);
        Vector3 dir = departMissile.forward;
        GemBurst.Explode(departMissile.position + dir * 0.3f, 0.5f, gemmes);
        GameObject projectile = new GameObject("VfxBench_Missile");
        enVolMissile = projectile;
        projectile.transform.position = departMissile.position;
        projectile.transform.rotation = Quaternion.LookRotation(dir);
        SkullMissileVisual crane = SkullMissileVisual.Attach(projectile.transform, formeCrane, Vector3.zero, 0.55f, gemmes);
        float t = 0f;
        while (t < 1.6f && projectile != null)
        {
            t += Time.deltaTime;
            projectile.transform.position += dir * 12f * Time.deltaTime;
            yield return null;
        }
        if (crane != null) crane.Shatter();
        if (projectile != null) Destroy(projectile, 0.1f);
    }

    // Bouclier : incantation, tenue, coups de plus en plus graves (bleu, orange, rouge), rupture.
    private IEnumerator CycleBouclier()
    {
        if (bouclier == null) yield break;
        bouclier.Baisser();
        yield return new WaitForSeconds(0.3f);
        bouclier.Lever();
        yield return new WaitForSeconds(bouclier.CastSeconds + tenueBouclier);
        float[] coups = { 60f, 90f, 90f, 50f, 40f };
        for (int i = 0; i < coups.Length && bouclier.IsUp; i++)
        {
            float azimut = Random.Range(0f, Mathf.PI * 2f);
            Vector3 point = bouclier.BasePosition + new Vector3(Mathf.Cos(azimut), 0f, Mathf.Sin(azimut)) * bouclier.Radius + Vector3.up * Random.Range(0.8f, 3f);
            bouclier.Frapper(coups[i], point);
            yield return new WaitForSeconds(1.2f);
        }
    }

    // Désintégration (Relic : EnemyVisual.Vaporize) : rendus coupés, GemBurst.Rise sur leur volume, retour 2,6 s après.
    private IEnumerator Vaporisation()
    {
        if (squeletteDesintegration == null || gemmes == null) yield break;
        Renderer[] rendus = squeletteDesintegration.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in rendus) r.enabled = true;
        Bounds bounds = new Bounds(squeletteDesintegration.transform.position + Vector3.up, new Vector3(0.8f, 2f, 0.8f));
        bool premier = true;
        foreach (Renderer r in rendus)
        {
            if (r is ParticleSystemRenderer) continue;
            if (premier) { bounds = r.bounds; premier = false; }
            else bounds.Encapsulate(r.bounds);
            r.enabled = false;
        }
        GemBurst.Rise(bounds, gemmes);
        yield return new WaitForSeconds(2.6f);
        foreach (Renderer r in rendus) r.enabled = true;
    }

    // Sortie de terre (Relic : EnemyVisual.UpdateIntro / ApplyRise) : clip Skeletons_Spawn_Ground échantillonné à 1,5,
    // montée depuis 1,9 m sous terre pendant 75 % du clip, DirtBurst au départ (1) et à 35 % (0,55).
    private IEnumerator Sortie()
    {
        if (squeletteSortie == null || clipSortie == null) yield break;
        Transform modele = squeletteSortie.transform;
        Vector3 sol = modele.parent != null ? modele.parent.position : Vector3.zero;
        float joue = clipSortie.length / vitesseClipSortie;
        float finMontee = joue * 0.75f;
        bool seconde = false;
        if (feu != null) DirtBurst.Spawn(sol, feu, 1f);
        float t = 0f;
        while (t < joue)
        {
            t += Time.deltaTime;
            if (!seconde && t >= joue * 0.35f)
            {
                seconde = true;
                if (feu != null) DirtBurst.Spawn(sol, feu, 0.55f);
            }
            clipSortie.SampleAnimation(squeletteSortie, Mathf.Min(clipSortie.length, t * vitesseClipSortie));
            float k = Mathf.Clamp01(t / finMontee);
            float eased = 1f - Mathf.Pow(1f - k, 2f);
            modele.localPosition = new Vector3(0f, -profondeurSortie * (1f - eased), 0f);
            yield return null;
        }
        modele.localPosition = Vector3.zero;
    }

    // Flammèches du burn et cône de flammes : effets bouclés, Play() puis Stop() (fin du burn, relâchement du sort).
    private IEnumerator Flammes()
    {
        Particules(burn, true);
        Particules(cone, true);
        yield return new WaitForSeconds(dureeFlammes);
        Particules(burn, false);
        Particules(cone, false);
    }

    private static void Particules(GameObject racine, bool jouer)
    {
        if (racine == null) return;
        foreach (ParticleSystem ps in racine.GetComponentsInChildren<ParticleSystem>())
            if (jouer) ps.Play(false); else ps.Stop(false);
    }

    private static void Visible(Transform t, bool visible)
    {
        foreach (Renderer r in t.GetComponentsInChildren<Renderer>()) r.enabled = visible;
    }
}
