using System;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class TransportDetails
    {
        public string VehicleNumber { get; set; }
        public string TypeOfTransport { get; set; }
        public string TypeOfTransportCode { get; set; }
        public DateTime? DeliveryDateForVehicle { get; set; }
    }
}