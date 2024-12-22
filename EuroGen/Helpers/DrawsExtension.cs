using EuroGen.Models;

namespace EuroGen.Helpers;

public enum OrderBy
{
    None,
    Key,
    KeyDescending,
    Value,
    ValueDescending
}

public static class DrawsExtension
{
    public static IDictionary<TKey, TValue> ReorderBy<TKey, TValue>(this IDictionary<TKey, TValue> values, OrderBy order = OrderBy.None) where TKey : notnull
    {
        return order switch
        {
            OrderBy.Key => values.OrderBy(d => d.Key).ToDictionary(),
            OrderBy.KeyDescending => values.OrderByDescending(d => d.Key).ToDictionary(),
            OrderBy.Value => values.OrderBy(d => d.Value).ToDictionary(),
            OrderBy.ValueDescending => values.OrderByDescending(d => d.Value).ToDictionary(),
            _ => values,
        };
    }

    public static IDictionary<int, int> CalculateNumbers(this IEnumerable<int> collection)
    {
        return collection.GroupBy(i => i).ToDictionary(g => g.Key, g => g.Count());
    }

    public static IDictionary<int, double> CalculateNumbersPercent(this IDictionary<int, int> counts, int? total)
    {
        total ??= counts.Values.Sum();
        return counts.ToDictionary(kv => kv.Key, kv => (double)kv.Value / (int)total * 100);
    }

    public static IDictionary<int, DateTime> CalculateLastDates(this IEnumerable<(int Value, DateTime Date)> collection)
    {
        return collection.GroupBy(x => x.Value)
            .ToDictionary(g => g.Key, g => g.Max(x => x.Date));
    }

    public static (int Value, DateTime Date) GetPropertyValueAndDate(this Draw obj, string propertyName)
    {
        var value = (int)obj.GetType().GetProperty(propertyName)?.GetValue(obj)!;
        return (Value: value, Date: obj.DrawDate);
    }

    public static int GetPropertyValue(this Draw obj, string propertyName)
    {
        var value = (int)obj.GetType().GetProperty(propertyName)?.GetValue(obj)!;
        return value;
    }
}
