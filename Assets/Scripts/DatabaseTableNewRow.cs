using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Data;
using TMPro;
using Mono.Data.Sqlite;

public class DatabaseTableNewRow : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Transform tableContentParent1;
    [SerializeField] private Transform tableContentParent2;
    [SerializeField] private GameObject tableRowPrefab;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Button addNewRowButton;
    [SerializeField] private Button saveRowButton;
    
    private DatabaseLoader _databaseLoader;
    private DatabaseManager _databaseManager;
    private DatabaseTableViewer _tableViewer;
    
    private string _currentDBPath;
    private string _activeTableName;
    private List<TMP_InputField> _newRowInputFields = new List<TMP_InputField>();
    private List<TableColumn> _activeTableColumns = new List<TableColumn>();
    private string _primaryKeyColumn;
    private GameObject _newRowObject;
    
    private void Start()
    {
        _databaseLoader = GetComponent<DatabaseLoader>();
        _databaseManager = GetComponent<DatabaseManager>();
        _tableViewer = GetComponent<DatabaseTableViewer>();
        
        if (addNewRowButton != null)
        {
            addNewRowButton.onClick.AddListener(CreateEmptyRow);
        }
        
        if (saveRowButton != null)
        {
            saveRowButton.onClick.AddListener(SaveNewRow);
        }
        
        _databaseManager.onDataLoaded.AddListener(OnDatabaseLoaded);
    }
    
    private void OnDatabaseLoaded()
    {
        _currentDBPath = _databaseManager.databasePath;
    }
    
    // Called when active table changes (should be hooked up to the same event in DatabaseManager)
    public void OnActiveTableChanged(string tableName)
    {
        _activeTableName = tableName;
        
        Debug.Log(_currentDBPath);
        Debug.Log(_activeTableName);
        
        _activeTableColumns = _databaseLoader.GetTableStructure(_currentDBPath, _activeTableName);
        IdentifyPrimaryKey();
        
        // Remove any existing empty row
        if (_newRowObject != null)
        {
            Destroy(_newRowObject);
            _newRowObject = null;
            _newRowInputFields.Clear();
        }
    }
    
    // Called when user clicks "Add New Row" button
    public void CreateEmptyRow()
    {
        if (string.IsNullOrEmpty(_activeTableName) || string.IsNullOrEmpty(_currentDBPath))
        {
            Debug.LogError("No active table selected or database path is empty");
            return;
        }
        
        // Remove any existing empty row
        if (_newRowObject != null)
        {
            Destroy(_newRowObject);
            _newRowInputFields.Clear();
        }
        
        // Determine which parent to use
        Transform contentParent = GetActiveTableContentParent();
        if (contentParent == null) return;
        
        // Create a new row
        _newRowObject = Instantiate(tableRowPrefab, contentParent);
        _newRowObject.transform.SetAsLastSibling(); // Make sure it's at the bottom
        
        // Setup row layout
        SetupRowLayout(_newRowObject);
        
        // Create input fields for each column
        foreach (var column in _activeTableColumns)
        {
            // Create a cell
            var cell = Instantiate(cellPrefab, _newRowObject.transform);
            
            // Setup cell layout
            var layoutElement = cell.GetComponent<LayoutElement>() ?? cell.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1;
            layoutElement.minWidth = 80;
            
            // Get the input field
            var inputField = cell.GetComponentInChildren<TMP_InputField>();
            if (inputField == null) continue;
            
            // If this is a primary key with autoincrement, disable the field
            bool isPrimaryKey = column.Name == _primaryKeyColumn;
            if (isPrimaryKey)
            {
                inputField.interactable = false;
                inputField.placeholder.GetComponent<TextMeshProUGUI>().text = "AUTO";
            }
            else
            {
                // Set placeholder based on column type
                SetPlaceholderForType(inputField, column.Type);
            }
            
            _newRowInputFields.Add(inputField);
        }
    }
    
    private void SetPlaceholderForType(TMP_InputField inputField, string columnType)
    {
        var placeholder = inputField.placeholder.GetComponent<TextMeshProUGUI>();
        
        switch (columnType.ToLower())
        {
            case "integer":
                inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                placeholder.text = "Enter number";
                break;
                
            case "real":
            case "float":
            case "double":
                inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                placeholder.text = "Enter decimal";
                break;
                
            case "date":
            case "datetime":
                placeholder.text = "YYYY-MM-DD";
                break;
                
            default:
                placeholder.text = "Enter value";
                break;
        }
    }
    
    private void SetupRowLayout(GameObject row)
    {
        // Remove existing children if there are any
        foreach (Transform child in row.transform)
        {
            Destroy(child.gameObject);
        }
        
        // Add/Configure horizontal layout
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 5, 5);
        }
        
        // Add content size fitter
        var sizeFitter = row.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = row.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
    
    private Transform GetActiveTableContentParent()
    {
        // Get table names from TableViewer through reflection or other means
        // For now, we'll assume table1 is displayed in tableContentParent1, etc.
        
        // This part depends on how your DatabaseTableViewer identifies which table is active
        // You'll need to adapt this to match your existing code structure
        
        // Method 1: Check active table name against known table names
        if (_tableViewer != null)
        {
            // Use reflection to get the private field values (since they're private in your code)
            var table1Name = GetPrivateField<string>(_tableViewer, "_table1Name");
            var table2Name = GetPrivateField<string>(_tableViewer, "_table2Name");
            
            if (_activeTableName == table1Name) return tableContentParent1;
            if (_activeTableName == table2Name) return tableContentParent2;
        }
        
        Debug.LogError($"Could not determine parent for active table: {_activeTableName}");
        return null;
    }
    
    private void IdentifyPrimaryKey()
    {
        try
        {
            var query = $"PRAGMA table_info({_activeTableName})";
            var tableInfo = _databaseLoader.ExecuteQuery(_currentDBPath, query);
            
            _primaryKeyColumn = null;
            
            // Look for a column with primary key
            foreach (DataRow row in tableInfo.Rows)
            {
                if (Convert.ToInt32(row["pk"]) > 0)
                {
                    _primaryKeyColumn = row["name"].ToString();
                    Debug.Log($"Primary key for table {_activeTableName}: {_primaryKeyColumn}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error identifying primary key: {ex.Message}");
            _primaryKeyColumn = null;
        }
    }
    
    private void SaveNewRow()
    {
        if (_newRowObject == null || _newRowInputFields.Count == 0)
        {
            Debug.LogError("No new row to save");
            return;
        }
        
        try
        {
            // Build SQL query
            var columns = new List<string>();
            var paramNames = new List<string>();
            var values = new List<string>();
            
            for (int i = 0; i < _activeTableColumns.Count; i++)
            {
                // Skip primary key if it's set to AUTO
                if (_activeTableColumns[i].Name == _primaryKeyColumn && 
                    (_newRowInputFields[i].text == "AUTO" || string.IsNullOrEmpty(_newRowInputFields[i].text)))
                {
                    continue;
                }
                
                // Add non-empty fields to the query
                if (!string.IsNullOrEmpty(_newRowInputFields[i].text))
                {
                    columns.Add(_activeTableColumns[i].Name);
                    paramNames.Add($"@param{i}");
                    values.Add(_newRowInputFields[i].text);
                }
            }
            
            if (columns.Count == 0)
            {
                Debug.LogWarning("No data to insert");
                return;
            }
            
            // Create the SQL query
            var columnsStr = string.Join(", ", columns);
            var paramsStr = string.Join(", ", paramNames);
            var query = $"INSERT INTO {_activeTableName} ({columnsStr}) VALUES ({paramsStr})";
            
            // Execute the query
            using (var connection = new SqliteConnection($"URI=file:{_currentDBPath}"))
            {
                connection.Open();
                
                using (var command = new SqliteCommand(query, connection))
                {
                    // Add parameters
                    for (int i = 0; i < paramNames.Count; i++)
                    {
                        command.Parameters.AddWithValue(paramNames[i], values[i]);
                    }
                    
                    int rowsAffected = command.ExecuteNonQuery();
                    
                    if (rowsAffected > 0)
                    {
                        Debug.Log("New row added successfully");
                        
                        // Clear the input fields or destroy the row
                        Destroy(_newRowObject);
                        _newRowObject = null;
                        _newRowInputFields.Clear();
                        
                        // Refresh table view
                        _tableViewer.LoadTableData(_currentDBPath, _activeTableName);
                    }
                    else
                    {
                        Debug.LogWarning("Failed to add new row");
                    }
                }
                
                connection.Close();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving new row: {ex.Message}");
        }
    }
    
    // Helper method to get private field value using reflection
    private T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | 
                                                       System.Reflection.BindingFlags.NonPublic);
        if (field != null)
        {
            return (T)field.GetValue(obj);
        }
        return default(T);
    }
}