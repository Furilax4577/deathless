using UnityEngine;

// Forme en « soupe de gemmes » (étape 68) : des points répartis sur la surface d'un modèle, avec la luminosité de sa
// texture à cet endroit. Calculée une fois dans l'éditeur (GemShapeBaker, menu Relic > VFX), parce qu'un modèle
// importé sans lecture/écriture n'est pas lisible dans un build. Sert au crâne du nécromancien (SkullMissileVisual).
[CreateAssetMenu(menuName = "Relic/Forme en gemmes", fileName = "GemShape")]
public class GemShape : ScriptableObject
{
    [Tooltip("Points sur la surface, centrés, le plus grand côté ramené à 1.")]
    public Vector3[] points = new Vector3[0];
    [Tooltip("Luminosité de la texture du modèle en chaque point (0 sombre, 1 clair).")]
    public float[] brightness = new float[0];
    [Tooltip("Normale extérieure de la surface en chaque point : les gemmes s'y couchent à plat, comme des écailles.")]
    public Vector3[] normals = new Vector3[0];
}
