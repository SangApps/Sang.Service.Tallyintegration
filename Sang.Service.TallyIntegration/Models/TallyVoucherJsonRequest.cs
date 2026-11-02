namespace Sang.Service.TallyIntegration.Models
{
    public class TallyVoucherJsonRequest
    {
        public int TransId { get; set; }
        public int DocType { get; set; }
        public bool IsForIssue { get; set; }
        public string VoucherJson { get; set; } = string.Empty;
    }
}