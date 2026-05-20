using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anyware.TaskManagement.Application.Common.Models
{
    public sealed record ErrorResponse
    {
        public int StatusCode { get; init; }

        public string Message { get; init; } = default!;

        public IDictionary<string, string[]>? Errors { get; init; }

        public string? TraceId { get; init; }
    }
}
