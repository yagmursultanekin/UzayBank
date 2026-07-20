using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UzayBank.Application.DTOs
{
    public class CreateUzayAccountDto
    {
        /// <summary>Para birimi — şimdilik yalnızca TL destekleniyor.</summary>
        public string Currency { get; set; } = "TL";
    }
}
