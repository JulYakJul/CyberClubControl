# Система автоматизации управления компьютерным клубом

## Описание
Микросервисное приложение на C# .NET для автоматизации работы компьютерных клубов. Система обеспечивает полный контроль над клиентскими ПК, автоматизацию бизнес-процессов и поддержку пользователей (клиентов и сотрудников) с помощью AI-ассистента. Система создана с прохождением всех стадий жизненного цикла: анализ требований, проектирование архитектуры, реализация, тестирование и внедрение

## Ключевые особенности
🔹 **Удаленное управление сетью клиентских компьютеров через WebSockets**  
🔹 **Встроенный AI-ассистент на базе Ollama с моделью Gemma2**  
🔹 **Запрет на использование служебных комбинаций клавиш во время игровой сессии на клиентском ПК**  
🔹 **Автоматизированная отчетность и аналитика продаж**  
🔹 **Мультиплатформенная система оплаты (карты, наличные, YooMoney)**  
🔹 **Реализована система ролевой модели доступа (RBAC) для разграничения прав сотрудников**  

## Технологический стек

| Категория            | Технологии                                                                 |
|----------------------|---------------------------------------------------------------------------|
| **Язык программирования** | C#, .NET 8                                                      |
| **База данных**       | PostgreSQL 16                                  |
| **DAL**       | Entity Framework Core                                   |
| **Тестирование**      | xUnit, Moq                                                     |
| **ИИ-компоненты**     | Ollama, Gemma2                                               |
| **Коммуникация**      | WebSocket (SignalR), REST API                                           |
| **Платежи**          | YooMoney API                                                            |
| **Фронтенд**       | Razor Pages, HTML, CSS, JavaScript, jQuery, FullCalendar |
| **Документация**     | Swagger                                       |
| **Контроль версий**   | Git                                                          |
| **Контейнеризация**   | Docker                                                  |
| **Логирование**       | Serilog                          |
| **Безопасность**      | ASP.NET Core Identity, Cookie Auth, Role-Based Auth, CORS, HTTPS    |
| **Работа с документами** | EPPlus (Excel), iTextSharp (PDF), DocumentFormat.OpenXml (Office) |
| **Фоновые задачи**   | IHostedService (SessionCleanupService)  |

## Функционал системы
- **Управление данными:**
  - Товары  
  - Клиенты  
  - Игровые сессии
  - Сотрудники   
  - Компьютеры
  - Тарифы
  - График дежурств
  - Продажи 
- **Механизм удаленной блокировки ПК (Websocket).**  
- **AI ассистент для клиентов и работников клуба.**  
- **Формирование ежемесячной отчетности по продажам и зарплатам сострудников.**  
- **Оплата наличными/картой (YooKassa).**  

# Фрагменты интерфейса

## Страница входа
![Authentication](https://github.com/JulYakJul/CyberClubControl/raw/main/GitPictures/Authentication.png)

## Страница регистрации
![Registration](https://github.com/JulYakJul/CyberClubControl/raw/main/GitPictures/Registration.png)

## Страница графика дежурств
![Duty Scheduler](https://github.com/JulYakJul/CyberClubControl/raw/main/GitPictures/Duty_scheduler.png)

## Страница игровых сессий
![Sessions](https://github.com/JulYakJul/CyberClubControl/raw/main/GitPictures/Sessions.png)

## Страница тарифов
![Tariffs](https://github.com/JulYakJul/CyberClubControl/raw/main/GitPictures/Tariffs.png)

## Оплата
![YooMoney](https://github.com/JulYakJul/CyberClubControl/raw/main/GitPictures/YooMoney.png)

## Сгенерированный отчет XLSX
![Report](https://github.com/JulYakJul/CyberClubControl/raw/main/GitPictures/Report.png)

## Клиентское приложение. Устанавливается на клиентские ПК в клубе для удалённой блокировки и разблокировки компьютера, слежения состояния ПК, а также поддержки клиентов с помощью ИИ
![Utility](https://github.com/JulYakJul/CyberClubControl/raw/main/GitPictures/Utility.png)

## ИИ-агент для клиентов
![AI Agent](https://github.com/JulYakJul/CyberClubControl/raw/main/GitPictures/AIAgent.png)

# Use-Case диаграммы
## Роль Управляющий
![Manager Use-case Diagram](https://github.com/JulYakJul/CyberClubControl/raw/main/GitPictures/Manager%20Use-case%20diagram.png)

## Роль Администратор
![Admin Use-case Diagram](https://github.com/JulYakJul/CyberClubControl/raw/main/GitPictures/Admin%20Use-case%20diagram.png)

## Роль Клиент
![Client Use-case Diagram](https://github.com/JulYakJul/CyberClubControl/raw/main/GitPictures/Client%20Use-case%20diagram.png)

## Результаты внедрения системы

| Показатель                          |  Эффект                          |
|-------------------------------------|---------------------------------|
| **Точность ответов AI-ассистента**  | Снижение нагрузки на персонал   |
| **Снижение времени обслуживания**   | Экономия 120 ч/мес администратора (34% от месяца работы) |
| **Снижение времени управления**     | Оптимизация работы управляющего (78% от месяца работы)|
