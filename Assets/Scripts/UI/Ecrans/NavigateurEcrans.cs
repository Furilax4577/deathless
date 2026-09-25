using System.Collections.Generic;
using Deathless.Audio;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Écrans de la version 0.1 dans un seul UIDocument : menu principal, options, crédits, HUD, pause, score.
    ///
    /// - Pile d'écrans : le sommet reçoit les actions ; on affiche tout depuis le dernier écran opaque.
    /// - Écran de base selon DonneesUI : pas de partie → menu principal ; partie en cours → HUD ;
    ///   partie terminée → score.
    /// - **Une seule carte d'actions active** : Gameplay quand le HUD est au sommet, UI dans les menus
    ///   (Gameplay/Pause ouvre la pause ; UI/Cancel ou UI/Pause la referme). La partie continue pendant la pause.
    [RequireComponent(typeof(UIDocument))]
    [DefaultExecutionOrder(10)]
    public class NavigateurEcrans : MonoBehaviour
    {
        public InputActionAsset actions;

        [Header("Écrans (UXML)")]
        public VisualTreeAsset menuPrincipal;
        public VisualTreeAsset options;
        public VisualTreeAsset credits;
        public VisualTreeAsset hud;
        public VisualTreeAsset pause;
        public VisualTreeAsset score;
        public VisualTreeAsset choixClasse;
        public VisualTreeAsset saisie;
        public VisualTreeAsset lobby;
        [Tooltip("Menu d'achat (relique, taverne) : Assets/UI/Screens/Achat/Achat.uxml.")]
        public VisualTreeAsset achat;
        [Tooltip("Menu du personnage (Tab / Y) : Assets/UI/Screens/Personnage/Personnage.uxml.")]
        public VisualTreeAsset personnage;

        public EcranMenuPrincipal MenuPrincipal { get; private set; }
        public EcranOptions Options { get; private set; }
        public EcranCredits Credits { get; private set; }
        public EcranHud Hud { get; private set; }
        public EcranPause Pause { get; private set; }
        public EcranScore Score { get; private set; }
        public EcranChoixClasse ChoixClasse { get; private set; }
        public EcranSaisie Saisie { get; private set; }
        public EcranLobby Lobby { get; private set; }
        public EcranAchat Achat { get; private set; }
        public EcranPersonnage Personnage { get; private set; }

        readonly List<Ecran> m_Pile = new List<Ecran>();
        readonly List<Ecran> m_Tous = new List<Ecran>();
        VisualElement m_Racine;
        InputActionMap m_CarteUI;
        InputActionMap m_CarteJeu;
        IEtatPartie m_PartieSuivie;
        int m_ImageDerniereBascule = -1;
        float m_SilenceSurvol;

        /// Son de survol (le focus change) : pas pendant l'ouverture d'un écran ni juste après une validation.
        public bool SurvolPermis => Time.unscaledTime >= m_SilenceSurvol;
        public void SilencerSurvol(float duree = 0.3f) => m_SilenceSurvol = Time.unscaledTime + duree;

        public Ecran Sommet => m_Pile.Count > 0 ? m_Pile[m_Pile.Count - 1] : null;
        public IReadOnlyList<Ecran> Pile => m_Pile;

        void OnEnable()
        {
            if (actions == null && InputGlyphs.Default != null) actions = InputGlyphs.Default.actions;
            var document = GetComponent<UIDocument>();
            m_Racine = document.rootVisualElement;
            var conteneur = m_Racine.Q("ecrans") ?? m_Racine;

            MenuPrincipal = Creer(new EcranMenuPrincipal(), menuPrincipal, conteneur);
            Hud = Creer(new EcranHud(), hud, conteneur);
            Score = Creer(new EcranScore(), score, conteneur);
            Pause = Creer(new EcranPause(), pause, conteneur);
            Options = Creer(new EcranOptions(), options, conteneur);
            Credits = Creer(new EcranCredits(), credits, conteneur);
            ChoixClasse = Creer(new EcranChoixClasse(), choixClasse, conteneur);
            Lobby = Creer(new EcranLobby(), lobby, conteneur);
            Saisie = Creer(new EcranSaisie(), saisie, conteneur);
            Achat = Creer(new EcranAchat(), achat, conteneur);
            Personnage = Creer(new EcranPersonnage(), personnage, conteneur);

            UINavigation.SetupScreen(m_Racine);
            UIScale.TagRoot(m_Racine);
            UIScale.Changed += OnEchelle;
            InputDeviceWatcher.Changed += OnAppareil;
            OnAppareil(InputDeviceWatcher.Current);

            if (actions != null)
            {
                m_CarteUI = actions.FindActionMap("UI", true);
                m_CarteJeu = actions.FindActionMap("Gameplay", true);
                Abonner("UI/Cancel", OnRetour);
                Abonner("UI/Pause", OnPauseUI);
                Abonner("UI/TabPrevious", _ => Sommet?.OngletPrecedent());
                Abonner("UI/TabNext", _ => Sommet?.OngletSuivant());
                Abonner("UI/Reset", _ => Sommet?.Reinitialiser());
                Abonner("Gameplay/Pause", OnPauseJeu);
            }

            DonneesUI.Changees += EvaluerEcranDeBase;
            DonneesUI.MenuAchatDemande += OuvrirAchat;
            DonneesUI.MenuPersonnageDemande += OuvrirPersonnage;
            EvaluerEcranDeBase();
        }

        void OnDisable()
        {
            DonneesUI.Changees -= EvaluerEcranDeBase;
            DonneesUI.MenuAchatDemande -= OuvrirAchat;
            DonneesUI.MenuPersonnageDemande -= OuvrirPersonnage;
            Suivre(null);
            UIScale.Changed -= OnEchelle;
            InputDeviceWatcher.Changed -= OnAppareil;
            foreach (var (action, rappel) in m_Abonnements) action.performed -= rappel;
            m_Abonnements.Clear();
        }

        T Creer<T>(T ecran, VisualTreeAsset uxml, VisualElement conteneur) where T : Ecran
        {
            var racine = uxml != null ? uxml.Instantiate() : new VisualElement();
            racine.name = typeof(T).Name;
            conteneur.Add(racine);
            ecran.Initialiser(this, racine);
            ecran.Cacher();
            m_Tous.Add(ecran);
            return ecran;
        }

        readonly List<(InputAction, System.Action<InputAction.CallbackContext>)> m_Abonnements =
            new List<(InputAction, System.Action<InputAction.CallbackContext>)>();

        void Abonner(string chemin, System.Action<InputAction.CallbackContext> rappel)
        {
            var action = actions.FindAction(chemin, true);
            action.performed += rappel;
            m_Abonnements.Add((action, rappel));
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            foreach (var e in m_Tous)
                if (e.Racine.resolvedStyle.display != DisplayStyle.None) e.MiseAJour(dt);
        }

        // ------------------------------------------------------------------ Pile

        /// Ouvre un écran par-dessus le sommet.
        public void Ouvrir(Ecran ecran)
        {
            if (ecran == null || Sommet == ecran) return;
            m_Pile.Remove(ecran);
            m_Pile.Add(ecran);
            Rafraichir();
        }

        /// Ferme le sommet (jamais l'écran de base).
        public void Fermer()
        {
            if (m_Pile.Count <= 1) return;
            m_Pile.RemoveAt(m_Pile.Count - 1);
            Rafraichir();
        }

        /// Remplace toute la pile par un écran de base.
        public void Base(Ecran ecran)
        {
            m_Pile.Clear();
            m_Pile.Add(ecran);
            Rafraichir();
        }

        void Rafraichir()
        {
            // Visible : le dernier écran opaque (HUD, menu principal, score) et le sommet par-dessus.
            // Les écrans superposés intermédiaires (pause sous les options) sont cachés : un seul voile, une seule barre d'invites.
            var debut = 0;
            for (var i = m_Pile.Count - 1; i >= 0; i--)
                if (m_Pile[i].Opaque) { debut = i; break; }
            foreach (var e in m_Tous)
            {
                var index = m_Pile.IndexOf(e);
                if (index == debut || (index >= 0 && index == m_Pile.Count - 1)) e.Montrer(); else e.Cacher();
            }
            // Ordre d'affichage = ordre de la pile.
            foreach (var e in m_Pile) e.Racine.BringToFront();

            var sommet = Sommet;
            if (sommet == null) return;
            SilencerSurvol();
            AppliquerCarte(sommet.CarteUI);
            sommet.AuSommet();
            if (sommet.CarteUI)
            {
                var cible = sommet.FocusARendre;
                m_Racine.schedule.Execute(() => { if (Sommet == sommet) UINavigation.Focus(cible); }).StartingIn(30);
            }
            else
            {
                (m_Racine.panel?.focusController?.focusedElement as VisualElement)?.Blur();
            }
        }

        /// Une seule carte active : UI dans les menus, Gameplay en jeu.
        void AppliquerCarte(bool ui)
        {
            if (m_CarteUI == null) return;
            m_ImageDerniereBascule = Time.frameCount;
            if (ui)
            {
                m_CarteJeu.Disable();
                m_CarteUI.Enable();
            }
            else
            {
                m_CarteUI.Disable();
                m_CarteJeu.Enable();
            }
        }

        /// Carte active en ce moment (« UI » ou « Gameplay »), pour les tests.
        public string CarteActive => m_CarteUI != null && m_CarteUI.enabled ? (m_CarteJeu.enabled ? "UI+Gameplay" : "UI")
            : m_CarteJeu != null && m_CarteJeu.enabled ? "Gameplay" : "aucune";

        // ------------------------------------------------------------------ Données

        /// Réévalue l'écran de base (après le choix du pseudo, par exemple).
        public void Reevaluer() => EvaluerEcranDeBase();

        void EvaluerEcranDeBase()
        {
            var partie = DonneesUI.Partie;
            Suivre(partie);
            // Premier lancement : pas de pseudo enregistré → écran de saisie du pseudo avant le menu principal.
            if (partie == null && !DonneesUI.Profil.PseudoDefini && saisie != null)
            {
                if (m_Pile.Count == 0 || m_Pile[0] != Saisie || !Saisie.Obligatoire)
                {
                    Saisie.ConfigurerPseudo(true, Reevaluer);
                    Base(Saisie);
                }
                return;
            }
            Ecran voulu = partie == null ? MenuPrincipal : partie.Phase == PhasePartie.Terminee ? (Ecran)Score : Hud;
            if (m_Pile.Count == 0 || m_Pile[0] != voulu) Base(voulu);
        }

        void Suivre(IEtatPartie partie)
        {
            if (m_PartieSuivie == partie) return;
            if (m_PartieSuivie != null) m_PartieSuivie.PartieTerminee -= EvaluerEcranDeBase;
            m_PartieSuivie = partie;
            if (m_PartieSuivie != null) m_PartieSuivie.PartieTerminee += EvaluerEcranDeBase;
            Hud.Suivre(partie);
        }

        // ------------------------------------------------------------------ Entrées

        bool BasculeRecente => Time.frameCount == m_ImageDerniereBascule;

        void OnRetour(InputAction.CallbackContext _)
        {
            if (BasculeRecente) return;
            var sommet = Sommet;
            if (sommet == null) return;
            if (sommet.CarteUI && sommet != MenuPrincipal) VolumesAudio.JouerInterface(SonInterface.Retour);
            if (sommet.Retour()) return;
            Fermer();
        }

        void OnPauseUI(InputAction.CallbackContext ctx)
        {
            // Au clavier, Échap est à la fois Retour et Pause : Retour s'en charge.
            if (BasculeRecente || ctx.control?.device is Keyboard) return;
            if (Sommet == Pause) Fermer();
        }

        void OnPauseJeu(InputAction.CallbackContext _)
        {
            if (BasculeRecente) return;
            if (Sommet == Hud) Ouvrir(Pause);
        }

        /// Le jeu demande un menu d'achat (interaction) : ouvert par-dessus le HUD seulement.
        void OuvrirAchat(IMenuAchat menu)
        {
            if (menu == null || achat == null || Sommet != Hud || BasculeRecente) return;
            Achat.Afficher(menu);
            Ouvrir(Achat);
        }

        /// Le jeu demande le menu du personnage (Tab / Y) : ouvert par-dessus le HUD seulement.
        void OuvrirPersonnage()
        {
            var menu = DonneesUI.Personnage;
            if (menu == null || personnage == null || Sommet != Hud || BasculeRecente) return;
            Personnage.Afficher(menu);
            Ouvrir(Personnage);
        }

        void OnEchelle(int _) => UIScale.TagRoot(m_Racine);

        void OnAppareil(InputFamily famille)
        {
            var nom = InputDeviceWatcher.DisplayName(famille);
            if (famille != InputFamily.KeyboardMouse) nom += " détectée";
            m_Racine.Query<Label>(className: "dl-device-name").ForEach(l => l.text = nom);
        }
    }
}
