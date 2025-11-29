using System;
using System.Text;

namespace BililiveRecorder.Web
{
    public class BasicAuthCredential
    {
        public BasicAuthCredential(string username, string password)
        {
            this.Username = username ?? throw new ArgumentNullException(nameof(username));
            this.Password = password ?? throw new ArgumentNullException(nameof(password));
            this.EncoededValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        }

        public string Username { get; }
        public string Password { get; }
        public string EncoededValue { get; }
    }
}
