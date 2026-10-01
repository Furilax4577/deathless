using UnityEngine;

namespace Deathless.Jeu
{
    /// Effets ponctuels des boss (Golem, Morgrim, Nyxar ; 30/09/2026) : son, terre projetée, gerbes de gemmes. L'IA des
    /// boss ne tourne que chez l'hôte (Docs/reseau.md) ; ces effets, purement visuels et sonores, sont rejoués chez les
    /// clients par un seul message léger (EnnemiReseau.DiffuserEffetBoss : un octet d'effet, un point, une taille),
    /// au lieu d'un RPC par gemme. Couleurs par thème de palette (VfxPalette) : Terre et Rage pour Morgrim, Nyxessa
    /// (vert) pour Nyxar, dont les éclats et la magie sont l'énergie de la relique.
    public enum EffetBoss : byte
    {
        /// Coup lourd au sol (Golem, Morgrim) : son de coup et terre projetée.
        CoupSol = 0,
        /// Coup lourd sans terre (tourbillon, charge, fauche) : son seulement.
        CoupSon = 1,
        /// Cri de Morgrim : son de cri et gerbe de gemmes Rage.
        Cri = 2,
        /// Nyxar disparaît (téléportation) : implosion de gemmes vertes.
        TeleportDepart = 3,
        /// Nyxar réapparaît : explosion de gemmes vertes et terre.
        TeleportArrivee = 4,
        /// Un éclat de Nyx de Nyxar se brise.
        EclatBrise = 5,
        /// Nyxar enragé (phase 3) : grande gerbe verte et cri.
        Enrage = 6,
        /// Coup de faux de Nyxar : son de lame.
        Faux = 7,
        /// Onde de choc de Morgrim qui part (Fracas, Coup écrasé) : son dédié.
        Onde = 8,
        /// Coup de brèche sur le bouclier levé : fer sur verre.
        Breche = 9,
        /// Coup de zone du Golem d'origine (repli sans prefab Morgrim) : onde brève du prefab du Golem, terre et son.
        OndeGolem = 10,
        /// Coup sur un éclat de Nyx (critique garanti sur Nyxar, 01/10/2026) : effet et son de critique (Combat.Critique).
        Critique = 11,
    }

    public static class EffetsBoss
    {
        /// Hôte (ou solo) : joue l'effet ici et le fait rejouer aux autres postes.
        public static void Diffuser(Squelette source, EffetBoss e, Vector3 point, float taille = 1f)
        {
            Vector3 dir = source != null ? source.transform.forward : Vector3.forward;
            Jouer(e, point, dir, taille);
            if (source == null) return;
            var r = source.GetComponent<Deathless.Reseau.EnnemiReseau>();
            if (r != null && r.IsSpawned && r.IsServer) r.DiffuserEffetBoss((byte)e, point, dir, taille);
        }

        /// Joue l'effet sur ce poste seulement (hôte, solo, ou client qui le reçoit).
        public static void Jouer(EffetBoss e, Vector3 point, Vector3 direction, float taille)
        {
            switch (e)
            {
                case EffetBoss.OndeGolem:
                {
                    if (EffetsJeu.Terre != null) DirtBurst.Spawn(point, EffetsJeu.Terre, taille);
                    var fx = EffetsJeu.Instance;
                    direction.y = 0f;
                    if (fx != null && fx.prefabOndeGolem != null)
                    {
                        var onde = Object.Instantiate(fx.prefabOndeGolem, point + Vector3.up, Quaternion.LookRotation(direction.sqrMagnitude > 0.001f ? direction : Vector3.forward));
                        var o = onde.GetComponent<OndeDeChoc>();
                        if (o != null) o.Jouer();
                        Object.Destroy(onde, 3f);
                    }
                    AudioBank.Jouer(SonsDuJeu.GolemCoup, point, 1f);
                    break;
                }
                case EffetBoss.CoupSol:
                    if (EffetsJeu.Terre != null) DirtBurst.Spawn(point, EffetsJeu.Terre, taille);
                    AudioBank.Jouer(SonsDuJeu.GolemCoup, point, 1f);
                    break;
                case EffetBoss.CoupSon:
                    AudioBank.Jouer(SonsDuJeu.GolemCoup, point, 1f);
                    break;
                case EffetBoss.Cri:
                    AudioBank.Jouer(SonsDuJeu.MorgrimCri, point, 1f);
                    Gerbe(point + Vector3.up * 2.2f, VfxTheme.Rage, 2.2f * taille, 360f);
                    break;
                case EffetBoss.TeleportDepart:
                    if (EffetsJeu.Gemmes != null) GemBurst.Implode(point + Vector3.up * 1.1f, 1.6f * taille, EffetsJeu.Gemmes);
                    AudioBank.Jouer(SonsDuJeu.NyxarTeleport, point, 0.9f);
                    break;
                case EffetBoss.TeleportArrivee:
                    if (EffetsJeu.Gemmes != null) GemBurst.Explode(point + Vector3.up * 1.1f, 1.8f * taille, EffetsJeu.Gemmes);
                    if (EffetsJeu.Terre != null) DirtBurst.Spawn(point, EffetsJeu.Terre, 0.6f);
                    AudioBank.Jouer(SonsDuJeu.NyxarTeleport, point, 0.9f);
                    break;
                case EffetBoss.EclatBrise:
                    if (EffetsJeu.Gemmes != null) GemBurst.Explode(point, 1.4f * taille, EffetsJeu.Gemmes);
                    AudioBank.Jouer(SonsDuJeu.NyxarEclatBrise, point, 1f);
                    break;
                case EffetBoss.Enrage:
                    if (EffetsJeu.Gemmes != null) GemBurst.Explode(point + Vector3.up * 1.2f, 3f * taille, EffetsJeu.Gemmes);
                    Gerbe(point + Vector3.up * 0.2f, VfxTheme.Nyxessa, 3f * taille, 360f);
                    AudioBank.Jouer(SonsDuJeu.NyxarEnrage, point, 1f);
                    break;
                case EffetBoss.Faux:
                    AudioBank.Jouer(SonsDuJeu.NyxarFaux, point, 0.9f, 0.05f);
                    break;
                case EffetBoss.Onde:
                    if (EffetsJeu.Terre != null) DirtBurst.Spawn(point, EffetsJeu.Terre, taille);
                    AudioBank.Jouer(SonsDuJeu.MorgrimOnde, point, 1f);
                    break;
                case EffetBoss.Breche:
                    AudioBank.Jouer(SonsDuJeu.BouclierBreche, point, 1f);
                    break;
                case EffetBoss.Critique:
                    // `direction` : l'avant du boss, tourné vers le héros ; le coup va dans l'autre sens.
                    Combat.Critique(point, -direction, false);
                    break;
            }
        }

        /// Gerbe de gemmes d'un thème (langage commun MorgrimEffets), détruite après une seconde.
        public static void Gerbe(Vector3 point, VfxTheme theme, float rayon, float coneDeg, Vector3? direction = null)
        {
            var mat = EffetsJeu.GemmesMorgrim;
            if (mat == null) return;
            MorgrimEffets.MateriauGemmes = mat;
            var go = MorgrimEffets.EclatGemmes(point, theme, Mathf.Max(0.3f, rayon), direction, coneDeg, "EffetBoss");
            Object.Destroy(go, 1.2f);
        }
    }
}
