[logo]: https://raw.githubusercontent.com/Geeksltd/Zebble.Inspector/master/Shared/NuGet/Icon.png "Zebble.Inspector"


## Zebble.Inspector

![logo]

A Zebble plugin that allows you to inspect the design of your application on all platforms.

[![NuGet](https://img.shields.io/nuget/v/Zebble.Inspector.svg?label=NuGet)](https://www.nuget.org/packages/Zebble.Inspector/)

> An Inspector that allows the user to see the style-sheet result and change them in the runtime. Also, it contains some tool to show the code of the selected view and it enables the user to simulate device sensors.

<br>


### Setup
* Available on NuGet: [https://www.nuget.org/packages/Zebble.Inspector/](https://www.nuget.org/packages/Zebble.Inspector/)
* Install in your platform client projects.
* Available for UWP.
<br>


### Api Usage

#### DEBUGGING LAYOUT AND STYLES

Every view object has style related properties such as X, Y, Width, Height, Margin, TextColor, .... The effective value for each item is determined by either the direct Style setting of that object, or the most specific CSS rule which defines a value for that setting.

When the effective value for any style property comes from CSS (instead of inline Style) then you can easily see the situation to understand which CSS rules are being picked up and applied to your view and in what order.

##### Direct styles

But there are cases when you need to set certain properties via Style property directly. For example:
```csharp
myView.Background(color: Colors.Blue);
// Which is the same as:
myView.Style.BackgroundColor = Colors.Blue;
```
In these cases when you look at the object's properties in the inspector you can see the value (blue, in the example above) but you can't see exactly where and when this is being set. So if the value is incorrect, you might not immediately be able to see when this is being set.

In particular, consider scenarios when a complex mesh of event handlers is leading to a style property being set which you can't figure out how or even know where to start looking.

#### Solution: Layout Tracker
Zebble comes with a creative solution for this problem. This is how it works:

Using the inspector you identify the style property (width, height, etc) which has the wrong value.
There is a magnifying glass icon next to it which means "set up a tracker"
In this moment, the Zebble engine will register a tracker for that element and the page will refresh.
Open the Output window in Visual studio while the app is running.
You will notice that it's now reporting every time that style property is set, along with the stack trace, allowing you to see exactly what's going on.

#### Testing styles for iOS and Android on Windows

Ctrl + Click on any element on the page to launch the inspection window. Then from the drop-down list at the top right corner, select a device to see the application updated for that device size and styles. Make sure to download the Android and iOS fonts for Windows
