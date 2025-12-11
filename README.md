🔧 Co potrzebujesz

.NET 8

SQL Server (np. LocalDB)

Przeglądarka

I to w sumie tyle.

⚙️ Jak to uruchomić (krótko)

W pliku appsettings.json wpisujesz poprawny connection string do bazy.

Odpalasz migracje:

Terminal:

dotnet ef database update


albo Visual Studio → Package Manager Console:

Update-Database


Potem tylko:

dotnet run


W przeglądarce wchodzisz na adres, który wyświetli się w konsoli (np. https://localhost:12345
).

I działa.

👥 Role – kto co może
Zwykły użytkownik

robi rezerwację

widzi swoje rezerwacje

może anulować przyszłe

Manager

widzi wszystkie rezerwacje

akceptuje, odrzuca, usuwa

ma osobny panel ze szczegółami użytkowników

🧪 Konta testowe (proponowane)

Rejestrujesz dwa konta:

user@test.local

manager@test.local

Do drugiego dodajesz rolę Manager (SQL-em lub ręcznie w DB).

I już masz komplet.

🔄 Jak wygląda praca w systemie
🧑 Użytkownik

wchodzi → Reservations/Create

wybiera salę, daty → wysyła → status Pending

👨‍💼 Manager

loguje się

widzi baner: „Masz oczekujące rezerwacje”

wchodzi do Reservations/Index

akceptuje / odrzuca

📅 Kalendarz / sala

Rooms/Details/{id} → widok miesiąca

Reservations/Calendar?roomId= → widok tygodnia lub miesiąca

📎 Załączniki

W szczegółach rezerwacji możesz wrzucać PDF, DOC, XLSX, PNG itd.
Do 20 MB.

🎨 Wygląd

bootstrap 5

schludne tabele

małe kolorowe badge do statusów

ładne logowanie

🛠️ Gdy coś nie działa

migracje nie wchodzą? → sprawdź connection string i dotnet ef database update

CSS nie wchodzi? → Ctrl + F5

ikony nie działają? → sprawdź CDN Bootstrap Icons
