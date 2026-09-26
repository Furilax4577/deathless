using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Deathless.Controls
{
    /// Résout l'accord de la manette (décision du 25/09/2026, « court délai ») :
    ///   LB → Compétence 1, RB → Compétence 2, LB + RB → Compétence 3.
    /// (Pas d'ultime ni d'accroupissement : L3 est Sprinter seul, sans délai ; R3 est libre.)
    /// Quand un des deux boutons est pressé, l'action seule attend <see cref="chordWindow"/> secondes (0,1 par défaut).
    /// Si l'autre bouton arrive dans ce délai, c'est l'accord qui part, et aucune des deux actions seules.
    /// Sinon l'action seule part à la fin du délai (même si le bouton a déjà été relâché).
    /// Au clavier (touches distinctes) tout part immédiatement.
    ///
    /// Le résolveur relaie aussi toutes les autres actions « Button » de la carte, sans délai : le code de jeu
    /// s'abonne à <see cref="Triggered"/> au lieu des `performed` de la carte Gameplay. S'abonner directement à
    /// `Skill1.performed` contournerait la résolution (Compétence 1 partirait avec la 3).
    ///
    /// Actions maintenues (garde, Sprinter) : <see cref="IsHeld"/>. Pour un côté d'accord, vrai entre le départ résolu
    /// et le relâchement, faux si le bouton a servi à l'accord ; pour les autres actions, simple IsPressed.
    public sealed class InputChordResolver : IDisposable
    {
        public const float DefaultChordWindow = 0.1f;

        /// Délai d'attente de l'accord, en secondes (à équilibrer).
        public float chordWindow = DefaultChordWindow;

        /// Action logique déclenchée (après résolution des accords).
        public event Action<InputAction> Triggered;

        enum Phase { Idle, Pending, Fired, Consumed }

        sealed class Side
        {
            public InputAction action;
            public Chord chord;
            public Side other;
            public Phase phase;
            public double pressTime;
            public bool pressed;
        }

        sealed class Chord
        {
            public Side first;
            public Side second;
            public InputAction both;
        }

        readonly InputActionMap m_Map;
        readonly Dictionary<InputAction, Side> m_Sides = new Dictionary<InputAction, Side>();
        readonly HashSet<InputAction> m_ChordActions = new HashSet<InputAction>();
        readonly List<InputAction> m_Relayed = new List<InputAction>();
        readonly List<Chord> m_Chords = new List<Chord>();
        bool m_Disposed;

        /// Résolveur sur la carte Gameplay de DeathlessControls, avec l'accord du jeu (LB + RB).
        public static InputChordResolver ForGameplay(InputActionAsset asset, float window = DefaultChordWindow)
        {
            var map = asset.FindActionMap("Gameplay", throwIfNotFound: true);
            var resolver = new InputChordResolver(map) { chordWindow = window };
            resolver.AddChord(map.FindAction("Skill1", true), map.FindAction("Skill2", true), map.FindAction("Skill3", true));
            resolver.RelayOtherButtons();
            return resolver;
        }

        public InputChordResolver(InputActionMap map)
        {
            m_Map = map;
            InputSystem.onAfterUpdate += OnAfterUpdate;
        }

        /// Déclare un accord : <paramref name="first"/> et <paramref name="second"/> seuls, <paramref name="both"/> ensemble.
        public void AddChord(InputAction first, InputAction second, InputAction both)
        {
            var chord = new Chord { both = both };
            chord.first = new Side { action = first, chord = chord };
            chord.second = new Side { action = second, chord = chord, other = chord.first };
            chord.first.other = chord.second;
            m_Chords.Add(chord);
            foreach (var side in new[] { chord.first, chord.second })
            {
                m_Sides[side.action] = side;
                m_ChordActions.Add(side.action);
                side.action.performed += OnSidePerformed;
                side.action.canceled += OnSideCanceled;
            }
            m_ChordActions.Add(both);
            both.performed += OnBothPerformed;
        }

        /// Relaie sans délai toutes les autres actions de type Button de la carte.
        public void RelayOtherButtons()
        {
            foreach (var action in m_Map.actions)
            {
                if (action.type != InputActionType.Button || m_ChordActions.Contains(action) || m_Relayed.Contains(action)) continue;
                action.performed += OnRelayPerformed;
                m_Relayed.Add(action);
            }
        }

        /// Vrai si l'action est maintenue et a été déclenchée seule (pas consommée par un accord).
        public bool IsHeld(InputAction action)
        {
            if (m_Sides.TryGetValue(action, out var side)) return side.pressed && side.phase == Phase.Fired;
            return action.IsPressed();
        }

        static bool FromGamepad(InputAction.CallbackContext ctx) =>
            ctx.control != null && !(ctx.control.device is Keyboard) && !(ctx.control.device is Mouse);

        void OnRelayPerformed(InputAction.CallbackContext ctx) => Emit(ctx.action);

        void OnBothPerformed(InputAction.CallbackContext ctx)
        {
            // À la manette, l'accord est détecté par le résolveur (le composite LB+RB ne sert qu'à l'affichage).
            if (!FromGamepad(ctx)) Emit(ctx.action);
        }

        void OnSidePerformed(InputAction.CallbackContext ctx)
        {
            var side = m_Sides[ctx.action];
            if (!FromGamepad(ctx))
            {
                Emit(side.action);
                return;
            }

            side.pressed = true;
            if (side.phase == Phase.Pending) Fire(side);   // second appui du même bouton avant la fin du délai
            var other = side.other;
            // L'autre bouton attend encore : si le délai n'est pas écoulé à l'instant de cet appui, c'est l'accord.
            if (other.phase == Phase.Pending)
            {
                if (ctx.time - other.pressTime <= chordWindow)
                {
                    other.phase = Phase.Consumed;
                    side.phase = Phase.Consumed;
                    Emit(side.chord.both);
                    return;
                }
                Fire(other);   // délai dépassé (événements traités en retard) : l'autre part d'abord.
            }
            side.phase = Phase.Pending;
            side.pressTime = ctx.time;
        }

        void OnSideCanceled(InputAction.CallbackContext ctx)
        {
            if (!m_Sides.TryGetValue(ctx.action, out var side)) return;
            side.pressed = false;
            // Pending : l'action seule partira quand même à la fin du délai.
            if (side.phase == Phase.Consumed || side.phase == Phase.Fired) side.phase = Phase.Idle;
        }

        void OnAfterUpdate()
        {
            if (m_Disposed) return;
            var now = InputState.currentTime;
            foreach (var chord in m_Chords)
            {
                Expire(chord.first, now);
                Expire(chord.second, now);
            }
        }

        void Expire(Side side, double now)
        {
            if (side.phase == Phase.Pending && now - side.pressTime >= chordWindow) Fire(side);
        }

        void Fire(Side side)
        {
            // Carte désactivée pendant le délai (menu ouvert, pause) : l'appui en attente est abandonné, sinon la
            // compétence partait derrière le menu.
            if (!side.action.enabled)
            {
                side.phase = Phase.Idle;
                side.pressed = false;
                return;
            }
            side.phase = side.pressed ? Phase.Fired : Phase.Idle;
            Emit(side.action);
        }

        void Emit(InputAction action) => Triggered?.Invoke(action);

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            InputSystem.onAfterUpdate -= OnAfterUpdate;
            foreach (var chord in m_Chords)
            {
                foreach (var side in new[] { chord.first, chord.second })
                {
                    side.action.performed -= OnSidePerformed;
                    side.action.canceled -= OnSideCanceled;
                }
                chord.both.performed -= OnBothPerformed;
            }
            foreach (var action in m_Relayed) action.performed -= OnRelayPerformed;
            m_Relayed.Clear();
            Triggered = null;
        }
    }
}
