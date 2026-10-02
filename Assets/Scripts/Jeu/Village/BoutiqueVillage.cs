using System.Collections.Generic;
using Deathless.Reseau;
using Deathless.UI.Donnees;
using UnityEngine;

namespace Deathless.Jeu
{
    /// Boutique d'un villageois de la carte v5 (03/10/2026, modèle : Taverne) : de jour seulement, devant le vendeur, la touche
    /// Interagir ouvre le menu d'achat (EcranAchat) ; l'or est pris dans la caisse commune, l'autorité décide (hôte en
    /// multijoueur) et ce qui est acheté va dans l'inventaire du joueur (EtatJoueur : potions, clés, crochets ; Inventaire).
    /// Deux boutiques : Mecano (clés de bronze, d'argent et d'or, kit de crochetage) et Druide (potions de santé, de mana,
    /// d'endurance). Posées sur les ancres d'échange Villageois/Ancre_Echange_Mecano et Ancre_Echange_Druide par le
    /// builder de la carte (VillageV5Villageois, étape 1b) et, à défaut, par Boutiques.Assurer au démarrage de la partie.
    public abstract class BoutiqueVillage : PointInteraction, IMenuAchat
    {
        sealed class Ligne : IArticleAchat
        {
            public ArticleBoutique a;
            public string Nom { get; set; }
            public string Description { get; set; }
            public string Niveau { get; set; } = "";
            public int Prix { get; set; }
            public bool Achetable { get; set; }
            public float cleTexte = float.NaN;   // valeur de réglage affichée dans Description (texte refait si elle change)
            public int quantiteVue = -1, maxVu = -1;
        }

        readonly List<IArticleAchat> m_Lignes = new List<IArticleAchat>();
        Heros m_Heros;
        static string s_Message = "";
        static bool s_Refus;

        protected abstract ArticleBoutique[] Catalogue { get; }
        protected abstract string TitreBoutique { get; }
        protected abstract string InviteBoutique { get; }
        protected abstract string SousTitreBoutique { get; }
        /// Début de la phrase de refus hors de la journée (« Le mécano ne vend que de jour. »).
        protected abstract string RefusNuit { get; }

        Partie P => Partie.Instance;
        GameBalance B => GameBalance.Courant;

        protected virtual void Awake()
        {
            foreach (var a in Catalogue) m_Lignes.Add(new Ligne { a = a, Nom = Inventaire.Nom(a) });
        }

        float Distance(Heros h)
        {
            Vector3 d = h.transform.position - transform.position; d.y = 0f;
            return d.magnitude;
        }

        bool Disponible(Heros h) => h != null && h.Vivant && P != null && P.EnCours && P.Etat.phase == Phase.Jour;

        public override string Invite(Heros h, out float distance)
        {
            distance = h != null ? Distance(h) : float.MaxValue;
            return Disponible(h) && !h.Distant && !Crochetage.Bloque && distance <= B.boutiqueDistance ? InviteBoutique : null;
        }

        public override void Interagir(Heros h)
        {
            m_Heros = h;
            s_Message = "";
            DonneesUI.OuvrirMenuAchat(this);
        }

        // ----------------------------------------------------------------- IMenuAchat

        public string Titre => TitreBoutique;
        public string SousTitre => SousTitreBoutique;
        public int Or => P != null ? P.Etat.orEquipe : 0;
        public string LegendeOr => "caisse commune";
        public string Message => s_Message;
        public bool MessageRefus => s_Refus;
        public bool Ouvert => Disponible(m_Heros) && Distance(m_Heros) <= B.boutiqueDistance + 1.5f;

        public IReadOnlyList<IArticleAchat> Articles
        {
            get
            {
                var b = B;
                var j = P != null ? P.JoueurLocal : null;
                foreach (Ligne l in m_Lignes)
                {
                    l.Prix = Inventaire.Prix(l.a);
                    var raison = Boutiques.Verifier(l.a, j, m_Heros, P);
                    l.Achetable = raison == Boutiques.Refus.Aucun;
                    // Lu à chaque image par le menu ouvert (EcranAchat) : textes refaits seulement quand une valeur change.
                    int q = Inventaire.Quantite(j, l.a), max = Inventaire.Max(l.a);
                    if (q != l.quantiteVue || max != l.maxVu)
                    {
                        l.quantiteVue = q; l.maxVu = max;
                        l.Niveau = (l.a == ArticleBoutique.KitCrochetage ? "Crochets : " : "Possédé : ") + q + " / " + max;
                    }
                    float cle = b.potionSantePart * 1000f + b.potionManaPart * 100f + b.potionEnduranceDuree + b.potionEnduranceFacteur * 7f + b.crochetsParKit * 31f + b.clesMaxParSorte * 13f;
                    bool reserve = l.a == ArticleBoutique.PotionMana && raison == Boutiques.Refus.Classe;
                    float cleLigne = cle + (reserve ? 0.5f : 0f);
                    if (cleLigne != l.cleTexte) { l.cleTexte = cleLigne; l.Description = Boutiques.Description(l.a, reserve); }
                }
                return m_Lignes;
            }
        }

