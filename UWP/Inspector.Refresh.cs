namespace Zebble.UWP
{
    using System;
    using System.Threading.Tasks;

    partial class Inspector : IInspector
    {
        static DateTime LastDomUpdated;
        static DateTime LastTreeUpdated;

        Inspector()
        {
            Nav.Navigated.Handle(OnNavigated);
            Device.ThreadPool.RunOnNewThread(Watch);
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

                await Task.Delay(100);

                if (LastDomUpdated > moment) continue;
                if (LastTreeUpdated > LastDomUpdated) continue;
                if (!IsOpen()) continue;

                LastTreeUpdated = DateTime.UtcNow;
                await InspectionBox.CreateTreeView();
            }
        }

        async Task OnNavigated()
        {
            try
            {
                if (!IsOpen()) return;

                HideHighlighters();

                CurrentView = null;
                await (InspectionBox.PropertiesScroller?.ClearChildren()).OrCompleted();
            }
            catch (Exception ex)
            {
                await Alert.Show("Internal error: " + ex.Message).ConfigureAwait(continueOnCapturedContext: false);
            }
        }
    }
}
