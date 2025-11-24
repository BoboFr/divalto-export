using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Divalto.Models
{
    /// <summary>
    /// Représente une colonne sélectionnable avec un statut de sélection
    /// </summary>
    public class SelectableColumn : INotifyPropertyChanged
    {
        private string _columnName = "";
        private bool _isSelected = true;

        public string ColumnName
        {
            get => _columnName;
            set => SetProperty(ref _columnName, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public SelectableColumn(string columnName, bool isSelected = true)
        {
            _columnName = columnName;
            _isSelected = isSelected;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Lève l'événement PropertyChanged
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Définit une propriété et lève PropertyChanged si la valeur change
        /// </summary>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
