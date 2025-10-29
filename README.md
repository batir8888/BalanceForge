# BalanceForge

**BalanceForge** — мощный Unity-плагин для управления табличными игровыми данными (balance tables). Предоставляет интуитивный редактор с поддержкой продвинутых функций: undo/redo, фильтрация, сортировка, валидация данных и импорт/экспорт в CSV.

---

## Возможности

### Редактирование данных
- **Табличный редактор** с поддержкой больших объемов данных
- **Виртуальная прокрутка** для оптимальной производительности
- **Inline редактирование** всех типов данных
- **Множественное выделение** строк
- **Горячие клавиши**: Ctrl+C/V/Z/Y, Delete

### Управление данными
- **Undo/Redo** для всех операций
- **Фильтрация** с множественными условиями (AND/OR)
- **Сортировка** по любой колонке
- **Валидация** данных с настраиваемыми правилами
- **Буфер обмена** (Copy/Paste)

### Типы данных
- Примитивные: `String`, `Integer`, `Float`, `Boolean`
- Unity типы: `Vector2`, `Vector3`, `Color`
- Специальные: `Enum`, `AssetReference`

### Импорт/Экспорт
- **CSV экспорт** с сохранением структуры
- **CSV импорт** с автоопределением типов
- Проверка совместимости структуры при импорте

---

## Быстрый старт

### Создание первой таблицы

#### Способ 1: Через мастер
```
Assets → Create → BalanceForge → Balance Table Wizard
```
1. Введите имя таблицы
2. Настройте колонки (имя, тип, обязательность)
3. Нажмите "Create Table"

#### Способ 2: Через меню
```
Assets → Create → BalanceForge → Balance Table
```

### Открытие редактора
```
Window → BalanceForge → Table Editor
```
Или дважды кликните на `.asset` файл таблицы.

---

## Модули системы

## 1. Core.Data - Ядро данных

**Назначение**: Фундаментальные классы для представления табличных данных.

### Ключевые классы

#### BalanceTable (ScriptableObject)
Главный контейнер данных таблицы.

**Основные возможности:**
```csharp
// Создание таблицы
var table = ScriptableObject.CreateInstance<BalanceTable>();
table.TableName = "Characters";

// Добавление колонки
var column = new ColumnDefinition("name", "Character Name", ColumnType.String, required: true);
table.AddColumn(column);

// Добавление строки
var row = table.AddRow();
row.SetValue("name", "Hero");

// Валидация
ValidationResult result = table.ValidateData();
if (!result.IsValid) {
    foreach (var error in result.Errors) {
        Debug.LogError(error.Message);
    }
}
```

**Свойства:**
| Свойство | Тип | Описание |
|----------|-----|----------|
| `TableName` | `string` | Имя таблицы |
| `TableId` | `string` | Уникальный идентификатор (GUID) |
| `Columns` | `List<ColumnDefinition>` | Список колонок |
| `Rows` | `List<BalanceRow>` | Список строк |
| `LastModified` | `DateTime` | Время последнего изменения |

**Методы:**
- `AddColumn(ColumnDefinition)` - Добавить колонку
- `RemoveColumn(string)` - Удалить колонку по ID
- `AddRow()` - Добавить строку с дефолтными значениями
- `RemoveRow(string)` - Удалить строку по ID
- `GetRow(int)` - Получить строку по индексу
- `GetColumn(string)` - Получить колонку по ID
- `ValidateData()` - Валидировать все данные
- `HasStructure(List<string>)` - Проверить структуру

---

#### BalanceRow
Представление одной строки данных.

**Использование:**
```csharp
var row = new BalanceRow();

// Установка значений
row.SetValue("health", 100);
row.SetValue("damage", 25.5f);
row.SetValue("isActive", true);
row.SetValue("position", new Vector3(10, 0, 5));

// Получение значений
int health = (int)row.GetValue("health");
Vector3 pos = (Vector3)row.GetValue("position");

// Клонирование
var clonedRow = row.Clone();
```

**Особенности:**
- Автоматическая сериализация для Unity
- Поддержка всех типов данных
- Двойной словарь (runtime + serialized)
- Timestamp создания

---

#### 🔷 ColumnDefinition
Описание колонки таблицы.

