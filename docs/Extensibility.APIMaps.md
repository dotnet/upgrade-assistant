# API Map transformations

## Overview

Its pretty common for different project flavor upgrades to:

- replace old namespaces with new ones (in usings or in invocations etc)
- replace old type names (including namespace) with new types
- replace member invocations (methods, properties) static or instance based with new ones

and/or add a comment next to a corresponding old code element with suggestions about manual upgrade steps when automatic upgrades are not possible.

All of that is generalized via API maps and corresponding base abstract transformers to be subclassed for each project scenario and provide corresponding maps to be executed by the base transformers logic.

All base transformers working with API maps support both C# and VB.

## API map format

An API map is a json file containing a dictionary of map between old entity (namespace, type or member) to the model describing how to replace it with new one.

Here is a sample json with descriptions for each model element:

```json
{
  "Windows.UI.WindowManagement.AppWindow.TryCreateAsync": {
    "value": "Microsoft.UI.Windowing.AppWindow.Create", // new value to replace old one with, if empty if state is not Replaced
    "kind": "method", // method|property|namespace|type
    "state": "Replaced", // Replaced|Removed|NotImplemented
    "isStatic": true,
    "needsManualUpgrade": false, // if true, only comment is added, no other code modifications happening
    "documentationUrl": "some url", // link to documentation URL,
    "needsTodoInComment": true, // if true TODO is added to the comment if comment is being added
    "isAsync": false,
    "messageId": "resource id", // [internal only] in case custom comment needs to be added, this resource id will be looked up in the ResourceManager,
    "MessageParams": [ "", "" ] // [internal only] parameters to be passed into string format for custom message
  }
}
```
