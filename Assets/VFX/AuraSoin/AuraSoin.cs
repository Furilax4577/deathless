using UnityEngine;

// Aura de soin : déclencheur runtime (écrit pour Deathless le 25/09/2026 à partir d'AuraSoinDemo du bac à sable
// sandbox-vfx, sans la boucle ni IEffetDemo). Jouer() au soin : relance ensemble les croix (ParticleSystem non bouclés,
// URP Lit menthe) et les paillettes en gemmes (AuraGemmes, langage gemmes de Relic).
public class AuraSoin : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] croix;
    [SerializeField] private AuraGemmes gemmes;

    private VfxLumiere lumiere;

    public void Jouer()
    {
        // Lumière commune des effets (thème Soin, classe moyenne), le temps des croix.
        if (lumiere == null) lumiere = VfxLumiere.Creer(transform, new Vector3(0f, 1.6f, 0f), VfxTheme.Soin, VfxTailleLumiere.Moyenne, -1f);
        lumiere.Allumer(0.6f);
        if (croix != null)
            foreach (ParticleSystem ps in croix)
                if (ps != null) { ps.Clear(); ps.Play(false); }
        if (gemmes != null)
            gemmes.Jouer();
    }
}