**Создание колонки:**
```csharp
// Простая колонка
var nameCol = new ColumnDefinition(
    id: "char_name",
    name: "Character Name",
    type: ColumnType.String,
    required: true,
    defaultValue: "Unnamed"
);

// Колонка с валидацией
var healthCol = new ColumnDefinition("health", "HP", ColumnType.Integer, true, 100);
healthCol.Validator = new RangeValidator(0, 1000);

// Enum колонка
var rarityCol = new ColumnDefinition("rarity", "Rarity", ColumnType.Enum);
rarityCol.EnumDefinition.AddValue("Common");
rarityCol.EnumDefinition.AddValue("Rare");
rarityCol.EnumDefinition.AddValue("Epic");
rarityCol.EnumDefinition.AddValue("Legendary");

// Asset Reference колонка
var prefabCol = new ColumnDefinition("prefab", "Prefab", ColumnType.AssetReference);
prefabCol.SetAssetType(typeof(GameObject));
```

**Свойства:**
| Свойство | Тип | Описание |
|----------|-----|----------|
| `ColumnId` | `string` | Уникальный ID колонки |
| `DisplayName` | `string` | Отображаемое имя |
| `DataType` | `ColumnType` | Тип данных |
| `IsRequired` | `bool` | Обязательность заполнения |
| `DefaultValue` | `object` | Значение по умолчанию |
| `EnumDefinition` | `EnumDefinition` | Определение enum (если тип Enum) |
| `Validator` | `IValidator` | Валидатор значений |

---

#### ColumnType (enum)
Поддерживаемые типы данных.

```csharp
public enum ColumnType
{
    String,           // Текст
    Integer,          // Целое число
    Float,            // Дробное число
    Boolean,          // true/false
    Enum,             // Перечисление
    AssetReference,   // Ссылка на Unity Asset
    Color,            // Цвет (Unity Color)
    Vector2,          // 2D вектор
    Vector3           // 3D вектор
}
```

**Примеры использования:**
```csharp
// String
row.SetValue("description", "Powerful sword");

// Integer
row.SetValue("level", 10);

// Float
row.SetValue("critChance", 0.25f);

// Boolean
row.SetValue("isUnlocked", true);

// Enum
row.SetValue("rarity", "Legendary");

// AssetReference
GameObject prefab = Resources.Load<GameObject>("Prefabs/Sword");
row.SetValue("prefab", prefab);

// Color
row.SetValue("tint", Color.red);

// Vector2
row.SetValue("gridPosition", new Vector2(5, 3));

// Vector3
row.SetValue("spawnPoint", new Vector3(10, 0, -5));
```

---

#### SerializableDictionary<TKey, TValue>
Словарь с поддержкой Unity сериализации.

**Проблема стандартного Dictionary:**
```csharp
// ❌ НЕ РАБОТАЕТ с Unity сериализацией
[SerializeField] private Dictionary<string, int> data;
```

**Решение:**
```csharp
// ✅ РАБОТАЕТ
[SerializeField] private SerializableDictionary<string, int> data;
```

**Как это работает:**
```csharp
// При сериализации Unity вызывает:
OnBeforeSerialize() {
    keys.Clear();
    values.Clear();
    foreach (var kvp in this) {
        keys.Add(kvp.Key);
        values.Add(kvp.Value);
    }
}

// При десериализации:
OnAfterDeserialize() {
    Clear();
    for (int i = 0; i < keys.Count; i++) {
        this[keys[i]] = values[i];
    }
}
```

---

#### ValidationResult & ValidationError
Результаты валидации данных.

**Использование:**
```csharp
ValidationResult result = table.ValidateData();

if (result.IsValid) {
    Debug.Log("All data is valid!");
} else {
    Debug.LogWarning($"Found {result.Errors.Count} errors:");
    
    foreach (var error in result.Errors) {
        Debug.LogError($"Row: {error.RowId}, Column: {error.ColumnId}");
        Debug.LogError($"Message: {error.Message}");
        Debug.LogError($"Severity: {error.Severity}");
    }
}
```

**Severity уровни:**
```csharp
public enum ErrorSeverity
{
    Warning,   // Предупреждение (не критично)
    Error,     // Ошибка (нарушение правил)
    Critical   // Критическая ошибка (невозможно использовать)
}
```

---

## 2. Data.Operations - Операции над данными

**Назначение**: Фильтрация и сортировка строк таблицы.

### Фильтрация

#### IFilter (интерфейс)
Базовый интерфейс для всех фильтров.

```csharp
public interface IFilter
{
    List<BalanceRow> Apply(List<BalanceRow> rows);
}
```

---

#### ColumnFilter
Простой фильтр по одной колонке.

**Использование:**
```csharp
// Найти всех персонажей с уровнем > 10
var condition = new FilterCondition {
    ColumnId = "level",
    Operator = FilterOperator.GreaterThan,
    Value = 10
};

var filter = new ColumnFilter(condition);
var filtered = filter.Apply(table.Rows);
```

