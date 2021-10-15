# Hybrid Web Solutions

As large web solutions are migrated from .NET Framework to .NET Core and later, it is very common for the process to take many months and even years. During this time, the code is often continuing to be modified and deployed to add additional business value. Migration must often continue alongside this.

A common issue in many of these large products are dependencies on System.Web deep within the business logic of the application. Often, this is specifically `HttpContext` and the class that come off of that. The focus of this document will be to address how we can guide customers to a place that will allow them to move towards .NET Core but still meeting the business needs of continually deploying the .NET Framework version until .NET Core is ready.

## Problem

## Solution

The adapter pattern is ideal for this situation, in that we can create an adapter that will allow both `System.Web.HttpContext` and `Microsoft.AspNet.Http.HttpContext` to be used via an abstraction throughout an application.

> Note: The behavior of the two `HttpContext` types are quite different in many ways which this document does not attempt to address. The focus of this document is to provide a pattern as to how developers may begin to address pervasive usage of this type.

`UA0014` is an analyzer that allows a project to specify what adapters should be used for a given type and provides codefixers to automate that change. This can be defined by first defining an attribute of the following shape:

```csharp
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class AdapterDescriptor : Attribute
{
    public AdapterDescriptor(Type original, Type? interfaceType)
    {
    }
}
```

This can then be used to register what expected adapters should be. Assuming the following interfaces are defined (see the example below for how this may be set up), the adapters may be registered as follows:

```csharp
[assembly: AdapterDescriptor(typeof(System.Web.HttpContext), typeof(IHttpContext))]
[assembly: AdapterDescriptor(typeof(System.Web.HttpContextBase), typeof(IHttpContext))]
[assembly: AdapterDescriptor(typeof(System.Web.HttpRequest), typeof(IRequest))]
```

Now, the analyze `UA0014` will start flaggin any usage of the types on the right hand side and will offer to replace them with the abstraction desired. This can be applied at the scope of document/project/solution level.

In order to apply it beyond the current project, the abstractions should be moved to a separate project. In that project, the adapters may be registered and will apply to any project that references it.

## Example

This process should be applied to a project *before* attempting to migrate to .NET Core. This will help remove the most common usage of many `System.Web` classes and should be able to be adapted fairly trivially to work with other classes that cause problems when migrating.

1. Apply UA0005 that will replace `HttpContext.Current` with a custom implementation of an accessor that works on both .NET Framework or .NET Core depending on the compilation target.
2. This may be centralized to a shared project if desired. If so, the target frameworks should be set up to target `.NET Standard 2.0`, `net46` (or appropraite), and `net5.0` (or appropriate). This will allow for sharing the code with all projects and can light up implementations of `HttpContext` where available. See example project [here](./hybrid_example).
3. Create a set of interfaces that mimic the structure of `HttpContext`. For example:

    ```csharp
    public interface IHttpContext
    {
        IRequest Request { get; }

        IResponse Response { get; }
    }

    public interface IRequest
    {
        IHeaders Headers { get; }
    }

    public interface IResponse
    {
    }

    public interface IHeaders
    {
        string this[string name] { get; }
    }

    public class SystemWebHttpContext : IHttpContext, IRequest, IResponse
    {
        private readonly HttpContext _context;

        public SystemWebHttpContext(HttpContext context)
        {
            _context = context;
        }

        public IRequest Request => this;

        public IResponse Response => this;

        IHeaders IRequest.Headers => new Headers(_context.Request.Headers);
    }

    public class Headers : IHeaders
    {
        private readonly NameValueCollection _collection;

        public Headers(NameValueCollection collection)
        {
            _collection = collection;
        }

        public string this[string name] => _collection[name];
    }
    ```
4. Annotate the assembly with the `HttpContextHelper` type with `AdapterDescriptor` attributes:

    ```csharp
    [assembly: AdapterDescriptor(typeof(System.Web.HttpContext), typeof(IHttpContext))]
    [assembly: AdapterDescriptor(typeof(System.Web.HttpContextBase), typeof(IHttpContext))]
    [assembly: AdapterDescriptor(typeof(System.Web.HttpRequest), typeof(IHttpRequest))]
    [assembly: AdapterDescriptor(typeof(System.Web.HttpResponse), typeof(IHttpResponse))]
    ```

5. Run UA0014 that will replace all parameters, return types, fields, and properties with the appropriate interface

6. The only place that should actually wrap the `System.Web.HttpContext` should be in the head project.

7. Once All `System.Web.HttpContext` related references are removed, the reference to `System.Web` itself should be removed.