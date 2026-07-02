using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexBank.Application.DTOs;

public class LoginDto
{
    //vs_installer.exe --locale en-US
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

}
