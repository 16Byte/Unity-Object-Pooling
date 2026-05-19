# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-05-19

### Added
- Initial release.
- `ObjectPoolManager` with static `SpawnObject` and `ReturnObjectToPool` API.
- `IPooledObject` interface for per-spawn lifecycle hooks.
- Generic and non-generic spawn overloads (`Instantiate`-mirrored signatures).
- Three default pool categories for hierarchy organization: `GameObjects`, `ParticleSystems`, `SoundFX`.
- Lazy bootstrap — the system works with or without an `ObjectPoolManager` component in scene.
- `Basic Spawning` sample demonstrating the full lifecycle.
