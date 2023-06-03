# About

This is a fork of the [ElmahCore](https://github.com/ElmahCore/ElmahCore) project, with changes for PR's that were sitting idle.
It should be an almost slot in replacement for ElmahCore v2.1.2.

See [changelog.md](changelog.md) for any further changes from v2.1.2

**Please note:** The source code for the front end appears non-existent, in ElmahCore the front end Vue SPA files are all [minified](https://github.com/ElmahCoreEx/ElmahCoreEx/issues/77). Consider this a warning sign for the continuation of the front end without a rewrite WITH SOURCE. Source-maps may have enough content to obtain the code but this has not be investigated.

The interfaces and namespaces have been kept the same.

# License

This project is licensed under the terms of the Apache license 2.0.

# Using ElmahCore

ELMAH for Net.Standard 2.0 and .Net 6

Add NuGet package **ElmahCoreEx**

## Simple usage

Startup.cs

```csharp
services.AddElmah() in ConfigureServices
app.UseElmah(); in Configure
```

`app.UseElmah()` must be set after initializing other exception handling middleware, such as (`UseExceptionHandler`, `UseDeveloperExceptionPage`, etc.)

Default Elmah path `~/elmah`.

## Change URL path

```csharp
services.AddElmah(options => options.Path = "you_path_here")
```

## Restrict access to the Elmah URL

```csharp
services.AddElmah(options =>
{
    options.OnPermissionCheck = context => context.User.Identity.IsAuthenticated;
});
```

**Note:** `app.UseElmah();` needs to be placed after `UseAuthentication` and `UseAuthorization`

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseElmah();
```

or the user will be redirected to the sign in screen even if they are authenticated.

## Change Error Log type

You can create your own error log, which will store errors anywhere.

```csharp
    class MyErrorLog: ErrorLog
    // Implement ErrorLog
```

This ErrorLogs available:

- MemoryErrorLog – store errors in memory (by default)
- XmlFileErrorLog – store errors in XML files
- SqlErrorLog - store errors in MS SQL (add reference to [ElmahCore.Sql](https://www.nuget.org/packages/ElmahCoreEx.Sql))
- MysqlErrorLog - store errors in MySQL (add reference to [ElmahCore.MySql](https://www.nuget.org/packages/ElmahCoreEx.MySql))
- PgsqlErrorLog - store errors in PostgreSQL (add reference to [ElmahCore.Postgresql](https://www.nuget.org/packages/ElmahCoreEx.Postgresql))

```csharp
services.AddElmah<XmlFileErrorLog>(options =>
{
    options.LogPath = "~/log"; // OR options.LogPath = "с:\errors";
});
```

```csharp
services.AddElmah<SqlErrorLog>(options =>
{
    options.ConnectionString = "connection_string";
    options.SqlServerDatabaseSchemaName = "Errors"; //Defaults to dbo if not set
    options.SqlServerDatabaseTableName = "ElmahError"; //Defaults to ELMAH_Error if not set
});
```

## Raise exception

```csharp
public IActionResult Test()
{
    HttpContext.RaiseError(new InvalidOperationException("Test"));
    ...
}
```

## Microsoft.Extensions.Logging support

Since version 2.0 ElmahCore support Microsoft.Extensions.Logging

## Source Preview

Since version 2.0.1 ElmahCore support source preview.
Just add paths to source files.

```csharp
services.AddElmah(options =>
{
   options.SourcePaths = new []
   {
      @"D:\tmp\ElmahCore.DemoCore3",
      @"D:\tmp\ElmahCore.Mvc",
      @"D:\tmp\ElmahCore"
   };
});
```

## Log the request body

Since version 2.0.5 ElmahCore can log the request body.

## Logging SQL request body

Since version 2.0.6 ElmahCore can log the SQL request body.

## Logging method parameters

Since version 2.0.6 ElmahCore can log method parameters.

```csharp
using ElmahCore;
...

public void TestMethod(string p1, int p2)
{
    // Logging method parameters
    this.LogParams((nameof(p1), p1), (nameof(p2), p2));
    ...
}

```

## Using UseElmahExceptionPage

You can replace UseDeveloperExceptionPage to UseElmahExceptionPage

```csharp
if (env.IsDevelopment())
{
   //app.UseDeveloperExceptionPage();
   app.UseElmahExceptionPage();
}
```

## Using Notifiers

You can create your own notifiers by implement IErrorNotifier or IErrorNotifierWithId interface and add notifier to Elmah options:

```csharp
services.AddElmah<XmlFileErrorLog>(options =>
{
    options.Path = @"errors";
    options.LogPath = "~/logs";
    options.Notifiers.Add(new ErrorMailNotifier("Email",emailOptions));
});
```

Each notifier must have unique name.

## Using Filters

You can use Elmah XML filter configuration in separate file, create and add custom filters:

```csharp
services.AddElmah<XmlFileErrorLog>(options =>
{
    options.FiltersConfig = "elmah.xml";
    options.Filters.Add(new MyFilter());
})
```

Custom filter must implement IErrorFilter.
XML filter config example:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<elmah>
 <errorFilter>
  <notifiers>
   <notifier name="Email"/>
  </notifiers>
  <test>
   <and>
    <greater binding="HttpStatusCode" value="399" type="Int32" />
    <lesser  binding="HttpStatusCode" value="500" type="Int32" />
   </and>
  </test>
 </errorFilter>
</elmah>
```

see more [here](https://elmah.github.io/a/error-filtering/examples/)

JavaScript filters not yet implemented

Add notifiers to errorFilter node if you do not want to send notifications
Filtered errors will be logged, but will not be sent.
