namespace Deathless.Jeu
{
    /// Événements sonores du jeu → ids du catalogue (Wiki/data/sons.json), par ordre de préférence : le premier id
    /// présent dans le catalogue joue. Les sons « à créer » sont en tête : ils prendront la place du repli dès qu'ils
    /// existeront (relancer l'import du catalogue).
    public static class SonsDuJeu
    {
        public static readonly string[] EpeeElan = { "epee_elan", "epee_coup", "kenney_rpg_knifeslice" };
        public static readonly string[] EpeeImpact = { "epee_impact", "skeleton_hit" };
        public static readonly string[] Blocage = { "shield_block" };
        public static readonly string[] Parade = { "parry" };
        public static readonly string[] Charge = { "knight_charge" };
        public static readonly string[] ChargeImpact = { "knight_charge_impact", "viking_leap_land" };
        public static readonly string[] Soin = { "knight_heal" };
        public static readonly string[] Saut = { "jump", "kenney_rpg_cloth" };
        public static readonly string[] Reception = { "land" };
        public static readonly string[] Esquive = { "dodge" };
        public static readonly string[] Pas = { "kenney_rpg_footstep" };
        public static readonly string[] JoueurTouche = { "player_hurt" };
        public static readonly string[] JoueurMort = { "player_death" };
        public static readonly string[] Reapparition = { "respawn" };

        public static readonly string[] SqueletteSortie = { "skeleton_spawn" };
        public static readonly string[] SquelettePreparation = { "kenney_rpg_drawknife" };
        public static readonly string[] SqueletteTouche = { "skeleton_hit" };
        public static readonly string[] SqueletteMort = { "skeleton_death" };
        public static readonly string[] SqueletteAube = { "dawn_vaporize" };
        public static readonly string[] GolemCoup = { "golem_coup", "viking_leap_land" };
        public static readonly string[] Invocation = { "necromancien_invocation", "necro_summon" };

        public static readonly string[] MissileVol = { "skull_flight_loop" };
        public static readonly string[] MissileEclat = { "skull_explosion" };
        public static readonly string[] NyxessaTir = { "relic_pulse" };
        public static readonly string[] NyxessaFrappee = { "relic_hit" };
        public static readonly string[] NyxessaAlerte = { "nyxessa_alerte" };
        public static readonly string[] NyxessaDestruction = { "nyxessa_destruction", "shield_break" };

        public static readonly string[] PortailOuverture = { "portal_open" };
        public static readonly string[] PortailFermeture = { "portal_close" };
        public static readonly string[] PortailBourdon = { "portal_hum_loop" };

        public static readonly string[] AlerteNuit = { "donjon_alerte_nuit", "ui_decompte" };
        public static readonly string[] TombeeNuit = { "nightfall" };
        public static readonly string[] Aube = { "dawn" };
        public static readonly string[] Vague = { "nuit_vague" };
        public static readonly string[] Pret = { "vote_pret", "ui_confirmation" };
        public static readonly string[] PretAnnule = { "vote_annule", "ui_retour" };
        public static readonly string[] TousPrets = { "vote_tous_prets", "ui_confirmation" };
        public static readonly string[] ChargePortail = { "nyxessa_charge_portail" };
        public static readonly string[] RetourEnergie = { "nyxessa_retour_energie" };
        public static readonly string[] EnergieMort = { "nyxessa_onde_passage" };
        public static readonly string[] Victoire = { "partie_victoire", "dawn" };

        public static readonly string[] MusiqueJour = { "musique_dehors_jour" };
        public static readonly string[] MusiqueNuit = { "musique_dehors_nuit" };
    }
}
