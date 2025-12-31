using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace DraftSpec.Scripting;

/// <summary>
/// Globals object passed to Roslyn scripts for state sharing.
/// </summary>
public class ScriptGlobals
{
    /// <summary>
    /// Action to capture the root context after spec definitions.
    /// Called at the end of the script to transfer state back to the host.
    /// </summary>
    public Action<SpecContext?>? CaptureRootContext { get; set; }
}
