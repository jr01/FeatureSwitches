# Feature Switches

Switch application features on, off, or to any defined value.

* Conditional feature switch support (boolean)
```C#
     bool isEnabled = await featureService.IsEnabled("feature");
```
* Any type can be switched to any value; for example to do A/B testing.
```C#
    bool isEnabled = await featureService.GetValue<bool>("feature");
    var myTypedValue = await featureService.GetValue<MyType>("feature");
    if (myTypedValue.Setting == 'A')
        ...
```
* Feature filters and filter groups, allowing for complex rule evaluation.
* Contextual feature evaluation.
  * Via evaluation context parameter: `bool isEnabled = featureService.IsEnabled("feature", evaluationContext: "mycontext");`
  * Via evaluation context accessor. `serviceCollection.AddScoped<IEvaluationContextAccessor, MyEvaluationContextAccessor>()`
  * Both parameter and accessor can be combined together.
* Out-of-the-box feature filters  
  * `OnOff` accts as a main switch setting a feature either on or off.
  * `DateTime` to set a feature on or off on a specific date.
  * `ParallelChange` - in conjunction with an evaluation context parameter gives support for the [ParallelChange pattern](https://martinfowler.com/bliki/ParallelChange.html) aka Expand/(Migrate/)Contract pattern.
  * `SessionFeatureFilter` for stable feature evaluations during the user's session.
* Optimized for speed
  * In memory evaluation cache
    * Singleton by default.
    * Values are always cached using the evaluation context.
* Pluggable feature definition providers
  * `InMemoryFeatureProvider` can be used in automated tests, or as an intermediate.
* Support for stable feature evaluation during a user's session
  * Via a `SessionFeatureFilter` applied to session features (preferred)
  * Or via `SessionFeatureCache`
* Dependency Injection framework independent
  
## Usage

### Registration

```C#
// dotnet add package SoftWine.FeatureSwitches.ServiceCollection

serviceCollection.AddFeatureSwitches();
```

### Feature definition

* Define a boolean feature
    ```C#
    var featureDefinitionProvider = serviceProvider.GetRequired<InMemoryFeatureProvider>();
    featureDefinitionProvider.SetFeature("MyBoolFeature", isOn: true);
    ```

### Basic usage

```C#
public class MyClass
{
    private readonly IFeatureService featureService;

    public Some(IFeatureService featureService)
    {
        this.featureService = featureService;
    }

    public void Execute()
    {
        if (await this.featureService.IsEnabled("MyBoolFeature"))
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

_Note: if the feature hasn't been defined the false is returned._

### Feature types

* Define a custom feature type
    ```C#
    public enum Direction {
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

    _Note: If the current switch value cannot be converted to the featuretype the offValue is returned._


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
    ```
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

### Feature filters and groups

Feature rules define when a feature should be on or off.
A feature is on when all applied feature filters decide that the feature should be on (logical AND).

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

        public async Task<bool> IsEnabled(FeatureFilterEvaluationContext context)
        {
            var settings = context.GetSettings<MyUserFeatureFilterSettings>();
            return settings.AllowedNames.Contains(this.appContext.Name) ?? false;
        }
    }

    var featureDefinitionProvider = serviceProvider.GetRequired<InMemoryFeatureProvider>();
    featureDefinitionProvider.SetFeature("MyBoolFeature", isOn: true);
    featureDefinitionProvider.SetFeatureFilter("MyBoolFeature", "User", config: "{ \"AllowedNames\": [\"John\", \"Jane\"] }");
    // or
    featureDefinitionProvider.SetFeatureFilter("MyBoolFeature", "User", config: new MyUserFeatureFilterSettings { AllowedNames = new HashSet<string> { "John", "Jane" } });

    serviceCollection.AddScoped<MyAppContext>();

    using (var scope = serviceProvider.CreateScope())
    {
        scope.ServiceProvider.GetRequired<MyAppContext>().Name = "John";
        var featureService = scope.ServiceProvider.GetRequired<FeatureService>();
        Assert.IsTrue(await featureService.IsEnabled("MyBoolFeature"));
    }
    ```

* Feature filter groups
    A feature is on when the first filter group decides the feature should be on.
    A feature filter group has one or more feature filters.
    Each filter group defines it's 'OnValue'.

    ```C#
    public enum AB
    {
        Off,
        A,
        B
    }

    var featureDefinitionProvider = serviceProvider.GetRequired<InMemoryFeatureProvider>();
    featureDefinitionProvider.SetFeature<AB>("Feature", isOn: true, offValue: AB.Off);
    featureDefinitionProvider.SetFeatureGroup("Feature", "GroupA", onValue: AB.A);
    featureDefinitionProvider.SetFeatureGroup("Feature", "GroupB", onValue: AB.B);

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

    public async Task<bool> IsEnabled(FeatureFilterEvaluationContext context, MyAppContext appContext)
    {
        var settings = context.Parameters.Get<MyUserFeatureFilterSettings>();
        return settings?.AllowedNames.Contains(appContext.Name) ?? false;
    }
}

var featureDefinitionProvider = serviceProvider.GetRequired<InMemoryFeatureProvider>();
featureDefinitionProvider.SetFeature("MyBoolFeature", isOn: true);
featureDefinitionProvider.SetFeatureFilter("MyBoolFeature", "User", "{ \"allowedNames\": [\"John\", \"Jane\"] }");

var featureService = serviceProvider.GetRequired<FeatureService>();
Assert.IsTrue(await featureService.IsEnabled("MyBoolFeature", new MyAppContext { Name = "John" }));
```


### ParallelChange pattern aka Expand/Migrate/Contract

Out of the box the library ships with a ParallelChange contextual feature filter. This filter can be added to any feature.
* Define the feature
    ```C#
        var featureDefinitionProvider = serviceProvider.GetRequired<InMemoryFeatureProvider>();
        featureDefinitionProvider.SetFeature("MyBoolFeature", isOn: true);
        featureDatabase.SetFeatureFilter("FeatureA", "ParallelChange", "{ \"setting\": \"Expanded\" }");
    ```


* When writing data
    ```C#
    if (!await featureService.IsEnabled("feature", ParallelChange.Contracted)) {
        Perform_Old_DataWrite();
    }
    if (await featureService.IsEnabled("feature", ParallelChange.Expanded)) {
        Perform_New_DataWrite();
    }
    ```

* When checking from UI if the feature can be used
    ```C#
    if (await featureService.IsEnabled("feature", ParallelChange.Migrated)) {
        Perform_New_UI();
    } else {
        Perform_Old_UI();
    }
    ```

* Alternatively the UI can do
    ```C#
    if (await featureService.IsEnabled("feature")) {
        Perform_New_UI();
    } else {
        Perform_Old_UI();
    }
    ```

## Session scoped feature evaluation

Some features may require that they are stable during the user's session.

Suppose an admin sets a feature to on while the user is logged in and you don't want the user to get that feature directly. Only when the user logs in again the feature should be on.

There are different solutions to this:
1. Take a snapshot of the feature state when logging in and keep the state within the session.
    In a desktop application the state can be kept in a singleton. In a web application the session state is typically kept either in a state cookie, or the state cookie contains an identifier to the state in some (distributed) session store.

    ```C#
    // Dependency registration
    serviceCollection.AddScoped<SessionEvaluationCache>();
    serviceCollection.AddScoped<IFeatureEvaluationCache>(sp => sp.GetRequired<SessionEvaluationCache>());
    serviceCollection.AddFeatureSwitches(); // Registers FeatureEvaluationCache if no IFeatureEvaluationCache was registered.

    // When logging in fill the session
    sessionEvaluationCache.ResetState();
    var featureState = new Dictionary<string, string>();
    foreach (var feature in await featureService.GetFeatures())
    {
        if (IsSessionFeature(feature))
        {
            _ = await featureService.GetValue<string>();
        }
    }

    TempData["FeatureState"] = sessionEvaluationCache.GetState();

    // Upon a web request (for example via middelware)
    using (var scope = sp.CreateScope())
    {
        var sessionEvaluationCache = sp.ServiceProvider.GetRequired<SessionEvaluationCache>();
        sessionEvaluationCache.LoadState(TempData["FeatureState"]);
        DoExecuteRequest();
        TempData["FeatureState"] = sessionEvaluationCache.GetState();
    }
    ```

    This approach has some effects to be aware of:
    * Login gets delayed depending on the complexity and the number of features.
    * Can't use contextual feature filters like `ParallelChange`, or perhaps only once when taking the snapshot.

2. Use a session feature filter and set a point-in-time in it's configuration from which the feature is available. Typically that would be when the feature is set to on.
    ```C#
    // Use the built-in session feature filter
    featureDefinitionProvider.SetFeature("SessionFeature", isOn: true);
    featureDefinitionProvider.SetFeatureFilter("SessionFeature", "Session", config: "{ \"From\": \"2020-11-04\" }");

    var sessionContext = serviceProvider.GetRequired<SessionContext>);
    sessionContext.LoginTime = DateTimeOffSet.Parse("2020-11-03");
    Assert.IsFalse(await featureService.IsEnabled("SessionFeature"));
    sessionContext.LoginTime = DateTimeOffSet.Parse("2020-11-04");
    Assert.IsTrue(await featureService.IsEnabled("SessionFeature"));

    // Or roll-your-own
    public class MyAppContext
    { 
        public DateTimeOffSet LoginTime { get; set; }
    }

    public class MySessionFeatureFilterSettings
    {
        public DateTimeOffSet From { get; set; }
    }

    public class MySessionFeatureFilter : IFeatureFilter
    {
        private readonly MyAppContext appContext;

        public string Name => "MySession";

        public MyUserFeatureFilter(MyAppContext appContext)
        {
            this.appContext = appContext;
        }

        public async Task<bool> IsEnabled(FeatureFilterEvaluationContext context)
        {
            var settings = context.GetSettings<MySessionFeatureFilterSettings>();
            return this.appContext.LoginTime >= settings.From;
        }
    }

    featureDefinitionProvider.SetFeature("SessionFeature", isOn: true);
    featureDefinitionProvider.SetFeatureFilter("SessionFeature", "MySession", config: "{ \"From\": \"2020-11-04\" }");

    appContext.LoginTime = DateTimeOffSet.Parse("2020-11-03");
    Assert.IsFalse(await featureService.IsEnabled("SessionFeature"));
    appContext.LoginTime = DateTimeOffSet.Parse("2020-11-04");
    Assert.IsTrue(await featureService.IsEnabled("SessionFeature"));
    ```