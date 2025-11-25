# Divalto

Application WPF pour la connexion à des bases de données MSSQL et l'export de données vers Excel.

## Prérequis

- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0) ou supérieur

## Build

### Build en Debug

```bash
dotnet build
```

### Build en Release

```bash
dotnet build -c Release
```

Les fichiers compilés se trouvent dans `bin\Release\net7.0-windows\`.

## Publish

### Publication standard (exécutable unique, self-contained)

```bash
dotnet publish -c Release
```

Cette commande génère un **exécutable unique** (`Divalto.exe`) d'environ 9.5 MB dans :

```
bin\Release\net7.0-windows\win-x64\publish\
```

**Caractéristiques de la publication :**

- **Self-contained** : Inclut le runtime .NET (pas besoin d'installer .NET sur la machine cible)
- **Single file** : Un seul fichier `.exe` à distribuer
- **Platform** : Windows x64 uniquement

### Publication avec dossier de sortie personnalisé

```bash
dotnet publish -c Release -o ./dist
```

### Restaurer les dépendances (si nécessaire)

```bash
dotnet restore
```

## Dépendances

| Package               | Version | Description          |
| --------------------- | ------- | -------------------- |
| System.Data.SqlClient | 4.8.6   | Connexion SQL Server |
| ClosedXML             | 0.102.1 | Export Excel         |

## Structure du projet

```
Divalto/
├── Services/       # Logique métier (connexion DB, export)
├── ViewModels/     # ViewModels MVVM
├── Models/         # Modèles de données
├── Commands/       # Commandes WPF (RelayCommand)
├── Converters/     # Convertisseurs XAML
├── Helpers/        # Utilitaires
├── Assets/         # Assets du projet
└── Interfaces/     # Interfaces
```
