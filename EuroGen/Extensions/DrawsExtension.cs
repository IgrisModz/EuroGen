using EuroGen.Models;

namespace EuroGen.Extensions;

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

	public static IEnumerable<int> GetValues(this Draw draw, IEnumerable<Func<Draw, int>> selectors)
	{
		return selectors.Select(selector => selector(draw));
	}

	public static IEnumerable<(int Value, DateTime Date)> GetValuesAndDates(this Draw draw, IEnumerable<Func<Draw, int>> selectors)
	{
		return selectors.Select(selector => (Value: selector(draw), Date: draw.DrawDate));
	}
}