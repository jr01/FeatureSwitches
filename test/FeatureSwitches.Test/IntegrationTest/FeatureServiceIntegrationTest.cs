using System.Drawing;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using FeatureSwitches.Caching;
using FeatureSwitches.Definitions;
using FeatureSwitches.Filters;
using Microsoft.Extensions.DependencyInjection;

[assembly: CLSCompliant(true)]

namespace FeatureSwitches.Test.IntegrationTest;

[TestClass]
public sealed class FeatureServiceIntegrationTest
{
    private readonly ServiceProvider sp;

    public FeatureServiceIntegrationTest()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddScoped<CurrentCustomer>();

        serviceCollection.AddFeatureSwitches(addScopedCache: true);

        serviceCollection.AddScoped<IFeatureFilterMetadata, CustomerFeatureFilter>();

        serviceCollection.AddScoped<IFeatureCacheContextAccessor, FeatureCacheContextAccessor>();

        this.sp = new DefaultServiceProviderFactory()
            .CreateBuilder(serviceCollection)
            .BuildServiceProvider();
    }

    [TestMethod]
    public async Task Customer_filter_with_thread_identity()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        featureDatabase.SetFeature("FeatureA", isOn: true, offValue: false, onValue: true);
        featureDatabase.SetFeatureFilter("FeatureA", "Customer", new CustomerFeatureFilterSettings { Customers = ["A", "C"] });

        var featureService = this.sp.GetRequiredService<FeatureService>();
        SetCurrentCustomer("A");
        Assert.IsTrue(await featureService.IsOn("FeatureA"));
        SetCurrentCustomer("B");
        Assert.IsFalse(await featureService.IsOn("FeatureA"));
        SetCurrentCustomer("C");
        Assert.IsTrue(await featureService.IsOn("FeatureA"));
    }

    [TestMethod]
    public async Task Speed_single_scope()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        featureDatabase.SetFeature("FeatureA", offValue: false, isOn: true);
        featureDatabase.SetFeatureFilter("FeatureA", "Customer", new CustomerFeatureFilterSettings { Customers = ["A", "C"] });

        var featureService = this.sp.GetRequiredService<FeatureService>();
        SetCurrentCustomer("A");
        for (var i = 0; i < 10000; i++)
        {
            await featureService.IsOn("FeatureA");
        }
    }

    [TestMethod]
    public async Task Speed_multi_scope()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        featureDatabase.SetFeature("FeatureA", offValue: false, isOn: true);
        featureDatabase.SetFeatureFilter("FeatureA", "Customer", new CustomerFeatureFilterSettings { Customers = ["A", "C"] });

        SetCurrentCustomer("A");
        for (var i = 0; i < 10000; i++)
        {
            using var scope = this.sp.CreateScope();
            var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
            await featureService.IsOn("FeatureA");
        }
    }

    [TestMethod]
    public async Task Speed_many_features_single_scope()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        const int ActiveFeatureCount = 1000;
        for (int i = 0; i < ActiveFeatureCount; i++)
        {
            featureDatabase.SetFeature($"Feature{i}");
        }

        var featureService = this.sp.GetRequiredService<FeatureService>();
        foreach (var feature in await featureService.GetFeatures())
        {
            await featureService.IsOn(feature);
        }
    }

    [TestMethod]
    public async Task Invalid_request()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        featureDatabase.SetFeature("Egg");
        var featureService = this.sp.GetRequiredService<FeatureService>();
        await Assert.ThrowsAsync<JsonException>(() => featureService.GetValue<TestVariation>("Egg"));
        Assert.IsTrue(await featureService.IsOn("Egg"));
    }

    [TestMethod]
    public async Task Feature_not_defined()
    {
        var featureService = this.sp.GetRequiredService<FeatureService>();
        Assert.IsFalse(await featureService.IsOn("Chicken"));
        Assert.IsFalse(await featureService.GetValue<bool>("Chicken"));
        Assert.IsNull(await featureService.GetValue<TestVariation>("Egg"));
    }

    [TestMethod]
    public async Task IsOn_with_non_boolean_feature()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        featureDatabase.SetFeature("Switch", isOn: true, offValue: "Off", onValue: "On");

        var featureService = this.sp.GetRequiredService<FeatureService>();
        await Assert.ThrowsAsync<JsonException>(() => featureService.IsOn("Switch"));
    }

    [TestMethod]
    public async Task Feature_is_on_when_no_filters_defined()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        featureDatabase.SetFeature("Egg", isOn: true);

        var featureService = this.sp.GetRequiredService<FeatureService>();

        Assert.IsTrue(await featureService.IsOn("Egg"));
    }

    [TestMethod]
    public async Task Groups()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        featureDatabase.SetFeature("FeatureA", isOn: true, offValue: false);
        featureDatabase.SetFeatureGroup("FeatureA", "GroupA", isOn: false, onValue: true);
        featureDatabase.SetFeatureGroup("FeatureA", "GroupB", isOn: true, onValue: true);

        featureDatabase.SetFeatureFilter("FeatureA", "Customer", new CustomerFeatureFilterSettings { Customers = ["A", "C"] }, "GroupA");
        featureDatabase.SetFeatureFilter("FeatureA", "Customer", new CustomerFeatureFilterSettings { Customers = ["B"] }, "GroupB");

        var featureService = this.sp.GetRequiredService<FeatureService>();
        SetCurrentCustomer("A");
        Assert.IsFalse(await featureService.IsOn("FeatureA"));
        SetCurrentCustomer("B");
        Assert.IsTrue(await featureService.IsOn("FeatureA"));
        SetCurrentCustomer("C");
        Assert.IsFalse(await featureService.IsOn("FeatureA"));
    }

    [TestMethod]
    public async Task First_matching_group_wins()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        featureDatabase.SetFeature("FeatureA", isOn: true, onValue: "on", offValue: "off");
        featureDatabase.SetFeatureGroup("FeatureA", "GroupA", isOn: true, onValue: "a");
        featureDatabase.SetFeatureGroup("FeatureA", "GroupB", isOn: true, onValue: "b");

        var featureService = this.sp.GetRequiredService<FeatureService>();
        Assert.AreEqual("a", await featureService.GetValue<string>("FeatureA"));
    }

    [TestMethod]
    public async Task LoadFromJson()
    {
        // can't expect the user to write base64
        // anything below the node should be auto byte arrayed.
        var json = /*lang=json,strict*/ @"
[
  {
    ""Name"": ""FeatureA"",
    ""OffValue"": false,
    ""IsOn"": true,
    ""OnValue"": false,
    ""Filters"": [
      {
        ""Name"": ""Customer"",
        ""Settings"": {
            ""Customers"": [
                ""A"",
                ""C""
            ]
        },
        ""Group"": ""GroupA""
      },
      {
        ""Name"": ""Customer"",
        ""Settings"": {
            ""Customers"": [
                ""B""
            ]
        },
        ""Group"": ""GroupB""
      }
    ],
    ""FilterGroups"": [
      {
        ""Name"": ""GroupA"",
        ""IsOn"": false,
        ""OnValue"": true
      },
      {
        ""Name"": ""GroupB"",
        ""IsOn"": true,
        ""OnValue"": true
      }
    ]
  }
]";
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        featureDatabase.LoadFromJson(json);
        var featureService = this.sp.GetRequiredService<FeatureService>();
        SetCurrentCustomer("A");
        Assert.IsFalse(await featureService.IsOn("FeatureA"));
        SetCurrentCustomer("B");
        Assert.IsTrue(await featureService.IsOn("FeatureA"));
        SetCurrentCustomer("C");
        Assert.IsFalse(await featureService.IsOn("FeatureA"));
    }

    [TestMethod]
    public async Task Customer_filter_with_scoped_customer()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        featureDatabase.SetFeature("FeatureA", isOn: true, offValue: false);
        featureDatabase.SetFeatureGroup("FeatureA", "GroupA", isOn: false, onValue: true);
        featureDatabase.SetFeatureGroup("FeatureA", "GroupB", isOn: true, onValue: true);

        featureDatabase.SetFeatureFilter("FeatureA", "Customer", /*lang=json,strict*/ "{ \"Customers\": [\"A\", \"C\"] }", "GroupA");
        featureDatabase.SetFeatureFilter("FeatureA", "Customer", /*lang=json,strict*/ "{ \"Customers\": [\"B\"] }", "GroupB");

        using (var scope = this.sp.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<CurrentCustomer>().Name = "A";
            var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
            Assert.IsFalse(await featureService.IsOn("FeatureA"));
        }

        using (var scope = this.sp.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<CurrentCustomer>().Name = "B";
            var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
            Assert.IsTrue(await featureService.IsOn("FeatureA"));
        }

        using (var scope = this.sp.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<CurrentCustomer>().Name = "C";
            var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
            Assert.IsFalse(await featureService.IsOn("FeatureA"));
        }
    }

    [TestMethod]
    public async Task Group_with_main_switch_in_main_group()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        featureDatabase.SetFeature("FeatureA");
        featureDatabase.SetFeatureGroup("FeatureA", "GroupA", isOn: false);
        featureDatabase.SetFeatureGroup("FeatureA", "GroupB", isOn: false);

        featureDatabase.SetFeatureFilter("FeatureA", "Customer", /*lang=json,strict*/ "{ \"Customers\": [\"A\", \"C\"] }", "GroupA");
        featureDatabase.SetFeatureFilter("FeatureA", "Customer", /*lang=json,strict*/ "{ \"Customers\": [\"B\"] }", "GroupB");

        var featureService = this.sp.GetRequiredService<FeatureService>();
        SetCurrentCustomer("A");
        Assert.IsFalse(await featureService.IsOn("FeatureA"));
        SetCurrentCustomer("B");
        Assert.IsFalse(await featureService.IsOn("FeatureA"));
        SetCurrentCustomer("C");
        Assert.IsFalse(await featureService.IsOn("FeatureA"));
    }

    [TestMethod]
    public async Task Variations_enums()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();

        featureDatabase.SetFeature("FeatureA", offValue: MultiSwitch.Off, onValue: MultiSwitch.On);
        featureDatabase.SetFeatureGroup("FeatureA", "GroupA", onValue: MultiSwitch.Halfway);
        featureDatabase.SetFeatureGroup("FeatureA", "GroupB", onValue: MultiSwitch.On);

        featureDatabase.SetFeatureFilter("FeatureA", "Customer", /*lang=json,strict*/ "{ \"Customers\": [\"A\"] }", "GroupA");
        featureDatabase.SetFeatureFilter("FeatureA", "Customer", /*lang=json,strict*/ "{ \"Customers\": [\"B\"] }", "GroupB");

        var featureService = this.sp.GetRequiredService<FeatureService>();
        SetCurrentCustomer("A");
        Assert.AreEqual(MultiSwitch.Halfway, await featureService.GetValue<MultiSwitch>("FeatureA"));
        SetCurrentCustomer("B");
        Assert.AreEqual(MultiSwitch.On, await featureService.GetValue<MultiSwitch>("FeatureA"));
        SetCurrentCustomer("C");
        Assert.AreEqual(MultiSwitch.Off, await featureService.GetValue<MultiSwitch>("FeatureA"));
    }

    [TestMethod]
    public async Task Variations_strings()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();

        var offValue = JsonSerializer.Deserialize<object?>(JsonSerializer.SerializeToUtf8Bytes("Off"));
        var onValue = JsonSerializer.Deserialize<object?>(JsonSerializer.SerializeToUtf8Bytes("On"));

        featureDatabase.SetFeature("FeatureA", offValue: offValue, onValue: onValue, isOn: false);

        using (var scope = this.sp.CreateScope())
        {
            var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
            Assert.AreEqual("Off", await featureService.GetValue<string>("FeatureA"));
        }

        featureDatabase.ToggleFeature("FeatureA", isOn: true);

        using (var scope = this.sp.CreateScope())
        {
            var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
            Assert.AreEqual("On", await featureService.GetValue<string>("FeatureA"));
        }
    }

    [TestMethod]
    public async Task Variations_objects()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();

        var offVariation = new TestVariation { Color = Color.Black };
        var defaultOnVariation = new TestVariation { Color = Color.White };
        var halfWayVariation = new TestVariation { Color = Color.Gray };
        featureDatabase.SetFeature("FeatureA", offValue: offVariation, onValue: defaultOnVariation);

        featureDatabase.SetFeatureGroup("FeatureA", "GroupA", onValue: halfWayVariation);
        featureDatabase.SetFeatureFilter("FeatureA", "Customer", /*lang=json,strict*/ "{ \"Customers\": [\"A\"] }", "GroupA");

        featureDatabase.SetFeatureGroup("FeatureA", "GroupB", onValue: defaultOnVariation);
        featureDatabase.SetFeatureFilter("FeatureA", "Customer", /*lang=json,strict*/ "{ \"Customers\": [\"B\"] }", "GroupB");

        var featureService = this.sp.GetRequiredService<FeatureService>();
        SetCurrentCustomer("A");
        var variation = await featureService.GetValue<TestVariation>("FeatureA");
        Assert.IsNotNull(variation);
        Assert.AreEqual(Color.Gray, variation.Color);
        SetCurrentCustomer("B");
        variation = await featureService.GetValue<TestVariation>("FeatureA");
        Assert.IsNotNull(variation);
        Assert.AreEqual(Color.White, variation.Color);
        SetCurrentCustomer("C");
        variation = await featureService.GetValue<TestVariation>("FeatureA");
        Assert.IsNotNull(variation);
        Assert.AreEqual(Color.Black, variation.Color);
    }

    [TestMethod]
    public async Task Variations_structs()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();

        var offVariation = new StructVariation { Name = "Off" };
        var defaultOnVariation = new StructVariation { Name = "On" };
        var halfWayVariation = new StructVariation { Name = "Halfway" };
        featureDatabase.SetFeature("FeatureA", offValue: offVariation, onValue: defaultOnVariation);

        featureDatabase.SetFeatureGroup("FeatureA", "GroupA", onValue: halfWayVariation);
        featureDatabase.SetFeatureFilter("FeatureA", "Customer", /*lang=json,strict*/ "{ \"Customers\": [\"A\"] }", "GroupA");

        featureDatabase.SetFeatureGroup("FeatureA", "GroupB", onValue: defaultOnVariation);
        featureDatabase.SetFeatureFilter("FeatureA", "Customer", /*lang=json,strict*/ "{ \"Customers\": [\"B\"] }", "GroupB");

        var featureService = this.sp.GetRequiredService<FeatureService>();
        SetCurrentCustomer("A");
        var variation = await featureService.GetValue<StructVariation>("FeatureA");
        Assert.AreEqual(halfWayVariation, variation);
        SetCurrentCustomer("B");
        variation = await featureService.GetValue<StructVariation>("FeatureA");
        Assert.AreEqual(defaultOnVariation, variation);
        SetCurrentCustomer("C");
        variation = await featureService.GetValue<StructVariation>("FeatureA");
        Assert.AreEqual(offVariation, variation);

        variation = await featureService.GetValue<StructVariation, StructVariation>("FeatureA", halfWayVariation);
        Assert.AreEqual(offVariation, variation);
    }

    [TestMethod]
    public async Task Update_feature_filter()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        featureDatabase.SetFeature("FeatureA", isOn: true);
        featureDatabase.SetFeatureFilter("FeatureA", "Customer", /*lang=json,strict*/ "{ \"Customers\": [] }");

        using (var scope = this.sp.CreateScope())
        {
            var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
            scope.ServiceProvider.GetRequiredService<CurrentCustomer>().Name = "A";
            Assert.IsFalse(await featureService.IsOn("FeatureA"));
        }

        featureDatabase.SetFeatureFilter("FeatureA", "Customer", /*lang=json,strict*/ "{ \"Customers\": [\"A\"] }");

        using (var scope = this.sp.CreateScope())
        {
            var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
            scope.ServiceProvider.GetRequiredService<CurrentCustomer>().Name = "A";
            Assert.IsTrue(await featureService.IsOn("FeatureA"));
        }
    }

    [TestMethod]
    public async Task Parallel_change()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        featureDatabase.SetFeature("FeatureA", isOn: false);
        featureDatabase.SetFeatureFilter("FeatureA", "ParallelChange", "\"Expanded\"");

        using (var scope = this.sp.CreateScope())
        {
            var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();

            Assert.IsFalse(await featureService.IsOn("FeatureA"));
            Assert.IsFalse(await featureService.IsOn("FeatureA", ParallelChange.Expanded));
            Assert.IsFalse(await featureService.IsOn("FeatureA", ParallelChange.Migrated));
            Assert.IsFalse(await featureService.IsOn("FeatureA", ParallelChange.Contracted));
        }

        featureDatabase.ToggleFeature("FeatureA", isOn: true);

        using (var scope = this.sp.CreateScope())
        {
            var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();

            Assert.IsFalse(await featureService.IsOn("FeatureA"));
            Assert.IsTrue(await featureService.IsOn("FeatureA", ParallelChange.Expanded));
            Assert.IsFalse(await featureService.IsOn("FeatureA", ParallelChange.Migrated));
            Assert.IsFalse(await featureService.IsOn("FeatureA", ParallelChange.Contracted));
        }

        featureDatabase.SetFeatureFilter("FeatureA", "ParallelChange", "\"Migrated\"");

        using (var scope = this.sp.CreateScope())
        {
            var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
            Assert.IsTrue(await featureService.IsOn("FeatureA"));
            Assert.IsTrue(await featureService.IsOn("FeatureA", ParallelChange.Expanded));
            Assert.IsTrue(await featureService.IsOn("FeatureA", ParallelChange.Migrated));
            Assert.IsFalse(await featureService.IsOn("FeatureA", ParallelChange.Contracted));
        }

        featureDatabase.SetFeatureFilter("FeatureA", "ParallelChange", "\"Contracted\"");

        using (var scope = this.sp.CreateScope())
        {
            var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
            Assert.IsTrue(await featureService.IsOn("FeatureA"));
            Assert.IsTrue(await featureService.IsOn("FeatureA", ParallelChange.Expanded));
            Assert.IsTrue(await featureService.IsOn("FeatureA", ParallelChange.Migrated));
            Assert.IsTrue(await featureService.IsOn("FeatureA", ParallelChange.Contracted));
        }
    }

    [TestMethod]
    public async Task All_features()
    {
        var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
        featureDatabase.SetFeature("FeatureA");
        featureDatabase.SetFeature("FeatureB");

        var featureService = this.sp.GetRequiredService<FeatureService>();
        var features = await featureService.GetFeatures();
        Assert.HasCount(2, features);
        Assert.AreEqual("FeatureA", features[0]);
        Assert.AreEqual("FeatureB", features[1]);
    }

    private static void SetCurrentCustomer(string name)
    {
        Thread.CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.Name, name)]));
    }

    private struct StructVariation
    {
        public string Name { get; set; }
    }

    private sealed class TestVariation
    {
        [JsonIgnore]
        public Color Color { get; set; }

        [JsonPropertyName("Color")]
        public string BackColorAsArgb
        {
            get => this.Color.IsNamedColor ? this.Color.Name : this.Color.ToArgb().ToString(CultureInfo.InvariantCulture);

            set
            {
                if (int.TryParse(value, out var argb))
                {
                    this.Color = Color.FromArgb(argb);
                }
                else
                {
                    this.Color = Color.FromName(value);
                }
            }
        }
    }
}
