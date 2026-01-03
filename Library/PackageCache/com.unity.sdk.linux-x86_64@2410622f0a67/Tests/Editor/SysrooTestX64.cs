using NUnit.Framework;
using UnityEditor.Il2Cpp;

/// <summary>
/// EditMode tests for the Linux x86_64 sysroot package.
/// </summary>
/// <remarks>
/// Verifies construction, a non-null payload path, and that compiler/linker flags
/// include the expected target triple—without requiring the payload to be installed.
/// </remarks>
/// <seealso cref="SysrootLinuxX86_64"/>
class SysrootTestX64
{
    /// <summary>
    /// Expected Clang/LLVM target triple for the Linux x86_64 sysroot.
    /// </summary>
    private string targetTriplet = "x86_64-unity-linux-gnu";

    /// <summary>
    /// Verifies the sysroot package can be instantiated without throwing.
    /// </summary>
    [Test]
    public void TestPackageInitialization()
    {
        Assert.NotNull(new SysrootLinuxX86_64());
    }

    /// <summary>
    /// Ensures the package reports a payload path string.
    /// </summary>
    /// <remarks>
    /// This test only checks that a non-null string is returned; it does not assert the path exists on disk.
    /// </remarks>
    [Test]
    public void TestPayloadExtracted()
    {
        var sysrootPackage = new SysrootLinuxX86_64();
        Assert.NotNull(sysrootPackage.PathToPayload());
    }

    /// <summary>
    /// Confirms the IL2CPP compiler flags include the expected target triple.
    /// </summary>
    [Test]
    public void TestIL2CPPCompilerFlags()
    {
        var sysrootPackage = new SysrootLinuxX86_64();
        Assert.IsTrue(
            string.Equals($"-target {targetTriplet}", sysrootPackage.GetIl2CppCompilerFlags()),
            "Compiler flags should exactly match the expected target triple.");
    }

    /// <summary>
    /// Confirms the IL2CPP linker flags include the expected target triple.
    /// </summary>
    [Test]
    public void TestIL2CPPLinkerFlags()
    {
        var sysrootPackage = new SysrootLinuxX86_64();
        Assert.IsTrue(
            string.Equals($"-target {targetTriplet}", sysrootPackage.GetIl2CppLinkerFlags()),
            "Linker flags should exactly match the expected target triple.");
    }
}
