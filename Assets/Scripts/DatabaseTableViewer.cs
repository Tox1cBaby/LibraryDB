using UnityEngine;
using UnityEngine.UI;
using System;
using System.Data;
using TMPro;

public class DatabaseTableViewer : MonoBehaviour
{
    [SerializeField] private Transform tableContentParent1;
    [SerializeField] private Transform tableContentParent2;
    [SerializeField] private GameObject tableRowPrefab;
    [SerializeField] private GameObject cellPrefab; // Префаб для ячеек данных
    [SerializeField] private GameObject cellTypePrefab; // Префаб для ячеек заголовков
    [SerializeField] private TextMeshProUGUI tableName1;
    [SerializeField] private TextMeshProUGUI tableName2;
    [SerializeField] private Image tableHighlight1;
    [SerializeField] private Image tableHighlight2;

    private DatabaseLoader _databaseLoader;
    private CellEditor _cellEditor;
    
    private DataTable _table1Data; // Данные первой таблицы
    private DataTable _table2Data; // Данные второй таблицы
    private string _currentDBPath; // Текущий путь к БД
    private string _table1Name; // Имя первой таблицы
    private string _table2Name; // Имя второй таблицы
    private string _activeTableName; // Имя активной таблицы

    private string _table1PrimaryKey; // Имя столбца с первичным ключом первой таблицы
    private string _table2PrimaryKey; // Имя столбца с первичным ключом второй таблицы

    private void Start()
    {
        _databaseLoader = GetComponent<DatabaseLoader>();
        _cellEditor = GetComponent<CellEditor>();
    }

    // Загрузка статических таблиц (вызывается один раз при загрузке БД)
    public void LoadStaticTables(string dbPath, string table1Name, string table2Name)
    {
        _currentDBPath = dbPath;
        _table1Name = table1Name;
        _table2Name = table2Name;
        _activeTableName = table1Name; // По умолчанию активна первая таблица

        // Определяем первичные ключи таблиц
        _table1PrimaryKey = IdentifyPrimaryKey(dbPath, table1Name);
        _table2PrimaryKey = IdentifyPrimaryKey(dbPath, table2Name);

        // Загружаем данные таблиц
        LoadTableData();

        // Устанавливаем имена таблиц в заголовки
        if (tableName1 != null) tableName1.text = table1Name;
        if (tableName2 != null) tableName2.text = table2Name;

        // Устанавливаем подсветку активной таблицы
        UpdateTableHighlight();
    }

    // Обновить подсветку активной таблицы
    private void UpdateTableHighlight()
    {
        if (tableHighlight1 != null)
        {
            Color color = tableHighlight1.color;
            color.a = _activeTableName == _table1Name ? 0.3f : 0.1f;
            tableHighlight1.color = color;
        }

        if (tableHighlight2 != null)
        {
            Color color = tableHighlight2.color;
            color.a = _activeTableName == _table2Name ? 0.3f : 0.1f;
            tableHighlight2.color = color;
        }
    }

    // Установка активной таблицы (вызывается из DatabaseManager)
    public void SetActiveTable(string tableName)
    {
        _activeTableName = tableName;
        UpdateTableHighlight();
        
        GetComponent<DatabaseTableNewRow>()?.OnActiveTableChanged(tableName);
    }

    // Перезагрузка данных таблиц
    public void ReloadActiveTable()
    {
        LoadTableData();
    }

    // Загрузка данных для указанной таблицы (используется DatabaseTableModifier)
    public void LoadTableData(string dbPath, string tableName)
    {
        // Проверяем, какую таблицу нужно обновить
        if (tableName == _table1Name)
        {
            ClearTableContent(tableContentParent1);
            var query = $"SELECT * FROM {_table1Name} LIMIT 100";
            _table1Data = _databaseLoader.ExecuteQuery(dbPath, query);
            DisplayTableData(_table1Data, tableContentParent1, _table1Name, _table1PrimaryKey);
        }
        else if (tableName == _table2Name)
        {
            ClearTableContent(tableContentParent2);
            var query = $"SELECT * FROM {_table2Name} LIMIT 100";
            _table2Data = _databaseLoader.ExecuteQuery(dbPath, query);
            DisplayTableData(_table2Data, tableContentParent2, _table2Name, _table2PrimaryKey);
        }
    }

