using System;
using System.Threading.Tasks;
using Divalto.Interfaces;

namespace Divalto.Services
{
    /// <summary>
    /// Exemple d'utilisation de la classe MssqlConnection
    /// </summary>
    public class MssqlConnectionExample
    {
        public static async Task DemoBasicConnection()
        {
            // Créer une instance avec authentification Windows (par défaut)
            var connection = new MssqlConnection(
                serverName: "srv-divalto-bdd.incb.local",
                databaseName: "DIVATURBOSELF"
            );

            // S'abonner à l'événement de changement d'état
            connection.ConnectionStateChanged += (sender, args) =>
            {
                Console.WriteLine($"État de connexion changé:");
                Console.WriteLine($"  - Connecté : {args.IsConnected}");
                Console.WriteLine($"  - Serveur : {args.ServerName}");
                Console.WriteLine($"  - BD : {args.DatabaseName}");
                Console.WriteLine($"  - Message : {args.StatusMessage}");
            };

            // Se connecter de manière asynchrone
            bool success = await connection.ConnectAsync();

            if (success)
            {
                Console.WriteLine("Connexion réussie !");
                Console.WriteLine($"État : {connection.IsConnected}");
                Console.WriteLine($"Message : {connection.StatusMessage}");

                // Exécuter une requête simple
                var result = await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES");
                Console.WriteLine($"Nombre de tables : {result}");

                // Se déconnecter
                await connection.DisconnectAsync();
            }
            else
            {
                Console.WriteLine($"Erreur : {connection.StatusMessage}");
            }
        }

        public static async Task DemoWithCredentials()
        {
            // Créer une instance avec authentification par mot de passe
            var connection = new MssqlConnection(
                serverName: "srv-divalto-bdd.incb.local",
                databaseName: "DIVATURBOSELF",
                userId: "utilisateur",
                password: "motdepasse"
            );

            if (await connection.ConnectAsync())
            {
                // Exécuter une requête SELECT
                var reader = await connection.ExecuteQueryAsync("SELECT TOP 10 * FROM vos_tables");

                if (reader != null)
                {
                    while (await reader.ReadAsync())
                    {
                        // Accéder aux colonnes
                        // var colonne1 = reader["NomColonne"];
                    }
                    await reader.DisposeAsync();
                }

                await connection.DisconnectAsync();
            }
        }

        public static async Task DemoUsingStatement()
        {
            // Utiliser 'using' pour s'assurer que la ressource est libérée
            using (var connection = new MssqlConnection(
                serverName: "srv-divalto-bdd.incb.local",
                databaseName: "DIVATURBOSELF"
            ))
            {
                if (await connection.ConnectAsync())
                {
                    // Votre code ici
                    Console.WriteLine(connection.StatusMessage);
                }
            } // La déconnexion et libération se font automatiquement
        }

        public static void DemoSynchronous()
        {
            // Version synchrone pour les contextes qui ne supportent pas async/await
            var connection = new MssqlConnection(
                serverName: "srv-divalto-bdd.incb.local",
                databaseName: "DIVATURBOSELF"
            );

            if (connection.Connect())
            {
                Console.WriteLine("Connecté avec succès");
                connection.Disconnect();
            }
        }
    }
}
