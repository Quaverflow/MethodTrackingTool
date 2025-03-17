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
public class MethodTrackerReader : ToolWindowPane
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MethodTrackerReader"/> class.
    /// </summary>
    public MethodTrackerReader() : base(null)
    {
        this.Caption = "MethodTrackerReader";
        this.Content = new Components.MethodTrackerReader();
    }
}