**Доступные операторы:**
```csharp
public enum FilterOperator
{
    Equals,        // ==
    NotEquals,     // !=
    GreaterThan,   // >
    LessThan,      // <
    Contains,      // substring search
    StartsWith,    // начинается с
    EndsWith       // заканчивается на
}
```

---

#### CompositeFilter
Композитный фильтр (AND/OR логика).

**Пример - AND логика:**
```csharp
// Найти персонажей: level > 10 И rarity == "Epic"
var composite = new CompositeFilter(LogicalOperator.And);

composite.AddFilter(new ColumnFilter(new FilterCondition {
    ColumnId = "level",
    Operator = FilterOperator.GreaterThan,
    Value = 10
}));

composite.AddFilter(new ColumnFilter(new FilterCondition {
    ColumnId = "rarity",
    Operator = FilterOperator.Equals,
    Value = "Epic"
}));

var result = composite.Apply(table.Rows);
```

**Пример - OR логика:**
```csharp
// Найти персонажей: rarity == "Epic" ИЛИ rarity == "Legendary"
var composite = new CompositeFilter(LogicalOperator.Or);

composite.AddFilter(new ColumnFilter(new FilterCondition {
    ColumnId = "rarity",
    Operator = FilterOperator.Equals,
    Value = "Epic"
}));

composite.AddFilter(new ColumnFilter(new FilterCondition {
    ColumnId = "rarity",
    Operator = FilterOperator.Equals,
    Value = "Legendary"
}));

var result = composite.Apply(table.Rows);
```

**Вложенные фильтры:**
```csharp
// (level > 10 AND damage > 50) OR (isUnlocked == false)
var mainFilter = new CompositeFilter(LogicalOperator.Or);

var andFilter = new CompositeFilter(LogicalOperator.And);
andFilter.AddFilter(levelFilter);
andFilter.AddFilter(damageFilter);

mainFilter.AddFilter(andFilter);
mainFilter.AddFilter(unlockedFilter);
```

---

### Сортировка

#### TableSorter
Оптимизированная сортировка строк.

**Использование:**
```csharp
// Сортировка по имени (возрастание)
var sorted = TableSorter.Sort(
    rows: table.Rows,
    columnId: "name",
    direction: SortDirection.Ascending,
    columnType: ColumnType.String
);

// Сортировка по уровню (убывание)
var sortedByLevel = TableSorter.Sort(
    rows: table.Rows,
    columnId: "level",
    direction: SortDirection.Descending,
    columnType: ColumnType.Integer
);
```

**Направления сортировки:**
```csharp
public enum SortDirection
{
    None,        // Без сортировки
    Ascending,   // По возрастанию ▲
    Descending   // По убыванию ▼
}
```

**Оптимизации:**
- Использует `Array.Sort` вместо LINQ (в 3-5 раз быстрее)
- Кастомный компаратор для каждого типа данных
- Умная обработка null значений

---

#### SortingState
Состояние сортировки с переключением.

**Использование:**
```csharp
var state = new SortingState();

// Первый клик: None → Ascending
state.Toggle("level");
Debug.Log(state.Direction); // Ascending

// Второй клик: Ascending → Descending
state.Toggle("level");
Debug.Log(state.Direction); // Descending

// Третий клик: Descending → None
state.Toggle("level");
Debug.Log(state.Direction); // None

// Клик по другой колонке: сброс + Ascending
state.Toggle("name");
Debug.Log(state.Direction); // Ascending
```

---

## 3. Services - Сервисы

**Назначение**: Вспомогательные сервисы для валидации, undo/redo и работы с буфером обмена.

### Валидация данных

#### IValidator (интерфейс)
```csharp
public interface IValidator
{
    bool Validate(object value);
    string GetErrorMessage();
}
```

---

#### RangeValidator
Проверка числовых значений в диапазоне.

**Использование:**
```csharp
// HP от 0 до 1000
var healthValidator = new RangeValidator(0, 1000);
column.Validator = healthValidator;

// Проверка
bool valid = healthValidator.Validate(500);  // true
valid = healthValidator.Validate(1500);      // false
```

---

#### RegexValidator
Валидация по регулярному выражению.

**Примеры:**
```csharp
// Email
var emailValidator = new RegexValidator(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");

// Телефон (формат +X-XXX-XXX-XXXX)
var phoneValidator = new RegexValidator(@"^\+\d{1}-\d{3}-\d{3}-\d{4}$");

// Только буквы и пробелы
var nameValidator = new RegexValidator(@"^[a-zA-Z\s]+$");

// IP адрес
var ipValidator = new RegexValidator(
    @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}" +
    @"(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"
);
```

