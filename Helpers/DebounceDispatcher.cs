using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Divalto.Helpers
{
    /// <summary>
    /// Utilitaire pour différer l'exécution d'une action (debouncing)
    /// Permet d'éviter les appels répétitifs en attendant une pause dans les événements
    /// </summary>
    public class DebounceDispatcher
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly Dispatcher _dispatcher;

        public DebounceDispatcher(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Différer l'exécution d'une action. Si appelée plusieurs fois, seule la dernière action sera exécutée
        /// </summary>
        /// <param name="action">Action à exécuter</param>
        /// <param name="delay">Délai en millisecondes avant l'exécution</param>
        public void Debounce(Action action, int delay)
        {
            // Annuler toute action en attente
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // Créer une nouvelle tâche différée
            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay, token);

                    // Si non annulée, exécuter l'action sur le thread UI
                    if (!token.IsCancellationRequested)
                    {
                        _dispatcher.Invoke(action);
                    }
                }
                catch (TaskCanceledException)
                {
                    // Normal - l'action a été annulée par un nouvel appel
                }
            });
        }

        /// <summary>
        /// Annule toute action en attente
        /// </summary>
        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }
    }
}
