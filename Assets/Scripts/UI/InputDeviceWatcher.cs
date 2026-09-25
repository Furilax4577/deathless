using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XInput;

namespace Deathless.UI
{
    /// Famille d'appareils affichée à l'écran : elle choisit les icônes de boutons et le schéma de contrôle.
    public enum InputFamily
    {
        KeyboardMouse,
        Xbox,
        PlayStation,
    }

    /// Suit le dernier appareil utilisé (clavier-souris, manette Xbox, manette PlayStation) et prévient par `Changed`.
    /// Toute manette qui n'est ni XInput/Xbox ni DualShock/DualSense est traitée comme une manette Xbox.
    /// Statique et sans composant : s'installe tout seul au lancement du jeu (et en mode Play dans l'éditeur).
    public static class InputDeviceWatcher
    {
        /// Seuil d'amplitude au-dessous duquel un changement de contrôle est ignoré (dérive des sticks, bruit).
        public const float MagnitudeThreshold = 0.35f;

        static bool s_Installed;

        /// Famille actuelle. Au lancement : manette si une manette est branchée, sinon clavier-souris.
        public static InputFamily Current { get; private set; } = InputFamily.KeyboardMouse;

        /// Dernier appareil ayant produit une entrée significative (peut être null au lancement).
        public static InputDevice CurrentDevice { get; private set; }

        /// Nom du schéma de contrôle de DeathlessControls correspondant à la famille actuelle.
        public static string CurrentControlScheme => SchemeFor(Current);

        /// Appelé à chaque changement de famille (pas à chaque appareil : deux manettes Xbox ne le déclenchent pas).
        public static event Action<InputFamily> Changed;

        public static string SchemeFor(InputFamily family) => family == InputFamily.KeyboardMouse ? "KeyboardMouse" : "Gamepad";

        /// Libellé français de la famille, pour l'indicateur d'appareil.
        public static string DisplayName(InputFamily family)
        {
            switch (family)
            {
                case InputFamily.Xbox: return "Manette Xbox";
                case InputFamily.PlayStation: return "Manette PlayStation";
                default: return "Clavier et souris";
            }
        }

        /// Classe un appareil. Renvoie false pour ce qui ne pilote pas le jeu (capteurs, casques VR, etc.).
        public static bool TryClassify(InputDevice device, out InputFamily family)
        {
            family = InputFamily.KeyboardMouse;
            switch (device)
            {
                case Keyboard _:
                case Mouse _:
                    family = InputFamily.KeyboardMouse;
                    return true;
                case DualShockGamepad _:
                    family = InputFamily.PlayStation;
                    return true;
                case XInputController _:
                    family = InputFamily.Xbox;
                    return true;
                case Gamepad _:
                case Joystick _:
                    family = LooksLikePlayStation(device) ? InputFamily.PlayStation : InputFamily.Xbox;
                    return true;
                default:
                    return false;
            }
        }

        // Manettes Sony vues comme HID générique (pilote inconnu) : on se fie au fabricant.
        static bool LooksLikePlayStation(InputDevice device)
        {
            var d = device.description;
            return (d.manufacturer != null && d.manufacturer.IndexOf("Sony", StringComparison.OrdinalIgnoreCase) >= 0)
                   || (d.product != null && (d.product.IndexOf("DualSense", StringComparison.OrdinalIgnoreCase) >= 0
                                             || d.product.IndexOf("DualShock", StringComparison.OrdinalIgnoreCase) >= 0
                                             || d.product.IndexOf("Wireless Controller", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        /// Force la famille (outil de test, options « icônes de manette » éventuelles).
        public static void SetCurrent(InputFamily family, InputDevice device = null)
        {
            CurrentDevice = device ?? CurrentDevice;
            if (family == Current) return;
            Current = family;
            Changed?.Invoke(family);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // Mode Play sans rechargement de domaine : on repart propre.
            if (s_Installed) InputSystem.onEvent -= OnEvent;
            s_Installed = false;
            Changed = null;
            CurrentDevice = null;
            Current = InputFamily.KeyboardMouse;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (s_Installed) return;
            s_Installed = true;
            InputSystem.onEvent += OnEvent;
            var pad = Gamepad.current;
            if (pad != null && TryClassify(pad, out var family))
            {
                CurrentDevice = pad;
                Current = family;
            }
            else
            {
                CurrentDevice = Keyboard.current;
                Current = InputFamily.KeyboardMouse;
            }
        }

        static void OnEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (device == null) return;
            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>()) return;
            if (!TryClassify(device, out var family)) return;
            if (family == Current && device == CurrentDevice) return;

            // Seul un vrai geste compte (bouton pressé, stick poussé, souris déplacée) : pas le bruit ni la dérive.
            var moved = false;
            foreach (var control in eventPtr.EnumerateChangedControls(device, MagnitudeThreshold))
            {
                if (control is InputControl<float> || control is InputControl<Vector2>)
                {
                    moved = true;
                    break;
                }
            }
            if (!moved) return;

            CurrentDevice = device;
            if (family != Current)
            {
                Current = family;
                Changed?.Invoke(family);
            }
        }
    }
}
