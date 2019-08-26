using System;

using GrobExp.Mutators.CustomFields;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class CommonGoodItem : GoodItemWithAdditionalInfo, IComparable<CommonGoodItem>
    {
        public int CompareTo(CommonGoodItem other)
        {
            var n1 = GoodNumber ?? new GoodNumber {MessageType = MessageType.Junk};
            var n2 = other.GoodNumber ?? new GoodNumber {MessageType = MessageType.Junk};
            return n1.CompareTo(n2);
        }

        [CustomField]
        public string Comment { get; set; }

        public GoodNumber GoodNumber { get; set; }

        [CustomField]
        public DateTime? ExpireDate { get; set; }

        public bool ExcludeFromSummation { get; set; }

        public decimal? Price { get; set; }

        public decimal? PriceSummary { get; set; }

        public string FlowType { get; set; }

        [CustomField]
        public string Dimensions { get; set; }

        public CustomDeclaration[] Declarations { get; set; }

        public string[] Marks { get; set; }

        public string[] CountriesOfOriginCode { get; set; }

        public decimal? ExciseTax { get; set; }

        public Quantity Quantity { get; set; }

        public QuantityVariance[] QuantityVariances { get; set; }

        public PackageForItem[] Packages { get; set; }

        public bool IsReturnable { get; set; }
    }
}