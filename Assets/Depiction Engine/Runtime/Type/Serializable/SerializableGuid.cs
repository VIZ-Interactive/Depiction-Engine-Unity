using System;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// A serializable Global Unique Identifier
    /// </summary>
    [Serializable]
    public struct SerializableGuid : IEquatable<SerializableGuid>, ISerializationCallbackReceiver
    {
        public static readonly SerializableGuid Empty = new SerializableGuid(Guid.Empty);

#if UNITY_EDITOR
        public const string GUID_STR_FIELD_NAME = nameof(_guidStr);
#endif

        private Guid _guid;

        [SerializeField]
        private string _guidStr;

        public SerializableGuid(string guid)
        {
            _guid = Guid.Parse(guid);
            _guidStr = _guid.ToString();
        }

        public SerializableGuid(Guid guid)
        {
            _guid = guid;
            _guidStr = _guid.ToString();
        }

        public SerializableGuid(SerializableGuid guid)
        {
            _guid = guid._guid;
            _guidStr = _guid.ToString();
        }

        public static SerializableGuid NewGuid()
        {
            return Guid.NewGuid();
        }

        public static SerializableGuid Parse(string input)
        {
            return new SerializableGuid(input);
        }

        public static bool TryParse(string input, out SerializableGuid result)
        {
            if (Guid.TryParse(input, out Guid guid))
            {
                result = new SerializableGuid(guid);
                return true;
            }
            else
            {
                result = Empty;
                return false;
            }
        }

        public void OnBeforeSerialize()
        {

        }

        public void OnAfterDeserialize()
        {
            if (!Guid.TryParse(_guidStr, out Guid parsedGuid))
                parsedGuid = Guid.Empty;
            
            _guid = parsedGuid;
        }

        public static implicit operator SerializableGuid(Guid guid)
        {
            return Parse(guid.ToString());
        }

        public static implicit operator Guid(SerializableGuid serializableGuid)
        {
            return Guid.Parse(serializableGuid.ToString());
        }

        public int CompareTo(object value)
        {
            return value is SerializableGuid && this == (SerializableGuid)value ? 0 : 1;
        }

        public static bool operator ==(SerializableGuid lhs, Guid rhs)
        {
            Guid lhsGuid = Guid.Empty;
            if (!Object.ReferenceEquals(lhs, null))
                lhsGuid = lhs._guid;

            return lhsGuid == rhs;
        }

        public static bool operator !=(SerializableGuid lhs, Guid rhs)
        {
            Guid lhsGuid = Guid.Empty;
            if (!Object.ReferenceEquals(lhs, null))
                lhsGuid = lhs._guid;

            return lhsGuid != rhs;
        }

        public static bool operator ==(SerializableGuid lhs, SerializableGuid rhs)
        {
            Guid lhsGuid = Guid.Empty;
            if (!Object.ReferenceEquals(lhs, null))
                lhsGuid = lhs._guid;

            Guid rhsGuid = Guid.Empty;
            if (!Object.ReferenceEquals(rhs, null))
                rhsGuid = rhs._guid;

            return lhsGuid == rhsGuid;
        }

        public static bool operator !=(SerializableGuid lhs, SerializableGuid rhs)
        {
            Guid lhsGuid = Guid.Empty;
            if (!Object.ReferenceEquals(lhs, null))
                lhsGuid = lhs._guid;

            Guid rhsGuid = Guid.Empty;
            if (!Object.ReferenceEquals(rhs, null))
                rhsGuid = rhs._guid;

            return lhsGuid != rhsGuid;
        }

        public override bool Equals(object obj)
        {
            if (obj is SerializableGuid)
                return Equals((SerializableGuid)obj);
            return false;
        }

        public bool Equals(SerializableGuid other)
        {
            return _guid.Equals(other._guid);
        }

        public override int GetHashCode()
        {
            return _guid.GetHashCode();
        }

        public override string ToString()
        {
            return _guid.ToString();
        }
    }
}