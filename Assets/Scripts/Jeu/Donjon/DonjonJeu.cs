using Deathless.Donjon;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Le donjon en partie (wiki : deroule.md, Le donjon ; portail.md). Posé dans la scène du village, loin de lui
    /// (Origine), avec le générateur (Deathless > Donjon > Placer dans le village).
    public class DonjonJeu : MonoBehaviour
    {
        /// Coin du donjon (60 x 48 m) : loin du village (le village tient dans ± 240 m, caméra à 400 m).
        public static readonly Vector3 Origine = new Vector3(1000f, 0f, 0f);

        public static DonjonJeu Instance { get; private set; }

        public DonjonGenerateur generateur;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
