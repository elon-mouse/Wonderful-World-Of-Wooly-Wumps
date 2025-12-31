using System;
using System.Collections.Generic;
using NiceIO.Sysroot;

namespace UnityEditor.Il2Cpp
{
    /// <summary>
    /// Toolchain package for building Linux players from a macOS (Arm64) host.
    /// </summary>
    /// <remarks>
    /// Provides an LLVM/Clang toolchain (compiler, linker, etc.) for use by IL2CPP on macOS Arm64.
    /// This package does <b>not</b> include a sysroot; pair it with an appropriate Linux sysroot
    /// package to cross-compile player code. The payload is shipped via UPM under
    /// <see cref="_packageName"/> and installed into the cache by the base <see cref="SysrootPackage"/>.
    /// </remarks>
    public class ToolchainMacOSArm64 : SysrootPackage
    {
        /// <summary>
        /// UPM package name that contains the LLVM/Clang payload.
        /// </summary>
        private string _packageName => "com.unity.toolchain.macos-arm64-linux";

        /// <summary>
        /// Human-readable package name as exposed to callers.
        /// </summary>
        public override string Name => _packageName;

        /// <summary>
        /// Host operating system this toolchain runs on.
        /// </summary>
        public override string HostPlatform => "macos";

        /// <summary>
        /// Host CPU architecture this toolchain runs on.
        /// </summary>
        public override string HostArch => "arm64";

        /// <summary>
        /// Target operating system this toolchain emits binaries for.
        /// </summary>
        /// <remarks>
        /// The precise target triple and sysroot (if any) come from the paired sysroot package
        /// and compiler/linker flags. This package focuses on providing host-side tool binaries.
        /// </remarks>
        public override string TargetPlatform => "linux";

        /// <summary>
        /// Version identifier of the toolchain payload expected by this package.
        /// </summary>
        /// <remarks>
        /// CI typically substitutes this placeholder at pack/publish time so the package resolves
        /// a concrete, immutable payload directory on disk.
        /// </remarks>
        private string _payloadVersion => "15.0.6_e4ae26aeef12b22751ce91a70bb02076a2e16c574e8c41dd21f98f3bcad3181e-1";

        /// <summary>
        /// Relative path (under the cache) to the installed toolchain payload root.
        /// </summary>
        private string _payloadDir;

        /// <summary>
        /// Cached absolute path to the installed toolchain root.
        /// </summary>
        private NPath _toolchainPath = null;

        /// <summary>
        /// Relative path (within the payload) to the linker binary used via <c>-fuse-ld=</c>.
        /// </summary>
        private string _linkerFile => "bin/ld.lld";

        /// <summary>
        /// Initializes the package and registers its toolchain payload so it can be resolved on disk.
        /// </summary>
        public ToolchainMacOSArm64()
            : base()
        {
            _payloadDir = $"llvm-mac-arm64/{_payloadVersion}";
            RegisterPayload(_packageName, _payloadDir);
            _toolchainPath = PayloadInstallDirectory(_payloadDir);
        }

        /// <summary>
        /// Ensures the toolchain bin directory is on <c>PATH</c>, then installs payloads if needed.
        /// </summary>
        /// <remarks>
        /// Adds <c>$CACHE/llvm-mac-arm64/&lt;version&gt;/bin</c> to the current process <c>PATH</c> (macOS uses <c>:</c> as the path separator),
        /// allowing IL2CPP to find the bundled <c>clang</c>, <c>ld.lld</c>, etc., without hardcoding absolute paths.
        /// </remarks>
        /// <returns><see langword="true"/> if initialization succeeds; otherwise <see langword="false"/>.</returns>
        public override bool Initialize()
        {
            UpdatePath();
            return base.Initialize();
        }

        /// <summary>
        /// Prepends the toolchain's <c>bin</c> directory to the current process <c>PATH</c> if it's not already present.
        /// </summary>
        /// <remarks>
        /// This affects only the current Unity Editor process (and its children). It does not modify the user’s shell profile.
        /// On macOS the path-list separator is <c>:</c>; using it here is intentional.
        /// </remarks>
        private void UpdatePath()
        {
            string binPath = _toolchainPath.Combine("bin").ToString(SlashMode.Native);
            string paths = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in paths.Split(':'))
            {
                if (path == binPath)
                    return;
            }
            Environment.SetEnvironmentVariable("PATH", $"{binPath}:{paths}");
        }

        /// <summary>
        /// Gets the absolute path to the installed toolchain payload root directory.
        /// </summary>
        /// <returns>Absolute path to the toolchain payload root.</returns>
        public string PathToPayload()
        {
            return PayloadInstallDirectory(_payloadDir).ToString();
        }

        /// <summary>
        /// Absolute path to a sysroot for IL2CPP (not applicable for this toolchain-only package).
        /// </summary>
        /// <returns>Always <c>null</c> for this package.</returns>
        public override string GetSysrootPath()
        {
            return null;
        }

        /// <summary>
        /// Absolute path to the installed toolchain that IL2CPP should use.
        /// </summary>
        /// <returns>Absolute path to the toolchain payload root.</returns>
        public override string GetToolchainPath()
        {
            return PathToPayload();
        }

        /// <summary>
        /// Additional compiler flags to pass to IL2CPP/Clang (none required by this package).
        /// </summary>
        /// <remarks>
        /// Targeting flags (e.g., <c>-target</c>) are typically provided by the paired sysroot package.
        /// </remarks>
        /// <returns><c>null</c> (no extra compiler flags).</returns>
        public override string GetIl2CppCompilerFlags()
        {
            return null;
        }

        /// <summary>
        /// Additional linker flags to ensure IL2CPP uses this package's linker.
        /// </summary>
        /// <remarks>
        /// Forces <c>ld.lld</c> from the payload via <c>-fuse-ld=</c> (quoted absolute path), and links
        /// libstdc++ statically for portability. Adjust if your distribution policy prefers dynamic libstdc++.
        /// </remarks>
        /// <returns>A space-separated string of linker flags.</returns>
        public override string GetIl2CppLinkerFlags()
        {
            var linkerpath = PayloadInstallDirectory(_payloadDir).Combine(_linkerFile);
            return $"-fuse-ld={linkerpath.InQuotes()} -static-libstdc++";
        }
    }
}
