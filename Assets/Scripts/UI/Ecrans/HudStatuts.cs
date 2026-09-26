using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Case d'un statut : icône (table IconesUI), liseré rouge (affliction) ou or (bienfait), jauge de durée en bas et,
    /// en grand format, secondes restantes. Sans durée (eau du donjon) : ni jauge ni secondes. Réutilisée d'image en image.
    public sealed class CaseStatut
    {
        public readonly VisualElement racine;
        readonly VisualElement m_Icone, m_Jauge, m_Remplissage;
        readonly Label m_Temps;
        string m_IconePosee;

        public CaseStatut(bool petit)
        {
            racine = new VisualElement { pickingMode = PickingMode.Ignore };
            racine.AddToClassList("statut");
            if (petit) racine.AddToClassList("statut--petit");
            m_Icone = new VisualElement { pickingMode = PickingMode.Ignore };
            m_Icone.AddToClassList("dl-icone");
            m_Icone.AddToClassList("statut__icone");
            m_Jauge = new VisualElement { pickingMode = PickingMode.Ignore };
            m_Jauge.AddToClassList("statut__jauge");
            m_Remplissage = new VisualElement { pickingMode = PickingMode.Ignore };
            m_Remplissage.AddToClassList("statut__jauge-remplie");
            m_Jauge.Add(m_Remplissage);
            racine.Add(m_Icone);
            racine.Add(m_Jauge);
            if (!petit)
            {
                m_Temps = new Label { pickingMode = PickingMode.Ignore };
                m_Temps.AddToClassList("statut__temps");
                racine.Add(m_Temps);
            }
        }

        public void Poser(IStatutAffiche s)
        {
            if (m_IconePosee != s.Icone)
            {
                m_IconePosee = s.Icone;
                IconesUI.Poser(m_Icone, s.Icone);
            }
            racine.EnableInClassList("statut--bienfait", !s.Nefaste);
            bool duree = s.Restant >= 0f && s.Duree > 0f;
            m_Jauge.style.display = duree ? DisplayStyle.Flex : DisplayStyle.None;
            if (duree) m_Remplissage.style.width = Length.Percent(Mathf.Clamp01(s.Restant / s.Duree) * 100f);
            if (m_Temps != null)
            {
                m_Temps.style.display = duree ? DisplayStyle.Flex : DisplayStyle.None;
                if (duree) m_Temps.text = Secondes(s.Restant);
            }
        }

        /// 12 → « 12 », 0,4 → « 0,4 » (une décimale sous 1 s, comme les recharges du HUD).
        public static string Secondes(float s) => s >= 1f ? Mathf.CeilToInt(s).ToString() : s.ToString("0.0");
    }

    /// Rangée de cases de statut (joueur, ou au-dessus d'un ennemi) : montre les `max` premiers statuts de la liste.
    public sealed class RangeeStatuts
    {
        public readonly VisualElement racine;
        readonly List<CaseStatut> m_Cases = new List<CaseStatut>();
        readonly bool m_Petit;
        readonly int m_Max;

        public RangeeStatuts(VisualElement racine, bool petit, int max)
        {
            this.racine = racine;
            m_Petit = petit;
            m_Max = max;
        }

        /// Renvoie le nombre de cases visibles.
        public int Maj(IReadOnlyList<IStatutAffiche> liste)
        {
            int n = liste == null ? 0 : Mathf.Min(liste.Count, m_Max);
            while (m_Cases.Count < n)
            {
                var c = new CaseStatut(m_Petit);
                m_Cases.Add(c);
                racine.Add(c.racine);
            }
            for (int i = 0; i < m_Cases.Count; i++)
            {
                bool vu = i < n;
                m_Cases[i].racine.style.display = vu ? DisplayStyle.Flex : DisplayStyle.None;
                if (vu) m_Cases[i].Poser(liste[i]);
            }
            return n;
        }
    }

    /// Statuts dans le HUD (interface.md, « Statuts ») : rangée du joueur (bas, à gauche, au-dessus du portrait et des
    /// barres ; élément « statuts ») et petites rangées au-dessus des ennemis affectés, placées chaque image en espace
    /// écran comme les pseudos (calque « statuts-ennemis »). Séparé d'EcranHud, qui ne fait que le créer et l'appeler :
    /// déplacer la rangée du joueur ne demande que Hud.uss (règles `.hud-statuts`). Les deux éléments sont créés ici
    /// s'ils manquent dans Hud.uxml. Lit DonneesUI.Statuts (IEtatStatuts) ; absent : rien n'est affiché.
    public sealed class HudStatuts
    {
        /// Distance (profondeur de caméra, m) au-delà de laquelle les statuts d'un ennemi ne sont plus montrés ; ils
        /// s'estompent sur le dernier cinquième.
        public static float DistanceEnnemis = 30f;
        public const int MaxJoueur = 6, MaxParEnnemi = 4, EnnemisMax = 24;

        readonly VisualElement m_Racine;
        readonly RangeeStatuts m_Joueur;
        readonly VisualElement m_Calque;
        readonly List<RangeeStatuts> m_Ennemis = new List<RangeeStatuts>();

        public HudStatuts(VisualElement racine)
        {
            m_Racine = racine;
            var hud = racine.Q("hud") ?? racine;
            var joueur = racine.Q("statuts");
            if (joueur == null)
            {
                joueur = new VisualElement { name = "statuts" };
                joueur.AddToClassList("hud-statuts");
                hud.Add(joueur);
            }
            joueur.pickingMode = PickingMode.Ignore;
            m_Joueur = new RangeeStatuts(joueur, false, MaxJoueur);
            m_Calque = racine.Q("statuts-ennemis");
            if (m_Calque == null)
            {
                m_Calque = new VisualElement { name = "statuts-ennemis" };
                m_Calque.AddToClassList("hud-statuts-ennemis");
                hud.Insert(0, m_Calque);   // sous le reste du HUD, comme les pseudos
            }
            m_Calque.pickingMode = PickingMode.Ignore;
        }

        /// Chaque image : `cache` vrai (mort) masque la rangée du joueur.
        public void Maj(bool cache)
        {
            var source = DonneesUI.Statuts;
            int n = m_Joueur.Maj(source != null && !cache ? source.StatutsJoueur : null);
            m_Joueur.racine.style.display = n > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            MajEnnemis(source != null ? source.EnnemisAffectes : null);
        }

        void MajEnnemis(IReadOnlyList<IEnnemiAffecte> ennemis)
        {
            var cam = ennemis != null && ennemis.Count > 0 ? Camera.main : null;
            var panel = m_Racine.panel;
            int k = 0;
            if (cam != null && panel != null)
            {
                for (int i = 0; i < ennemis.Count && k < EnnemisMax; i++)
                {
                    var e = ennemis[i];
                    if (!e.Visible || e.Statuts == null || e.Statuts.Count == 0) continue;
                    var v = cam.WorldToViewportPoint(e.PositionTete);
                    if (v.z < 0.5f || v.z > DistanceEnnemis || v.x < -0.05f || v.x > 1.05f || v.y < -0.05f || v.y > 1.2f) continue;
                    if (k >= m_Ennemis.Count)
                    {
                        var r = new VisualElement { pickingMode = PickingMode.Ignore };
                        r.AddToClassList("hud-statuts-ennemi");
                        m_Calque.Add(r);
                        m_Ennemis.Add(new RangeeStatuts(r, true, MaxParEnnemi));
                    }
                    var rangee = m_Ennemis[k++];
                    rangee.Maj(e.Statuts);
                    var p = RuntimePanelUtils.CameraTransformWorldToPanel(panel, e.PositionTete, cam);
                    var s = rangee.racine.style;
                    s.display = DisplayStyle.Flex;
                    s.left = p.x;
                    s.top = p.y;
                    s.opacity = 1f - Mathf.Clamp01((v.z - DistanceEnnemis * 0.8f) / (DistanceEnnemis * 0.2f));
                }
            }
            for (int i = k; i < m_Ennemis.Count; i++) m_Ennemis[i].racine.style.display = DisplayStyle.None;
        }
    }
}
