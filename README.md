# Rezarwacja Sal – instrukcja uruchomienia i scenariusze

## Wymagania
- .NET SDK 8.0+
- SQL Server (np. LocalDB) lub inny serwer zgodny z ADO.NET
- Przeglądarka (Chrome/Edge/Firefox)

## Konfiguracja
1) Skonfiguruj połączenie z bazą danych w `appsettings.json` pod kluczem `ConnectionStrings:DefaultConnection`.
2) Zastosuj migracje bazy (jedna z metod):
   - Terminal (w katalogu projektu):
     ```bash
     dotnet tool update --global dotnet-ef
     dotnet ef database update
     ```
   - Lub Visual Studio → Package Manager Console:
     ```powershell
     Update-Database
     ```

## Uruchomienie
- Terminal:
  ```bash
  dotnet build
  dotnet run --project "Rezarwacja Sal.csproj"
  ```
- Aplikacja wystartuje pod adresem pokazanym w konsoli (np. `https://localhost:PORT/`).

## Role i uprawnienia
- **Użytkownik**
  - Tworzenie rezerwacji (`Reservations/Create`).
  - Podgląd i zarządzanie własnymi rezerwacjami (`Reservations/My`, anulowanie aktywnych/przyszłych).
- **Manager**
  - Widok wszystkich rezerwacji (`Reservations/Index`) z akcjami: akceptuj, odrzuć, usuń.
  - Szczegóły użytkownika z podsumowaniem liczby rezerwacji (`Reservations/UserDetails/{userId}`).
  - Po zalogowaniu przekierowanie na stronę główną, gdzie widoczny jest baner o oczekujących rezerwacjach.

## Konta testowe (propozycja)
Aplikacja nie zawiera domyślnych kont. Sugerowany sposób przygotowania środowiska testowego:
1) Zarejestruj dwa konta przez stronę `Account/Register`:
   - `user@test.local` – zwykły użytkownik.
   - `manager@test.local` – będzie przypisany do roli Manager.
2) Dodaj rolę `Manager` i przypisz do konta menedżera (SQL przykład dla Identity):
   - Znajdź wartości `UserId` (z tabeli `AspNetUsers`).
   - Wstaw rolę i powiązanie:
     ```sql
     -- 1) Dodaj rolę Manager (jeśli nie istnieje)
     INSERT INTO AspNetRoles (Id, [Name], NormalizedName)
     SELECT NEWID(), 'Manager', 'MANAGER'
     WHERE NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'MANAGER');

     -- 2) Pobierz Id roli Manager i Id użytkownika managera
     DECLARE @RoleId nvarchar(450) = (SELECT TOP 1 Id FROM AspNetRoles WHERE NormalizedName = 'MANAGER');
     DECLARE @UserId nvarchar(450) = (SELECT TOP 1 Id FROM AspNetUsers WHERE NormalizedEmail = 'MANAGER@TEST.LOCAL');

     -- 3) Przypisz rolę, jeśli brak wpisu
     IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
     INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);
     ```
   - Po przypisaniu roli, zaloguj się jako `manager@test.local`.

> Uwaga: Jeśli korzystasz z innej nazwy/konwencji emaili w bazie, zmień zapytanie do `NormalizedEmail` zgodnie z rzeczywistą wartością.

## Podstawowe scenariusze
- **Utworzenie rezerwacji (Użytkownik)**
  1) Wejdź w `Reservations/Create`.
  2) Wybierz salę, ustaw daty (walidacja: koniec po początku), podaj tytuł i opcjonalne notatki/załącznik.
  3) Złóż prośbę – rezerwacja trafia do statusu `Pending`.

- **Zatwierdzanie / odrzucanie (Manager)**
  1) Zaloguj się jako Manager – po zalogowaniu trafisz na stronę główną.
  2) Jeśli istnieją oczekujące rezerwacje, pojawi się baner przypomnienia na Home.
  3) Przejdź do `Reservations/Index`, filtruj i użyj akcji Akceptuj/Odrzuć/Usuń.

- **Moje rezerwacje (Użytkownik)**
  1) `Reservations/My` – lista z możliwością anulowania przyszłych, oczekujących lub zatwierdzonych rezerwacji.

- **Kalendarz i szczegóły sali**
  - `Rooms/Details/{id}` – miesięczny przegląd z podświetleniem „dzisiaj” i legendą kolorów.
  - `Reservations/Calendar?roomId={id}` – widok tygodniowy/miesięczny rezerwacji wybranej sali.

- **Załączniki**
  - W szczegółach rezerwacji (`Reservations/Details/{id}`) można dodać i pobrać załączniki (PDF/DOC/XLSX/PPTX/PNG/JPG/TXT, max 20 MB).

## UX/UI
- Bootstrap 5, tabele `table-sm` z przyklejonym nagłówkiem (`table-sticky`), przyciski z ikonami.
- Spójne oznaczenia statusów przez partial `Views/Shared/_StatusBadge.cshtml`.
- Strona logowania w układzie karty (Bootstrap 5) z polskim UI.

## Rozwiązywanie problemów
- Jeśli nie widać ikon, sprawdź dostęp do CDN Bootstrap Icons w `_Layout.cshtml` lub zamień na lokalne.
- W przypadku błędów migracji przejrzyj `appsettings.json` (connection string) i uruchom `dotnet ef database update`.
- Odśwież przeglądarkę z pominięciem cache (Ctrl+F5), gdy zmienisz CSS/układ.
