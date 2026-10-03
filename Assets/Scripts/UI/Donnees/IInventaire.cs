namespace Deathless.UI.Donnees
{
    /// Potions, clés et crochets du joueur local (03/10/2026, boutiques du mécano et du druide ; wiki : village.md, commandes.md) :
    /// le jeu la pose dans DonneesUI.Inventaire ; le HUD affiche, près de la jauge de vie, une pastille par potion (icône,
    /// quantité, bouton : croix haut / 1 santé, croix gauche / 2 mana, croix droite / 3 endurance) et, quand il y en a, les clés
    /// et les crochets. Absente : rien n'est affiché. Locale au joueur.
    public interface IInventaireJoueur
    {
        /// Quantité de la potion `sorte` (0 santé, 1 mana, 2 endurance).
        int Potions(int sorte);
        /// Maximum de chaque sorte de potion portée.
        int PotionsMax { get; }
        /// La classe du joueur peut utiliser cette potion (la potion de mana est réservée au Mage) : faux, la pastille est masquée.
        bool PotionUtilisable(int sorte);
        /// Quantité de clés `sorte` (0 bronze, 1 argent, 2 or) ; à usage unique.
        int Cles(int sorte);
        /// Crochets restants du kit de crochetage.
        int Crochets { get; }

        // Objets portés pour le menu du personnage (Tab), dans l'ordre : 3 potions, 3 clés, le kit de crochetage.
        /// Nombre d'objets décrits (InventaireTaille.Objets).
        int NbObjets { get; }
        string ObjetNom(int i);
        string ObjetDescription(int i);
        /// Identifiant d'icône (IconesUI).
        string ObjetIcone(int i);
        /// Quantité portée (pour le kit : les crochets) et maximum portable.
        int ObjetQuantite(int i);
        int ObjetMax(int i);
        /// Faux : l'objet est inutile à cette classe (potion de mana hors Mage) : la ligne est masquée.
        bool ObjetUtilisable(int i);
    }

    /// Mini-jeu de crochetage d'une serrure (03/10/2026 ; wiki : donjon.md) : on maintient Interagir pour monter l'aiguille et on
    /// la garde dans la zone qui bouge jusqu'à remplir la jauge ; un essai raté casse un crochet. Posé par le jeu dans
    /// DonneesUI.Crochetage ; le HUD le dessine tant qu'il est actif. Locale au joueur qui crochète.
    public interface IEtatCrochetage
    {
        bool Actif { get; }
        /// Nom de la serrure (« Serrure de bronze »).
        string Serrure { get; }
        /// Position de l'aiguille et de la zone sur la piste (0 à 1) ; largeur de la zone (part de la piste).
        float Aiguille { get; }
        float ZoneCentre { get; }
        float ZoneLargeur { get; }
        /// Remplissage de la jauge de réussite (0 à 1) et temps restant à l'essai (0 à 1, part du temps total).
        float Progression { get; }
        float TempsRestant { get; }
        /// Essai en cours (à partir de 1), nombre d'essais, crochets restants.
        int Essai { get; }
        int EssaisMax { get; }
        int Crochets { get; }
        /// L'aiguille est dans la zone en ce moment.
        bool Dans { get; }
        /// Dernier événement (« Le crochet casse ! », « Crochetée ! ») ou vide, et s'il est un échec.
        string Message { get; }
        bool MessageEchec { get; }
    }
}

namespace Deathless.UI.Donnees
{
    /// Nombre de sortes de potions et de clés (les index de IInventaireJoueur vont de 0 à ces nombres moins un).
    public static class InventaireTaille
    {
        public const int Potions = 3, Cles = 3, Objets = 7;
    }
}
