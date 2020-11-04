using System.Threading.Tasks;

namespace FeatureSwitches.EvaluationCaching
{
    public interface IFeatureEvaluationCache
    {
        Task<EvaluationCacheResult<T>?> GetItem<T>(string feature, string sessionContextValue);

        Task SetItem<T>(string feature, string sessionContextValue, T value);
    }
}
