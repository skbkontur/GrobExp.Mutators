using System;

namespace Mutators.Tests.FunctionalTests.FirstOuterContract
{
    public class FirstContractDocumentHeader
    {
        public string Sender { get; set; }

        public string Recipient { get; set; }

        public string IsTest { get; set; }

        public DateTime? CreationDateTime { get; set; }

    }
}