---

#### RequiredValidator
Проверка обязательных полей.

```csharp
var validator = new RequiredValidator();
column.Validator = validator;

// Проверка
validator.Validate("Some text");  // true
validator.Validate("");           // false
validator.Validate(null);         // false
```

---

### Undo/Redo система

#### UndoRedoService
Управление историей изменений.

**Использование:**
```csharp
var undoRedo = new UndoRedoService();

// Выполнение команды
var command = new EditCellCommand(table, rowId, columnId, oldValue, newValue);
undoRedo.ExecuteCommand(command);

// Отмена
if (undoRedo.CanUndo()) {
    undoRedo.Undo();
}

// Повтор
if (undoRedo.CanRedo()) {
    undoRedo.Redo();
}

// Очистка истории
undoRedo.Clear();
```

**Архитектура:**
```
┌──────────────────────────────────┐
│      UndoRedoService             │
├──────────────────────────────────┤
│  undoStack: Stack<ICommand>      │ ← История отмен
│  redoStack: Stack<ICommand>      │ ← История повторов
└──────────────────────────────────┘
```

---

#### ICommand (интерфейс)
Базовый интерфейс для команд.

```csharp
public interface ICommand
{
    void Execute();           // Выполнить команду
    void Undo();             // Отменить команду
    string GetDescription(); // Описание команды
}
```

---

#### Доступные команды

**AddRowCommand** - Добавление строки
```csharp
var row = table.AddRow();
var command = new AddRowCommand(table, row);
undoRedo.ExecuteCommand(command);
```

**EditCellCommand** - Редактирование ячейки
```csharp
var oldValue = row.GetValue("health");
var newValue = 150;
var command = new EditCellCommand(table, rowId, "health", oldValue, newValue);
undoRedo.ExecuteCommand(command);
```

**DeleteRowCommand** - Удаление одной строки
```csharp
var command = new DeleteRowCommand(table, row);
undoRedo.ExecuteCommand(command);
```

**MultiDeleteCommand** - Удаление множества строк
```csharp
var rowsToDelete = new List<BalanceRow> { row1, row2, row3 };
var command = new MultiDeleteCommand(table, rowsToDelete);
undoRedo.ExecuteCommand(command);
```

---

### Буфер обмена

#### ClipboardService (static)
Копирование и вставка данных.

**Использование:**
```csharp
// Копирование одного значения
ClipboardService.Copy("health", 100);

// Проверка возможности вставки
if (ClipboardService.CanPaste("health")) {
    var value = ClipboardService.Paste("health");
    row.SetValue("health", value);
}

// Копирование нескольких значений
var values = new Dictionary<string, object> {
    { "health", 100 },
    { "damage", 25 },
    { "defense", 10 }
};
ClipboardService.CopyMultiple(values);

// Вставка нескольких значений
var pastedValues = ClipboardService.PasteMultiple();
foreach (var kvp in pastedValues) {
    row.SetValue(kvp.Key, kvp.Value);
}

// Очистка буфера
ClipboardService.Clear();
```

**Особенности:**
- Интеграция с системным буфером обмена
- Поддержка множественных значений
- Проверка совместимости типов

---

### Управление таблицами

#### TableManager (Singleton)
Менеджер для работы с таблицами на уровне проекта.

**Использование:**
```csharp
var manager = TableManager.Instance;

// Загрузка таблицы
var table = manager.LoadTable("Assets/Data/Characters.asset");

// Создание новой таблицы
var columns = new List<ColumnDefinition> {
    new ColumnDefinition("id", "ID", ColumnType.String, true),
    new ColumnDefinition("name", "Name", ColumnType.String, true)
};
var newTable = manager.CreateTable("Characters", columns);

// Сохранение изменений
manager.SaveTable(table);

// Удаление таблицы
manager.DeleteTable(table.TableId);
```

---

## 4. ImportExport - Импорт/Экспорт

**Назначение**: Обмен данными с внешними форматами (CSV).

### Экспорт

#### CSVExporter
Экспорт таблицы в CSV формат.

**Использование:**
```csharp
var exporter = new CSVExporter();
bool success = exporter.Export(table, "Assets/Export/characters.csv");

if (success) {
    Debug.Log("Table exported successfully!");
}
```

**Формат вывода:**
```csv
ID,Name,Level,Health,Damage,IsActive
char_001,Knight,10,100,25,true
char_002,Archer,8,75,30,true
char_003,Mage,12,60,40,false
```

---

### Импорт

#### CSVImporter
Импорт таблицы из CSV с автоопределением типов.

