# Exposé Creator – Lizenz-Generator (intern)

Internes Windows-Tool zum Ausstellen von Lizenzen für [Exposé Creator](https://github.com/eXPressoHD/exposecreator).
**Nicht an Kunden weitergeben.**

## Starten

`LicenseGenerator.csproj` in Visual Studio 2022 öffnen und starten (.NET 8, Windows Forms).

## Was es tut

1. **Schlüsselpaar erzeugen** (einmalig): Der private Schlüssel landet in `Dokumente\ExposeCreator-Lizenzierung\private-key.pem`, der öffentliche Schlüssel in der Zwischenablage. Ihn in der App in `Services/LicensePublicKey.cs` eintragen.
2. **Lizenz ausstellen:** Name, E-Mail, Installations-ID (vom Kunden) und Bestellnummer eingeben. Heraus kommt ein signierter Schlüssel (`EXC1.…`) bzw. eine `.lic`-Datei. Jede Lizenz wird in `ausgestellte-lizenzen.csv` protokolliert.

## Sicherheit

- `private-key.pem` **sichern** (USB-Stick, Passwortmanager) und **niemals** einchecken oder weitergeben. Ohne die Datei lassen sich keine passenden Lizenzen mehr ausstellen; in fremden Händen kann jemand anderes Lizenzen erzeugen.
- Nicht erneut „Schlüsselpaar erzeugen“ klicken, sonst passen bisherige Lizenzen nicht mehr zur App.

## Kompatibilität mit der App

Das Lizenzformat (`EXC1.<payload base64url>.<signature base64url>`, ECDSA P-256 / SHA-256, Felder `n e m t i o`) und die Normalisierung der Installations-ID müssen mit `Services/LicenseService.cs` in der App übereinstimmen. **Bei Änderungen immer beide Repos zusammen anpassen.**
