# Haotični Kupidon — WCF PubSub

Kolokvijum 2, SNUS 2026. WCF Publish-Subscribe aplikacija koja simulira haotičnog kupidona koji šalje ljubavna pisma prijavljenim osobama.

---

## Struktura projekta

```
ChaosKupidon/
├── Server/
│   ├── Contracts.cs          ← WCF ugovori (interfejsi i data contracti)
│   ├── KupidonService.cs     ← Implementacija servisa
│   └── Program.cs            ← Pokretanje ServiceHost-a
├── PersonClient/
│   ├── ServiceContracts.cs   ← Kopija ugovora na strani klijenta
│   └── Program.cs            ← Prijava osobe, primanje pisama
└── CupidClient/
    └── Program.cs            ← Kupidon koji šalje pisma svakog minuta
```

---

## Arhitektura

Koristi se **WCF Duplex** (PubSub) pattern:

- `IPersonService` — duplex ugovor (`WSDualHttpBinding`), osobe se prijavljuju i primaju pisma putem **callback-a**
- `ICupidService` — jednosmerni ugovor (`BasicHttpBinding`), kupidon poziva `SendLetters()`
- `IPersonCallback` — callback interfejs koji **server poziva** kada treba da dostavi pismo osobi

```
PersonClient ──────────► Server (IPersonService)
             ◄────────── Server poziva callback (ReceiveLetter)

CupidClient  ──────────► Server (ICupidService.SendLetters)
                              └──► Server poziva callback svakog primaoca
```

---

## Pokretanje u Visual Studio

> ⚠️ **Visual Studio mora biti pokrenut kao Administrator** (desni klik → Run as administrator), jer WCF `ServiceHost` zahteva privilegije za registraciju HTTP porta.


### 1. Reference 

Za svaki projekat: desni klik → **Add → Reference → Assemblies**, pa čekiraj:

| Projekat | Reference |
|----------|-----------|
| Server | `System.ServiceModel`, `System.Runtime.Serialization`, `System.ServiceModel.Web` |
| PersonClient | `System.ServiceModel`, `System.Runtime.Serialization` |
| CupidClient | `System.ServiceModel`, `System.Runtime.Serialization` |

### 2. Multiple Startup Projects

Desni klik na **Solution → Properties → Startup Project → Multiple startup projects**:

| Projekat | Action |
|----------|--------|
| Server | Start |
| CupidClient | Start |
| PersonClient | Start |

### 3. Pokretanje

Pritisni **Ctrl+F5** — otвориће se 3 konzole. Server mora biti spreman pre nego što klijenti pokušaju da se povežu.

Za testiranje prijavi **minimum 2 osobe** — pokreni još jednu instancu PersonClient-a:
desni klik na `PersonClient` → **Debug → Start New Instance**

---

## Tok programa

1. Osoba unosi podatke: `username`, `grad`, `godine`, `telefon` — sa validacijom (prazno polje, slova umesto broja, negativan broj)
2. Kupidon svakih **60 sekundi** poziva `SendLetters()` (i odmah na početku)
3. Server za svakog primaoca računa **score** za svakog potencijalnog pošiljaoca:
   - Isti grad: **+30 poena**
   - Slične godine (±2): **+20 poena**
   - Nasumični faktor 0–100 (`RNGCryptoServiceProvider`): **+0–100 poena**
4. Osobi sa **najvećim score-om** se šalje pismo putem WCF Duplex callback-a
5. Na konzoli primaoca se prikazuju detalji pošiljaoca i nasumična poruka:
   - `"Radujem se našem susretu!"` → telefon se prikazuje
   - `"Zelim da se upoznamo."` → telefon se prikazuje
   - `"Nisam zainteresovan/a za upoznavanje."` → **telefon se NE prikazuje**
6. Primalac mora pritisnuti **Enter** da potvrdi prijem pre nego što može dobiti sledeće pismo
7. Kada osoba izađe (Ctrl+C ili X), automatski se odjavljuje sa servera

---

## Komande u PersonClient

| Komanda | Opis |
|---------|------|
| `/block username` | Blokira korisnika — više nećeš dobijati pisma od njega |
| `Enter` | Potvrđuje prijem pisma |
| `Ctrl+C` | Odjavljuje se sa servera i zatvara aplikaciju |

## Komande u CupidClient

| Komanda | Opis |
|---------|------|
| `l` | Lista svih trenutno prijavljenih osoba |
| `Ctrl+C` | Zatvara kupidon klijent |

---

## Napomene

- Blokiranje je **jednosmerno** — `/block marko` znači da ti ne možeš dobiti pismo od Marka, ali Marko i dalje može dobiti pismo od tebe
- Osoba koja čeka potvrdu pisma (`WaitingForAck = true`) **ne prima nova pisma**, ali i dalje može biti pošiljalac drugima
- Ako osoba izgubi konekciju bez odjave, server je detektuje i uklanja pri sledećem slanju pisama
- `RNGCryptoServiceProvider` je deprecated u novijim verzijama .NET-a ali se koristi jer to zahteva specifikacija