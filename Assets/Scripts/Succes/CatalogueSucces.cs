using System.Collections.Generic;

namespace Deathless.Succes
{
    /// Un succès (Wiki/pages/succes.md) : identifiant Steam `ACH_…` (stable une fois publié), nom et description affichés
    /// (traduisibles plus tard), caché jusqu'au déblocage, et pour les succès à compteur la statistique `STAT_…` et son
    /// seuil. `Masque` : la statistique est un champ de bits (classes jouées, versions de Morgrim) dont on compte les bits.
    /// `Equipe` : succès d'équipe, décidé par l'hôte et donné à tous les joueurs présents (ServiceSucces.Equipe).
    public sealed class DefSucces
    {
        public string Id, Nom, Description, Categorie, Stat;
        public int Seuil = 1;
        public bool Cache, Equipe, Masque;
        /// Icône (identifiant de la table IconesUI) : une icône générique pour tous en attendant les icônes par succès.
        public string Icone = CatalogueSucces.IconeGenerique;
    }

    /// Catalogue des succès, en code (table unique, dans l'ordre du wiki). Ajouter un succès : une ligne ici, son
    /// déclencheur dans ServiceSucces / SuiviSucces, et la même entrée dans Steamworks (« Stats & Achievements »).
    public static class CatalogueSucces
    {
        public const string IconeGenerique = "succes_generique";

        // Statistiques (entiers, comme les stats Steam). Les trois premières sont celles du wiki ; les autres sont ajoutées
        // par l'implémentation (01/10/2026) pour les succès à cumul ou à collection.
        public const string StatParades = "STAT_PARADES_PARFAITES";
        public const string StatTetes = "STAT_TIRS_TETE";
        public const string StatDos = "STAT_COUPS_DOS";
        public const string StatCoffres = "STAT_COFFRES";
        public const string StatPeauDeFer = "STAT_PEAU_DE_FER";
        public const string StatClasses = "STAT_CLASSES_NUIT";        // bits : paladin, viking, mage, rodeur, assassin
        public const string StatMorgrim = "STAT_MORGRIM_VERSIONS";    // bits : massue, martache

        public const string PremierePartie = "Premiers pas", Nuit = "Tenir la nuit", Boss = "Nyxar et Morgrim",
            Classes = "Classes", Village = "Village, donjon et taverne", Rire = "Pour rire";