**Использование:**
```csharp
var importer = new CSVImporter();

if (importer.CanImport("Assets/Import/data.csv")) {
    var table = importer.Import("Assets/Import/data.csv");
    
    if (table != null) {
        Debug.Log($"Imported {table.Rows.Count} rows");
        Debug.Log($"Columns: {table.Columns.Count}");
    }
}
```

**Автоопределение типов:**
- Все значения парсятся как `bool` → `ColumnType.Boolean`
- Все значения парсятся как `int` → `ColumnType.Integer`
- Все значения парсятся как `float` → `ColumnType.Float`
- Иначе → `ColumnType.String`

**Пример CSV:**
```csv
Name,Level,Experience,IsUnlocked
Hero,10,1500,true
Warrior,5,500,false
Mage,15,3000,true
```

**Результат:**
- `Name`: String
- `Level`: Integer
- `Experience`: Integer
- `IsUnlocked`: Boolean

---

## 5. Editor.UI - Пользовательский интерфейс

**Назначение**: Главное окно редактора таблиц.

### BalanceTableEditorWindow

Мощный табличный редактор с оптимизацией для больших данных.

#### Открытие редактора
```
Window → BalanceForge → Table Editor
```

#### Основные возможности

**1. Редактирование данных**
- Inline редактирование всех типов
- Множественное выделение (Shift/Ctrl)
- Copy/Paste (Ctrl+C/V)
- Удаление строк (Delete)

**2. Undo/Redo**
- Отмена: `Ctrl+Z` / `⌘+Z`
- Повтор: `Ctrl+Y` / `Ctrl+Shift+Z` / `⌘+Shift+Z`
- История всех изменений

**3. Фильтрация**
- Множественные условия
- AND/OR логика
- 7 типов операторов
- Живое обновление

**4. Сортировка**
- Клик по заголовку колонки
- Переключение направления: None → ▲ → ▼ → None
- Поддержка всех типов данных

**5. Валидация**
- Проверка при редактировании
- Отображение списка ошибок
- Подсветка некорректных значений

**6. Импорт/Экспорт**
- CSV экспорт
- CSV импорт с проверкой структуры
- Предупреждения о несоответствиях

#### Горячие клавиши

| Клавиши | Действие |
|---------|----------|
| `Ctrl+C` | Копировать выделенную ячейку |
| `Ctrl+V` | Вставить в фокусную ячейку |
| `Ctrl+Z` | Отменить последнее действие |
| `Ctrl+Y` или `Ctrl+Shift+Z` | Повторить отмененное действие |
| `Delete` | Удалить выделенные строки |
| `Click` | Выделить строку/ячейку |
| `Shift+Click` | Множественное выделение |

#### Оптимизации

**Виртуальная прокрутка:**
```csharp
// Отрисовываются только видимые строки + буфер
visibleStartIndex = (scrollY - headerHeight) / rowHeight - BUFFER;
visibleEndIndex = (scrollY + viewHeight) / rowHeight + BUFFER;
```

**Кеширование:**
```csharp
// Кеш значений ячеек (обновляется каждый фрейм)
Dictionary<string, object> cellValueCache;

// Кеш GUI контента (постоянный, до 500 элементов)
Dictionary<string, GUIContent> guiContentCache;
```

**Throttling перерисовки:**
```csharp
// Перерисовка максимум каждые 50ms
const float REPAINT_THROTTLE = 0.05f;
```

**Прямая отрисовка:**
```csharp
// Использование GUI вместо GUILayout (быстрее в 2-3 раза)
GUI.BeginScrollView(...);
EditorGUI.DrawRect(...);
EditorGUI.TextField(...);
GUI.EndScrollView();
```

#### Производительность

| Количество строк | FPS | Памяти |
|------------------|-----|--------|
| 100 | 60 | ~5 MB |
| 1,000 | 60 | ~15 MB |
| 10,000 | 55-60 | ~50 MB |
| 50,000 | 45-55 | ~200 MB |

---

## 6. Editor.Windows - Вспомогательные окна

### CreateTableWizard

Мастер создания новых таблиц.

#### Открытие
```
Assets → Create → BalanceForge → Balance Table Wizard
```

#### Процесс создания

**Шаг 1: Имя таблицы**
```
Table Name: [Characters]
```

**Шаг 2: Настройка колонок**
```
Column 1:
  Name: [ID]
  Type: [String ▼]
  Required: [✓]
  
Column 2:
  Name: [Character Name]
  Type: [String ▼]
  Required: [✓]
  
Column 3:
  Name: [Level]
  Type: [Integer ▼]
  Required: [□]
  
Column 4:
  Name: [Rarity]
  Type: [Enum ▼]
  Required: [✓]
  Enum Values:
    - Common
    - Rare
    - Epic
    - Legendary
```

