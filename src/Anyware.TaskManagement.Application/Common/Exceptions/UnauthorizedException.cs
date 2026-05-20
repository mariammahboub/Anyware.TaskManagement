using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Common.Exceptions
{
    public sealed class UnauthorizedException(string message = "Unauthorized.") : Exception(message);

}
