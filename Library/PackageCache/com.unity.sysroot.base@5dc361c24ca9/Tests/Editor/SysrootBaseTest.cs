using NUnit.Framework;
using UnityEditor.Il2Cpp;

/// <summary>
/// EditMode tests for the base sysroot package scaffold.
/// </summary>
/// <remarks>
/// Sanity-checks that the base <see cref="SysrootPackage"/> type can be instantiated
/// in the current compile configuration.
/// </remarks>
/// <seealso cref="SysrootPackage"/>
class SysrootBaseTest
{
    /// <summary>
    /// Verifies that <see cref="SysrootPackage"/> can be constructed without throwing.
    /// </summary>
    [Test]
    public void TestPackageInitialization()
    {
        Assert.NotNull(new SysrootPackage());
    }
}