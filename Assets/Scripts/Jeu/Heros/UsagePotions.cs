using UnityEngine;

namespace Deathless.Jeu
{
    /// Boire une potion (03/10/2026 ; wiki : village.md, commandes.md) : croix haut / touche 1 (DrinkPotion, santé), croix gauche /
    /// 2 (DrinkPotionMana, mana : Mage seulement) et croix droite / 3 (DrinkPotionStamina, endurance). Appelé par Heros.OnAction
    /// chez le propriétaire du héros : l'effet est local (vie, jauge, endurance : ce poste les simule), la quantité diminue dans
    /// EtatJoueur (HerosReseau la réplique à l'hôte). Impossible si la jauge est pleine (rien n'est consommé), s'il n'en reste pas,
    /// pour une potion que la classe ne peut pas boire, ou pendant la recharge d'usage (GameBalance.potionRecharge). Retour :
    /// aura de soin sur le héros et son de potion ; le geste de boire (emote « Boire un coup ») seulement à l'arrêt, pour ne
    /// jamais immobiliser un héros qui se bat. {à confirmer} : visuels distincts par potion et modèle de fiole en main.
    public static class UsagePotions
    {
        static float s_Prochain = -99f;

        /// Secondes avant de pouvoir boire de nouveau (HUD, tests).
        public static float RechargeRestante => Mathf.Max(0f, s_Prochain - Time.time);

        /// Potion liée à une action du jeu, ou faux.
        public static bool DeAction(string action, out Potion p)
        {
            p = Potion.Sante;
            switch (action)
            {
                case "DrinkPotion": p = Potion.Sante; return true;
                case "DrinkPotionMana": p = Potion.Mana; return true;
                case "DrinkPotionStamina": p = Potion.Endurance; return true;
                default: return false;
            }
        }

        /// Raison pour laquelle on ne peut pas boire maintenant (null si c'est possible). Ne tient pas compte de la recharge.
        public static string Refus(Heros h, Potion p)
        {
            if (h == null || h.EtatJoueur == null) return "Aucune potion.";
            if (!Inventaire.PotionUtilisable(p, h)) return "La potion de mana est réservée au Mage.";
            if (Inventaire.Potions(h.EtatJoueur, p) <= 0) return "Plus de " + Inventaire.NomPotion(p).ToLowerInvariant() + ".";
            switch (p)
            {
                case Potion.Sante:
                    if (h.Sante.Pv >= h.Sante.pvMax - 0.5f) return "Vous êtes déjà en pleine forme.";
                    break;
                case Potion.Mana:
                    if (h.Classe == null || h.Classe.ValeurJauge >= h.Classe.JaugeMax - 0.5f) return "Votre mana est déjà au maximum.";
                    break;
                default:
                    if (h.Endurance >= h.EnduranceMax - 0.5f) return "Votre endurance est déjà pleine.";
                    break;
            }
            return null;
        }

        /// Boit la potion `p` (héros local, vivant et en jeu). Renvoie vrai si elle a été bue ; le refus (message du HUD, son)
        /// est joué ici. Pendant la recharge d'usage, rien ne se passe (ni message, ni son).
        public static bool Boire(Heros h, Potion p)
        {
            if (h == null || h.Distant) return false;
            if (Time.time < s_Prochain) return false;
            string refus = Refus(h, p);
            if (refus != null)
            {
                DonjonJeu.Instance?.Annoncer(refus, 2.5f);
                AudioBank.Jouer2D(SonsDuJeu.AchatRefuse, 0.6f);
                return false;
            }
            var b = GameBalance.Courant;
            var j = h.EtatJoueur;
            string effet;
            switch (p)
            {
                case Potion.Sante:
                {
                    float rendu = h.Sante.Soigner(h.Sante.pvMax * b.potionSantePart, h.Id);
                    effet = "+" + rendu.ToString("0") + " points de vie";
                    AudioBank.Jouer(SonsDuJeu.Soin, h.transform.position + Vector3.up, 0.7f);
                    break;
                }
                case Potion.Mana:
                {
                    float rendu = h.Classe.AjouterMana(h.Classe.JaugeMax * b.potionManaPart);
                    effet = "+" + rendu.ToString("0") + " mana";
                    break;
                }
                default:
                    h.RemplirEndurance(b.potionEnduranceDuree, b.potionEnduranceFacteur);
                    effet = "endurance pleine, récupération ×" + b.potionEnduranceFacteur.ToString("0.#") + " pendant " + b.potionEnduranceDuree.ToString("0") + " s";
                    break;
            }
            Inventaire.Retirer(j, p);
            s_Prochain = Time.time + Mathf.Max(0f, b.potionRecharge);
            AudioBank.Jouer(SonsDuJeu.PotionBue, h.transform.position + Vector3.up, 0.8f);
            JouerRetour(h);
            if (h.Partie != null) h.Partie.Journal("Potion : " + Inventaire.NomPotion(p) + " (" + effet + ") ; reste " + Inventaire.Potions(j, p));
            return true;
        }

        /// Aura de soin sous les pieds (effet existant) et geste de boire s'il ne gêne pas (immobile, au sol, sans action en cours).
        static void JouerRetour(Heros h)
        {
            var fx = EffetsJeu.Instance;
            if (fx != null && fx.prefabAuraSoin != null)
            {
                var go = Object.Instantiate(fx.prefabAuraSoin, h.transform);
                go.name = "AuraPotion";
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                var a = go.GetComponent<AuraSoin>();
                if (a != null) a.Jouer();
                Object.Destroy(go, 4f);
            }
            var c = h.Classe;
            if (h.Emotes != null && h.AuSol && h.Entrees != null && h.Entrees.Deplacement.sqrMagnitude < 0.01f && (c == null || (!c.HautDuCorps && !c.FaceVisee)))
                h.Emotes.Lancer(EmotesHeros.Boire, true);
        }
    }
}
