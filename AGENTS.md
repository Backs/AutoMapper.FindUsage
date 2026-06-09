# AI Project Context: AutoMapper.FindUsage

This file provides essential information for AI agents working on the AutoMapper.FindUsage project.

## Project Overview
AutoMapper.FindUsage is a JetBrains Rider and ReSharper plugin that enables navigation between DTOs and Models based on AutoMapper configurations. It helps developers jump from a property in a destination class to its source in the original model and vice versa.

## Technology Stack
- **C# / .NET**: Main logic for ReSharper/Rider backend.
- **Gradle**: Build system for the entire project.
- **IntelliJ Platform SDK**: For Rider frontend integration.
- **ReSharper SDK**: For static analysis and navigation logic.
- **Kotlin/Java**: Currently minimal or none, as the plugin relies on the ReSharper backend host in Rider.

## Project Structure
- `src/dotnet`: Contains the ReSharper plugin code.
  - `ReSharperPlugin.AutoMapper.FindUsage`: Main project containing mapping analysis and navigation logic.
    - `Registrations/`: Core logic for finding and caching AutoMapper mappings.
  - `ReSharperPlugin.AutoMapper.FindUsage.Tests`: Unit and integration tests.
    - `test/data/Navigation/`: C# files used as input for navigation tests.
- `src/rider`: Contains Rider-specific resources and potential frontend code.
  - `main/resources/META-INF/plugin.xml`: Plugin metadata and registration of components.
- `build.gradle.kts`: Main Gradle build script.
- `ReSharperPlugin.AutoMapper.FindUsage.sln`: .NET solution file.

## Key Concepts
- **Context Actions**: The plugin provides actions in the `Alt+Enter` menu on property setters (`set`) or `init` accessors.
- **Mapping Analysis**: The plugin scans the codebase for `CreateMap<TSource, TDest>()` calls and handles `.ReverseMap()`.
- **Zones**: ReSharper uses "Zones" for component isolation. See `IAutoMapper.FindUsageZone.cs` and `ZoneMarker.cs`.
- **Navigation**: Implements custom navigation to jump between mapped properties.

## Build and Run
- **Build .NET part**: `./gradlew compileDotNet`
- **Run Rider with plugin**: `./gradlew runIde` (Starts an experimental instance of Rider).
- **Run Tests**: `dotnet test` or via Gradle if configured.

## Development Guidelines for AI
- **Adding new mapping patterns**: Modify `AutoMapperRegistrationFinder.cs` in the `Registrations` folder to support more complex AutoMapper configurations.
- **Extending Navigation**: Look at how `AutoMapperSearchFactory.cs` and `AutoMapperMapping.cs` are implemented.
- **Tests**: When adding features, add a corresponding test case in `src/dotnet/ReSharperPlugin.AutoMapper.FindUsage.Tests/test/data/Navigation`.
- **Code Style**: Adhere to the existing C# coding standards (ReSharper style).

## Useful Documentation
- [ReSharper SDK Documentation](https://www.jetbrains.com/help/resharper/sdk/Welcome.html)
- [Rider SDK Documentation](https://plugins.jetbrains.com/docs/rider/introduction.html)
- [AutoMapper Documentation](https://docs.automapper.org/)
