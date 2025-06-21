# Shopping List App

## 1. Обзор проекта

Это кроссплатформенное приложение для управления списками покупок, разработанное с использованием .NET MAUI для мобильных (Android, iOS, Windows) и десктопных клиентов, и Blazor WebAssembly для веб-клиента. Приложение использует архитектуру Offline-First, позволяя пользователям работать с данными даже без подключения к интернету, с последующей синхронизацией с сервером.

## 2. Архитектура

Приложение следует подходу **Offline-First**.

*   **Пользовательский интерфейс (UI)**: Реализован с использованием Blazor-компонентов, общих для MAUI и Web клиентов. UI взаимодействует исключительно с локальной базой данных.
*   **Локальная база данных (Local DB)**: На каждом клиенте используется база данных SQLite для хранения всех данных приложения (списки покупок, товары, категории, магазины). Это обеспечивает мгновенный доступ и возможность работы в оффлайн-режиме. `ShoppingRepository` инкапсулирует всю логику работы с локальной БД.
*   **Сервис синхронизации (SyncService)**: Фоновый сервис, отвечающий за двустороннюю синхронизацию данных между локальной БД и серверным API. Он отправляет локальные изменения на сервер и применяет полученные от сервера обновления к локальной БД.
*   **Серверное API (Server API)**: ASP.NET Core Web API, предоставляющее эндпоинт `/api/sync` для обработки запросов на синхронизацию.
*   **Серверная база данных (Server DB)**: MariaDB, используемая сервером для хранения основной копии данных.

**Схема взаимодействия:**

```
+-----------------+     +-----------------+     +---------------------+     +-----------------+     +-------------------+
|       UI        | --> |    Local DB     | --> |     SyncService     | <-> |   Server API    | --> |    Server DB      |
| (Blazor Comp.)  |     | (SQLite via     |     | (HttpClient to API) |     | (ASP.NET Core)  |     | (MariaDB)         |
|                 |     | ShoppingRepo)   |     |                     |     |                 |     |                   |
+-----------------+     +-----------------+     +---------------------+     +-----------------+     +-------------------+
```

## 3. Структура проекта

*   `ShoppingListApp.sln`: Файл решения.
*   `/src`: Корневая папка для исходного кода.
    *   `ShoppingListApp.Shared`: Библиотека классов, содержащая общие модели данных (`Models`) и объекты передачи данных (`DTOs`) для сервера и клиентов.
    *   `ShoppingListApp.Server`: Проект ASP.NET Core Web API. Бэкенд для синхронизации данных, работающий с MariaDB через Entity Framework Core.
        *   `/Controllers`: Содержит `SyncController` для обработки запросов синхронизации.
        *   `/Data`: Содержит `ServerDbContext`.
    *   `ShoppingListApp.Client.Core`: Библиотека классов Razor, содержащая общий UI (Blazor компоненты) и клиентскую логику.
        *   `/Components`: Общие Blazor-компоненты (`ItemListView.razor`, `FilterSortPanel.razor`, `ItemView.razor`).
        *   `/Data`: `LocalDbContext` (для SQLite) и `ShoppingRepository`.
        *   `/Services`: `SyncService` для логики синхронизации.
    *   `ShoppingListApp.Client.Maui`: Хост-проект .NET MAUI для Windows, Android (и iOS, Mac при наличии соответствующей среды сборки). Инициализирует сервисы из `Client.Core` и отображает BlazorWebView.
    *   `ShoppingListApp.Client.Web`: Хост-проект Blazor WebAssembly. Инициализирует сервисы из `Client.Core` для веб-приложения.

## 4. Запуск и сборка

### Необходимые инструменты:

*   **.NET 8 SDK**: Убедитесь, что установлен .NET 8 SDK или выше.
*   **Docker (рекомендуется для MariaDB)**: Для запуска экземпляра MariaDB. Либо используйте существующий сервер MariaDB.
*   **IDE**: Visual Studio 2022, VS Code или Rider.
*   **Для MAUI**:
    *   В Visual Studio Installer убедитесь, что установлена рабочая нагрузка ".NET Multi-platform App UI development".
    *   Для сборки под Android: Android SDK, эмуляторы или физическое устройство.
    *   Для сборки под iOS/macOS: macOS с Xcode.

### Настройка MariaDB (используя Docker):

