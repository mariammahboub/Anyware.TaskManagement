using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Common.Exceptions
{
    internal static class CacheKeys
    {
        internal static string Task(Guid taskId) => $"task:{taskId}";
    }
}
