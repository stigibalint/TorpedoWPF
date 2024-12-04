Battleship Játék - Kliens-Szerver Implementáció
Ez a projekt egy klasszikus Battleship (tengerészeti csata) játék kliens-szerver alapú implementációja. A szerver kezeli a játék logikáját és a két játékos közötti kommunikációt, míg a kliens lehetővé teszi a felhasználók számára, hogy interakcióba lépjenek a játékkal, beállítsák a hajóikat és játsszanak a szerver által irányított játékban.

Tartalomjegyzék
Bevezetés
Projekt Célja
Rendszerkövetelmények
A Játék Működése
Kliens Funkciók
Szerver Funkciók
Telepítés és Használat
Játék Leírás
Fejlesztés és Bővítés
Licenc
Bevezetés
A Battleship játék egy kétfős stratégiai játék, amelyben a játékosok hajókat helyeznek el egy rácsos térképen, és próbálják eltalálni ellenfelük hajóit. A játék célja, hogy az egyik játékos előbb elsüllyessze az összes ellenfele hajóját. Ez a projekt a kliens-szerver architektúra használatával valósítja meg a játékot, ahol a szerver felelős a játékmenet irányításáért, a két játékos közötti kommunikációért és a játékszabályok betartatásáért, míg a kliens biztosítja a felhasználói felületet és lehetőséget ad a játékosoknak a játékhoz való interakcióra.

Projekt Célja
A projekt célja, hogy megvalósítsa a Battleship játékot egy kliens-szerver környezetben, amely lehetővé teszi, hogy két játékos egymás ellen játszhasson valós időben. A szerver kezeli a játék logikáját, a játékosok hajóinak elhelyezését, a lövéseket, és ellenőrzi, hogy ki nyert. A kliens pedig biztosítja a játékosok számára az interaktív felületet, ahol hajókat helyezhetnek el, lövéseket adhatnak le, és nyomon követhetik a játék állapotát.

Rendszerkövetelmények
A projekt futtatásához az alábbi követelmények szükségesek:

Kliens:
.NET Framework (WPF alkalmazás, 4.7-es vagy újabb verzió)
Visual Studio vagy bármilyen kompatibilis C# fejlesztői környezet
Internet kapcsolat a szerverhez való csatlakozáshoz (helyi hálózaton vagy távoli IP-n keresztül)
Szerver:
.NET Framework (Konzolos alkalmazás, 4.7-es vagy újabb verzió)
Visual Studio vagy bármilyen kompatibilis C# fejlesztői környezet
A Játék Működése
A játék két részből áll: a kliensből és a szerverből.

Szerver oldali működés:

A szerver fogadja a két játékos csatlakozását, és irányítja a játékmenetet.
Kezeli a hajók elhelyezését, nyomon követi azokat, és kezeli a lövéseket.
Ha egy játékos eltalálja az ellenfél hajóját, értesíti mindkét játékost.
A játék körökre osztott, azaz a játékosok felváltva adhatnak le lövéseket.
Amikor az egyik játékos elsüllyeszti az összes hajót, a játék véget ér, és a szerver értesíti a játékosokat a győzelemről.
Kliens oldali működés:

A kliens biztosítja a felhasználói felületet, amelyen keresztül a játékosok hajókat helyezhetnek el és küldhetnek lövéseket.
A játékosok láthatják a saját és az ellenfél tábláját, valamint a lövések eredményeit.
A kliens értesíti a játékost, hogy mikor van a soron, és milyen akciót kell végrehajtania.
Kliens Funkciók
Hajó elhelyezése: A játékosok a vizuális felületen húzhatják a hajókat, és helyezhetik el őket a táblán.
Körökre osztott játékmenet: A kliens kijelzi, mikor jön el a játékos sora, és mikor kell megtenniük a következő lövéseiket.
Játékállapot kijelzése: A kliens mutatja a jelenlegi állapotot, például "A te sorod" vagy "Ellenfél sora".
Hajó elhelyezési validálás: A rendszer ellenőrzi, hogy a hajók nem lógnak ki a tábláról, és nem fedik le egymást.
Játék vége: A játék végén a kliens értesíti a játékosokat, hogy nyertek vagy vesztettek.
Szerver Funkciók
Játékosok kezelése: A szerver kezeli a két játékos csatlakozását és az ő állapotukat.
Hajópozíciók kezelése: A szerver nyilvántartja mindkét játékos hajóinak helyét és a hajók állapotát (el vannak találva vagy sem).
Körökre osztott logika: A szerver biztosítja, hogy a játékosok felváltva lőjenek, és ellenőrzi, hogy az aktuális játékos megfelelő időben próbál-e támadni.
Lövés validálása: A szerver ellenőrzi a lövéseket, és visszajelzést ad a találatokról vagy hibákról.
Játék vége és újrakezdés lehetősége: A játék végén a szerver értesíti a játékosokat az eredményről, és lehetőséget biztosít a játék újrakezdésére.
Telepítés és Használat
A projekt klónozása:

Klónozd le a repozitóriumot a következő parancs segítségével:

bash
Kód másolása
git clone https://github.com/your-repository/battleship.git
A megoldás megnyitása:

Nyisd meg a Battleship.sln fájlt a Visual Studio-ban.
A projekt lefordítása:

A Visual Studio-ban építsd le mindkét projektet (kliens és szerver).
A szerver indítása:

A BattleshipServer projektben nyomd meg az F5-t vagy kattints a Start gombra a szerver indításához. A szerver az 5000-es porton fog várakozni.
A kliens indítása:

A BattleshipClient projektben nyomd meg az F5-t a kliens elindításához. A kliens csatlakozni fog a szerverhez (győződj meg róla, hogy a szerver már fut).
Játék Leírás
Játék indítása:

Először indítsd el a szervert, és várd meg, amíg mindkét játékos csatlakozik.
Miután mindkét játékos csatlakozott, mindkét játékos hajókat helyez el, majd kattintanak a "Kész" gombra, hogy elinduljon a játék.
Hajó elhelyezése:

A játékosoknak hajókat kell elhelyezniük a saját táblájukon. A hajóknak nem szabad egymásra kerülniük, és nem mehetnek ki a tábláról.
Körökre osztott játékmenet:

A játékosok felváltva adhatnak le lövéseket az ellenfél tábláján.
A szerver ellenőrzi, hogy a lövés eltalált-e egy hajót, és értesíti a játékosokat.
Játék vége:

Amikor az egyik játékos elsüllyeszti az összes ellenfél hajóját, a játék véget ér, és a szerver értesíti mindkét játékost.
Újrakezdés:

A játék végén a játékosok kérhetnek újrakezdést. Ha mindkét játékos egyetért, a játék újraindul.
Fejlesztés és Bővítés
Bármely fejlesztő szabadon módosíthatja a kódot, hozzáadhat új funkciókat, vagy javíthatja a meglévőket. A projekthez pull request-eket lehet benyújtani a GitHub repozitóriumban.

Licenc
A projekt az MIT Licenc alatt érhető el. A licenc részleteit a LICENSE fájlban találhatók.

Ez a projekt egy alapvető implementációja a Battleship játéknak kliens-szerver architektúrában, és remek alapot adhat további fejlesztésekhez és bővítésekhez.
