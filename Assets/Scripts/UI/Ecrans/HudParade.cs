using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Jauge de parade du paladin (interface.md, « Jauge de parade » ; Docs/ui-v01.md) : petite barre sous le réticule,
    /// montrée seulement quand un coup parable vise le joueur local. L'impact prévu est au bord droit ; le curseur ivoire
    /// avance vers lui ; la fenêtre de parade (ivoire léger) et la fenêtre parfaite (or) sont marquées avant l'impact ; un
    /// fin repère note l'instant de l'appui. Issue : liseré or et « Parfaite » (parade parfaite), liseré ivoire (parade),
    /// atténué (bloqué), liseré rouge (touché), puis la jauge s'efface. Ni vert ni icône.
    /// Classe à part : EcranHud ne fait que la créer et l'appeler ; l'allure est dans Hud.uss (règles `.hud-parade`).
    /// Lit DonneesUI.Parade (IJaugeParade) ; absent : rien n'est affiché.
    public sealed class HudParade
    {
        readonly VisualElement m_Racine, m_Piste, m_Zone, m_Parfaite, m_Appui, m_Curseur;
        readonly Label m_Texte;
        ResultatParade m_Issue = (ResultatParade)(-1);

        public HudParade(VisualElement racine)
        {
            var hud = racine.Q("hud") ?? racine;
            m_Racine = Element("hud-parade");
            m_Racine.name = "parade";
            m_Piste = Element("hud-parade__piste");
            m_Zone = Element("hud-parade__zone");
            m_Parfaite = Element("hud-parade__parfaite");
            m_Appui = Element("hud-parade__appui");
            m_Curseur = Element("hud-parade__curseur");
            m_Texte = new Label("Parfaite") { pickingMode = PickingMode.Ignore };
            m_Texte.AddToClassList("hud-parade__texte");
            m_Piste.Add(m_Zone);
            m_Piste.Add(m_Parfaite);
            m_Piste.Add(m_Appui);
            m_Racine.Add(m_Piste);
            m_Racine.Add(m_Curseur);
            m_Racine.Add(m_Texte);
            var reticule = racine.Q("reticule");
            if (reticule != null && reticule.parent != null) reticule.parent.Insert(reticule.parent.IndexOf(reticule) + 1, m_Racine);
            else hud.Add(m_Racine);
            m_Racine.style.display = DisplayStyle.None;
        }

        static VisualElement Element(string classe)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList(classe);
            return e;
        }

        /// Chaque image : `cache` vrai (mort) masque la jauge.
        public void Maj(bool cache)
        {
            var j = DonneesUI.Parade;
            bool vu = !cache && j != null && j.Visible;
            m_Racine.style.display = vu ? DisplayStyle.Flex : DisplayStyle.None;
            if (!vu) return;
            float duree = Mathf.Max(0.1f, j.Duree);
            m_Zone.style.width = Length.Percent(Mathf.Clamp01(j.FenetreParade / duree) * 100f);
            m_Parfaite.style.width = Length.Percent(Mathf.Clamp01(j.FenetreParfaite / duree) * 100f);
            m_Curseur.style.left = Length.Percent(Position(j.AvantImpact, duree));
            bool appui = j.Appui >= 0f;
            m_Appui.style.display = appui ? DisplayStyle.Flex : DisplayStyle.None;
            if (appui) m_Appui.style.left = Length.Percent(Position(j.Appui, duree));

            var r = j.Resultat;
            if (r != m_Issue)
            {
                m_Issue = r;
                m_Racine.EnableInClassList("hud-parade--parfaite", r == ResultatParade.Parfaite);
                m_Racine.EnableInClassList("hud-parade--parade", r == ResultatParade.Parade);
                m_Racine.EnableInClassList("hud-parade--bloque", r == ResultatParade.Bloque);
                m_Racine.EnableInClassList("hud-parade--touche", r == ResultatParade.Touche);
                m_Texte.style.display = r == ResultatParade.Parfaite ? DisplayStyle.Flex : DisplayStyle.None;
            }
            // Issue connue : petit « pop » (parfaite), puis la jauge s'efface.
            float k = r == ResultatParade.Aucun ? 0f : Mathf.Clamp01(j.DepuisResultat / 0.45f);
            float pop = r == ResultatParade.Parfaite ? 1f + 0.12f * Mathf.Sin(Mathf.Clamp01(j.DepuisResultat / 0.18f) * Mathf.PI) : 1f;
            m_Racine.style.scale = new Scale(new Vector3(pop, pop, 1f));
            m_Racine.style.opacity = r == ResultatParade.Aucun ? 1f : 1f - k * k;
        }

        /// Position du curseur (%, 0 à gauche, 100 à l'impact) pour `avant` secondes avant l'impact.
        static float Position(float avant, float duree) => Mathf.Clamp01(1f - avant / duree) * 100f;
    }
}
