namespace Sang.Service.TallyIntegration.Models
{
    public class TallyVoucherRequest
    {
        public int TransId { get; set; }

        public int DocType { get; set; }

        public bool IsForIssue { get; set; }
        public string VoucherXml { get; set; } = string.Empty;
    }
    public class TallyVoucherResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? ResponseXml { get; set; }
    }
 
        public class TallyLedgerDto
        {
            public string Name { get; set; } = string.Empty;

            public string Parent { get; set; } = string.Empty;

            public string MasterId { get; set; } = string.Empty;

            public string ClosingBalance { get; set; } = string.Empty;
        }
}

