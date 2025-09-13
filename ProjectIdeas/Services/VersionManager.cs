using System;

namespace ProjectIdeas.Services
{
    public static class VersionManager
    {
        public static Version CurrentVersion { get; } = new Version(2, 0, 0);
        
        public static string GetVersionString() => CurrentVersion.ToString();
        
        public static string GetFileNameWithVersion(string baseName) =>
            $"{baseName}-v{GetVersionString()}";
    }
}