using System;

namespace Divalto.Models
{
    /// <summary>
    /// Représente les informations d'une table (label pour affichage et nom pour requête)
    /// </summary>
    public class TableInfo : IEquatable<TableInfo>
    {
        public string Label { get; set; } = "";
        public string TableName { get; set; } = "";

        public TableInfo() { }

        public TableInfo(string label, string tableName)
        {
            Label = label;
            TableName = tableName;
        }

        public override string ToString()
        {
            return Label;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as TableInfo);
        }

        public bool Equals(TableInfo? other)
        {
            if (other == null)
                return false;
            return TableName == other.TableName;
        }

        public override int GetHashCode()
        {
            return TableName.GetHashCode();
        }
    }
}
