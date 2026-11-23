using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sang.Service.TallyIntegration.Models;

namespace Sang.Service.TallyIntegration.Builders
{
    /// <summary>
    /// Builds Tally JSONEx voucher payloads from typed requests.
    /// Sign convention: Debit = isdeemedpositive "Yes" with negative amount,
    ///                  Credit = isdeemedpositive "No" with positive amount.
    /// </summary>
    public class TallyVoucherJsonBuilder
    {
        private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

        // ---------------- SALES ----------------
        public string BuildSales(SalesVoucherRequest r)
        {
            ValidateBase(r, requireParty: true);
            var v = Header("Sales", r);

            if (r.Items.Count > 0)
            {
                ValidateItems(r.Items);
                var total = r.Items.Sum(i => i.Amount);
                v["allledgerentries"] = new JsonArray { Debit(r.PartyLedgerName, total) };
                v["allinventoryentries"] = InventoryArray(r.Items, debitSide: false, r.SalesLedgerName);
            }
            else
            {
                RequireAmount(r.Amount);
                RequireText(r.SalesLedgerName, "SalesLedgerName");
                v["allledgerentries"] = new JsonArray
                {
                    Debit(r.PartyLedgerName, r.Amount),
                    Credit(r.SalesLedgerName, r.Amount)
                };
            }
            return Wrap(r, v);
        }

        // ---------------- RECEIPT ----------------
        public string BuildReceipt(ReceiptVoucherRequest r)
        {
            ValidateBase(r, requireParty: true);
            RequireText(r.CashOrBankLedgerName, "CashOrBankLedgerName");
            RequireAmount(r.Amount);

            var v = Header("Receipt", r, includeParty: false);

            var party = Credit(r.PartyLedgerName, r.Amount);
            party["billallocations"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = string.IsNullOrWhiteSpace(r.VoucherNumber) ? "On Account" : r.VoucherNumber,
                    ["billtype"] = "On Account",
                    ["amount"] = Amt(Math.Abs(r.Amount))
                }
            };

            v["allledgerentries"] = new JsonArray
            {
                Debit(r.CashOrBankLedgerName, r.Amount),
                party
            };
            return Wrap(r, v);
        }

        // ---------------- SALES ORDER ----------------
        public string BuildSalesOrder(SalesOrderVoucherRequest r)
        {
            ValidateBase(r, requireParty: true);
            var v = Header("Sales Order", r);

            if (r.Items.Count > 0)
            {
                ValidateItems(r.Items);
                v["allinventoryentries"] =
                    InventoryArray(r.Items, debitSide: false, allocationLedger: null);
            }
            else
            {
                RequireAmount(r.Amount);
                v["allledgerentries"] = new JsonArray
        {
            Debit(r.PartyLedgerName, r.Amount)
        };
            }

            return Wrap(r, v);
        }
        // ---------------- SALES RETURN (CREDIT NOTE) ----------------
        public string BuildSalesReturn(SalesReturnVoucherRequest r)
        {
            ValidateBase(r, requireParty: true);
            RequireText(r.ReturnLedgerName, "ReturnLedgerName");
            var v = Header("Credit Note", r);

            if (r.Items.Count > 0)
            {
                ValidateItems(r.Items);
                var total = r.Items.Sum(i => i.Amount);
                v["allledgerentries"] = new JsonArray { Credit(r.PartyLedgerName, total) };
                v["allinventoryentries"] = InventoryArray(r.Items, debitSide: true, r.ReturnLedgerName);
            }
            else
            {
                RequireAmount(r.Amount);
                v["allledgerentries"] = new JsonArray
                {
                    Credit(r.PartyLedgerName, r.Amount),
                    Debit(r.ReturnLedgerName, r.Amount)
                };
            }
            return Wrap(r, v);
        }

        // =====================================================
        // helpers
        // =====================================================

        private static string Amt(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);

        private static JsonObject Debit(string ledger, decimal amount) => new()
        {
            ["ledgername"] = ledger,
            ["isdeemedpositive"] = "Yes",
            ["amount"] = Amt(-Math.Abs(amount))
        };

