using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor;
using NiceIO.Sysroot;
#if UNITY_STANDALONE_LINUX_API
using UnityEditor.LinuxStandalone;
#endif
#if UNITY_EMBEDDED_LINUX_API
using UnityEditor.EmbeddedLinux;
#endif

namespace UnityEditor.Il2Cpp
{
    /// <summary>
    /// Describes where a sysroot/toolchain payload is located and where it should be installed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="path"/> points to the compressed payload archive (typically
    /// <c>Packages/&lt;name&gt;/data~/payload.tar.7z</c>).
    /// </para>
    /// <para>
    /// <see cref="dir"/> is the destination directory under the sysroot cache where the
    /// archive will be extracted.
    /// </para>
    /// </remarks>
    public struct PayloadDescriptor
    {
        /// <summary>
        /// Absolute path to the payload tarball to install (e.g., <c>payload.tar.7z</c>).
        /// </summary>
        internal NPath path;

        /// <summary>
        /// Absolute destination directory where the payload will be extracted.
        /// </summary>
        internal NPath dir;
    }

    /// <summary>
    /// Tracks one-time initialization state for a package.
    /// </summary>
    enum InitializationStatus
    {
        /// <summary>Initialization has not been attempted.</summary>
        Uninitialized,
        /// <summary>Initialization was attempted and failed.</summary>
        Failed,
        /// <summary>Initialization completed successfully.</summary>
        Succeeded
    }

    /// <summary>
    /// Base class for sysroot and toolchain packages.
    /// </summary>
    /// <remarks>
    /// When the <c>UNITY_STANDALONE_LINUX_API</c> or <c>UNITY_EMBEDDED_LINUX_API</c> defines are set,
    /// this class implements the <see cref="Sysroot"/> interface consumed by the Linux build pipeline.
    /// Derived classes supply platform/arch metadata and the payload locations needed by IL2CPP.
    /// </remarks>
    public class SysrootPackage
#if UNITY_STANDALONE_LINUX_API || UNITY_EMBEDDED_LINUX_API
        : Sysroot
#endif
    {
        /// <summary>
        /// Heuristically checks whether the Linux IL2CPP Players are present in the Editor installation.
        /// </summary>
        /// <remarks>
        /// Used to emit a helpful debug warning when sysroot/toolchain packages are present but
        /// the IL2CPP player toolchain for Linux is missing.
        /// </remarks>
        private static bool IsLinuxIL2CPPPresent()
        {
            string targetDir = $"{BuildPipeline.GetPlaybackEngineDirectory(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64, BuildOptions.None)}/Variations/il2cpp";
            if (Directory.Exists(targetDir))
                return true;

            return false;
        }

        /// <summary>
        /// Returns <see langword="true"/> when verbose debug logging for sysroots is enabled.
        /// </summary>
        /// <remarks>
        /// Controlled by the <c>UNITY_SYSROOT_DEBUG</c> environment variable (any non-empty value).
        /// </remarks>
        /// <returns>
        /// <see langword="true"/> if <c>UNITY_SYSROOT_DEBUG</c> is set to a non-empty string; 
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public static bool ShouldLogDebugMessage()
        {
            return !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("UNITY_SYSROOT_DEBUG"));
        }

        /// <summary>
        /// On editor load, warns (in debug mode) if Linux IL2CPP is missing while Linux toolchain packages are present.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void IssueWarningIfLinuxIL2CPPNotPresent()
        {
            if (ShouldLogDebugMessage() && !IsLinuxIL2CPPPresent())
            {
                UnityEngine.Debug.LogWarning($"Linux Compiler Toolchain package(s) present, but required Linux-IL2CPP is missing");
            }
        }

        /// <summary>
        /// Package identifier (UPM name).
        /// </summary>
        public
#if UNITY_STANDALONE_LINUX_API || UNITY_EMBEDDED_LINUX_API
            override
#else
            virtual
#endif
            string Name           => "com.unity.sysroot.base";

        /// <summary>
        /// Host operating system (e.g., <c>linux</c>, <c>win</c>, <c>macos</c>).
        /// </summary>
        /// <remarks>
        /// Base implementation returns an empty string; derived classes should override with a stable token.
        /// </remarks>
        public
#if UNITY_STANDALONE_LINUX_API || UNITY_EMBEDDED_LINUX_API
            override
#else
            virtual
#endif
        string HostPlatform   => "";

