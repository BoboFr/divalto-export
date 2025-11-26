# Guide de mise à jour automatique

## Comment ça fonctionne

L'application Divalto utilise **Velopack** pour gérer les mises à jour automatiques. Velopack est un système de mise à jour moderne et sécurisé pour les applications Windows.

### Architecture

1. **Client** : L'application Divalto vérifie automatiquement s'il y a une mise à jour au démarrage
2. **GitHub Releases** : Les packages Velopack sont publiés comme releases sur GitHub
3. **Service** : `UpdateService.cs` gère la détection et l'installation des mises à jour

## Publication d'une nouvelle version

### Option 1 : Utiliser un tag git (automatique)

1. Incrementer la version dans `AssemblyInfo.cs` (ex: `1.0.1.0`)
2. Créer un commit avec le changement
3. Créer un tag git :
   ```bash
   git tag -a v1.0.1 -m "Release version 1.0.1"
   git push origin v1.0.1
   ```
4. Le workflow `release-velopack.yml` est automatiquement déclenché
5. Le package Velopack est généré et publié sur GitHub Releases

### Option 2 : Déclencher manuellement

1. Aller sur `Actions` → `Release Velopack`
2. Cliquer sur `Run workflow`
3. Entrer la version (ex: `1.0.1`)
4. Attendre que le workflow se termine

## Versioning

- Utilisez la [Semantic Versioning](https://semver.org/) : `MAJOR.MINOR.PATCH`
- Mettez à jour le fichier `AssemblyInfo.cs` avec la nouvelle version
- Chaque release doit avoir des notes de version descriptives

## Structure des releases

Chaque release Velopack contient :

- **Update.exe** : L'installateur de mise à jour
- **Divalto-xxx.nupkg** : Le package complet
- **Divalto-xxx-delta.nupkg** : Le delta pour les mises à jour légères

## Notes de version

Les notes de version sont générées automatiquement à partir des commits depuis la dernière version. Vous pouvez les modifier manuellement sur la page de release GitHub.

## Dépannage

### L'application ne détecte pas les mises à jour

- Vérifier que les releases sont publiées sur GitHub (et non en brouillon)
- Vérifier la version actuelle : regarder les logs de démarrage
- Vérifier que `release-velopack.yml` s'est exécuté sans erreur

### Velopack CLI non trouvé

Si vous compilez localement sans le workflow GitHub :

```bash
dotnet tool install -g Velopack.Cli
vpk pack -u divalto-export -v 1.0.1 -e ./publish/Divalto.exe -o ./releases
```

### Les utilisateurs ne reçoivent pas les mises à jour

- Les utilisateurs doivent avoir la même version d'architecture (`win-x64`)
- Attendre quelques minutes après la publication de la release pour que l'application la détecte
- Redémarrer l'application pour forcer la vérification

## Limites actuelles

- Les mises à jour nécessitent un redémarrage de l'application
- Seule l'architecture `win-x64` est supportée
- Les mises à jour ne sont pas silencieuses (demande confirmation à l'utilisateur)

## Améliorations futures

- [ ] Mises à jour silencieuses en arrière-plan
- [ ] Support de plusieurs architectures (win-x86, ARM64)
- [ ] Notifications plus discrètes
- [ ] Possibilité de planifier les mises à jour
