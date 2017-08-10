namespace Zebble.UWP
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.UI.ViewManagement;

    public partial class Inspector : IInspector
    {
        internal static Inspector Current => UIRuntime.Inspector as Inspector;

        internal InspectionBox InspectionBox;
        internal View CurrentView;
        public bool IsRotating { get; set; }

        public async Task Load(View view)
        {
            if (view == null) return;

            try
            {
                var objectPath = view.GetFullPath();

                if (view.GetAllParents().Lacks(View.Root)) return;

                if (InspectionBox.Ignored)
                {
                    CurrentView = null;

                    await Start();

                    for (var retries = 25; retries > 0; retries--)
                    {
                        if (InspectionBox.Ignored || CurrentView != null) return;
                        view = Nav.CurrentPage?.CurrentDescendants().FirstOrDefault(v => v.GetFullPath() == objectPath);
                        if (view != null) break;
                    }

                    await Load(view);
                }
                else
                {
                    CurrentView = view;

                    await HighlightItem();
                    await InspectionBox.SelectCurrentNode();
                }
            }
            catch (Exception ex)
            {
                await Alert.Show("Internal error occured: " + ex.Message)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        async Task Start()
        {
            await CreateHighlightMask();
            await Resize();
        }

        internal async Task Resize()
        {
            await ResizeWindow(View.Root.ActualWidth + InspectionBox.WIDTH);
            InspectionBox.Style.Ignored = false;
        }

        static Task ResizeWindow(double width)
        {
            var source = new TaskCompletionSource<bool>();
            var done = false;

            Device.UIThread.Post(async () =>
           {
               var appView = ApplicationView.GetForCurrentView();

               var newSize = new Size((float)width, (float)appView.VisibleBounds.Height);

               Windows.Foundation.TypedEventHandler<ApplicationView, object> changed = null;

               changed = (_, __) =>
               {
                   done = true;
                   appView.VisibleBoundsChanged -= changed;
                   source.TrySetResult(result: true);
               };

               appView.VisibleBoundsChanged += changed;

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

            InspectionBox.Style.Ignored = true;
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