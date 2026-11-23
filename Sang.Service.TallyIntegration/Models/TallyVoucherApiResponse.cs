namespace Sang.Service.TallyIntegration.Models
{
    /// <summary>
    /// Response returned by the voucher endpoints.
    /// Same shape as the /tally/masters response.
    /// </summary>
    public class TallyVoucherApiResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string VoucherType { get; set; } = string.Empty;

        public string VoucherNumber { get; set; } = string.Empty;

        public string Format { get; set; }

        public TallyVoucherInnerResponse Response { get; set; } = new();
    }

    public class TallyVoucherInnerResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        /// <summary>Raw reply from Tally (named ResponseXml to match the masters response).</summary>
        public string ResponseXml { get; set; } = string.Empty;
    }
}
