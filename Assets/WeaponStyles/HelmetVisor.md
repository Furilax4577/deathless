# Visière du casque du chevalier / paladin (pièce articulée)

Validé dans `Assets/Scenes/RigTest.unity` le 24/09/2026 sur `Knight_Ref` (`Knight.fbx`, pack Adventurers 2.0). Script : `Assets/Scripts/HelmetVisor.cs` (bool `open`, interpolation 0.2 s). Captures : `Visor_down_3q.png` (abaissée, combat), `Visor_up_3q.png` et `Visor_up_front.png` (relevée, répit).

## Ce qu'il y a dans le FBX

- La visière est un **SkinnedMeshRenderer séparé** : `Knight_HelmetVisor` (520 sommets, 1 sous-maillage), modélisée **relevée**. Aucune découpe de maillage n'est nécessaire.
- Elle est liée **à 100 % à l'os `head`** (tous les sommets, poids 1 sur l'os d'index 14 = `head` ; le tableau `bones` contient tout le rig mais seul `head` porte des poids).
- Étendue en repère modèle (bind) : X [−0.61, 0.61], Y [1.65, 2.54], Z [−0.09, 0.77] ; le casque `Knight_Helmet` va de Y 1.10 à 2.40, Z −0.54 à 0.60. Les sommets les plus en arrière de la visière (les rivets des tempes) sont centrés en (0, 1.764, −0.064) : c'est la charnière.

## Mécanisme (celui de `HeadGear.cs` dans Relic, repris tel quel)

1. Créer un enfant `VisorHinge` de l'os `head` (identité).
2. Dans `visor.bones`, remplacer `head` par `VisorHinge` : la visière suit alors la charnière, qui suit la tête (le skin reste celui du FBX : bind poses inchangées).
3. Fermer = faire pivoter la charnière autour de l'axe des tempes, en gardant le pivot fixe : `hinge.localRotation = AngleAxis(angle, axis)`, `hinge.localPosition = pivot − rotation × pivot`.

| Paramètre | Valeur | Repère |
|---|---|---|
| Pivot (charnière) | **(0, 0.566, 0.068)** | os `head` (`head` est à (0, 1.223, 0.008) dans le modèle ; = (0, 1.79, 0.07) en repère modèle, valeur de Relic vérifiée sur le maillage) |
| Axe | **(1, 0, 0)** | os `head` (axe des tempes = droite du modèle) |
| Relevée (répit) | **0°** | pose modélisée |
| Abaissée (combat) | **41°** (angle positif = le bord avant descend sur le visage) | la visière vient se poser sous le bord du casque et cache tout le visage, sans décalage supplémentaire |
| Durée | 0.2 s, SmoothStep | |

`HelmetVisor.Build()` fait la substitution (réexécutable, retrouve un `VisorHinge` existant), `SetAmount(0..1)` pose l'état sans Play mode, `Update` interpole vers `open`.

## À reporter dans Deathless

Rien de nouveau : `HeadGear.cs` fait déjà exactement cela (`visorHinge` (0, 1.79, 0.07), `visorClosedAngle` 41, `visorClosedOffset` 0). Le bac à sable confirme les valeurs sur le maillage et fournit une version autonome (`HelmetVisor`) sans dépendance à `RunProgress`/`PlayerHotbar`, réutilisable pour le paladin s'il partage le casque du Knight. Si un autre casque a une visière fusionnée dans le maillage du casque, il faudra soit la séparer dans Blender, soit isoler ses triangles par plage d'indices et les re-skinner sur la charnière (non nécessaire pour Knight.fbx).
