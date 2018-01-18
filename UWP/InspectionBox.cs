namespace Zebble.UWP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Services;

    partial class InspectionBox : Stack
    {
        internal const int WIDTH = 600;
        const string SELECTED = "#43aaa9";

        Stack HeaderBar;
        internal ScrollView TreeScroller, PropertiesScroller, DeviceScroller;
        TreeView Tree;
        Button CloseButton;
        Stack Row;
        View PageButton, DeviceButton;
        List<View> PageTabs = new List<View>();
        List<View> DeviceTabs = new List<View>();

        public override async Task OnInitializing()
        {
            await base.OnInitializing();
            Height.BindTo(Root.Height);

            await Add(HeaderBar = new Stack(RepeatDirection.Horizontal).Id("HeaderBar"));

            Row = await Add(new Stack(RepeatDirection.Horizontal).Id("Row"));
            Row.Height.BindTo(Height, HeaderBar.Height, (x, y) => x - y);

            await AddPageContainer();

            Shown.Handle(OnShown);
        }

        internal async Task AddPageContainer()
        {
            if (TreeScroller != null) return;

            if (DeviceScroller != null)
            {
                await DeviceScroller.parent.Remove(DeviceScroller);
                DeviceScroller = null;
            }

            TreeScroller = await Row.Add(new ScrollView().Id("TreeScroller").Width(60.Percent())
                .Background(color: "#333"));

            PropertiesScroller = await Row.Add(new ScrollView().Width(40.Percent()).Background(color: "#222"));

            new[] { TreeScroller, PropertiesScroller }.Do(x =>
            {
                x.ShowHorizontalScrollBars = x.ShowVerticalScrollBars = true;
                x.Padding(10).Height(100.Percent());
                x.ShowVerticalScrollBars = false;
            });

            await CreateTreeView();

            if (PageTabs.Count > 0)
                PageTabs[1]?.Margin(left: 320);

            CloseButton?.X(570);
            PageTabs.ForEach(view => { view.Visible = true; });
            DeviceTabs.ForEach(view => { view.Visible = false; });

            PageButton?.Background(color: DeviceScroller == null ? SELECTED : "#444");
            DeviceButton?.Background(color: DeviceScroller != null ? SELECTED : "#444");
        }

        internal async Task AddDeviceContainer()
        {
            if (DeviceScroller != null) return;

            if (TreeScroller != null && PropertiesScroller != null)
            {
                await TreeScroller.parent.Remove(TreeScroller);
                await PropertiesScroller.parent.Remove(PropertiesScroller);

                TreeScroller = null;
                PropertiesScroller = null;
            }

            DeviceScroller = await Row.Add(new ScrollView().Id("DeviceScroller").Background(color: "#111"));

            new[] { DeviceScroller }.Do(x =>
            {
                x.ShowHorizontalScrollBars = x.ShowVerticalScrollBars = true;
                x.Padding(10).Height(100.Percent());
                x.ShowVerticalScrollBars = false;
            });

            new[] { DeviceScroller }.Do(x =>
            {
                x.Padding(0);
            });

            await LoadDevice();

            if (PageTabs.Count > 0)
                PageTabs[1]?.Margin(left: 0);

            CloseButton?.X(570);
            DeviceTabs.ForEach(view => { view.Visible = true; });
            PageTabs.ForEach(view => { view.Visible = false; });

            DevicePanel.HeaderButtons.AddRange(new List<View> { DeviceTabs[DeviceTabs.Count - 1], DeviceTabs[DeviceTabs.Count - 2], DeviceTabs[DeviceTabs.Count - 3] });
            await DevicePanel.Activate();

            PageButton?.Background(color: DeviceScroller == null ? SELECTED : "#444");
            DeviceButton?.Background(color: DeviceScroller != null ? SELECTED : "#444");
        }

        async Task OnShown()
        {
            PageButton = CreateButton("Page.png").On(x => x.Tapped, async () => { await AddPageContainer(); }).Background(color: SELECTED);
            DeviceButton = CreateButton("Device.png").On(x => x.Tapped, async () => { await AddDeviceContainer(); }).Background(color: "#444");

            await HeaderBar.Add(PageButton);
            await HeaderBar.Add(DeviceButton);

            await HeaderBar.Add(new Canvas().Id("Push to right").Height(100.Percent()));

            PageTabs.AddRange(new List<View> {
                CreateButton("Refresh.png")
                .On(x => x.Tapped, () => { Nav.DisposeCache(); return Nav.Reload(); }).Absolute().X(350),
                new TextView("CSS platform: ") { AutoSizeWidth = true }.Margin(left:320)
                .TextColor("#999")
                .TextAlignment(Alignment.Middle)
                .Height(100.Percent()),
                await CreateDevicePicker()
            });

            var geoLocationButton = CreateButton("Earth.png").Id("Earth").Background(color: "#444");
            geoLocationButton.On(x => x.Tapped, () =>
            {
                DevicePanel?.CreateGeoLocationForm(WIDTH - 300, 1);
                geoLocationButton.Background(color: DevicePanel?.GeoLocationForm == null ? "#444" : SELECTED);
            });

            DeviceTabs.AddRange(new List<View> {
                CreateButton("Back.png")
                .On(x => x.Tapped, Nav.OnHardwareBack),
                CreateButton("Shake.png")
                .On(x => x.Tapped, () => Device.Accelerometer.DeviceShaken.RaiseOn(Thread.Pool)),
                CreateButton("Rotation.png")
                .On(x => x.Tapped, async () =>
                {
                   Inspector.Current.IsRotating = true;

                    var oldWidth = Device.Screen.Width;
                    var oldHeight = Device.Screen.Height;

                    await Device.Screen.ConfigureSize(() => oldHeight, () => oldWidth);
                    Inspector.Current.IsRotating  = false;

                    await Inspector.Current.Resize();
                }),
                CreateButton("Warning.png").On(x=>x.Tapped,()=>
                    Thread.UI.Run(()=> Device.App.RaiseReceivedMemoryWarning())),
                new TextView().Width(8).Height(100.Percent()).Border(color:"#777", left:1),
                geoLocationButton
    });

            await HeaderBar.AddRange(PageTabs);
            await HeaderBar.AddRange(DeviceTabs);

            await HeaderBar.Add(CloseButton = new Button { Id = "Close", Text = "X", AutoSizeWidth = true }
                   .Padding(horizontal: 8).Height(100.Percent()).TextAlignment(Alignment.Middle)
                   .On(x => x.Tapped, Inspector.Current.Collapse));

            CloseButton.X(570);

            DeviceTabs.ForEach(view => { view.Visible = false; });
        }

        View CreateButton(string name)
        {
            return new ImageView
            {
                ImageData = GetType().GetAssembly()
                .ReadEmbeddedResource("Zebble.UWP.Resources." + name)
            }.Size(32).Padding(6);
        }

        async Task<View> CreateDevicePicker()
        {
            var result = new Stack(RepeatDirection.Horizontal).Margin(left: 5).Height(100.Percent());

            foreach (var platform in GetStandardDevices())
            {
                var button = await result.Add(CreateButton(platform.Platform == DevicePlatform.Windows ? "Windows.png" :
                    platform.Platform == DevicePlatform.Android ? "Android.png" : "IOS.png")
                .Background(color: platform.Platform == DevicePlatform.Windows ? SELECTED : "#666")
                .Height(100.Percent()));

                button.Tapped.Handle(async () =>
                {
                    result.AllChildren.Except(button).Do(x => x.Background("#666"));
                    button.Background(SELECTED);
                    UIRuntime.RenderRoot.CssClass = platform.RootCss;
                    CssEngine.Platform = platform.Platform;

                    await Nav.FullRefresh();
                });
            }

            return result;
        }

        static DeviceSpec[] GetStandardDevices()
        {
            return new[] {
              new DeviceSpec(DevicePlatform.Windows,  "Windows", "windows-only" ),
               new DeviceSpec(DevicePlatform.Android,"Android", "android-only" ),
               new DeviceSpec(DevicePlatform.IOS,"iOS", "ios-only" ),
           };
        }

        class DeviceSpec
        {
            public string Name, RootCss;
            public DevicePlatform Platform;

            public DeviceSpec(DevicePlatform platform, string name, string css)
            {
                Platform = platform;
                Name = name;
                RootCss = css;
            }

            public override string ToString() => Name;
        }
    }
}