namespace FeatureSwitches.Filters
{
    /// <summary>
    /// A scalar value setting.
    /// </summary>
    /// <typeparam name="T">The setting typ.</typeparam>
    public class ScalarValueSetting<T>
    {
        public ScalarValueSetting(T setting)
        {
            this.Setting = setting;
        }

        public ScalarValueSetting()
        {
        }

        /// <summary>
        /// Gets or sets the setting.
        /// </summary>
        public T Setting { get; set; } = default!;
    }
}
