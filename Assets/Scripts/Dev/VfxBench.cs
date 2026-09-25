using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Banc de vérification des effets visuels (scène Assets/Scenes/VfxBench.unity). Outil de développement, pas du gameplay.
// En Play mode, chaque poste rejoue en boucle un geste de personnage et son effet, synchronisés : les mannequins
// KayKit (Mannequin_Medium, équipés par MannequinEquip à partir d'un asset WeaponStyle) sont posés image par image par
// un PlayableGraph en mise à jour manuelle (temps du clip piloté ici), et l'effet part à l'instant clé du clip
// (impact, tir, cri...), calculé au démarrage en échantillonnant le clip (voir Docs/vfx.md). Les séquences qui, dans
// Relic, vivaient dans du code réseau ou de gameplay (SkillEffects, EnemyVisual, PlayerZone, RelicTurret) sont
// rejouées avec les mêmes appels statiques. ToutJouer() relance tous les postes au même instant (captures).
public class VfxBench : MonoBehaviour
{
    [Header("Matériaux communs")]
    [Tooltip("PortalVoxel (shader Relic/VertexColorUnlit) : gemmes des effets Relic.")]
    public Material gemmes;
    [Tooltip("FireBurst : boule de feu, explosion, gerbe de terre.")]
    public Material feu;
    public Material flamme;
    public Material fumee;

    [Header("Clips (KayKit Rig_Medium)")]
    public AnimationClip clipIdle;           // Idle_A
    public AnimationClip clipMarche;         // Walking_A
    public AnimationClip clipCourse;         // Running_A
    public AnimationClip clipGarde;          // Melee_Blocking (haut du corps pendant la course)
    public AnimationClip clipCoupBouclier;   // Melee_Block_Attack
    public AnimationClip clipIdle2H;         // Melee_2H_Idle
    public AnimationClip clipSautFrappe;     // Melee_1H_Attack_Jump_Chop
    public AnimationClip clipCri;            // Skeletons_Taunt_Longer
    public AnimationClip clipSoin;           // Ranged_Magic_Raise
    public AnimationClip clipTir;            // Ranged_Magic_Shoot
    public AnimationClip clipIncantation;    // Ranged_Magic_Spellcasting_Long
    public AnimationClip clipSortie;         // Skeletons_Spawn_Ground

    [Header("Gemme Nyxessa + missile tiré par la relique")]
    public RelicBelt anneau;
    public float intervalleAnneau = 6f;
    public Transform cristal;
    public GemShape formeCrane;
    public GameObject cibleMissile;
    [Tooltip("Échelle du missile tiré par la relique (1 = missile du nécromancien).")]
    public float echelleMissile = 1.5f;
    public float intervalleMissile = 4f;

    [Header("Portail de donjon")]
    public PortalVisual portail;
    public float portailFerme = 2.5f;
    public float intervallePortail = 9f;

    [Header("Téléportation (mannequin sans arme)")]
    public PortalVisual portailTransit;
    public GameObject voyageur;
    public Vector3 pointA = new Vector3(-3f, 0f, 0f);
    public Vector3 pointB = new Vector3(3f, 0f, 0f);
    public float intervalleTeleportation = 6.5f;

    [Header("Bouclier de la relique")]
    public RelicShieldEtat bouclier;
    public float tenueBouclier = 3f;
    public float intervalleBouclier = 14f;

    [Header("Cône de flammes (mage bâton) + flammèches du burn sur la cible")]
    public GameObject mageCone;
    public GameObject cone;
    public GameObject cibleCone;
    public GameObject burn;
    public float dureeCone = 3f;
    public float intervalleCone = 7.5f;

    [Header("Boule de feu (mage bâton)")]
    public GameObject mageBoule;
    public GameObject cibleBoule;
    public float intervalleBoule = 3.5f;

    [Header("Aura de soin (paladin épée + bouclier, allié)")]
    public GameObject paladin;
    public GameObject allie;
    public AuraSoin aura;
    public float intervalleAura = 3.5f;

    [Header("Rugissement (viking hache + bouclier)")]
    public GameObject viking;
    public Rugissement rugissement;
    public float intervalleRugissement = 5f;

    [Header("Saut percutant (hache à deux mains)")]
    public GameObject barbare;
    public OndeDeChoc ondeSaut;
    [Tooltip("Bond vers l'avant (m) pendant la phase aérienne du clip, et hauteur ajoutée au saut du clip (m).")]
    public float distanceSaut = 5f;
    public float hauteurSaut = 0.6f;
    public float intervalleSaut = 4f;

    [Header("Charge bélier (chevalier épée + bouclier) : ruée, bulle en tête de bélier, traînée, onde")]
    public GameObject chevalier;
    public ChargeBelier charge;
    public float distanceCharge = 7f;
    public float dureeRuee = 0.5f;
    public float anticipation = 0.18f;
    [Tooltip("Inclinaison du corps vers l'avant pendant la ruée (degrés).")]
    public float penche = 14f;
    public float intervalleCharge = 4f;

