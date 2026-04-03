## Qu'est-ce que c'est?

Il s'agit d'un bot Discord **P**higros **S**core **L**ookup (alias PSLDiscordBot) qui peut récupérer et afficher vos scores sur Phigros en utilisant `/get-photo`, `/get-scores` ou `/export-scores`, etc.<br/>

Remarque : Le bot nécessite une sauvegarde dans le cloud (sur TapTap ; les comptes internationaux et chinois sont compatibles !).

## Comment l'utiliser?

Si vous utilisez ce bot pour la première fois, veuillez suivre les instructions ci-dessous pour configurer votre compte.

1. Utilisez `/login` pour lier votre compte. Vous verrez un paramètre nommé `is_international`, dont la valeur dépend de la plateforme sur laquelle vous avez téléchargé votre jeu.
2. Par exemple, sur Google Play, l'App Store et TapTap (Chine), vous utiliserez un compte non international (indiquez `false`). Sur TapTap Global, vous utiliserez un compte international (indiquez `true`). Vous pouvez également utiliser `/link-token` si vous possédez déjà un jeton d'authentification et que vous ne pouvez pas ou ne souhaitez pas vous connecter. Vous devrez tout de même renseigner le paramètre `is_international`.
2. (Facultatif) Ajustez la précision de votre score avec `/set-precision` pour un affichage plus précis.
3. Vous pouvez maintenant consulter vos scores avec `/get-scores` ou `/get-photo`.

Si vous rencontrez un problème, utilisez `/report-problem` pour le signaler (les développeurs pourront vous contacter pour plus d'informations).

## Utilisation des commandes

Veuillez consulter la [documentation sur GitHub](https://github.com/yt6983138/PSLDiscordBot/blob/master/Documentation/usage.md). Nous vous recommandons de lire attentivement la documentation avant utilisation. (Impossible de tout faire tenir dans un message de 2000 caractères maximum :sob:)<br/>

Conditions d'utilisation : https://github.com/yt6983138/PSLDiscordBot/blob/master/TermsOfService.md<br/>
Politique de confidentialité : https://github.com/yt6983138/PSLDiscordBot/blob/master/PrivacyPolicy.md