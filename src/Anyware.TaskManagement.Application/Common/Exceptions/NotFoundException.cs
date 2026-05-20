using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Common.Exceptions
{
    public sealed class NotFoundException(string entity, object key)
       : Exception($"{entity} with key '{key}' was not found.");
}
