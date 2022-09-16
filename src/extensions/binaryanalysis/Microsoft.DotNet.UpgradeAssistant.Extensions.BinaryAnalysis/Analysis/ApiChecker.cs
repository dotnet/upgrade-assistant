// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;

using Microsoft.Cci;
using Microsoft.Cci.Extensions;
using Microsoft.Cci.Mappings;
using Microsoft.Cci.UtilityDataStructures;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.UpgradeAssistant.Analysis;
using Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.ApiCatalog;
using Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.UsageCrawling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NuGet.Frameworks;

using static Microsoft.DotNet.UpgradeAssistant.TargetFrameworkMoniker;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.BinaryAnalysis.Analysis
{
    public sealed class ApiChecker : IBinaryAnalysisExecutor
    {
        private readonly IBinaryAnalysisExecutorOptions _options;
        private readonly DefaultTfmOptions _tfmSelector;
        private readonly ILogger<ApiChecker> _logger;

        public ApiChecker(IBinaryAnalysisExecutorOptions options,
            IOptions<DefaultTfmOptions> selectorOptions,
            ILogger<ApiChecker> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _tfmSelector = selectorOptions?.Value ?? throw new ArgumentNullException(nameof(selectorOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunAsync(Func<OutputResult, Task> receiver)
        {
            var framework = NuGetFramework.ParseFolder(_tfmSelector.DetermineTargetTfmValue());
            var platforms = _options.Platform.Select(p => p.ToString().ToLowerInvariant()).ToImmutableList();
            async void Receiver(AssemblyResult result)
            {
                _logger.LogInformation(Resources.BinaryAnalysisProcessedAssemblyMessageFormat, result.AssemblyName);

                foreach (var r in CreateUaResults(result).ToList())
                {
                    await receiver(r).ConfigureAwait(false);
                }
            }

            var catalog = await CatalogService.LoadCatalogAsync().ConfigureAwait(false);
            var apiByGuid = catalog.GetAllApis().ToDictionary(a => a.UniqueId);
            var availabilityContext = ApiAvailabilityContext.Create(catalog);

            var platformContext = PlatformAnnotationContext.Create(availabilityContext, framework.GetShortFolderName());

            foreach (var api in catalog.GetAllApis())
            {
                var forwardedApi = catalog.GetForwardedApi(api);
                if (forwardedApi is not null)
                {
                    apiByGuid[api.UniqueId] = forwardedApi.Value;
                }
            }

            var apiAvailability = new ConcurrentDictionary<ApiModel, ApiAvailability>();

            using var resultSink = new BlockingCollection<AssemblyResult>();

            var resultSinkTask = Task.Run(() =>
            {
                foreach (var result in resultSink.GetConsumingEnumerable())
                {
                    Receiver(result);
                }
            });

            var filePaths = AssemblyFileSet.Create(_options.Content.ToList());
            Parallel.ForEach(filePaths, filePath =>
            {
                using var env = new HostEnvironment();
                var assembly = env.LoadAssemblyFrom(filePath);
                var assemblyName = assembly is not null
                                    ? assembly.Name.Value
                                    : Path.GetFileName(filePath);
                if (assembly is null)
                {
                    var result = new AssemblyResult(assemblyName, Resources.BinaryAnalysisInvalidAssemblyMessage, Array.Empty<ApiResult>());
                    resultSink.Add(result);
                }
                else
                {
                    var assemblyTfm = assembly.GetTargetFrameworkMoniker();
                    var assemblyFramework = string.IsNullOrEmpty(assemblyTfm) ? null : NuGetFramework.Parse(assemblyTfm);

                    var crawler = new AssemblyCrawler();
                    crawler.Crawl(assembly);

                    var crawlerResults = crawler.CreateResults();

                    var apiResults = new List<ApiResult>();
                    var frameworkResultBuilder = new List<FrameworkResult>();
                    var platformResultBuilder = new List<PlatformResult?>(platforms.Count);

                    foreach (var apiKey in crawlerResults.Data.Keys)
                    {
                        if (apiByGuid.TryGetValue(apiKey.Id, out var api))
                        {
                            var availability = apiAvailability.GetOrAdd(api, a => availabilityContext.GetAvailability(a));

                            frameworkResultBuilder.Clear();

                            AnalyzeAvailability(availability, framework, out AvailabilityResult availabilityResult, out ApiFrameworkAvailability? info);

                            var obsoletionResult = AnalyzeObsoletion(availabilityContext, assemblyFramework, api, info);

                            AnalyzePlatformSupport(platforms, platformContext, frameworkResultBuilder, platformResultBuilder!, api, framework!, availabilityResult, info, obsoletionResult);

                            var apiResult = new ApiResult(api, frameworkResultBuilder.ToArray());
                            apiResults.Add(apiResult);
                        }
                    }

                    var results = new AssemblyResult(assemblyName, null, apiResults.ToArray());
                    resultSink.Add(results);
                }
            });

            resultSink.CompleteAdding();
            resultSinkTask.Wait();
        }

        private IEnumerable<OutputResult> CreateUaResults(AssemblyResult asmResult)
        {
            const string BINARY_ANALYSIS_ASSEMBLY_ISSUE_RULE_ID = "UA9000";
            const string BINARY_ANALYSIS_API_UNAVAILABLE_RULE_ID = "UA9010";
            const string BINARY_ANALYSIS_API_AVAILABLE_WITH_PACKAGE_RULE_ID = "UA9011";
            const string BINARY_ANALYSIS_API_OBSOLETED_RULE_ID = "UA9020";
            const string BINARY_ANALYSIS_PLATFORM_UNSUPPORTED_RULE_ID = "UA9030";

            var helpUri = new Uri("https://github.com/dotnet/upgrade-assistant/tree/main/docs/binary_analysis.md");

            if (!string.IsNullOrEmpty(asmResult.AssemblyIssues))
            {
                yield return new OutputResult
                {
                    FileLocation = asmResult.AssemblyName,
                    ResultMessage = asmResult.AssemblyIssues,
                    RuleId = BINARY_ANALYSIS_ASSEMBLY_ISSUE_RULE_ID,
                    RuleName = "InvalidAssembly",
                    FullDescription = Resources.BinaryAnalysisInvalidAssemblyMessage,
                    HelpUri = helpUri,
                };
            }
            else
            {
                foreach (var apiResult in asmResult.Apis.Where(i => i.IsRelevant()))
                {
                    var namespaceName = apiResult.Api.GetNamespaceName();
                    var typeName = apiResult.Api.GetTypeName();
                    var memberName = apiResult.Api.GetMemberName();

                    var qualifier = !string.IsNullOrWhiteSpace(memberName)
                        ? string.Format(CultureInfo.CurrentCulture, Resources.BinaryAnalysisMemberQualifierFormat, namespaceName, typeName, memberName)
                        : !string.IsNullOrWhiteSpace(typeName)
                            ? string.Format(CultureInfo.CurrentCulture, Resources.BinaryAnalysisTypeQualifierFormat, namespaceName, typeName)
                            : string.Format(CultureInfo.CurrentCulture, Resources.BinaryAnalysisNamespaceQualifierFormat, namespaceName);

                    var message = $"{qualifier} : ";

                    foreach (var frameworkResult in apiResult.FrameworkResults)
                    {
                        message = string.Concat(message, string.Format(CultureInfo.CurrentCulture, Resources.BinaryAnalysisApiAvailabilityFormatMessageFormat, frameworkResult.FrameworkName, frameworkResult.Availability), ' ');
                        if (!frameworkResult.Availability.IsAvailable)
                        {
                            yield return new OutputResult
                            {
                                FileLocation = asmResult.AssemblyName,
                                ResultMessage = message,
                                RuleId = BINARY_ANALYSIS_API_UNAVAILABLE_RULE_ID,
                                RuleName = "ApiUnavailable",
                                FullDescription = Resources.BinaryAnalysisAssemblyNotAvailableRuleName,
                                HelpUri = helpUri,
                            };
                        }
                        else if (frameworkResult.Availability.Package is not null)
                        {
                            yield return new OutputResult
                            {
                                FileLocation = asmResult.AssemblyName,
                                ResultMessage = string.Concat(message, string.Format(CultureInfo.CurrentCulture, Resources.BinaryAnalysisApiPackageVersionMessageFormat, frameworkResult.Availability.Package.Value.Version)),
                                RuleId = BINARY_ANALYSIS_API_AVAILABLE_WITH_PACKAGE_RULE_ID,
                                RuleName = "ApiAvailableViaExternalPackage",
                                FullDescription = Resources.BinaryAnalysisApiAvailableExternalPackageRuleName,
                                HelpUri = helpUri,
                            };
                        }

                        if (_options.Obsoletion && frameworkResult.Obsoletion is not null)
                        {
                            yield return new OutputResult
                            {
                                FileLocation = asmResult.AssemblyName,
                                ResultMessage = string.Concat(message, Resources.BinaryAnalysisApiObsoletedMessage),
                                RuleId = BINARY_ANALYSIS_API_OBSOLETED_RULE_ID,
                                RuleName = "ApiObsoleted",
                                FullDescription = Resources.BinaryAnalysisApiObsoletedRuleName,
                                HelpUri = helpUri,
                            };
                        }

                        foreach (var platformResult in frameworkResult.Platforms.Where(p => p is not null && !p.IsSupported))
                        {
                            yield return new OutputResult
                            {
                                FileLocation = asmResult.AssemblyName,
                                ResultMessage = string.Concat(message, string.Format(CultureInfo.CurrentCulture, Resources.BinaryAnalysisPackageUnsupportedPlatformMessageFormat, platformResult!.PlatformName)),
                                RuleId = BINARY_ANALYSIS_PLATFORM_UNSUPPORTED_RULE_ID,
                                RuleName = "PlatformNotSupported",
                                FullDescription = Resources.BinaryAnalysisApiUnsupportedPlatformRuleName,
                                HelpUri = helpUri,
                            };
                        }
                    }
                }
            }
        }

        private static void AnalyzePlatformSupport(ImmutableList<string> platforms, PlatformAnnotationContext platformContext, IList<FrameworkResult> frameworkResultBuilder, IList<PlatformResult?> platformResultBuilder, ApiModel api, NuGetFramework framework, AvailabilityResult availabilityResult, ApiFrameworkAvailability? info, ObsoletionResult? obsoletionResult)
        {
            platformResultBuilder.Clear();

            if (info is null)
            {
                for (var i = 0; i < platforms.Count; i++)
                {
                    platformResultBuilder.Add(null);
                }
            }
            else
            {
                foreach (var platform in platforms)
                {
                    var annotation = platformContext.GetPlatformAnnotation(api);
                    var isSupported = annotation.IsSupported(platform);
                    var platformResult = isSupported ? PlatformResult.Supported(platform) : PlatformResult.Unsupported(platform);
                    platformResultBuilder.Add(platformResult);
                }
            }

            var frameworkResult = new FrameworkResult(framework, availabilityResult, obsoletionResult, platformResultBuilder.ToImmutableList());
            frameworkResultBuilder.Add(frameworkResult);
        }

        private ObsoletionResult? AnalyzeObsoletion(ApiAvailabilityContext availabilityContext, NuGetFramework? assemblyFramework, ApiModel api, ApiFrameworkAvailability? info)
        {
            ObsoletionResult? obsoletionResult;

            if (!_options.Obsoletion || info?.Declaration.Obsoletion is null)
            {
                obsoletionResult = null;
            }
            else
            {
                var compiledAgainstObsoleteApi = false;

                if (assemblyFramework is not null)
                {
                    var compiledAvailability = availabilityContext.GetAvailability(api, assemblyFramework);
                    if (compiledAvailability?.Declaration.Obsoletion is not null)
                    {
                        compiledAgainstObsoleteApi = true;
                    }
                }

                if (compiledAgainstObsoleteApi)
                {
                    obsoletionResult = null;
                }
                else
                {
                    var o = info.Declaration.Obsoletion.Value;
                    obsoletionResult = new ObsoletionResult(o.Message, o.Url);
                }
            }

            return obsoletionResult;
        }

        private void AnalyzeAvailability(ApiAvailability availability, NuGetFramework? framework, out AvailabilityResult availabilityResult, out ApiFrameworkAvailability? info)
        {
            var infos = availability.Frameworks.Where(fx => fx.Framework == framework).ToList();

            // NOTE: There are APIs that exist in multiple places in-box, e.g. Microsoft.Windows.Themes.ListBoxChrome.
            //       It doesn't really matter for our purposes. Either way, we'll pick the first one.
            info = infos.FirstOrDefault(i => i.IsInBox) ?? infos.FirstOrDefault(i => !i.IsInBox && (_options.AllowPrerelease || i.Package?.Version.Contains('-', StringComparison.Ordinal) == false));
            if (info is null)
            {
                availabilityResult = AvailabilityResult.Unavailable;
            }
            else if (info.IsInBox)
            {
                availabilityResult = AvailabilityResult.AvailableInBox;
            }
            else
            {
                availabilityResult = AvailabilityResult.AvailableInPackage(info.Package.Value);
            }
        }
    }
}
