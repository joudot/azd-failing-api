using System.Runtime.Serialization;

namespace SimpleTodo.Api
{
    [Serializable]
    internal class IntendedException : Exception
    {
        public IntendedException()
        {
        }

        public IntendedException(string? message) : base(message)
        {
        }

        public IntendedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected IntendedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}