# Nijhof Panel - Revit Plugin

Een uitgebreide Revit-plugin ontwikkeld voor Nijhof Installaties om workflows te automatiseren en de productiviteit te verhogen binnen BIM-projecten.

## 📋 Overzicht

Nijhof Panel is een multi-versie Revit-plugin die tools biedt voor elektrotechnische installaties, prefab-management, en diverse projectspecifieke automatiseringen. De plugin ondersteunt Revit 2024, 2025 en 2026.

## ✨ Hoofdfuncties

### Elektrotechnische Tools
- **Component Plaatsing**: Snel plaatsen van stopcontacten, schakelaars, aansluitpunten, data-aansluitingen en verlichting
- **Automatische Tagging**:
    - Groepnummers taggen
    - Switchcodes toevoegen
    - Codelijsten genereren

### Prefab Management
- **Set Beheer**: Creëer en beheer prefab sets met unieke identificatie
- **Automatische Sheets**: Genereer sheets en views op basis van prefab sets
- **Materiaal- en Zaaglijsten**: Exporteer lijsten naar Excel/CSV
- **Artikelnummer Toewijzing**: Automatische artikelnummers op basis van diameter en type

### W-Tools (MEP Tools)
- **Element Connectie**: Verbind MEP-elementen automatisch
- **Pipe Splitting**: Splits leidingen in segmenten van 5000mm met sokken
- **Sparingen**: Automatisch sparingen plaatsen in muren, vloeren en funderingen
- **HWA Management**: Update artikelnummers en lengtes voor hemelwaterafvoer

### Export Functionaliteit
- Excel export van schedules
- CSV export voor zaagmachine-integratie
- Materiaallijsten en stuklijsten
- Automatische bestandsstructuur op netwerkschijf

## 🔧 Technische Specificaties

### Vereisten
- Autodesk Revit 2024, 2025, of 2026
- Windows 10/11
- .NET Framework 4.8 (voor Revit 2024)
- .NET 8.0 (voor Revit 2025/2026)

### Afhankelijkheden
- Nice3point.Revit.Toolkit
- Nice3point.Revit.Extensions
- EPPlus 8.2.1
- Microsoft.Xaml.Behaviors.Wpf
- CommunityToolkit.Mvvm
- Newtonsoft.Json

## 🏗️ Project Structuur

```
NijhofPanel/
├── Commands/           # Revit commando's en handlers
│   ├── Core/          # Basis functionaliteit
│   ├── Electrical/    # Elektra tools
│   └── Tools/         # Algemene tools
├── ViewModels/        # MVVM ViewModels
├── Views/            # WPF Views en UI
├── Services/         # Business logic en services
├── Helpers/          # Utility classes
├── UI/              # Theming en controls
└── Resources/       # Icons en assets
```

## 🚀 Installatie

1. Download de laatste release van de plugin
2. Kopieer de `.addin` file naar:
   ```
   %APPDATA%\Autodesk\Revit\Addins\[VERSION]\
   ```
3. Start Revit
4. De plugin verschijnt in het ribbon als "Nijhof Tools"

## 💡 Gebruik

### Basisfunctionaliteit

**Panel Openen:**
- Via ribbon: Klik op "Open Panel" om het dockable panel te openen
- Alternatief: "Open Venster" voor een los venster

**Navigatie:**
- Gebruik de sidebar voor navigatie tussen verschillende tool-categorieën
- Elektrische tools, W-tools, Library, Prefab Manager, etc.

### Prefab Workflow

1. **Nieuwe Set Maken:**
    - Selecteer elementen in het model
    - Klik op "Nieuwe Set" in Tools → Prefab
    - Voer bouwnummer/kavel in
    - Sheets en views worden automatisch aangemaakt

2. **Materiaallijst Genereren:**
    - Open Prefab Manager
    - Vink "Materiaallijst" aan voor gewenste sets
    - Lijst wordt automatisch gegenereerd en gefilterd

