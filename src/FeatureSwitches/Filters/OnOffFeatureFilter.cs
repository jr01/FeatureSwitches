namespace FeatureSwitches.Filters
{
    public class OnOffFeatureFilter : IFeatureFilter
    {
        public string Name => "OnOff";

        public bool IsEnabled(FeatureFilterEvaluationContext context)
        {
            var settings = context.GetSettings<ScalarValueSetting<bool>>();
            return settings.Setting;
        }
    }
}
