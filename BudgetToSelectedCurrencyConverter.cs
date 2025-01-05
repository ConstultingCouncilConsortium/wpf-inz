using System;
using System.Globalization;
using System.Windows.Data;

namespace wpf_inz
{
    public class BudgetToSelectedCurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Budget budget)
            {
                decimal amountPLN = CurrencyHelper.GetAmountInPLN(budget);
                decimal convertedValue = CurrencyHelper.ConvertPLNToSelectedCurrency(amountPLN, budget.Date);

                if (convertedValue == 0m && budget.Amount != 0m)
                {
                    // Tzn. kursu brak, a budżet != 0 – wyświetl coś innego
                    return "";
                }

                // Formatowanie do 2 miejsc:
                return convertedValue.ToString("N2", culture);
            }
            return "";
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
