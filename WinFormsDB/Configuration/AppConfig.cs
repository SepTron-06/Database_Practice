using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsDB.Configuration
{
    public static class AppConfig
    {
        public static string ConnectionString { get; } =
            "Host=localhost;Port=5432;Database=UD;Username=postgres;Password=1;";
    }
}