    // Загрузка данных из выбранных таблиц
    private void LoadTableData()
    {
        if (string.IsNullOrEmpty(_table1Name) || string.IsNullOrEmpty(_table2Name))
        {
            Debug.LogError("Имена таблиц не указаны!");
            return;
        }

        // Очищаем предыдущие строки
        ClearTableContent();

        try
        {
            // Выполняем запросы и получаем данные
            var query1 = $"SELECT * FROM {_table1Name} LIMIT 100"; // Ограничение на 100 строк
            var query2 = $"SELECT * FROM {_table2Name} LIMIT 100"; // Ограничение на 100 строк

            _table1Data = _databaseLoader.ExecuteQuery(_currentDBPath, query1);
            _table2Data = _databaseLoader.ExecuteQuery(_currentDBPath, query2);

            // Отображаем данные
            DisplayTableData(_table1Data, tableContentParent1, _table1Name, _table1PrimaryKey);
            DisplayTableData(_table2Data, tableContentParent2, _table2Name, _table2PrimaryKey);

            Debug.Log($"Загружено {_table1Data.Rows.Count} строк из таблицы {_table1Name}");
            Debug.Log($"Загружено {_table2Data.Rows.Count} строк из таблицы {_table2Name}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при загрузке данных из таблиц: {ex.Message}");
        }
    }

    // Определение первичного ключа таблицы
    private string IdentifyPrimaryKey(string dbPath, string tableName)
    {
        try
        {
            string query = $"PRAGMA table_info({tableName})";
            DataTable tableInfo = _databaseLoader.ExecuteQuery(dbPath, query);

            string primaryKeyColumn = null;

            // Ищем столбец с primary key
            foreach (DataRow row in tableInfo.Rows)
            {
                if (Convert.ToInt32(row["pk"]) > 0)
                {
                    primaryKeyColumn = row["name"].ToString();
                    Debug.Log($"Первичный ключ для таблицы {tableName}: {primaryKeyColumn}");
                    break;
                }
            }

            // Если не нашли PK, используем первый столбец как идентификатор
            if (string.IsNullOrEmpty(primaryKeyColumn) && tableInfo.Rows.Count > 0)
            {
                primaryKeyColumn = tableInfo.Rows[0]["name"].ToString();
                Debug.Log($"Первичный ключ не найден, используем столбец {primaryKeyColumn} как идентификатор");
            }

            return primaryKeyColumn;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при определении первичного ключа: {ex.Message}");
            return null;
        }
    }

    // Отображение результатов поиска
    public void DisplaySearchResults(DataTable data)
    {
        // Очищаем содержимое только активной таблицы
        if (_activeTableName == _table1Name)
        {
            ClearTableContent(tableContentParent1);
            DisplayTableData(data, tableContentParent1, _table1Name, _table1PrimaryKey);
        }
        else
        {
            ClearTableContent(tableContentParent2);
            DisplayTableData(data, tableContentParent2, _table2Name, _table2PrimaryKey);
        }
    }

    // Очистка содержимого всех таблиц
    private void ClearTableContent()
    {
        ClearTableContent(tableContentParent1);
        ClearTableContent(tableContentParent2);
    }

    // Очистка содержимого одной таблицы
    private void ClearTableContent(Transform parent)
    {
        if (parent == null) return;

        // Удаляем все дочерние объекты из родительского элемента
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }

    // Отображение данных с динамическим созданием столбцов
    private void DisplayTableData(DataTable data, Transform contentParent, string tableName, string primaryKeyColumn)
    {
        if (contentParent == null) return;

        if (data == null || data.Rows.Count == 0)
        {
            Debug.Log($"Нет данных для отображения в таблице {tableName}");

            // Создаем сообщение "Нет данных" в таблице
            var emptyRow = Instantiate(tableRowPrefab, contentParent);
            var cell = Instantiate(cellPrefab, emptyRow.transform);
            var cellText = cell.GetComponent<TMP_InputField>();
            if (cellText != null)
            {
                cellText.text = "Нет данных, соответствующих запросу";
                cellText.interactable = false;
            }

            return;
        }

        // Создаем заголовок таблицы
        var headerRow = CreateHeaderRow(data);
        headerRow.transform.SetParent(contentParent, false);

        // Создаем строки с данными
        foreach (DataRow row in data.Rows)
        {
            var rowObject = CreateDataRow(row, data.Columns, tableName, primaryKeyColumn);
            rowObject.transform.SetParent(contentParent, false);
        }
    }

    // Метод для создания строки заголовка
    private GameObject CreateHeaderRow(DataTable data)
    {
        var columnCount = data.Columns.Count;
        var row = Instantiate(tableRowPrefab);

        // Настраиваем компоненты Layout для строки
        SetupRowLayoutComponents(row);

        // Создаем ячейки заголовков
        for (var i = 0; i < columnCount; i++)
        {
            var columnName = data.Columns[i].ColumnName;

            // Используем cellTypePrefab для заголовков
            var headerCell = Instantiate(cellTypePrefab, row.transform);
            var headerText = headerCell.GetComponentInChildren<TextMeshProUGUI>();
            if (headerText != null)
            {
                headerText.text = columnName;
            }

            // Настраиваем размер ячейки
            SetupCellLayoutElement(headerCell);
        }

        return row;
    }

