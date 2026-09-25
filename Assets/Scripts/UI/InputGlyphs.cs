using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deathless.UI
{
    /// Associe un chemin de contrôle (sans l'appareil, en minuscules : « buttonsouth », « dpad/up », « space »)
    /// à l'icône Kenney de chaque famille d'appareils, et traduit une action en une suite d'icônes affichables.
    ///
    /// Touches de lettres : l'Input System lie des positions physiques (disposition US), mais l'icône doit montrer
    /// la lettre gravée sur la touche du joueur. Pour une lettre, on cherche donc l'icône de la lettre affichée par
    /// la disposition active (<Keyboard>/q affiche « A » en AZERTY, donc keyboard_a). Les autres touches (chiffres,
    /// Espace, Ctrl…) sont cherchées par leur chemin. Sans icône, l'invite dessine la touche (texte dans une pastille).
    ///
    /// L'asset par défaut est chargé depuis Resources (<see cref="ResourcePath"/>) : les invites fonctionnent sans
    /// câblage, y compris dans l'UI Builder.
    [CreateAssetMenu(menuName = "Deathless/UI/Input Glyphs", fileName = "DeathlessInputGlyphs")]
    public class InputGlyphs : ScriptableObject
    {
        public const string ResourcePath = "DeathlessInputGlyphs";

        [Serializable]
        public struct Glyph
        {
            [Tooltip("Chemin du contrôle sans l'appareil, en minuscules (buttonsouth, dpad/up, leftshoulder, space, f1, leftbutton).")]
            public string control;
            public Texture2D icon;
        }

        /// Un morceau d'invite : une icône, ou à défaut un texte à dessiner dans une pastille.
        public struct Part
        {
            public Texture2D icon;
            public string text;
            public bool isSeparator;   // le « + » d'une combinaison
        }

        [Tooltip("Actions du jeu (DeathlessControls). Les invites cherchent leurs actions ici.")]
        public InputActionAsset actions;

        [Header("Icônes d'appareil (indicateur « appareil détecté »)")]
        public Texture2D xboxDevice;
        public Texture2D playStationDevice;
        public Texture2D keyboardMouseDevice;

        [Header("Icônes par contrôle")]
        public List<Glyph> xbox = new List<Glyph>();
        public List<Glyph> playStation = new List<Glyph>();
        public List<Glyph> keyboardMouse = new List<Glyph>();

        static InputGlyphs s_Default;
        Dictionary<string, Texture2D>[] m_Maps;

        /// Asset chargé depuis Resources/DeathlessInputGlyphs.
        public static InputGlyphs Default
        {
            get
            {
                if (s_Default == null) s_Default = Resources.Load<InputGlyphs>(ResourcePath);
                return s_Default;
            }
            set => s_Default = value;
        }

        void OnEnable() => m_Maps = null;
        void OnValidate() => m_Maps = null;

        public Texture2D DeviceIcon(InputFamily family)
        {
            switch (family)
            {
                case InputFamily.Xbox: return xboxDevice;
                case InputFamily.PlayStation: return playStationDevice;
                default: return keyboardMouseDevice;
            }
        }

        /// Icône d'un contrôle pour une famille ; <paramref name="control"/> sans appareil (« buttonSouth », « dpad/up »).
        public Texture2D Find(InputFamily family, string control)
        {
            if (string.IsNullOrEmpty(control)) return null;
            if (m_Maps == null)
            {
                m_Maps = new Dictionary<string, Texture2D>[3];
                m_Maps[(int)InputFamily.KeyboardMouse] = Build(keyboardMouse);
                m_Maps[(int)InputFamily.Xbox] = Build(xbox);
                m_Maps[(int)InputFamily.PlayStation] = Build(playStation);
            }
            m_Maps[(int)family].TryGetValue(control.ToLowerInvariant(), out var tex);
            return tex;
        }

        static Dictionary<string, Texture2D> Build(List<Glyph> list)
        {
            var map = new Dictionary<string, Texture2D>();
            foreach (var g in list)
                if (!string.IsNullOrEmpty(g.control) && g.icon != null)
                    map[g.control.ToLowerInvariant()] = g.icon;
            return map;
        }

        /// Cherche une action par « Carte/Action » ou par son seul nom.
        public InputAction FindAction(string nameOrPath)
        {
            if (actions == null || string.IsNullOrEmpty(nameOrPath)) return null;
            return actions.FindAction(nameOrPath, throwIfNotFound: false);
        }

        /// Remplit <paramref name="parts"/> avec les icônes de l'action pour la famille donnée.
        /// Renvoie false si l'action n'a aucune liaison dans le schéma de cette famille.
        public bool Resolve(InputAction action, InputFamily family, List<Part> parts)
        {
            parts.Clear();
            if (action == null) return false;
            var index = PickBinding(action, family);
            if (index < 0) return false;

            var bindings = action.bindings;
            var binding = bindings[index];
            if (!binding.isComposite)
            {
                parts.Add(MakePart(action, index, family));
                return true;
            }

            var first = index + 1;
            var last = first;
            while (last < bindings.Count && bindings[last].isPartOfComposite) last++;

            var composite = binding.path ?? string.Empty;
            if (composite.StartsWith("2DVector", StringComparison.OrdinalIgnoreCase)
                || composite.StartsWith("Dpad", StringComparison.OrdinalIgnoreCase))
            {
                // Quatre flèches : une seule icône. Sinon les quatre touches dans l'ordre haut, gauche, bas, droite (ZQSD).
                if (AllArrows(bindings, first, last) && family == InputFamily.KeyboardMouse)
                {
                    var arrows = Find(family, "arrows");
                    if (arrows != null)
                    {
                        parts.Add(new Part { icon = arrows });
                        return true;
                    }
                }
                foreach (var partName in new[] { "up", "left", "down", "right" })
                    for (var i = first; i < last; i++)
                        if (string.Equals(bindings[i].name, partName, StringComparison.OrdinalIgnoreCase))
                        {
                            parts.Add(MakePart(action, i, family));
                            break;
                        }
                return parts.Count > 0;
            }

            // Combinaisons (ButtonWithOneModifier, OneModifier, TwoModifiers…) : modificateurs puis bouton, séparés par « + ».
            for (var i = first; i < last; i++)
            {
                if (parts.Count > 0) parts.Add(new Part { isSeparator = true, text = "+" });
                parts.Add(MakePart(action, i, family));
            }
            return parts.Count > 0;
        }

        static bool AllArrows(IReadOnlyList<InputBinding> bindings, int first, int last)
        {
            for (var i = first; i < last; i++)
                if (bindings[i].path == null || !bindings[i].path.EndsWith("Arrow", StringComparison.OrdinalIgnoreCase))
                    return false;
            return last > first;
        }

        /// Choisit la liaison à afficher : dans le schéma de la famille, en préférant un chemin propre à la famille
        /// (<DualShockGamepad>/… pour PlayStation, <XInputController>/… pour Xbox) à un chemin générique <Gamepad>/….
        public static int PickBinding(InputAction action, InputFamily family)
        {
            var group = InputDeviceWatcher.SchemeFor(family);
            var bindings = action.bindings;
            var best = -1;
            var bestScore = 0;
            for (var i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isPartOfComposite) continue;
                var probe = b;
                if (b.isComposite)
                {
                    if (i + 1 >= bindings.Count || !bindings[i + 1].isPartOfComposite) continue;
                    probe = bindings[i + 1];
                }
                if (!InGroup(probe.groups, group)) continue;

                var score = LayoutScore(probe.effectivePath, family);
                if (score > bestScore)
                {
                    best = i;
                    bestScore = score;
                }
            }
            return best;
        }

        static bool InGroup(string groups, string group)
        {
            if (string.IsNullOrEmpty(groups)) return false;
            foreach (var g in groups.Split(InputBinding.Separator))
                if (string.Equals(g, group, StringComparison.Ordinal))
                    return true;
            return false;
        }

        // 0 = liaison inapplicable à la famille, 1 = générique, 2 = propre à la famille.
        static int LayoutScore(string path, InputFamily family)
        {
            var layout = InputControlPath.TryGetDeviceLayout(path);
            if (string.IsNullOrEmpty(layout) || family == InputFamily.KeyboardMouse) return 1;
            var isPs = IsBasedOn(layout, "DualShockGamepad");
            var isXbox = IsBasedOn(layout, "XInputController") || layout.IndexOf("Xbox", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isPs) return family == InputFamily.PlayStation ? 2 : 0;
            if (isXbox) return family == InputFamily.Xbox ? 2 : 0;
            return 1;
        }

        static bool IsBasedOn(string layout, string baseLayout)
        {
            if (string.Equals(layout, baseLayout, StringComparison.OrdinalIgnoreCase)) return true;
            try { return InputSystem.IsFirstLayoutBasedOnSecond(layout, baseLayout); }
            catch (ArgumentException) { return false; }
        }

        Part MakePart(InputAction action, int bindingIndex, InputFamily family)
        {
            var display = action.GetBindingDisplayString(bindingIndex, out _, out var controlPath,
                InputBinding.DisplayStringOptions.DontIncludeInteractions);
            var path = action.bindings[bindingIndex].effectivePath;
            if (string.IsNullOrEmpty(controlPath)) controlPath = StripDevice(path);

            Texture2D icon = null;
            if (IsLetterKey(path, controlPath))
            {
                // Lettre : icône de la lettre gravée sur la touche selon la disposition active.
                if (!string.IsNullOrEmpty(display) && display.Length == 1 && char.IsLetter(display[0]))
                    icon = Find(family, display.ToLowerInvariant());
            }
            else
            {
                icon = Find(family, controlPath);
            }

            return new Part { icon = icon, text = icon != null ? null : FrenchKeyName(controlPath, display) };
        }

        static bool IsLetterKey(string path, string controlPath)
        {
            return path != null && path.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase)
                   && controlPath != null && controlPath.Length == 1 && controlPath[0] >= 'a' && controlPath[0] <= 'z';
        }

        public static string StripDevice(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            var slash = path.IndexOf('/');
            return slash >= 0 && path.StartsWith("<") ? path.Substring(slash + 1) : path;
        }

        /// Nom affiché d'une touche dessinée (sans icône). Les noms usuels sont traduits ; sinon, le nom de la disposition active.
        public static string FrenchKeyName(string controlPath, string display)
        {
            switch ((controlPath ?? string.Empty).ToLowerInvariant())
            {
                case "space": return "Espace";
                case "enter": case "numpadenter": return "Entrée";
                case "escape": return "Échap";
                case "delete": return "Suppr";
                case "backspace": return "Retour arr.";
                case "leftshift": case "rightshift": return "Maj";
                case "leftctrl": case "rightctrl": return "Ctrl";
                case "leftalt": return "Alt";
                case "rightalt": return "Alt Gr";
                case "tab": return "Tab";
                case "insert": return "Inser";
                case "home": return "Début";
                case "end": return "Fin";
                case "pageup": return "Pg préc";
                case "pagedown": return "Pg suiv";
                case "numlock": return "Verr num";
                case "printscreen": return "Impr écran";
                case "scrolllock": return "Arrêt défil";
                case "pause": return "Pause";
                case "capslock": return "Verr maj";
                case "leftbutton": return "Clic G";
                case "rightbutton": return "Clic D";
                case "middlebutton": return "Clic milieu";
            }
            if (!string.IsNullOrEmpty(display)) return display.Length == 1 ? display.ToUpperInvariant() : display;
            return controlPath ?? "?";
        }
    }
}