1.  Создайте файл `docker-compose.yml` (не входит в этот проект, создается пользователем) в удобном месте:
    ```yml
    version: '3.8'
    services:
      mariadb:
        image: mariadb:latest
        restart: always
        environment:
          MYSQL_ROOT_PASSWORD: your_root_password # Замените на надежный пароль
          MYSQL_DATABASE: shoppinglistdb
          MYSQL_USER: your_user # Замените на ваше имя пользователя
          MYSQL_PASSWORD: your_password # Замените на ваш пароль
        ports:
          - "3306:3306"
        volumes:
          - mariadb_data:/var/lib/mysql

    volumes:
      mariadb_data:
    ```
2.  Запустите контейнер: `docker-compose up -d`
3.  Обновите строку подключения в `ShoppingListApp/src/ShoppingListApp.Server/appsettings.json` (создайте его, если нет) или непосредственно в `Program.cs` сервера:
    `"DefaultConnection": "Server=localhost;Port=3306;Database=shoppinglistdb;Uid=your_user;Pwd=your_password;"`
    (Замените `your_user` и `your_password` на те, что указали в `docker-compose.yml`).

    Пример `appsettings.Development.json` для сервера (создайте этот файл, если его нет):
    ```json
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning",
          "Microsoft.EntityFrameworkCore.Database.Command": "Information"
        }
      },
      "ConnectionStrings": {
        "DefaultConnection": "Server=localhost;Port=3306;Database=shoppinglistdb;Uid=your_user;Pwd=your_password;"
      }
    }
    ```

### Команды для запуска сервера (`ShoppingListApp.Server`):

1.  Перейдите в директорию сервера: `cd ShoppingListApp/src/ShoppingListApp.Server`
2.  Запустите сервер: `dotnet run`
    Сервер по умолчанию будет доступен по адресам, указанным в `Properties/launchSettings.json` (обычно `http://localhost:ПОРТ_HTTP` и `https://localhost:ПОРТ_HTTPS`). Следите за выводом консоли.

### Команды для запуска MAUI клиента (`ShoppingListApp.Client.Maui`):

1.  Убедитесь, что сервер запущен.
2.  Обновите базовый адрес API в `ShoppingListApp/src/ShoppingListApp.Client.Maui/MauiProgram.cs`, если ваш сервер работает на нестандартном порту или адресе.
    *   Для Android эмулятора `localhost` хост-машины обычно доступен по адресу `http://10.0.2.2`.
    *   Пример для Android эмулятора, если сервер API на `http://localhost:5258` (порт из `launchSettings.json` сервера):
        `baseAddress = "http://10.0.2.2:5258";`
    *   Для Windows используйте `http://localhost:ПОРТ_СЕРВЕРА`.
3.  Выберите целевую платформу:
    *   Через Visual Studio: выберите проект `ShoppingListApp.Client.Maui` как стартовый и выберите цель из выпадающего списка.
    *   Через CLI:
        *   Windows: `dotnet build -t:Run -f net8.0-windows10.0.19041.0` (версия Windows SDK может отличаться)
        *   Android: `dotnet build -t:Run -f net8.0-android` (убедитесь, что эмулятор запущен или устройство подключено)

### Команды для запуска Web клиента (`ShoppingListApp.Client.Web`):

1.  Убедитесь, что сервер запущен.
2.  **Настройка CORS на сервере**: Если клиент и сервер работают на разных портах/доменах во время разработки, настройте CORS в `ShoppingListApp.Server/Program.cs`:
    ```csharp
    // ... builder configuration ...
    var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: MyAllowSpecificOrigins,
                          policy =>
                          {
                              // Укажите URL вашего Blazor WASM клиента
                              policy.WithOrigins("http://localhost:ПОРТ_WASM_HTTP",
                                                 "https://localhost:ПОРТ_WASM_HTTPS")
                                    .AllowAnyHeader()
                                    .AllowAnyMethod();
                          });
    });

    // ... app configuration ...
    app.UseCors(MyAllowSpecificOrigins); // Добавьте это перед app.UseAuthorization();
    // ...
    ```
    Замените `ПОРТ_WASM_HTTP` и `ПОРТ_WASM_HTTPS` на порты из `Properties/launchSettings.json` проекта `ShoppingListApp.Client.Web`.