        public void Acheter(int index)
        {
            if (P == null || index < 0 || index >= m_Lignes.Count) return;
            var a = ((Ligne)m_Lignes[index]).a;
            var raison = Boutiques.Verifier(a, P.JoueurLocal, m_Heros, P);
            if (raison != Boutiques.Refus.Aucun)
            {
                s_Message = Boutiques.Texte(raison, a, RefusNuit); s_Refus = true;
                AudioBank.Jouer2D(SonsDuJeu.AchatRefuse, 0.7f);
                return;
            }
            s_Message = Boutiques.Payer(a, m_Heros != null ? m_Heros.Id : 1, out bool ok);
            s_Refus = !ok;
        }

        /// Réponse de l'hôte (client) : message et, si l'achat est fait, son effet sur l'inventaire du joueur local.
        public static void Reponse(string message, bool ok, ArticleBoutique a)
        {
            s_Message = message; s_Refus = !ok;
            if (ok) Boutiques.AppliquerLocal(a);
        }
    }

    /// Mécano (vendeur traditionnel) : clés de bronze, d'argent et d'or, kit de crochetage.
    public class Mecano : BoutiqueVillage
    {
        static readonly ArticleBoutique[] s_Catalogue = { ArticleBoutique.CleBronze, ArticleBoutique.CleArgent, ArticleBoutique.CleOr, ArticleBoutique.KitCrochetage };
        protected override ArticleBoutique[] Catalogue => s_Catalogue;
        protected override string TitreBoutique => "Mécano";
        protected override string InviteBoutique => "Mécano : acheter";
        protected override string SousTitreBoutique => "Clés à usage unique et kit de crochetage. L’or est pris dans la caisse commune.";
        protected override string RefusNuit => "Le mécano ne vend que de jour.";
    }

    /// Druide : potions de santé, de mana (Mage seulement) et d'endurance.
    public class Druide : BoutiqueVillage
    {
        static readonly ArticleBoutique[] s_Catalogue = { ArticleBoutique.PotionSante, ArticleBoutique.PotionMana, ArticleBoutique.PotionEndurance };
        protected override ArticleBoutique[] Catalogue => s_Catalogue;
        protected override string TitreBoutique => "Druide";
        protected override string InviteBoutique => "Druide : potions";
        protected override string SousTitreBoutique => "Potions à boire à la croix directionnelle ou aux touches 1, 2, 3. L’or est pris dans la caisse commune.";
        protected override string RefusNuit => "Le druide ne vend que de jour.";
    }

    /// Règles d'achat des boutiques du village (partagées par les deux vendeurs) : raisons de refus, paiement par l'autorité,
    /// effet sur l'inventaire, pose des composants sur les ancres.
    public static class Boutiques
    {
        public enum Refus { Aucun, Nuit, Classe, Maximum, Or }

        public static GameObject AncreMecano() => GameObject.Find("VillageBlockout/Villageois/Ancre_Echange_Mecano");
        public static GameObject AncreDruide() => GameObject.Find("VillageBlockout/Villageois/Ancre_Echange_Druide");

        /// Carte v5 : pose les composants Mecano et Druide sur leurs ancres d'échange (déjà posés par le builder, ils sont gardés) ;
        /// une ancre absente (scène construite avant les boutiques) est créée à un mètre devant le villageois, comme le builder.
        /// Rien sur l'ancienne carte (ni mécano ni druide). Renvoie le nombre de boutiques en place.
        public static int Assurer()
        {
            int n = 0;
            if (Poser<Mecano>(AncreMecano(), "Mecano", "Ancre_Echange_Mecano")) n++;
            if (Poser<Druide>(AncreDruide(), "Druide", "Ancre_Echange_Druide")) n++;
            return n;
        }

        static bool Poser<T>(GameObject ancre, string villageois, string nomAncre) where T : BoutiqueVillage
        {
            if (ancre == null)
            {
                var pnj = GameObject.Find("VillageBlockout/Villageois/" + villageois + "/" + villageois);
                var groupe = GameObject.Find("VillageBlockout/Villageois");
                if (pnj == null || groupe == null) return false;
                ancre = new GameObject(nomAncre);
                ancre.transform.SetParent(groupe.transform, false);
                ancre.transform.SetPositionAndRotation(PosteAncre(pnj.transform), pnj.transform.rotation);
            }
            if (ancre.GetComponent<T>() == null) ancre.AddComponent<T>();
            return true;
        }

        /// Ancre d'échange : un mètre devant le villageois, à hauteur de comptoir (même règle que le builder et la taverne).
        public static Vector3 PosteAncre(Transform villageois) => villageois.position + villageois.forward * 1.0f + Vector3.up * 0.95f;

        // ----------------------------------------------------------------- Règles

