using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Worker.Domain.Exceptions
{
    [Serializable]
    public sealed class DomainValidationException : Exception
    {
        public DomainValidationException() { }
        public DomainValidationException(string message) : base(message) { }
        public DomainValidationException(string message, Exception inner) : base(message, inner) { }
        private DomainValidationException(SerializationInfo info, StreamingContext  context) : base(info, context) { }
    }
}
