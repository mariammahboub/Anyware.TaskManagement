using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Anyware.TaskManagement.Application.Common.Exceptions
{
    public sealed class UnauthorizedException : Exception
    {
        public UnauthorizedException()
            : base("You are not authenticated. Please log in and supply a valid token.") { }
        public UnauthorizedException(string message)
            : base(message) { }
    }
}
