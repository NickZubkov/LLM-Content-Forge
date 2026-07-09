using System.Runtime.CompilerServices;

// The EditMode test assembly lives separately, so expose internal pipeline types to it.
[assembly: InternalsVisibleTo("ContentForge.Editor.Tests")]
