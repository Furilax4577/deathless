using System.Collections.Generic;
using Deathless.UI.Donnees;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deathless.UI.Ecrans
{
    /// HUD en jeu (interface.md) : Nyxessa et son bouclier, temps restant, repère « Jour N » / « Vague x / y »
    /// (IEtatVagues, 01/10/2026), vote prêt (badge « Prêt » du joueur local et de chaque allié, invite « Se déclarer
    /// prêt » / « Annuler », 01/10/2026), alerte avant la nuit, bannière « NUIT N », indicateur « Nyxessa attaquée », or,
    /// joueur, compétences, interaction, mort ; en multijoueur, vie des autres joueurs (colonne de gauche) et leur pseudo
    /// au-dessus de leur tête.
    /// Lit DonneesUI.Partie (et IEtatEquipe s'il l'implémente) et DonneesUI.Joueur à chaque image.
    public class EcranHud : Ecran
    {
        /// Délai d'alerte avant la nuit (deroule.md : 15 s avant le crépuscule, à équilibrer).
        public static float DelaiAlerteNuit = 15f;

        /// Durée d'affichage de l'indicateur « Nyxessa attaquée » après le dernier coup.
        public static float DureeAlerteAttaque = 3f;

        /// Durée de la bannière « NUIT N ».
        public static float DureeBanniere = 3.5f;

        /// Missiles de Nyxessa : durée du « pop » d'un missile gagné et de l'onde d'un missile tiré.
        public static float DureePopMissile = 0.3f;
        public static float DureeTirMissile = 0.35f;

        public override bool CarteUI => false;
        public override bool Opaque => true;

        /// Durée du liseré « nouvelle vague » sur le repère des vagues.
        public static float DureeNouvelleVague = 2.5f;

        VisualElement m_VieNyx, m_PisteBouclier, m_Bouclier;
        Label m_Temps, m_Prets, m_PretsCompte, m_AlerteBouclier, m_AlerteNuit, m_Or;
        VisualElement m_BlocPrets, m_PretsBadge, m_JoueurPret, m_BlocAlerteNuit;
        VisualElement m_Vague;
        Label m_VagueTexte;
        /// Dernier repère des vagues écrit (phase, nuit, vague, total) et instant de la dernière vague vue.
        PhasePartie m_VaguePhase = (PhasePartie)(-1);
        int m_VagueNuit = -1, m_VagueAffichee = -1, m_VaguesTotalAffiche = -1;
        float m_TempsNouvelleVague = -1f;
        IconeJourNuit m_Icone;
        VisualElement m_Banniere;
        Label m_BanniereTitre;
        VisualElement m_Attaque;
        FlecheHud m_FlecheAttaque;
        VisualElement m_Portrait;
        Label m_Initiale;
        VisualElement m_Embleme;
        string m_ClasseEmbleme;
        VisualElement m_VieRemplissage, m_EndurancePiste, m_EnduranceRemplissage;
        Label m_VieValeur;
        AnneauJauge m_Anneau;
        VisualElement m_Furtif, m_Potion;
        Label m_PotionNombre;
        string m_ClasseBarre;
        JaugeClasse m_JaugeAffichee = (JaugeClasse)(-1);
        VisualElement m_Barre;
        VisualElement m_Interaction, m_PointsCompetence, m_OrPorte, m_DonjonAlerte, m_DonjonMessage;
        Label m_OrPorteValeur, m_OrPorteLegende, m_DonjonAlerteTexte, m_DonjonMessageTexte;
        Label m_PointsTexte;
        Label m_InteractionTexte;
        VisualElement m_Reticule;
        VisualElement m_Mort;
        Label m_MortTexte;
        VisualElement m_Missiles, m_MissilesIcone, m_MissilesCharge, m_MissilesOnde;
        Label m_MissilesNombre, m_MissilesMax;
        int m_MissilesVus = -1;
        float m_TempsPopMissile = -1f, m_TempsTirMissile = -1f;

        /// Dernières valeurs écrites dans les textes du HUD (lu à chaque image) : le texte n'est reconstruit
        /// (chaînes allouées) qu'au changement de la valeur affichée.
        PhasePartie m_PhaseTexte = (PhasePartie)(-1);
        int m_SecondesTexte = -1, m_NuitTexte = -1;
        string m_AubeTexte;
        int m_BouclierAffiche = -1, m_PretsAffiches = -1, m_TotalAffiche = -1, m_AlerteNuitAffichee = int.MinValue;
        /// État « prêt » du joueur local tel qu'écrit dans la pastille (-1 : jamais).
        int m_PretLocalAffiche = -1;
        int m_OrAffiche = int.MinValue, m_MissilesNombreAffiche = -1, m_MissilesMaxAffiche = -1;
        int m_VieAffichee = int.MinValue, m_MortAffichee = int.MinValue, m_OrPorteAffiche = int.MinValue;
        int m_AvantRappelAffiche = int.MinValue, m_PointsAffiches = -1, m_PotionsAffichees = int.MinValue;

        readonly List<Emplacement> m_Emplacements = new List<Emplacement>();
        /// Statuts du joueur et des ennemis (classe à part : HudStatuts.cs).
        HudStatuts m_Statuts;
        /// Jauge de relevé du Renversé (classe à part : HudRelevage.cs).
        HudRelevage m_Relevage;
        /// Jauge de parade du paladin, sous le réticule (classe à part : HudParade.cs).
        HudParade m_Parade;
        /// Barres de vie des ennemis et méga barre du/des boss (classe à part : HudVieEnnemis.cs).
        HudVieEnnemis m_VieEnnemis;
        /// Chiffres de dégâts flottants (classe à part : HudDegats.cs).
        HudDegats m_Degats;

        /// Autres joueurs affichés (colonne de gauche) : trois au plus (salon de quatre).
        public const int AlliesMax = 3;
        /// Distance au-delà de laquelle le pseudo d'un allié n'est plus écrit au-dessus de sa tête.
        public static float DistancePseudo = 70f;

        sealed class LigneAllie
        {
            public VisualElement racine, embleme, piste, vie, pret;
            public Label pseudo, etat;
            public string classe;
            public int reapparitionAffichee = int.MinValue;
        }

        readonly List<LigneAllie> m_Allies = new List<LigneAllie>();
        readonly List<Label> m_PseudosTetes = new List<Label>();
        VisualElement m_ColonneAllies;
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
            public VectorImage vecteur;
            public object imageAffichee;
            /// Clé du compte à rebours affiché (100 + secondes au-dessus d'une seconde, sinon dixièmes) : -1 = aucun.
            public int rechargeAffichee = -1;
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
            m_PretsCompte = Racine.Q<Label>("prets-compte");
            m_PretsBadge = Racine.Q("prets-badge");
            m_JoueurPret = Racine.Q("joueur-pret");
            m_Vague = Racine.Q("vague");
            m_VagueTexte = Racine.Q<Label>("vague-texte");
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
            m_Embleme = Racine.Q("portrait-embleme");
            m_Anneau = Racine.Q<AnneauJauge>("joueur-anneau");
            m_VieRemplissage = Racine.Q("joueur-vie");
            m_VieValeur = Racine.Q<Label>("joueur-vie-valeur");
            m_EndurancePiste = Racine.Q("joueur-endurance-piste");
            m_EnduranceRemplissage = Racine.Q("joueur-endurance");
            m_Furtif = Racine.Q("furtif");
            IconesUI.Poser(Racine.Q("furtif-icone"), IconesUI.Furtif);
            m_Potion = Racine.Q("potion");
            m_PotionNombre = Racine.Q<Label>("potion-nombre");
            IconesUI.Poser(Racine.Q("potion-icone"), IconesUI.Potion);
            m_Barre = Racine.Q("competences");
            m_Interaction = Racine.Q("interaction");
            m_PointsCompetence = Racine.Q("points-competence");
            m_OrPorte = Racine.Q("or-porte");
            m_OrPorteValeur = Racine.Q<Label>("or-porte-valeur");
            m_OrPorteLegende = Racine.Q<Label>("or-porte-legende");
            m_DonjonAlerte = Racine.Q("donjon-alerte");
            m_DonjonAlerteTexte = Racine.Q<Label>("donjon-alerte-texte");
            m_DonjonMessage = Racine.Q("donjon-message");
            m_DonjonMessageTexte = Racine.Q<Label>("donjon-message-texte");
            m_PointsTexte = Racine.Q<Label>("points-texte");
            m_InteractionTexte = Racine.Q<Label>("interaction-texte");
            m_Reticule = Racine.Q("reticule");
            m_Mort = Racine.Q("mort");
            m_MortTexte = Racine.Q<Label>("mort-texte");
            m_Missiles = Racine.Q("missiles");
            m_MissilesIcone = Racine.Q("missiles-icone");
            m_MissilesCharge = Racine.Q("missiles-charge");
            m_MissilesOnde = Racine.Q("missiles-onde");
            m_MissilesNombre = Racine.Q<Label>("missiles-nombre");
            m_MissilesMax = Racine.Q<Label>("missiles-max");
            var eteint = Racine.Q("missiles-eteint");
            if (eteint != null) IconesUI.Poser(eteint, IconesUI.MissileNyxessaEteint);
            var plein = Racine.Q("missiles-plein");
            if (plein != null) IconesUI.Poser(plein, IconesUI.MissileNyxessa);
            ConstruireAllies();
            m_Statuts = new HudStatuts(Racine);
            m_Relevage = new HudRelevage(Racine);
            m_Parade = new HudParade(Racine);
            m_VieEnnemis = new HudVieEnnemis(Racine);
            m_Degats = new HudDegats(Racine);
        }

        void ConstruireAllies()
        {
            m_ColonneAllies = Racine.Q("allies");
            var pseudos = Racine.Q("pseudos");
            for (var i = 0; i < AlliesMax; i++)
            {
                var l = new LigneAllie { racine = new VisualElement(), piste = new VisualElement(), vie = new VisualElement() };
                l.racine.AddToClassList("hud-allie");
                l.racine.pickingMode = PickingMode.Ignore;
                l.embleme = IconesUI.Creer(IconesUI.RepliClasse, "hud-allie__embleme");
                var corps = new VisualElement { pickingMode = PickingMode.Ignore };
                corps.AddToClassList("hud-allie__corps");
                l.pseudo = new Label { pickingMode = PickingMode.Ignore };
                l.pseudo.AddToClassList("hud-allie__pseudo");
                l.piste.AddToClassList("hud-allie__piste");
                l.vie.AddToClassList("hud-allie__vie");
                l.piste.Add(l.vie);
                l.etat = new Label { pickingMode = PickingMode.Ignore };
                l.etat.AddToClassList("hud-allie__etat");
                corps.Add(l.pseudo);
                corps.Add(l.piste);
                corps.Add(l.etat);
                l.pret = CreerBadgePret("hud-allie__pret");
                l.racine.Add(l.embleme);
                l.racine.Add(corps);
                l.racine.Add(l.pret);
                l.racine.style.display = DisplayStyle.None;
                m_ColonneAllies?.Add(l.racine);
                m_Allies.Add(l);

                var t = new Label { pickingMode = PickingMode.Ignore };
                t.AddToClassList("hud-pseudo");
                t.style.display = DisplayStyle.None;
                pseudos?.Add(t);
                m_PseudosTetes.Add(t);
            }
        }

        /// Badge « Prêt » (vote du jour, 01/10/2026) : le même sur la ligne d'un allié que sur le portrait du joueur local.
        static VisualElement CreerBadgePret(string classe)
        {
            var badge = new VisualElement { pickingMode = PickingMode.Ignore };
            badge.AddToClassList("hud-pret-badge");
            badge.AddToClassList(classe);
            var texte = new Label("Prêt") { pickingMode = PickingMode.Ignore };
            texte.AddToClassList("hud-pret-badge__texte");
            badge.Add(texte);
            return badge;
        }

        /// Appelé par le navigateur quand la source de la partie change.
        public void Suivre(IEtatPartie partie)
        {
            if (m_Partie == partie) return;
            if (m_Partie != null)
            {
                m_Partie.NuitCommencee -= OnNuit;
                m_Partie.NyxessaFrappee -= OnFrappee;
                if (m_Partie is IEtatBoss bossAvant) bossAvant.BossSurgit -= OnBoss;
            }
            m_Partie = partie;
            if (m_Partie != null)
            {
                m_Partie.NuitCommencee += OnNuit;
                m_Partie.NyxessaFrappee += OnFrappee;
                if (m_Partie is IEtatBoss boss) boss.BossSurgit += OnBoss;
            }
            m_TempsBanniere = -1f;
            m_TempsAttaque = -1f;
            m_MissilesVus = -1;
            m_TempsPopMissile = m_TempsTirMissile = -1f;
            m_VaguePhase = (PhasePartie)(-1);
            m_VagueAffichee = -1;
            m_TempsNouvelleVague = -1f;
        }

        void OnNuit(int nuit)
        {
            m_BanniereTitre.text = "NUIT " + nuit;
            m_TempsBanniere = 0f;
        }

        /// Boss de la nuit sorti de terre (IEtatBoss, 30/09/2026) : même bannière que « NUIT N », à son nom.
        void OnBoss(string nom)
        {
            m_BanniereTitre.text = string.IsNullOrEmpty(nom) ? "" : nom.ToUpperInvariant();
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
            MajEquipe(partie as IEtatEquipe);
            m_Statuts?.Maj(joueur == null || joueur.EstMort);
            m_Relevage?.Maj();
            m_Parade?.Maj(joueur == null || joueur.EstMort);
            m_VieEnnemis?.Maj(dt);
            m_Degats?.Suivre();
            m_Degats?.Maj(dt);
        }

        /// Colonne de gauche : un allié par ligne (emblème de classe, pseudo, barre de vie fine ; mort : ligne grisée et
        /// compte à rebours). Vide (masquée) en solo. Pseudo au-dessus de la tête de chaque allié vivant et visible.
        void MajEquipe(IEtatEquipe equipe)
        {
            var allies = equipe?.Allies;
            var n = allies == null ? 0 : Mathf.Min(allies.Count, AlliesMax);
            if (m_ColonneAllies != null) m_ColonneAllies.style.display = n > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            var cam = n > 0 ? Camera.main : null;
            // Badges « Prêt » : seulement pendant le vote du jour (même règle que la pastille de l'horloge).
            var vote = n > 0 && VoteVisible(DonneesUI.Partie);
            for (var i = 0; i < m_Allies.Count; i++)
            {
                var l = m_Allies[i];
                var t = m_PseudosTetes[i];
                if (i >= n)
                {
                    l.racine.style.display = DisplayStyle.None;
                    t.style.display = DisplayStyle.None;
                    continue;
                }
                var a = allies[i];
                l.racine.style.display = DisplayStyle.Flex;
                if (l.classe != a.ClasseId)
                {
                    l.classe = a.ClasseId;
                    var c = ClassesJouables.Trouver(a.ClasseId);
                    IconesUI.Poser(l.embleme, c != null ? c.Embleme : IconesUI.RepliClasse);
                }
                l.pseudo.text = a.Pseudo;
                l.pret.EnableInClassList("hud-pret-badge--visible", vote && a.EstPret);
                l.racine.EnableInClassList("hud-allie--mort", a.EstMort);
                if (a.EstMort)
                {
                    var reapparition = Mathf.CeilToInt(Mathf.Max(0f, a.TempsAvantReapparition));
                    if (reapparition != l.reapparitionAffichee)
                    {
                        l.reapparitionAffichee = reapparition;
                        l.etat.text = "Réapparition dans " + reapparition + " s";
                    }
                }
                else l.vie.style.width = Length.Percent((a.VieMax > 0f ? Mathf.Clamp01(a.Vie / a.VieMax) : 0f) * 100f);
                PlacerPseudo(t, a, cam);
            }
        }

        void PlacerPseudo(Label t, IAllie a, Camera cam)
        {
            var tete = a.PositionTete;
            var visible = tete.HasValue && !a.EstMort && cam != null && Racine.panel != null;
            if (visible)
            {
                var v = cam.WorldToViewportPoint(tete.Value);
                visible = v.z > 0.5f && v.z < DistancePseudo && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f;
            }
            t.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;
            var p = RuntimePanelUtils.CameraTransformWorldToPanel(Racine.panel, tete.Value, cam);
            t.text = a.Pseudo;
            t.style.left = p.x;
            t.style.top = p.y;
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
            if (bouclierFaible)
            {
                var pourcent = Mathf.RoundToInt(ratio * 100f);
                if (pourcent != m_BouclierAffiche)
                {
                    m_BouclierAffiche = pourcent;
                    m_AlerteBouclier.text = "Bouclier de Nyxessa à " + pourcent + " %";
                }
            }

            // Temps (texte reconstruit seulement quand la phase, la seconde affichée ou le numéro de nuit change).
            var phase = partie.Phase;
            var nuit = phase == PhasePartie.Nuit || phase == PhasePartie.Crepuscule;
            m_Icone.nuit = nuit;
            var secondes = phase == PhasePartie.Jour || phase == PhasePartie.Nuit ? Mathf.Max(0, Mathf.CeilToInt(partie.TempsRestantPhase)) : 0;
            var numeroNuit = partie.NumeroNuit;
            // Nuit prolongée tant que le boss vit (IEtatBoss, 30/09/2026) : le compte à rebours laisse place au boss attendu.
            var aubeAttend = phase == PhasePartie.Nuit && partie is IEtatBoss etatBoss ? etatBoss.AubeAttend : null;
            if (phase != m_PhaseTexte || secondes != m_SecondesTexte || numeroNuit != m_NuitTexte || aubeAttend != m_AubeTexte)
            {
                m_PhaseTexte = phase;
                m_SecondesTexte = secondes;
                m_NuitTexte = numeroNuit;
                m_AubeTexte = aubeAttend;
                switch (phase)
                {
                    case PhasePartie.Jour: m_Temps.text = "Jour · " + Horloge(secondes) + " avant la nuit"; break;
                    case PhasePartie.Crepuscule: m_Temps.text = "Crépuscule · la nuit " + numeroNuit + " tombe"; break;
                    case PhasePartie.Nuit:
                        m_Temps.text = aubeAttend != null
                            ? "Nuit " + numeroNuit + " · l’aube attend la chute de " + aubeAttend
                            : "Nuit " + numeroNuit + " · " + Horloge(secondes) + " avant l’aube";
                        break;
                    case PhasePartie.Aube: m_Temps.text = "Aube · le jour se lève"; break;
                    default: m_Temps.text = ""; break;
                }
            }

            // Repère permanent du jour et des vagues (01/10/2026).
            MajVague(partie, dt);

            // Vote prêt (01/10/2026 : plus de « Prêts 0 / 2 » ; l'état du joueur local est dit en clair, le compte reste en
            // multijoueur). Au donjon, le vote est inactif : l'invite n'a rien à y faire (audit du 27/09/2026, C5).
            var vote = VoteVisible(partie);
            m_BlocPrets.style.display = vote ? DisplayStyle.Flex : DisplayStyle.None;
            if (vote)
            {
                var joueur = DonneesUI.Joueur;
                var pret = joueur != null && joueur.EstPret ? 1 : 0;
                if (pret != m_PretLocalAffiche)
                {
                    m_PretLocalAffiche = pret;
                    m_Prets.text = pret == 1 ? "Annuler" : "Se déclarer prêt";
                    m_PretsBadge?.EnableInClassList("hud-pret-badge--visible", pret == 1);
                    m_BlocPrets.EnableInClassList("hud-prets--pret", pret == 1);
                }
                if (partie.JoueursPrets != m_PretsAffiches || partie.JoueursTotal != m_TotalAffiche)
                {
                    m_PretsAffiches = partie.JoueursPrets;
                    m_TotalAffiche = partie.JoueursTotal;
                    if (m_PretsCompte != null) m_PretsCompte.text = m_PretsAffiches + " / " + m_TotalAffiche + " prêts";
                    m_BlocPrets.EnableInClassList("hud-prets--multi", m_TotalAffiche > 1);
                    m_BlocPrets.EnableInClassList("hud-prets--tous", m_TotalAffiche > 1 && m_PretsAffiches >= m_TotalAffiche);
                }
            }
            else m_PretLocalAffiche = -1;

            // Alerte avant la nuit (clignote).
            // Au donjon, l'alerte du rappel par Nyxessa (même place, plus précise) remplace celle de la nuit.
            var alerte = partie.Phase == PhasePartie.Jour && partie.TempsRestantPhase <= DelaiAlerteNuit
                && !(DonneesUI.Donjon != null && DonneesUI.Donjon.AuDonjon);
            m_BlocAlerteNuit.style.display = alerte ? DisplayStyle.Flex : DisplayStyle.None;
            if (alerte)
            {
                var avantNuit = Mathf.CeilToInt(partie.TempsRestantPhase);
                if (avantNuit != m_AlerteNuitAffichee)
                {
                    m_AlerteNuitAffichee = avantNuit;
                    m_AlerteNuit.text = "La nuit tombe dans " + avantNuit + " s";
                }
                m_BlocAlerteNuit.EnableInClassList("hud-alerte-nuit--pulse", Mathf.Repeat(m_Horloge, 1f) < 0.5f);
            }

            // Or.
            if (partie.OrEquipe != m_OrAffiche)
            {
                m_OrAffiche = partie.OrEquipe;
                m_Or.text = Milliers(m_OrAffiche);
            }

            // Missiles de Nyxessa (facultatif : IEtatMissiles sur la source de la partie).
            MajMissiles(partie as IEtatMissiles, dt);

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

        /// Le vote du jour est affichable : jour, vote actif, et le joueur n'est pas au donjon.
        static bool VoteVisible(IEtatPartie partie)
        {
            return partie != null && partie.Phase == PhasePartie.Jour && partie.VoteActif
                && !(DonneesUI.Donjon != null && DonneesUI.Donjon.AuDonjon);
        }

        /// Repère permanent à droite de l'horloge (demande de Quentin, 01/10/2026 : numéro de vague toujours visible) :
        /// « Jour N » le jour (N = numéro de la nuit à venir), « N vagues » au crépuscule (dès que le directeur les a
        /// préparées), « Vague x / y » la nuit (liseré or, et éclat blanc un instant à chaque nouvelle vague), « Jour N+1 »
        /// à l'aube. Sans IEtatVagues sur la source, la nuit se contente de « Nuit N ». Texte reconstruit seulement quand
        /// la valeur affichée change.
        void MajVague(IEtatPartie partie, float dt)
        {
            if (m_Vague == null || m_VagueTexte == null) return;
            var vagues = partie as IEtatVagues;
            var phase = partie.Phase;
            var nuit = partie.NumeroNuit;
            int vague = 0, total = 0;
            if (vagues != null && (phase == PhasePartie.Crepuscule || phase == PhasePartie.Nuit))
            {
                vague = Mathf.Max(0, vagues.VagueEnCours);
                total = Mathf.Max(0, vagues.VaguesTotal);
            }
            var visible = phase != PhasePartie.Terminee;
            m_Vague.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;
            // Nouvelle vague lancée (la nuit seulement) : éclat du repère.
            if (phase == PhasePartie.Nuit && m_VaguePhase == PhasePartie.Nuit && m_VagueNuit == nuit && vague > m_VagueAffichee && m_VagueAffichee >= 0)
                m_TempsNouvelleVague = 0f;
            if (phase != m_VaguePhase || nuit != m_VagueNuit || vague != m_VagueAffichee || total != m_VaguesTotalAffiche)
            {
                m_VaguePhase = phase;
                m_VagueNuit = nuit;
                m_VagueAffichee = vague;
                m_VaguesTotalAffiche = total;
                switch (phase)
                {
                    case PhasePartie.Jour: m_VagueTexte.text = "Jour " + nuit; break;
                    case PhasePartie.Crepuscule: m_VagueTexte.text = total > 0 ? total + " vagues" : "Nuit " + nuit; break;
                    case PhasePartie.Nuit:
                        m_VagueTexte.text = vague > 0 && total > 0 ? "Vague " + vague + " / " + total
                            : total > 0 ? total + " vagues" : "Nuit " + nuit;
                        break;
                    case PhasePartie.Aube: m_VagueTexte.text = "Jour " + (nuit + 1); break;
                    default: m_VagueTexte.text = ""; break;
                }
                m_Vague.EnableInClassList("hud-vague--nuit", phase == PhasePartie.Nuit || phase == PhasePartie.Crepuscule);
            }
            if (m_TempsNouvelleVague >= 0f)
            {
                m_TempsNouvelleVague += dt;
                if (m_TempsNouvelleVague >= DureeNouvelleVague) m_TempsNouvelleVague = -1f;
            }
            m_Vague.EnableInClassList("hud-vague--nouvelle", m_TempsNouvelleVague >= 0f);
        }

        /// Compteur des missiles de Nyxessa, à droite de sa barre : « 3 / 5 » et icône qui se charge (l'icône allumée,
        /// découpée du bas vers le haut, suit ChargeProchainMissile ; stock plein : entière). Missile gagné : « pop »
        /// d'échelle de l'icône ; missile tiré : onde verte et nombre en vert un instant. Masqué sans IEtatMissiles.
        void MajMissiles(IEtatMissiles missiles, float dt)
        {
            if (m_Missiles == null) return;
            var max = missiles != null ? missiles.MissilesMax : 0;
            m_Missiles.style.display = max > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (max <= 0)
            {
                m_MissilesVus = -1;
                return;
            }
            var dispo = Mathf.Clamp(missiles.MissilesDisponibles, 0, max);
            var plein = dispo >= max;
            if (m_MissilesVus >= 0 && dispo > m_MissilesVus) m_TempsPopMissile = 0f;
            else if (m_MissilesVus >= 0 && dispo < m_MissilesVus) m_TempsTirMissile = 0f;
            m_MissilesVus = dispo;

            if (dispo != m_MissilesNombreAffiche)
            {
                m_MissilesNombreAffiche = dispo;
                m_MissilesNombre.text = dispo.ToString();
            }
            if (max != m_MissilesMaxAffiche)
            {
                m_MissilesMaxAffiche = max;
                m_MissilesMax.text = "/ " + max;
            }
            m_Missiles.EnableInClassList("hud-missiles--plein", plein);
            m_Missiles.EnableInClassList("hud-missiles--vide", dispo == 0);
            // Missile gagné : l'icône grossit puis revient (demi-sinus).
            var echelle = 1f;
            if (m_TempsPopMissile >= 0f)
            {
                m_TempsPopMissile += dt;
                var t = m_TempsPopMissile / DureePopMissile;
                if (t >= 1f) m_TempsPopMissile = -1f;
                else echelle = 1f + 0.3f * Mathf.Sin(t * Mathf.PI);
            }
            m_MissilesIcone.style.scale = new StyleScale(new Scale(new Vector3(echelle, echelle, 1f)));

            // Charge du prochain missile ; pendant le pop, l'icône arrivée reste entièrement allumée.
            var charge = plein || m_TempsPopMissile >= 0f ? 1f : Mathf.Clamp01(missiles.ChargeProchainMissile);
            m_MissilesCharge.style.height = Length.Percent(charge * 100f);
            m_MissilesCharge.EnableInClassList("hud-missiles__charge--pleine", charge >= 1f);

            // Missile tiré : anneau vert qui s'élargit et s'efface ; nombre en vert.
            var onde = 0f;
            var ondeEchelle = 1f;
            if (m_TempsTirMissile >= 0f)
            {
                m_TempsTirMissile += dt;
                var t = m_TempsTirMissile / DureeTirMissile;
                if (t >= 1f) m_TempsTirMissile = -1f;
                else
                {
                    onde = 1f - t;
                    ondeEchelle = Mathf.Lerp(0.8f, 1.7f, t);
                }
            }
            m_MissilesOnde.style.opacity = onde;
            m_MissilesOnde.style.scale = new StyleScale(new Scale(new Vector3(ondeEchelle, ondeEchelle, 1f)));
            m_Missiles.EnableInClassList("hud-missiles--tir", m_TempsTirMissile >= 0f);
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
            if (m_ClasseEmbleme != joueur.Classe)
            {
                // Emblème hexagonal de la classe (table IconesUI), posé sans rond derrière lui (Quentin, 26/09/2026) ;
                // l'initiale sur fond teinté reste en repli s'il manque.
                m_ClasseEmbleme = joueur.Classe;
                var embleme = ClassesJouables.TrouverParNom(joueur.Classe)?.Embleme;
                bool avecEmbleme = m_Embleme != null && !string.IsNullOrEmpty(embleme) && IconesUI.Poser(m_Embleme, embleme);
                if (m_Embleme != null && !avecEmbleme) m_Embleme.style.display = DisplayStyle.None;
                m_Initiale.text = string.IsNullOrEmpty(joueur.Classe) ? "?" : joueur.Classe.Substring(0, 1);
                m_Initiale.style.display = avecEmbleme ? DisplayStyle.None : DisplayStyle.Flex;
                m_Portrait.style.backgroundColor = avecEmbleme ? Color.clear : joueur.TeinteClasse;
            }
            // Vie (maquette B) : large barre à embouts de gemme, seule à afficher son chiffre (pas de « / max »).
            var vieRatio = joueur.VieMax > 0f ? Mathf.Clamp01(joueur.Vie / joueur.VieMax) : 0f;
            m_VieRemplissage.style.width = Length.Percent(vieRatio * 100f);
            var vie = Mathf.RoundToInt(joueur.Vie);
            if (vie != m_VieAffichee)
            {
                m_VieAffichee = vie;
                m_VieValeur.text = vie.ToString();
            }

            // Endurance : filet presque invisible, qui ne s'éclaire vraiment que sous 70 % environ.
            var enduranceRatio = joueur.EnduranceMax > 0f ? Mathf.Clamp01(joueur.Endurance / joueur.EnduranceMax) : 0f;
            m_EnduranceRemplissage.style.width = Length.Percent(enduranceRatio * 100f);
            m_EndurancePiste.EnableInClassList("hud-joueur__endurance-piste--active", enduranceRatio < 0.7f);

            MajClasse(joueur as IEtatJoueurClasse);
            MajPotions(joueur as IEtatJoueurPotions);
            // Badge « Prêt » sur le portrait (01/10/2026), le temps du vote du jour.
            m_JoueurPret?.EnableInClassList("hud-pret-badge--visible", joueur.EstPret && VoteVisible(partie));

            if (!ReferenceEquals(m_CompetencesAffichees, joueur.Competences) || m_ClasseBarre != joueur.Classe)
                ConstruireBarre(joueur.Competences, joueur.Classe);
            for (var i = 0; i < m_Emplacements.Count && i < joueur.Competences.Count; i++)
                MajEmplacement(m_Emplacements[i], joueur.Competences[i], joueur.EstMort);

            var mort = joueur.EstMort;
            m_Mort.style.display = mort ? DisplayStyle.Flex : DisplayStyle.None;
            if (mort)
            {
                var reapparition = Mathf.CeilToInt(joueur.TempsAvantReapparition);
                if (reapparition != m_MortAffichee)
                {
                    m_MortAffichee = reapparition;
                    m_MortTexte.text = "Réapparition dans " + reapparition + " s";
                }
            }

            // Donjon : or porté (sous la caisse), alerte avant le rappel par Nyxessa, message (rappel, dépôt).
            var donjon = DonneesUI.Donjon;
            if (m_OrPorte != null)
            {
                int porte = donjon != null ? donjon.OrPorte : 0;
                bool dedans = donjon != null && donjon.AuDonjon;
                m_OrPorte.style.display = porte > 0 || dedans ? DisplayStyle.Flex : DisplayStyle.None;
                if (porte != m_OrPorteAffiche)
                {
                    m_OrPorteAffiche = porte;
                    m_OrPorteValeur.text = porte.ToString();
                }
                m_OrPorteLegende.text = dedans ? "or porté · au donjon" : "or porté";
                float avant = donjon != null && dedans ? donjon.AvantRappel : -1f;
                m_DonjonAlerte.style.display = avant >= 0f ? DisplayStyle.Flex : DisplayStyle.None;
                if (avant >= 0f)
                {
                    var avantRappel = Mathf.CeilToInt(avant);
                    if (avantRappel != m_AvantRappelAffiche)
                    {
                        m_AvantRappelAffiche = avantRappel;
                        m_DonjonAlerteTexte.text = "Le portail se ferme dans " + avantRappel + " s : rentrez au village !";
                    }
                    m_DonjonAlerte.style.opacity = 0.75f + 0.25f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
                }
                string msg = donjon != null ? donjon.Message : null;
                m_DonjonMessage.style.display = string.IsNullOrEmpty(msg) ? DisplayStyle.None : DisplayStyle.Flex;
                if (!string.IsNullOrEmpty(msg)) m_DonjonMessageTexte.text = msg;
            }

            // Points de compétence à dépenser : pastille et invite du menu du personnage (Tab / Y).
            int points = DonneesUI.Personnage != null ? DonneesUI.Personnage.Points : 0;
            if (m_PointsCompetence != null)
            {
                m_PointsCompetence.style.display = points > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                if (points > 0 && points != m_PointsAffiches)
                {
                    m_PointsAffiches = points;
                    m_PointsTexte.text = points == 1 ? "1 point de compétence" : points + " points de compétence";
                }
            }

            var invite = mort ? null : joueur.InviteInteraction;
            m_Interaction.style.display = string.IsNullOrEmpty(invite) ? DisplayStyle.None : DisplayStyle.Flex;
            if (!string.IsNullOrEmpty(invite)) m_InteractionTexte.text = invite;
            m_Reticule.style.display = mort ? DisplayStyle.None : DisplayStyle.Flex;
            m_Barre.EnableInClassList("hud-competences--mort", mort);
        }

        /// Jauge de classe (mana, rage) en anneau plein autour du portrait (maquette B) et œil barré du mode furtif :
        /// seulement si la source du joueur implémente IEtatJoueurClasse (facultatif). Pas d'anneau sans jauge.
        void MajClasse(IEtatJoueurClasse classe)
        {
            var jauge = classe != null ? classe.Jauge : JaugeClasse.Aucune;
            if (jauge != m_JaugeAffichee)
            {
                m_JaugeAffichee = jauge;
                m_Anneau.style.display = jauge == JaugeClasse.Aucune ? DisplayStyle.None : DisplayStyle.Flex;
                m_Anneau.couleur = jauge == JaugeClasse.Rage ? "#ff8c1a" : "#4a8fe0";
            }
            if (jauge != JaugeClasse.Aucune) m_Anneau.value = classe.ValeurJauge / Mathf.Max(1f, classe.JaugeMax);
            m_Furtif.style.display = classe != null && classe.Furtif ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// Potion (croix haut) : seulement si la source du joueur implémente IEtatJoueurPotions.
        void MajPotions(IEtatJoueurPotions potions)
        {
            m_Potion.style.display = potions != null && potions.PotionsMax > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (potions == null) return;
            if (potions.Potions != m_PotionsAffichees)
            {
                m_PotionsAffichees = potions.Potions;
                m_PotionNombre.text = m_PotionsAffichees.ToString();
            }
            m_Potion.EnableInClassList("hud-potion--vide", potions.Potions <= 0);
        }

        void ConstruireBarre(IReadOnlyList<ICompetenceHud> competences, string nomClasse)
        {
            m_Barre.Clear();
            m_Emplacements.Clear();
            m_CompetencesAffichees = competences;
            m_ClasseBarre = nomClasse;
            if (competences == null) return;
            // Icônes : celles de la classe (ClassesJouables.Catalogue → IconesUI), à la place des abréviations du jeu.
            var classe = ClassesJouables.TrouverParNom(nomClasse);
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
                e.vecteur = IconesUI.Trouver(ClassesJouables.IconeAction(classe, c.Action));
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
            // Emplacement vide (pas de compétence : Nom vide) : case grisée, sans icône, abréviation ni recharge.
            var vide = string.IsNullOrEmpty(c.Nom);
            // Icône : texture fournie par le jeu, sinon icône vectorielle de la classe, sinon abréviation.
            object image = vide ? null : c.Icone != null ? c.Icone : (object)e.vecteur;
            var icone = image;
            if (!ReferenceEquals(image, e.imageAffichee))
            {
                e.imageAffichee = image;
                e.icone.style.display = image != null ? DisplayStyle.Flex : DisplayStyle.None;
                if (image is Texture2D t) e.icone.style.backgroundImage = new StyleBackground(t);
                else if (image is VectorImage v) e.icone.style.backgroundImage = new StyleBackground(v);
            }
            e.abreviation.text = c.Abreviation;

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
                // Texte reconstruit seulement quand la valeur affichée change (secondes entières, puis dixièmes).
                var restante = c.RechargeRestante;
                var cle = restante >= 1f ? 100 + Mathf.CeilToInt(restante) : Mathf.RoundToInt(restante * 10f);
                if (cle != e.rechargeAffichee)
                {
                    e.rechargeAffichee = cle;
                    e.recharge.text = restante >= 1f ? Mathf.CeilToInt(restante).ToString() : (cle * 0.1f).ToString("0.0");
                }
                // Le voile descend à mesure que la recharge avance.
                var part = c.RechargeTotale > 0f ? Mathf.Clamp01(c.RechargeRestante / c.RechargeTotale) : 1f;
                e.voile.style.height = Length.Percent(part * 100f);
            }
        }

        /// Secondes entières (positives) → « m:ss ».
        static string Horloge(int s)
        {
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        /// 1250 → « 1 250 » (espace simple : Fredoka n'a pas l'espace fine insécable).
        public static string Milliers(int n) => n.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");
    }
}
