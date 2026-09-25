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

        public static readonly string[] BouclierLeve = { "shield_raise" };
        public static readonly string[] BouclierTouche = { "shield_hit" };
        public static readonly string[] BouclierBrise = { "shield_break" };
        public static readonly string[] SorcierIncantation = { "sorcier_incantation" };
        public static readonly string[] Or = { "kenney_rpg_handlecoins" };
        public static readonly string[] PalierAchete = { "nyxessa_palier", "ui_confirmation" };
        public static readonly string[] AchatRefuse = { "ui_refus" };
        public static readonly string[] PointDepense = { "ui_confirmation" };
        public static readonly string[] PointGagne = { "vote_tous_prets", "ui_confirmation" };

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

        // Classes
        public static readonly string[] Critique = { "coup_critique" };
        public static readonly string[] CritiqueMeilleur = { "coup_critique_meilleur", "coup_critique" };
        public static readonly string[] BouleLancer = { "fireball_cast" };
        public static readonly string[] BouleVol = { "fireball_flight_loop" };
        public static readonly string[] BouleExplosion = { "fireball_explosion" };
        public static readonly string[] Cone = { "mage_flame_cone_loop" };
        public static readonly string[] Brulure = { "burn_loop", "brulure" };
        public static readonly string[] ArcBander = { "arc_bander" };
        public static readonly string[] ArcPret = { "arc_charge_complete" };
        public static readonly string[] ArcTir = { "bow_shot_v3", "bow_shot_v2" };
        public static readonly string[] ArcTirCharge = { "bow_shot_v3_charged", "bow_shot_charged" };
        public static readonly string[] FlecheImpact = { "arrow_impact" };
        public static readonly string[] NueeMarqueur = { "nuee_marqueur" };
        public static readonly string[] Nuee = { "arrow_rain" };
        public static readonly string[] Dague = { "dague_coup", "kenney_rpg_knifeslice" };
        public static readonly string[] ArbaleteTir = { "crossbow_shot_v3", "crossbow_shot" };
        public static readonly string[] ArbaleteRecharge = { "arbalete_recharge" };
        public static readonly string[] FurtifEntree = { "assassin_furtif" };
        public static readonly string[] FurtifSortie = { "assassin_furtif_sortie" };
        public static readonly string[] Repere = { "assassin_repere" };
        public static readonly string[] GrenadeLancer = { "grenade_lancer" };
        public static readonly string[] Fumee = { "smoke_bomb" };
        public static readonly string[] Hache = { "hache_coup", "kenney_rpg_chop" };
        public static readonly string[] Tournante = { "whirlwind_loop" };
        public static readonly string[] Rugissement = { "viking_roar" };
        public static readonly string[] SautPercutant = { "viking_leap_land" };

        public static readonly string[] MusiqueJour = { "musique_dehors_jour" };
        public static readonly string[] MusiqueNuit = { "musique_dehors_nuit" };
    }
}