**Шаг 3: Создание**
- Нажать "Create Table"
- Выбрать место сохранения
- Таблица автоматически откроется в редакторе

---

## Использование

### Пример 1: Создание таблицы персонажей

```csharp
using BalanceForge.Core.Data;
using BalanceForge.Services;

public class CharacterTableSetup
{
    public static BalanceTable CreateCharacterTable()
    {
        var table = ScriptableObject.CreateInstance<BalanceTable>();
        table.TableName = "Characters";
        
        // ID колонка
        var idCol = new ColumnDefinition("id", "ID", ColumnType.String, true);
        idCol.Validator = new RegexValidator(@"^char_\d{3}$");
        table.AddColumn(idCol);
        
        // Имя
        var nameCol = new ColumnDefinition("name", "Name", ColumnType.String, true);
        table.AddColumn(nameCol);
        
        // Уровень
        var levelCol = new ColumnDefinition("level", "Level", ColumnType.Integer, true, 1);
        levelCol.Validator = new RangeValidator(1, 100);
        table.AddColumn(levelCol);
        
        // HP
        var hpCol = new ColumnDefinition("hp", "HP", ColumnType.Integer, true, 100);
        hpCol.Validator = new RangeValidator(1, 10000);
        table.AddColumn(hpCol);
        
        // Редкость
        var rarityCol = new ColumnDefinition("rarity", "Rarity", ColumnType.Enum, true);
        rarityCol.EnumDefinition.AddValue("Common");
        rarityCol.EnumDefinition.AddValue("Rare");
        rarityCol.EnumDefinition.AddValue("Epic");
        rarityCol.EnumDefinition.AddValue("Legendary");
        table.AddColumn(rarityCol);
        
        // Prefab
        var prefabCol = new ColumnDefinition("prefab", "Prefab", ColumnType.AssetReference);
        prefabCol.SetAssetType(typeof(GameObject));
        table.AddColumn(prefabCol);
        
        return table;
    }
}
```

### Пример 2: Заполнение данными

```csharp
public static void PopulateCharacterTable(BalanceTable table)
{
    // Рыцарь
    var knight = table.AddRow();
    knight.SetValue("id", "char_001");
    knight.SetValue("name", "Knight");
    knight.SetValue("level", 10);
    knight.SetValue("hp", 500);
    knight.SetValue("rarity", "Common");
    
    // Маг
    var mage = table.AddRow();
    mage.SetValue("id", "char_002");
    mage.SetValue("name", "Mage");
    mage.SetValue("level", 15);
    mage.SetValue("hp", 300);
    mage.SetValue("rarity", "Epic");
    
    // Дракон
    var dragon = table.AddRow();
    dragon.SetValue("id", "char_003");
    dragon.SetValue("name", "Dragon");
    dragon.SetValue("level", 50);
    dragon.SetValue("hp", 5000);
    dragon.SetValue("rarity", "Legendary");
}
```

### Пример 3: Поиск и фильтрация

```csharp
using BalanceForge.Data.Operations;

public class CharacterQueries
{
    // Найти всех персонажей уровня 10+
    public static List<BalanceRow> GetHighLevelCharacters(BalanceTable table)
    {
        var filter = new ColumnFilter(new FilterCondition {
            ColumnId = "level",
            Operator = FilterOperator.GreaterThan,
            Value = 10
        });
        
        return filter.Apply(table.Rows);
    }
    
    // Найти Epic и Legendary персонажей
    public static List<BalanceRow> GetRareCharacters(BalanceTable table)
    {
        var composite = new CompositeFilter(LogicalOperator.Or);
        
        composite.AddFilter(new ColumnFilter(new FilterCondition {
            ColumnId = "rarity",
            Operator = FilterOperator.Equals,
            Value = "Epic"
        }));
        
        composite.AddFilter(new ColumnFilter(new FilterCondition {
            ColumnId = "rarity",
            Operator = FilterOperator.Equals,
            Value = "Legendary"
        }));
        
        return composite.Apply(table.Rows);
    }
    
    // Найти персонажей: уровень > 10 И HP > 400
    public static List<BalanceRow> GetStrongCharacters(BalanceTable table)
    {
        var composite = new CompositeFilter(LogicalOperator.And);
        
        composite.AddFilter(new ColumnFilter(new FilterCondition {
            ColumnId = "level",
            Operator = FilterOperator.GreaterThan,
            Value = 10
        }));
        
        composite.AddFilter(new ColumnFilter(new FilterCondition {
            ColumnId = "hp",
            Operator = FilterOperator.GreaterThan,
            Value = 400
        }));
        
        return composite.Apply(table.Rows);
    }
}
```