        /// <summary>
        /// Host CPU architecture (e.g., <c>x86_64</c>, <c>arm64</c>).
        /// </summary>
        /// <remarks>Base implementation returns an empty string; override in derived packages.</remarks>
        public
#if UNITY_STANDALONE_LINUX_API || UNITY_EMBEDDED_LINUX_API
            override
#else
            virtual
#endif
            string HostArch       => "";

        /// <summary>
        /// Target operating system (e.g., <c>linux</c>, <c>win</c>, <c>macos</c>).
        /// </summary>
        /// <remarks>Base implementation returns an empty string; override in derived packages.</remarks>
        public
#if UNITY_STANDALONE_LINUX_API || UNITY_EMBEDDED_LINUX_API
            override
#else
            virtual
#endif
            string TargetPlatform => "";

        /// <summary>
        /// Target CPU architecture (e.g., <c>x86_64</c>, <c>arm64</c>).
        /// </summary>
        /// <remarks>Base implementation returns an empty string; override in derived packages.</remarks>
        public
#if UNITY_STANDALONE_LINUX_API || UNITY_EMBEDDED_LINUX_API
            override
#else
            virtual
#endif
            string TargetArch     => "";

        /// <summary>
        /// Additional arguments to append to the IL2CPP invocation.
        /// </summary>
        /// <remarks>
        /// Return <c>null</c> for no extra arguments. Derived classes can yield a sequence of
        /// flags (e.g., <c>--sysroot=&lt;path&gt;</c>) that IL2CPP will consume.
        /// </remarks>
        /// <returns>Enumerable of additional arguments, or <c>null</c>.</returns>
        public
#if UNITY_STANDALONE_LINUX_API || UNITY_EMBEDDED_LINUX_API
            override
#else
            virtual
#endif
            IEnumerable<string> GetIl2CppArguments() { return null; }

        /// <summary>
        /// Absolute path to the installed sysroot to be used by IL2CPP.
        /// </summary>
        /// <remarks>
        /// Return <c>null</c> if this package does not supply a sysroot (e.g., toolchain-only packages).
        /// </remarks>
        /// <returns>Sysroot directory path, or <c>null</c>.</returns>
        public
#if UNITY_STANDALONE_LINUX_API || UNITY_EMBEDDED_LINUX_API
            override
#else
            virtual
#endif
            string GetSysrootPath() { return null; }

        /// <summary>
        /// Absolute path to the installed toolchain to be used by IL2CPP.
        /// </summary>
        /// <remarks>
        /// Return <c>null</c> if this package does not supply a toolchain (e.g., sysroot-only packages).
        /// </remarks>
        /// <returns>Toolchain directory path, or <c>null</c>.</returns>
        public
#if UNITY_STANDALONE_LINUX_API || UNITY_EMBEDDED_LINUX_API
            override
#else
            virtual
#endif
            string GetToolchainPath() { return null; }

        /// <summary>
        /// Extra compiler flags (passed to Clang via IL2CPP) required by this package.
        /// </summary>
        /// <remarks>
        /// Typical use is to supply a target triple (e.g., <c>-target aarch64-unity-linux-gnu</c>)
        /// or sysroot/emulation flags. Return <c>null</c> for none.
        /// </remarks>
        /// <returns>Space-separated compiler flags, or <c>null</c>.</returns>
        public
#if UNITY_STANDALONE_LINUX_API || UNITY_EMBEDDED_LINUX_API
            override
#else
            virtual
#endif
            string GetIl2CppCompilerFlags() { return null; }

        /// <summary>
        /// Extra linker flags (passed via IL2CPP) required by this package.
        /// </summary>
        /// <remarks>
        /// Usually mirrors compiler targeting flags so the linker resolves from the same sysroot.
        /// Return <c>null</c> for none.
        /// </remarks>
        /// <returns>Space-separated linker flags, or <c>null</c>.</returns>
        public
#if UNITY_STANDALONE_LINUX_API || UNITY_EMBEDDED_LINUX_API
            override
#else
            virtual
#endif
            string GetIl2CppLinkerFlags() { return null; }

        /// <summary>
        /// Default archive file name used by sysroot/toolchain packages.
        /// </summary>
        /// <remarks>
        /// The file lives under the package’s <c>data~</c> folder by convention (see <see cref="PayloadPath"/>).
        /// </remarks>
        protected string Payload => "payload.tar.7z";

        private List<PayloadDescriptor> _payloads = new List<PayloadDescriptor>();
        private InitializationStatus _initStatus = InitializationStatus.Uninitialized;

