using UnityEngine;

// Réglages communs des lanternes (LanterneLumiere) : couleur par défaut de la lumière et des vitres, scintillement.
// Un seul asset pour tout le village (Assets/VFX/_Ambiance/LanternesReglages.asset) : le modifier change toutes les
// lanternes d'un coup, y compris en jeu (lu à chaque image). Une lanterne peut garder sa propre couleur
// (LanterneLumiere.couleurPropre).
[CreateAssetMenu(fileName = "LanternesReglages", menuName = "Deathless/Réglages des lanternes")]
public class LanternesReglages : ScriptableObject
{
    [Tooltip("Couleur par défaut de la lumière et de l'émission des vitres.")]
    public Color couleur = new Color(1f, 0.64f, 0.34f);
    [Tooltip("Amplitude du scintillement de l'intensité (0,1 = ±10 %).")]
    [Range(0f, 0.3f)] public float amplitude = 0.1f;
    [Tooltip("Vitesse du scintillement (1 = deux ondulations lentes, de 0,45 et 1,1 Hz).")]
    [Range(0.1f, 3f)] public float vitesse = 1f;
    [Tooltip("Éclat des vitres (émission HDR) à pleine intensité, multiplié par la couleur.")]
    [Min(0f)] public float eclatVitres = 1.1f;   // au-delà de ~1,3 les vitres saturent vers le blanc et perdent la teinte
}