    [Header("Squelettes (désintégration, sortie de terre)")]
    public GameObject squeletteDesintegration;
    public GameObject squeletteSortie;
    public float vitesseClipSortie = 1.5f;
    public float profondeurSortie = 1.9f;
    public float intervalleSquelettes = 6f;

    // Instants clés mesurés au démarrage (s dans le clip) : exposés pour la doc et les captures.
    public float TempsImpactSaut { get; private set; }
    public float TempsDecollage { get; private set; }
    public float TempsSommet { get; private set; }
    public float TempsAtterrissage { get; private set; }
    public float TempsCoupBouclier { get; private set; }
    public float TempsTir { get; private set; }
    public float TempsPoussee { get; private set; }
    public float TempsSoin { get; private set; }
    public float TempsCri { get; private set; }

    private const float Fondu = 0.15f;
    private readonly Dictionary<GameObject, Acteur> acteurs = new Dictionary<GameObject, Acteur>();
    private readonly List<GameObject> projectiles = new List<GameObject>();
    private Vector3 voyageurOrigine;
    private Vector3 chevalierOrigine, chevalierAxe, barbareOrigine;
    private Quaternion barbareRotation;
    private bool versB = true;

    // ---------------------------------------------------------------- démarrage

    private void Start()
    {
        MesurerInstants();
        if (voyageur != null) voyageurOrigine = voyageur.transform.parent != null ? voyageur.transform.parent.position : Vector3.zero;
        if (chevalier != null) { chevalierOrigine = chevalier.transform.position; chevalierAxe = chevalier.transform.forward; }
        if (barbare != null) { barbareOrigine = barbare.transform.position; barbareRotation = barbare.transform.rotation; }
        foreach (GameObject s in new[] { squeletteDesintegration, squeletteSortie, cibleCone, cibleBoule, cibleMissile })
            if (s != null && clipSortie != null)
            {
                Animator a = s.GetComponentInChildren<Animator>();
                if (a != null) a.enabled = false;
                clipSortie.SampleAnimation(s, clipSortie.length);
            }
        ToutJouer();
    }

    private void OnDestroy()
    {
        foreach (Acteur a in acteurs.Values) a.Detruire();
        acteurs.Clear();
    }

    private Acteur A(GameObject go)
    {
        if (go == null) return null;
        Acteur a;
        if (!acteurs.TryGetValue(go, out a))
        {
            a = new Acteur(go);
            acteurs[go] = a;
        }
        return a;
    }