        /// <summary>
        /// Ensures all registered payloads are installed. Safe to call multiple times.
        /// </summary>
        /// <remarks>
        /// If a destination directory does not exist, the corresponding payload is extracted into it.
        /// On any failure, the method logs an error and returns <see langword="false"/>.
        /// </remarks>
        /// <returns><see langword="true"/> if initialization completed successfully; otherwise <see langword="false"/>.</returns>
        public
#if UNITY_STANDALONE_LINUX_API || UNITY_EMBEDDED_LINUX_API
            override
#else
            virtual
#endif
            bool Initialize()
        {
            if (_initStatus != InitializationStatus.Uninitialized)
                return _initStatus == InitializationStatus.Succeeded;

            foreach (PayloadDescriptor pd in _payloads)
            {
                if (!Directory.Exists(pd.dir.ToString(SlashMode.Native)) && !InstallPayload(pd))
                {
                    UnityEngine.Debug.LogError($"Failed to initialize package: {Name}");
                    _initStatus = InitializationStatus.Failed;
                    return false;
                }
            }

            _initStatus = InitializationStatus.Succeeded;
            return true;
        }

        /// <summary>
        /// Computes the absolute path to the packaged payload archive inside a UPM package.
        /// </summary>
        /// <param name="packageName">UPM package name (e.g., <c>com.unity.sdk.linux-arm64</c>).</param>
        /// <returns>Absolute path to <c>data~/payload.tar.7z</c> under the package root.</returns>
        internal NPath PayloadPath(string packageName)
        {
            return new NPath(Path.GetFullPath($"Packages/{packageName}")).Combine("data~/payload.tar.7z");
        }

        /// <summary>
        /// Registers a payload tarball and its installation directory for later initialization.
        /// </summary>
        /// <param name="packageName">UPM package name that contains the payload.</param>
        /// <param name="payloadDir">Destination directory relative to the sysroot cache (see <see cref="PayloadInstallDirectory"/>).</param>
        public void RegisterPayload(string packageName, string payloadDir)
        {
            _payloads.Add(new PayloadDescriptor{path = PayloadPath(packageName).ToString(SlashMode.Native), dir = PayloadInstallDirectory(payloadDir).ToString(SlashMode.Native)});
        }

        /// <summary>
        /// Executes a shell command synchronously.
        /// </summary>
        /// <param name="command">Command line to run. Quoting is handled by this method for platform differences.</param>
        /// <param name="workDir">Optional working directory. Defaults to <see cref="Environment.CurrentDirectory"/>.</param>
        /// <returns><see langword="true"/> if the command exits with code 0; otherwise <see langword="false"/>.</returns>
        /// <remarks>
        /// On Windows this runs under <c>cmd /c</c>; on Unix under <c>/bin/sh -c</c>.
        /// When <see cref="ShouldLogDebugMessage"/> is <see langword="true"/>, failures are logged with command details.
        /// </remarks>
        private bool RunShellCommand(string command, string workDir = null)
        {
            var p = new Process();
            p.StartInfo.UseShellExecute = false;
#if UNITY_EDITOR_WIN
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = "cmd";
            p.StartInfo.Arguments = $"/c \"{command}\"";
#else
            p.StartInfo.FileName = "/bin/sh";
            p.StartInfo.Arguments = $"-c \'{command}\'";
#endif
            p.StartInfo.WorkingDirectory = string.IsNullOrEmpty(workDir) ? Environment.CurrentDirectory : workDir;
            p.Start();
            p.WaitForExit();
            bool result = p.ExitCode == 0;
            if (!result && ShouldLogDebugMessage())
                UnityEngine.Debug.LogError($"Failed to execute command command=\"{p.StartInfo.FileName}\" arguments=\"{p.StartInfo.Arguments}\"");
            return result;
        }

        /// <summary>
        /// Creates the destination directory and extracts the payload into it.
        /// </summary>
        /// <param name="payload">Path to the compressed tarball.</param>
        /// <param name="workDir">Destination directory to extract to.</param>
        /// <returns><see langword="true"/> on success; otherwise <see langword="false"/>.</returns>
        /// <remarks>
        /// On failure the partially created directory is cleaned up. Extraction uses 7-Zip on both platforms
        /// and pipes to <c>tar</c> on Unix.
        /// </remarks>
        private bool DecompressSysroot(NPath payload, NPath workDir)
        {
            if (!RunShellCommand(CommandCreateDirectory(workDir)))
            {
                UnityEngine.Debug.LogError($"Failed to create directory {workDir}");
                return false;
            }

            if (!RunShellCommand(CommandUncompressTarball(payload, workDir), workDir.ToString(SlashMode.Native)))
            {
                UnityEngine.Debug.LogError($"Failed to uncompress payload");
                RunShellCommand(CommandRemoveDirectoryTree(workDir));
                return false;
            }

            return PostDecompressActions(workDir);
        }