        /// Pourquoi cet achat est impossible pour ce joueur (Aucune : possible). `h` : son héros (classe de la potion de mana).
        public static Refus Verifier(ArticleBoutique a, EtatJoueur j, Heros h, Partie p)
        {
            if (p == null || p.Etat.phase != Phase.Jour) return Refus.Nuit;
            if (Inventaire.EstPotion(a) && !Inventaire.PotionUtilisable(Inventaire.DePotion(a), h)) return Refus.Classe;
            int q = Inventaire.Quantite(j, a);
            int fin = a == ArticleBoutique.KitCrochetage ? q + Mathf.Max(1, GameBalance.Courant.crochetsParKit) : q + 1;
            if (fin > Inventaire.Max(a)) return Refus.Maximum;
            if (p.Etat.orEquipe < Inventaire.Prix(a)) return Refus.Or;
            return Refus.Aucun;
        }

        public static string Texte(Refus r, ArticleBoutique a, string refusNuit)
        {
            switch (r)
            {
                case Refus.Nuit: return refusNuit;
                case Refus.Classe: return "Cette potion est réservée au Mage.";
                case Refus.Maximum:
                    return a == ArticleBoutique.KitCrochetage ? "Vous portez déjà assez de crochets (" + Inventaire.Max(a) + " au plus)."
                        : Inventaire.Nom(a) + " : vous en portez déjà " + Inventaire.Max(a) + ", le maximum.";
                case Refus.Or: return "Pas assez d’or dans la caisse commune.";
                default: return "";
            }
        }

        public static string Description(ArticleBoutique a, bool reservee)
        {
            var b = GameBalance.Courant;
            switch (a)
            {
                case ArticleBoutique.CleBronze: return "Ouvre une serrure de bronze, une seule fois. " + b.clesMaxParSorte + " au plus.";
                case ArticleBoutique.CleArgent: return "Ouvre une serrure d’argent, une seule fois. " + b.clesMaxParSorte + " au plus.";
                case ArticleBoutique.CleOr: return "Ouvre une serrure d’or, une seule fois. " + b.clesMaxParSorte + " au plus. L’or ne se crochète pas.";
                case ArticleBoutique.KitCrochetage: return b.crochetsParKit + " crochets pour les serrures simples, de bronze et d’argent. Un crochet casse à chaque échec.";
                case ArticleBoutique.PotionSante: return "Rend " + Mathf.RoundToInt(b.potionSantePart * 100f) + " % des points de vie. Croix haut ou touche 1.";
                case ArticleBoutique.PotionMana:
                    return reservee ? "Réservée au Mage : votre classe ne peut ni l’acheter ni la boire."
                        : "Rend " + Mathf.RoundToInt(b.potionManaPart * 100f) + " % de la jauge de mana. Réservée au Mage. Croix gauche ou touche 2.";
                default: return "Rend toute l’endurance, puis " + b.potionEnduranceDuree.ToString("0") + " s de récupération ×" + b.potionEnduranceFacteur.ToString("0.#") + ". Croix droite ou touche 3.";
            }
        }

        // ----------------------------------------------------------------- Paiement et effet

        /// Achat : l'autorité (hôte, ou ce poste en solo) décide et prend l'or ; un client transmet sa demande à l'hôte, qui
        /// décide et répond (BoutiqueVillage.Reponse applique alors l'effet chez lui).
        public static string Payer(ArticleBoutique a, int joueurId, out bool ok)
        {
            ok = false;
            var p = Partie.Instance;
            if (p == null) return "";
            if (Partie.ClientReseau) { PartieReseau.Instance?.DemanderBoutique((byte)a); return "Commande passée…"; }
            var j = p.Joueur(joueurId);
            var r = Verifier(a, j, p.HerosDe(joueurId), p);
            if (r != Refus.Aucun) return Texte(r, a, "Le vendeur ne sert que de jour.");
            int prix = Inventaire.Prix(a);
            p.Etat.orEquipe -= prix;
            ok = true;
            p.Journal("Boutique : " + Inventaire.Nom(a) + " (" + prix + " or, par " + (j != null ? j.nom : "Joueur") + ")");
            if (p.JoueurLocal != null && joueurId == p.JoueurLocal.id) AppliquerLocal(a);
            else Inventaire.Ajouter(j, a);   // héros d'un autre poste : l'hôte tient sa copie à jour, le propriétaire l'applique à la réponse
            return Message(a, prix);
        }

        public static string Message(ArticleBoutique a, int prix)
        {
            string base_ = Inventaire.Nom(a) + (a == ArticleBoutique.KitCrochetage ? " acheté" : " achetée");
            return base_ + " (" + prix + " or)" + (a == ArticleBoutique.KitCrochetage ? " : +" + GameBalance.Courant.crochetsParKit + " crochets." : ".");
        }

        /// Effet d'un article sur le joueur local : dans son inventaire, avec le son de la boutique.
        public static void AppliquerLocal(ArticleBoutique a)
        {
            var p = Partie.Instance; var h = p != null ? p.HerosLocal : null;
            if (p == null) return;
            Inventaire.Ajouter(p.JoueurLocal, a);
            Vector3 point = h != null ? h.transform.position + Vector3.up : Vector3.zero;
            AudioBank.Jouer(Inventaire.EstPotion(a) ? SonsDuJeu.AchatBoutique : SonsDuJeu.AchatCle, point, 0.8f);
        }
    }
}
