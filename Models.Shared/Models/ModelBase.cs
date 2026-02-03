using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Shared.Models
{
    public class ModelBase
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
        public ModelBase() { }
        public ModelBase(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}
