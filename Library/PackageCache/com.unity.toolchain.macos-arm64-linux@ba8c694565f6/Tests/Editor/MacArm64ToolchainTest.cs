using System;
using NUnit.Framework;
using UnityEditor.Il2Cpp;

/// <summary>
/// EditMode tests for the MacOS Arm64 toolchain package.
/// </summary>
/// <remarks>
/// Verifies construction, a non-null payload path, that compiler flags are not
/// provided by this toolchain-only package, and that linker flags include both
/// <c>-fuse-ld=…</c> and <c>-static-libstdc++</c>.
/// </remarks>
/// <seealso cref="ToolchainMacOSArm64"/>
class MacArm64ToolchainTest
{
    /// <summary>
    /// Ensures the toolchain package can be instantiated without throwing.
    /// </summary>
    [Test]
    public void TestPackageInitialization()
    {
        Assert.NotNull(new ToolchainMacOSArm64());
    }

    /// <summary>
    /// Confirms the package reports a payload path string.
    /// </summary>
    /// <remarks>
    /// This test only checks for a non-null string; it does not assert the path exists on disk.
    /// </remarks>
    [Test]
    public void TestPayloadExtracted()
    {
        var toolchainPackage = new ToolchainMacOSArm64();
        Assert.NotNull(toolchainPackage.PathToPayload());
    }

    /// <summary>
    /// Verifies that no compiler flags are emitted by the toolchain package.
    /// </summary>
    [Test]
    public void TestIL2CPPCompilerFlags()
    {
        var toolchainPackage = new ToolchainMacOSArm64();
        Assert.Null(toolchainPackage.GetIl2CppCompilerFlags());
    }

    /// <summary>
    /// Checks that the linker flags include both the linker selection and static libstdc++.
    /// </summary>
    [Test]
    public void TestIL2CPPLinkerFlags()
    {
        var toolchainPackage = new ToolchainMacOSArm64();
        string linkerflags = toolchainPackage.GetIl2CppLinkerFlags();

        bool containsFuseLd = linkerflags.Contains("-fuse-ld=", StringComparison.Ordinal);
        bool containsStaticCpp = linkerflags.Contains("-static-libstdc++", StringComparison.Ordinal);

        Assert.IsTrue(containsFuseLd && containsStaticCpp,
            "Linker flags should include both -fuse-ld=… and -static-libstdc++.");
    }
}
