namespace Tecan.Sila2.Generator.Generators
{
    /// <summary>
    /// Helper class to avoid changing contracts for new configurations
    /// </summary>
    public static class ClientGeneratorConfig
    {
        /// <summary>
        /// If set, the generated code will query unobservable properties every time
        /// </summary>
        public static bool LazyUnobservableProperties { get; set; } = true;
    }
}
