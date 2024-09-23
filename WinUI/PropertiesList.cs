namespace Zebble.WinUI
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Olive;

    class PropertiesList : Stack
    {
        Inspector.PropertySettings[] CurrentSettings;

        string CurrentCss;
        View View;
        Stack CssStak = new Stack(RepeatDirection.Vertical);
        string[] SearchKeywords;

        TextView TypeInfo = new TextView().TextColor("#888").Background("#333").Padding(5).Margin(bottom: 5);
        TextInput AttributeFilter = new TextInput { Placeholder = "Search..." };
        Button OpenInVSButton;
        PropertiesRecyclerList Properties = new PropertiesRecyclerList();

        internal AsyncEvent<bool> ScrollEnabledChange = new AsyncEvent<bool>();

        public async Task Load(View view)
        {
            View = view;

            CurrentCss = view.CurrentlyAppliedCss;

            if (!CssStak.AllChildren.None())
                await CssStak.ClearChildren();

            foreach (var css in CurrentCss.Split("🗋").Trim())
                await AddCssTextBox(css);

            TypeInfo.Text = "Type ➝ " + view.GetType().WithAllParents()
              .TakeWhile(x => x != typeof(View))
              .Select(x => x.GetProgrammingName(useGlobal: false, useNamespace: false, useNamespaceForParams: false))
              .ToString("\r\n➤ ");

            CurrentSettings = Inspector.GetSettings(View).ToArray();

            await EnsureProperties();

            OpenInVSButton.Enabled = Inspector.Current.CurrentView?.Page?.GetType() == Inspector.Current.CurrentView?.GetType();
            UpdateOpenInVSButtonColor();
        }

        public async Task Reset()
        {
            TypeInfo.Text = "";
            CurrentSettings = new Inspector.PropertySettings[0];
            await EnsureProperties();
        }

        public override async Task OnInitializing()
        {
            await base.OnInitializing();
            await CreateComponents();
        }

        async Task CreateComponents()
        {
            var buttons = new Stack(RepeatDirection.Horizontal);
            await Add(buttons);
            await buttons.Add(CreateDeleteButton());
            await buttons.Add(CreateBringToFrontButton());
            await buttons.Add(CreateSendToBackButton());

            await buttons.Add(await CreateOpenInVSButton());

            await Add(TypeInfo);
            await AddCss();
            await AddAttributeFilter();
            await Add(Properties);
        }

        async Task AddAttributeFilter()
        {
            AttributeFilter.Margin(top: 10)
                .Padding(5)
                .Border(0)
                .Background(color: "#333")
                .Font(12, color: "#aaa")
                .On(x => x.UserTextChanged, FilterAttributes);

            await Add(AttributeFilter);
        }

        async Task CreateVsIconAndText(string file)
        {
            var path = file.TrimStart("Styles/", caseSensitive: false).Reverse().ToString("").Summarize(30).Reverse().ToString("");

            var stack = new Stack(RepeatDirection.Horizontal);
            await stack.Add(new TextView().Text(path).Font(11.5f).TextColor("#bbb").Margin(bottom: 5));
            await stack.Add(CreateVsIcon(file));
            await CssStak.Add(stack);
        }

        ImageView CreateVsIcon(string file)
        {
            var img = GetType().Assembly.ReadEmbeddedResource("Zebble", "Resources.VS.png");
            var vsIcon = new ImageView().Id("VsButton").Size(15).Alignment(Alignment.Right).Margin(bottom: 5);
            vsIcon.BackgroundImageData = img;
            vsIcon.Tapped.Handle(() => LoadInVisualStudio(GetSCSSFileLocation(file)));
            return vsIcon;
        }

        string GetSCSSFileLocation(string file)
        {
            return System.IO.Path.Combine(Helper.GetAppUIPath(), file.RemoveFrom(":").Replace("/", "\\"));
        }

        async Task LoadInVisualStudio(string cssSource) => await Helper.LoadInVisualStudio(cssSource);

        async Task AddCssTextBox(string css)
        {
            try
            {
                var lines = css.ToLines();
                if (!lines.HasMany()) return;

                await CreateVsIconAndText(lines[0]);

                css = css.Remove(lines[0]).Trim();
                await CssStak.Add(new TextView(css).Background(color: "#333").Padding(5).Font(11.5f, color: "#fda").Border(0).Margin(bottom: 10).Set(x => x.Height.MinLimit = 40));
            }
            catch (Exception ex)
            {
                var error = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }
        }

        async Task AddCss()
        {
            await Add(new TextView("CSS").ChangeInBatch(x => x.Font(20).Margin(vertical: 10)));
            await Add(CssStak);
        }

        Task EnsureProperties()
        {
            var settings = CurrentSettings;

            if (SearchKeywords?.Any() == true)
                settings = settings
                    .Where(x => (x.Group + " " + x.Label).ContainsAll(SearchKeywords, caseSensitive: false))
                    .ToArray();

            return Properties.Load(settings);
        }

        Task FilterAttributes()
        {
            SearchKeywords = AttributeFilter.Text.OrEmpty().Split(' ');
            return EnsureProperties();
        }

        Button CreateDeleteButton()
        {
            var result = new Button { Text = "Delete" }.TextColor(Colors.Red).Margin(5).Padding(0).Height(null);

            result.Tapped
                .Handle(async () =>
            {
                var toSelect = View.Parent;
                await View.RemoveSelf();
                await Inspector.Current.Load(toSelect);
            });

            return result;
        }

        Button CreateBringToFrontButton()
        {
            var result = new Button { Text = "↥ Front" }.TextColor(Colors.LightGreen).Margin(5).Padding(0);
            result.Tapped.Handle(() => View.BringToFront());
            return result;
        }

        Button CreateSendToBackButton()
        {
            var result = new Button { Text = "↧ Back" }.TextColor(Colors.LightBlue).Margin(5).Padding(0);
            result.Tapped.Handle(() => View.SendToBack());
            return result;
        }

        async Task<Stack> CreateOpenInVSButton()
        {
            var result = new Stack(RepeatDirection.Horizontal);
            OpenInVSButton = new Button { Text = "Open" }.TextColor(Colors.LightBlue).Margin(5).Padding(0);

            result.Tapped
                .Handle(async () =>
            {
                if (!OpenInVSButton.Enabled) return;
                var appUiFolder = Helper.GetAppUIPath();
                var sourceCodeAttr = Helper.GetSourceCodeAttribute(Inspector.Current.CurrentView.GetType());
                await Helper.LoadInVisualStudio(System.IO.Path.Combine(appUiFolder, sourceCodeAttr));
            });

            OpenInVSButton.Enabled = false;

            var img = GetType().Assembly.ReadEmbeddedResource("Zebble", "Resources.VS.png");
            var vsIcon = new ImageView().Id("VsButton").Size(12, 12).Alignment(Alignment.Right).Margin(top: 5, left: 5);
            vsIcon.BackgroundImageData = img;

            await result.Add(vsIcon);
            await result.Add(OpenInVSButton);
            return result;
        }

        void UpdateOpenInVSButtonColor()
        {
            OpenInVSButton.TextColor = OpenInVSButton.Enabled ? Colors.LightBlue : Color.Parse("#3F4254");
        }
    }
}