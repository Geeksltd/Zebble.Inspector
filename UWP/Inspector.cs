namespace Zebble.UWP
{
    using System;
    using System.Threading.Tasks;
    using Olive;
    using Windows.UI.ViewManagement;

    public partial class Inspector : IInspector
    {
        internal static Inspector Current => UIRuntime.Inspector as Inspector;

        internal InspectionBox InspectionBox;

        internal string CurrentViewPath;
        internal View CurrentView => GetCurrentView();

        public bool IsRotating { get; set; }

        public async Task Load(View view = null)
        {
            try
            {
                if (InspectionBox.Ignored)
                {
                    CurrentViewPath = null;

                    await Start();
                }

                await LoadEnsured(view);
            }
            catch (Exception ex)
            {
                await Alert.Show("Internal error occurred: " + ex.Message)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        async Task LoadEnsured(View view)
        {
            CurrentViewPath = view?.GetFullyQualifiedPath();

            await HighlightItem();
            await InspectionBox.SelectCurrentNode();
            await InspectionBox.PropertiesScroller.Load();
        }

        async Task Start()
        {
            await CreateHighlightMask();
            await Resize();
        }

        internal async Task Resize()
        {
            await InspectionBox.IgnoredAsync(false);
            // +1 makes sure the layout will be updated
            await ResizeWindow(View.Root.ActualWidth + InspectionBox.WIDTH + 1);
        }

        static Task ResizeWindow(double width)
        {
            var source = new TaskCompletionSource<bool>();
            var done = false;

            Thread.UI.Post(async () =>
            {
                var appView = ApplicationView.GetForCurrentView();
                var newSize = new Size((float)width, (float)appView.VisibleBounds.Height);

                void Changed(ApplicationView _, object __)
                {
                    done = true;
                    appView.VisibleBoundsChanged -= Changed;
                    source.TrySetResult(result: true);
                }

                appView.VisibleBoundsChanged += Changed;
                appView.TryResizeView(new Windows.Foundation.Size(newSize.Width, newSize.Height));

                await Task.Delay(2.Seconds());
                if (!done) source.TrySetResult(result: false);
            });

            return source.Task;
        }

        public async Task Collapse()
        {
            if (!UIRuntime.IsDevMode) return;

            LastDomUpdated = DateTime.MinValue;
            LastTreeUpdated = DateTime.MinValue;

            await InspectionBox.IgnoredAsync();
            new[] { HighlightBorder, HighlightMask }.Do(x => x.Hide());
            await ResizeWindow(Device.Screen.Width);
        }

        public async Task PrepareRuntimeRoot()
        {
            CreateStyles();

            var wrapper = new Stack(RepeatDirection.Horizontal).Id("RenderRootStack");

            UIRuntime.PageContainer = UIRuntime.RenderRoot.Id("PageContainer");

            await wrapper.Add(View.Root.Height(Device.Screen.Height));
            await wrapper.Add(InspectionBox = new InspectionBox { Id = "ZebbleInspectionBox" }, awaitNative: true);
            await InspectionBox.Initialize();

            UIRuntime.RenderRoot = wrapper;
            wrapper.IsAddedToNativeParentOnce = true;
        }
    }
}