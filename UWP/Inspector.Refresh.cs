namespace Zebble.UWP
{
    using System;
    using System.Threading.Tasks;
    using System.Linq;

    partial class Inspector : IInspector
    {
        static DateTime LastDomUpdated, LastTreeUpdated;

        public Inspector()
        {
            Nav.Navigated.Handle(OnNavigated);
            Thread.Pool.RunOnNewThread(Watch);
        }

        public Task DomUpdated(View view)
        {
            if (!IsOpen()) return Task.CompletedTask;
            if (view.WithAllParents().Lacks(View.Root)) return Task.CompletedTask;

            LastDomUpdated = DateTime.UtcNow;

            return Task.CompletedTask;
        }

        bool IsOpen() => InspectionBox?.Ignored == false;

        async Task Watch()
        {
            while (true)
            {
                var moment = DateTime.UtcNow;

                await Task.Delay(500);

                if (LastDomUpdated > moment) continue;
                if (LastTreeUpdated > LastDomUpdated) continue;
                if (!IsOpen()) continue;

                LastTreeUpdated = DateTime.UtcNow;
                await InspectionBox.CreateTreeView();
                await InspectionBox.SelectCurrentNode();
                await InspectionBox.PropertiesScroller.Load();
            }
        }

        async Task OnNavigated()
        {
            try
            {
                if (!IsOpen()) return;
                if (SkipPageRefresh) return;

                HideHighlighters();

                CurrentViewPath = null;
                await (InspectionBox.PropertiesScroller?.Reset()).OrCompleted();
            }
            catch (Exception ex)
            {
                await Alert.Show("Internal error: " + ex.Message).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        /// <summary>
        /// This method will find the most recent instance
        /// of the selected path
        /// </summary>
        /// <returns></returns>
        View GetCurrentView()
        {
            var currentViewPath = Current.CurrentViewPath;
            if (currentViewPath == null) return null;

            var tree = InspectionBox.Tree;
            if (tree == null) return null;

            var allNodes = tree.AllNodes.ExceptNull().ToArray();
            var currentView = allNodes
                .Select(x => x.Source is View view ? view : null)
                .Where(x => x != null)
                .FirstOrDefault(x => x.GetFullyQualifiedPath() == currentViewPath);

            return currentView;
        }
    }
}
