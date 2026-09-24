using UnityEngine;

// Style d'arme validé dans le bac à sable : ce qu'il faut reporter dans Deathless pour qu'un personnage compatible
// (squelette Rig_Medium KayKit avec les sockets handslot.r / handslot.l) porte n'importe quelle arme de ce style.
// Un asset par style dans Assets/WeaponStyles/. Les valeurs sont celles mesurées ici (scène RigTest), pas des valeurs de jeu.
[CreateAssetMenu(menuName = "Rig/Style d'arme", fileName = "WeaponStyle")]
public class WeaponStyle : ScriptableObject
{
    [System.Serializable]
    public class Attachment
    {
        [Tooltip("Modèle KayKit (FBX) de la pièce.")]
        public GameObject prefab;
        [Tooltip("Nom de l'os cible dans le squelette (handslot.r, handslot.l, chest, hips...).")]
        public string boneName = "handslot.r";
        [Tooltip("Position, rotation (Euler) et échelle locales dans l'os.")]
        public Vector3 localPosition;
        public Vector3 localEuler;
        public float localScale = 1f;

        [Header("Pose alternative (visée, arme rangée...) : vide si la pièce n'a qu'une pose")]
        public bool hasAlternate;
        [Tooltip("Quand cette pose s'applique (ex. 'visée : Aiming = vrai', 'arbalète en main : Crossbow = vrai').")]
        public string alternateLabel;
        public string alternateBone;
        public Vector3 alternatePosition;
        public Vector3 alternateEuler;
        [Tooltip("Durée de l'interpolation entre les deux poses (s).")]
        public float alternateBlendSeconds = 0.15f;
    }

    [Header("Identité")]
    public string styleName = "Style";
    [TextArea] public string notes;

    [Header("Pièces attachées")]
    public Attachment[] attachments;

    [Tooltip("Sockets à créer si le rig ne les a pas (Mannequin_Medium du pack Animations) : mêmes valeurs que sur les rigs Adventurers.")]
    public Attachment[] requiredSockets;

    [Header("Animations (clips KayKit, Rig_Medium)")]
    public AnimationClip idle;
    public AnimationClip walk;
    public AnimationClip run;
    public AnimationClip[] attacks;
    public AnimationClip guard;
    public AnimationClip guardHit;
    [Tooltip("Etats particuliers du style (visée, arme rangée...) : clip et nom d'état du contrôleur.")]
    public AnimationClip[] extraClips;
    [Tooltip("Contrôleur minimal construit ici.")]
    public RuntimeAnimatorController controller;
    [Tooltip("Script de bascule à poser sur le personnage (BowStance, AltWeaponSwitch...), vide si aucune bascule.")]
    public string stanceScript;
}
