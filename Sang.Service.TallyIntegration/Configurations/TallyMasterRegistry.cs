namespace Sang.Service.TallyIntegration.Configurations
{
    public static class TallyMasterRegistry
    {
        private static readonly Dictionary<string, TallyMasterDefinition>
            MasterDefinitions = new(StringComparer.OrdinalIgnoreCase)
            {
                ["StockItem"] = new()
                {
                    MasterType = "StockItem",
                    XmlElementName = "STOCKITEM",
                    JsonCodeField = "code",
                    XmlCodeElement = "ALIAS"
                },

                ["Ledger"] = new()
                {
                    MasterType = "Ledger",
                    XmlElementName = "LEDGER",
                    JsonCodeField = "code",
                    XmlCodeElement = "ALIAS"
                },

                ["Godown"] = new()
                {
                    MasterType = "Godown",
                    XmlElementName = "GODOWN",
                    JsonCodeField = "code",
                    XmlCodeElement = "ALIAS"
                },

                ["Unit"] = new()
                {
                    MasterType = "Unit",
                    XmlElementName = "UNIT",
                    JsonCodeField = "code",
                    XmlCodeElement = "ALIAS"
                },

                ["Group"] = new()
                {
                    MasterType = "Group",
                    XmlElementName = "GROUP",
                    JsonCodeField = "code",
                    XmlCodeElement = "ALIAS"
                }
            };

        public static TallyMasterDefinition Get(string masterType)
        {
            if (string.IsNullOrWhiteSpace(masterType))
            {
                throw new ArgumentException(
                    "Master type is required.",
                    nameof(masterType));
            }

            if (!MasterDefinitions.TryGetValue(
                masterType.Trim(), out var definition))
            {
                throw new ArgumentException(
                    $"Unsupported Tally master type: {masterType}");
            }

            return definition;
        }
    }
}