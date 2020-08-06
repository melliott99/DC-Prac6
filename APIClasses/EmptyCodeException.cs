using System;
using System.Collections.Generic;
using System.Text;

namespace APIClasses
{
	/* Guided by:
	*  https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/exceptions/creating-and-throwing-exceptions
	*/
	[Serializable]
	public class EmptyCodeException : System.Exception
	{
		public EmptyCodeException() : base() { }
		public EmptyCodeException(string message) : base(message) { }
		public EmptyCodeException(string message, System.Exception inner) : base(message, inner) { }
		protected EmptyCodeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

	}
}
