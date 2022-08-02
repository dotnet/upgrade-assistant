using System;
using System.Collections.Immutable;

namespace MSBuild.Conversion.Facts
{
    public static class WebFacts
    {
        public const string WebSDKAttribute = "Microsoft.NET.Sdk.Web";
        public const string MvcBuildViewsName = "MvcBuildViews";
        public const string WebProjectPropertiesName = "WebProjectProperties";
        public const string WebApplicationTargets = "Microsoft.WebApplication.targets";

        public static ImmutableArray<Guid> LegacyWebProjectTypeGuids => ImmutableArray.Create(
            Guid.Parse("{349c5851-65df-11da-9384-00065b846f21}"), // ASP.NET MVC 5
            Guid.Parse("{E3E379DF-F4C6-4180-9B81-6769533ABE47}"), // ASP.NET MVC 4
            Guid.Parse("{E53F8FEA-EAE0-44A6-8774-FFD645390401}"), // ASP.NET MVC 3
            Guid.Parse("{F85E285D-A4E0-4152-9332-AB1D724D3325}"), // ASP.NET MVC 2
            Guid.Parse("{603C0E0B-DB56-11DC-BE95-000D561079B0}"), // ASP.NET MVC 1
            Guid.Parse("{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}") // ASP.NET 5
        );

        /// <summary>
        /// The core set of references all ASP.NET projects use.
        /// </summary>
        public static ImmutableArray<string> KnownWebReferences => ImmutableArray.Create(
            "System.Web"
        );
    }
}
