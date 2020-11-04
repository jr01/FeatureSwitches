using System.Threading.Tasks;

namespace FeatureSwitches.Filters
{
    public interface IFeatureFilter : IFeatureFilterMetadata
    {
        Task<bool> IsEnabled(FeatureFilterEvaluationContext context);
    }
}