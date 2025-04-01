using UnityEngine;

public class CellEditor : MonoBehaviour
{
    private DatabaseTableModifier _tableModifier;
    private string _currentTableName;
    private string _currentColumnName;
    private string _currentPrimaryKey;
    private string _currentPrimaryKeyValue;
    
    private void Awake()
    {
        _tableModifier = GetComponent<DatabaseTableModifier>();
    }
    
    // Обработчик нажатия на кнопку "Сохранить"
    public void SaveData(string tableName, string columnName, string primaryKey, string primaryKeyValue, string currentValue)
    {
        _currentTableName = tableName;
        _currentColumnName = columnName;
        _currentPrimaryKey = primaryKey;
        _currentPrimaryKeyValue = primaryKeyValue;

        // Сохраняем изменения в базе данных
        _tableModifier.UpdateCellValue(_currentTableName, _currentColumnName, 
            _currentPrimaryKey, _currentPrimaryKeyValue, currentValue);
    }
}