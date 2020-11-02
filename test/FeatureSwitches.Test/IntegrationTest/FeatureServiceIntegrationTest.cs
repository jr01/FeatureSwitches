using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FeatureSwitches.Definitions;
using FeatureSwitches.EvaluationCaching;
using FeatureSwitches.Filters;

namespace FeatureSwitches.Test.IntegrationTest
{
    [TestClass]
    public class FeatureServiceIntegrationTest
    {
        private readonly ServiceProvider sp;

        public FeatureServiceIntegrationTest()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped<CurrentCustomer>();

            serviceCollection.AddFeatureSwitches();

            serviceCollection.AddScoped<IFeatureFilterMetadata, CustomerFeatureFilter>();

            serviceCollection.AddScoped<IEvaluationContextAccessor, FeatureContextAccessor>();

            this.sp = new DefaultServiceProviderFactory()
                .CreateBuilder(serviceCollection)
                .BuildServiceProvider();
        }

        [TestMethod]
        public void On_off_override()
        {
            var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
            featureDatabase.SetFeature("FeatureA");
            featureDatabase.SetFeatureGroup("FeatureA");
            featureDatabase.SetFeatureFilter("FeatureA", "OnOff", new ScalarValueSetting<bool>(true));

            featureDatabase.SetFeature("FeatureB");
            featureDatabase.SetFeatureGroup("FeatureB");
            featureDatabase.SetFeatureFilter("FeatureB", "OnOff", new ScalarValueSetting<bool>(false));

            var featureService = this.sp.GetRequiredService<FeatureService>();
            Assert.IsTrue(featureService.IsEnabled("FeatureA"));
            Assert.IsFalse(featureService.IsEnabled("FeatureB"));
        }

        [TestMethod]
        public void Main_switch()
        {
            var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
            featureDatabase.SetFeature("FeatureA", isOn: true);
            featureDatabase.SetFeature("FeatureB");

            var featureService = this.sp.GetRequiredService<FeatureService>();
            Assert.IsTrue(featureService.IsEnabled("FeatureA"));
            Assert.IsFalse(featureService.IsEnabled("FeatureB"));
        }

        [TestMethod]
        public void Customer_filter_with_thread_identity()
        {
            var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
            featureDatabase.SetFeature("FeatureA", isOn: true, offValue: false);
            featureDatabase.SetFeatureGroup("FeatureA", true);
            featureDatabase.SetFeatureFilter("FeatureA", "Customer", new CustomerFeatureFilterSettings { Customers = new HashSet<string> { "A", "C" } });

            var featureService = this.sp.GetRequiredService<FeatureService>();
            SetCurrentCustomer("A");
            Assert.IsTrue(featureService.IsEnabled("FeatureA"));
            SetCurrentCustomer("B");
            Assert.IsFalse(featureService.IsEnabled("FeatureA"));
            SetCurrentCustomer("C");
            Assert.IsTrue(featureService.IsEnabled("FeatureA"));
        }

        [TestMethod]
        public void Evaluation_Speed()
        {
            var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
            featureDatabase.SetFeature("FeatureA", offValue: false);
            featureDatabase.SetFeatureGroup("FeatureA", null, true);
            featureDatabase.SetFeatureFilter("FeatureA", "Customer", new CustomerFeatureFilterSettings { Customers = new HashSet<string> { "A", "C" } });

            var featureService = this.sp.GetRequiredService<FeatureService>();
            SetCurrentCustomer("A");
            for (var i = 0; i < 10000; i++)
            {
                featureService.IsEnabled("FeatureA");
            }
        }

        [TestMethod]
        public void Speed_Many_features()
        {
            var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
            const int ActiveFeatureCount = 1000;
            for (int i = 0; i < ActiveFeatureCount; i++)
            {
                featureDatabase.SetFeature($"Feature{i}", isOn: true);
            }

            var featureService = this.sp.GetRequiredService<FeatureService>();
            foreach (var feature in featureService.GetFeatures())
            {
                featureService.IsEnabled(feature);
            }
        }

