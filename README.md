# Лабораторная работа №33-34. Полноценный CRUD с базой данных

**Студент:** Грошев Никита  
**Группа:** ИСП-232  
**Дата:** 2026-04-08

---

## Описание проекта

Приложение «Заметки» (NotesApp) представляет собой полноценное REST API для управления заметками и категориями. Проект реализует:

- Полный CRUD (Create, Read, Update, Delete) для двух связанных сущностей: `Category` и `Note`.
- Связь **один-ко-многим**: одна категория может содержать много заметок.
- Валидацию входных данных через **Data Annotations** (`[Required]`, `[MaxLength]`, `[Range]`, `[RegularExpression]`).
- Единый формат ответов (`ApiResponse<T>` / `ApiError`) для успешных операций и ошибок.
- **Паттерн Repository** для изоляции логики доступа к данным от контроллеров.
- Фильтрацию, поиск, сортировку и пагинацию заметок.
- Автоматическое применение миграций при старте приложения.
- Документацию API через **Swagger**.

**Технологии:** ASP.NET Core 10, Entity Framework Core, SQLite, Swashbuckle.

---

## Структура проекта

```
NotesApp/
├── Controllers/
│   ├── CategoriesController.cs
│   └── NotesController.cs
├── Data/
│   └── AppDbContext.cs
├── Helpers/
│   └── ApiResponse.cs
├── Models/
│   ├── Category.cs
│   ├── Note.cs
│   └── DTOs/
│       ├── CategoryDto.cs
│       ├── NoteDto.cs
│       └── NoteFilterDto.cs
├── Repositories/
│   ├── ICategoryRepository.cs
│   ├── CategoryRepository.cs
│   ├── INoteRepository.cs
│   └── NoteRepository.cs
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── notesapp.db (создаётся при миграции)
```

---

## Таблица маршрутов API

### Категории

| Метод   | URL                           | Описание                              | Статусы ответа               |
|---------|-------------------------------|---------------------------------------|------------------------------|
| GET     | /api/categories               | Все категории с количеством заметок    | 200                          |
| GET     | /api/categories/{id}          | Одна категория по ID                  | 200, 404                     |
| GET     | /api/categories/{id}/notes    | Категория со списком заметок          | 200, 404                     |
| POST    | /api/categories               | Создать новую категорию               | 201, 400                     |
| PUT     | /api/categories/{id}          | Обновить категорию                    | 200, 400, 404                |
| DELETE  | /api/categories/{id}          | Удалить категорию (если нет заметок)  | 204, 400, 404                |

### Заметки

| Метод   | URL                           | Описание                              | Статусы ответа               |
|---------|-------------------------------|---------------------------------------|------------------------------|
| GET     | /api/notes                    | Все заметки (с фильтрацией, поиском, пагинацией) | 200                  |
| GET     | /api/notes/{id}               | Одна заметка по ID                    | 200, 404                     |
| POST    | /api/notes                    | Создать новую заметку                 | 201, 400                     |
| PUT     | /api/notes/{id}               | Обновить заметку                      | 200, 400, 404                |
| PATCH   | /api/notes/{id}/pin           | Закрепить / открепить заметку         | 200, 404                     |
| PATCH   | /api/notes/{id}/archive       | Архивировать / восстановить заметку   | 200, 404                     |
| DELETE  | /api/notes/{id}               | Удалить заметку                       | 204, 404                     |

**Параметры GET /api/notes:**
- `categoryId` – фильтр по категории
- `isPinned` – только закреплённые (true/false)
- `archived` – включать архивные (по умолчанию false)
- `search` – поиск по заголовку и содержимому
- `minPriority` – минимальный приоритет (1–5)
- `sortBy` – поле сортировки (createdAt, title, priority, updatedAt)
- `descending` – направление (true – по убыванию)
- `page` – номер страницы (≥1)
- `pageSize` – размер страницы (1–50)

---

## Паттерн Repository

**Что это?**  
Repository – это паттерн проектирования, который изолирует логику работы с данными от логики контроллера. Вместо того чтобы напрямую использовать `DbContext` внутри контроллера, мы создаём слой-посредник – репозиторий.

**Как устроен в проекте:**
- Интерфейсы (`ICategoryRepository`, `INoteRepository`) описывают контракт – какие методы доступны для работы с данными.
- Конкретные классы (`CategoryRepository`, `NoteRepository`) реализуют эти интерфейсы, содержат всю логику запросов к БД (фильтрацию, пагинацию, Include, сохранение).
- Контроллеры зависят только от интерфейсов, а не от конкретной реализации.

**Зачем нужен:**
| Проблема без Repository | Решение с Repository |
|------------------------|----------------------|
| Логика запросов размазана по контроллерам | Все запросы собраны в одном месте |
| Трудно тестировать контроллер отдельно от БД | Можно подменить репозиторий на тестовый мок |
| При смене БД нужно менять все контроллеры | Меняется только репозиторий |
| Дублирование одинаковых запросов | Переиспользуемые методы |

**Пример в коде:**
```csharp
public class NotesController : ControllerBase
{
    private readonly INoteRepository _noteRepo;
    public NotesController(INoteRepository noteRepo) => _noteRepo = noteRepo;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] NoteFilterDto filter)
    {
        var notes = await _noteRepo.GetAllAsync(filter);
        return Ok(ApiResponse<IEnumerable<NoteResponseDto>>.Ok(notes));
    }
}
```

Благодаря этому подходу код становится модульным, тестируемым и легко поддерживаемым.

---

## Запуск проекта

1. Клонировать репозиторий:
   ```bash
   git clone <URL репозитория>
   cd NotesApp
   ```
2. Установить .NET 10 SDK.
3. Восстановить зависимости:
   ```bash
   dotnet restore
   ```
4. Применить миграции (автоматически при первом запуске или вручную):
   ```bash
   dotnet ef database update
   ```
5. Запустить приложение:
   ```bash
   dotnet run
   ```
6. Открыть Swagger UI: https://localhost:5001/swagger

---

## Примеры запросов (REST Client)

Файл `requests.http` (пример):
```http
@baseUrl = http://localhost:5000

### Все заметки
GET {{baseUrl}}/api/notes

### Заметки категории "учёба" (id=2)
GET {{baseUrl}}/api/notes?categoryId=2

### Поиск по слову "LINQ"
GET {{baseUrl}}/api/notes?search=LINQ

### Создать заметку
POST {{baseUrl}}/api/notes
Content-Type: application/json

{
  "title": "Заметка из REST Client",
  "content": "Тестируем полноценный CRUD с паттерном Repository",
  "priority": 4,
  "categoryId": 2
}
```

---

## Выводы

В ходе лабораторной работы:
- Закреплены навыки работы с EF Core (миграции, связи, навигационные свойства).
- Освоена валидация через Data Annotations.
- Реализован паттерн Repository для разделения логики доступа к данным.
- Создано полноценное REST API с фильтрацией, пагинацией и единым форматом ответов.
- Настроено автоматическое применение миграций при старте приложения.

Проект готов к интеграции с любым фронтендом (React, Vue, Angular) благодаря продуманной архитектуре и документированному API.
```