using UnityEngine;

namespace Deathless.Donjon
{
    /// Pièces KayKit utilisées par le générateur de donjon. Grille de 4 m : dalles 4 x 4, murs 4 x 4 x 1 (centrés
    /// sur le bord de la cellule), garde-corps 4 m, piliers 1,5 m, escalier long 8 m pour 4 m de haut.
    /// Aucune pièce verte (le vert est réservé à Nyxessa) : pas de bannières vertes, pas de murs moussus.
    [CreateAssetMenu(menuName = "Deathless/Donjon/Kit", fileName = "DonjonKit")]
    public class DonjonKit : ScriptableObject
    {
        [Header("Sols (4 x 4 m)")]
        public GameObject[] solsRez;
        public GameObject[] solsPierre;
        public GameObject[] solsBois;
        [Tooltip("Dalle posée sous les planchers des étages, vue d'en bas (ceiling_tile : pivot au plan du plancher, pend de 0,25 m).")]
        public GameObject plafond;
        [Tooltip("Grille d'égout posée en décor voulu (une par bloc de hall au plus, jamais sous un point d'apparition ni à l'arrivée) ; plus jamais tirée au hasard.")]
        public GameObject solGrille;

        [Header("Murs, garde-corps, piliers")]
        [Tooltip("Murs pleins (wall, wall_pillar, wall_scaffold) : le mur par défaut, tiré au hasard dans cette liste. Jamais de porte ni d'arche ici (27/09/2026) : une porte ou une arche n'a de sens que sur un passage, et les passages du donjon sont les arcades.")]
        public GameObject[] murs;
        [Tooltip("Fenêtres FERMÉES (volets clos, rien ne se voit derrière) des murs d'enceinte des étages : au plus une par pan de 3 cellules, jamais sous une torche.")]
        public GameObject[] mursHauts;
        [Tooltip("Mur cassé : rare (un pan sur 15), jamais deux côte à côte, doublé d'un mur plein derrière sur l'enceinte pour ne pas montrer le vide.")]
        public GameObject murCasse;
        [Tooltip("Muret plein entre deux halls du rez : bloc de fondation (2 x 2 m) mis à 4 m de large, 1 m de haut et 1 m d'épaisseur. Vide : bloc de pierre facetté généré.")]
        public GameObject muret;
        public GameObject gardeCorps;
        public GameObject pilier;
        public GameObject poteau;
        public GameObject escalier;
        [Tooltip("Escalier court (4 m) réduit à 2 m de haut : descente au bassin.")]
        public GameObject escalierBassin;
        [Tooltip("Arcade sous le bord d'un plancher : poteaux et linteau de bois (passage ouvert).")]
        public GameObject arcade;

        [Header("Bassin")]
        [Tooltip("Bloc de fondation de 2 m : muret du bassin (mis à 4 m de large).")]
        public GameObject fondationBassin;
        public Material eau;
        public GameObject tonneauFlottant;

        [Header("Butin (or seulement)")]
        public GameObject grandCoffre;
        public GameObject coffre;
        public GameObject[] tasOr;
        [Tooltip("Modèle du grand coffre sans serrure (Assets/Art/Coffres/…) : posé à la place de grandCoffre dès qu'il est renseigné.")]
        public GameObject grandCoffreSansSerrure;
        [Tooltip("Modèle du coffre sans serrure (Assets/Art/Coffres/…) : posé à la place de coffre dès qu'il est renseigné.")]
        public GameObject coffreSansSerrure;
        [Tooltip("Fin du nom de l'enfant qui sert de couvercle (il bascule à l'ouverture) : « _lid » pour les coffres KayKit.")]
        public string suffixeCouvercle = "_lid";

        /// Modèles posés par le générateur : les coffres sans serrure quand ils sont renseignés, sinon ceux de KayKit.
        public GameObject ModeleGrandCoffre => grandCoffreSansSerrure != null ? grandCoffreSansSerrure : grandCoffre;
        public GameObject ModeleCoffre => coffreSansSerrure != null ? coffreSansSerrure : coffre;

        [Header("Décor (jamais rien qui ressemble à du butin : seul le vrai butin a l'allure du butin)")]
        public GameObject[] decorsCoin;
        [Tooltip("Longue table posée contre un mur du rez.")]
        public GameObject tableLongue;
        public GameObject[] bannieres;
        [Tooltip("Ossements posés sur les points d'apparition (les squelettes sortent de terre là).")]
        public GameObject[] os;
        public GameObject dalleArrivee;

        [Header("Torches")]
        public GameObject torcheMurale;
        public GameObject torcheSurPied;
        public GameObject colonneTorchere;
        public Color couleurTorche = new Color(1f, 0.56f, 0.24f);
        public float intensiteTorche = 5f;
        public float porteeTorche = 11f;

        [Header("Portail de retour (repère, non alimenté par Nyxessa)")]
        public GameObject socle;
        [Tooltip("Portail posé en jeu sur le repère du portail de retour : le même que celui du village " +
                 "(Assets/VFX/PortailDonjon/PortailDonjon.prefab, gemmes vertes), toujours ouvert. Vide : anneau de bronze.")]
        public GameObject portail;
        [Tooltip("Anneau de bronze : ancien repère, gardé hors jeu (génération dans l'éditeur) ou sans portail.")]
        public Material anneau;
        public Color couleurPortail = new Color(1f, 0.78f, 0.45f);
        public float intensitePortail = 4f;
    }
}