3. **Export:**
    - Klik op het Materiaallijst/Zaaglijst icoon
    - Selecteer gewenste lijsten
    - Kies Excel of CSV formaat
    - Bestanden worden opgeslagen op netwerklocatie

### Elektrische Tools

1. **Component Plaatsen:**
    - Navigeer naar "E-Tools"
    - Selecteer gewenst component type
    - Klik in de view om te plaatsen

2. **Automatisch Taggen:**
    - Gebruik "Groep nummer" of "Switch code"
    - Tags worden automatisch geplaatst op relevante elementen
    - Verticale elementen worden verborgen

## ⚙️ Configuratie

### Artikelnummers Instellen

1. Ga naar Instellingen (tandwiel icoon)
2. Selecteer producttype (Dyka PVC, Sono, HWA, Air)
3. Vul artikelnummers in per diameter
4. Klik op "Opslaan"

### Netwerkpaden

Standaard netwerkpaden:
```
Families: F:\Stabiplan\Custom\Families\
Exports: T:\Data\[ProjectNr]\2.8 Tekeningen\02 Nijhof\03 PDF Prefab tekeningen\
Settings: F:\Revit\Plugin\Nijhof Panel\Data\
```

## 🔄 Ontwikkeling

### Build Configuraties

De solution ondersteunt meerdere configuraties:
- Debug/Release R24 (Revit 2024)
- Debug/Release R25 (Revit 2025)
- Debug/Release R26 (Revit 2026)

### Development Host

Een DevHost project is beschikbaar voor UI development zonder Revit:
```
NijhofPanel.DevHost
```

### Build Process

Het project gebruikt Nice3point.Revit.Build.Tasks voor automatische deployment:
- Automatisch kopiëren naar Revit Addins folder
- Multi-version support
- Hot reload tijdens development

## 📝 Changelog

Zie [Changelog.md](Changelog.md) voor volledige versiegeschiedenis.

### Laatste Versie (1.0.0-beta.1)

**Nieuw:**
- Basis elektrotechnische tools
- Prefab management systeem
- Material/zaaglijst export
- Sparingen tool voor muren/vloeren/funderingen
- Library browser met thumbnails
- Dark mode ondersteuning

**Bekend Issues:**
- USO Tool nog niet functioneel
- GPS functionaliteit in ontwikkeling

## 🤝 Bijdragen

Dit is een intern project voor Nijhof Installaties. Voor vragen of suggesties, neem contact op met het development team.

## 📄 Licentie

© 2025 Nijhof Installaties. © 2025 Bluetech Engineering. Alle rechten voorbehouden.

Dit is proprietary software ontwikkeld voor intern gebruik.

## 🆘 Support

Voor technische ondersteuning:
- Intern: Contact Damian Maats via e-mail
- Bug reports: Contact Damian Maats via e-mail
- Feature requests: Contact Damian Maats via e-mail

## 🔐 Beveiliging

**Belangrijk:**
- De plugin gebruikt netwerkshares voor data-opslag
- Zorg voor juiste toegangsrechten op T:\ en F:\ drives
- Instellingen worden centraal opgeslagen voor team-gebruik

## 🎯 Roadmap

### Gepland voor toekomstige releases:
- [ ] USO Tool implementatie
- [ ] GPS functionaliteit
- [ ] Uitgebreide 3D view management
- [ ] Automatische maatvoering
- [ ] BIM 360/ACC integratie
- [ ] Verbeterde foutafhandeling
- [ ] Performance optimalisaties

## 📚 Documentatie

Aanvullende documentatie beschikbaar:
- API referentie (in code comments)
- Video tutorials (intern beschikbaar)
- Best practices guide

---

**Versie:** 1.0.0-beta.1  
**Laatste Update:** Oktober 2025  
**Ontwikkelaar:** Nijhof Installaties BIM Team (Damian Maats - Bluetech Engineering)