using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Section « Afflictions » du menu du personnage (interface.md, « Statuts ») : un bouton par statut actif du joueur
    /// (icône, secondes restantes, jauge de durée), « Aucune affliction. » sinon. Survol à la souris ou focus à la
    /// manette et au clavier : infobulle (nom, effet, durée restante, source) posée à côté du bouton, mise à jour à chaque
    /// image. Navigation (Docs/ui-socle.md) : gauche et droite passent d'un statut à l'autre ; à droite du dernier, la
    /// première amélioration de compétence ; à gauche d'une amélioration, le dernier statut visité. Si le statut qui a le
    /// focus s'achève, le focus revient à la première amélioration.
    public sealed class SectionAfflictions
    {
        sealed class Bouton
        {
            public Button bouton;
            public VisualElement icone, jauge, remplissage;
            public Label temps;
            public string iconePosee;
            public IStatutAffiche statut;
            public bool visible;
        }

        public const int Max = 8;

        readonly VisualElement m_Liste, m_Racine;
        readonly Label m_Vide;
        readonly List<Bouton> m_Boutons = new List<Bouton>();
        readonly VisualElement m_Bulle;
        readonly Label m_BulleNom, m_BulleEffet, m_BulleDuree, m_BulleSource;
        IList<VisualElement> m_Ameliorations;
        Bouton m_Survol, m_Focus, m_Dernier;

        /// `liste` : conteneur des boutons (« perso-afflictions ») ; `vide` : texte « Aucune affliction. » ; `racine` :
        /// racine de l'écran (l'infobulle y est posée en position absolue).
        public SectionAfflictions(VisualElement liste, Label vide, VisualElement racine)
        {
            m_Liste = liste;
            m_Vide = vide;
            m_Racine = racine;
            m_Bulle = new VisualElement { name = "perso-infobulle", pickingMode = PickingMode.Ignore };
            m_Bulle.AddToClassList("dl-panel");
            m_Bulle.AddToClassList("perso__bulle");
            m_BulleNom = NouveauTexte("perso__bulle-nom");
            m_BulleEffet = NouveauTexte("perso__bulle-effet");
            m_BulleDuree = NouveauTexte("perso__bulle-ligne");
            m_BulleSource = NouveauTexte("perso__bulle-ligne");
            m_Bulle.style.display = DisplayStyle.None;
            racine?.Add(m_Bulle);
        }

        Label NouveauTexte(string classe)
        {
            var l = new Label { pickingMode = PickingMode.Ignore };
            l.AddToClassList("dl-text");
            l.AddToClassList(classe);
            m_Bulle.Add(l);
            return l;
        }

        /// Lignes des améliorations (dans l'ordre) : navigation gauche / droite entre les deux sections.
        public void Lier(IList<VisualElement> ameliorations)
        {
            m_Ameliorations = ameliorations;
            if (ameliorations == null) return;
            foreach (var a in ameliorations)
                a.RegisterCallback<NavigationMoveEvent>(evt =>
                {
                    if (evt.direction != NavigationMoveEvent.Direction.Left) return;
                    var cible = m_Dernier != null && m_Dernier.visible ? m_Dernier : PremierVisible();
                    if (cible == null) return;   // aucun statut : navigation normale
                    cible.bouton.Focus();
                    evt.StopPropagation();
                    (evt.currentTarget as VisualElement)?.focusController?.IgnoreEvent(evt);
                });
        }

        /// À l'ouverture du menu : pas d'infobulle restée ouverte.
        public void Reinitialiser()
        {
            m_Survol = m_Focus = null;
            m_Bulle.style.display = DisplayStyle.None;
        }

        public void Maj(IReadOnlyList<IStatutAffiche> statuts)
        {
            if (m_Liste == null) return;
            int n = statuts == null ? 0 : Mathf.Min(statuts.Count, Max);
            while (m_Boutons.Count < n) Creer();
            bool focusPerdu = false;
            for (int i = 0; i < m_Boutons.Count; i++)
            {
                var b = m_Boutons[i];
                bool vu = i < n;
                if (b.visible && !vu && (m_Focus == b)) focusPerdu = true;
                b.visible = vu;
                b.bouton.style.display = vu ? DisplayStyle.Flex : DisplayStyle.None;
                b.statut = vu ? statuts[i] : null;
                if (!vu) continue;
                var s = b.statut;
                if (b.iconePosee != s.Icone) { b.iconePosee = s.Icone; IconesUI.Poser(b.icone, s.Icone); }
                b.bouton.EnableInClassList("perso__affliction--bienfait", !s.Nefaste);
                bool duree = s.Restant >= 0f && s.Duree > 0f;
                b.temps.text = duree ? CaseStatut.Secondes(s.Restant) + " s" : "–";
                b.jauge.style.visibility = duree ? Visibility.Visible : Visibility.Hidden;
                if (duree) b.remplissage.style.width = Length.Percent(Mathf.Clamp01(s.Restant / s.Duree) * 100f);
            }
            if (m_Vide != null) m_Vide.style.display = n == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_Survol != null && !m_Survol.visible) m_Survol = null;
            if (m_Focus != null && !m_Focus.visible) m_Focus = null;
            if (focusPerdu && m_Ameliorations != null && m_Ameliorations.Count > 0) UINavigation.Focus(m_Ameliorations[0]);
            MajBulle();
        }

        Bouton PremierVisible()
        {
            foreach (var b in m_Boutons) if (b.visible) return b;
            return null;
        }

        void Creer()
        {
            int index = m_Boutons.Count;
            var b = new Bouton { bouton = new Button { name = "perso-affliction-" + index } };
            b.bouton.AddToClassList("perso__affliction");
            b.icone = new VisualElement { pickingMode = PickingMode.Ignore };
            b.icone.AddToClassList("dl-icone");
            b.icone.AddToClassList("perso__affliction-icone");
            b.temps = new Label { pickingMode = PickingMode.Ignore };
            b.temps.AddToClassList("perso__affliction-temps");
            b.jauge = new VisualElement { pickingMode = PickingMode.Ignore };
            b.jauge.AddToClassList("perso__affliction-jauge");
            b.remplissage = new VisualElement { pickingMode = PickingMode.Ignore };
            b.remplissage.AddToClassList("perso__affliction-jauge-remplie");
            b.jauge.Add(b.remplissage);
            b.bouton.Add(b.icone);
            b.bouton.Add(b.jauge);
            b.bouton.Add(b.temps);
            b.bouton.RegisterCallback<PointerEnterEvent>(_ => { m_Survol = b; m_Dernier = b; MajBulle(); });
            b.bouton.RegisterCallback<PointerLeaveEvent>(_ => { if (m_Survol == b) m_Survol = null; MajBulle(); });
            b.bouton.RegisterCallback<FocusInEvent>(_ => { m_Focus = b; m_Dernier = b; MajBulle(); });
            b.bouton.RegisterCallback<FocusOutEvent>(_ => { if (m_Focus == b) m_Focus = null; MajBulle(); });
            b.bouton.RegisterCallback<NavigationMoveEvent>(evt => Naviguer(b, evt));
            m_Liste.Add(b.bouton);
            m_Boutons.Add(b);
        }

        void Naviguer(Bouton b, NavigationMoveEvent evt)
        {
            bool gauche = evt.direction == NavigationMoveEvent.Direction.Left;
            bool droite = evt.direction == NavigationMoveEvent.Direction.Right;
            if (!gauche && !droite) return;   // haut, bas : navigation normale
            int i = m_Boutons.IndexOf(b);
            VisualElement cible = null;
            if (gauche && i > 0) cible = m_Boutons[i - 1].bouton;
            else if (droite && i + 1 < m_Boutons.Count && m_Boutons[i + 1].visible) cible = m_Boutons[i + 1].bouton;
            else if (droite && m_Ameliorations != null && m_Ameliorations.Count > 0) cible = m_Ameliorations[0];
            if (cible != null) cible.Focus();
            evt.StopPropagation();
            (evt.currentTarget as VisualElement)?.focusController?.IgnoreEvent(evt);
        }

        /// Infobulle du statut survolé (souris), sinon de celui qui a le focus.
        void MajBulle()
        {
            var b = m_Survol ?? m_Focus;
            if (b == null || b.statut == null || m_Racine == null || m_Racine.panel == null)
            {
                m_Bulle.style.display = DisplayStyle.None;
                return;
            }
            var s = b.statut;
            m_BulleNom.text = s.Nom;
            m_BulleNom.EnableInClassList("perso__bulle-nom--bienfait", !s.Nefaste);
            m_BulleEffet.text = s.Effet;
            m_BulleDuree.text = s.Restant >= 0f ? "Durée restante : " + CaseStatut.Secondes(s.Restant) + " s" : "Durée : tant que la cause dure";
            m_BulleSource.text = "Source : " + (string.IsNullOrEmpty(s.Source) ? "inconnue" : s.Source);
            m_Bulle.style.display = DisplayStyle.Flex;

            // Sous le bouton, alignée sur lui (sans cacher les statuts voisins) ; au-dessus s'il n'y a pas la place.
            var zone = m_Racine.worldBound;
            var wb = b.bouton.worldBound;
            float largeur = m_Bulle.resolvedStyle.width > 1f ? m_Bulle.resolvedStyle.width : 480f;
            float hauteur = m_Bulle.resolvedStyle.height > 1f ? m_Bulle.resolvedStyle.height : 200f;
            var bas = m_Racine.WorldToLocal(new Vector2(wb.xMin, wb.yMax + 9f));
            var haut = m_Racine.WorldToLocal(new Vector2(wb.xMin, wb.yMin - 9f));
            var coin = m_Racine.WorldToLocal(new Vector2(zone.xMax, zone.yMax));
            float y = bas.y + hauteur <= coin.y - 12f ? bas.y : haut.y - hauteur;
            float x = Mathf.Min(bas.x, coin.x - largeur - 12f);
            m_Bulle.style.left = Mathf.Max(12f, x);
            m_Bulle.style.top = Mathf.Max(12f, y);
        }
    }
}
