using System;

using GrobExp.Mutators.CustomFields;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public abstract class InnerDocumentBase
    {
        [CustomField]
        public PartyInfo Supplier { get; set; }

        [CustomField]
        public PartyInfo Buyer { get; set; }

        [CustomField]
        public DespatchPartyInfo[] DespatchParties { get; set; }

        public string OrdersNumber { get; set; }

        public DateTime? OrdersDate { get; set; }

        public DateTime? CreationDateTimeBySender { get; set; }

        public Package[] Packages { get; set; }

        public string FromGln { get; set; }
        public string ToGln { get; set; }

        public bool IsTest { get; set; }
        
        public CommonGoodItem[] GoodItems { get; set; }
    }
}