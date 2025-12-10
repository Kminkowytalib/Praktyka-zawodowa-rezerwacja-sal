Rezerwacja Sal – szybka instrukcja
Wymagania
.NET SDK 8.0+
SQL Server (np. LocalDB)
Przeglądarka (Chrome/Edge/Firefox)
Konfiguracja
W pliku appsettings.json ustaw ConnectionStrings:DefaultConnection.
Zastosuj migracje bazy:
dotnet tool update --global dotnet-ef
dotnet ef database update
lub w Visual Studio:
Update-Database
Uruchomienie
dotnet build
dotnet run --project "Rezarwacja Sal.csproj"
Aplikacja uruchomi się pod adresem podanym w konsoli.
Role
Użytkownik:
Tworzy rezerwacje (Reservations/Create)
Zarządza swoimi (Reservations/My)
Manager:
Widzi wszystkie rezerwacje (Reservations/Index)
Może je zatwierdzać, odrzucać i usuwać
Widzi alerty o oczekujących rezerwacjach po zalogowaniu
Konta testowe
Zarejestruj:
user@test.local
manager@test.local
Aby nadać rolę Manager (SQL):
INSERT INTO AspNetRoles (Id, Name, NormalizedName)
SELECT NEWID(), 'Manager', 'MANAGER'
WHERE NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'MANAGER');
DECLARE @RoleId nvarchar(450) = (SELECT Id FROM AspNetRoles WHERE NormalizedName='MANAGER');
DECLARE @UserId nvarchar(450) = (SELECT Id FROM AspNetUsers WHERE NormalizedEmail='MANAGER@TEST.LOCAL');
IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId=@UserId AND RoleId=@RoleId)
INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);
Scenariusze
Nowa rezerwacja → Reservations/Create
Akceptacja/Odrzucenie → Reservations/Index
Moje rezerwacje → Reservations/My
Kalendarz sali → Rooms/Details/{id} lub Reservations/Calendar?roomId={id}
Załączniki
Obsługiwane: PDF, DOC, XLSX, PPTX, PNG, JPG, TXT (do 20 MB).
Problemy
Brak ikon → sprawdź Bootstrap Icons w _Layout.cshtml
Błędy bazy → sprawdź connection string i uruchom migracje
Zmiany CSS → odśwież stronę Ctrl + F5
