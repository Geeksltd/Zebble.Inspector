namespace Zebble.UWP
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.System;
    using Zebble.Services;

    class PropertiesList : Stack
    {
        View View;
        TextInput CssTextbox;
        TextView Error;
        TextInput AttributeFilter;
        Stack PropertiesContainer;
        DateTime LastFilterChanged;

        internal AsyncEvent<bool> ScrollEnabledChange = new AsyncEvent<bool>();

        public PropertiesList(View view)
        {
            View = view;

            // Shown.Handle(OnShown);
        }

        public override async Task OnInitializing()
        {
            await base.OnInitializing();
            await OnShown();
        }

        async Task OnShown()
        {
            await AddCss();

            await AddAttributeFilter();

            await AddProperties();

            Thread.UI.RunAction(() =>
           (CssTextbox.Native() as Windows.UI.Xaml.Controls.Border)
               .Get(x => x?.Child as Windows.UI.Xaml.Controls.TextBox)
               .Perform(x => x.IsReadOnly = true));

            await Add(Error = new TextView().Wrap().TextColor(Colors.Red)
                .Padding(10).Ignored().Margin(10)
                .Background(color: Colors.LightYellow));

            await Add(CreateDeleteButton());
            await Add(CreateBringToFrontButton());
            await Add(CreateSendToBackButton());
            await Add(CreateTypeInfo());
        }

        async Task AddAttributeFilter()
        {
            AttributeFilter = new TextInput { Placeholder = "Search..." }
            .ChangeInBatch(x => x.Margin(top: 10).Padding(5).Border(0).Background(color: "#333").Font(12, color: "#aaa"))
                .On(x => x.UserTextChanged, FilterAttributes);

            await Add(AttributeFilter);
        }

        async Task AddCss()
        {
            var code = View.CurrentlyAppliedCss;

            await Add(new TextView("Applicable CSS Rules").ChangeInBatch(x => x.Font(20).Margin(vertical: 10)));
            CssTextbox = await Add(new TextInput
            {
                Text = code,
                Lines = 3
            }.Background(color: "#333").Padding(5).Font(11, color: "#7da").Border(0));

            CssTextbox.Height.BindTo(CssTextbox.Width, CssTextbox.Padding.Top, CssTextbox.Padding.Bottom,
                (w, pt, pb) => (CssTextbox.Font.GetTextHeight(w, code) * 1.05f + pt + pb).LimitMax(500));
        }

        async Task FilterAttributes()
        {
            LastFilterChanged = DateTime.Now;
            await PropertiesContainer.ClearChildren();
            await AddProperties(AttributeFilter.Text, LastFilterChanged);
        }

        async Task AddProperties(string filter = "", DateTime? triggeredOn = null)
        {
            filter = filter?.Trim() ?? "";

            if (PropertiesContainer == null)
                await Add(PropertiesContainer = new Stack());

            var settings = Inspector.GetSettings(View).ToList();

            foreach (var group in settings.GroupBy(x => x.Group))
            {
                if (!group.Key.Contains(filter, caseSensitive: false) && !group.Any(item => item.Label.TrimStart(item.Group).ToLiteralFromPascalCase().Contains(filter, caseSensitive: false)))
                    continue;

                await PropertiesContainer.Add(new TextView(group.Key)
                    .ChangeInBatch(x => x.Font(20).Margin(top: 10, bottom: PropertiesContainer.AllChildren.Count == 0 ? 10 : 5)));

                foreach (var item in group)
                {
                    if (triggeredOn != null && triggeredOn != LastFilterChanged) return;

                    var labelText = item.Label.TrimStart(item.Group).ToLiteralFromPascalCase();

                    if (!group.Key.Contains(filter, caseSensitive: false) && !labelText.Contains(filter, caseSensitive: false))
                        continue;

                    var field = CreateField(item).Set(x => x.Direction = RepeatDirection.Horizontal).ChangeInBatch(x => x.Height(20).Margin(bottom: 1)).Data("Prop", item);
                    field.Label.ChangeInBatch(x => x.Text(labelText).Font(12, color: "#aaa").Padding(3).Width(50.Percent()));

                    await PropertiesContainer.Add(field);

                    if (item.Notes.HasValue())
                    {
                        await field.Add(new TextView("?").Background(Colors.Gray).Size(15).TextAlignment(Alignment.Middle).Round().On(x => x.Tapped,
                            () => Alert.Show(item.Notes)));
                    }

                    if (item.Instance is ITrackable tra) await field.Add(GetTrackerIcon(tra));

                    if (item.Instance is Gap)
                    {
                        await field.Add(GetTrackerIcon(item.View.Css, item.View.Style));
                    }
                }
            }
        }

        static TextView GetTrackerIcon(params ITrackable[] trackables)
        {
            return new TextView("⌕").Font(16, color: Colors.Pink).Size(16).Margin(left: 2)
                                 .TextAlignment(Alignment.Middle).Round().On(x => x.Tapped,
                                () =>
                                {
                                    LayoutTracker.StartTracking(trackables); Nav.Reload().RunInParallel();
                                });
        }

        FormField CreateField(Inspector.PropertySettings prop)
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

        FormField CreateTextField(Inspector.PropertySettings prop)
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

            result.Control.UserFocusChanged.Handle(focused => ScrollEnabledChange.Raise(!focused));

            result.Control.ChangeInBatch(x => x.Font(12).Background(color: "#111").TextColor("#eee").Padding(3).Border(0));
            result.Control.UserTextChanged.Handle(() => UpdateValue(result, result.Control.Text.OrEmpty()));
            return result;
        }

        async Task UpdateValue(FormField field, string newValue)
        {
            var prop = field.Data<Inspector.PropertySettings>("Prop");

            void setValue()
            {
                try
                {
                    prop.Property.SetValue(prop.Instance, newValue.To(prop.Property.PropertyType));
                    Error.Text(string.Empty).Ignored();
                    field.Background(color: Colors.Transparent);
                }
                catch (Exception ex)
                {
                    field.Background(color: Colors.DarkRed);
                    Error.Text(ex.Message).Ignored(value: false);
                }
            };

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
            result.Tapped.Handle(View.BringToFront);
            return result;
        }

        Button CreateSendToBackButton()
        {
            var result = new Button { Text = "Send to back" }.TextColor(Colors.LightBlue).Margin(10).Padding(0);
            result.Tapped.Handle(View.SendToBack);
            return result;
        }

        TextView CreateTypeInfo()
        {
            var text = View.GetType().WithAllParents()
                .TakeWhile(x => x != typeof(View))
                .Select(x => x.GetProgrammingName(useGlobal: false, useNamespace: false, useNamespaceForParams: false))
                .ToString("\r\n➤ ");

            return new TextView().TextColor("#888").Background("#333").Padding(5).Margin(bottom: 5).Text("Type ➝ " + text);
        }
    }
}