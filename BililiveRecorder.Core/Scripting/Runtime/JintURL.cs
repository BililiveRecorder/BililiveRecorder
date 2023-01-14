using Flurl;

namespace BililiveRecorder.Core.Scripting.Runtime
{
    internal class JintURL
    {
        private Url url;

        public JintURL(string url) : this(url, null) { }

        public JintURL(string url, string? @base)
        {
            this.url = @base is not null ? new Url(Url.Combine(@base, url)) : new Url(url);
        }

        public string Hash
        {
            get => '#' + this.url.Fragment;
            set => this.url.Fragment = value.TrimStart('#');
        }

        public string Host
        {
            get => this.url.Authority;
            set
            {
                if (value.Contains(":"))
                {
                    var parts = value.Split(':');
                    this.url.Host = parts[0];
                    this.url.Port = int.Parse(parts[1]);
                }
                else
                {
                    this.url.Host = value;
                    this.url.Port = null;
                }
            }
        }

        public string Hostname
        {
            get => this.url.Host;
            set => this.url.Host = value;
        }

        public string Href
        {
            get => this.url.ToString();
            set => this.url = new Url(value);
        }

        public string Origin => this.url.Scheme + "://" + this.url.Authority;

        public string Password
        {
            get
            {
                var parts = this.url.UserInfo.Split(':');
                return parts.Length == 1 ? "" : parts[1];
            }
            set
            {
                var result = string.IsNullOrEmpty(this.url.UserInfo) ? ":" + value : this.url.UserInfo.Split(':')[0] + ":" + value;
                this.url.UserInfo = result == ":" ? "" : result;
            }
        }

        public string Pathname
        {
            get => this.url.Path;
            set => this.url.Path = value;
        }

        public string Port
        {
            get => this.url.Port?.ToString() ?? string.Empty;
            set => this.url.Port = int.TryParse(value, out var port) ? port : null;
        }

        public string Protocol
        {
            get => this.url.Scheme + ':';
            set => this.url.Scheme = value.TrimEnd(':');
        }

        public string Search
        {
            get => '?' + this.url.Query;
            set => this.url.Query = value.TrimStart('?');
        }

        public JintURLSearchParams SearchParams => new JintURLSearchParams(this.url.QueryParams);

        public string ToJSON() => this.url.ToString();
        public override string ToString() => this.url.ToString();
    }
}
