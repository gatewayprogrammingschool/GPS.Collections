using System;
using System.Linq;

namespace GPS.Collections.Tests.Tests
{
    public class TestEntity : IComparable<TestEntity>
    {
        private Guid _keyField;
        private int _keyHash = 0;

        public Guid KeyField
        {
            get=>_keyField;
            private set
            {
                _keyField = value;
                _keyHash = value.GetHashCode();
            }
        }

        public string Name { get; set; }

        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string EmailAddress => $"{FirstName}.{Surname}@mailinator.com";
        public DateTime Dob { get; set; }
        public TimeSpan Age => DateTime.Today - Dob.Date;

        public TestEntity(Guid key = default)
        {
            KeyField = key != default ? key : Guid.NewGuid();
        }

        public TestEntity(string name) : this()
        {
            Name = name;
            FirstName = name;

            if (name.IndexOf(",", StringComparison.Ordinal) > -1)
            {
                Surname = name.Substring(0, name.IndexOf(',')).Trim();
                FirstName = name.Substring(Surname.Length + 1).Trim();
            }
            else if (name.IndexOf(' ') > -1)
            {
                var split = name.Split(' ');
                if (split.Length == 2)
                {
                    FirstName = split[0];
                    Surname = split[1];
                }
                else
                {
                    FirstName = string.Join(' ', split.SkipLast(1));
                    Surname = split.Last();
                }
            }
        }

        public override string ToString()
        {
            return $"{KeyField}: {FirstName} {Surname}";
        }

        public int CompareTo(TestEntity other)
        {
            return KeyField.CompareTo(other.KeyField);
        }

        public override bool Equals(object obj)
        {
            return obj is { } && 
                   _keyHash == ((TestEntity)obj)._keyHash;
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return _keyHash;
        }
    }
}