# Components in Upgrade Assistant

Components are a concept in Upgrade Assistant that allows various services to understand what kind of technologies are in a given project. A project's components by design may change through the lifetime of an upgrade (for example ASP.NET to ASP.NET Core, XamarinAndroid to MauiAndroid).

Components are defined by implementing the [IComponentIdentifier](../src/common/Microsoft.DotNet.UpgradeAssistant.Abstractions/IComponentIdentifier.cs) and all registered implementations are run when identifying components.

The following are the current components in the project with a summary of what conditions will trigger that component to be included:

### ASP.NET

> Implemented in [AspNetComponentIdentifier](../src/extensions/web/Microsoft.DotNet.UpgradeAssistant.Extensions.Web/AspNetComponentIdentifier.cs)

- References `System.Web.*` assemblies
- Contains `Microsoft.WebApplication.targets` import in the project file

### ASP.NET Core

> Implemented in [AspNetComponentIdentifier](../src/extensions/web/Microsoft.DotNet.UpgradeAssistant.Extensions.Web/AspNetComponentIdentifier.cs)

- Is an SDK project and uses the `Microsoft.NET.Sdk.Web` SDK
- References the framework package `Microsoft.AspNetCore.App`

### Windows Desktop

> Implemented in [AspNetComponentIdentifier](../src/extensions/windows/Microsoft.DotNet.UpgradeAssistant.Extensions.Windows/WindowsComponentIdentifier.cs)

- If the project has either `WinForms` or `Wpf` comnponents
- References the `Microsoft.WindowsDesktop.App` package
- Is an SDK project and uses the `Microsoft.NET.Sdk.Desktop` SDK

### WinForms

> Implemented in [AspNetComponentIdentifier](../src/extensions/windows/Microsoft.DotNet.UpgradeAssistant.Extensions.Windows/WindowsComponentIdentifier.cs)

- Contains a references to the assembly `System.Windows.Forms`
- Is an SDK project and has the property `UseWindowsForms=true`
- References the framework package `Microsoft.WindowsDesktop.App.WindowsForms`

### Wpf

> Implemented in [AspNetComponentIdentifier](../src/extensions/windows/Microsoft.DotNet.UpgradeAssistant.Extensions.Windows/WindowsComponentIdentifier.cs)

- References `System.Xaml`, `PresentationCore`, `PresentationFramework`, or `WindowsBase`
- Is an SDK project and has the property `UseWPF=true`
- References the framework package `Microsoft.WindowsDesktop.App.Wpf`

### WinRT

> Implemented in [AspNetComponentIdentifier](../src/extensions/windows/Microsoft.DotNet.UpgradeAssistant.Extensions.Windows/WindowsComponentIdentifier.cs)

- Contains a references to `Microsoft.Windows.SDK.Contracts`

### Xamarin.iOS

> Implemented in [MauiComponentIdentifier](../src/extensions/maui/Microsoft.DotNet.UpgradeAssistant.Extensions.Maui/MauiComponentIdentifier.cs)

- References `Xamarin.iOS`

### Xamarin.Android

> Implemented in [MauiComponentIdentifier](../src/extensions/maui/Microsoft.DotNet.UpgradeAssistant.Extensions.Maui/MauiComponentIdentifier.cs)

- References `Xamarin.Android`

### Maui[iOS|Android]

> Implemented in [MauiComponentIdentifier](../src/extensions/maui/Microsoft.DotNet.UpgradeAssistant.Extensions.Maui/MauiComponentIdentifier.cs)

- References `Xamarin.Forms`
- Targets `android` or `ios` platforms
  - This will also add the `MauiiOS` or `MauiAndroid` component
