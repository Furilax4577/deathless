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
- **Lobby multijoueur** {décidé} : chaque joueur choisit son personnage puis se déclare **prêt** ; la partie se lance quand **tous** sont prêts. Quatre joueurs au plus. **Chaque classe est unique** dans un salon : deux joueurs ne peuvent pas prendre la même (pas deux mages, par exemple) ; une classe déjà prise apparaît prise, avec le pseudo de son joueur {décidé}. **Rejoindre par un code** {décidé} : l'hôte crée un salon et reçoit un code court ; les amis le saisissent, sans adresse IP ni port à ouvrir {{dev: (Unity Relay et Lobby, services Unity Gaming Services)}}. Une connexion par **adresse IP directe** reste possible en secours.
- **Options** : jeu (pseudo, taille de l'interface, langue), commandes, affichage, audio. L'onglet audio règle quatre volumes : principal, musique, effets spéciaux et interface. Côté manette : sensibilité de la caméra, inversion de l'axe vertical, vibrations, aide à la visée, zone morte des sticks.
- **Pause** : la partie continue pendant la pause. Reprendre, Options, Quitter la partie, Quitter le jeu.
- **Écran de score** {décidé} : en fin de partie, classement des joueurs par catégorie, puis Rejouer (vote prêt) ou Arrêter. Voir [Déroulé d'une partie](deroule.md).

## HUD en jeu {décidé}

| Zone | Contenu |
|---|---|
| Haut, au centre | Vie de Nyxessa et de son bouclier, temps restant avant la nuit ou avant l'aube |
| Haut, à droite | Or de l'équipe |
| Gauche | Vie des autres joueurs |
| Bas, à gauche | Portrait, vie, endurance, et la jauge de la classe s'il y en a une |
| Bas, au centre | Attaques et compétences avec leur bouton et leur temps de recharge, potions |
| Centre | Réticule de visée, et l'action possible devant soi (par exemple « Entrer dans le portail ») |
