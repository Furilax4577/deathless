using UnityEngine;

namespace Deathless.Jeu
{
    /// Porte d'un bâtiment de la carte v5 (03/10/2026, demande de Quentin : « rends les bâtiments entrables ») : posé sur le repère
    /// « Entree » de chaque bâtiment (Maisons/Batiment_<Rôle>/KayKit_<pièce>/Entree) par le builder (VillageBuilder.V5PoserMaisonKayKit).
    /// Quand le héros local est devant la porte (sur les marches, ou à 1,5 m au pied d'elles, de la largeur de l'escalier),
    /// l'invite « Entrer » apparaît, comme celle du portail ou de la taverne ; la touche Interagir (E, X, Carré) le conduit dans la
    /// pièce du bâtiment (PiecesBatiments : bruit de porte, fondu au noir, pièce basique, fondu retour). En sortant, il réapparaît
    /// au pied de la porte, dos au bâtiment, face à l'allée (PointRetour).
    ///
    /// Disponible jour et nuit (décision d'implémentation : les joueurs peuvent se réfugier dans les pièces la nuit, sans autre effet) ;
    /// les squelettes n'entrent pas (rien ne les y envoie) ; les villageois restent dehors, devant leur porte. Rien pour un héros
    /// distant (marionnette d'un autre poste) : chacun entre pour lui seul, la téléportation est celle du propriétaire.
    public class EntreeBatiment : PointInteraction
    {
        [Tooltip("Rôle du bâtiment (Taverne, Mecano, Maison, Forge, Sorcier, Druide) : choisit la pièce et sa teinte (PiecesBatiments.Roles).")]
        public string role = "Maison";
        [Tooltip("Pied de la porte, dans le repère de ce repère (x : axe de la porte ; z : vers l'extérieur) : au bas des marches, plus 0,9 m. Le héros y réapparaît.")]
        public Vector3 seuilLocal = new Vector3(0f, 0f, 3f);
        [Tooltip("Demi-largeur de la zone d'invite devant la porte (m) : la largeur de l'escalier et un peu plus.")]
        public float demiLargeur = 1.7f;
        [Tooltip("Zone d'invite : de la porte (z de ce repère - recul) à seuilLocal.z + avant (m).")]
        public float recul = 0.3f, avant = 1.5f;

        /// Point où le héros réapparaît en sortant (au sol, au pied des marches) et son lacet (dos au bâtiment, face à l'allée).
        public Vector3 PointRetour
        {
            get
            {
                Vector3 p = transform.TransformPoint(seuilLocal);
                if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
                return p;
            }
        }
        public float LacetRetour => transform.eulerAngles.y;
        /// Position de la porte (monde), pour les sons.
        public Vector3 PointPorte => transform.position + Vector3.up * 1.2f;

        /// Le héros est dans la zone devant la porte (repère de la porte : largeur, de la porte à 1,5 m au-delà du pied des marches).
        public bool Devant(Vector3 pieds, out float distance)
        {
            Vector3 l = transform.InverseTransformPoint(pieds);
            Vector3 s = transform.TransformPoint(seuilLocal);
            Vector2 d = new Vector2(pieds.x - s.x, pieds.z - s.z);
            distance = d.magnitude;
            if (Mathf.Abs(pieds.y - transform.position.y) > 3.5f) return false;
            return Mathf.Abs(l.x - seuilLocal.x) <= demiLargeur && l.z >= -recul && l.z <= seuilLocal.z + avant;
        }

        public override string Invite(Heros h, out float distance)
        {
            distance = float.MaxValue;
            if (!PiecesBatiments.PeutPasser(h)) return null;
            return Devant(h.transform.position, out distance) ? "Entrer" : null;
        }

        public override void Interagir(Heros h)
        {
            if (!PiecesBatiments.PeutPasser(h)) return;
            PiecesBatiments.Assurer().Entrer(h, this);
        }
    }
}
