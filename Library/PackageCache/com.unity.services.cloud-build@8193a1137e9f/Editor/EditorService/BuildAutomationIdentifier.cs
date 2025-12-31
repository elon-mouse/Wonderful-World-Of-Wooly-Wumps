using Unity.Services.Core.Editor;

namespace Unity.Services.CloudBuild.Editor
{
    /// <summary>
    /// Implementation of the <see cref="IEditorGameServiceIdentifier"/> for the Cloud Build package
    /// </summary>
    public struct BuildAutomationIdentifier : IEditorGameServiceIdentifier
    {
        /// <summary>
        /// Gets the key for the Cloud Build package
        /// </summary>
        /// <returns>The key for the service</returns>
        public string GetKey() => "Build";
    }
}
