using UnityEngine;
using UnityEngine.UI;

public class ManageWindow : MonoBehaviour
{
    [SerializeField] private Button deleteButton;

    private string _currentTableName;
    private string _currentPrimaryKeyColumn;
    private string _currentPrimaryKeyValue;
    private DatabaseTableModifier _databaseTableModifier;

    private void Start()
    {
        _databaseTableModifier = GetComponent<DatabaseTableModifier>();

        deleteButton.onClick.AddListener(OnDeleteButtonClick);
    }

    public void SelectRow(string tableName, string primaryKeyColumn, string primaryKeyValue)
    {
        _currentTableName = tableName;
        _currentPrimaryKeyColumn = primaryKeyColumn;
        _currentPrimaryKeyValue = primaryKeyValue;

        deleteButton.interactable = true;
    }

    private void OnDeleteButtonClick()
    {
        if (string.IsNullOrEmpty(_currentTableName) ||
            string.IsNullOrEmpty(_currentPrimaryKeyColumn) ||
            string.IsNullOrEmpty(_currentPrimaryKeyValue)) return;
        
        _databaseTableModifier.DeleteRow(_currentTableName, _currentPrimaryKeyColumn, _currentPrimaryKeyValue);
    }
}