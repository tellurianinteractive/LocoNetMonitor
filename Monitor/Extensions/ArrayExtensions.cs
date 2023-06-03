namespace Tellurian.Trains.LocoNetMonitor.Extensions;
public static class ArrayExtensions
{
    public static void SetAll<T>(this T[] array, T value) where T : struct
    {
        for (int i = 0; i < array.Length; i++) { array[i] = value; }
    }

    public static void SetWhen<T>(this T[] array, T value, Func<T, bool> predicate)
    {
        for (int i = 0; i < array.Length; i++) { if (predicate(array[i])) array[i] = value; }

    }
}
