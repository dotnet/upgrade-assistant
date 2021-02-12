This folder is for IConfigUpdaters that will be used to update the target
project based on its config files (app.config, web.config, etc.).

The ASP.NET Migration tool will probe assemblies in this directory at
runtime and use any public implementations of IConfigUpdater found during
the ConfigUpdaterStep migration step.