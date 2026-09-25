using UnityEngine;

namespace Deathless.Donjon
{
    /// Repère posé par le générateur : point d'arrivée, portail de retour, emplacement de butin ou point d'apparition.
    /// Transform vide (le visuel éventuel est à part, dans `visuel`), visible dans l'éditeur par un gizmo.
    /// L'index est stable pour une graine donnée : le serveur et les clients désignent le même butin par son index.
    public class DonjonRepere : MonoBehaviour
    {
        public enum Genre : byte { Arrivee, PortailRetour, Butin, Apparition }

        public Genre genre;
        public int index;
        public int niveau;
        public TypeButin butin;
        public TypeApparition apparition;
        [Tooltip("Objet visible associé (coffre, tas d'or, anneau du portail), s'il y en a un.")]
        public GameObject visuel;

        void OnDrawGizmos()
        {
            Vector3 p = transform.position;
            switch (genre)
            {
                case Genre.Arrivee: Gizmos.color = Color.white; break;
                case Genre.PortailRetour: Gizmos.color = new Color(1f, 0.75f, 0.3f); break;
                case Genre.Butin: Gizmos.color = new Color(1f, 0.85f, 0.1f); break;
                default:
                    Gizmos.color = apparition == TypeApparition.Guerrier ? new Color(1f, 0.3f, 0.2f)
                        : apparition == TypeApparition.Mage ? new Color(0.65f, 0.4f, 1f)
                        : apparition == TypeApparition.Voleur ? new Color(0.3f, 0.6f, 1f) : new Color(0.9f, 0.9f, 0.85f);
                    break;
            }
            Gizmos.DrawWireSphere(p + Vector3.up, 0.45f);
            Gizmos.DrawLine(p, p + Vector3.up * 2f);
            Gizmos.DrawLine(p + Vector3.up, p + Vector3.up + transform.forward * 1.2f);
        }
    }
}
