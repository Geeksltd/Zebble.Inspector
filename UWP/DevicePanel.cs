namespace Zebble.UWP
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Zebble.Device;

    class DevicePanel : Stack
    {
        const string SELECTED = "#43aaa9";

        View View;
        Canvas MainPhone;
        ImageView Phone;
        TextView Title, Description, AccelerometerInfo, GyroscopeInfo, CompassInfo;
        float ZRotation, YRotation, XRotation;

        public Stack GeoLocationForm;
        public List<View> HeaderButtons = new List<View>();

        [EscapeGCop("Hardcoded styles are fine here.")]
        public DevicePanel(View view)
        {
            View = view;

            Title = new TextView().Font(color: Colors.White, size: 28).X(10).Y(10).Absolute();
            Description = new TextView().Font(color: Colors.White, size: 12).X(10).Y(42).Absolute();

            EnvironmentSimulator.Start();

            var actualHeight = Root.ActualHeight - 90;
            AccelerometerInfo = new TextView().Font(color: Colors.White, size: 15).X(10).Y(actualHeight - 60).Absolute();
            GyroscopeInfo = new TextView().Font(color: Colors.White, size: 15).X(10).Y(actualHeight - 40).Absolute();
            CompassInfo = new TextView().Font(color: Colors.White, size: 15).X(10).Y(actualHeight - 20).Absolute();

            Shown.Handle(OnShown);

            ShowValues();
        }

        void ShowValues()
        {
            if (Phone == null) return;

            var acc = new MotionVector
            {
                X = Phone.RotationX / 360,
                Y = Phone.RotationY / 360,
                Z = Phone.Rotation / 360,
            };

            // TODO: For gyro, this should be the speed, not present value.
            // We should find a time-based approach to find the correct values.
            var gyro = new MotionVector
            {
                X = Phone.RotationX / 360,
                Y = Phone.RotationY / 360,
                Z = Phone.Rotation / 360,
            };

            AccelerometerInfo.Text($"Accelorometer       X: {acc.X.Round(2)}      Y: {acc.Y.Round(2)}       Z: {acc.Z.Round(2)}");
            GyroscopeInfo.Text($"Gyroscope             X: {gyro.X.Round(2)}      Y: {gyro.Y.Round(2)}       Z: {gyro.Z.Round(2)}");
            CompassInfo.Text($"Compass                Heading: {acc.Z.Round(2)}");
        }

        async Task OnShown()
        {
            await Add(MainPhone = new Canvas().MiddleAlign().CenterAlign().On(x => x.Panning, p => OnPanning(p)));

            await MainPhone.Add(Title);
            await MainPhone.Add(Description);

            await MainPhone.Add(AccelerometerInfo);
            await MainPhone.Add(GyroscopeInfo);
            await MainPhone.Add(CompassInfo);

            await MainPhone.Add(CreatePhone("Device.png").MiddleAlign().CenterAlign());
        }

        View CreateButton(string name)
        {
            return new ImageView
            {
                ImageData = GetType().GetAssembly().ReadEmbeddedResource("Zebble.UWP", "Inspection/Resources/" + name)
            }.Size(32).Padding(6);
        }

        ImageView CreatePhone(string name)
        {
            var img = GetType().GetAssembly().ReadEmbeddedResource("Zebble.UWP", "Inspection/Resources/" + name);
            Phone = new ImageView().Id("Target").Size(220, 250).Alignment(Alignment.Middle).Margin(top: 300);
            Phone.BackgroundImageData = img;
            return Phone;
        }

        public async Task CreateGeoLocationForm(float xPosition, float yPosition)
        {
            if (GeoLocationForm != null)
            {
                GeoLocationForm.parent?.Remove(GeoLocationForm);
                GeoLocationForm = null;
            }
            else
            {
                GeoLocationForm = new Stack().Size(200, 90).Padding(vertical: 8, horizontal: 8).Border(color: "#444", all: 1)
                    .Background(color: "#666").X(xPosition).Y(yPosition).Absolute().ZIndex(1000);

                var title = new TextView { Text = "Your Location", TextAlignment = Alignment.Middle };
                title.Width(100.Percent()).Padding(bottom: 5).Font(bold: true);
                var latField = new FormField<TextInput> { LabelText = "Latitude" };
                var longField = new FormField<TextInput> { LabelText = "Longitude" };
                latField.Control.Background(color: "#444").Padding(top: 5).Font(color: Colors.White, size: 10);
                longField.Control.Background(color: "#444").Padding(top: 5).Font(color: Colors.White, size: 10);

                if (EnvironmentSimulator.Location != null)
                {
                    latField.Control.Text(EnvironmentSimulator.Location.Latitude.ToString());
                    longField.Control.Text(EnvironmentSimulator.Location.Longitude.ToString());
                }

                var configButton = new Button
                {
                    Text = "Set",
                    BackgroundColor = new GradientColor(GradientColor.Direction.Down).Add("#555", 50).EndWith("#444")
                };
                configButton.Tapped.Handle(() =>
                {
                    EnvironmentSimulator.Location = new Services.GeoPosition
                    {
                        Latitude = latField.GetValue<float>(),
                        Longitude = longField.GetValue<float>()
                    };
                    GeoLocationForm.parent?.Remove(GeoLocationForm);
                    GeoLocationForm = null;
                });

                await GeoLocationForm.Add(title);
                await GeoLocationForm.Add(latField);
                await GeoLocationForm.Add(longField);
                await GeoLocationForm.Add(configButton);

                await MainPhone.Add(GeoLocationForm);
            }
        }

        public Task OnPanning(PannedEventArgs arg)
        {
            var touches = arg.Touches;

            var xDiff = arg.To.X - arg.From.X;
            var yDiff = arg.To.Y - arg.From.Y;

            if (touches == 2)
            {
                ZRotation += xDiff / 2 + yDiff / 2;
                Phone.Rotation(ZRotation);
            }
            else
            {
                XRotation += xDiff;
                YRotation += yDiff;
                Phone.RotationY(XRotation).RotationX(YRotation);
            }

            EnvironmentSimulator.Gyroscope.Invoke(new MotionVector(XRotation, YRotation, ZRotation));
            EnvironmentSimulator.Compass.Invoke(ZRotation);

            // TODO: It should be time based, to calculate the speed.
            EnvironmentSimulator.Accelerometer.Invoke(new MotionVector(Phone.X.CurrentValue, Phone.Y.CurrentValue, 0));

            ShowValues();

            return Task.CompletedTask;
        }

        public Task Activate()
        {
            Title.Text("Environment APIs simulation");

            Description.Text("Device.Accelerometer returns the angle of the device relative to the earth core.\r\n" +
                "Device.Gyroscope returns the motion (or rotation) speed of the device in different directions.\r\nDrag the phone with your mouse to rotate it (for Z rotation hold \"Shift\").");

            return Task.CompletedTask;
        }
    }
}