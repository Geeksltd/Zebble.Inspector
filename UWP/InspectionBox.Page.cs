namespace Zebble.UWP
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.Web.Http;

    partial class InspectionBox
    {
        internal async Task CreateTreeView()
        {
            if (TreeScroller == null) return;

            // Remove the old tree.
            await TreeScroller.ClearChildren();

            Tree = await TreeScroller.Add(new TreeView());

            foreach (var item in Root.AllChildren)
                await Tree.AddNode(new TreeView.Node(item)); // Recursively creates child nodes

            foreach (var item in Tree.AllNodes)
            {
                // Enable tap on the items:
                item.Tapped.HandleOn(Thread.UI, p => Inspector.Current.Load(item.Source as View));
            }
        }

        async Task LoadInVisualStudio(Type type)
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(new Uri("http://localhost:19778/Zebble/VSIX/?ver=" + Guid.NewGuid() + "&type=" + type.FullName));
                }
                catch (Exception ex)
                {
                    var error = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
                }
            }
        }

        internal async Task SelectCurrentNode()
        {
            var currentView = Inspector.Current.CurrentView;
            if (currentView == null || Tree == null) return;

            var path = currentView.WithAllParents().ToList();

            // Expand the path to the currently selected item:
            foreach (var node in Tree.AllNodes.ExceptNull().ToArray())
                if (node.Source is View nodeView && path.Contains(nodeView)) await node.Expand();

            await HighlightSelectedNode();
        }

        async Task HighlightSelectedNode()
        {
            var currentView = Inspector.Current.CurrentView;
            if (currentView == null) return;

            foreach (var view in Tree.AllNodes.Except(x => x.Source == currentView).Select(x => x.View))
                view.Perform(x => x.Style.TextColor = null);

            var selectedNode = Tree.AllNodes.FirstOrDefault(x => x.Source as View == currentView);

            if (selectedNode?.View != null)
            {
                void highlight() => selectedNode.View.Style.TextColor("#ff6");
                await selectedNode.View.WhenShown(highlight);
            }
        }
    }
}