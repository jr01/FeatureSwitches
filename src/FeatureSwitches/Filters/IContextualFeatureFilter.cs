using System.Threading.Tasks;

namespace FeatureSwitches.Filters
{
    public interface IContextualFeatureFilter : IFeatureFilterMetadata
    {
        Task<bool> IsEnabled(FeatureFilterEvaluationContext context, object? evaluationContext);
    }
}
