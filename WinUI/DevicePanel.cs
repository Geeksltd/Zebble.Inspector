namespace Zebble.WinUI
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Olive;
    using Zebble.Device;

    class DevicePanel : Stack
    {
        View View;
        Canvas MainPhone;
        ImageView Phone;
        TextView Title, Description;
        float ZRotation, YRotation, XRotation;

        public List<View> HeaderButtons = new List<View>();

        [EscapeGCop("Hardcoded styles are fine here.")]
        public DevicePanel(View view)
        {
            View = view;

            Title = new TextView().Font(color: Colors.White, size: 28).X(10).Y(10).Absolute();
            Description = new TextView().Font(color: Colors.White, size: 12).X(10).Y(42).Absolute();

            var actualHeight = Root.ActualHeight - 90;

            Shown.Handle(OnShown);
        }

        async Task OnShown()
        {
            await Add(MainPhone = new Canvas().Center().On(x => x.Panning, p => OnPanning(p)));

            await MainPhone.Add(Title);
            await MainPhone.Add(Description);

            await MainPhone.Add(CreatePhone("Device.png").Center());
        }

        ImageView CreatePhone(string name)
        {
            var img = GetType().Assembly.ReadEmbeddedResource("Zebble", "Inspection/Resources/" + name);
            Phone = new ImageView().Id("Target").Size(220, 250).Alignment(Alignment.Middle).Margin(top: 300);
            Phone.BackgroundImageData = img;
            return Phone;
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