namespace Sang.Service.TallyIntegration.Models
{
    /// <summary>Stock line used by Sales, Sales Order and Sales Return.</summary>
    public class TallyStockLineDto
    {
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public string Unit { get; set; } = "Nos";

        /// <summary>Optional. Adds a godown/batch allocation when set.</summary>
        public string? GodownName { get; set; }

        public decimal Amount => Math.Round(Quantity * Rate, 2);
    }

    /// <summary>Fields common to every voucher.</summary>
    public abstract class TallyVoucherRequestBase
    {
        // Carried through to logs/responses (same meaning as in TallyVoucherJsonRequest).
        public int TransId { get; set; }
        public int DocType { get; set; }
        public bool IsForIssue { get; set; }

        /// <summary>Optional. Empty = currently open company in Tally.</summary>
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>Optional. Empty = Tally auto-numbers.</summary>
        public string VoucherNumber { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.Today;

        public string PartyLedgerName { get; set; } = string.Empty;

        public string Narration { get; set; } = string.Empty;
    }

    /// <summary>
    /// Sales. Provide Amount for an accounting-only invoice,
    /// or Items for a stock invoice (Amount is then calculated).
    /// </summary>
    public class SalesVoucherRequest : TallyVoucherRequestBase
    {
        public string SalesLedgerName { get; set; } = "Sales";
        public decimal Amount { get; set; }
        public List<TallyStockLineDto> Items { get; set; } = new();
    }

    /// <summary>Receipt: Cash/Bank debit, party credit.</summary>
    public class ReceiptVoucherRequest : TallyVoucherRequestBase
    {
        /// <summary>Must be a real Cash/Bank ledger, not the group name.</summary>
        public string CashOrBankLedgerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    /// <summary>Sales Order: stock lines only.</summary>
    public class SalesOrderVoucherRequest : TallyVoucherRequestBase
    {
        public decimal Amount { get; set; }
        public List<TallyStockLineDto> Items { get; set; } = new();
    }

    /// <summary>Sales Return (Credit Note).</summary>
    public class SalesReturnVoucherRequest : TallyVoucherRequestBase
    {
        public string ReturnLedgerName { get; set; } = "Sales";
        public decimal Amount { get; set; }
        public List<TallyStockLineDto> Items { get; set; } = new();
    }
}
