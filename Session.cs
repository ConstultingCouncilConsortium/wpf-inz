using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wpf_inz
{
    public static class Session
    {
        public static User CurrentUser { get; set; }
        public static string SelectedCurrency { get; set; } = "PLN"; // domyślnie
    }
}

