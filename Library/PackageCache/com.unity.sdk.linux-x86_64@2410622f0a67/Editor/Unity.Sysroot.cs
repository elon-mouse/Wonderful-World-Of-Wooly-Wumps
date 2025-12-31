using System.IO;
using System.Collections.Generic;
using NiceIO.Sysroot;

namespace UnityEditor.Il2Cpp
{
    /// <summary>
    /// Sysroot package implementation for building Linux (x86_64) IL2CPP players.
    /// </summary>
    /// <remarks>
    /// This class wires a sysroot payload (headers/libs) into IL2CPP's compile/link
    /// steps by advertising the target triple and locating the on-disk sysroot path.
    /// The payload itself is distributed via UPM under <see cref="_packageName"/>.
    /// </remarks>
    public class SysrootLinuxX86_64 : SysrootPackage
    {
        /// <summary>
        /// The UPM package name that ships this sysroot payload.
        /// </summary>
        private string _packageName => "com.unity.sdk.linux-x86_64";

        /// <summary>
        /// Human-readable package name as exposed to callers.
        /// </summary>
        public override string Name => _packageName;

        /// <summary>
        /// The OS this sysroot targets.
        /// </summary>
        /// <remarks>Used for selection/routing in higher-level tooling.</remarks>
        public override string TargetPlatform => "linux";

        /// <summary>
        /// The CPU architecture this sysroot targets.
        /// </summary>
        /// <remarks>Follows the the toolchain triple token (x86_64).</remarks>
        public override string TargetArch => "x86_64";

        /// <summary>
        /// Identifier for the concrete payload version that this package expects.
        /// </summary>
        /// <remarks>
        /// CI typically substitutes this placeholder at pack/publish time so the package
        /// resolves a precise, immutable payload directory on disk.
        /// </remarks>
        private string _payloadVersion => "2.35-134_7122795dd37e3872a95e34e6a9498cffaea142059e76c124b0e54b781e96f792-1";

        /// <summary>
        /// Relative path (within the UPM cache) to the payload root for this platform/arch.
        /// </summary>
        private string _payloadDir;

        /// <summary>
        /// The Clang/LLVM target triple used for cross-compiling IL2CPP code against this sysroot.
        /// </summary>
        private string _target => "x86_64-unity-linux-gnu";

        /// <summary>
        /// Cached install directory of the resolved payload.
        /// </summary>
        private NPath _sysrootInstallDir;

        /// <summary>
        /// Initializes the package and registers its payload so it can be resolved on disk.
        /// </summary>
        public SysrootLinuxX86_64()
        {
            _payloadDir = $"linux-x86_64/{_payloadVersion}";
            RegisterPayload(_packageName, _payloadDir);
            _sysrootInstallDir = PayloadInstallDirectory(_payloadDir);
        }

        /// <summary>
        /// Gets the absolute path to the installed payload root directory.
        /// </summary>
        /// <returns>Absolute path to the payload root.</returns>
        public string PathToPayload()
        {
            return PayloadInstallDirectory(_payloadDir).ToString();
        }

        /// <summary>
        /// Resolves the absolute path to the sysroot directory inside the installed payload.
        /// </summary>
        /// <remarks>
        /// Looks for a child directory named exactly like the target triple (e.g.,
        /// <c>x86_64-unity-linux-gnu</c>) and returns its <c>sysroot</c> subfolder.
        /// Returns <c>null</c> if the expected structure is not present.
        /// </remarks>
        /// <returns>Absolute path to the sysroot directory, or <c>null</c> if not found.</returns>
        private string PathToSysroot()
        {
            var sdkPath = PathToPayload();
            var sdkInfo = new DirectoryInfo(sdkPath);
            foreach (var target in sdkInfo.GetDirectories(_target))
            {
                var sysrootPath = Path.Combine(sdkPath, target.Name, "sysroot");
                if (Directory.Exists(sysrootPath))
                {
                    return sysrootPath.ToString();
                }
            }
            return null;
        }

        /// <summary>
        /// Absolute path to the sysroot that IL2CPP should compile/link against (not applicable for this toolchain-only package).
        /// </summary>
        /// <returns>Absolute sysroot path, or <c>null</c> if the payload is missing/incomplete or a toolchain-only package.</returns>
        public override string GetSysrootPath()
        {
            return PathToSysroot();
        }

        /// <summary>
        /// Absolute path to a toolchain payload (not applicable for this sysroot-only package).
        /// </summary>
        /// <returns>Absolute toolchain path, or <c>null</c> if the payload is missing/incomplete or a sysroot-only package.</returns>
        public override string GetToolchainPath()
        {
            return null;
        }

        /// <summary>
        /// Compiler flags that IL2CPP (Clang) must receive to target this sysroot.
        /// </summary>
        /// <remarks>
        /// Primarily sets the target triple used for cross-compilation.
        /// Example result: <c>-target x86_64-unity-linux-gnu</c>.
        /// </remarks>
        /// <returns>A space-separated string of compiler flags.</returns>
        public override string GetIl2CppCompilerFlags()
        {
            return $"-target {_target}";
        }

        /// <summary>
        /// Linker flags that IL2CPP must receive to target this sysroot.
        /// </summary>
        /// <remarks>
        /// Mirrors the compiler target so the linker resolves libraries and startup files
        /// from the correct sysroot. Example: <c>-target x86_64-unity-linux-gnu</c>.
        /// </remarks>
        /// <returns>A space-separated string of linker flags.</returns>
        public override string GetIl2CppLinkerFlags()
        {
            return $"-target {_target}";
        }
    }
}
