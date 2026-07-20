# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.2.1] - 2026-07-20
### Changed
- Downgraded ReSharper SDK to 2025.1.9

## [0.2.0] - 2026-06-30
### Changed
- **Breaking Change**: Removed support for Context Actions (`Alt+Enter`). The plugin now integrates exclusively with the standard "Find Usages" action.
- Updated version to 0.2.0.

## [0.1.3] - 2026-06-30
### Added
- Deep integration with standard "Find Usages" action.
- Support for `init` accessors in navigation.
- Smart grouping of results in the "Find Results" window for properties with multiple mappings.
- New tests for multiple source mappings and `init` property navigation.

### Changed
- Navigation from Source to Destination now requires `.ReverseMap()` to be defined, while Destination to Source always works.

### Fixed
- Prevented self-mapping navigation (e.g., when a type is mapped to itself).
- Improved performance of mapping discovery by deduplicating project files during analysis.

## [0.1.2] - 2026-05-24
### Added
- Support for `Ignore()` method for properties. Navigation to the source property is not suggested if the destination property is marked as ignored in the AutoMapper configuration.

## [0.1.1] - 2026-05-24
### Changed
- Minor changes.

## [0.1.0] - 2026-05-22
### Added
- Navigation from DTO property to source Model property via `Alt+Enter` (on `set`/`init` accessors).
- Support for `CreateMap<TSource, TDestination>` configurations.
- Support for `.ReverseMap()` allowing bidirectional navigation.
- Grouping of mappings by type when multiple mappings are found for the same property.
- Automatic use of full type names in the menu when multiple mappings are present.
- Integration with ReSharper 2025.3 SDK.

[0.2.0]: https://github.com/Backs/AutoMapper.FindUsage/compare/v0.1.3...v0.2.0
[0.1.3]: https://github.com/Backs/AutoMapper.FindUsage/compare/v0.1.2...v0.1.3
[0.1.2]: https://github.com/Backs/AutoMapper.FindUsage/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/Backs/AutoMapper.FindUsage/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/Backs/AutoMapper.FindUsage/releases/tag/v0.1.0
