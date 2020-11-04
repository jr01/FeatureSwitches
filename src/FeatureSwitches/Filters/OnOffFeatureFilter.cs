using System.Threading.Tasks;

namespace FeatureSwitches.Filters
{
    public class OnOffFeatureFilter : IFeatureFilter
    {
        public string Name => "OnOff";

        public Task<bool> IsEnabled(FeatureFilterEvaluationContext context)
        {
            var settings = context.GetSettings<ScalarValueSetting<bool>>();
            return Task.FromResult(settings.Setting);
        }
    }
}
