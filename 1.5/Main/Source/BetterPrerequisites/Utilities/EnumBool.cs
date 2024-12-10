using System;

namespace BigAndSmall
{
    public struct EnumBool<TEnum> where TEnum : Enum
    {
        private readonly bool _value;
        private readonly bool _isBool;
        public TEnum Outcome { get; private set; }
        private EnumBool(bool value)
        {
            _value = value;
            Outcome = (TEnum)Enum.GetValues(typeof(TEnum)).GetValue(value ? 1 : 0);
        }
        private EnumBool(TEnum outcome)
        {
            Outcome = outcome;
            int oInt = Convert.ToInt32(outcome);
            _value = oInt != 0;
            _isBool = oInt < 2;
        }
        public static implicit operator EnumBool<TEnum>(bool value) => new(value);
        public static implicit operator EnumBool<TEnum>(TEnum outcome) => new(outcome);
        public static bool operator ==(EnumBool<TEnum> left, EnumBool<TEnum> right) => left.Outcome.Equals(right.Outcome);
        public static bool operator !=(EnumBool<TEnum> left, EnumBool<TEnum> right) => !left.Outcome.Equals(right.Outcome);
        public static bool operator ==(EnumBool<TEnum> left, bool right) => left._value == right;
        public static bool operator !=(EnumBool<TEnum> left, bool right) => left._value != right;

        public static bool AsBool(EnumBool<TEnum> value) => value._value;
        public static bool? AsPureBool(EnumBool<TEnum> value) => value._isBool ? value._value : null;
        public override readonly bool Equals(object obj)
        {
            if (obj is null) return false;
            if (obj is EnumBool<TEnum> other) return Outcome.Equals(other.Outcome);
            if (obj is bool b) return _value == b;
            return false;
        }
        public override readonly int GetHashCode() => _isBool ? (_value ? 1 : 0) : _value.GetHashCode() ^ Outcome.GetHashCode();
        public override readonly string ToString() => Outcome.ToString();
    }
    public static class EnumBoolComparer
    {
        public static bool CompareEnumBoolOutcome<TEnum1, TEnum2>(EnumBool<TEnum1> first, EnumBool<TEnum2> second)
            where TEnum1 : Enum
            where TEnum2 : Enum
        {
            (bool? b1, bool? b2) = (EnumBool<TEnum1>.AsPureBool(first), EnumBool<TEnum2>.AsPureBool(second));
            return b1.HasValue && b2.HasValue && b1.Value == b2.Value;
        }
    }
}
