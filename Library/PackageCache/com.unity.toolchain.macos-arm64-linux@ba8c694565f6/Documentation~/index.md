# com.unity.toolchain.macos-arm64-linux

The `com.unity.toolchain.macos-arm64-linux` package provides a toolchain for building IL2CPP players targeting both Linux and Embedded Linux platforms on a MacOS Arm64 host. It depends on the following package:
- `com.unity.sysroot.base` for common code

For an overview of Unity's Embedded Linux support, refer to the [Getting started with Embedded Linux](https://docs.unity.com/embeddedlinux/Getting_Started.html) guide.

Prior to installing a toolchain package, ensure that Linux and/or Embedded Linux support has been added to the Unity Editor. For additional details, see [Install the Unity Editor for Embedded Linux](https://docs.unity.com/embeddedlinux/Installation.html).

## Choosing the correct SDK package

hen building Linux or Embedded Linux IL2CPP players, the appropriate `com.unity.sdk.*` package should be selected based on the intended target platform:

| Target Platform       | Recommended Sysroot Package              |
|-----------------------|------------------------------------------|
| Linux x86_64          | `com.unity.sdk.linux-x86_64`         |
| Embedded Linux x86_64 | `com.unity.sdk.linux-x86_64`         |
| Embedded Linux ARM64  | `com.unity.sdk.linux-arm64`        |

## Building IL2CPP Players for Linux and Embedded Linux

Once the appropriate toolchain package has been installed for the host platform, the Unity project can be configured by navigating to:

**Project Settings** > **Player** > **Configuration**, and setting the **Scripting Backend** to **IL2CPP**.

To build the player:

1. Open **File** > **Build Settings**.
2. In the **Target Platform** dropdown, select either **Linux** or **Embedded Linux**.
3. If **Embedded Linux** is selected, choose **x64** or **Arm64** as the desired **Architecture**.
4. Select **Build** or **Build and Run** to generate the player executable.
