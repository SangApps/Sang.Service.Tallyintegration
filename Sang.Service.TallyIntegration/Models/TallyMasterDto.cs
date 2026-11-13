namespace Sang.Service.TallyIntegration.Models
{
    public class TallyMasterDto
    {
        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string Parent { get; set; } = string.Empty;

        public Dictionary<string, object?> ExtraFields { get; set; } = new();
    }
}