        /// <summary>
        /// Installs a single payload by decompressing it into the configured destination.
        /// </summary>
        private bool InstallPayload(PayloadDescriptor pd)
        {
            return DecompressSysroot(pd.path, pd.dir);
        }

        /// <summary>
        /// Returns a platform-appropriate command to create a directory (mkdir).
        /// </summary>
        private string CommandCreateDirectory(NPath dir)
        {
            _initStatus = InitializationStatus.Failed;
#if UNITY_EDITOR_WIN
            return $"mkdir {dir.InQuotes(SlashMode.Native)}";
#else
            return $"mkdir -p {dir.InQuotes()}";
#endif
        }

        /// <summary>
        /// Returns the fully quoted path to the 7-Zip binary Unity ships with the Editor.
        /// </summary>
        private string Get7zPath()
        {
#if UNITY_EDITOR_WIN
            string command = $"{EditorApplication.sevenZipPath}";
#else
            string command = EditorApplication.sevenZipPath;
#endif
            return new NPath($"{command}").InQuotes(SlashMode.Native);
        }

        /// <summary>
        /// Builds a command that extracts <paramref name="tarball"/> into <paramref name="destDir"/>.
        /// </summary>
        /// <remarks>
        /// Windows: <c>7z x -so | 7z x -ttar -si</c> (7-Zip for both layers).<br/>
        /// Unix: <c>7z x -so | tar xf -</c> (7-Zip then system <c>tar</c>).
        /// </remarks>
        private string CommandUncompressTarball(NPath tarball, NPath destDir)
        {
#if UNITY_EDITOR_WIN
            return $"{Get7zPath()} x -y {tarball.InQuotes(SlashMode.Native)} -so | {Get7zPath()} x -y -aoa -ttar -si";
#else
            return $"{Get7zPath()} x -y {tarball.InQuotes()} -so | tar xf - --directory={destDir.InQuotes()}";
#endif
        }

        /// <summary>
        /// Returns a platform-appropriate command to remove an entire directory tree.
        /// </summary>
        private string CommandRemoveDirectoryTree(NPath dir)
        {
#if UNITY_EDITOR_WIN
             return $"rd /s /q {dir.InQuotes(SlashMode.Native)}";
#else
             return $"rm -rf {dir.InQuotes()}";
#endif
        }

        /// <summary>
        /// Hook for subclasses to perform additional fixups after extraction (e.g., symlinks, RPATH tweaks).
        /// </summary>
        /// <param name="workDir">The directory the payload was extracted to.</param>
        /// <returns><see langword="true"/> to continue initialization; <see langword="false"/> to fail.</returns>
        private bool PostDecompressActions(NPath workDir)
        {
            return true;
        }

        /// <summary>
        /// Returns the base folder used for per-user Unity data on the current OS.
        /// </summary>
        /// <remarks>
        /// macOS: <c>~/Library/Unity</c><br/>
        /// Windows/Linux: <c>%LOCALAPPDATA%/unity3d</c>
        /// </remarks>
        private string UserAppDataFolder()
        {
            return 
#if UNITY_EDITOR_OSX
                $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Library/Unity";
#else
                $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/unity3d";
#endif
        }

        /// <summary>
        /// Computes the absolute installation directory for a given payload under the sysroot cache.
        /// </summary>
        /// <param name="payloadDir">Relative path (e.g., <c>linux-arm64/&lt;version&gt;</c>).</param>
        /// <returns>Absolute cache path where the payload should be installed.</returns>
        /// <remarks>
        /// Uses the <c>UNITY_SYSROOT_CACHE</c> environment variable if set; otherwise defaults to
        /// <c>{UserAppDataFolder}/cache/sysroots</c>.
        /// </remarks>
        internal NPath PayloadInstallDirectory(string payloadDir)
        {
            string cacheDir = Environment.GetEnvironmentVariable("UNITY_SYSROOT_CACHE");
            if (string.IsNullOrEmpty(cacheDir))
                cacheDir = $"{UserAppDataFolder()}/cache/sysroots";
            return new NPath($"{cacheDir}/{payloadDir}");
        }
    }
}
