using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Anyware.TaskManagement.Application.Common.Exceptions
{
    public sealed class ForbiddenException : Exception
    {
        public ForbiddenException()
            : base("You do not have permission to perform this action.") { }
        public ForbiddenException(string message)
            : base(message) { }
    }
}
