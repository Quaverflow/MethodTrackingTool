using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;

namespace MethodTrackerVisualizer;
/// <summary>
/// This class implements the tool window exposed by this package and hosts a user control.
/// </summary>
/// <remarks>
/// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
/// usually implemented by the package implementer.
/// <para>
/// This class derives from the ToolWindowPane class provided from the MPF in order to use its
/// implementation of the IVsUIElementPane interface.
/// </para>
/// </remarks>
[Guid("4c711c01-7ce8-4057-9a49-d8478cf3f96d")]
public class MethodDumpReader : ToolWindowPane
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MethodDumpReader"/> class.
    /// </summary>
    public MethodDumpReader() : base(null)
    {
        this.Caption = "MethodDumpReader";

        // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
        // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
        // the object returned by the Content property.
        this.Content = new Components.MethodDumpReader();
    }
}
