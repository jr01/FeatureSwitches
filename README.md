# Feature Switches

Switch application features on, off, or to any defined value.

* Conditional feature switch support (boolean)

    ```C#
    bool isOn = await featureService.IsOn("feature");
    ```

* Any type can be switched to any value; for example to do A/B testing.

    ```C#
    bool isOn = await featureService.GetValue<bool>("feature");
    var myTypedValue = await featureService.GetValue<MyType>("feature");
    if (myTypedValue.Setting == 'A')
        ...
    ```

* Feature filters and filter groups, allowing for complex rule evaluation.
* Turn a feature on/off on feature and filter group level. (aka main/kill-switch)
* Contextual feature evaluation.
  * Via evaluation context parameter: `bool isOn = featureService.IsOn("feature", evaluationContext: "mycontext");`
  * Via evaluation context accessor. `serviceCollection.AddScoped<IEvaluationContextAccessor, MyEvaluationContextAccessor>()`
  * Both parameter and accessor can be combined together.
* Out-of-the-box feature filters
  * `DateTime` to set a feature on or off on a specific date.
  * `ParallelChange` - in conjunction with an evaluation context parameter gives support for the [ParallelChange pattern](https://martinfowler.com/bliki/ParallelChange.html) aka Expand/(Migrate/)Contract pattern.
* Configurable feature evaluation caching
  * For performance and 'stable' feature state.
  * Supports multiple caches / cache levels.
    * For example
      * Level-1: In-memory cache with a scoped lifetime.
      * Level-2: Distributed cache like Redis.
  * Feature evaluations are cached using the evaluation context.
* Pluggable feature definition providers
  * `InMemoryFeatureDefinitionProvider` can be used in automated tests, or as an intermediate.
    * Load feature definitions from JSON
    * Load feature definitions programmatically
* Dependency Injection framework independent.
* MSTest attributes - run a unit test multiple times with feature on/off.

## Usage

### Basic usage

* Define a boolean feature

    ```C#
    var featureDefinitionProvider = serviceProvider.GetRequired<InMemoryFeatureDefinitionProvider>();
    featureDefinitionProvider.SetFeature("MyBoolFeature", isOn: true);
    ```

    _Note: the feature is on by default, use `isOn` to turn the feature off_

* Use the feature switch

    ```C#
    public class MyClass
    {
        private readonly IFeatureService featureService;

        public Some(IFeatureService featureService)
        {
            this.featureService = featureService;
        }

        public async Task Execute()
        {
            if (await this.featureService.IsOn("MyBoolFeature"))
            {
                ...
            }

            // or
            if (await this.featureService.GetValue<bool>("MyBoolFeature"))
            {
                ...
            }
        }
    }
    ```

    _Note: if the feature hasn't been defined false will be returned._

### Dependency registration

When using Microsoft.Extensions.DependencyInjection, e.g. ASP.NET Core, you can use:

```C#
// dotnet add package FeatureSwitches.ServiceCollection

serviceCollection.AddFeatureSwitches(addScopedCache: true);
```

Or instead of that NuGet define what you need manually:

```C#
serviceCollection.AddSingleton<IFeatureFilterMetadata, DateTimeFeatureFilter>();
serviceCollection.AddScoped<FeatureService>();
serviceCollection.AddScoped<IFeatureCache, InMemoryFeatureCache>();

serviceCollection.AddSingleton<InMemoryFeatureDefinitionProvider>();
serviceCollection.AddSingleton<IFeatureDefinitionProvider>(sp => sp.GetRequiredService<InMemoryFeatureDefinitionProvider>());
serviceCollection.AddSingleton<IFeatureCacheContextAccessor, EmptyFeatureCacheContextAccessor>();
```

When using Autofac:

```C#
builder.RegisterType<DateTimeFeatureFilter>().As<IFeatureFilterMetadata>().SingleInstance();
builder.RegisterType<FeatureService>().As<IFeatureService>().InstancePerLifetimeScope();
builder.RegisterType<InMemoryFeatureCache>().As<IFeatureCache>().InstancePerLifetimeScope();

builder.RegisterType<InMemoryFeatureDefinitionProvider>()
    .AsSelf()
    .As<IFeatureDefinitionProvider>()
    .SingleInstance();
```

### Feature types

* Define a custom feature type

    ```C#
    public enum Direction
    {
        Left,
        Right
    }

    featureDefinitionProvider.SetFeature(
        "DirectionFeature",
        isOn: true,
        offValue: Direction.Left,
        onValue: Direction.Right );
    ```
* Usage

    ```C#
    if (await this.featureService.GetValue<Direction>("DirectionFeature") == DirectionFeature.Left)
    {
        ...
    }
    ```

    _Note: If the switch value cannot be converted to the featuretype the offValue will be returned._

### Feature evaluation context accessor

* Ambient evaluation context

    ```C#
    public class MyEvaluationContextAccessor : IEvaluationContextAccessor
    {
        public object? GetContext()
        {
            return (Thread.CurrentPrincipal?.Identity as ClaimsIdentity)?.Name;
        }
    }

    serviceCollection.AddSingleton<IEvaluationContextAccessor, MyEvaluationContextAccessor>();
    ```

* Dependency scope evaluation context

    ```C#
    public class MyScopedEvaluationContextAccessor : IEvaluationContextAccessor
    {
        private readonly MyApplicationContext context;

        public MyScopedEvaluationContextAccessor(MyApplicationContext context)
        {
            this.context = context;
        }

        public object? GetContext()
        {
            return this.context?.Name;
        }
    }

    serviceCollection.AddScoped<IEvaluationContextAccessor, MyScopedEvaluationContextAccessor>();
    ```

### Feature filters

A feature is on when it is set to on and all applied feature filters decide that the feature should be on (logical AND).

* Scoped feature filters

    ```C#
    public class MyAppContext
    {
        public string Name { get; set; }
    }

    public class MyUserFeatureFilterSettings
    {
        public HashSet<string> AllowedNames { get; set; }
    }

    public class MyUserFeatureFilter : IFeatureFilter
    {
        private readonly MyAppContext appContext;

        public string Name => "User";

        public MyUserFeatureFilter(MyAppContext appContext)
        {
            this.appContext = appContext;
        }

        public async Task<bool> IsOn(FeatureFilterEvaluationContext context)
        {
            var settings = context.GetSettings<MyUserFeatureFilterSettings>();
            return settings.AllowedNames.Contains(this.appContext.Name) ?? false;
        }
    }

    var featureDefinitionProvider = serviceProvider.GetRequired<InMemoryFeatureDefinitionProvider>();
    featureDefinitionProvider.SetFeature("MyBoolFeature", isOn: true);
    featureDefinitionProvider.SetFeatureFilter("MyBoolFeature", "User", config: "{ \"AllowedNames\": [\"John\", \"Jane\"] }");
    // or
    featureDefinitionProvider.SetFeatureFilter("MyBoolFeature", "User", config: new MyUserFeatureFilterSettings { AllowedNames = new HashSet<string> { "John", "Jane" } });

    serviceCollection.AddScoped<MyAppContext>();

    using (var scope = serviceProvider.CreateScope())
    {
        scope.ServiceProvider.GetRequired<MyAppContext>().Name = "John";
        var featureService = scope.ServiceProvider.GetRequired<FeatureService>();
        Assert.IsTrue(await featureService.IsOn("MyBoolFeature"));
    }
    ```

* Feature filter groups

    A feature is on when it is set to on and the first applied filter groups decides that the feature should indeed be on  (Logical OR).
    The feature's `OnValue` is taken from that filter group.

    Feature filters can be applied to a feature filter group.

    As an example we define a feature with 2 filter groups: group A and group B. Users in group A (John) should always get the feature. Users in group B (Jane) should get the feature from a certain launch date.

    ```C#
    public enum AB
    {
        Off,
        A,
        B
    }

    var featureDefinitionProvider = serviceProvider.GetRequired<InMemoryFeatureDefinitionProvider>();
    featureDefinitionProvider.SetFeature<AB>("Feature", isOn: true, offValue: AB.Off);
    featureDefinitionProvider.SetFeatureGroup("Feature", "GroupA", isOn:true, onValue: AB.A);
    featureDefinitionProvider.SetFeatureGroup("Feature", "GroupB", isOn:true, onValue: AB.B);

    featureDefinitionProvider.SetFeatureFilter("Feature", "User", group: "GroupA", config: new MyUserFeatureFilterSettings { AllowedNames = new HashSet<string> { "John" } });

    featureDefinitionProvider.SetFeatureFilter("Feature", "User", group: "GroupB", config: new MyUserFeatureFilterSettings { AllowedNames = new HashSet<string> { "Jane" } });
    featureDefinitionProvider.SetFeatureFilter("Feature", "DateTime", group: "GroupA", config: new DateTimeFeatureFilterSettings { From = new DateTimeOffSet(new DateTime(2020, 11, 3)) });

    using (var scope = serviceProvider.CreateScope())
    {
        scope.ServiceProvider.GetRequired<MyAppContext>().Name = "John";
        Assert.AreEqual(AB.A, await featureService.GetValue<AB>("Feature"));
    }
    using (var scope = serviceProvider.CreateScope())
    {
        scope.ServiceProvider.GetRequired<MyAppContext>().Name = "Jane";

        SystemClock.Now = new DateTimeOffSet(new DateTime(2020, 11, 2));
        Assert.AreEqual(AB.Off, await featureService.GetValue<AB>("Feature"));

        SystemClock.Now = new DateTimeOffSet(new DateTime(2020, 11, 3));
        Assert.AreEqual(AB.B, await featureService.GetValue<AB>("Feature"));
    }
    using (var scope = serviceProvider.CreateScope())
    {
        scope.ServiceProvider.GetRequired<MyAppContext>().Name = "James";
        Assert.AreEqual(AB.Off, await featureService.GetValue<AB>("Feature"));
    }

    ```

### Contextual feature filters

A contextual feature filter implements `IContextualFeatureFilter`. A handy `ContextualFeatureFilter<T>` subclass is provided.

```C#
public class MyAppContext
{
    public string Name { get; set; }
}

public class MyUserFeatureFilter : ContextualFeatureFilter<MyAppContext>
{
    public string Name => "User";

    public async Task<bool> IsOn(FeatureFilterEvaluationContext context, MyAppContext appContext)
    {
        var settings = context.Parameters.Get<MyUserFeatureFilterSettings>();
        return settings?.AllowedNames.Contains(appContext.Name) ?? false;
    }
}

var featureDefinitionProvider = serviceProvider.GetRequired<InMemoryFeatureDefinitionProvider>();
featureDefinitionProvider.SetFeature("MyBoolFeature", isOn: true);
featureDefinitionProvider.SetFeatureFilter("MyBoolFeature", "User", "{ \"allowedNames\": [\"John\", \"Jane\"] }");

var featureService = serviceProvider.GetRequired<FeatureService>();
Assert.IsTrue(await featureService.IsOn("MyBoolFeature", new MyAppContext { Name = "John" }));
```

### ParallelChange pattern aka Expand/Migrate/Contract

The ParallelChange contextual feature filter can be applied to any feature or feature filter group.

* Define the feature

    ```C#
    var featureDefinitionProvider = serviceProvider.GetRequired<InMemoryFeatureDefinitionProvider>();
    featureDefinitionProvider.SetFeature("MyBoolFeature", isOn: true);
    featureDatabase.SetFeatureFilter("FeatureA", "ParallelChange", "\"Expanded\"");
    ```

* When writing data

    ```C#
    if (!await featureService.IsOn("feature", ParallelChange.Contracted)) {
        Perform_Old_DataWrite();
    }
    if (await featureService.IsOn("feature", ParallelChange.Expanded)) {
        Perform_New_DataWrite();
    }
    ```

* When checking in the UI if the feature is on

    ```C#
    if (await featureService.IsOn("feature", ParallelChange.Migrated)) {
        Perform_New_UI();
    } else {
        Perform_Old_UI();
    }
    ```

* Alternatively the UI can do

    ```C#
    if (await featureService.IsOn("feature")) {
        Perform_New_UI();
    } else {
        Perform_Old_UI();
    }
    ```

## Loading features from JSON

The `InMemoryFeatureDefinitionProvider` supports loading features from a JSON file.

```json
[
  {
    "Name": "FeatureA",
    "OffValue": false,
    "IsOn": true,
    "OnValue": false,
    "Filters": [
      {
        "Name": "User",
        "Settings": {
            "AllowedNames": [
                "John"
            ]
        },
        "Group": "GroupA"
      },
      {
        "Name": "User",
        "Settings": {
            "AllowedNames": [
                "Jane"
            ]
        },
        "Group": "GroupB"
      }
    ],
    "FilterGroups": [
      {
        "Name": "GroupA",
        "IsOn": false,
        "OnValue": true
      },
      {
        "Name": "GroupB",
        "IsOn": true,
        "OnValue": true
      }
    ]
  }
]
```

```C#
var featureDefinitionProvider = serviceProvider.GetRequired<InMemoryFeatureDefinitionProvider>();
featureDefinitionProvider.LoadFromJson(json);

// or do your own deserialization
using (var fs = File.OpenRead("features.json"))
{
    var definitions = await System.Text.Json.JsonSerializer.DeserializeAsync<IEnumerable<FeatureDefinition>>(fs);
    featureDefinitionProvider.Load(definitions);
}
```

## Loading features from a database and a UI to modify definitions

There are many different databases, dataaccess layers, UI frameworks and that's why this library doesn't come with any of those.

To give some direction for an Entity Framework + SQL setup. A SQL schema might look roughly like:

```sql
CREATE TABLE [Features] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(50) NOT NULL,
    [Description] nvarchar(200) NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [OffValue] varbinary(100) NOT NULL,
    [IsOn] bit NOT NULL,
    [OnValue] varbinary(100) NOT NULL
);

CREATE TABLE [FeatureFilterGroups] (
    [Id] uniqueidentifier NOT NULL,
    [FeatureId] uniqueidentifier NOT NULL,
    [Name] nvarchar(50) NOT NULL,
    [IsOn] bit NOT NULL,
    [OnValue] varbinary(100) NOT NULL
);

CREATE TABLE [FeatureFilters] (    
    [Id] uniqueidentifier NOT NULL,
    [FeatureId] uniqueidentifier NOT NULL,
    [GroupId] uniqueidentifier NULL,
    [Type] nvarchar(50) NOT NULL,
    [Settings] varbinary(max) NOT NULL,
);
```

Of course you should define primary keys, foreign key relations, any indexes and more columns depending on your requirements.

Entities are similar to the tables:

```c#
public class Feature
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public IList<ApplicationFeatureFilter> Filters { get; private set; } = new List<ApplicationFeatureFilter>();

    public IList<ApplicationFeatureFilterGroup> Groups { get; private set; } = new List<ApplicationFeatureFilterGroup>();
    ...
}

...
public void Configure(EntityTypeBuilder<Feature> builder)
{
    builder.HasMany(x => x.Filters)
        .WithOne(x => x.Feature)
        .IsRequired(false)
        .HasForeignKey(x => x.FeatureId);

    builder.HasMany(x => x.Groups)
        .WithOne(x => x.Feature)
        .IsRequired(false)
        .HasForeignKey(x => x.FeatureId);
}
```

A provider:

```C#

public class DatabaseFeatureDefinitionProvider : IFeatureDefinitionProvider
{
    private readonly DbContext dbContext;

    public DatabaseFeatureDefinitionProvider(DbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<string[]> GetFeatures(CancellationToken cancellationToken = default)
    {
        var features = await this.dbContext.ApplicationFeatures
            .OrderBy(x => x.Name)
            .Select(x => x.Name)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return features;
    }

    public async Task<FeatureDefinition?> GetFeatureDefinition(string feature, CancellationToken cancellationToken = default)
    {
        var applicationFeature = await this.dbContext.ApplicationFeatures
            .Select(x => new
            {
                x.Name,
                x.IsOn,
                x.OnValue,
                x.OffValue,
                Filters = x.Filters.Select(f => new
                {
                    f.Type,
                    f.Settings,
                    f.GroupId
                }).ToList(),
                Groups = x.Groups.Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.IsOn,
                    g.OnValue
                }).ToList()
            })
            .Where(x => x.Name == feature)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        
        if (applicationFeature == null)
        {
            return null;
        }

        var featureDefinition = new FeatureDefinition
        {
            Name = applicationFeature.Name,
            OffValue = JsonSerializer.Deserialize<object?>(applicationFeature.OffValue),
            IsOn = applicationFeature.IsOn,
            OnValue = JsonSerializer.Deserialize<object?>(applicationFeature.OnValue)
        };

        var groups = new Dictionary<Guid, string>();
        foreach (var group in applicationFeature.Groups)
        {
            featureDefinition.FilterGroups.Add(new FeatureFilterGroupDefinition
            {
                OnValue = JsonSerializer.Deserialize<object?>(group.OnValue),
                Name = group.Name,
                IsOn = group.IsOn
            });

            groups.Add(group.Id, group.Name);
        }

        foreach (var filter in applicationFeature.Filters)
        {
            featureDefinition.Filters.Add(new FeatureFilterDefinition
            {
                Group = filter.GroupId == null ? null : groups[filter.GroupId.Value],
                Name = filter.Type,
                Settings = JsonSerializer.Deserialize<object?>(filter.Settings)
            });
        }

        return featureDefinition;
    }
}
```

Be aware that each `await featureService.IsOn(feature)` call will, if the feature evaluation is not cached, invoke the `DatabaseFeatureDefinitionProvider.GetFeatureDefinition(` and query the database.

It depends on your specific setup if that is acceptable or if you need any caching. Caching can be done using the feature evaluation caching system, or you could cache the query results within the `DatabaseFeatureDefinitionProvider` in for example Redis.

## Caching feature evalutions and stable feature evalution results

Caching is a complex topic. What you need all depends on your requirements (business, performance, etc) and application setup.

The `InMemoryFeatureCache` uses a simple ConcurrenctDictionary and typically is registered with a scoped lifetime. Any subsequent calls to `await featureService.IsOn(feature)` within the dependency scope will deliver the cached result. This can be perfect for an http request handler (e.g. ASP.Net controller) where you want both performance and a stable feature evaluation result during the request.

### Distributed computing

In a distributed application setup you might have multiple instances serving requests and one instance could evaluate the feature to be on (for example a time activated feature), while at the same time another instance could evaluate the feature to be off. This might, or might not be acceptable, it all depends on your requirements.

If this is not acceptable you probably need a distributed cache like Redis. Since there are many cache subtleties (TTL on cached entries, Redis 6 client-caching yes or no, cache invalidation) implementing an `IFeatureCache` is up to you.

```C#
serviceCollection.AddScoped<IFeatureCache, YourRedisFeatureCache>();
```

You can also setup Redis as a 2nd level feature evaluation cache:

```C#
serviceCollection.AddScoped<IFeatureCache, InMemoryFeatureCache>();
serviceCollection.AddScoped<IFeatureCache, YourRedisFeatureCache>();
```

## FeatureSwitches.MSTest

The `FeatureSwitches.MSTest` nuget delivers functionality to run a test multiple times for all defined feature On/Off combinations.

Run the same test twice with feature On and Off:

```C#
[FeatureTestMethod(onOff: "FeatureA")]
public void MyTestMethod()
{
    var featureDefinitionProvider = serviceProvider.GetRequired<InMemoryFeatureDefinitionProvider>();
    var featureService = serviceProvider.GetRequired<IFeatureService>();

    featureDefinitionProvider.Load(FeatureTestMethodAttribute.Features);

    if (await featureService.IsOn("FeatureA"))
    {
        // FeatureA is On asserts
    }
    else
    {
        // FeatureA is Off asserts
    }
}
```

The `FeatureTestMethodAttribute` defines a feature with `Name=FeatureA,OnValue=true,OffValue=false`. The `FeatureTestMethodAttribute.Features` is an enumerable that contains the feature definitions for the current test run with their `IsOn=true/false`.

The feature definitions can also be loaded in test initialize, or the test class constructor.

```C#
[TestClass]
public class MyTestClass
{
    public MyTestClass()
    {
        ...
        var featureDefinitionProvider = serviceProvider.GetRequired<InMemoryFeatureDefinitionProvider>();
        featureDefinitionProvider.Load(FeatureTestMethodAttribute.Features);
    }

    // or
    [TestInitialize]
    public async Task Initialize()
    {
        ...
        var featureDefinitionProvider = serviceProvider.GetRequired<InMemoryFeatureDefinitionProvider>();
        featureDefinitionProvider.Load(FeatureTestMethodAttribute.Features);
    }

    [FeatureTestMethod(onOff: "FeatureA")]
    public void MyTestMethod()
    {
        ...
        var featureService = serviceProvider.GetRequired<IFeatureService>();
        if (await featureService.IsOn("FeatureA"))
        {
            // FeatureA is On asserts
        }
        else
        {
            // FeatureA is Off asserts
        }
    }
}
```

Set features On or Off without varying them.

```C#
[FeatureTestMethod(onOff: "FeatureA", on: "AlwaysOn", off: "AllwaysOn")]
public void MyTestMethod()
{
    ...
    var featureService = serviceProvider.GetRequired<IFeatureService>();

    Assert.IsTrue(await featureService.IsOn("AlwaysOn"));
    Assert.IsFalse(await featureService.IsOff("AlwaysOff"));

    if (await featureService.IsOn("FeatureA"))
    {
        // FeatureA is On asserts
    }
    else
    {
        // FeatureA is Off asserts
    }
}
```

### Testing with Feature types

```C#
[FeatureTestMethod(onOff: "FeatureA")]
[FeatureTestValue("FeatureA", onValue: "On", offValue: "Off")]
public void MyTestMethod()
{
    ...
    var featureService = serviceProvider.GetRequired<IFeatureService>();
    var featureValue = await featureService.GetValue<string>("FeatureA");
    if (featureValue == "On")
    {
        // Feature is On asserts
    }
    else if (featureValue == "Off")
    {
        // Feature is Off asserts
    }
}
```

The `FeatureTestValueAttribute` defines the on and off values for a feature. It's not necessary to specify the attribute for boolean features as that is assumed to be the default when no `FeatureTestValueAttribute` is defined.

The `FeatureTestMethodAttribute.Features` is an enumerable that contains all feature definitions for the current test invocation with their `IsOn=true/false`.

Use multiple on values.
In the following example the test is run 3 times. 1x with FeatureA off, 1x with FeatureA set to AB.A and 1x with FeatureA set to AB.B .

```C#
[FeatureTestMethod(onOff: "FeatureA")]
[FeatureTestValue("FeatureA", onValues: new object[] { AB.A, AB.B }, offValue: AB.Off)]
public void MyTestMethod()
{
    ...
    var featureService = serviceProvider.GetRequired<IFeatureService>();
    var featureValue = await featureService.GetValue<AB>("FeatureA");
    if (featureValue == AB.Off)
    {
        // FeatureA is Off asserts
    }
    else if (featureValue == AB.A)
    {
        // FeatureA is A asserts
    }
    else if (featureValue == AB.B)
    {
        // FeatureA is B asserts
    }
}
```

Vary between multiple features and their values.
In the following example the test will be run 4 times: 2^(#onOffFeatures).

```C#
[FeatureTestMethod(onOff: "FeatureA,FeatureB")]
public void MyTestMethod()
{
    if (await featureService.IsOn("FeatureA"))
    {
        if (await featureService.IsOn("FeatureB"))
        {
            // FeatureA is On & Feature B is On asserts
        }
        else
        {
            // FeatureA is On & Feature B is Off asserts
        }
    }
    else
    {
        if (await featureService.IsOn("FeatureB"))
        {
            // FeatureA is Off & Feature B is On asserts
        }
        else
        {
            // FeatureA is Off & Feature B is Off asserts
        }
    }
}
```

### Testing with feature filters (future work: not yet implemented)

Define a feature filter with a single configuration

```C#
[FeatureTestMethod(on: "FeatureA")]
[FeatureTestFilter("FeatureA", "ParallelChange", ParallelChange.Expanded)];
public void MyTestMethod()
{
    ...
    Assert.IsTrue(await featureService.IsOn("FeatureA", ParallelChange.Expanded));
}
```

Define a feature filter with multiple configurations. The test will be run for each configuration.
In the following case the test will be run 4 times:
    1. FeatureA off
    2. FeatureA on and ParallelChange.Expanded
    3. FeatureA on and ParallelChange.Migrated
    4. FeatureA on and ParallelChange.Contracted

```C#
[FeatureTestMethod(onOff: "FeatureA")]
[FeatureTestFilter("FeatureA", "ParallelChange", new object[] { ParallelChange.Expanded, ParallelChange.Migrated, ParallelChange.Contracted })]
public void MyTestMethod()
{
    ...
    if (!await featureService.IsOn("FeatureA", ParallelChange.Contracted)) {
        // Assert Old_DataWrite();
    }
    if (await featureService.IsOn("FeatureA", ParallelChange.Expanded)) {
        // Assert New_DataWrite();
    }

    if (await featureService.IsOn("FeatureA", ParallelChange.Migrated)) {
        // Assert New_UI();
    } else {
        // Assert Old_UI();
    }
}

```
