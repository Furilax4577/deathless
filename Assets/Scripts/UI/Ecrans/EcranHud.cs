using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// HUD en jeu (interface.md) : Nyxessa et son bouclier, temps restant, vote prêt, alerte avant la nuit,
    /// bannière « NUIT N », indicateur « Nyxessa attaquée », or, joueur, compétences, interaction, mort.
    /// Lit DonneesUI.Partie et DonneesUI.Joueur à chaque image.
    public class EcranHud : Ecran
    {
        /// Délai d'alerte avant la nuit (deroule.md : 15 s avant le crépuscule, à équilibrer).
        public static float DelaiAlerteNuit = 15f;

        /// Durée d'affichage de l'indicateur « Nyxessa attaquée » après le dernier coup.
        public static float DureeAlerteAttaque = 3f;

        /// Durée de la bannière « NUIT N ».
        public static float DureeBanniere = 3.5f;

        public override bool CarteUI => false;
        public override bool Opaque => true;

        VisualElement m_VieNyx, m_PisteBouclier, m_Bouclier;
        Label m_Temps, m_Prets, m_AlerteBouclier, m_AlerteNuit, m_Or;
        VisualElement m_BlocPrets, m_BlocAlerteNuit;
        IconeJourNuit m_Icone;
        VisualElement m_Banniere;
        Label m_BanniereTitre;
        VisualElement m_Attaque;
        FlecheHud m_FlecheAttaque;
        VisualElement m_Portrait;
        Label m_Initiale;
        Gauge m_Vie, m_Endurance, m_JaugeClasse;
        VisualElement m_Furtif;
        JaugeClasse m_JaugeAffichee = (JaugeClasse)(-1);
        VisualElement m_Barre;
        VisualElement m_Interaction;
        Label m_InteractionTexte;
        VisualElement m_Reticule;
        VisualElement m_Mort;
        Label m_MortTexte;

        readonly List<Emplacement> m_Emplacements = new List<Emplacement>();
        IReadOnlyList<ICompetenceHud> m_CompetencesAffichees;
        IEtatPartie m_Partie;
        float m_TempsBanniere = -1f;
        float m_TempsAttaque = -1f;
        float m_Horloge;

        sealed class Emplacement
        {
            public VisualElement racine, case_, voile, icone;
            public Label abreviation, recharge;
            public InputPrompt invite;
        }

        protected override void Construire()
        {
            m_VieNyx = Racine.Q("nyx-vie");
            m_PisteBouclier = Racine.Q("nyx-piste-bouclier");
            m_Bouclier = Racine.Q("nyx-bouclier");
            m_Temps = Racine.Q<Label>("temps-texte");
            m_Icone = Racine.Q<IconeJourNuit>("temps-icone");
            m_BlocPrets = Racine.Q("prets");
            m_Prets = Racine.Q<Label>("prets-texte");
            m_AlerteBouclier = Racine.Q<Label>("alerte-bouclier");
            m_BlocAlerteNuit = Racine.Q("alerte-nuit");
            m_AlerteNuit = Racine.Q<Label>("alerte-nuit-texte");
            m_Or = Racine.Q<Label>("or-valeur");
            m_Banniere = Racine.Q("banniere");
            m_BanniereTitre = Racine.Q<Label>("banniere-titre");
            m_Attaque = Racine.Q("attaque");
            m_FlecheAttaque = Racine.Q<FlecheHud>("attaque-fleche");
            m_Portrait = Racine.Q("portrait");
            m_Initiale = Racine.Q<Label>("portrait-initiale");
            m_Vie = Racine.Q<Gauge>("joueur-vie");
            m_Endurance = Racine.Q<Gauge>("joueur-endurance");
            m_JaugeClasse = Racine.Q<Gauge>("joueur-jauge");
            m_Furtif = Racine.Q("furtif");
            m_Barre = Racine.Q("competences");
            m_Interaction = Racine.Q("interaction");
            m_InteractionTexte = Racine.Q<Label>("interaction-texte");
            m_Reticule = Racine.Q("reticule");
            m_Mort = Racine.Q("mort");
            m_MortTexte = Racine.Q<Label>("mort-texte");
        }

        /// Appelé par le navigateur quand la source de la partie change.
        public void Suivre(IEtatPartie partie)
        {
            if (m_Partie == partie) return;
            if (m_Partie != null)
            {
                m_Partie.NuitCommencee -= OnNuit;
                m_Partie.NyxessaFrappee -= OnFrappee;
            }
            m_Partie = partie;
            if (m_Partie != null)
            {
                m_Partie.NuitCommencee += OnNuit;
                m_Partie.NyxessaFrappee += OnFrappee;
            }
            m_TempsBanniere = -1f;
            m_TempsAttaque = -1f;
        }

        void OnNuit(int nuit)
        {
            m_BanniereTitre.text = "NUIT " + nuit;
            m_TempsBanniere = 0f;
        }

        void OnFrappee() => m_TempsAttaque = 0f;

        public override void MiseAJour(float dt)
        {
            m_Horloge += dt;
            var partie = DonneesUI.Partie;
            var joueur = DonneesUI.Joueur;
            if (partie != null) MajPartie(partie, dt);
            if (joueur != null) MajJoueur(joueur, partie);
        }

        void MajPartie(IEtatPartie partie, float dt)
        {
            // Nyxessa et bouclier.
            var vie = partie.VieMaxNyxessa > 0 ? Mathf.Clamp01(partie.VieNyxessa / partie.VieMaxNyxessa) : 0f;
            m_VieNyx.style.width = Length.Percent(vie * 100f);
            var aBouclier = partie.BouclierMax > 0f && partie.Bouclier > 0f;
            m_PisteBouclier.style.display = aBouclier ? DisplayStyle.Flex : DisplayStyle.None;
            var ratio = aBouclier ? Mathf.Clamp01(partie.Bouclier / partie.BouclierMax) : 0f;
            if (aBouclier)
            {
                m_Bouclier.style.width = Length.Percent(ratio * 100f);
                m_Bouclier.EnableInClassList("hud-nyx__bouclier--entame", ratio < 0.6f && ratio >= 0.3f);
                m_Bouclier.EnableInClassList("hud-nyx__bouclier--critique", ratio < 0.3f);
            }
            var bouclierFaible = aBouclier && ratio < 0.5f;
            m_AlerteBouclier.style.display = bouclierFaible ? DisplayStyle.Flex : DisplayStyle.None;
            if (bouclierFaible) m_AlerteBouclier.text = "Bouclier de Nyxessa à " + Mathf.RoundToInt(ratio * 100f) + " %";

            // Temps.
            var reste = Horloge(partie.TempsRestantPhase);
            var nuit = partie.Phase == PhasePartie.Nuit || partie.Phase == PhasePartie.Crepuscule;
            m_Icone.nuit = nuit;
            switch (partie.Phase)
            {
                case PhasePartie.Jour: m_Temps.text = "Jour · " + reste + " avant la nuit"; break;
                case PhasePartie.Crepuscule: m_Temps.text = "Crépuscule · la nuit " + partie.NumeroNuit + " tombe"; break;
                case PhasePartie.Nuit: m_Temps.text = "Nuit " + partie.NumeroNuit + " · " + reste + " avant l’aube"; break;
                case PhasePartie.Aube: m_Temps.text = "Aube · le jour se lève"; break;
                default: m_Temps.text = ""; break;
            }

            // Vote prêt.
            var vote = partie.Phase == PhasePartie.Jour && partie.VoteActif;
            m_BlocPrets.style.display = vote ? DisplayStyle.Flex : DisplayStyle.None;
            if (vote) m_Prets.text = "Prêts " + partie.JoueursPrets + " / " + partie.JoueursTotal;

            // Alerte avant la nuit (clignote).
            var alerte = partie.Phase == PhasePartie.Jour && partie.TempsRestantPhase <= DelaiAlerteNuit;
            m_BlocAlerteNuit.style.display = alerte ? DisplayStyle.Flex : DisplayStyle.None;
            if (alerte)
            {
                m_AlerteNuit.text = "La nuit tombe dans " + Mathf.CeilToInt(partie.TempsRestantPhase) + " s";
                m_BlocAlerteNuit.EnableInClassList("hud-alerte-nuit--pulse", Mathf.Repeat(m_Horloge, 1f) < 0.5f);
            }

            // Or.
            m_Or.text = Milliers(partie.OrEquipe);

            // Bannière NUIT N.
            if (m_TempsBanniere >= 0f)
            {
                m_TempsBanniere += dt;
                var visible = m_TempsBanniere < DureeBanniere;
                m_Banniere.EnableInClassList("hud-banniere--visible", visible);
                if (!visible) m_TempsBanniere = -1f;
            }
            else m_Banniere.EnableInClassList("hud-banniere--visible", false);

            // Nyxessa attaquée : pastille au bord de l'écran, du côté de Nyxessa.
            if (m_TempsAttaque >= 0f)
            {
                m_TempsAttaque += dt;
                if (m_TempsAttaque > DureeAlerteAttaque) m_TempsAttaque = -1f;
            }
            var attaque = m_TempsAttaque >= 0f;
            m_Attaque.style.display = attaque ? DisplayStyle.Flex : DisplayStyle.None;
            if (attaque) PlacerAttaque(partie.AngleNyxessa);
        }

        /// Place l'indicateur au bord : devant (±35°) en haut, derrière (±145°) en bas, sinon à gauche ou à droite.
        void PlacerAttaque(float angle)
        {
            var s = m_Attaque.style;
            // Null : on rend la main aux classes USS (Auto en ligne écraserait leurs positions).
            s.left = s.right = s.top = s.bottom = StyleKeyword.Null;
            m_Attaque.RemoveFromClassList("hud-attaque--gauche");
            m_Attaque.RemoveFromClassList("hud-attaque--droite");
            m_Attaque.RemoveFromClassList("hud-attaque--haut");
            m_Attaque.RemoveFromClassList("hud-attaque--bas");
            var abs = Mathf.Abs(angle);
            if (abs <= 35f)
            {
                m_Attaque.AddToClassList("hud-attaque--haut");
                m_FlecheAttaque.angle = 270f;
            }
            else if (abs >= 145f)
            {
                m_Attaque.AddToClassList("hud-attaque--bas");
                m_FlecheAttaque.angle = 90f;
            }
            else if (angle < 0f)
            {
                m_Attaque.AddToClassList("hud-attaque--gauche");
                m_FlecheAttaque.angle = 180f;
                // Plus Nyxessa est derrière, plus la pastille descend le long du bord.
                s.top = Length.Percent(Mathf.Lerp(30f, 62f, Mathf.InverseLerp(35f, 145f, abs)));
            }
            else
            {
                m_Attaque.AddToClassList("hud-attaque--droite");
                m_FlecheAttaque.angle = 0f;
                s.top = Length.Percent(Mathf.Lerp(30f, 62f, Mathf.InverseLerp(35f, 145f, abs)));
            }
        }

        void MajJoueur(IEtatJoueur joueur, IEtatPartie partie)
        {
            m_Portrait.style.backgroundColor = joueur.TeinteClasse;
            m_Initiale.text = string.IsNullOrEmpty(joueur.Classe) ? "?" : joueur.Classe.Substring(0, 1);
            m_Vie.SetValue(joueur.Vie, joueur.VieMax);
            m_Endurance.SetValue(joueur.Endurance, joueur.EnduranceMax);
            MajClasse(joueur as IEtatJoueurClasse);

            if (!ReferenceEquals(m_CompetencesAffichees, joueur.Competences)) ConstruireBarre(joueur.Competences);
            for (var i = 0; i < m_Emplacements.Count && i < joueur.Competences.Count; i++)
                MajEmplacement(m_Emplacements[i], joueur.Competences[i], joueur.EstMort);

            var mort = joueur.EstMort;
            m_Mort.style.display = mort ? DisplayStyle.Flex : DisplayStyle.None;
            if (mort) m_MortTexte.text = "Réapparition dans " + Mathf.CeilToInt(joueur.TempsAvantReapparition) + " s";

            var invite = mort ? null : joueur.InviteInteraction;
            m_Interaction.style.display = string.IsNullOrEmpty(invite) ? DisplayStyle.None : DisplayStyle.Flex;
            if (!string.IsNullOrEmpty(invite)) m_InteractionTexte.text = invite;
            m_Reticule.style.display = mort ? DisplayStyle.None : DisplayStyle.Flex;
            m_Barre.EnableInClassList("hud-competences--mort", mort);
        }

        /// Jauge de classe (mana, rage) sous l'endurance et œil barré du mode furtif : seulement si la source du joueur
        /// implémente IEtatJoueurClasse (facultatif).
        void MajClasse(IEtatJoueurClasse classe)
        {
            var jauge = classe != null ? classe.Jauge : JaugeClasse.Aucune;
            if (jauge != m_JaugeAffichee)
            {
                m_JaugeAffichee = jauge;
                m_JaugeClasse.style.display = jauge == JaugeClasse.Aucune ? DisplayStyle.None : DisplayStyle.Flex;
                m_JaugeClasse.label = jauge == JaugeClasse.Rage ? "Rage" : "Mana";
                m_JaugeClasse.EnableInClassList("dl-gauge--mana", jauge == JaugeClasse.Mana);
                m_JaugeClasse.EnableInClassList("dl-gauge--rage", jauge == JaugeClasse.Rage);
            }
            if (jauge != JaugeClasse.Aucune) m_JaugeClasse.SetValue(classe.ValeurJauge, Mathf.Max(1f, classe.JaugeMax));
            m_Furtif.style.display = classe != null && classe.Furtif ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void ConstruireBarre(IReadOnlyList<ICompetenceHud> competences)
        {
            m_Barre.Clear();
            m_Emplacements.Clear();
            m_CompetencesAffichees = competences;
            if (competences == null) return;
            foreach (var c in competences)
            {
                var e = new Emplacement { racine = new VisualElement(), case_ = new VisualElement(), voile = new VisualElement(), icone = new VisualElement() };
                e.racine.AddToClassList("hud-emplacement");
                e.case_.AddToClassList("hud-emplacement__case");
                e.icone.AddToClassList("hud-emplacement__icone");
                e.abreviation = new Label { pickingMode = PickingMode.Ignore };
                e.abreviation.AddToClassList("hud-emplacement__abrev");
                e.voile.AddToClassList("hud-emplacement__voile");
                e.recharge = new Label { pickingMode = PickingMode.Ignore };
                e.recharge.AddToClassList("hud-emplacement__recharge");
                e.case_.Add(e.icone);
                e.case_.Add(e.abreviation);
                e.case_.Add(e.voile);
                e.case_.Add(e.recharge);
                e.invite = new InputPrompt(c.Action);
                e.invite.AddToClassList("hud-emplacement__invite");
                e.racine.Add(e.case_);
                e.racine.Add(e.invite);
                e.racine.tooltip = c.Nom;
                m_Barre.Add(e.racine);
                m_Emplacements.Add(e);
            }
        }

        static void MajEmplacement(Emplacement e, ICompetenceHud c, bool mort)
        {
            var icone = c.Icone;
            e.icone.style.display = icone != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (icone != null) e.icone.style.backgroundImage = new StyleBackground(icone);
            e.abreviation.text = c.Abreviation;

            // Emplacement vide (pas de compétence : Nom vide) : case grisée, sans abréviation ni recharge.
            var vide = string.IsNullOrEmpty(c.Nom);
            e.racine.EnableInClassList("hud-emplacement--vide", vide);
            if (vide) e.abreviation.text = "";
            var etat = mort || vide ? EtatCompetence.Indisponible : c.Etat;
            // En recharge, seul le compte à rebours est lisible (comme la maquette).
            var enRecharge = etat == EtatCompetence.Recharge && c.RechargeRestante > 0f;
            e.abreviation.style.display = icone != null || enRecharge ? DisplayStyle.None : DisplayStyle.Flex;
            e.case_.EnableInClassList("hud-emplacement__case--active", etat == EtatCompetence.Active);
            e.case_.EnableInClassList("hud-emplacement__case--prete", etat == EtatCompetence.Prete);
            e.case_.EnableInClassList("hud-emplacement__case--indisponible", etat == EtatCompetence.Indisponible || etat == EtatCompetence.Recharge);
            var recharge = etat == EtatCompetence.Recharge && c.RechargeRestante > 0f;
            e.voile.style.display = recharge ? DisplayStyle.Flex : DisplayStyle.None;
            e.recharge.style.display = recharge ? DisplayStyle.Flex : DisplayStyle.None;
            if (recharge)
            {
                e.recharge.text = c.RechargeRestante >= 1f ? Mathf.CeilToInt(c.RechargeRestante).ToString() : c.RechargeRestante.ToString("0.0");
                // Le voile descend à mesure que la recharge avance.
                var part = c.RechargeTotale > 0f ? Mathf.Clamp01(c.RechargeRestante / c.RechargeTotale) : 1f;
                e.voile.style.height = Length.Percent(part * 100f);
            }
        }

        static string Horloge(float secondes)
        {
            var s = Mathf.Max(0, Mathf.CeilToInt(secondes));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        /// 1250 → « 1 250 » (espace simple : Fredoka n'a pas l'espace fine insécable).
        public static string Milliers(int n) => n.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");
    }
}
