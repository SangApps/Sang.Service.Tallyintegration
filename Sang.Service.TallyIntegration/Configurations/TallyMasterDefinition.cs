namespace Sang.Service.TallyIntegration.Configurations
{
    public class TallyMasterDefinition
    {
        public string MasterType { get; set; } = string.Empty;

        public string XmlElementName { get; set; } = string.Empty;

        public string JsonCodeField { get; set; } = "code";

        public string XmlCodeElement { get; set; } = "CODE";

        public string JsonAction { get; set; } = "create";

        public string XmlAction { get; set; } = "Create";
    }
}