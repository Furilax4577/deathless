using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// Potions, clés et crochets du joueur local (03/10/2026 ; wiki : interface.md, commandes.md) : à droite de la barre de vie,
    /// trois pastilles de potion (santé, mana, endurance) avec leur quantité et l'invite de leur bouton (croix haut / 1, gauche /
    /// 2, droite / 3, qui suivent le dernier appareil) ; la pastille de mana n'existe que pour le Mage ; une pastille grisée
    /// veut dire « plus de potion ». Dessous, quand il y en a, une puce par sorte de clé (bronze, argent, or) et pour les crochets.
    /// Lit DonneesUI.Inventaire (IInventaireJoueur) ; absent : rien n'est affiché. Classe à part : EcranHud la crée et l'appelle ;
    /// l'allure est dans Hud.uss (`.hud-sac`).
    public sealed class HudPotions
    {
        sealed class Pastille
        {
            public VisualElement racine;
            public Label nombre;
            public int vu = -1;
        }

        static readonly string[] s_IconesPotions = { "commun_potion_soin", "commun_potion_mana", "commun_potion_endurance" };
        static readonly string[] s_ActionsPotions = { "Gameplay/DrinkPotion", "Gameplay/DrinkPotionMana", "Gameplay/DrinkPotionStamina" };
        static readonly string[] s_IconesCles = { "commun_cle_bronze", "commun_cle_argent", "commun_cle_or", "commun_crochets" };

        readonly VisualElement m_Racine, m_RangeeCles;
        readonly Pastille[] m_Potions = new Pastille[Deathless.UI.Donnees.InventaireTaille.Potions];
        readonly Pastille[] m_Cles = new Pastille[Deathless.UI.Donnees.InventaireTaille.Cles + 1];
        bool m_Vu;

        public HudPotions(VisualElement racine)
        {
            var hud = racine.Q("hud") ?? racine;
            m_Racine = new VisualElement { name = "sac", pickingMode = PickingMode.Ignore };
            m_Racine.AddToClassList("hud-sac");
            m_Racine.style.display = DisplayStyle.None;
            var rangee = new VisualElement { pickingMode = PickingMode.Ignore };
            rangee.AddToClassList("hud-sac__potions");
            for (int i = 0; i < m_Potions.Length; i++)
            {
                var p = new Pastille { racine = new VisualElement { name = "sac-potion-" + i, pickingMode = PickingMode.Ignore } };
                p.racine.AddToClassList("hud-sac__potion");
                var caseP = new VisualElement { pickingMode = PickingMode.Ignore };
                caseP.AddToClassList("hud-sac__case");
                caseP.Add(IconesUI.Creer(s_IconesPotions[i], "hud-sac__icone"));
                p.nombre = new Label("0") { pickingMode = PickingMode.Ignore };
                p.nombre.AddToClassList("hud-sac__nombre");
                caseP.Add(p.nombre);
                p.racine.Add(caseP);
                var invite = new InputPrompt(s_ActionsPotions[i], "") { pickingMode = PickingMode.Ignore };
                invite.AddToClassList("hud-emplacement__invite");
                p.racine.Add(invite);
                rangee.Add(p.racine);
                m_Potions[i] = p;
            }
            m_Racine.Add(rangee);
            m_RangeeCles = new VisualElement { pickingMode = PickingMode.Ignore };
            m_RangeeCles.AddToClassList("hud-sac__cles");
            for (int i = 0; i < m_Cles.Length; i++)
            {
                var p = new Pastille { racine = new VisualElement { name = "sac-cle-" + i, pickingMode = PickingMode.Ignore } };
                p.racine.AddToClassList("hud-sac__cle");
                p.racine.Add(IconesUI.Creer(s_IconesCles[i], "hud-sac__cle-icone"));
                p.nombre = new Label("0") { pickingMode = PickingMode.Ignore };
                p.nombre.AddToClassList("hud-sac__cle-nombre");
                p.racine.Add(p.nombre);
                m_RangeeCles.Add(p.racine);
                m_Cles[i] = p;
            }
            m_Racine.Add(m_RangeeCles);
            var joueur = racine.Q(className: "hud-joueur");
            if (joueur != null) joueur.Add(m_Racine); else hud.Add(m_Racine);
        }

        /// Chaque image : `cache` vrai (mort) masque tout.
        public void Maj(bool cache)
        {
            var inv = DonneesUI.Inventaire;
            bool vu = !cache && inv != null;
            if (vu != m_Vu) { m_Vu = vu; m_Racine.style.display = vu ? DisplayStyle.Flex : DisplayStyle.None; }
            if (!vu) return;
            for (int i = 0; i < m_Potions.Length; i++)
            {
                var p = m_Potions[i];
                bool dispo = inv.PotionUtilisable(i);
                p.racine.style.display = dispo ? DisplayStyle.Flex : DisplayStyle.None;
                int n = inv.Potions(i);
                if (n != p.vu) { p.vu = n; p.nombre.text = n.ToString(); }
                p.racine.EnableInClassList("hud-sac__potion--vide", n <= 0);
                p.racine.EnableInClassList("hud-sac__potion--max", n >= inv.PotionsMax);
            }
            bool tout = false;
            for (int i = 0; i < m_Cles.Length; i++)
            {
                var p = m_Cles[i];
                int n = i < Deathless.UI.Donnees.InventaireTaille.Cles ? inv.Cles(i) : inv.Crochets;
                if (n != p.vu) { p.vu = n; p.nombre.text = i < Deathless.UI.Donnees.InventaireTaille.Cles ? "×" + n : n.ToString(); }
                p.racine.style.display = n > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                tout |= n > 0;
            }
            m_RangeeCles.style.display = tout ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
