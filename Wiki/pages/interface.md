# Interface

## Principes {décidé}

- L'interface se navigue entièrement à la manette comme au clavier et à la souris.
- Les icônes de boutons suivent le dernier appareil utilisé : Xbox, PlayStation, ou clavier et souris. Ce sont les icônes du pack Kenney Input Prompts.
- {dev} **Police** : Fredoka partout, titres en SemiBold ou Bold, texte en Regular ou Medium.
- {dev} **Technologie** : UI Toolkit.
- {dev} **Style** : celui des maquettes : fond ardoise sombre, panneaux bleu nuit, texte ivoire, et une bordure or pour l'élément sélectionné à la manette.
- **Taille de l'interface** : réglable ×1, ×2 ou ×3 dans les options, soit 80 %, 100 % (défaut) et 135 % de la taille des maquettes, pour lire confortablement sur une télévision.

## Menus {décidé}

- **Premier lancement** {décidé} : le jeu demande le **pseudo** souhaité. Il reste modifiable dans les options.
- **Menu principal** : Solo, Multijoueur, Options, Quitter. La classe se choisit juste avant de lancer la partie : le menu principal n'affiche plus la dernière classe jouée {décidé}.
- **Choix de classe** : les cinq classes, avec leur arme, leurs actions et le personnage en 3D. Les classes à venir, **Druide** et **Mécanicien**, y figurent **verrouillées**, avec une étiquette « Bientôt » {décidé}.
- **Lobby multijoueur** {décidé} : chaque joueur choisit son personnage puis se déclare **prêt** ; la partie se lance quand **tous** sont prêts. Le compte à rebours s'affiche sans rien décaler : ni les cases des joueurs, ni les boutons. Quatre joueurs au plus. **Chaque classe est unique** dans un salon : deux joueurs ne peuvent pas prendre la même (pas deux mages, par exemple) ; une classe déjà prise apparaît prise, avec le pseudo de son joueur {décidé}. **Rejoindre par un code** {décidé} : l'hôte crée un salon et reçoit un code court ; les amis le saisissent, sans adresse IP ni port à ouvrir {{dev: (Unity Relay et Lobby, services Unity Gaming Services)}}. Une connexion par **adresse IP directe** reste possible en secours.
- **Options** : jeu (pseudo, taille de l'interface, langue), commandes, affichage, audio. L'onglet audio règle quatre volumes : principal, musique, effets spéciaux et interface. Valeurs par défaut : principal 100 %, musique 10 %, effets spéciaux 15 %, interface 15 % {décidé}. Côté manette : sensibilité de la caméra, inversion de l'axe vertical, vibrations, aide à la visée, zone morte des sticks.
- **Pause** : la partie continue pendant la pause. Reprendre, Options, Quitter la partie, Quitter le jeu.
- **Écran de score** {décidé} : en fin de partie, classement des joueurs par catégorie, puis Rejouer (vote prêt) ou Arrêter. Voir [Déroulé d'une partie](deroule.md).

## HUD en jeu {décidé}

| Zone | Contenu |
|---|---|
| Haut, au centre | Vie de Nyxessa et de son bouclier, temps restant avant la nuit ou avant l'aube |
| Haut, au centre, à droite de la barre de Nyxessa | Missiles de Nyxessa : icône du crâne vert qui se remplit pendant la recharge du prochain missile, et le stock (par exemple « 3 / 5 ») |
| Haut, à droite | Or de l'équipe |
| Gauche | Vie des autres joueurs |
| Bas, à gauche | Portrait avec l’emblème de la classe, vie, endurance, et la jauge de la classe s’il y en a une (barres sans icône) |
| Bas, à gauche, au-dessus du portrait et des barres | Statuts du joueur (brûlure, ralenti…) : icône, jauge de durée, secondes restantes |
| Au-dessus des ennemis | Statuts de chaque ennemi affecté : petites icônes et jauge de durée discrète |
| Bas, au centre | Attaques et compétences avec leur bouton et leur temps de recharge, potions |
| Centre | Réticule de visée, et l'action possible devant soi (par exemple « Entrer dans le donjon » près du portail) |

## Statuts {décidé}

Liste des statuts, effets et durées : [Statuts](statuts.md).

- **HUD du joueur** : en bas à gauche, juste au-dessus du portrait et des barres, une case par statut actif (six au plus) : l'icône, une jauge de durée en bas de la case et les secondes restantes dans une pastille. Liseré rouge pour une affliction, or pour un bienfait (l'ivresse). Sans durée (dans l'eau du donjon), ni jauge ni secondes. La pastille des points de compétence est au-dessus de cette rangée. La rangée suit la taille de l'interface (×1, ×2, ×3).
- **Au-dessus des ennemis** : une rangée de petites icônes au-dessus de la tête de l'ennemi affecté (quatre au plus), chacune avec une fine jauge de durée. Elle n'apparaît que tant qu'il est affecté, s'il est à l'écran et à moins de 30 m, et s'estompe en approchant de cette limite.
- **Menu du personnage** (Tab, Y, Triangle) : section « Afflictions » sous les caractéristiques, un bouton par statut (icône, jauge, secondes), ou « Aucune affliction. ». Le **survol à la souris** ou le **focus à la manette** ouvre une infobulle : nom, effet, durée restante et source. À la manette : gauche et droite passent d'un statut à l'autre ; à droite du dernier, on arrive aux compétences ; à gauche d'une compétence, on revient aux statuts.

## Roue à emotes {décidé}

- **Ouvrir** : maintenir la croix directionnelle bas (manette) ou B (clavier) {à confirmer}. La roue s'affiche au centre de l'écran, par-dessus le HUD.
- **Choisir** : pointer une emote avec le stick droit ou la souris, depuis le centre. La caméra ne tourne pas tant que la roue est ouverte.
- **Lancer** : relâcher la touche lance l'emote pointée. Relâcher au centre annule.
- **Roue** : huit secteurs autour du centre, chacun avec son icône et son nom. Le secteur pointé passe en or, et son nom s'affiche au centre avec l'invite de la touche. La roue suit la taille de l'interface (×1, ×2, ×3).
- **Les huit emotes**, dans l'ordre de la roue en partant du haut : Salut, Acclamation, Provocation, S'asseoir, Se reposer, Pompes, Boire un coup, Faire le mort.
- **Quand** : seulement si le héros est vivant, au sol, sans action en cours, et hors d'un portail ou d'un menu. Pas de dégâts, pas de coût.
- **Fin** : un déplacement, une attaque, une compétence, l'esquive, le saut, un coup reçu ou la mort interrompent l'emote. Assis, couché ou en faisant le mort, se déplacer fait d'abord se relever, sauf sous un coup ou à la mort.
- **Arme** : elle reste en main pendant l'emote. Pour « Boire un coup », une chope de bière la remplace le temps du geste : pleine au début, puis vide une fois bue.
- **Multijoueur** : les autres joueurs voient l'emote et la chope.
