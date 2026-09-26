using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Deathless.UI
{
    /// Outils de navigation manette pour UI Toolkit.
    ///
    /// La navigation (croix, stick, flèches) arrive par l'EventSystem et InputSystemUIInputModule, qui ne l'envoient
    /// qu'à l'objet sélectionné de l'EventSystem. Un clic sur le panneau le sélectionne, mais pas un Focus() fait par
    /// le code : <see cref="SyncEventSystemSelection"/> sélectionne le panneau dès qu'un de ses éléments prend le focus.
    ///
    /// Un TextField qui a le focus garde pour lui les événements de navigation (ils déplacent le curseur de texte) :
    /// à la manette, on resterait bloqué dedans. <see cref="ReleaseTextFields"/> fait sortir le focus du champ :
    /// à la manette dans toutes les directions, au clavier seulement vers le haut et le bas (gauche et droite
    /// restent au curseur de texte).
    public static class UINavigation
    {
        static readonly List<VisualElement> s_Candidates = new List<VisualElement>();

        /// À appeler une fois par écran : tout focus dans <paramref name="root"/> sélectionne le panneau dans l'EventSystem
        /// et fait défiler le ScrollView parent jusqu'à l'élément.
        public static void SyncEventSystemSelection(VisualElement root)
        {
            root.UnregisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
            root.RegisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
        }

        static void OnFocusIn(FocusInEvent evt)
        {
            if (evt.currentTarget is VisualElement ve) SelectPanel(ve.panel);
            // Écran qui défile (ex. à ×3) : l'élément focus à la manette reste visible.
            if (evt.target is VisualElement target)
                target.GetFirstAncestorOfType<ScrollView>()?.ScrollTo(target);
        }

        /// Préparation standard d'un écran : sélection EventSystem, défilement vers le focus, sortie des TextField.
        public static void SetupScreen(VisualElement root)
        {
            SyncEventSystemSelection(root);
            ReleaseTextFields(root);
        }

        /// Sélectionne dans l'EventSystem le PanelEventHandler du panneau (sans quoi la manette ne navigue pas).
        public static void SelectPanel(IPanel panel)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || panel == null) return;
            var selected = eventSystem.currentSelectedGameObject;
            if (selected != null && selected.TryGetComponent<PanelEventHandler>(out var current) && current.panel == panel) return;
            foreach (var handler in Object.FindObjectsByType<PanelEventHandler>(FindObjectsSortMode.None))
            {
                if (handler.panel != panel) continue;
                eventSystem.SetSelectedGameObject(handler.gameObject);
                return;
            }
        }

        /// Focus + sélection du panneau : à utiliser pour donner le focus initial d'un écran.
        public static void Focus(VisualElement element)
        {
            if (element == null) return;
            element.Focus();
            SelectPanel(element.panel);
        }

        /// À appeler une fois après la construction d'un écran (ou après l'ajout de champs texte).
        public static void ReleaseTextFields(VisualElement root)
        {
            root.Query<TextField>().ForEach(field =>
            {
                field.UnregisterCallback<NavigationMoveEvent>(OnTextFieldNavigate, TrickleDown.TrickleDown);
                field.RegisterCallback<NavigationMoveEvent>(OnTextFieldNavigate, TrickleDown.TrickleDown);
            });
        }

        static void OnTextFieldNavigate(NavigationMoveEvent evt)
        {
            var field = evt.currentTarget as VisualElement;
            if (field == null) return;
            var vertical = evt.direction == NavigationMoveEvent.Direction.Up || evt.direction == NavigationMoveEvent.Direction.Down;
            var gamepad = InputDeviceWatcher.Current != InputFamily.KeyboardMouse;
            if (!vertical && !gamepad) return;
            if (evt.direction == NavigationMoveEvent.Direction.None || evt.direction == NavigationMoveEvent.Direction.Next
                || evt.direction == NavigationMoveEvent.Direction.Previous) return;

            Move(field, evt.direction);
            evt.StopImmediatePropagation();
            field.focusController?.IgnoreEvent(evt);
        }

        /// Chaîne haut / bas une liste d'éléments focusables (lignes d'une table dans un ScrollView) : la navigation
        /// spatiale d'UI Toolkit ne passe pas toujours d'une ligne à la suivante dans un ScrollView. Au-dessus de la
        /// première ligne, la navigation normale reprend (vers les onglets, par exemple). Sous la dernière, le focus va à
        /// <paramref name="apresDernier"/> s'il est donné (bouton Retour sous la table), sinon il reste sur place.
        public static void ChainerVerticalement(IList<VisualElement> elements, VisualElement apresDernier = null)
        {
            for (var i = 0; i < elements.Count; i++)
            {
                var precedent = i > 0 ? elements[i - 1] : null;
                var suivant = i < elements.Count - 1 ? elements[i + 1] : apresDernier;
                elements[i].RegisterCallback<NavigationMoveEvent>(evt =>
                {
                    var cible = evt.direction == NavigationMoveEvent.Direction.Down ? suivant
                        : evt.direction == NavigationMoveEvent.Direction.Up ? precedent : null;
                    var bas = evt.direction == NavigationMoveEvent.Direction.Down;
                    if (cible == null && !bas) return;   // en haut de la liste : navigation normale
                    if (cible != null) cible.Focus();
                    evt.StopPropagation();
                    (evt.currentTarget as VisualElement)?.focusController?.IgnoreEvent(evt);
                });
            }
        }

        /// Donne le focus à l'élément focusable le plus proche de <paramref name="from"/> dans la direction donnée.
        /// Renvoie false s'il n'y en a pas (le focus ne bouge pas).
        public static bool Move(VisualElement from, NavigationMoveEvent.Direction direction)
        {
            var root = from?.panel?.visualTree;
            if (root == null) return false;
            var origin = from.worldBound;
            var dir = ToVector(direction);
            if (dir == Vector2.zero) return false;

            s_Candidates.Clear();
            root.Query<VisualElement>().ForEach(e =>
            {
                if (IsCandidate(e, from)) s_Candidates.Add(e);
            });

            VisualElement best = null;
            var bestScore = float.MaxValue;
            foreach (var c in s_Candidates)
            {
                var r = c.worldBound;
                var delta = r.center - origin.center;
                // Distance sur l'axe de la direction, bord à bord.
                float along;
                if (dir.x > 0) along = r.xMin - origin.xMax;
                else if (dir.x < 0) along = origin.xMin - r.xMax;
                else if (dir.y > 0) along = r.yMin - origin.yMax;
                else along = origin.yMin - r.yMax;
                var forward = Vector2.Dot(delta, dir);
                if (forward <= 1f || along < -Mathf.Min(origin.height, origin.width) * 0.5f) continue;
                var across = Mathf.Abs(dir.x != 0 ? delta.y : delta.x);
                var score = Mathf.Max(0f, along) + across * 2f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            s_Candidates.Clear();
            if (best == null) return false;
            best.Focus();
            return true;
        }

        static bool IsCandidate(VisualElement e, VisualElement from)
        {
            if (e == from || !e.focusable || !e.canGrabFocus || e.tabIndex < 0 || !e.enabledInHierarchy) return false;
            if (from.Contains(e) || e.Contains(from)) return false;
            var r = e.worldBound;
            if (float.IsNaN(r.width) || r.width < 1f || r.height < 1f) return false;
            // Élément interne d'un contrôle qui délègue son focus (ex. saisie d'un TextField) : on garde le contrôle.
            for (var p = e.hierarchy.parent; p != null; p = p.hierarchy.parent)
            {
                if (p.focusable && p.delegatesFocus) return false;
                if (p.resolvedStyle.display == DisplayStyle.None || p.resolvedStyle.visibility == Visibility.Hidden) return false;
            }
            return e.resolvedStyle.display != DisplayStyle.None && e.resolvedStyle.visibility != Visibility.Hidden;
        }

        static Vector2 ToVector(NavigationMoveEvent.Direction direction)
        {
            switch (direction)
            {
                case NavigationMoveEvent.Direction.Left: return Vector2.left;
                case NavigationMoveEvent.Direction.Right: return Vector2.right;
                case NavigationMoveEvent.Direction.Up: return Vector2.down;   // y vers le bas en UI Toolkit
                case NavigationMoveEvent.Direction.Down: return Vector2.up;
                default: return Vector2.zero;
            }
        }
    }
}
