namespace Sang.Service.TallyIntegration.Models
{
    public class TallyMasterRequest
    {
        public string CompanyName { get; set; } = string.Empty;

        public string MasterType { get; set; } = string.Empty;

        public TallyMasterInputDto Master { get; set; } = new();
    }

    public class TallyMasterInputDto
    {
        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string Parent { get; set; } = string.Empty;

        public Dictionary<string, object?> ExtraFields { get; set; } = new();
    }
}