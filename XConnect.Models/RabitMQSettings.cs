using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XConnect.Models
{
    public class RabitMQSettings
    {
        public string? IsRabbitMQActive { get; set; } = "0"; //24-06-25
        public string? HostName { get; set; } = String.Empty; //270525

        public string? Port { get; set; } = String.Empty; //270525

        public string? UserName { get; set; } = String.Empty; //270525

        public string? Password { get; set; } = String.Empty; //270525

    }
}