        [TestMethod]
        public void Invalid_request()
        {
            var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
            featureDatabase.SetFeature("Egg", offValue: true);
            var featureService = this.sp.GetRequiredService<FeatureService>();
            Assert.IsNull(featureService.GetValue<TestVariation>("Egg"));
            Assert.IsTrue(featureService.IsEnabled("Egg"));
        }

        [TestMethod]
        public void Feature_not_defined()
        {
            // ToDo: Or should we throw when we don't know the feature?
            var featureService = this.sp.GetRequiredService<FeatureService>();
            Assert.IsFalse(featureService.IsEnabled("Chicken"));
            Assert.IsFalse(featureService.GetValue<bool>("Chicken"));
            Assert.IsNull(featureService.GetValue<TestVariation>("Chicken"));
        }

        [TestMethod]
        public void Groups()
        {
            var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
            featureDatabase.SetFeature("FeatureA", isOn: true, offValue: false);
            featureDatabase.SetFeatureGroup("FeatureA", null, true);
            featureDatabase.SetFeatureGroup("FeatureA", "GroupA", true);
            featureDatabase.SetFeatureGroup("FeatureA", "GroupB", true);

            featureDatabase.SetFeatureFilter("FeatureA", "Customer", new CustomerFeatureFilterSettings { Customers = new HashSet<string> { "A", "C" } }, "GroupA");
            featureDatabase.SetFeatureFilter("FeatureA", "OnOff", "{ \"Setting\": false }", "GroupA");
            featureDatabase.SetFeatureFilter("FeatureA", "Customer", new CustomerFeatureFilterSettings { Customers = new HashSet<string> { "B" } }, "GroupB");

            var featureService = this.sp.GetRequiredService<FeatureService>();
            SetCurrentCustomer("A");
            Assert.IsFalse(featureService.IsEnabled("FeatureA"));
            SetCurrentCustomer("B");
            Assert.IsTrue(featureService.IsEnabled("FeatureA"));
            SetCurrentCustomer("C");
            Assert.IsFalse(featureService.IsEnabled("FeatureA"));
        }

        [TestMethod]
        public void Customer_filter_with_scoped_customer()
        {
            var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
            featureDatabase.SetFeature("FeatureA", isOn: true, offValue: false);
            featureDatabase.SetFeatureGroup("FeatureA", null, true);
            featureDatabase.SetFeatureGroup("FeatureA", "GroupA", true);
            featureDatabase.SetFeatureGroup("FeatureA", "GroupB", true);

            featureDatabase.SetFeatureFilter("FeatureA", "Customer", "{ \"Customers\": [\"A\", \"C\"] }", "GroupA");
            featureDatabase.SetFeatureFilter("FeatureA", "OnOff", "{ \"Setting\": false }", "GroupA");
            featureDatabase.SetFeatureFilter("FeatureA", "Customer", "{ \"Customers\": [\"B\"] }", "GroupB");

            using (var scope = this.sp.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<CurrentCustomer>().Name = "A";
                var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
                Assert.IsFalse(featureService.IsEnabled("FeatureA"));
            }

            using (var scope = this.sp.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<CurrentCustomer>().Name = "B";
                var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
                Assert.IsTrue(featureService.IsEnabled("FeatureA"));
            }

            using (var scope = this.sp.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<CurrentCustomer>().Name = "C";
                var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
                Assert.IsFalse(featureService.IsEnabled("FeatureA"));
            }
        }

