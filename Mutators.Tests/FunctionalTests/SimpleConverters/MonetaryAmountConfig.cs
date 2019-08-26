using System;
using System.Linq.Expressions;

namespace Mutators.Tests.FunctionalTests.SimpleConverters
{
    public class MonetaryAmountConfig<T>
    {
        public MonetaryAmountConfig(string monetaryAmountsFunctionalCode, Expression<Func<T, decimal?>> pathsToMoa, string statusDescriptionCode = null)
        {
            MonetaryAmountsFunctionalCodes = new[] { monetaryAmountsFunctionalCode };
            StatusDescriptionCode = statusDescriptionCode;
            PathsToMOA = pathsToMoa;
        }

        public MonetaryAmountConfig(string[] monetaryAmountsFunctionalCodes, Expression<Func<T, decimal?>> pathsToMoa, string statusDescriptionCode = null)
        {
            MonetaryAmountsFunctionalCodes = monetaryAmountsFunctionalCodes;
            StatusDescriptionCode = statusDescriptionCode;
            PathsToMOA = pathsToMoa;
        }

        public string[] MonetaryAmountsFunctionalCodes { get; set; }
        public string StatusDescriptionCode { get; set; }
        public Expression<Func<T, decimal?>> PathsToMOA { get; set; }
    }
}