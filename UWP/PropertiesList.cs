namespace Zebble.UWP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.System;

    class PropertiesList : Stack
    {
        Inspector.PropertySettings[] CurrentSettings;

        string CurrentCss;
        View View;
        string[] SearchKeywords;
        TextInput CssTextbox = new TextInput { Lines = 3 }.Background(color: "#333").Padding(5).Font(11, color: "#7da").Border(0);
        TextView TypeInfo = new TextView().TextColor("#888").Background("#333").Padding(5).Margin(bottom: 5);
        TextInput AttributeFilter = new TextInput { Placeholder = "Search..." };
        Stack PropertiesContainer = new Stack();
        Dictionary<string, PropertyFormField> Fields = new Dictionary<string, PropertyFormField>();

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
            await AddCss();
            await AddAttributeFilter();
            await Add(PropertiesContainer);

            Thread.UI.RunAction(() =>
           (CssTextbox.Native() as Windows.UI.Xaml.Controls.Border)
               .Get(x => x?.Child as Windows.UI.Xaml.Controls.TextBox)
               .Perform(x => x.IsReadOnly = true));

            await Add(CreateDeleteButton());
            await Add(CreateBringToFrontButton());
            await Add(CreateSendToBackButton());
            await Add(TypeInfo);
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

        bool ShouldIgnore(PropertyFormField item)
        {
            if (CurrentSettings.Lacks(item.Item)) return true;
            if (SearchKeywords.None()) return false;
            return !item.SearchKeywords.ContainsAll(SearchKeywords, caseSensitive: false);
        }

        bool ShouldIgnore(string group)
        {
            if (CurrentSettings.Select(x => x.Group).Lacks(group)) return true;
            if (SearchKeywords.None()) return false;
            return Fields.Where(x => x.Value.Group == group).All(x => x.Value.Field.Ignored);
        }

        Task EnsureProperties()
        {
            return UIWorkBatch.Run(async () =>
           {
               var validGroups = CurrentSettings.Select(x => x.Group).ToArray();

               // Old ones to ignore:
               foreach (var view in Fields.Where(x => ShouldIgnore(x.Value)))
                   await view.Value.Field.IgnoredAsync();

               foreach (var group in CurrentSettings.GroupBy(x => x.Group))
               {
                   await AddOrReviveGroup(group.Key);

                   foreach (var item in group)
                       await AddOrReviveField(item);
               }

               var notRelevant = PropertiesContainer.AllChildren.OfType<GroupView>().Where(x => ShouldIgnore(x.Text)).ToArray();
               foreach (var view in notRelevant) await view.IgnoredAsync();
           });
        }

        Task FilterAttributes()
        {
            SearchKeywords = AttributeFilter.Text.OrEmpty().Split(' ');
            return EnsureProperties();
        }

        async Task AddOrReviveGroup(string group)
        {
            var view = PropertiesContainer.AllChildren.OfType<GroupView>().FirstOrDefault(v => v.Text == group);

            if (view != null) await view.IgnoredAsync(false);
            else await PropertiesContainer.Add(new GroupView(group));
        }

        async Task AddOrReviveField(Inspector.PropertySettings item)
        {
            var field = Fields.GetOrDefault(item.Key);

            if (field != null) await field.Field.IgnoredAsync(false);
            else
            {
                field = Fields[item.Key] = await Create(item);

                if (field.Field.GetControl() is TextInput it)
                    it.UserFocusChanged.Handle(focused => ScrollEnabledChange.Raise(!focused));
                await PropertiesContainer.Add(field.Field);
            }

            field.Load(item);

            if (ShouldIgnore(field))
                await field.Field.IgnoredAsync();
        }

        public static async Task<PropertyFormField> Create(Inspector.PropertySettings item)
        {
            var field = CreateField(item);

            var result = new PropertyFormField(field);

            var labelText = item.Label.TrimStart(item.Group).ToLiteralFromPascalCase();

            field.Set(x => x.Direction = RepeatDirection.Horizontal).ChangeInBatch(x => x.Height(20).Margin(bottom: 1));
            field.Label.ChangeInBatch(x => x.Text(labelText).Font(12, color: "#aaa").Padding(3).Width(50.Percent()));

            // if (item.Instance is Gap)
            //    await field.Add(GetTrackerIcon(item.View.Css, item.View.Style));

            return result;
        }

        static FormField CreateField(Inspector.PropertySettings prop)
        {
            if (prop.Property.CanWrite == false)
                return new FormField<TextView> { Value = prop.ExistingValue }
                .Set(x => x.Control.TextColor(Colors.Yellow));

            if (prop.Property.PropertyType == typeof(bool))
            {
                var result = new FormField<CheckBox>();
                result.Control.Checked = prop.ExistingValue.ToStringOrEmpty().TryParseAs<bool>() ?? false;
                result.Control.CheckedChanged.Handle(() => UpdateValue(result, result.Control.Checked.ToString()));
                return result;
            }

            return CreateTextField(prop);
        }

        static FormField CreateTextField(Inspector.PropertySettings prop)
        {
            var result = new FormField<TextInput>
            {
                Value = prop.ExistingValue,
                Placeholder = "-Auto-".OnlyWhen(prop.Label.IsAnyOf("Width", "Height", "X", "Y"))
            };

            result.Control.UserKeyUp.Handle(async key =>
            {
                if (!result.Text.Is<double>()) return;

                if (key == (int)VirtualKey.Up || key == (int)VirtualKey.Down)
                {
                    var add = 1.0;

                    if (result.Text.Contains("."))
                        add = 1.0 / Math.Pow(10, result.Text.TrimBefore(".", trimPhrase: true).Length);

                    if (add == 1 && prop.Label == "Opacity") add = 0.1;

                    if (key == (int)VirtualKey.Down) add *= -1;

                    result.Control.Text = (result.Control.Text.To<double>() + add).ToString();

                    await UpdateValue(result, result.Control.Text);
                }
            });

            result.Control.ChangeInBatch(x => x.Font(12).Background(color: "#111").TextColor("#eee").Padding(3).Border(0));
            result.Control.UserTextChanged.Handle(() => UpdateValue(result, result.Control.Text.OrEmpty()));
            return result;
        }

        static async Task UpdateValue(FormField field, string newValue)
        {
            var prop = field.Data<Inspector.PropertySettings>("Prop");

            void setValue()
            {
                try
                {
                    prop.Property.SetValue(prop.Instance, newValue.To(prop.Property.PropertyType));
                    field.Background(color: Colors.Transparent);
                }
                catch (Exception ex)
                {
                    field.Background(color: Colors.DarkRed);
                }
            }
;

            prop.View.Animate(x => setValue()).RunInParallel();

            await Inspector.Current.HighlightItem();
        }

        Button CreateDeleteButton()
        {
            var result = new Button { Text = "Delete" }.TextColor(Colors.Red).Margin(10, top: 30).Padding(0).Height(null);

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
            var result = new Button { Text = "Bring to front" }.TextColor(Colors.LightGreen).Margin(10).Padding(0);
            result.Tapped.Handle(() => View.BringToFront());
            return result;
        }

        Button CreateSendToBackButton()
        {
            var result = new Button { Text = "Send to back" }.TextColor(Colors.LightBlue).Margin(10).Padding(0);
            result.Tapped.Handle(() => View.SendToBack());
            return result;
        }
    }
}