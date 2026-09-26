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

        /// Rang (0 à 3) de l'amélioration de compétence `index` (ArbreCompetences), achetée avec les points de compétence.
        protected int Rang(int index)
        {
            var e = H != null ? H.EtatJoueur : null;
            return e != null && e.rangs != null && index >= 0 && index < e.rangs.Length ? e.rangs[index] : 0;
        }

        /// Facteur de l'amélioration `index` à son rang actuel (1 sans amélioration ; nombre ajouté pour un ajout).
        protected float Facteur(int index) => ArbreCompetences.Facteur(Id, index, Rang(index));

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
        /// Ligne de tir de l'arme tenue en visée (arc bandé, arbalète), telle que l'animation la pose : origine et direction
        /// (monde). Heros tourne le corps et penche le buste pour l'aligner sur le point visé par le réticule (le décalage de
        /// lacet des animations KayKit de tir est compensé). Faux : pas d'alignement (le héros regarde la visée).
        public virtual bool AxeDeTir(out Vector3 origine, out Vector3 direction) { origine = direction = Vector3.zero; return false; }
        /// Déplacement imposé (ruée, bond, roulade) : vrai et vitesse monde, ou faux (déplacement libre).
        public virtual bool DeplacementImpose(float dt, out Vector3 vitesse) { vitesse = Vector3.zero; return false; }
        /// Couche « haut du corps » active (garde, visée, cône…).
        public virtual bool HautDuCorps => false;
        /// Vitesse de l'animation de marche quand la classe ralentit (marche discrète…).
        public virtual float FacteurAnimation => 1f;
        /// Penché voulu du corps vers l'avant (degrés, pivot aux pieds), appliqué par Heros au modèle seul (ni caméra ni
        /// capsule). Lu aussi sur les marionnettes : à déduire de l'état de l'Animator (répliqué), pas de la logique locale.
        public virtual float Penche => 0f;

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
        public virtual void SurCoupDonne(Sante cible, float reel, bool parBoule, bool continu) { }
        /// Esquive, mort, étourdissement : l'action en cours s'arrête proprement (effets, sons, animation).
        public virtual void Interrompre() { }
        /// Heros a heurté le décor sur le côté pendant un déplacement imposé.
        public virtual void SurCollisionCote() { }

        // ----------------------------------------------------------------- Effets visibles par tous (multijoueur)

        /// Effets communs à toutes les classes (numéros 200 et plus ; ceux des classes sont en dessous).
        protected const int EffetCritique = 200;
        public const int EffetEsquive = 201, EffetSaut = 202, EffetTransitDepart = 203, EffetTransitArrivee = 204;

        /// DonjonJeu : passage d'un portail (départ, arrivée) à rejouer chez les autres, à la position donnée ; `portail` :
        /// 0 aucun (rappel), 1 portail du village, 2 portail de retour du donjon (DonjonJeu.PortailDe).
        public void DiffuserTransit(int effet, Vector3 position, int portail = 0) => Diffuser(effet, position, default, portail);

        /// Heros : effet commun (esquive, saut) à rejouer chez les autres.
        public void DiffuserCommun(int effet) => Diffuser(effet);

        /// Propriétaire, en partie réseau : l'effet `effet` vient d'être joué ici (visuel et son) ; les autres postes le
        /// rejouent sur la marionnette de ce héros (EffetDistant). Rien que du visuel et du son : les dégâts restent
        /// décidés comme avant. `a`, `b` : points ou directions, `v` : valeur libre (force, charge, rayon…).
        protected void Diffuser(int effet, Vector3 a = default, Vector3 b = default, float v = 0f)
        {
            if (H == null || H.Distant) return;
            Deathless.Reseau.HerosReseau.Local(H)?.Effet((byte)effet, a, b, v);
        }

        /// Marque de coup critique (Combat.Critique), jouée ici et chez les autres.
        protected void Critique(Vector3 point, Vector3 direction, bool meilleur)
        {
            Combat.Critique(point, direction, meilleur);
            Diffuser(EffetCritique, point, direction, meilleur ? 1f : 0f);
        }

        /// Autre poste : rejoue sur cette marionnette l'effet diffusé par son propriétaire (sans dégâts).
        public virtual void EffetDistant(int effet, Vector3 a, Vector3 b, float v)
        {
            if (effet == EffetCritique) Combat.Critique(a, b, v > 0.5f);
            else if (effet == EffetEsquive) AudioBank.Jouer(SonsDuJeu.Esquive, transform.position + Vector3.up, 0.8f);
            else if (effet == EffetSaut) AudioBank.Jouer(SonsDuJeu.Saut, transform.position, 0.5f);
            else if (effet == EffetTransitDepart) DonjonJeu.TransitDistant(H, a, false, Mathf.RoundToInt(v));
            else if (effet == EffetTransitArrivee) DonjonJeu.TransitDistant(H, a, true, Mathf.RoundToInt(v));
        }

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
