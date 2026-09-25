using UnityEngine;
using UnityEngine.Audio;

namespace Deathless.Audio
{
    /// Réglages audio du projet (un seul asset : Assets/Audio/Resources/DeathlessAudio.asset, chargé par Resources).
    /// Mixer Assets/Audio/Deathless.mixer : Master et trois sous-groupes (Musique, Effets, Interface), volumes exposés
    /// en dB (VolumePrincipal, VolumeMusique, VolumeEffets, VolumeInterface). Sons courts de l'interface (menus) et
    /// son d'aperçu du réglage Effets.
    [CreateAssetMenu(menuName = "Deathless/Réglages audio", fileName = "DeathlessAudio")]
    public class ReglagesAudio : ScriptableObject
    {
        public AudioMixer mixer;
        public AudioMixerGroup groupeMusique;
        public AudioMixerGroup groupeEffets;
        public AudioMixerGroup groupeInterface;

        [Header("Sons de l'interface (catalogue : ui_survol, ui_clic, ui_retour, ui_refus)")]
        public AudioClip survol;
        public AudioClip clic;
        public AudioClip retour;
        [Tooltip("Action refusée (classe verrouillée…), joué doucement.")]
        public AudioClip refus;

        [Header("Aperçu du réglage Effets (son de combat court)")]
        public AudioClip apercuEffets;
    }
}
