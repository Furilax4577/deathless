using System.Collections;
using UnityEngine;

namespace Deathless.Jeu.Dev
{
    /// Vérification en Play des barres de vie des ennemis, de la méga barre du boss et des chiffres de dégâts
    /// flottants (interface.md § « Barres de vie des ennemis »), sur le modèle de ScenariosParade. Lancé par
    /// execute_code : `ScenariosVie.Lancer("ennemis")`, `"boss"`, `"degats"`. Résultats dans la console (préfixe
    /// [Vie]) ; captures facultatives dans Assets/Screenshots/&lt;nom&gt;.png.
    public class ScenariosVie : MonoBehaviour
    {
        static ScenariosVie s_I;
        public static string Dernier = "";

        public static void Lancer(string essai, string capture = null)
        {
            if (s_I == null) s_I = new GameObject("ScenariosVie").AddComponent<ScenariosVie>();
            s_I.StopAllCoroutines();
            switch (essai)
            {
                case "ennemis": s_I.StartCoroutine(s_I.EssaiEnnemis(capture)); break;
                case "boss": s_I.StartCoroutine(s_I.EssaiBoss(capture)); break;
                case "degats": s_I.StartCoroutine(s_I.EssaiDegats(capture)); break;
                default: Log("essai inconnu : " + essai); break;
            }
        }

        static void Log(string t) { Dernier = t; Debug.Log("[Vie] " + t); }
        static Heros H => Partie.Instance != null ? Partie.Instance.HerosLocal : null;

        static void Frapper(Squelette cible, float montant, bool critique = false, bool continu = false)
        {
            var h = H;
            if (h == null || cible == null || cible.Sante == null) return;
            cible.Sante.Encaisser(new InfoDegats
            {
                montant = montant, sourceId = h.Id, equipeSource = Equipe.Heros, critique = critique, continu = continu,
                point = cible.CentreTete, source = h.gameObject,
            });
        }

        /// Ennemi commun blessé (barre qui apparaît) puis soigné (barre qui s'efface après 2 s) ; élite toujours visible.
        IEnumerator EssaiEnnemis(string capture)
        {
            var h = H;
            if (h == null) { Log("pas de héros local"); yield break; }
            DevPartie.PlacerHeros(new Vector3(0f, 0f, -11f), new Vector3(0f, 0f, -20f));
            yield return null;
            var commun = DevPartie.PoserDevant(TypeEnnemi.Sbire, 3f, -1.2f);
            var elite = DevPartie.PoserDevant(TypeEnnemi.Guerrier, 3.4f, 1.4f, elite: true);
            yield return new WaitForSeconds(1.8f);   // sortie de terre
            if (commun != null) Frapper(commun, commun.Sante.pvMax * 0.55f);
            yield return new WaitForSeconds(0.3f);
            Log("commun : vie=" + (commun != null ? commun.Sante.Ratio.ToString("0.00") : "?") + " | élite : vie=" + (elite != null ? elite.Sante.Ratio.ToString("0.00") : "?"));
            if (capture != null) DevPartie.Capturer(capture);
            yield return new WaitForSeconds(2.5f);   // au-delà des 2 s d'effacement
            Log("commun blessé : barre attendue masquée après 2 s (à confirmer sur la capture suivante si besoin)");
            if (commun != null) commun.Sante.Soigner(commun.Sante.pvMax);
            yield return new WaitForSeconds(0.5f);
            Finir(commun, elite);
        }

        /// Entrée en scène du boss (déploiement, capture à mi-course) puis combat (statut sur la barre, capture).
        IEnumerator EssaiBoss(string capture)
        {
            var h = H;
            if (h == null) { Log("pas de héros local"); yield break; }
            DevPartie.PlacerHeros(new Vector3(0f, 0f, -11f), new Vector3(0f, 0f, -20f));
            yield return null;
            var morgrim = DevPartie.PoserDevant(TypeEnnemi.Golem, 6f);
            if (morgrim == null) { Log("Morgrim non posé"); yield break; }
            yield return new WaitForSeconds(0.45f);   // mi-déploiement de la méga barre
            Log("entrée en scène (mi-déploiement)");
            if (capture != null) DevPartie.Capturer(capture);
            yield return new WaitForSeconds(1.6f);    // pleinement déployé (et sorti de terre)
            morgrim.Etourdir(6f);
            morgrim.Statuts?.Ajouter(TypeStatut.Ralenti, 6f, 0.4f, OrigineStatut.Joueur, h.Id);
            Frapper(morgrim, morgrim.Sante.pvMax * 0.35f);
            yield return new WaitForSeconds(0.4f);
            Log("combat : vie=" + morgrim.Sante.Ratio.ToString("0.00") + " statuts=" + (morgrim.Statuts != null ? morgrim.Statuts.Liste.Count : 0));
            if (capture != null) DevPartie.Capturer(capture.Replace("entree", "combat_statut"));
            yield return new WaitForSeconds(0.5f);
            Finir(morgrim);
        }

        /// Chiffres de dégâts : ordinaire, critique, brûlure cumulée, reçu, un mot (Esquivé).
        IEnumerator EssaiDegats(string capture)
        {
            var h = H;
            if (h == null) { Log("pas de héros local"); yield break; }
            DevPartie.PlacerHeros(new Vector3(0f, 0f, -11f), new Vector3(0f, 0f, -20f));
            yield return null;
            var cible1 = DevPartie.PoserDevant(TypeEnnemi.Sbire, 3f, -1.6f);
            var cible2 = DevPartie.PoserDevant(TypeEnnemi.Sbire, 3.4f, 1.6f);
            yield return new WaitForSeconds(1.8f);
            if (cible1 != null) Frapper(cible1, 27f);                 // ordinaire (blanc)
            if (cible2 != null) Frapper(cible2, 146f, critique: true); // critique (or)
            yield return new WaitForSeconds(0.15f);
            if (cible1 != null) Frapper(cible1, 6f, continu: true);    // brûlure, 1er tic
            yield return new WaitForSeconds(0.15f);
            if (cible1 != null) Frapper(cible1, 6f, continu: true);    // brûlure, cumulée (12 au total)
            DevPartie.Blesser(24f);                                    // reçu (rouge)
            // Esquivé (mot) : fenêtre d'invulnérabilité forcée (même effet qu'une esquive réussie, Heros.Invulnerable
            // est public et déjà utilisé par EsquiveImposee) puis un coup parable pendant cette fenêtre.
            h.Invulnerable(1f);
            h.Sante.Encaisser(new InfoDegats { montant = 15f, equipeSource = Equipe.Ennemis, parable = true, point = h.transform.position + Vector3.up });
            yield return new WaitForSeconds(0.25f);
            Log("dégâts posés : ordinaire, critique, brûlure cumulée, reçu, esquivé");
            if (capture != null) DevPartie.Capturer(capture);
            yield return new WaitForSeconds(0.8f);
            Finir(cible1, cible2);
        }

        static void Finir(params Squelette[] squelettes)
        {
            foreach (var s in squelettes) if (s != null) s.Desintegrer(true);
        }
    }
}
