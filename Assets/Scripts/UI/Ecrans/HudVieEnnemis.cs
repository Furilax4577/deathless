using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Barres de vie des ennemis (interface.md § « Barres de vie des ennemis ») : barre fine au-dessus des ennemis
    /// communs et élites (même ancre que la rangée de statuts, HudStatuts.cs : StatutsUI.MargeTete côté jeu, mêmes
    /// limites de distance et d'estompage), et méga barre du/des boss (Morgrim, Nyxar), en haut au centre sous la
    /// barre de Nyxessa (Hud.uxml, `boss-zone`, variante B « ornée » choisie par Quentin le 27/09/2026). Classe à
    /// part : EcranHud ne fait que la créer et l'appeler. Lit DonneesUI.VieEnnemis (IEtatVieEnnemis) à chaque image ;
    /// absent : rien n'est affiché.
    public sealed class HudVieEnnemis
    {
        public const float DureeDeploiement = 0.8f, DureeRepli = 0.8f;

        readonly VisualElement m_Racine;
        readonly VisualElement m_CalqueEnnemis;
        readonly List<Barre> m_Barres = new List<Barre>();
        readonly EmplacementBoss[] m_Boss = new EmplacementBoss[2];

        sealed class Barre
        {
            public VisualElement racine, remplissage;
        }

        /// Un emplacement empilé de la méga barre (boss1 ou boss2 dans Hud.uxml).
        sealed class EmplacementBoss
        {
            public VisualElement deploiement, cartouche, statutsRacine;
            public Label nom;
            public VisualElement[] segmentsRemplissage;
            public RangeeStatuts statuts;
            /// Cle du boss actuellement affiché (ou en train de se replier) ; -1 : emplacement libre.
            public int cle = -1;
            /// 0 replié à 1 pleinement déployé.
            public float progres;
        }

        public HudVieEnnemis(VisualElement racine)
        {
            m_Racine = racine;
            var hud = racine.Q("hud") ?? racine;
            m_CalqueEnnemis = racine.Q("vie-ennemis");
            if (m_CalqueEnnemis == null)
            {
                m_CalqueEnnemis = new VisualElement { name = "vie-ennemis" };
                m_CalqueEnnemis.AddToClassList("hud-statuts-ennemis");
                hud.Insert(0, m_CalqueEnnemis);
            }
            m_CalqueEnnemis.pickingMode = PickingMode.Ignore;

            for (int i = 0; i < m_Boss.Length; i++)
            {
                var prefixe = "boss" + (i + 1);
                var e = new EmplacementBoss
                {
                    deploiement = racine.Q(prefixe + "-deploiement"),
                    cartouche = racine.Q(prefixe + "-cartouche"),
                    nom = racine.Q<Label>(prefixe + "-nom"),
                    statutsRacine = racine.Q(prefixe + "-statuts"),
                    segmentsRemplissage = new VisualElement[4],
                };
                for (int s = 0; s < e.segmentsRemplissage.Length; s++)
                    e.segmentsRemplissage[s] = racine.Q(prefixe + "-seg" + s + "-remplissage");
                if (e.statutsRacine != null) e.statuts = new RangeeStatuts(e.statutsRacine, true, 4);
                m_Boss[i] = e;
            }
        }

        public void Maj(float dt)
        {
            var source = DonneesUI.VieEnnemis;
            MajEnnemis(source != null ? source.Ennemis : null);
            MajBoss(source != null ? source.Boss : null, dt);
        }

        void MajEnnemis(IReadOnlyList<IEnnemiVie> ennemis)
        {
            var cam = ennemis != null && ennemis.Count > 0 ? Camera.main : null;
            var panel = m_Racine.panel;
            int k = 0;
            if (cam != null && panel != null)
            {
                for (int i = 0; i < ennemis.Count && k < HudStatuts.EnnemisMax; i++)
                {
                    var e = ennemis[i];
                    if (!e.Visible) continue;
                    var v = cam.WorldToViewportPoint(e.PositionTete);
                    if (v.z < 0.5f || v.z > HudStatuts.DistanceEnnemis || v.x < -0.05f || v.x > 1.05f || v.y < -0.05f || v.y > 1.2f) continue;
                    if (k >= m_Barres.Count)
                    {
                        var b = new Barre { racine = new VisualElement { pickingMode = PickingMode.Ignore } };
                        b.racine.AddToClassList("mvi-barre");
                        b.racine.AddToClassList("hud-vie-barre");
                        b.remplissage = new VisualElement { pickingMode = PickingMode.Ignore };
                        b.remplissage.AddToClassList("mvi-barre__remplissage");
                        b.racine.Add(b.remplissage);
                        m_CalqueEnnemis.Add(b.racine);
                        m_Barres.Add(b);
                    }
                    var barre = m_Barres[k++];
                    barre.racine.style.display = DisplayStyle.Flex;
                    barre.racine.EnableInClassList("mvi-barre--elite", e.Elite);
                    barre.remplissage.style.width = Length.Percent(Mathf.Clamp01(e.Vie) * 100f);
                    var p = RuntimePanelUtils.CameraTransformWorldToPanel(panel, e.PositionTete, cam);
                    barre.racine.style.left = p.x;
                    barre.racine.style.top = p.y;
                    barre.racine.style.opacity = 1f - Mathf.Clamp01((v.z - HudStatuts.DistanceEnnemis * 0.8f) / (HudStatuts.DistanceEnnemis * 0.2f));
                }
            }
            for (int i = k; i < m_Barres.Count; i++) m_Barres[i].racine.style.display = DisplayStyle.None;
        }

        void MajBoss(IReadOnlyList<IBossVie> boss, float dt)
        {
            for (int i = 0; i < m_Boss.Length; i++)
            {
                var e = m_Boss[i];
                IBossVie b = null;
                if (boss != null)
                    for (int j = 0; j < boss.Count; j++)
                        if (boss[j].Cle == e.cle) { b = boss[j]; break; }
                if (b == null && e.cle < 0 && boss != null)
                {
                    // Emplacement libre : prend un boss de la liste pas déjà pris par l'autre emplacement.
                    for (int j = 0; j < boss.Count; j++)
                    {
                        bool pris = false;
                        for (int o = 0; o < m_Boss.Length; o++)
                            if (o != i && m_Boss[o].cle == boss[j].Cle) { pris = true; break; }
                        if (!pris) { b = boss[j]; e.cle = b.Cle; break; }
                    }
                }
                AppliquerBoss(e, b, dt);
            }
        }

        void AppliquerBoss(EmplacementBoss e, IBossVie b, float dt)
        {
            if (e.deploiement == null) return;
            bool cible = b != null && b.Vivant;
            float duree = cible ? DureeDeploiement : DureeRepli;
            float pas = dt / Mathf.Max(0.05f, duree);
            e.progres = Mathf.Clamp01(cible ? e.progres + pas : e.progres - pas);

            e.deploiement.style.width = Length.Percent(e.progres * 100f);
            // Le nom (et le cartouche) s'estompe dans la deuxième moitié du déploiement (comme la maquette).
            var opacite = Mathf.Clamp01((e.progres - 0.4f) / 0.6f);
            if (e.nom != null)
            {
                e.nom.style.opacity = opacite;
                if (b != null) e.nom.text = b.Nom;
            }
            if (e.cartouche != null) e.cartouche.style.opacity = opacite;
            var statutsVisibles = b != null && e.progres > 0.95f;
            if (e.statutsRacine != null) e.statutsRacine.style.display = statutsVisibles ? DisplayStyle.Flex : DisplayStyle.None;
            if (e.statuts != null) e.statuts.Maj(b != null ? b.Statuts : null);

            float vie = b != null ? b.Vie : 0f;
            for (int s = 0; s < e.segmentsRemplissage.Length; s++)
            {
                if (e.segmentsRemplissage[s] == null) continue;
                float bas = (float)s / e.segmentsRemplissage.Length;
                float rempli = Mathf.Clamp01((vie - bas) * e.segmentsRemplissage.Length);
                e.segmentsRemplissage[s].style.width = Length.Percent(rempli * 100f);
            }

            // Le boss a quitté la liste (désintégré) et l'emplacement est pleinement replié : le libère.
            if (b == null && e.progres <= 0f) e.cle = -1;
        }
    }
}