        [TestMethod]
        public void Group_with_main_switch_in_main_group()
        {
            var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
            featureDatabase.SetFeature("FeatureA", isOn: false);

            featureDatabase.SetFeatureFilter("FeatureA", "Customer", "{ \"Customers\": [\"A\", \"C\"] }", "GroupA");
            featureDatabase.SetFeatureFilter("FeatureA", "Customer", "{ \"Customers\": [\"B\"] }", "GroupB");

            var featureService = this.sp.GetRequiredService<FeatureService>();
            SetCurrentCustomer("A");
            Assert.IsFalse(featureService.IsEnabled("FeatureA"));
            SetCurrentCustomer("B");
            Assert.IsFalse(featureService.IsEnabled("FeatureA"));
            SetCurrentCustomer("C");
            Assert.IsFalse(featureService.IsEnabled("FeatureA"));
        }

        [TestMethod]
        public void Variations_enums()
        {
            var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();

            featureDatabase.SetFeature("FeatureA", isOn: true, offValue: MultiSwitch.Off, onValue: MultiSwitch.On);
            featureDatabase.SetFeatureGroup("FeatureA", "GroupA", MultiSwitch.Halfway);
            featureDatabase.SetFeatureGroup("FeatureA", "GroupB", MultiSwitch.On);

            featureDatabase.SetFeatureFilter("FeatureA", "Customer", "{ \"Customers\": [\"A\"] }", "GroupA");
            featureDatabase.SetFeatureFilter("FeatureA", "Customer", "{ \"Customers\": [\"B\"] }", "GroupB");

            var featureService = this.sp.GetRequiredService<FeatureService>();
            SetCurrentCustomer("A");
            Assert.AreEqual(MultiSwitch.Halfway, featureService.GetValue<MultiSwitch>("FeatureA"));
            SetCurrentCustomer("B");
            Assert.AreEqual(MultiSwitch.On, featureService.GetValue<MultiSwitch>("FeatureA"));
            SetCurrentCustomer("C");
            Assert.AreEqual(MultiSwitch.Off, featureService.GetValue<MultiSwitch>("FeatureA"));
        }

        [TestMethod]
        public void Variations_objects()
        {
            var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();

            var offVariation = new TestVariation { Color = Color.Black };
            var defaultOnVariation = new TestVariation { Color = Color.White };
            var halfWayVariation = new TestVariation { Color = Color.Gray };
            featureDatabase.SetFeature("FeatureA", isOn: true, offValue: offVariation, onValue: defaultOnVariation);

            featureDatabase.SetFeatureGroup("FeatureA", "GroupA", halfWayVariation);
            featureDatabase.SetFeatureFilter("FeatureA", "Customer", "{ \"Customers\": [\"A\"] }", "GroupA");

            featureDatabase.SetFeatureGroup("FeatureA", "GroupB", defaultOnVariation);
            featureDatabase.SetFeatureFilter("FeatureA", "Customer", "{ \"Customers\": [\"B\"] }", "GroupB");

            var featureService = this.sp.GetRequiredService<FeatureService>();
            SetCurrentCustomer("A");
            var variation = featureService.GetValue<TestVariation>("FeatureA");
            Assert.AreEqual(Color.Gray, variation.Color);
            SetCurrentCustomer("B");
            variation = featureService.GetValue<TestVariation>("FeatureA");
            Assert.AreEqual(Color.White, variation.Color);
            SetCurrentCustomer("C");
            variation = featureService.GetValue<TestVariation>("FeatureA");
            Assert.AreEqual(Color.Black, variation.Color);
        }

        [TestMethod]
        public void Update_feature_filter()
        {
            var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
            featureDatabase.SetFeature("FeatureA", isOn: true);
            featureDatabase.SetFeatureFilter("FeatureA", "Customer", "{ \"Customers\": [] }");

            using (var scope = this.sp.CreateScope())
            {
                var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
                scope.ServiceProvider.GetRequiredService<CurrentCustomer>().Name = "A";
                Assert.IsFalse(featureService.IsEnabled("FeatureA"));
            }

            featureDatabase.SetFeatureFilter("FeatureA", "Customer", "{ \"Customers\": [\"A\"] }");

            using (var scope = this.sp.CreateScope())
            {
                var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();
                scope.ServiceProvider.GetRequiredService<CurrentCustomer>().Name = "A";
                Assert.IsTrue(featureService.IsEnabled("FeatureA"));
            }
        }

