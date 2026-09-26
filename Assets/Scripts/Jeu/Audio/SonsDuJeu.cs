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
        public static readonly string[] NyxessaTir = { "dl_nyxessa_tir", "relic_pulse" };
        public static readonly string[] NyxessaFrappee = { "dl_nyxessa_frappee", "relic_hit" };
        public static readonly string[] NyxessaAlerte = { "dl_nyxessa_alerte", "nyxessa_alerte" };
        public static readonly string[] NyxessaDestruction = { "dl_nyxessa_destruction", "nyxessa_destruction", "shield_break" };

        public static readonly string[] BouclierLeve = { "shield_raise" };
        public static readonly string[] BouclierTouche = { "shield_hit" };
        public static readonly string[] BouclierBrise = { "shield_break" };
        public static readonly string[] SorcierIncantation = { "sorcier_incantation" };
        public static readonly string[] Or = { "kenney_rpg_handlecoins" };
        public static readonly string[] PalierAchete = { "dl_nyxessa_palier", "nyxessa_palier", "ui_confirmation" };
        public static readonly string[] AchatRefuse = { "dl_interface_refus", "ui_refus" };
        public static readonly string[] PointDepense = { "dl_interface_confirmation", "ui_confirmation" };
        public static readonly string[] Repas = { "kenney_rpg_metalpot" };
        public static readonly string[] Biere = { "kenney_rpg_metalclick" };
        public static readonly string[] ForgeEnclume = { "forge_enclume" };   // forgeron : marteau sur l'enclume (3 variantes)
        public static readonly string[] PointGagne = { "dl_interface_tous_prets", "vote_tous_prets", "ui_confirmation" };

        public static readonly string[] PortailOuverture = { "portal_open" };
        public static readonly string[] PortailFermeture = { "portal_close" };
        public static readonly string[] PortailBourdon = { "portal_hum_loop" };
        public static readonly string[] PortailPassage = { "portal_pass", "portail_goutte" };
        public static readonly string[] CoffreCadenas = { "kenney_rpg_metallatch", "kenney_rpg_metalclick" };
        public static readonly string[] CoffreOuvert = { "chest_open", "kenney_rpg_dooropen" };
        public static readonly string[] NyxessaRappel = { "nyxessa_rappel", "dl_nyxessa_onde", "nyxessa_onde_passage" };

        // "dl_interface_decompte" est un repli de dl_nyxessa_alerte / donjon_alerte_nuit ici : chaque seconde du
        // compte à rebours de l'alerte au donjon (§ 8 du cahier des charges son, table « Branchement proposé »).
        public static readonly string[] AlerteNuit = { "dl_nyxessa_alerte", "donjon_alerte_nuit", "dl_interface_decompte", "ui_decompte" };
        public static readonly string[] TombeeNuit = { "nightfall" };
        public static readonly string[] Aube = { "dawn" };
        public static readonly string[] Vague = { "nuit_vague" };
        public static readonly string[] Pret = { "dl_interface_pret", "vote_pret", "ui_confirmation" };
        public static readonly string[] PretAnnule = { "dl_interface_pret_annule", "vote_annule", "ui_retour" };
        public static readonly string[] TousPrets = { "dl_interface_tous_prets", "vote_tous_prets", "ui_confirmation" };
        public static readonly string[] ChargePortail = { "dl_nyxessa_charge_portail", "nyxessa_charge_portail" };
        public static readonly string[] RetourEnergie = { "dl_nyxessa_retour_energie", "nyxessa_retour_energie" };
        public static readonly string[] EnergieMort = { "dl_nyxessa_onde", "nyxessa_onde_passage" };
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
        public static readonly string[] TournanteVent = { "hache_vent" };   // whoosh à chaque tour complet (3 variantes)
        public static readonly string[] Rugissement = { "viking_roar" };
        public static readonly string[] SautPercutant = { "viking_leap_land" };

        public static readonly string[] MusiqueJour = { "musique_dehors_jour" };
        public static readonly string[] MusiqueNuit = { "musique_dehors_nuit" };
    }
}
