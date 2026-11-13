using System.Text.Json.Serialization;

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

            public int Code { get; set; }

            public TallyImportResult? Result { get; set; }

            public string Message { get; set; } = string.Empty;
        }

     
        public class TallyImportResult
        {
           [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
             public int? Created { get; set; }

           [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
           public int? Altered { get; set; }

          [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
          public int? Deleted { get; set; }
        }
        public class TallyLedgerDto
        {
            public string Name { get; set; } = string.Empty;

            public string Parent { get; set; } = string.Empty;

            public string MasterId { get; set; } = string.Empty;

            public string ClosingBalance { get; set; } = string.Empty;
        }
}

