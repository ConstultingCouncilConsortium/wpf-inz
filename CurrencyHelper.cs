using System;
using System.Collections.Generic;
using System.Linq;

namespace wpf_inz
{
    public static class CurrencyHelper
    {

        private static Dictionary<(string Currency, int Year, int Month), decimal> _exchangeRates;

        
        public static void SetExchangeRates(Dictionary<(string, int, int), decimal> rates)
        {
            _exchangeRates = rates;
        }

   
        private static decimal GetRateToPLN(string currency, DateTime date)
        {
            var keyExact = (currency, date.Year, date.Month);
            if (_exchangeRates != null && _exchangeRates.TryGetValue(keyExact, out var rateExact))
            {
                return rateExact;
            }

            if (_exchangeRates != null)
            {
                var fallback = _exchangeRates
                    .Where(x =>
                        x.Key.Item1 == currency 
                        && (x.Key.Item2 < date.Year
                            || (x.Key.Item2 == date.Year && x.Key.Item3 <= date.Month))
                    )
                    .OrderByDescending(x => x.Key.Item2)  
                    .ThenByDescending(x => x.Key.Item3)   
                    .FirstOrDefault();                  

                if (!fallback.Equals(default(KeyValuePair<(string, int, int), decimal>)))
                {
                    return fallback.Value; 
                }
            }

           
            if (currency == "PLN") return 1.0m;

            
            return 0m;
        }

        
        public static decimal GetAmountInPLN(Budget budget)
        {
            decimal rate = GetRateToPLN(budget.Currency, budget.Date);
            return budget.Amount * rate;
        }

       
        public static decimal ConvertPLNToSelectedCurrency(decimal amountInPLN, DateTime date)
        {
            var selected = Session.SelectedCurrency; 
            if (selected == "PLN") return amountInPLN;

          
            decimal rateToPLN = GetRateToPLN(selected, date);
            if (rateToPLN == 0m) return 0m;


            return amountInPLN / rateToPLN;
        }
    }
}