### Пример 4: Runtime загрузка данных

```csharp
public class CharacterLoader : MonoBehaviour
{
    [SerializeField] private BalanceTable characterTable;
    
    void Start()
    {
        LoadCharacters();
    }
    
    void LoadCharacters()
    {
        foreach (var row in characterTable.Rows)
        {
            string id = row.GetValue("id") as string;
            string name = row.GetValue("name") as string;
            int level = (int)row.GetValue("level");
            int hp = (int)row.GetValue("hp");
            string rarity = row.GetValue("rarity") as string;
            
            Debug.Log($"Character: {name} (Lv.{level})");
            Debug.Log($"  ID: {id}");
            Debug.Log($"  HP: {hp}");
            Debug.Log($"  Rarity: {rarity}");
        }
    }
    
    // Найти персонажа по ID
    BalanceRow FindCharacterById(string id)
    {
        foreach (var row in characterTable.Rows)
        {
            if (row.GetValue("id") as string == id)
                return row;
        }
        return null;
    }
    
    // Получить всех персонажей определенной редкости
    List<BalanceRow> GetCharactersByRarity(string rarity)
    {
        var result = new List<BalanceRow>();
        
        foreach (var row in characterTable.Rows)
        {
            if (row.GetValue("rarity") as string == rarity)
                result.Add(row);
        }
        
        return result;
    }
}
```

---

## API Reference

### Core API

```csharp
// Создание таблицы
var table = ScriptableObject.CreateInstance<BalanceTable>();

// Управление колонками
table.AddColumn(ColumnDefinition);
table.RemoveColumn(string columnId);
ColumnDefinition col = table.GetColumn(string columnId);

// Управление строками
BalanceRow row = table.AddRow();
bool removed = table.RemoveRow(string rowId);
BalanceRow row = table.GetRow(int index);

// Валидация
ValidationResult result = table.ValidateData();

// Строки
object value = row.GetValue(string columnId);
row.SetValue(string columnId, object value);
BalanceRow clone = row.Clone();

// Колонки
var col = new ColumnDefinition(id, name, type, required, defaultValue);
col.Validator = IValidator;
bool valid = col.Validate(object value);
```

### Operations API

```csharp
// Фильтрация
var filter = new ColumnFilter(FilterCondition);
var composite = new CompositeFilter(LogicalOperator);
composite.AddFilter(IFilter);
List<BalanceRow> filtered = filter.Apply(List<BalanceRow>);

// Сортировка
List<BalanceRow> sorted = TableSorter.Sort(
    rows, columnId, direction, columnType
);

var state = new SortingState();
state.Toggle(string columnId);
```

### Services API

```csharp
// Валидация
IValidator validator = new RangeValidator(min, max);
IValidator validator = new RegexValidator(pattern);
IValidator validator = new RequiredValidator();
bool valid = validator.Validate(object value);

// Undo/Redo
var service = new UndoRedoService();
service.ExecuteCommand(ICommand);
service.Undo();
service.Redo();
bool canUndo = service.CanUndo();
bool canRedo = service.CanRedo();

// Commands
var cmd = new AddRowCommand(table, row);
var cmd = new EditCellCommand(table, rowId, columnId, oldVal, newVal);
var cmd = new DeleteRowCommand(table, row);
var cmd = new MultiDeleteCommand(table, List<BalanceRow>);

// Clipboard
ClipboardService.Copy(columnId, value);
ClipboardService.CopyMultiple(Dictionary<string, object>);
bool canPaste = ClipboardService.CanPaste(columnId);
object value = ClipboardService.Paste(columnId);

// Table Manager
var manager = TableManager.Instance;
BalanceTable table = manager.LoadTable(assetPath);
manager.SaveTable(table);
BalanceTable table = manager.CreateTable(name, columns);
bool deleted = manager.DeleteTable(tableId);
```

### ImportExport API

```csharp
// Экспорт
var exporter = new CSVExporter();
bool success = exporter.Export(table, filePath);

// Импорт
var importer = new CSVImporter();
bool canImport = importer.CanImport(filePath);
BalanceTable table = importer.Import(filePath);
```

---

## Расширение функциональности

### Создание кастомного валидатора

