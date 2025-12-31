# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.0.6] - 2025-10-21

### Added
- Added the ability to manually refresh the Build Automation settings in the Build Profile window after connecting to Unity Cloud

### Modified
- Fixed an issue where the Cloud Build History page was failing to properly render
- Fixed an issue where an error was displayed in the terminal when Cloud Build credentials had not been defined

### Modified
- Fixed ThrowNullReferenceException when changing between build profiles by adding CancellationToken support

## [2.0.4] - 2025-04-11

### Added

### Modified
- Fix free tier banner not showing up

## [2.0.3] - 2025-04-03

### Added

### Modified

- Removed pricing details from the Machine Type drop-down. This information is still available in the documentation linked by the drop-down helper button.
- Fixed the alignment of the Build Automation settings helper boxes in the build profile window
- Removed the Configure button located in package manager, as it went nowhere

## [2.0.2] - 2025-02-18

### Added

### Modified

- API calls to the Build Automation service now use unique Org IDs instead of friendly IDs
- Now uses the Project level permissions for obtaining Signing Credentials

## [2.0.1] - 2025-01-10

### Added

- Added User information to Cloud Build History window
- Added a filter option to the Build History window to only show builds started by the current user

### Modified

- Updated the text in various places to be more clear
- Updated the XCode dropdown list to only include valid XCode versions for the selected Operating System
- Clicking the new credentials button will now properly link to the correct page in the DevOps dashboard, regardless of which platform is currently active
- The credentials dropdown list will now display all credentials, even if they have the same name
- The Build Profile filter in the Build History page now works correctly
- Removed the Build Automation section from Project Settings, as it was no longer used
- Build Automation Settings on a Build Profile now have default values
- Pagination on the Build History page now works correctly

## [2.0.0] - 2025-01-03

### Added

- Updated version to 2.0.0

## [2.0.0-pre.2] - 2024-12-18

### Added

- Onboarding flow was added when using the free tier and all free build minutes have been consumed for the current
  billing cycle.

## [2.0.0-pre.1] - 2024-12-16

Brings building in the cloud configurations closer in-line with local builds from the Editor. Only supports triggering
builds from the Editor when using Unity Version Control.

### Added

- Dependency on the com.unity.collab-proxy package.
- Support for Unity 6000.1 and newer.
- Cloud Build settings and button appear in the Build Profiles window of the Editor.
- Ability to build the latest checked in changeset or to shelve and build local changes.
- There is a new build history window which shows the status of builds triggered from the Editor. Upon build completion
  artifacts can be manually downloaded from the build history. Links to logs on the cloud dashboard are also available.
- Builds in the cloud utilize the build profile.
- Builds in the cloud can be triggered for profiles that are not the active profile. This means that an iOS build can be
  triggered from the Editor even if the active profile is set to Android.

### Removed

- Support for Unity versions older than 6000.1

## [1.0.5] - 2023-06-06

### Modified

- Added define constraints to the package assembly to don't load on Unity versions < 2022.

## [1.0.4] - 2023-06-01

### Modified

- Fixed documentation and dashboard links.
- Adjusted package accordingly to Unity Build Automation rebranding.

## [1.0.2] - 2021-10-19

### Modified

- Adjusted package compatibility

## [1.0.1] - 2021-10-07

### Updated Project Settings UI for standardization

## [1.0.0] - 2021-07-22

### This is the first release of *com.unity.services.cloud-build*.

