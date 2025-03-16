using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MethodTrackerVisualizer.Helpers;

public static class UiExtensions
{
    public static void ExpandAllParents(this ItemsControl container, object dataItem)
    {
        while (true)
        {
            var tvi = container.GetTreeViewItem(dataItem);
            if (tvi != null)
            {
                tvi.IsExpanded = true;
                if (tvi.GetParent() is { } parent)
                {
                    container = parent;
                    dataItem = parent.DataContext;
                    continue;
                }
            }

            break;
        }
    }

    public static TreeViewItem GetTreeViewItem(this ItemsControl container, object dataItem)
    {
        if (container.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
        {
            container.UpdateLayout();
        }

        if (container.ItemContainerGenerator.ContainerFromItem(dataItem) is TreeViewItem tvi)
        {
            return tvi;
        }
        foreach (var item in container.Items)
        {
            if (container.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem parentContainer)
            {
                parentContainer.IsExpanded = true; // Ensure children are generated.
                var childTvi = parentContainer.GetTreeViewItem(dataItem);
                if (childTvi != null)
                {
                    return childTvi;
                }
            }
        }
        return null;
    }

    public static ItemsControl GetParent(this TreeViewItem item)
    {
        var parent = VisualTreeHelper.GetParent(item);
        while (parent is not null and not TreeViewItem and not TreeView)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }
        return parent as ItemsControl;
    }

    public static void ExpandExpanderForEntry(this TreeViewItem tvi)
    {
        var expander = FindVisualChild<Expander>(tvi);
        if (expander != null)
        {
            expander.IsExpanded = true;
        }
    }

    public static T FindVisualChild<T>(this DependencyObject parent) where T : DependencyObject
    {
        if (parent == null)
            return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T tChild)
                return tChild;

            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }
}