```csharp
using BalanceForge.Services;

public class EmailValidator : IValidator
{
    private static readonly Regex EmailRegex = new Regex(
        @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$",
        RegexOptions.Compiled
    );
    
    public bool Validate(object value)
    {
        if (value == null) return false;
        return EmailRegex.IsMatch(value.ToString());
    }
    
    public string GetErrorMessage()
    {
        return "Invalid email format";
    }
}

// Использование
var emailColumn = new ColumnDefinition("email", "Email", ColumnType.String, true);
emailColumn.Validator = new EmailValidator();
```

### Создание кастомного фильтра

```csharp
using BalanceForge.Data.Operations;

public class RarityTierFilter : IFilter
{
    private int minTier;
    
    public RarityTierFilter(int minTier)
    {
        this.minTier = minTier;
    }
    
    public List<BalanceRow> Apply(List<BalanceRow> rows)
    {
        return rows.Where(row => {
            string rarity = row.GetValue("rarity") as string;
            int tier = GetRarityTier(rarity);
            return tier >= minTier;
        }).ToList();
    }
    
    private int GetRarityTier(string rarity)
    {
        return rarity switch {
            "Common" => 1,
            "Rare" => 2,
            "Epic" => 3,
            "Legendary" => 4,
            _ => 0
        };
    }
}

// Использование
var filter = new RarityTierFilter(minTier: 3); // Epic и выше
var filtered = filter.Apply(table.Rows);
```

### Создание кастомного экспортера

```csharp
using BalanceForge.ImportExport;
using System.Xml;

public class XMLExporter : IExporter
{
    public bool Export(BalanceTable table, string filePath)
    {
        try
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement("BalanceTable");
            root.SetAttribute("name", table.TableName);
            doc.AppendChild(root);
            
            // Колонки
            var columnsNode = doc.CreateElement("Columns");
            foreach (var col in table.Columns)
            {
                var colNode = doc.CreateElement("Column");
                colNode.SetAttribute("id", col.ColumnId);
                colNode.SetAttribute("name", col.DisplayName);
                colNode.SetAttribute("type", col.DataType.ToString());
                columnsNode.AppendChild(colNode);
            }
            root.AppendChild(columnsNode);
            
            // Строки
            var rowsNode = doc.CreateElement("Rows");
            foreach (var row in table.Rows)
            {
                var rowNode = doc.CreateElement("Row");
                rowNode.SetAttribute("id", row.RowId);
                
                foreach (var col in table.Columns)
                {
                    var cellNode = doc.CreateElement("Cell");
                    cellNode.SetAttribute("column", col.ColumnId);
                    cellNode.InnerText = row.GetValue(col.ColumnId)?.ToString() ?? "";
                    rowNode.AppendChild(cellNode);
                }
                
                rowsNode.AppendChild(rowNode);
            }
            root.AppendChild(rowsNode);
            
            doc.Save(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

### Добавление нового типа колонки

```csharp
// 1. Добавить в enum
public enum ColumnType
{
    // ... существующие типы
    DateTime,  // НОВЫЙ ТИП
}

// 2. Обновить сериализацию в ColumnDefinition
private string SerializeValue(object value)
{
    // ... существующие случаи
    
    if (value is System.DateTime dt)
        return dt.ToString("o"); // ISO 8601
    
    return value.ToString();
}

private object DeserializeValue(string serialized)
{
    // ... существующие случаи
    
    case ColumnType.DateTime:
        return System.DateTime.Parse(serialized);
}

// 3. Добавить отрисовку в редакторе
private object DrawCellByType(Rect position, ColumnDefinition column, object value)
{
    // ... существующие случаи
    
    case ColumnType.DateTime:
        System.DateTime dt = value is System.DateTime ? (System.DateTime)value : System.DateTime.Now;
        string dateStr = EditorGUI.TextField(position, dt.ToString("yyyy-MM-dd HH:mm:ss"));
        if (System.DateTime.TryParse(dateStr, out System.DateTime newDt))
            return newDt;
        return dt;
}
```

---

## Производительность

### Оптимизации

1. **Виртуальная прокрутка**
   - Отрисовываются только видимые строки
   - Буфер ±5 строк для плавности

2. **Кеширование**
   - Значения ячеек кешируются на фрейм
   - GUI контент кешируется постоянно
   - Ограничение кеша: 500 элементов

3. **Прямая отрисовка**
   - Использование GUI вместо GUILayout
   - Ускорение в 2-3 раза

4. **Throttling**
   - Перерисовка максимум каждые 50ms
   - Снижение нагрузки на CPU

5. **Оптимизированная сортировка**
   - Array.Sort вместо LINQ OrderBy
   - Ускорение в 3-5 раз

## Требования

Версия Unity 2022+
