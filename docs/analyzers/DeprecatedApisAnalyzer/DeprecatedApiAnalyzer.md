# UA0100: Refactor usage to alternate API

This will flag any types defined by adapter descriptors and will offer to change the type instance to the abstraction.

> Note: This should be done after all members have been added and no UA0101 are raised)

For example:

```csharp
[assembly: AdapterDescriptor(typeof(HttpContext), typeof(IHttpContext))]

public interface IHttpContext
{
}

public void Method(HttpContext context)
{
}
```

will be changed to


```csharp
[assembly: AdapterDescriptor(typeof(HttpContext), typeof(IHttpContext))]

public interface IHttpContext
{
}

public void Method(IHttpContext context)
{
}
```

# UA0101: Add member to abstraction

This analyzer will detect if any types for defined descriptors are using members that are not in the current abstraction. If so, it will offer to add that member to the abstraction.

For example:

```csharp
[assembly: AdapterDescriptor(typeof(HttpContext), typeof(IHttpContext))]
[assembly: AdapterDescriptor(typeof(HttpRequest), typeof(IHttpRequest))]

public interface IRequest
{
}

public interface IHttpContext
{
}

public void Method(HttpContext context)
{
  var request = context.Request;
}
```

will be transformed to:


```csharp
[assembly: AdapterDescriptor(typeof(HttpContext), typeof(IHttpContext))]
[assembly: AdapterDescriptor(typeof(HttpRequest), typeof(IHttpRequest))]

public interface IRequest
{
}

public interface IHttpContext
{
  IRequest Request { get; }
}

public void Method(HttpContext context)
{
  var request = context.Request;
}
```

# UA0103: Redirect static member access to alternate member

This analyzer will look for `AdapterStaticDescriptor` instances to identify if static method calls should be redirected.

For example:

```csharp
[assembly: AdapterStaticDescriptor(typeof(System.Web.HttpContext), nameof(System.Web.HttpContext.Current), typeof(HttpContextFactory), nameof(HttpContextFactory.Current))]

public static class HttpContextFactory
{
  public static HttpContext Current => HttpContext.Current;
}

public void Method()
{
  var current = HttpContext.Current;
}
```

will be transformed to:

```csharp
[assembly: AdapterStaticDescriptor(typeof(System.Web.HttpContext), nameof(System.Web.HttpContext.Current), typeof(HttpContextFactory), nameof(HttpContextFactory.Current))]

public static class HttpContextFactory
{
  public static HttpContext Current => HttpContext.Current;
}

public void Method()
{
  var current = HttpContextFactory.Current;
}
```

# UA0110: Create abstraction for descriptor

This will identify descriptors that only have a single type and offer to create an abstraction for it in the current project. For example:

```csharp
[assembly: AdapterDescriptor(typeof(HttpContext))]
```

will be offered to turn into:

```csharp
[assembly: AdapterDescriptor(typeof(IHttpContext))]
```

# UA0111: Add an adapter descriptor

This will identify well known types and suggest creating an adapter descriptor for it. If the attribute is not defined, it will create it and add it to the project as well.