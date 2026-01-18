# DemoApp (C# .NET 8, Blazor WASM + Web API)

Демонстрационное приложение с раздельным фронтендом и бэкендом:

- Backend: ASP.NET Core Web API (.NET 8) + SignalR
- Frontend: Blazor WebAssembly (.NET 8)
- Взаимодействие: HTTP-запросы + ответы через SignalR в реальном времени

## Требования

- .NET SDK 8.0+
- Браузер с поддержкой WebAssembly и HTTPS localhost-сертификатов

Структура решения:

- Backend: src/DemoApp.Backend
- Frontend: src/DemoApp.Frontend

## Запуск бэкенда

Из корня репозитория:

```bash
cd /home/hell/Work/Github/helldweller/test-csharp

dotnet restore

# Запуск backend (HTTPS: https://localhost:5003, HTTP: http://localhost:5002)
dotnet run --project src/DemoApp.Backend/DemoApp.Backend.csproj
```

После запуска:

- API: https://localhost:5003/api/messages
- SignalR hub: https://localhost:5003/hubs/messages
- Swagger/OpenAPI UI: https://localhost:5003/swagger

Swagger включён в среде Development (см. src/DemoApp.Backend/Properties/launchSettings.json).

## Запуск фронтенда

В отдельном терминале, также из корня:

```bash
cd /home/hell/Work/Github/helldweller/test-csharp

# Запуск Blazor WebAssembly фронтенда (Dev server)
dotnet run --project src/DemoApp.Frontend/DemoApp.Frontend.csproj
```

Открой в браузере:

- Фронтенд UI: https://localhost:5001

Фронтенд настроен на работу с бэкендом по адресу https://localhost:5003/ (см. src/DemoApp.Frontend/Program.cs).

## Как это работает

1. Пользователь вводит текст в поле и нажимает «Отправить» (или Enter).
2. Blazor-страница отправляет HTTP POST-запрос на api/messages бэкенда.
3. Бэкенд добавляет текущее время, формирует строку и рассылает её всем подключённым клиентам через SignalR hub /hubs/messages.
4. Фронтенд подписан на событие ReceiveMessage и, получая ответ, добавляет его в историю сообщений и обновляет UI (StateHasChanged).
5. Ошибки соединения (HTTP / SignalR) отображаются пользователю в виде сообщения об ошибке.

## Обработка ошибок и ретраи

- На фронтенде реализованы повторные попытки HTTP-запроса (несколько попыток с задержкой).
- При недоступности сервера / обрыве SignalR показывается человекопонятное сообщение об ошибке.
- На бэкенде включено логирование запросов и событий (Console, HttpLogging).

## Масштабирование и расширение

- SignalR вынесен в отдельный hub (MessageHub), что позволяет:
	- Добавлять группы, таргетированные сообщения, дополнительные события.
	- В дальнейшем подключить backplane (Redis, Azure SignalR и т.п.) для масштабирования.
- API вынесен в контроллер (MessagesController), легко добавлять новые методы (новые маршруты и DTO).

Для разработки можно менять порты и CORS-настройки в:

- Backend: src/DemoApp.Backend/Program.cs (CORS + URLы)
- Frontend: src/DemoApp.Frontend/Program.cs (базовый адрес бэкенда)
