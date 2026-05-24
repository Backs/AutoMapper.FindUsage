# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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

[0.1.2]: https://github.com/Backs/AutoMapper.FindUsage/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/Backs/AutoMapper.FindUsage/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/Backs/AutoMapper.FindUsage/releases/tag/v0.1.0
