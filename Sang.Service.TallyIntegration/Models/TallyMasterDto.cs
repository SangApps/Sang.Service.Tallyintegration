namespace Sang.Service.TallyIntegration.Models
{
    public class TallyMasterDto
    {
        public string Name { get; set; } = string.Empty;
        public string Parent { get; set; } = string.Empty;
        public string MasterId { get; set; } = string.Empty;
        public string BaseUnits { get; set; } = string.Empty;
        public string OpeningBalance { get; set; } = string.Empty;
        public string ClosingBalance { get; set; } = string.Empty;
    }
}
