C# / .NET 10 (WPF), сторонних пакетов нет, только BCL и WinAPI

Сборка и запуск
Нужен Windows и .NET 10 SDK.

dotnet build TestZ.slnx
dotnet run --project TestZ.Server : терминал 1 — сервер
dotnet run --project TestZ.Client : терминал 2 — клиент

Настройки
Обычные константы в исходниках, после правки необходима пересборка проекта.

TestZ.Client/Settings.cs:

ServerHost — IP сервера. 127.0.0.1 для теста на одной машине, для двух — IP машины с сервером 
ServerPort — порт, 4545.

TestZ.Server/Settings.cs:

Port — порт сервера, 4545, должен совпадать с клиентским.
HeartbeatIntervalSec — интервал проверки клиента, 5.
OfflineAfterMissedBeats — сколько пропущенных пульсов = статус Offline, 3.
AwayAfterIdleSec — секунд без ввода до статуса Away, 300.
ScreenshotTimeoutSec — таймаут ожидания скриншота, 20.

Автозапуск клиента при логине
Прописывается в HKCU...\Run, без прав администратора:

TestZ\TestZ.Client\bin\Debug\net10.0-windows\TestZ.Client.exe --install
TestZ\TestZ.Client\bin\Debug\net10.0-windows\TestZ.Client.exe --uninstall
для постановки/отмены автозапуска
