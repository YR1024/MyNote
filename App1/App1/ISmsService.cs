using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace App1
{
    public interface ISmsService
    {
        Task<string> ReadLatestSmsAsync();

        Task<string> ReadLatestSmsAsync(string phoneNumber);

        Task<string> ReadLatestSmsAsync(DateTime time, string phoneNumber);
    }
}
