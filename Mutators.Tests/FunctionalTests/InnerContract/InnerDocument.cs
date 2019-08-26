using System;

using GrobExp.Mutators.CustomFields;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class InnerDocument : InnerDocumentBase
    {
        public TransportDetails TransportDetails { get; set; }

        public TransportDetails[] Transports { get; set; }

        [CustomField]
        public string TransportBy { get; set; }

        public string CurrencyCode { get; set; }

        public DocumentsDeliveryType? DeliveryType { get; set; }

        public decimal? SumTotal { get; set; }
        public decimal? TotalWithVAT { get; set; }
        
        [CustomField]
        public string FlowType { get; set; }

        public TypeOfDocument? RecadvType { get; set; }
        
        public int? IntervalLength { get; set; }
        
        public string[] MessageCodes { get; set; }
        
        public bool? Nullify { get; set; }
        public DateTime? ReceivingDate { get; set; }
        public string BlanketOrdersNumber { get; set; }
        public decimal? RecadvTotal { get; set; }

        public string FreeText { get; set; }
        public decimal? OrdersTotalPackageQuantity { get; set; }

        public ContractInfo[] Contracts { get; set; }
    }
}