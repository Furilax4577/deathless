using System;
using Deathless.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deathless.Jeu
{
    /// Entrées du héros local. Les actions « Button » viennent **uniquement** de InputChordResolver.Triggered (accords
    /// LB + RB résolu) ; les valeurs continues (Move, Look) sont lues par ReadValue et les maintiens (garde,
    /// sprint) par resolver.IsHeld. Asset d'actions : celui du projet (InputSystem.actions = DeathlessControls), le même
    /// que l'EventSystem et le navigateur d'écrans, pour qu'une seule carte soit active à la fois (le navigateur bascule
    /// Gameplay / UI ; sans navigateur, ce composant active Gameplay lui-même).
    public class HerosEntrees : MonoBehaviour
    {
        public InputActionAsset actions;

        public Vector2 Deplacement { get; private set; }
        public bool GardeMaintenue { get; private set; }
        public bool SprintMaintenu { get; private set; }
        /// Attaque principale maintenue (bander l'arc du rôdeur).
        public bool AttaqueMaintenue { get; private set; }
        /// Touche de la roue à emotes maintenue (Gameplay/Emote : croix bas, B).
        public bool EmoteMaintenue { get; private set; }
        /// Carte Gameplay active (fausse dans les menus) : la roue à emotes se ferme sans rien lancer.
        public bool CarteJeuActive => m_Jeu != null && m_Jeu.enabled;
        /// Action résolue (nom de l'action de la carte Gameplay : Jump, Dodge, AttackPrimary, Skill1…).
        public event Action<string> Action;

        InputChordResolver m_Accords;
        InputAction m_Move, m_Look, m_Garde, m_Sprint, m_Attaque, m_Emote, m_Jump;
        InputActionMap m_Jeu;
        static bool s_NavigateurPresent;

        void OnEnable()
        {
            if (actions == null) actions = InputSystem.actions;
            if (actions == null) { Debug.LogError("HerosEntrees : pas d'asset d'actions"); return; }
            m_Jeu = actions.FindActionMap("Gameplay", true);
            m_Move = m_Jeu.FindAction("Move", true);
            m_Look = m_Jeu.FindAction("Look", true);
            m_Garde = m_Jeu.FindAction("AttackSecondary", true);
            m_Sprint = m_Jeu.FindAction("Sprint", true);
            m_Attaque = m_Jeu.FindAction("AttackPrimary", true);
            m_Emote = m_Jeu.FindAction("Emote", false);
            m_Jump = m_Jeu.FindAction("Jump", false);
            m_Accords = InputChordResolver.ForGameplay(actions);
            m_Accords.Triggered += OnAction;
            s_NavigateurPresent = FindAnyObjectByType<Deathless.UI.Ecrans.NavigateurEcrans>() != null;
            if (!s_NavigateurPresent)
            {
                actions.FindActionMap("UI")?.Disable();
                m_Jeu.Enable();
            }
        }

        void OnDisable()
        {
            if (m_Accords != null) { m_Accords.Dispose(); m_Accords = null; }
        }

        /// Dernière action résolue (tests).
        public string Derniere { get; private set; } = "";

        /// Saut maintenu (accessibilité du relevé du Renversé, mode « maintenir » : OptionsJoueur.RelevageMaintenir).
        public bool SautMaintenu => m_Jump != null && m_Jeu != null && m_Jeu.enabled && m_Accords != null && m_Accords.IsHeld(m_Jump);

        void OnAction(InputAction a)
        {
            Derniere = a.name + "@" + Time.time.ToString("F2");
            Action?.Invoke(a.name);
        }

        /// Rotation de caméra demandée pendant cette image (degrés : lacet, tangage).
        public Vector2 Regard()
        {
            if (m_Look == null || !m_Jeu.enabled) return Vector2.zero;
            var b = GameBalance.Courant;
            Vector2 v = m_Look.ReadValue<Vector2>();
            var dev = m_Look.activeControl != null ? m_Look.activeControl.device : null;
            if (dev is Pointer) return v * b.sensibiliteSouris;
            return v * b.sensibiliteManette * Time.deltaTime;
        }

        /// Regard brut de cette image, pour pointer un secteur de la roue à emotes : delta de la souris en pixels
        /// (`souris` vrai) ou position du stick droit (-1 à 1).
        public Vector2 RegardBrut(out bool souris)
        {
            souris = false;
            if (m_Look == null || !m_Jeu.enabled) return Vector2.zero;
            var dev = m_Look.activeControl != null ? m_Look.activeControl.device : null;
            souris = dev is Pointer;
            return m_Look.ReadValue<Vector2>();
        }

        /// Tests (client automatique du réseau) : déplacement et sprint imposés, à la place de la manette.
        public Vector2? DeplacementTest;
        public bool SprintTest;
        /// Tests : garde (LT) et attaque (RT) maintenues, avec DeplacementTest.
        public bool GardeTest, AttaqueTest;

        /// Tests : action simulée, comme si elle venait de InputChordResolver.
        public void SimulerAction(string action)
        {
            Derniere = action + "@" + Time.time.ToString("F2");
            Action?.Invoke(action);
        }

        void Update()
        {
            if (DeplacementTest.HasValue)
            {
                Deplacement = Vector2.ClampMagnitude(DeplacementTest.Value, 1f);
                SprintMaintenu = SprintTest;
                GardeMaintenue = GardeTest;
                AttaqueMaintenue = AttaqueTest;
                return;
            }
            if (m_Jeu == null || !m_Jeu.enabled)
            {
                Deplacement = Vector2.zero;
                GardeMaintenue = SprintMaintenu = AttaqueMaintenue = EmoteMaintenue = false;
                return;
            }
            Deplacement = Vector2.ClampMagnitude(m_Move.ReadValue<Vector2>(), 1f);
            GardeMaintenue = m_Accords.IsHeld(m_Garde);
            SprintMaintenu = m_Accords.IsHeld(m_Sprint);
            AttaqueMaintenue = m_Accords.IsHeld(m_Attaque);
            EmoteMaintenue = m_Emote != null && m_Accords.IsHeld(m_Emote);
        }
    }
}
