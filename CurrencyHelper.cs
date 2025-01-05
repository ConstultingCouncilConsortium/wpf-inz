using System;
using System.Collections.Generic;
using System.Linq;

namespace wpf_inz
{
    public static class CurrencyHelper
    {
        // Trzymajmy to w pamięci statycznie, 
        // ale trzeba to wypełnić np. na starcie aplikacji
        // lub za każdym razem, gdy się logujemy itp.
        private static Dictionary<(string Currency, int Year, int Month), decimal> _exchangeRates;

        // Ustawienie słownika z zewnątrz (np. z BudgetView lub StartView)
        public static void SetExchangeRates(Dictionary<(string, int, int), decimal> rates)
        {
            _exchangeRates = rates;
        }

        // Metoda do pobrania "RateToPLN" z najbliższego poprzedniego miesiąca
        // (a jeśli w tym samym roku i miesiącu jest brak, to schodzimy wstecz).
        private static decimal GetRateToPLN(string currency, DateTime date)
        {
            // Najpierw sprawdźmy, czy mamy EXACT (Waluta, year, month)
            var keyExact = (currency, date.Year, date.Month);
            if (_exchangeRates != null && _exchangeRates.TryGetValue(keyExact, out var rateExact))
            {
                return rateExact;
            }

            // Jeśli brak EXACT, szukamy najnowszego kursu, który (Year,Month) <= (date.Year, date.Month).
            // Znajdźmy wszystkie wpisy, waluta = currency, a (Year < date.Year) 
            //   lub Year = date.Year i Month <= date.Month
            // Potem sortujemy malejąco i bierzemy pierwszy
            if (_exchangeRates != null)
            {
                var fallback = _exchangeRates
                    .Where(x =>
                        x.Key.Item1 == currency // waluta
                        && (x.Key.Item2 < date.Year
                            || (x.Key.Item2 == date.Year && x.Key.Item3 <= date.Month))
                    )
                    .OrderByDescending(x => x.Key.Item2)  // najpierw sort. po Year malejąco
                    .ThenByDescending(x => x.Key.Item3)   // potem po Month malejąco
                    .FirstOrDefault();                   // bierzemy pierwszy

                if (!fallback.Equals(default(KeyValuePair<(string, int, int), decimal>)))
                {
                    return fallback.Value; // to jest najnowszy dostępny kurs
                }
            }

            // Jeśli w ogóle brak czegokolwiek, np. currency = "PLN" i go nie mamy:
            // Możesz przyjąć, że 1.0
            if (currency == "PLN") return 1.0m;

            // Fallback: 0 lub rzuć wyjątek
            return 0m;
        }

        // Zamiana kwoty budżetu (Budget.Amount, Budget.Currency, Budget.Date) na PLN
        public static decimal GetAmountInPLN(Budget budget)
        {
            decimal rate = GetRateToPLN(budget.Currency, budget.Date);
            return budget.Amount * rate;
        }

        // Mając kwotę w PLN, chcemy przerzucić do Session.SelectedCurrency (np. "USD")
        public static decimal ConvertPLNToSelectedCurrency(decimal amountInPLN, DateTime date)
        {
            var selected = Session.SelectedCurrency; // np. "USD"
            if (selected == "PLN") return amountInPLN;

            // Najpierw 1 [selected] = X PLN:
            decimal rateToPLN = GetRateToPLN(selected, date);
            if (rateToPLN == 0m) return 0m;

            // 1 PLN = 1 / X [selected]
            // => amountInPLN PLN = amountInPLN / X [selected]
            return amountInPLN / rateToPLN;
        }
    }
}
