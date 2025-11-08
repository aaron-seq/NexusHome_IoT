# Changelog

All notable changes to the NexusHome IoT Platform project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Comprehensive security policy (SECURITY.md)
- Professional README with advanced features and modern design
- Complete project documentation structure

### Changed
- Updated README with badges, architecture diagrams, and detailed sections
- Enhanced contribution guidelines

---

## [2.1.0] - 2025-11-08

### Added
- Real-time device monitoring via SignalR WebSocket connections
- AI-powered predictive maintenance using ML.NET
- Energy optimization algorithms based on usage patterns
- Anomaly detection for unusual device behavior
- Load forecasting capabilities
- Matter protocol support for universal IoT connectivity
- Advanced data visualization with Grafana integration
- Prometheus metrics collection
- Docker Compose orchestration for easy deployment
- Comprehensive API documentation with OpenAPI/Swagger
- Health check endpoints for monitoring
- Redis caching layer for performance optimization

### Changed
- Upgraded to .NET 8.0 LTS framework
- Enhanced Entity Framework Core to version 8.0
- Improved authentication with JWT token rotation
- Optimized database queries with better indexing
- Refactored services for better maintainability

### Fixed
- SQL injection vulnerabilities through parameterized queries
- Memory leaks in SignalR hub connections
- Race conditions in device state updates
- MQTT connection stability issues

### Security
- Implemented JWT-based authentication
- Added role-based access control (RBAC)
- Enhanced CORS policy configuration
- Implemented rate limiting (100 requests/minute)
- Added device certificate authentication
- Enabled TLS/SSL for all communications

---

## [2.0.0] - 2025-08-15

### Added
- Initial public release
- Multi-protocol device support (MQTT, HTTP, CoAP)
- Basic energy consumption tracking
- Room-based device organization
- Simple automation rules engine
- SQL Server database integration
- Basic web dashboard
- RESTful API endpoints

### Changed
- Migrated from .NET 6 to .NET 7
- Redesigned database schema for better performance
- Improved error handling and logging

### Deprecated
- Legacy API v1 endpoints (will be removed in v3.0.0)

---

## [1.5.0] - 2025-05-20

### Added
- MQTT broker integration
- Device discovery functionality
- Basic telemetry data collection
- Simple authentication system

### Changed
- Updated UI design
- Improved API response times

### Fixed
- Device connection timeouts
- Data synchronization issues

---

## [1.0.0] - 2025-02-10

### Added
- Initial release of NexusHome IoT Platform
- Basic device management
- Energy monitoring capabilities
- Simple web interface
- PostgreSQL database support
- Basic API endpoints

---

## Types of Changes

- **Added** for new features
- **Changed** for changes in existing functionality
- **Deprecated** for soon-to-be removed features
- **Removed** for now removed features
- **Fixed** for any bug fixes
- **Security** in case of vulnerabilities

---

## Versioning Policy

We follow Semantic Versioning (SemVer):

- **MAJOR** version for incompatible API changes
- **MINOR** version for backwards-compatible functionality additions
- **PATCH** version for backwards-compatible bug fixes

Example: v2.1.0
- 2 = Major version
- 1 = Minor version
- 0 = Patch version

---

## Release Notes

For detailed release notes and migration guides, please visit:
- [GitHub Releases](https://github.com/aaron-seq/NexusHome_IoT/releases)
- [Documentation](https://github.com/aaron-seq/NexusHome_IoT/tree/main/docs)

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for information on how to contribute to this project.

---

[Unreleased]: https://github.com/aaron-seq/NexusHome_IoT/compare/v2.1.0...HEAD
[2.1.0]: https://github.com/aaron-seq/NexusHome_IoT/compare/v2.0.0...v2.1.0
[2.0.0]: https://github.com/aaron-seq/NexusHome_IoT/compare/v1.5.0...v2.0.0
[1.5.0]: https://github.com/aaron-seq/NexusHome_IoT/compare/v1.0.0...v1.5.0
[1.0.0]: https://github.com/aaron-seq/NexusHome_IoT/releases/tag/v1.0.0
