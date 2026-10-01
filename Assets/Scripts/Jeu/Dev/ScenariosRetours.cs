using System.Collections;
using UnityEngine;

namespace Deathless.Jeu.Dev
{
    /// Vérification en Play des retours de Quentin du 01/10/2026 (sur le modèle de ScenariosVie), lancée par
    /// execute_code : `ScenariosRetours.Lancer("boire", "retours0110_boire_paladin", 180f)` (emote « Boire un coup »
    /// vue par la caméra de jeu tournée de `angle` degrés autour du héros : chope en main, à la bouche, vide),
    /// `Lancer("vote", "retours0110_vote")` (pastille du vote : pas prêt, prêt) et `Lancer("vague", "retours0110_vague")`
    /// (repère des vagues : crépuscule, vague 1, nouvelle vague 2). Résultats dans la console (préfixe [Retours]) ;
    /// captures dans Assets/Screenshots/&lt;nom&gt;_*.png.
    public class ScenariosRetours : MonoBehaviour
    {
        // Succès (01/10/2026) : outil de dev utilisé dans ce Play, plus rien ne compte jusqu'à la fin du Play.
        static ScenariosRetours() { Deathless.Succes.ServiceSucces.SuspendreDev("ScenariosRetours"); }

        static ScenariosRetours s_I;
        public static string Dernier = "";

        public static void Lancer(string essai, string capture = null, float angle = 180f)
        {
            if (s_I == null) s_I = new GameObject("ScenariosRetours").AddComponent<ScenariosRetours>();
            s_I.StopAllCoroutines();
            switch (essai)
            {
                case "boire": s_I.StartCoroutine(s_I.EssaiBoire(capture, angle)); break;
                case "vote": s_I.StartCoroutine(s_I.EssaiVote(capture)); break;
                case "vague": s_I.StartCoroutine(s_I.EssaiVague(capture)); break;
                default: Log("essai inconnu : " + essai); break;
            }
        }

        static void Log(string t) { Dernier = t; Debug.Log("[Retours] " + t); }
        static Partie P => Partie.Instance;
        static Heros H => P != null ? P.HerosLocal : null;

        /// Caméra de jeu tournée de `angle` degrés autour du héros (180 : de face), même distance qu'en jeu.
        static void TournerCamera(Heros h, float angle)
        {
            if (P == null || P.cameraJeu == null || h == null) return;
            P.cameraJeu.lacet = h.transform.eulerAngles.y + angle;
        }

        /// Emote « Boire un coup » : captures de la chope en main (début du geste), à la bouche (milieu de la boisson)
        /// et vide (après instantVide). Le temps normalisé de l'état « Boire » de l'Animator rythme les captures.
        IEnumerator EssaiBoire(string capture, float angle)
        {
            var h = H;
            if (h == null) { Log("pas de héros local"); yield break; }
            DevPartie.PlacerHeros(new Vector3(0f, 0f, -11f), new Vector3(0f, 0f, -20f));
            yield return null;
            // La souris du bureau fait dériver le lacet de la caméra (l'Input System la lit sans focus) : la caméra et
            // l'orientation du héros sont réimposées à chaque image, après Update et avant la caméra (LateUpdate).
            var face = h.transform.rotation;
            float t0 = Time.time;
            while (Time.time - t0 < 0.6f) { h.transform.rotation = face; TournerCamera(h, angle); yield return null; }
            var cat = CatalogueEmotes.Courant;
            float tMain = 0.12f, tBouche = cat != null ? (cat.instantBouche + cat.instantVide) * 0.5f : 0.55f, tVide = cat != null ? Mathf.Min(0.97f, cat.instantVide + 0.1f) : 0.8f;
            if (!h.Emotes.Lancer(EmotesHeros.Boire)) { Log("emote refusée (héros occupé ?)"); yield break; }
            int hBoire = Animator.StringToHash(EmotesHeros.EtatBoire);
            bool vuMain = false, vuBouche = false, vuVide = false;
            float depuis = 0f;
            while (depuis < 5f && !(vuMain && vuBouche && vuVide))
            {
                depuis += Time.deltaTime;
                h.transform.rotation = face;
                TournerCamera(h, angle);
                var anim = h.animator;
                var cur = anim.GetCurrentAnimatorStateInfo(0);
                float t = -1f;
                if (cur.shortNameHash == hBoire && !anim.IsInTransition(0)) t = cur.normalizedTime;
                else if (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).shortNameHash == hBoire) t = anim.GetNextAnimatorStateInfo(0).normalizedTime;
                if (t >= 0f)
                {
                    if (!vuMain && t >= tMain) { vuMain = true; Capturer(capture, "main", t, h); }
                    else if (!vuBouche && t >= tBouche) { vuBouche = true; Capturer(capture, "bouche", t, h); }
                    else if (!vuVide && t >= tVide) { vuVide = true; Capturer(capture, "vide", t, h); }
                }
                yield return null;
            }
            Log("boire : main=" + vuMain + " bouche=" + vuBouche + " vide=" + vuVide + " (" + (h.Classe != null ? h.Classe.Id : "?") + ", angle " + angle + ")");
        }