        [TestMethod]
        public void Parallel_change()
        {
            var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
            featureDatabase.SetFeature("FeatureA", isOn: false);
            featureDatabase.SetFeatureGroup("FeatureA");

            featureDatabase.SetFeatureFilter("FeatureA", "ParallelChange", "{ \"Setting\": \"Expanded\" }");

            using var scope = this.sp.CreateScope();
            var featureService = scope.ServiceProvider.GetRequiredService<FeatureService>();

            Assert.IsFalse(featureService.IsEnabled("FeatureA"));
            Assert.IsFalse(featureService.IsEnabled("FeatureA", ParallelChange.Expanded));
            Assert.IsFalse(featureService.IsEnabled("FeatureA", ParallelChange.Migrated));
            Assert.IsFalse(featureService.IsEnabled("FeatureA", ParallelChange.Contracted));

            featureDatabase.SetFeature("FeatureA", isOn: true);

            Assert.IsFalse(featureService.IsEnabled("FeatureA"));
            Assert.IsTrue(featureService.IsEnabled("FeatureA", ParallelChange.Expanded));
            Assert.IsFalse(featureService.IsEnabled("FeatureA", ParallelChange.Migrated));
            Assert.IsFalse(featureService.IsEnabled("FeatureA", ParallelChange.Contracted));

            featureDatabase.SetFeatureFilter("FeatureA", "ParallelChange", "{ \"Setting\": \"Migrated\" }");

            Assert.IsTrue(featureService.IsEnabled("FeatureA"));
            Assert.IsTrue(featureService.IsEnabled("FeatureA", ParallelChange.Expanded));
            Assert.IsTrue(featureService.IsEnabled("FeatureA", ParallelChange.Migrated));
            Assert.IsFalse(featureService.IsEnabled("FeatureA", ParallelChange.Contracted));

            featureDatabase.SetFeatureFilter("FeatureA", "ParallelChange", "{ \"Setting\": \"Contracted\" }");

            Assert.IsTrue(featureService.IsEnabled("FeatureA"));
            Assert.IsTrue(featureService.IsEnabled("FeatureA", ParallelChange.Expanded));
            Assert.IsTrue(featureService.IsEnabled("FeatureA", ParallelChange.Migrated));
            Assert.IsTrue(featureService.IsEnabled("FeatureA", ParallelChange.Contracted));

            // usage code is like
            // data_write:
            //      if (!contracted) OldBehavior()
            //      if (!expanded) NewBehavior()
            // UI: if (!migrated) OldUI() else NewUI()
            // FrontEnd if(enabled (==migrated!)) OldUI() else NewUI()
        }

        [TestMethod]
        public void All_features()
        {
            var featureDatabase = this.sp.GetRequiredService<InMemoryFeatureDefinitionProvider>();
            featureDatabase.SetFeature("FeatureA");
            featureDatabase.SetFeature("FeatureB");

            var featureService = this.sp.GetRequiredService<FeatureService>();
            var features = featureService.GetFeatures();
            Assert.AreEqual(2, features.Length);
            Assert.AreEqual(features[0], "FeatureA");
            Assert.AreEqual(features[1], "FeatureB");
        }

        private static void SetCurrentCustomer(string name)
        {
            Thread.CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, name) }));
        }

        private class TestVariation
        {
            [JsonIgnore]
            public Color Color { get; set; }

            [JsonPropertyName("Color")]
            public string BackColorAsArgb
            {
                get
                {
                    return this.Color.IsNamedColor ? this.Color.Name : this.Color.ToArgb().ToString(CultureInfo.InvariantCulture);
                }

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
}
