using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// État d'un emplacement de la barre de compétences, côté jeu (traduit en EtatCompetence par HudPresenter).
    public enum EtatEmplacement { Vide, Pret, Recharge, Actif, Indisponible }

    /// Ce qu'une classe ajoute au héros commun (Heros) : attaques, compétences, maintiens, jauge, emplacements du HUD.
    /// Heros garde la vie, l'endurance, le déplacement, le sprint, le saut, l'esquive, la mort et la réapparition ; il
    /// délègue à la classe les actions RT, LT, LB, RB (et LB + RB) et lui demande à chaque image comment se déplacer.
    /// Une sous-classe par classe jouable : ClassePaladin, ClasseMage, ClasseRodeur, ClasseAssassin, ClasseViking.
    public abstract class ClasseHeros : MonoBehaviour
    {
        protected Heros H { get; private set; }
        protected GameBalance B => GameBalance.Courant;
        protected Animator Anim => H != null ? H.animator : null;

        /// Identifiant (catalogue ClassesJouables) : « paladin », « mage », « rodeur », « assassin », « viking ».
        public abstract string Id { get; }
        public abstract float PvMax { get; }
        public virtual float Vitesse => B.vitesse;

        public virtual void Initialiser(Heros heros) { H = heros; }

        // ----------------------------------------------------------------- Déplacement demandé à Heros

        /// Une action de classe est en cours : pas de saut, pas de sprint, pas d'autre action.
        public virtual bool Occupe => false;
        /// L'esquive commune (B) est-elle permise maintenant ? (elle interrompt l'action de classe)
        public virtual bool PeutEsquiver => true;
        /// Facteur de vitesse du déplacement libre (garde, visée, cône…).
        public virtual float FacteurVitesse => Occupe ? 0.25f : 1f;
        public virtual bool BloqueSprint => false;
        /// Le héros se tourne vers la visée (caméra) au lieu de la direction de marche.
        public virtual bool FaceVisee => false;
        /// Déplacement imposé (ruée, bond, roulade) : vrai et vitesse monde, ou faux (déplacement libre).
        public virtual bool DeplacementImpose(float dt, out Vector3 vitesse) { vitesse = Vector3.zero; return false; }
        /// Couche « haut du corps » active (garde, visée, cône…).
        public virtual bool HautDuCorps => false;
        /// Vitesse de l'animation de marche quand la classe ralentit (marche discrète…).
        public virtual float FacteurAnimation => 1f;

        // ----------------------------------------------------------------- Événements

        /// Action résolue par InputChordResolver (AttackPrimary, AttackSecondary, Skill1, Skill2, Skill3).
        public virtual void SurAction(string action) { }
        /// Chaque image tant que le héros est vivant (recharges, jauges), même pendant une esquive ou un étourdissement.
        public virtual void Temps(float dt) { }
        /// Chaque image (vivant, en jeu), avant le déplacement ; `dir` : direction d'entrée relative à la caméra.
        public abstract void Maj(float dt, Vector3 dir);
        /// Garde du Paladin : interception d'un coup parable.
        public virtual Interception Intercepter(InfoDegats info) => Interception.Passe;
        public virtual void SurIntercepte(InfoDegats info, Interception r) { }
        /// Coup reçu (dégâts réels).
        public virtual void SurTouche(InfoDegats info, float reel) { }
        /// Dégâts infligés par ce héros (réels) : rage du viking, mana du mage…
        public virtual void SurCoupDonne(Sante cible, float reel, bool parBoule) { }
        /// Esquive, mort, étourdissement : l'action en cours s'arrête proprement (effets, sons, animation).
        public virtual void Interrompre() { }
        /// Heros a heurté le décor sur le côté pendant un déplacement imposé.
        public virtual void SurCollisionCote() { }

        // ----------------------------------------------------------------- HUD

        public virtual JaugeClasse Jauge => JaugeClasse.Aucune;
        public virtual float ValeurJauge => 0f;
        public virtual float JaugeMax => 0f;
        public virtual bool Furtif => false;
        /// Tests : remplit la jauge de la classe (mana, rage).
        public virtual void RemplirJauge() { }

        /// Emplacements 0 à 3 : RT, LT, LB, RB (Vide pour un emplacement sans action).
        public abstract EtatEmplacement Emplacement(int index, out float restant, out float total);

        protected static EtatEmplacement Recharge(float restant, float total, out float r, out float t, bool actif = false)
        {
            r = restant; t = total;
            if (actif) return EtatEmplacement.Actif;
            return restant > 0f ? EtatEmplacement.Recharge : EtatEmplacement.Pret;
        }
    }
}