        private static JsonObject Credit(string ledger, decimal amount) => new()
        {
            ["ledgername"] = ledger,
            ["isdeemedpositive"] = "No",
            ["amount"] = Amt(Math.Abs(amount))
        };

        private static JsonObject Header(string voucherType, TallyVoucherRequestBase r, bool includeParty = true)
        {
            var meta = new JsonObject { ["type"] = "Voucher", ["action"] = "create" };
            if (!string.IsNullOrWhiteSpace(r.VoucherNumber))
                meta["name"] = r.VoucherNumber.Trim();

            var v = new JsonObject
            {
                ["metadata"] = meta,
                ["date"] = r.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                ["vouchertypename"] = voucherType
            };

            if (!string.IsNullOrWhiteSpace(r.VoucherNumber))
                v["vouchernumber"] = r.VoucherNumber.Trim();

            if (includeParty && !string.IsNullOrWhiteSpace(r.PartyLedgerName))
                v["partyledgername"] = r.PartyLedgerName.Trim();

            if (!string.IsNullOrWhiteSpace(r.Narration))
                v["narration"] = r.Narration;

            return v;
        }

        private static JsonArray InventoryArray(
            IEnumerable<TallyStockLineDto> items, bool debitSide, string? allocationLedger)
        {
            var arr = new JsonArray();
            string flag = debitSide ? "Yes" : "No";

            foreach (var s in items)
            {
                decimal signed = debitSide ? -s.Amount : s.Amount;
                string qty = s.Quantity.ToString("0.####", CultureInfo.InvariantCulture) + " " + s.Unit;

                var line = new JsonObject
                {
                    ["stockitemname"] = s.ItemName.Trim(),
                    ["isdeemedpositive"] = flag,
                    ["rate"] = Amt(s.Rate) + "/" + s.Unit,
                    ["amount"] = Amt(signed),
                    ["actualqty"] = qty,
                    ["billedqty"] = qty
                };

                if (!string.IsNullOrWhiteSpace(allocationLedger))
                {
                    line["accountingallocations"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["ledgername"] = allocationLedger,
                            ["isdeemedpositive"] = flag,
                            ["amount"] = Amt(signed)
                        }
                    };
                }

                if (!string.IsNullOrWhiteSpace(s.GodownName))
                {
                    line["batchallocations"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["godownname"] = s.GodownName,
                            ["batchname"] = "Primary Batch",
                            ["actualqty"] = qty,
                            ["billedqty"] = qty,
                            ["amount"] = Amt(signed)
                        }
                    };
                }
                arr.Add(line);
            }
            return arr;
        }

        private static string Wrap(TallyVoucherRequestBase r, JsonObject voucher)
        {
            var vars = new JsonArray
            {
                new JsonObject { ["name"] = "svVchImportFormat", ["value"] = "JSONEx" }
            };

            if (!string.IsNullOrWhiteSpace(r.CompanyName))
                vars.Add(new JsonObject { ["name"] = "svCurrentCompany", ["value"] = r.CompanyName.Trim() });

            var payload = new JsonObject
            {
                ["static_variables"] = vars,
                ["tallymessage"] = new JsonArray(voucher)
            };
            return payload.ToJsonString(Pretty);
        }

        // ---------------- validation ----------------

        private static void ValidateBase(TallyVoucherRequestBase? r, bool requireParty)
        {
            ArgumentNullException.ThrowIfNull(r);
            if (requireParty) RequireText(r.PartyLedgerName, "PartyLedgerName");
        }

        private static void RequireText(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{name} is required.");
        }

        private static void RequireAmount(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");
        }

        private static void ValidateItems(List<TallyStockLineDto>? items)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("At least one stock item is required.");

            foreach (var i in items)
            {
                RequireText(i.ItemName, "ItemName");
                RequireText(i.Unit, "Unit");
                if (i.Quantity <= 0) throw new ArgumentException($"Quantity must be greater than zero for '{i.ItemName}'.");
                if (i.Rate < 0) throw new ArgumentException($"Rate cannot be negative for '{i.ItemName}'.");
            }
        }
    }
}