    // Relance tous les postes au même instant.
    public void ToutJouer()
    {
        StopAllCoroutines();
        foreach (GameObject p in projectiles) if (p != null) Destroy(p);
        projectiles.Clear();
        // La ceinture de la relique n'est plus pulsée à la main : elle réagit aux événements (Nyxessa).
        Boucle(intervalleMissile, () => StartCoroutine(Missile()));
        Boucle(intervallePortail, () => StartCoroutine(CyclePortail()));
        Boucle(intervalleBouclier, () => StartCoroutine(CycleBouclier()));
        Boucle(intervalleSquelettes, () => StartCoroutine(Vaporisation()));
        Boucle(intervalleSquelettes, () => StartCoroutine(Sortie()));
        StartCoroutine(Passage());
        StartCoroutine(PosteCone());
        StartCoroutine(PosteBoule());
        StartCoroutine(PosteAura());
        StartCoroutine(PosteRugissement());
        StartCoroutine(PosteSaut());
        StartCoroutine(PosteCharge());
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

    // ---------------------------------------------------------------- instants clés

    // Échantillonne les clips une fois (animator coupé) pour trouver l'instant où l'effet doit partir.
    private void MesurerInstants()
    {
        TempsImpactSaut = ImpactSaut(barbare, clipSautFrappe, 0.8f);
        TempsCoupBouclier = Maximum(chevalier, clipCoupBouclier, 0.47f, go => Local(go, Os(go, "handslot.l")).z);
        TempsTir = Maximum(mageBoule, clipTir, 0.3f, go => Local(go, PointArme(go, "staff", new Vector3(0f, 1.2f, 0f))).z);
        TempsPoussee = Maximum(mageCone, clipIncantation, 1.6f, go => Local(go, PointArme(go, "staff", new Vector3(0f, 1.2f, 0f))).z);
        TempsSoin = PremierAtteint(paladin, clipSoin, 0.6f, go => Local(go, Os(go, "handslot.r")).y, 0.98f);
        TempsCri = PremierAtteint(viking, clipCri, 1.6f, go => -Local(go, Os(go, "head")).z, 0.98f);
    }

    private delegate float Mesure(GameObject go);

    private static Vector3 Local(GameObject go, Vector3 monde) { return go.transform.InverseTransformPoint(monde); }

    private static Vector3 Os(GameObject go, string nom)
    {
        Transform t = MannequinEquip.Trouver(go.transform, nom);
        return t != null ? t.position : go.transform.position;
    }

    private static Vector3 PointArme(GameObject go, string arme, Vector3 local)
    {
        Transform t = MannequinEquip.Trouver(go.transform, arme);
        return t != null ? t.TransformPoint(local) : go.transform.position;
    }

    // Échantillonne `clip` sur `go` à 120 images/s et rend l'instant où `mesure` est maximale.
    private float Maximum(GameObject go, AnimationClip clip, float defaut, Mesure mesure)
    {
        if (go == null || clip == null) return defaut;
        float best = float.NegativeInfinity, tBest = defaut;
        Echantillonner(go, clip, (t) => { float v = mesure(go); if (v > best) { best = v; tBest = t; } });
        return tBest;
    }

    // Premier instant où `mesure` atteint `fraction` de son amplitude (min → max) sur le clip.
    private float PremierAtteint(GameObject go, AnimationClip clip, float defaut, Mesure mesure, float fraction)
    {
        if (go == null || clip == null) return defaut;
        List<float> ts = new List<float>(), vs = new List<float>();
        Echantillonner(go, clip, (t) => { ts.Add(t); vs.Add(mesure(go)); });
        float min = Mathf.Min(vs.ToArray()), max = Mathf.Max(vs.ToArray());
        float seuil = min + (max - min) * fraction;
        for (int i = 0; i < vs.Count; i++) if (vs[i] >= seuil) return ts[i];
        return defaut;
    }

    // Saut percutant : sommet des hanches, puis premier instant où la lame (fer −X de axe_2handed, à 0,92 m sur le
    // manche) atteint le sol (y = 0 dans le repère du personnage) après le sommet : c'est le contact.
    private float ImpactSaut(GameObject go, AnimationClip clip, float defaut)
    {
        if (go == null || clip == null) return defaut;
        List<float> ts = new List<float>(), hanches = new List<float>(), lame = new List<float>();
        Echantillonner(go, clip, (t) =>
        {
            ts.Add(t);
            hanches.Add(Local(go, Os(go, "hips")).y);
            lame.Add(Local(go, PointArme(go, "axe_2handed", new Vector3(-0.62f, 0.92f, 0f))).y);
        });
        int sommet = 0;
        for (int i = 1; i < hanches.Count; i++) if (hanches[i] > hanches[sommet]) sommet = i;
        // Phase aérienne : décollage = les hanches remontent de 15 % de l'amplitude au-dessus du point le plus bas de
        // l'appel (accroupi) ; atterrissage = après le sommet, retour à la hauteur de repos (début du clip) + 3 cm.
        int accroupi = 0;
        for (int i = 1; i < sommet; i++) if (hanches[i] < hanches[accroupi]) accroupi = i;
        float seuilDecollage = hanches[accroupi] + 0.15f * (hanches[sommet] - hanches[accroupi]);
        TempsDecollage = ts[accroupi];
        for (int i = accroupi; i < sommet; i++) if (hanches[i] >= seuilDecollage) { TempsDecollage = ts[i]; break; }
        TempsSommet = ts[sommet];
        TempsAtterrissage = ts[ts.Count - 1];
        for (int i = sommet; i < hanches.Count; i++) if (hanches[i] <= hanches[0] + 0.03f) { TempsAtterrissage = ts[i]; break; }
        float bas = float.PositiveInfinity;
        for (int i = sommet; i < lame.Count; i++) bas = Mathf.Min(bas, lame[i]);
        // La lame s'enfonce sous le sol dans le clip : le contact est le passage à y = 0 (sol) ; sinon 3 cm au-dessus du point bas.
        float seuil = bas < 0.02f ? 0.02f : bas + 0.03f;
        for (int i = sommet; i < lame.Count; i++) if (lame[i] <= seuil) return ts[i];
        return defaut;
    }

    private static void Echantillonner(GameObject go, AnimationClip clip, System.Action<float> pas)
    {
        Animator animator = go.GetComponentInChildren<Animator>();
        bool actif = animator != null && animator.enabled;
        if (animator != null) animator.enabled = false;
        for (float t = 0f; t <= clip.length + 1e-4f; t += 1f / 120f)
        {
            clip.SampleAnimation(go, t);
            pas(t);
        }
        if (animator != null) animator.enabled = actif;
    }

    // ---------------------------------------------------------------- postes avec personnage

    // Saut percutant : bond vers l'avant. Le clip (Melee_1H_Attack_Jump_Chop, hache 2H tenue en main droite) est « in
    // place » (aucune courbe sur la racine) : la racine est translatée par script pendant la phase aérienne mesurée
    // (décollage → atterrissage), `distanceSaut` à vitesse horizontale constante, plus un arc vertical de `hauteurSaut`
    // (4k(1−k)) ajouté au saut du clip. L'atterrissage précède le coup au sol ; l'onde part du point où la lame touche
    // le sol, devant le personnage. Retour discret au point de départ au début de chaque boucle.
    private IEnumerator PosteSaut()
    {
        Acteur a = A(barbare);
        if (a == null || clipSautFrappe == null) yield break;
        Transform t0 = barbare.transform;
        Vector3 origine = barbareOrigine;
        Quaternion rotation = barbareRotation;
        Vector3 axe = rotation * Vector3.forward;
        float vol = Mathf.Max(0.05f, TempsAtterrissage - TempsDecollage);
        while (true)
        {
            t0.SetPositionAndRotation(origine, rotation);
            yield return Tenir(a, clipIdle2H, 1f);
            bool onde = false;
            for (float t = 0f; t < clipSautFrappe.length; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01((t - TempsDecollage) / vol);
                t0.position = origine + axe * distanceSaut * k + Vector3.up * hauteurSaut * 4f * k * (1f - k);
                a.Poser(clipSautFrappe, t, clipIdle2H, Time.time, 1f - Mathf.Clamp01(t / Fondu));
                if (!onde && t >= TempsImpactSaut)
                {
                    onde = true;
                    if (ondeSaut != null)
                    {
                        Vector3 p = PointArme(barbare, "axe_2handed", new Vector3(-0.62f, 0.92f, 0f));
                        ondeSaut.transform.position = new Vector3(p.x, origine.y, p.z);
                        ondeSaut.Jouer();
                    }
                }
                yield return null;
            }
            t0.position = origine + axe * distanceSaut;
            yield return Fondre(a, clipSautFrappe, clipIdle2H, Mathf.Max(0.5f, intervalleSaut - 1f - clipSautFrappe.length));
        }
    }

    // Rugissement : Skeletons_Taunt_Longer (accroupi, bond, puis tête rejetée en arrière) ; le crâne s'ouvre au
    // moment où la tête part en arrière (Jouer() 0,32 s avant : pop + ouverture de la gueule).
    private IEnumerator PosteRugissement()
    {
        Acteur a = A(viking);
        if (a == null || clipCri == null) yield break;
        while (true)
        {
            yield return Tenir(a, clipIdle, 0.8f);
            bool lance = false;
            float depart = Mathf.Max(0f, TempsCri - 0.32f);
            for (float t = 0f; t < clipCri.length; t += Time.deltaTime)
            {
                a.Poser(clipCri, t, clipIdle, Time.time, 1f - Mathf.Clamp01(t / Fondu));
                if (!lance && t >= depart) { lance = true; if (rugissement != null) rugissement.Jouer(); }
                yield return null;
            }
            yield return Fondre(a, clipCri, clipIdle, Mathf.Max(0.5f, intervalleRugissement - 0.8f - clipCri.length));
        }
    }

    // Charge bélier : anticipation (corps penché, garde levée), ruée de `distanceCharge` m en `dureeRuee` s (racine
    // translatée par script : départ franc puis léger freinage), jambes en Running_A accéléré, haut du corps en garde
    // puis coup de bouclier (Melee_Block_Attack) calé pour que le coup porte à l'arrivée. ChargeBelier (bulle en tête
    // de bélier + traînée) part avec l'élan ; l'éclatement et l'onde partent à l'arrivée. Retour discret au départ.
    private IEnumerator PosteCharge()
    {
        Acteur a = A(chevalier);
        if (a == null || clipCourse == null || clipCoupBouclier == null) yield break;
        Transform t0 = chevalier.transform;
        Quaternion droit = Quaternion.LookRotation(chevalierAxe);
        float debutCoup = Mathf.Max(0f, dureeRuee - TempsCoupBouclier);
        while (true)
        {
            t0.SetPositionAndRotation(chevalierOrigine, droit);
            yield return Tenir(a, clipIdle, 0.8f);
            if (charge != null) charge.Jouer(t0, chevalierAxe, distanceCharge);
            for (float t = 0f; t < anticipation; t += Time.deltaTime)
            {
                float k = t / anticipation;
                t0.rotation = droit * Quaternion.Euler(penche * k, 0f, 0f);
                a.Poser(clipCourse, 0f, clipIdle, Time.time, 1f - k, clipGarde, 0.3f, Mathf.Clamp01(k * 2f));
                yield return null;
            }
            for (float t = 0f; t < dureeRuee; t += Time.deltaTime)
            {
                float s = Mathf.Clamp01(t / dureeRuee);
                float avance = 0.6f * (1f - (1f - s) * (1f - s)) + 0.4f * s;   // départ franc, léger freinage
                t0.SetPositionAndRotation(chevalierOrigine + chevalierAxe * distanceCharge * avance, droit * Quaternion.Euler(penche, 0f, 0f));
                if (t < debutCoup)
                    a.Poser(clipCourse, t * 1.8f, null, 0f, 0f, clipGarde, 0.3f, 1f);
                else
                    a.Poser(clipCourse, t * 1.8f, null, 0f, 0f, clipCoupBouclier, t - debutCoup, 1f);
                yield return null;
            }
            // Arrivée : le porteur est à `distanceCharge`, ChargeBelier éclate et lance l'onde (LateUpdate de cette image).
            t0.position = chevalierOrigine + chevalierAxe * distanceCharge;
            for (float t = TempsCoupBouclier; t < clipCoupBouclier.length; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01((t - TempsCoupBouclier) / 0.2f);
                t0.rotation = droit * Quaternion.Euler(penche * (1f - k), 0f, 0f);
                a.Poser(clipCoupBouclier, t, clipCourse, dureeRuee * 1.8f, 1f - Mathf.Clamp01((t - TempsCoupBouclier) / 0.1f));
                yield return null;
            }
            t0.rotation = droit;
            yield return Fondre(a, clipCoupBouclier, clipIdle, Mathf.Max(0.5f, intervalleCharge - 0.8f - anticipation - dureeRuee - (clipCoupBouclier.length - TempsCoupBouclier)));
        }
    }

    // Aura de soin : le paladin lève l'épée (Ranged_Magic_Raise) ; l'aura part sous l'allié quand l'épée est en haut.
    private IEnumerator PosteAura()
    {
        Acteur a = A(paladin), b = A(allie);
        if (a == null || clipSoin == null) yield break;
        if (b != null) StartCoroutine(Idle(b, 0.4f));
        while (true)
        {
            yield return Tenir(a, clipIdle, 0.5f);
            bool soin = false;
            for (float t = 0f; t < clipSoin.length; t += Time.deltaTime)
            {
                a.Poser(clipSoin, t, clipIdle, Time.time, 1f - Mathf.Clamp01(t / Fondu));
                if (!soin && t >= TempsSoin) { soin = true; if (aura != null) aura.Jouer(); }
                yield return null;
            }
            yield return Fondre(a, clipSoin, clipIdle, Mathf.Max(0.5f, intervalleAura - 0.5f - clipSoin.length));
        }
    }

    // Boule de feu : Ranged_Magic_Shoot ; la boule part de la pointe du bâton quand il est le plus en avant, vole vers
    // la poitrine de la cible (18 m/s, vitesse de Relic) et explose (LowPolyBlast.Fire + fumée, SkillEffects.Burst).
    private IEnumerator PosteBoule()
    {
        Acteur a = A(mageBoule);
        if (a == null || clipTir == null) yield break;
        while (true)
        {
            yield return Tenir(a, clipIdle, 0.8f);
            bool tir = false;
            for (float t = 0f; t < clipTir.length; t += Time.deltaTime)
            {
                a.Poser(clipTir, t, clipIdle, Time.time, 1f - Mathf.Clamp01(t / Fondu));
                if (!tir && t >= TempsTir)
                {
                    tir = true;
                    StartCoroutine(VolBoule(PointArme(mageBoule, "staff", new Vector3(0f, 1.2f, 0f)), Poitrine(cibleBoule)));
                }
                yield return null;
            }
            yield return Fondre(a, clipTir, clipIdle, Mathf.Max(0.5f, intervalleBoule - 0.8f - clipTir.length));
        }
    }

    private IEnumerator VolBoule(Vector3 depart, Vector3 cible)
    {
        if (feu == null) yield break;
        GameObject projectile = new GameObject("VfxBench_Boule");
        projectiles.Add(projectile);
        projectile.transform.position = depart;
        projectile.transform.rotation = Quaternion.LookRotation(cible - depart);
        FireballVisual.Attach(projectile.transform, feu);
        float duree = Vector3.Distance(depart, cible) / 18f;
        for (float t = 0f; t < duree && projectile != null; t += Time.deltaTime)
        {
            projectile.transform.position = Vector3.Lerp(depart, cible, t / duree);
            yield return null;
        }
        if (projectile == null) yield break;
        Destroy(projectile);
        LowPolyBlast.Fire(cible, 2.5f, feu);
        if (fumee != null)
        {
            FireEffect smoke = FireEffect.Create(null, cible, 1f, 0.8f, flamme, fumee, false, true, false);
            yield return new WaitForSeconds(0.4f);
            if (smoke != null) smoke.SetEmitting(false);
            yield return new WaitForSeconds(2.6f);
            if (smoke != null) Destroy(smoke.gameObject);
        }
    }

    // Cône de flammes : Ranged_Magic_Spellcasting_Long jusqu'à la poussée du bâton, pose maintenue pendant `dureeCone`
    // (léger balancement), fin du clip. Le cône suit la pointe du bâton et vise la poitrine de la cible ; les
    // flammèches du burn prennent sur la cible 0,3 s après le début du cône et s'éteignent 1,5 s après sa fin.
    private IEnumerator PosteCone()
    {
        Acteur a = A(mageCone);
        if (a == null || clipIncantation == null) yield break;
        Particules(cone, false, true);
        Particules(burn, false, true);
        while (true)
        {
            yield return Tenir(a, clipIdle, 0.6f);
            for (float t = 0f; t < TempsPoussee; t += Time.deltaTime)
            {
                a.Poser(clipIncantation, t, clipIdle, Time.time, 1f - Mathf.Clamp01(t / Fondu));
                PlacerCone();
                yield return null;
            }
            Particules(cone, true, false);
            bool brule = false;
            for (float t = 0f; t < dureeCone; t += Time.deltaTime)
            {
                a.Poser(clipIncantation, TempsPoussee + 0.04f * Mathf.Sin(t * Mathf.PI * 3f));
                PlacerCone();
                if (!brule && t >= 0.3f) { brule = true; Particules(burn, true, false); }
                yield return null;
            }
            Particules(cone, false, false);
            float fin = 0f;
            for (float t = TempsPoussee; t < clipIncantation.length; t += Time.deltaTime)
            {
                a.Poser(clipIncantation, t);
                PlacerCone();
                fin += Time.deltaTime;
                if (fin >= 1.5f) Particules(burn, false, false);
                yield return null;
            }
            float reste = Mathf.Max(0.5f, intervalleCone - 0.6f - clipIncantation.length - dureeCone);
            for (float t = 0f; t < reste; t += Time.deltaTime)
            {
                a.Poser(clipIdle, Time.time, clipIncantation, clipIncantation.length, 1f - Mathf.Clamp01(t / Fondu));
                fin += Time.deltaTime;
                if (fin >= 1.5f) Particules(burn, false, false);
                yield return null;
            }
        }
    }

    private void PlacerCone()
    {
        if (cone == null || mageCone == null) return;
        Vector3 pointe = PointArme(mageCone, "staff", new Vector3(0f, 1.2f, 0f));
        Vector3 cible = cibleCone != null ? Poitrine(cibleCone) : pointe + mageCone.transform.forward;
        cone.transform.SetPositionAndRotation(pointe, Quaternion.LookRotation(cible - pointe));
    }

    // Téléportation : le voyageur marche vers le portail, se désintègre en gemmes qui filent au centre
    // (PortalTransit.Depart 1,1 s + Ripple), réapparaît de l'autre côté (Arrive 1,0 s + Ripple). Relic : PlayerZone.Transit.
    private IEnumerator Passage()
    {
        Acteur a = A(voyageur);
        if (portailTransit == null || voyageur == null || gemmes == null || a == null) yield break;
        const float depart = 1.1f, arrivee = 1.0f, marche = 1.2f;
        Transform t0 = voyageur.transform;
        // Repartir d'un état connu (ToutJouer peut interrompre un passage) : visible, au point A, face au portail.
        Visible(t0, true);
        t0.position = voyageurOrigine + pointA;
        versB = true;
        while (true)
        {
            Vector3 centre = portailTransit.Center;
            Vector3 versPortail = new Vector3(centre.x - t0.position.x, 0f, centre.z - t0.position.z).normalized;
            yield return Tenir(a, clipIdle, 0.6f);
            Quaternion de = t0.rotation, vers = Quaternion.LookRotation(versPortail);
            for (float t = 0f; t < marche; t += Time.deltaTime)
            {
                t0.rotation = Quaternion.Slerp(de, vers, Mathf.Clamp01(t / 0.3f));
                t0.position += versPortail * 1.2f * Time.deltaTime;
                a.Poser(clipMarche, t, clipIdle, Time.time, 1f - Mathf.Clamp01(t / Fondu));
                yield return null;
            }
            Visible(t0, false);
            PortalTransit.Depart(Corps(t0.position), portailTransit, gemmes, depart);   // goutte d'entrée + réaction de la relique
            yield return new WaitForSeconds(depart);
            Vector3 cible = voyageurOrigine + (versB ? pointB : pointA);
            t0.position = cible;
            t0.rotation = Quaternion.LookRotation(new Vector3(cible.x - centre.x, 0f, cible.z - centre.z).normalized);
            a.Poser(clipIdle, 0f);
            yield return null;
            PortalTransit.Arrive(Corps(cible), portailTransit, gemmes, arrivee);   // goutte de sortie + réaction de la relique
            yield return new WaitForSeconds(arrivee);
            Visible(t0, true);
            versB = !versB;
            yield return Tenir(a, clipIdle, Mathf.Max(0.5f, intervalleTeleportation - 0.6f - marche - depart - arrivee));
        }
    }

    // Volume du joueur de Relic (0,8 x 1,9 x 0,8), le pivot du personnage étant aux pieds.
    private static Bounds Corps(Vector3 pieds)
    {
        return new Bounds(pieds + Vector3.up * 0.95f, new Vector3(0.8f, 1.9f, 0.8f));
    }

    private IEnumerator Idle(Acteur a, float phase)
    {
        while (true)
        {
            a.Poser(clipIdle, Time.time + phase);
            yield return null;
        }
    }

    private IEnumerator Tenir(Acteur a, AnimationClip clip, float duree)
    {
        for (float t = 0f; t < duree; t += Time.deltaTime)
        {
            a.Poser(clip, Time.time);
            yield return null;
        }
    }

    // Fondu de la fin de `de` vers le clip bouclé `vers`, puis `vers` seul jusqu'à `duree`.
    private IEnumerator Fondre(Acteur a, AnimationClip de, AnimationClip vers, float duree)
    {
        for (float t = 0f; t < duree; t += Time.deltaTime)
        {
            a.Poser(vers, Time.time, de, de.length, 1f - Mathf.Clamp01(t / Fondu));
            yield return null;
        }
    }

    private static Vector3 Poitrine(GameObject cible)
    {
        return cible != null ? cible.transform.position + Vector3.up * 0.7f : Vector3.zero;
    }

    // ---------------------------------------------------------------- postes sans personnage

    // Missile magique tiré par la relique (RelicTurret.FireRpc + SkullMissileVisual) : éclat au cristal, crâne à
    // 12 m/s vers la poitrine du squelette, Shatter à l'arrivée.
    private IEnumerator Missile()
    {
        if (cristal == null || formeCrane == null || gemmes == null || cibleMissile == null) yield break;
        Vector3 cible = Poitrine(cibleMissile);
        Vector3 dir = (cible - cristal.position).normalized;
        Vector3 depart = cristal.position + dir * 1.1f;
        GameObject projectile = new GameObject("VfxBench_Missile");
        projectiles.Add(projectile);
        projectile.transform.position = depart;
        projectile.transform.rotation = Quaternion.LookRotation(dir);
        // Tir par la relique : éclat au départ, réaction de Nyxessa (pulsation et recul du cristal), crâne à l'échelle 1,5.
        SkullMissileVisual crane = Nyxessa.Instance != null
            ? Nyxessa.Instance.TirerMissile(projectile.transform, formeCrane, gemmes, echelleMissile)
            : SkullMissileVisual.Attach(projectile.transform, formeCrane, Vector3.zero, 0.55f, gemmes, echelleMissile);
        float duree = Vector3.Distance(depart, cible) / 12f;
        for (float t = 0f; t < duree && projectile != null; t += Time.deltaTime)
        {
            projectile.transform.position = Vector3.Lerp(depart, cible, t / duree);
            yield return null;
        }
        if (crane != null) crane.Shatter();
        if (projectile != null) Destroy(projectile, 0.1f);
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
        for (float t = 0f; t < joue; t += Time.deltaTime)
        {
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

    // Effets bouclés (cône, burn) : Play() / Stop() sur tous les ParticleSystem de l'objet.
    private static void Particules(GameObject racine, bool jouer, bool vider)
    {
        if (racine == null) return;
        foreach (ParticleSystem ps in racine.GetComponentsInChildren<ParticleSystem>())
        {
            if (jouer) ps.Play(false);
            else ps.Stop(false, vider ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private static void Visible(Transform t, bool visible)
    {
        foreach (Renderer r in t.GetComponentsInChildren<Renderer>()) r.enabled = visible;
    }

    // ---------------------------------------------------------------- captures (outil)

    // Relance tous les postes (ToutJouer), attend `delai` secondes et enregistre une image 1600 x 1000 en PNG. Caméra
    // principale si `fov` <= 0, sinon caméra temporaire en `position` visant `visee`.
    public void Capturer(string chemin, float delai, Vector3 position, Vector3 visee, float fov)
    {
        ToutJouer();
        StartCoroutine(CaptureRoutine(chemin, delai, position, visee, fov));
    }

    [Tooltip("Captures : le temps est ralenti pendant l'attente (les à-coups de l'éditeur décalent moins l'instant).")]
    public float ralentiCapture = 0.25f;

    private IEnumerator CaptureRoutine(string chemin, float delai, Vector3 position, Vector3 visee, float fov)
    {
        float echelleTemps = Time.timeScale;
        Time.timeScale = Mathf.Clamp(ralentiCapture, 0.05f, 1f);
        yield return new WaitForSeconds(delai);
        Time.timeScale = echelleTemps;
        yield return new WaitForEndOfFrame();
        Camera cam = Camera.main;
        GameObject temporaire = null;
        if (fov > 0f)
        {
            temporaire = new GameObject("VfxBench_Capture");
            cam = temporaire.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.transform.position = position;
            cam.transform.LookAt(visee);
        }
        RenderTexture rt = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        RenderTexture ancien = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture actif = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
        tex.Apply();
        RenderTexture.active = actif;
        cam.targetTexture = ancien;
        System.IO.File.WriteAllBytes(chemin, tex.EncodeToPNG());
        Destroy(tex);
        rt.Release();
        Destroy(rt);
        if (temporaire != null) Destroy(temporaire);
    }

    // ---------------------------------------------------------------- pose des personnages

    // Pose un personnage par un PlayableGraph en mise à jour manuelle : couche de base (jusqu'à deux clips mélangés)
    // et couche « haut du corps » (os `chest` et descendants) pour la garde pendant la course. Les clips bouclés
    // (isLooping) sont repliés sur leur durée, les autres bornés.
    private class Acteur
    {
        private readonly GameObject go;
        private PlayableGraph graph;
        private AnimationLayerMixerPlayable couches;
        private AnimationMixerPlayable basse, haute;
        private readonly Dictionary<AnimationClip, int> indexBas = new Dictionary<AnimationClip, int>();
        private readonly Dictionary<AnimationClip, int> indexHaut = new Dictionary<AnimationClip, int>();

        public Acteur(GameObject go)
        {
            this.go = go;
            Animator animator = go.GetComponentInChildren<Animator>();
            if (animator == null) animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            animator.enabled = true;
            graph = PlayableGraph.Create("VfxBench_" + go.name);
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            couches = AnimationLayerMixerPlayable.Create(graph, 2);
            basse = AnimationMixerPlayable.Create(graph, 0);
            haute = AnimationMixerPlayable.Create(graph, 0);
            graph.Connect(basse, 0, couches, 0);
            graph.Connect(haute, 0, couches, 1);
            couches.SetInputWeight(0, 1f);
            couches.SetInputWeight(1, 0f);
            AvatarMask masque = new AvatarMask();
            masque.AddTransformPath(animator.transform, true);
            for (int i = 0; i < masque.transformCount; i++)
                masque.SetTransformActive(i, masque.GetTransformPath(i).Contains("/chest"));
            couches.SetLayerMaskFromAvatarMask(1, masque);
            AnimationPlayableOutput sortie = AnimationPlayableOutput.Create(graph, "Pose", animator);
            sortie.SetSourcePlayable(couches);
            graph.Play();
        }

        private static int Index(PlayableGraph g, AnimationMixerPlayable mix, Dictionary<AnimationClip, int> index, AnimationClip clip)
        {
            int i;
            if (index.TryGetValue(clip, out i)) return i;
            i = mix.GetInputCount();
            mix.SetInputCount(i + 1);
            AnimationClipPlayable p = AnimationClipPlayable.Create(g, clip);
            p.SetApplyFootIK(false);
            p.Pause();
            g.Connect(p, 0, mix, i);
            index[clip] = i;
            return i;
        }

        private static double Temps(AnimationClip clip, float t)
        {
            if (clip.isLooping && clip.length > 0f) return Mathf.Repeat(t, clip.length);
            return Mathf.Clamp(t, 0f, clip.length);
        }

        // `a` au temps `ta`, mélangé avec `b` (poids `wb`) ; `haut` sur le haut du corps (poids `wh`).
        public void Poser(AnimationClip a, float ta, AnimationClip b = null, float tb = 0f, float wb = 0f,
            AnimationClip haut = null, float th = 0f, float wh = 0f)
        {
            if (!graph.IsValid() || a == null) return;
            if (b == null || b == a) wb = 0f;
            int ia = Index(graph, basse, indexBas, a);
            int ib = b != null && wb > 0f ? Index(graph, basse, indexBas, b) : -1;
            for (int i = 0; i < basse.GetInputCount(); i++) basse.SetInputWeight(i, 0f);
            basse.SetInputWeight(ia, 1f - wb);
            basse.GetInput(ia).SetTime(Temps(a, ta));
            if (ib >= 0)
            {
                basse.SetInputWeight(ib, wb);
                basse.GetInput(ib).SetTime(Temps(b, tb));
            }
            if (haut != null && wh > 0f)
            {
                int ih = Index(graph, haute, indexHaut, haut);
                for (int i = 0; i < haute.GetInputCount(); i++) haute.SetInputWeight(i, i == ih ? 1f : 0f);
                haute.GetInput(ih).SetTime(Temps(haut, th));
                couches.SetInputWeight(1, wh);
            }
            else
                couches.SetInputWeight(1, 0f);
            graph.Evaluate();
        }

        public void Detruire()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
