# Migrating WCF server-side project to CoreWCF on .NET 6

This article documents the current support for upgrade-assistant to update WCF server-side projects on .NET Framework to use [CoreWCF](https://github.com/corewcf/corewcf) on .NET 6. 

> Note: At the time of this writing, the WCF extension is supplied as a Preview release. The list below summarizes the functionality and requirements for the Preview release version. When new features are added to the extension in the future, we will also update this list.
## Supported functionality and minimum requirements

- The latest version of .NET Upgrade Assistant with CoreWCF extension (Preview) from the [Azure DevOps Pipeline](https://dev.azure.com/dnceng/public/_artifacts/feed/dotnet-tools)
  - ✅ Upgrade csproj to the newer SDK style format
  - ✅ Update project TFM to net6.0
  - ✅ Update WCF project with a single `ServiceHost` instance and replace it with ASP.NET Core hosting.
  - ✅ Update WCF project with multiple services. All `ServiceHost` objects are instantiated and configured in the same method.
  - ✅ Update the original configuration file in the project and generate a new configuration file for CoreWCF.
  - ✅ Replace `System.ServiceModel` namespace and references with CoreWCF ones in .cs and project files.
  - ❌ WCF server that are Web-hosted and use .svc file.
  - ❌ Behavior configuration except `serviceDebug`, `serviceMetadata`, `serviceCredentials`(`clientCertificate`, `serviceCertificate`, `userNameAuthentication`, and `windowsAuthentication`)
  - ❌ Endpoints using bindings other than NetTcpBinding and HTTP based bindings


- For a WCF project to be applicable for this upgrade, it must meet the following requirements:
  - Include a .cs file that references `System.ServiceModel` and creates new `ServiceHost`
    - If the WCF project has multiple `ServiceHost`, all hosts need to be created in the same method.
  - Include a .config file that stores `System.ServiceModel` properties

> Note: If your project is not applicable for this tool, we recommend you to check out the [CoreWCF walkthrough guide](https://github.com/CoreWCF/CoreWCF/blob/main/Documentation/Walkthrough.md) and
[BeanTrader Sample demo](https://devblogs.microsoft.com/dotnet/upgrading-a-wcf-service-to-dotnet-6/) for guidance in manually updating your project.

## Migration Resources

To assist your migration process, please check out the following documentation and blogs:

- Step-by-Step demo: [Upgrade WCF Server-side Project to use CoreWCF on .NET 6](https://aka.ms/CoreWCFUpgradeAssistant)
- Manual upgrade guide: [Upgrading a WCF service to .NET 6 with CoreWCF](https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/migrate-to-windows-app-sdk-ovw)
- CoreWCF Blog: [CoreWCF 1.0 has been Released, WCF for .NET Core and .NET 5+](https://devblogs.microsoft.com/dotnet/corewcf-v1-released/)

After upgrading the project, you'll need to compile and test them. Upgrade Assistant will do what it can, but it can't solve every incompatibility as part of the project upgrade. Please pay close attention to the upgrade-assistant console output and complete any manual updates requested by the tool.

## Issues

- If you have any feedback for the CoreWCF extension, please open an issue [here](https://github.com/dotnet/upgrade-assistant/issues) with `area:WCF` tag.
- For upgrade-assistant tool related issues, please follow [here](https://github.com/dotnet/upgrade-assistant#engage-contribute-and-give-feedback).
- For CoreWCF related issues, please check the [CoreWCF Github Repo](https://github.com/CoreWCF/CoreWCF).