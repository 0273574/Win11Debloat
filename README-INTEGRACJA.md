# Win11Debloat - Integracja Wdrozyciel II Wielki

## 📌 Spis Treści

1. [Przegląd](#przegląd)
2. [Nowe Funkcjonalności](#nowe-funkcjonalności)
3. [Instalacja](#instalacja)
4. [Użytkowanie](#użytkowanie)
5. [Konfiguracja](#konfiguracja)
6. [Architektura](#architektura)

---

## 🎯 Przegląd

**Win11Debloat Enhanced** łączy możliwości oryginalnego Win11Debloat z zaawansowanym systemem wdrażania z projektu **Wdrozyciel II Wielki**. Pozwala to na profesjonalne zarządzanie konfiguracją systemu i wdrażaniem aplikacji poprzez profile zdefiniowane w JSON.

### Główne Cechy

- ✅ **Profile Wdrażania** - Gotowe szablony dla różnych zastosowań (Minimal, Standard, Full)
- ✅ **Integracja C#** - Zaawansowane komponenty zarządzające konfiguracją
- ✅ **System Logowania** - Szczegółowe śledzenie wszystkich operacji
- ✅ **Walidacja Konfiguracji** - Weryfikacja poprawności przed wdrożeniem
- ✅ **Analiza Systemu** - Automatyczne zbieranie informacji o systemie
- ✅ **Tryb Symulacji** - Testowanie zmian bez ich stosowania

---

## 🆕 Nowe Funkcjonalności

### 1. Manager Wdrażania (Deployment Manager)

Zarządzanie zdefiniowanymi profilami wdrażania:

```powershell
# Uruchomienie profilu Standardowego
.\Scripts\Deployer\DeploymentManager.ps1 -ProfileName "Standard"

# Tryb symulacji (bez zmian)
.\Scripts\Deployer\DeploymentManager.ps1 -ProfileName "Minimal" -DryRun
```

### 2. Analizator Systemu (System Analyzer)

Szybka analiza konfiguracji sprzętu i oprogramowania:

```csharp
var analyzer = new SystemAnalyzer();
var analysis = await analyzer.AnalyzeSystem();

Console.WriteLine($"OS: {analysis.OSInfo.VersionString}");
Console.WriteLine($"RAM: {analysis.HardwareInfo.TotalMemoryGB} GB");
Console.WriteLine($"Aplikacje: {analysis.InstalledApplications.Count}");
```

### 3. Walidator Konfiguracji (Config Validator)

Weryfikacja profili przed wdrożeniem:

```powershell
$profile = Get-DeploymentProfile -Name "Standard"
$validation = Test-DeploymentProfileValidity -Profile $profile

if ($validation.IsValid) {
    "Profil jest poprawny - można wdrażać"
} else {
    $validation.Errors | ForEach-Object { "Błąd: $_" }
}
```

### 4. Manager Konfiguracji (Config Manager)

Zarządzanie plikami JSON konfiguracyjnymi:

```csharp
var configManager = new ConfigManager();
var profiles = configManager.LoadConfiguration<List<Profile>>("DeploymentProfiles.json");
var isValid = configManager.ValidateConfiguration("DeploymentProfiles.json", profiles);
```

### 5. System Logowania (Deployment Logging)

Szczegółowe rejestrowanie operacji:

```powershell
$logger = [DeploymentLogger]::new($LogPath)
$logger.LogInfo("Rozpoczęcie wdrażania")
$logger.LogWarning("Ostrzeżenie operacyjne")
$logger.LogError("Błąd wdrażania")
```

---

## 📥 Instalacja

### Wymagania

- Windows 10/11
- PowerShell 5.1 lub nowszy
- .NET 6.0 Runtime (dla komponentów C#)
- Administrator

### Kroki Instalacji

1. **Klonuj repozytorium**

```bash
git clone https://github.com/0273574/Win11Debloat.git
cd Win11Debloat
git checkout development
```

2. **Uprawnienia wykonania**

```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
```

3. **Budowanie komponentów C# (opcjonalnie)**

```bash
cd CSharp/WdrozycielIntegration
dotnet build -c Release
```

---

## 🚀 Użytkowanie

### Uruchomienie Manager Wdrażania

```powershell
cd C:\ścieżka\do\Win11Debloat

# Profil Minimal (13 aplikacji, szybki)
.\Scripts\Deployer\DeploymentManager.ps1 -ProfileName "Minimal" -Verbose

# Profil Standard (5+ aplikacji, zbalansowany)
.\Scripts\Deployer\DeploymentManager.ps1 -ProfileName "Standard" -Verbose

# Profil Full (8+ aplikacji, dla developerów)
.\Scripts\Deployer\DeploymentManager.ps1 -ProfileName "Full" -Verbose

# Testowanie bez zmian
.\Scripts\Deployer\DeploymentManager.ps1 -ProfileName "Standard" -DryRun
```

### Uruchomienie Analizatora Systemu

```csharp
using WdrozycielIntegration;

var analyzer = new SystemAnalyzer();
var systemInfo = await analyzer.AnalyzeSystem();

Console.WriteLine($"Komputer: {systemInfo.SystemSettings[\"ComputerName\"]}");
Console.WriteLine($"Użytkownik: {systemInfo.SystemSettings[\"UserName\"]}");
Console.WriteLine($"Wersja OS: {systemInfo.OSInfo.VersionString}");
Console.WriteLine($"Pamięć RAM: {systemInfo.HardwareInfo.AvailableMemoryGB} GB dostępna");
```

---

## ⚙️ Konfiguracja

### Struktura Pliku DeploymentProfiles.json

```json
[
  {
    "name": "Niestandardowy",
    "description": "Moja konfiguracja",
    "priority": 1,
    "systemSettings": {
      "telemetryEnabled": false,
      "darkModeEnabled": true,
      "updatesBehavior": "deferred"
    },
    "applications": [
      {
        "name": "7-Zip",
        "version": "latest",
        "required": true
      }
    ]
  }
]
```

### Zmienne DeployerConfig.json

```json
{
  "version": "2.1.0",
  "integrationEnabled": true,
  "deployment": {
    "dryRun": false,
    "createRestorePoint": true,
    "backupCurrentSettings": true,
    "rollbackOnError": true
  },
  "validation": {
    "validateProfileBeforeExecution": true
  }
}
```

---

## 🏗️ Architektura

### Struktura Projektu

```
Win11Debloat/
├── 📁 CSharp/
│   └── WdrozycielIntegration/
│       ├── ConfigManager.cs          # Zarządzanie konfiguracją
│       ├── DeploymentManager.cs       # Manager wdrażania
│       ├── SystemAnalyzer.cs          # Analiza systemu
│       └── WdrozycielIntegration.csproj
│
├── 📁 Scripts/
│   ├── Deployer/
│   │   ├── DeploymentManager.ps1     # Orchestrator wdrażania
│   │   ├── ConfigValidator.ps1       # Walidacja profili
│   │   └── DeploymentLog.ps1         # System logowania
│   └── [Reszta istniejących skryptów]
│
├── 📁 Config/
│   ├── DeploymentProfiles.json       # Profile wdrażania
│   ├── DeployerConfig.json           # Konfiguracja systemu
│   └── ProfileMetadata.json          # Metadane profili
│
├── README.md                          # Dokumentacja główna
└── README-INTEGRATION.md              # Ta dokumentacja
```

### Przepływ Wdrażania

```
┌─────────────────────┐
│  Użytkownik         │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Manager Wdrażania   │
│ (DeploymentManager) │
└──────────┬──────────┘
           │
    ┌──────┴──────┐
    ▼             ▼
┌─────────┐  ┌──────────────┐
│Walidator│  │Analizator    │
│(Validator)  │Systemu       │
└──────┬──────┴──────┬───────┘
       │             │
       └──────┬──────┘
              ▼
      ┌────────────────┐
      │ Logger         │
      │ (Rejestracja)  │
      └────────┬───────┘
               ▼
      ┌────────────────┐
      │Zastosowanie    │
      │Zmian Systemu   │
      └────────────────┘
```

---

## 📊 Obsługiwane Profile

| Profil | Aplikacje | Ustawienia | Czas | Zastosowanie |
|--------|-----------|-----------|------|---------------|
| **Minimal** | 3 | Podstawowe | 15-20 min | Wysoka wydajność |
| **Standard** | 5+ | Umiarkowane | 30-45 min | Użytek ogólny |
| **Full** | 8+ | Zaawansowane | 60-90 min | Programiści |

---

## 🐛 Rozwiązywanie Problemów

### Problem: "Profil nie znaleziony"

```powershell
# Sprawdź dostępne profile
Get-Content .\Config\DeploymentProfiles.json | ConvertFrom-Json | Select-Object -ExpandProperty name
```

### Problem: "Brak uprawnień administratora"

```powershell
# Uruchom PowerShell jako administrator
Start-Process powershell -Verb RunAs
```

### Problem: "Błąd walidacji konfiguracji"

```powershell
# Sprawdź poprawność JSON
Test-Json -Path .\Config\DeploymentProfiles.json
```

---

## 📝 Logi

Wszystkie operacje są rejestrowane w:
- `./Logs/Deployment/deployment-YYYY-MM-DD-HHmmss.log`

Otwórz plik logu:

```powershell
Get-Content (Get-ChildItem .\Logs\Deployment | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
```

---

## 📞 Wsparcie

W przypadku problemów:

1. Sprawdź plik logu wdrażania
2. Uruchom w trybie `-Verbose` dla szczegółów
3. Testuj z `-DryRun` przed rzeczywistym wdrażaniem
4. Zgłoś problem na GitHub Issues

---

## 📜 Licencja

MIT License - patrz plik LICENSE

---

**Wersja:** 2.1.0  
**Ostatnia aktualizacja:** 2026-07-14  
**Autor:** Wdrozyciel Integration
