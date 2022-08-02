namespace MSBuild.Abstractions
{
    public enum ProjectStyle
    {
        /// <summary>
        /// The project has an import of two defaults. Typically Common.props and CSharp.targets or FSharp.targets, etc. 
        /// </summary>
        Default,

        /// <summary>
        /// Using one of the two defaults, typically CSharp.targets or FSharp.targets.
        /// </summary>
        DefaultSubset,

        /// <summary>
        /// Not using any default targets or props.
        /// </summary>
        Custom,

        /// <summary>
        /// The project is WPF or WinForms, and will use the WinDesktop framework reference
        /// </summary>
        WindowsDesktop,

        /// <summary>
        /// The project is an MSTest project that pulls in a lot of unnecessary imports.
        /// </summary>
        MSTest,

        /// <summary>
        /// The project is an ASP.NET project that will not be possible to completely convert automatically.
        /// If the user has specified --force-web-conversion, a best-effort will be made using the
        /// Microsoft.NET.Sdk.Web SDK.
        /// </summary>
        Web,

        /// <summary>
        /// The project is of type Xamarin.Android 
        /// </summary>
        XamarinDroid,

        /// <summary>
        /// The project is of type Xamarin.iOS
        /// </summary>
        XamariniOS
    }
}
