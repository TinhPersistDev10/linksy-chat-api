using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace linksy_backend_api.DTOs
{
    public class ApiResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object Data { get; set; } = new object();   
        
    }
}