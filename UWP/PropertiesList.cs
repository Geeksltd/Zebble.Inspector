namespace Zebble.UWP
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    class PropertiesList : Stack
    {
        Inspector.PropertySettings[] CurrentSettings;

        string CurrentCss;
        View View;
        string[] SearchKeywords;
        TextInput CssTextbox = new TextInput { Lines = 3 }.Background(color: "#333").Padding(5).Font(11, color: "#7da").Border(0);
        TextView TypeInfo = new TextView().TextColor("#888").Background("#333").Padding(5).Margin(bottom: 5);
        TextInput AttributeFilter = new TextInput { Placeholder = "Search..." };

        PropertiesRecyclerList Properties = new PropertiesRecyclerList();

        internal AsyncEvent<bool> ScrollEnabledChange = new AsyncEvent<bool>();

        public async Task Load(View view)
        {
            View = view;

            CurrentCss = CurrentCss = view.CurrentlyAppliedCss;
            CssTextbox.Text = CurrentCss;
            CssTextbox.Height.Update();
            TypeInfo.Text = "Type ➝ " + view.GetType().WithAllParents()
              .TakeWhile(x => x != typeof(View))
              .Select(x => x.GetProgrammingName(useGlobal: false, useNamespace: false, useNamespaceForParams: false))
              .ToString("\r\n➤ ");

            CurrentSettings = Inspector.GetSettings(View).ToArray();

            await EnsureProperties();
        }

        public async Task Reset()
        {
            CssTextbox.Text = TypeInfo.Text = "";
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
            await Add(TypeInfo);
            await AddCss();
            await AddAttributeFilter();
            // await Add(PropertiesContainer);
            await Add(Properties);

            Thread.UI.RunAction(() =>
           (CssTextbox.Native() as Windows.UI.Xaml.Controls.Border)
               .Get(x => x?.Child as Windows.UI.Xaml.Controls.TextBox)
               .Perform(x => x.IsReadOnly = true));
        }

        async Task AddAttributeFilter()
        {
            AttributeFilter.Margin(top: 10).Padding(5).Border(0).Background(color: "#333").Font(12, color: "#aaa")
                   .On(x => x.UserTextChanged, FilterAttributes);

            await Add(AttributeFilter);
        }

        async Task AddCss()
        {
            await Add(new TextView("Applicable CSS Rules").ChangeInBatch(x => x.Font(20).Margin(vertical: 10)));
            await Add(CssTextbox);

            CssTextbox.Height.BindTo(CssTextbox.Padding.Top, CssTextbox.Padding.Bottom,
                (pt, pb) => (CalculateCssContentHeight() + pt + pb).LimitMax(500));
        }

        float CalculateCssContentHeight()
        {
            return CssTextbox.Font.GetTextHeight(CssTextbox.ActualWidth, CurrentCss) * 1.05f;
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
            var result = new Button { Text = "Delete" }.TextColor(Colors.Red).Margin(10).Padding(0).Height(null);

            result.Tapped.Handle(async () =>
            {
                var toSelect = View.Parent;
                await View.RemoveSelf();
                await Inspector.Current.Load(toSelect);
            });

            return result;
        }

        Button CreateBringToFrontButton()
        {
            var result = new Button { Text = "↥ Front" }.TextColor(Colors.LightGreen).Margin(10).Padding(0);
            result.Tapped.Handle(() => View.BringToFront());
            return result;
        }

        Button CreateSendToBackButton()
        {
            var result = new Button { Text = "↧ Back" }.TextColor(Colors.LightBlue).Margin(10).Padding(0);
            result.Tapped.Handle(() => View.SendToBack());
            return result;
        }
    }
}