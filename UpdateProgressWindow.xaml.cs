using System;
using System.Windows;

namespace Divalto
{
    /// <summary>
    /// Fenêtre modale pour afficher la progression du téléchargement de mise à jour
    /// </summary>
    public partial class UpdateProgressWindow : Window
    {
        public UpdateProgressWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Met à jour la progression du téléchargement
        /// </summary>
        public void UpdateProgress(long bytesDownloaded, long totalBytes)
        {
            var percentage = totalBytes > 0 ? (bytesDownloaded * 100.0) / totalBytes : 0;
            DownloadProgress.Value = percentage;

            var mbDownloaded = bytesDownloaded / (1024.0 * 1024.0);
            var mbTotal = totalBytes / (1024.0 * 1024.0);

            ProgressText.Text = $"{percentage:F1}% - {mbDownloaded:F1} MB / {mbTotal:F1} MB";
        }

        /// <summary>
        /// Affiche un message de statut
        /// </summary>
        public void SetStatus(string status)
        {
            StatusText.Text = status;
        }
    }
}
