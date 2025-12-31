# Changelog

## [2.0.1] - 2025-11-07

## [2.0.0] - 2025-10-07

- Updated version to match Multiplayer Play Mode.

## [2.0.0-pre.1] - 2025-08-06

- Added support for Unity 6.3; migrated the code to the engine

## [2.0.0-exp.2] - 2025-07-23

- Updated version to match Multiplayer Play Mode.

## [2.0.0-exp.1] - 2025-06-15

- Updated version to match Multiplayer Play Mode.

## [1.5.0] - 2025-06-03

- Updated version to match Multiplayer Play Mode.

## [1.5.0-exp.4] - 2025-05-14

- Updated version to match Multiplayer Play Mode.

## [1.5.0-exp.3] - 2025-04-23

### Fixed
- Fixed "Invalid multiplayer role mask" Argument Exception issue when building for non-standalone platforms.

## [1.5.0-exp.2] - 2025-04-02

- Updated version to match Multiplayer Play Mode.

## [1.5.0-exp.1] - 2025-03-06

### Added
- Included lightmap components to rendering automatic selection: `ReflectionProbe`, `LightProbeGroup`, `LightProbeProxyVolume`

### Fixed
- Added a space to the Multiplayer Role tooltip for consistency
- Added a fixed size for Multiplayer Role dropdown

## [1.4.0-pre.2] - 2025-01-10

### Fixed
- Multiplayer Role reset to default value for build profiles after a restart

## [1.4.0-pre.1] - 2024-12-03

- Bumping up version to stay synchronized with Multiplayer Playmode

## [1.3.2] - 2024-11-14

- Bumping up version to stay synchronized with Multiplayer Playmode

## [1.3.1] - 2024-10-21

- Bumping up version to stay synchronized with Multiplayer Playmode

## [1.3.0] - 2024-09-26

### Fixed
- Fixed project settings multiplayer roles UI issues.
- Fixed a null reference error when a component is added to the automatic selection options and the script is removed later.

## [1.3.0-pre.3] - 2024-09-20

- Bumping up version to stay synchronized with Multiplayer Playmode

## [1.3.0-pre.2] - 2024-08-14

### Fixed
- Fixed a null reference exception error message that could appear when the old Build Settings window was opened and the Dedicated Server module was not installed.

## [1.3.0-pre.1] - 2024-07-26

### Added
- Documented a workflow to access the active multiplayer role in a script.

## [1.3.0-exp.4] - 2024-07-16

- Bumping up version to stay synchronized with Multiplayer Playmode

## [1.3.0-exp.3] - 2024-07-11

- Bumping up version to stay synchronized with Multiplayer Playmode

## [1.3.0-exp.2] - 2024-07-03

- Bumping up version to stay synchronized with Multiplayer Playmode

## [1.3.0-exp.1] - 2024-06-24

- Bumping up version to stay synchronized with Multiplayer Playmode

## [1.2.0] - 2024-06-04

### Added
- Added `EditorMultiplayerRolesManager.[Get|Set]MultiplayerRoleForClassicTarget` to complement the `EditorMultiplayerRolesManager.[Get|Set]MultiplayerRoleForBuildProfile` API.

## [1.1.0] - 2024-04-24

### Fixed

- Fixed the content selection icon overlapping with the Cinemachine for the camera in the hierarchy view.
- Fixed multiplayer roles settings not being synced to multiplayer playmode virtual players.
- Fixed multiplayer roles enabled automatically when the package is installed.

### Changed

- Multiplayer Roles can be now assigned to build profiles. The old `EditorMultiplayerRolesManager.[Get|Set]MultiplayerRoleForBuildTarget` API has been deprecated in favor of the new `EditorMultiplayerRolesManager.[Get|Set]MultiplayerRoleForBuildProfile` API.

## [1.0.0] - 2024-03-12

### Added

- CLI Arguments Defaults: Provides an UI in the build window for defining default values for the CLI arguments used to launch the game server.
- Multiplayer Roles: Allows to decide which multiplayer role (Client, Server) to use in each build target.
- Content Selection: Provides UI and API for selecting which content (GameObjects, Components) should be present/removed in the different multiplayer roles.
- Automatic Selection: Provides UI and API for selecting which component types should be automatically removed in the different multiplayer roles.
- Safety Checks: Activates warnings that helps detecting potential null reference exceptions caused by stripping objects for a multiplayer role.
