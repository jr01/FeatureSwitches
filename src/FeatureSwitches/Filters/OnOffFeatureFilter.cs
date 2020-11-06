using System.Threading;
using System.Threading.Tasks;

namespace FeatureSwitches.Filters
{
    public class OnOffFeatureFilter : IFeatureFilter
    {
        public string Name => "OnOff";

        public Task<bool> IsEnabled(FeatureFilterEvaluationContext context, CancellationToken cancellationToken = default)
        {
            var settings = context.GetSettings<ScalarValueSetting<bool>>();
            return Task.FromResult(settings.Setting);
        }
    }
}