        public static readonly IReadOnlyList<DefSucces> Tous = new List<DefSucces>
        {
            // Premiers pas
            S("ACH_NUIT_1", "Première aube", "Survivre à la première nuit.", PremierePartie, equipe: true),
            S("ACH_PREMIERE_MORT", "Ça sent le sapin", "Mourir pour la première fois (on revient tant que Nyxessa tient).", PremierePartie),
            S("ACH_PREMIER_COFFRE", "Pilleur novice", "Ouvrir son premier coffre au donjon.", PremierePartie, stat: StatCoffres, seuil: 1),
            S("ACH_PREMIER_PALIER", "Mécène de la relique", "Acheter un premier palier de Nyxessa (missiles ou bouclier).", PremierePartie),
            S("ACH_QUATUOR", "Bien entouré", "Jouer une partie à 4 joueurs.", PremierePartie, equipe: true),
            S("ACH_CINQ_CLASSES", "Touche-à-tout", "Finir une nuit avec chacune des cinq classes.", PremierePartie, stat: StatClasses, seuil: 5, masque: true),
            // Tenir la nuit
            S("ACH_NUIT_6", "Mi-chemin", "Atteindre la nuit 6.", Nuit, equipe: true),
            S("ACH_MORGRIM", "Le Roi des os est tombé", "Vaincre Morgrim.", Nuit, cache: true, equipe: true),
            S("ACH_VICTOIRE", "Deathless", "Vaincre Nyxar et voir l’aube de la nuit 12.", Nuit, cache: true, equipe: true),
            S("ACH_NYXESSA_INTACTE", "Sans une égratignure", "Gagner une partie sans que Nyxessa ne perde un point de vie à la nuit 12.", Nuit, equipe: true),
            S("ACH_BOUCLIER_TIENT", "Le bouclier tient", "Finir une nuit sans que le bouclier ne soit brisé, à partir de la nuit 8.", Nuit, equipe: true),
            S("ACH_DEATHLESS_EQUIPE", "Personne ne tombe", "Gagner une partie à 2 joueurs ou plus sans aucune mort.", Nuit, equipe: true),
            S("ACH_MORGRIM_DEUX", "Les deux visages", "Vaincre Morgrim à la massue et Morgrim à la martache (deux parties).", Nuit, stat: StatMorgrim, seuil: 2, masque: true),
            // Nyxar et Morgrim (cachés)
            S("ACH_ECLAT_COURONNE", "Couronne brisée", "Briser l’éclat de la couronne de Nyxar.", Boss, cache: true, equipe: true),
            S("ACH_ECLAT_GRIMOIRE", "Grimoire fermé", "Briser l’éclat du grimoire de Nyxar.", Boss, cache: true, equipe: true),
            S("ACH_ECLATS_INVERSE", "Ordre inverse", "Briser le grimoire avant la couronne.", Boss, cache: true, equipe: true),
            S("ACH_MORGRIM_MUET", "Pas le temps de crier", "Tuer Morgrim avant qu’il ait poussé son cri.", Boss, cache: true, equipe: true),
            S("ACH_NUIT_LONGUE", "L’aube attendra", "Que la nuit se prolonge plus d’une minute avant la chute du boss.", Boss, cache: true, equipe: true),
            // Classes
            S("ACH_PARADES_100", "Mur de fer", "Paladin : réussir 100 parades parfaites (cumul).", Classes, stat: StatParades, seuil: 100),
            S("ACH_SOIN_TRIPLE", "L’épée et le baume", "Paladin : soigner 3 alliés d’un seul soin d’aura.", Classes),
            S("ACH_TOURNANTE_10", "Berserk", "Viking : toucher 10 ennemis d’une seule attaque tournante.", Classes),
            S("ACH_PEAU_DE_FER", "Peau de fer", "Viking : encaisser 500 dégâts sous Peau de fer (cumul).", Classes, stat: StatPeauDeFer, seuil: 500),
            S("ACH_BRASIER_8", "Feu de joie", "Mage : porter 8 ennemis au palier 3 de brûlure en même temps.", Classes),
            S("ACH_MUR_20", "Vous ne passerez pas", "Mage : faire traverser son mur de flammes à 20 ennemis en une seule pose.", Classes),
            S("ACH_GRANDE_BOULE_6", "Grande boule, grand ménage", "Mage : tuer 6 ennemis d’une seule grande boule de feu.", Classes),
            S("ACH_TETES_1000", "Dans le mille", "Rôdeur : 1 000 tirs à la tête (cumul).", Classes, stat: StatTetes, seuil: 1000),
            S("ACH_CLOUE", "Cloué sur place", "Rôdeur : étourdir un élite d’une flèche à pleine charge puis le tuer avant qu’il ne se reprenne.", Classes),
            S("ACH_FURTIF_5", "Personne ne m’a vu", "Assassin : tuer 5 ennemis à la suite sans quitter le mode furtif.", Classes),
            S("ACH_EXECUTIONS_4", "Enchaînement", "Assassin : enchaîner 4 exécutions en moins de 10 s.", Classes),
            S("ACH_DOS_500", "Dans le dos, toujours", "Assassin : 500 coups dans le dos (cumul).", Classes, stat: StatDos, seuil: 500),
            // Village, donjon et taverne
            S("ACH_COFFRES_50", "Rat de donjon", "Ouvrir 50 coffres (cumul).", Village, stat: StatCoffres, seuil: 50),
            S("ACH_JUSTE_A_TEMPS", "Juste à temps", "Repasser le portail du donjon moins de 3 s avant sa fermeture.", Village),
            S("ACH_SAC_ALLIE", "Ce qui est à toi est à moi", "Ramasser le sac d’or d’un allié mort au donjon.", Village),
            S("ACH_CAISSE_1000", "Caisse pleine", "Avoir 1 000 or dans la caisse commune.", Village, equipe: true),
            S("ACH_IVRESSE", "La tournée du patron", "Boire à la taverne jusqu’à l’ivresse maximale.", Village, cache: true),
            S("ACH_EMOTE_MORGRIM", "Danse de la victoire", "Faire une emote sur le cadavre encore chaud de Morgrim.", Village),
            // Pour rire (cachés)
            S("ACH_RECONNEXION", "Je reviens tout de suite", "Revenir dans une partie en cours après une coupure (par le même code).", Rire, cache: true),
            S("ACH_HOTE_SEUL", "Seul contre tous", "Finir une nuit seul après la perte de connexion de l’hôte… qui était soi.", Rire, cache: true),
            S("ACH_SAUVE_PAR_NYX", "Tir ami… presque", "Être sauvé par Nyxessa : un missile tue l’ennemi qui allait vous achever.", Rire, cache: true),
            S("ACH_VOLEUR_VOLE", "Le voleur volé", "Tuer un voleur squelette qui vous chassait parce que vous étiez isolé.", Rire, cache: true),
            S("ACH_BASSIN", "Les pieds dans l’eau", "Rester 60 s d’affilée dans l’eau du bassin du donjon.", Rire, cache: true),
        };

        static Dictionary<string, DefSucces> s_Index;

        public static DefSucces Trouver(string id)
        {
            if (s_Index == null)
            {
                s_Index = new Dictionary<string, DefSucces>();
                foreach (var d in Tous) s_Index[d.Id] = d;
            }
            return id != null && s_Index.TryGetValue(id, out var r) ? r : null;
        }

        /// Classes jouables et leur bit dans STAT_CLASSES_NUIT.
        public static readonly string[] ClassesJouables = { "paladin", "viking", "mage", "rodeur", "assassin" };

        public static int BitClasse(string classeId)
        {
            int i = System.Array.IndexOf(ClassesJouables, classeId);
            return i >= 0 ? 1 << i : 0;
        }

        /// Nombre de bits à 1 (progression d'une statistique masque).
        public static int Bits(int v)
        {
            int n = 0;
            for (; v != 0; v &= v - 1) n++;
            return n;
        }

        static DefSucces S(string id, string nom, string description, string categorie, bool cache = false, bool equipe = false,
            string stat = null, int seuil = 1, bool masque = false)
            => new DefSucces { Id = id, Nom = nom, Description = description, Categorie = categorie, Cache = cache, Equipe = equipe, Stat = stat, Seuil = seuil, Masque = masque };
    }
}
