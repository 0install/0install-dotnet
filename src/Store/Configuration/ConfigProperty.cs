// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Linq.Expressions;

namespace ZeroInstall.Store.Configuration;

/// <summary>
/// Represents a string-serializable configuration property.
/// </summary>
internal class ConfigProperty : PropertyPointer<string>
{
    /// <summary>
    /// The default value of the property.
    /// </summary>
    public string DefaultValue { get; }

    /// <summary>
    /// <c>true</c> if <see cref="PropertyPointer{T}.Value"/> is equal to <see cref="DefaultValue"/>.
    /// </summary>
    public bool IsDefaultValue => Equals(Value, DefaultValue);

    /// <summary>
    /// Indicates that this property needs to be encoded (e.g. as base64) before it can be stored in a file.
    /// </summary>
    public bool NeedsEncoding { get; }

    /// <summary>
    /// Creates a configuration property.
    /// </summary>
    /// <param name="getValue">A delegate that returns the current value.</param>
    /// <param name="setValue">A delegate that sets the value.</param>
    /// <param name="defaultValue">The default value of the property.</param>
    /// <param name="needsEncoding">Indicates that this property needs to be encoded (e.g. as base64) before it can be stored in a file.</param>
    private ConfigProperty(Func<string> getValue, Action<string> setValue, string defaultValue, bool needsEncoding = false)
        : base(getValue, setValue)
    {
        DefaultValue = defaultValue;
        NeedsEncoding = needsEncoding;
    }

    /// <summary>
    /// Creates a configuration property.
    /// </summary>
    /// <param name="expression">An expression pointing to the property.</param>
    /// <param name="defaultValue">The default value of the property.</param>
    /// <param name="needsEncoding">Indicates that this property needs to be encoded (e.g. as base64) before it can be stored in a file.</param>
    public static ConfigProperty For(Expression<Func<string>> expression, string defaultValue, bool needsEncoding = false)
        => new(expression.Compile(), expression.ToSetValue(), defaultValue, needsEncoding);

    /// <summary>
    /// Creates a configuration property for a boolean.
    /// </summary>
    /// <param name="expression">An expression pointing to the property.</param>
    /// <param name="defaultValue">The default value of the property.</param>
    public static ConfigProperty For(Expression<Func<bool>> expression, bool defaultValue = false)
    {
        var getValue = expression.Compile();
        var setValue = expression.ToSetValue();
        return new(
            () => getValue().ToString(CultureInfo.InvariantCulture),
            value => setValue(value == "1" || (value != "0" && bool.Parse(value))),
            defaultValue.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Creates a configuration property for an integer.
    /// </summary>
    /// <param name="expression">An expression pointing to the property.</param>
    /// <param name="defaultValue">The default value of the property.</param>
    public static ConfigProperty For(Expression<Func<int>> expression, int defaultValue = 0)
    {
        var getValue = expression.Compile();
        var setValue = expression.ToSetValue();
        return new(
            () => getValue().ToString(CultureInfo.InvariantCulture),
            value => setValue(string.IsNullOrEmpty(value) ? defaultValue : int.Parse(value)),
            defaultValue.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Creates a configuration property for a <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="expression">An expression pointing to the property.</param>
    /// <param name="defaultValue">The default value of the property.</param>
    public static ConfigProperty For(Expression<Func<TimeSpan>> expression, TimeSpan defaultValue = default)
    {
        var getValue = expression.Compile();
        var setValue = expression.ToSetValue();
        return new(
            () => getValue().TotalSeconds.ToString(CultureInfo.InvariantCulture),
            value => setValue(string.IsNullOrEmpty(value) ? defaultValue : TimeSpan.FromSeconds(int.Parse(value))),
            defaultValue.TotalSeconds.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Creates a configuration property for a <see cref="FeedUri"/>.
    /// </summary>
    /// <param name="expression">An expression pointing to the property.</param>
    /// <param name="defaultValue">The default value of the property.</param>
    public static ConfigProperty For(Expression<Func<FeedUri?>> expression, string defaultValue)
    {
        var getValue = expression.Compile();
        var setValue = expression.ToSetValue();
        return new(
            () => getValue()?.ToStringRfc() ?? "",
            value => setValue(string.IsNullOrEmpty(value) ? null : new(value)),
            defaultValue);
    }

    /// <summary>
    /// Creates a configuration property for a <see cref="NetworkLevel"/>.
    /// </summary>
    /// <param name="expression">An expression pointing to the property.</param>
    /// <param name="defaultValue">The default value of the property.</param>
    public static ConfigProperty For(Expression<Func<NetworkLevel>> expression, NetworkLevel defaultValue)
    {
        static string ToString(NetworkLevel value)
            => value switch
            {
                NetworkLevel.Full => "full",
                NetworkLevel.Minimal => "minimal",
                NetworkLevel.Offline => "off-line",
                _ => throw new InvalidEnumArgumentException()
            };

        static NetworkLevel FromString(string value)
            => value switch
            {
                "full" => NetworkLevel.Full,
                "minimal" => NetworkLevel.Minimal,
                "off-line" => NetworkLevel.Offline,
                _ => throw new FormatException("Must be 'full', 'minimal' or 'off-line'")
            };

        var getValue = expression.Compile();
        var setValue = expression.ToSetValue();
        return new(
            getValue: () => ToString(getValue()),
            setValue: value => setValue(FromString(value)),
            defaultValue: ToString(defaultValue));
    }
}
