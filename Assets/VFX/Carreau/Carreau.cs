using System.Collections;
using UnityEngine;

// Carreau d'arbalète de l'assassin (25/09/2026, corrigé : « les flèches ne sont pas magiques ») : un carreau (modèle
// arrow_bow réduit, tel quel) part tout droit, très vite, sans gemme ni lueur, avec une traînée d'air courte, fine et claire (TraineeAir, non émissive) ; à l'impact,
// `impact(point, direction)` est appelé ; `cible` est le point où la pointe se fiche (surface de la cible). Règle : critique uniquement dans la tête (les passifs de l'assassin,
// furtivité et dos, ne s'appliquent pas aux carreaux). Gros temps de recharge : `tempsRecharge` (valeur à confirmer).
// API : Tirer(depart, cible, impact) → faux si l'arbalète recharge ; Pret ; Restant (s).
public class Carreau : MonoBehaviour
{
    [SerializeField] private GameObject modele;
    [Tooltip("Matériau à couleurs par sommet (PortalVoxel) de la traînée d'air.")]
    [SerializeField] private Material materiauTrainee;
    [Tooltip("Traînée d'air, plus courte que celle des flèches.")]
    [SerializeField] private float longueurTrainee = 1.2f;
    [SerializeField] private float echelleModele = 0.85f;
    [SerializeField] private float vitesse = 45f;
    [Tooltip("Temps de recharge de l'arbalète (s) : gros, l'arbalète ne remplace pas la dague. Valeur à confirmer.")]
    public float tempsRecharge = 8f;
    [Tooltip("Le carreau reste fiché dans la cible `dureePlante` s, enfoncé de `enfoncement` m.")]
    [SerializeField] private float dureePlante = 2f;
    [SerializeField] private float enfoncement = 0.1f;

    private float pret = -100f;

    public bool Pret { get { return Time.time >= pret; } }
    public float Restant { get { return Mathf.Max(0f, pret - Time.time); } }

    // `ignorerRecharge` : pour le banc (enchaîner les tirs).
    public bool Tirer(Vector3 depart, Vector3 cible, System.Action<Vector3, Vector3> impact, bool ignorerRecharge = false)
    {
        if (!ignorerRecharge && !Pret) return false;
        pret = Time.time + tempsRecharge;
        StartCoroutine(Vol(depart, cible, impact));
        return true;
    }

    private IEnumerator Vol(Vector3 depart, Vector3 cible, System.Action<Vector3, Vector3> impact)
    {
        GameObject c = modele != null ? Instantiate(modele) : new GameObject("Carreau");
        c.name = "Carreau_Tir";
        c.transform.localScale = Vector3.one * echelleModele;
        Vector3 dir = (cible - depart).normalized;
        c.transform.rotation = Quaternion.LookRotation(dir);
        // La pointe suit la trajectoire : le modèle a son pivot au milieu, on le recule d'une demi-longueur.
        MeshFilter mf = c.GetComponentInChildren<MeshFilter>();
        float demi = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.extents.z * echelleModele : 0f;
        TraineeAir trainee = TraineeAir.Attacher(c.transform, new Vector3(0f, 0f, -demi / Mathf.Max(0.001f, echelleModele)), materiauTrainee,
            longueurTrainee, 0.009f, 0.16f);
        float duree = Vector3.Distance(depart, cible) / vitesse;
        for (float t = 0f; t < duree; t += Time.deltaTime)
        {
            c.transform.position = Vector3.Lerp(depart, cible, t / duree) - dir * demi;
            yield return null;
        }
        c.transform.position = cible + dir * (enfoncement - demi);   // fiché dans la cible, pointe enfoncée
        if (trainee != null) trainee.Detacher();
        if (impact != null) impact(cible, dir);
        Destroy(c, dureePlante);
    }
}
