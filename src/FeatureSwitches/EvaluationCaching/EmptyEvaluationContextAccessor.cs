namespace FeatureSwitches.EvaluationCaching
{
    public class EmptyEvaluationContextAccessor : IEvaluationContextAccessor
    {
        public object? GetContext()
        {
            return null;
        }
    }
}