    // Метод для создания строки данных
    private GameObject CreateDataRow(DataRow dataRow, DataColumnCollection columns, string tableName,
        string primaryKeyColumn)
    {
        var row = Instantiate(tableRowPrefab);

        // Настраиваем компоненты Layout для строки
        SetupRowLayoutComponents(row);

        // Получаем значение первичного ключа для этой строки
        var primaryKeyValue = string.IsNullOrEmpty(primaryKeyColumn) ? "0" : dataRow[primaryKeyColumn].ToString();

        // Определяем, активна ли таблица
        bool isActiveTable = (tableName == _activeTableName);

        // Создаем ячейки данных
        for (var i = 0; i < columns.Count; i++)
        {
            var columnName = columns[i].ColumnName;
            var cellValue = dataRow[i].ToString();

            // Используем cellPrefab для ячеек данных
            var cell = Instantiate(cellPrefab, row.transform);

            // Настраиваем размер ячейки
            SetupCellLayoutElement(cell);

            // Находим компонент текста
            var cellInput = cell.GetComponentInChildren<TMP_InputField>();
            if (cellInput != null)
            {
                cellInput.text = cellValue;

                // Проверяем, является ли это первичным ключом
                bool isPrimaryKey = (columnName == primaryKeyColumn);

                // Делаем поле доступным для редактирования только если таблица активна и это не первичный ключ
                cellInput.interactable = isActiveTable && !isPrimaryKey;

                // Добавляем обработчик нажатия для редактирования, если ячейка не является первичным ключом
                // и таблица активна
                if (isActiveTable && !isPrimaryKey)
                {
                    cellInput.onEndEdit.AddListener(value =>
                    {
                        _cellEditor.SaveData(tableName, columnName, primaryKeyColumn, primaryKeyValue, value);
                    });
                }
            }
        }

        return row;
    }

    // Настройка компонентов Layout для строки
    private void SetupRowLayoutComponents(GameObject row)
    {
        // Уничтожаем все существующие ячейки (если они есть в префабе)
        foreach (Transform child in row.transform)
        {
            Destroy(child.gameObject);
        }

        // Добавляем Layout компонент, если его еще нет
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
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

        // Добавляем Content Size Fitter для автоматического изменения размера
        var sizeFitter = row.GetComponent<ContentSizeFitter>();

        if (sizeFitter == null)
        {
            sizeFitter = row.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    // Настройка компонентов Layout для ячейки
    private void SetupCellLayoutElement(GameObject cell)
    {
        var layoutElement = cell.GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement = cell.AddComponent<LayoutElement>();
        }

        // Все ячейки имеют одинаковую ширину (если таблица широкая, можно использовать скролл)
        layoutElement.flexibleWidth = 1;
        layoutElement.minWidth = 80;
    }
    
    // Метод для добавления новой строки (пустой) в активную таблицу
    public void AddNewRow()
    {
        if (string.IsNullOrEmpty(_activeTableName) || string.IsNullOrEmpty(_currentDBPath))
        {
            Debug.LogError("Активная таблица не выбрана или не указан путь к БД!");
            return;
        }
        
        // Получаем структуру активной таблицы для создания пустой строки
        var tableStructure = _databaseLoader.GetTableStructure(_currentDBPath, _activeTableName);
        
        // Создаем пустую строку в активной таблице UI
        var contentParent = _activeTableName == _table1Name ? tableContentParent1 : tableContentParent2;
        
        // Создаем новый GameObject для строки
        var newRow = Instantiate(tableRowPrefab, contentParent);
        
        // Настраиваем компоненты Layout для строки
        SetupRowLayoutComponents(newRow);
        
        // Создаем ячейки для каждого столбца
        foreach (var column in tableStructure)
        {
            // Используем cellPrefab для ячеек данных
            var cell = Instantiate(cellPrefab, newRow.transform);
            
            // Настраиваем размер ячейки
            SetupCellLayoutElement(cell);
            
            // Находим компонент текста
            var cellInput = cell.GetComponentInChildren<TMP_InputField>();
            if (cellInput != null)
            {
                // Если это первичный ключ, то делаем поле неактивным
                bool isPrimaryKey = (column.Name == (_activeTableName == _table1Name ? _table1PrimaryKey : _table2PrimaryKey));
                
                cellInput.text = isPrimaryKey ? "AUTO" : "";
                cellInput.interactable = !isPrimaryKey;
                
                // Добавляем обработчик для сохранения изменений
                if (!isPrimaryKey)
                {
                    cellInput.onEndEdit.AddListener(value =>
                    {
                        // Здесь можно добавить логику для сохранения изменений в новой строке
                        // Примечание: это требует дополнительной реализации для вставки новой строки в БД
                    });
                }
            }
        }
        
        // Добавить кнопку сохранения для новой строки можно здесь
        // Это потребует дополнительной реализации
    }
}