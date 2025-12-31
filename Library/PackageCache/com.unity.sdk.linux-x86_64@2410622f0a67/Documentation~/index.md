# com.unity.sdk.linux-x86_64

This package provides the required 64-bit(x86_64) sysroot for building IL2CPP-based Unity players targeting both Linux and Embedded Linux platforms. It contains the system headers and libraries needed to compile and run Unity players, and is intended to be used in conjunction with a matching Unity-provided toolchain package on the host platform.

For an overview of Unity Embedded Linux support, see [Getting started with Embedded Linux](https://docs.unity.com/embeddedlinux/Getting_Started.html).

## Choosing the correct toolchain package

When building IL2CPP-based Unity players targeting 64-bit(x86_64) Linux or Embedded Linux, you must pair this sysroot package with the appropriate Unity toolchain package based on your host development platform.

| Host Platform      | Recommended Toolchain Package                          |
|--------------------|--------------------------------------------------------|
| Linux x86_64       | `com.unity.toolchain.linux-x86_64-linux`               |
| macOS x86_64       | `com.unity.toolchain.macos-x86_64-linux`               |
| macOS ARM64        | `com.unity.toolchain.macos-arm64-linux`                |
| Windows x86_64     | `com.unity.toolchain.win-x86_64-linux`                 |

## Building IL2CPP Players for Linux and Embedded Linux

After the appropriate toolchain package has been installed for the host platform, open the Unity project and navigate to:

**Project Settings** > **Player** > **Configuration**, then set the **Scripting Backend** to **IL2CPP**.

To build the player:

1. Open **File** > **Build Settings**.
2. In the **Target Platform** dropdown, select either **Linux** or **Embedded Linux**.
3. If **Embedded Linux** is selected, choose **x64** as the desired **Architecture**.
4. Click **Build** or **Build and Run** to generate the player executable.