        static void Capturer(string capture, string etape, float t, Heros h)
        {
            Log(etape + " : t=" + t.ToString("0.00") + " chope=" + h.Emotes.EtatChope);
            if (capture != null) DevPartie.Capturer(capture + "_" + etape);
        }

        /// Pastille du vote : pas prêt, puis prêt (badge sur le portrait, « Annuler »), puis vote annulé.
        IEnumerator EssaiVote(string capture)
        {
            var p = P;
            if (p == null || H == null) { Log("pas de partie"); yield break; }
            if (p.Etat.phase != Phase.Jour) { Log("pas le jour : " + p.Etat.phase); yield break; }
            var j = p.JoueurLocal;
            if (j.pret) p.BasculerPret(j.id);
            yield return new WaitForSeconds(0.3f);
            Log("pas prêt : pret=" + j.pret + " prêts=" + p.JoueursPrets);
            if (capture != null) DevPartie.Capturer(capture + "_pasPret");
            yield return new WaitForSeconds(0.3f);
            p.BasculerPret(j.id);
            yield return new WaitForSeconds(0.35f);
            Log("prêt : pret=" + j.pret + " prêts=" + p.JoueursPrets + " compte=" + p.Etat.comptePret + " reste=" + p.Etat.TempsRestant.ToString("0.0"));
            if (capture != null) DevPartie.Capturer(capture + "_pret");
            yield return new WaitForSeconds(0.3f);
            p.BasculerPret(j.id);   // annulé : le jour reprend
            yield return new WaitForSeconds(0.2f);
            Log("annulé : pret=" + j.pret + " compte=" + p.Etat.comptePret + " reste=" + p.Etat.TempsRestant.ToString("0.0"));
        }

        /// Repère des vagues : crépuscule de la nuit 3 (« 3 vagues »), nuit (« Vague 1 / 3 »), puis saut juste avant la
        /// vague 2 (« Vague 2 / 3 », liseré d'éclat).
        IEnumerator EssaiVague(string capture)
        {
            var p = P;
            if (p == null || H == null) { Log("pas de partie"); yield break; }
            var h = H;
            bool invulnerable = h.Sante.invulnerable;
            h.Sante.invulnerable = true;   // le temps des captures : le voile « Vous êtes tombé » les gâcherait
            p.ForcerPhase(Phase.Crepuscule, 3);
            yield return new WaitForSeconds(1.2f);
            Log("crépuscule : total=" + p.Etat.vagues.total + " vague=" + p.Etat.vagues.vague);
            if (capture != null) DevPartie.Capturer(capture + "_crepuscule");
            float attente = 0f;
            while (p.Etat.phase != Phase.Nuit && attente < 10f) { attente += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(4.5f);   // bannière NUIT 3 passée en partie (3,5 s)
            Log("nuit : vague=" + p.Etat.vagues.vague + " / " + p.Etat.vagues.total + " t=" + p.Etat.tempsPhase.ToString("0.0"));
            if (capture != null) DevPartie.Capturer(capture + "_vague1");
            var departs = GameBalance.Courant.departsTroisVagues;
            float d2 = departs != null && departs.Length > 1 ? departs[1] : 40f;
            p.Etat.tempsPhase = d2 / Mathf.Max(0.01f, GameBalance.Courant.vitesseCycle) - 0.4f;
            yield return new WaitForSeconds(1.0f);
            Log("vague 2 : vague=" + p.Etat.vagues.vague + " / " + p.Etat.vagues.total + " t=" + p.Etat.tempsPhase.ToString("0.0"));
            if (capture != null) DevPartie.Capturer(capture + "_vague2");
            yield return new WaitForSeconds(0.5f);
            h.Sante.invulnerable = invulnerable;
        }
    }
}