3.  Обновите базовый адрес API в `ShoppingListApp/src/ShoppingListApp.Client.Web/Program.cs`, если необходимо. Если сервер API на `http://localhost:ПОРТ_СЕРВЕРА`, а клиент на другом порту:
    ```csharp
    // В builder.Services.AddScoped(sp => { ... });
    // Замените:
    // var httpClient = new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
    // На:
    // var apiBaseAddress = builder.HostEnvironment.IsDevelopment() ? "http://localhost:ПОРТ_СЕРВЕРА" : navigationManager.BaseUri;
    // var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseAddress) };
    ```
    Если сервер и клиент на одном домене/порту (например, при публикации ASP.NET Core Hosted Blazor WASM), стандартной `BaseAddress = new Uri(navigationManager.BaseUri)` должно быть достаточно.

4.  Перейдите в директорию веб-клиента: `cd ShoppingListApp/src/ShoppingListApp.Client.Web`
5.  Запустите клиент: `dotnet run`
    Клиент будет доступен по адресу, указанному в выводе консоли (например, `http://localhost:ПОРТ_WASM_HTTP`).

## 5. API

### `POST /api/sync`

Эндпоинт для синхронизации данных.

**Request Body (`SyncRequestDto`):**

```json
{
  "updatedItems": [
    {
      "id": "guid",
      "name": "string",
      "categoryId": "guid",
      "storeId": "guid",
      "purchaseType": "Online" / "Offline", // Enum: 0 for Online, 1 for Offline
      "isRecurring": false,
      "isActive": true,
      "isArchived": false,
      "userListId": "guid"
    }
    // ... more items
  ],
  "deletedItemIds": [
    "guid",
    // ... more guids
  ],
  "lastSyncTimestamp": "datetime" // Client's last successful sync timestamp (UTC)
}
```

**Response Body (`SyncResponseDto`):**

```json
{
  "serverUpdatesListItems": [ /* Array of ListItem objects */ ],
  "serverUpdatesCategories": [ /* Array of Category objects */ ],
  "serverUpdatesStores": [ /* Array of Store objects */ ],
  "serverUpdatesUserLists": [ /* Array of UserList objects */ ],
  "confirmedDeletions": [ /* Array of Guids for items confirmed deleted by server */ ],
  "serverSyncTimestamp": "datetime", // Server's current timestamp after sync (UTC)
  "hasMoreData": false, // For future pagination
  "errorMessage": null // String or null
}
```

## Примечания

*   Проект MAUI (`ShoppingListApp.Client.Maui`) был создан с базовой структурой. Для полноценной сборки и запуска на конкретных платформах (особенно iOS и Android) может потребоваться дополнительная настройка среды разработки, установка специфичных для платформы SDK и инструментов.
*   Локальная база данных SQLite в Blazor WebAssembly (`ShoppingListApp.Client.Web`) по умолчанию использует "shared in-memory" режим (`Data Source=shopping_local_wasm_shared.db;Mode=Memory;Cache=Shared`). Это означает, что данные будут сохраняться, пока активна вкладка браузера (или приложение WASM). Для полной оффлайн-персистентности между сессиями потребуется интеграция с IndexedDB, что выходит за рамки текущей базовой реализации.
*   Логирование добавлено в ключевые компоненты и сервисы. Просматривайте вывод консоли (сервера, клиента, браузера) для отладки.
*   Обработка конфликтов синхронизации в `SyncController` и `SyncService` реализована по принципу "last write wins" (клиентские данные перезаписывают серверные при совпадении ID). В реальном приложении может потребоваться более сложная логика разрешения конфликтов.
*   **Важно для MAUI Android**: Для корректной работы HTTP-запросов на `http://10.0.2.2:ПОРТ` (локальный сервер с хост-машины) из Android эмулятора, убедитесь, что в `AndroidManifest.xml` (в `Platforms/Android/`) добавлено разрешение на использование cleartext traffic, если сервер работает по HTTP:
    ```xml
    <application android:usesCleartextTraffic="true" ...></application>
    ```
    Для HTTPS потребуется корректная настройка сертификата или обработка исключений для самоподписанных сертификатов в режиме разработки.
*   **HTTP/HTTPS порты**: В инструкциях по запуску указаны примерные порты (`ПОРТ_HTTP`, `ПОРТ_HTTPS`, `ПОРТ_СЕРВЕРА`, `ПОРТ_WASM_HTTP`, `ПОРТ_WASM_HTTPS`). Всегда сверяйтесь с актуальными портами из файлов `Properties/launchSettings.json` ваших проектов или с выводом `dotnet run`.
