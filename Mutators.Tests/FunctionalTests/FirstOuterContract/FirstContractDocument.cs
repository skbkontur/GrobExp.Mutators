using System;

namespace Mutators.Tests.FunctionalTests.FirstOuterContract
{
    public class FirstContractDocument
    {
        public string Id { get; set; }

        public DateTime? CreationDateTime { get; set; }

        public FirstContractDocumentHeader Header { get; set; }

        public FirstContractDocumentBody[] Document { get; set; }
    }
}