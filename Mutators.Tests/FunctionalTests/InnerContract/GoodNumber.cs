using System;

namespace Mutators.Tests.FunctionalTests.InnerContract
{
    public class GoodNumber : IComparable<GoodNumber>
    {
        public int CompareTo(GoodNumber other)
        {
            if (MessageType != other.MessageType)
                return MessageType.CompareTo(other.MessageType);
            return Number.CompareTo(other.Number);
        }

        public bool Equals(GoodNumber other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.MessageType, MessageType) && other.Number == Number;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(GoodNumber)) return false;
            return Equals((GoodNumber)obj);
        }

        public override int GetHashCode()
        {
            return Number * 397 + (int)MessageType;
        }

        public MessageType MessageType { get; set; }
        public int Number { get; set; }
    }
}