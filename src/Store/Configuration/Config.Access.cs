// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections;
using NanoByte.Common.Values;

namespace ZeroInstall.Store.Configuration;

partial class Config
{
    /// <summary>
    /// Retrieves the string representation of an option identified by a key.
    /// </summary>
    /// <param name="key">The key of the option to retrieve.</param>
    /// <returns>The string representation of the the option.</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="key"/> is invalid.</exception>
    public string GetOption(string key)
        => _metaData[key].Value;

    /// <summary>
    /// Sets an option identified by a key.
    /// </summary>
    /// <param name="key">The key of the option to set.</param>
    /// <param name="value">A string representation of the option.</param>
    /// <exception cref="KeyNotFoundException"><paramref name="key"/> is invalid.</exception>
    /// <exception cref="FormatException"><paramref name="value"/> is invalid.</exception>
    /// <exception cref="UnauthorizedAccessException">This option is controlled by a group policy and can therefore not be modified.</exception>
    public void SetOption(string key, string value)
    {
        if (IsOptionLocked(key))
            throw new UnauthorizedAccessException(Resources.OptionLockedByPolicy);

        _metaData[key].Value = value;
    }

    /// <summary>
    /// Resets an option identified by a key to its default value.
    /// </summary>
    /// <param name="key">The key of the option to reset.</param>
    /// <exception cref="KeyNotFoundException"><paramref name="key"/> is invalid.</exception>
    public void ResetOption(string key)
        => SetOption(key, _metaData[key].DefaultValue);

    /// <summary>
    /// Creates a deep copy of this <see cref="Config"/> instance.
    /// </summary>
    public Config Clone()
    {
        var newConfig = new Config();
        foreach ((string key, var property) in _metaData)
            newConfig._metaData[key].Value = property.Value;
        return newConfig;
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        foreach ((string key, var property) in _metaData)
            yield return new(key, property.NeedsEncoding && !string.IsNullOrEmpty(property.Value) ? "***" : property.Value);
    }

    /// <inheritdoc/>
    public bool Equals(Config? other)
        => other != null
        && _metaData.All(property => property.Value.Value == other.GetOption(property.Key));

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        if (obj == this) return true;
        return obj is Config config && Equals(config);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
        => _metaData.GetUnsequencedHashCode(
            new KeyEqualityComparer<ConfigProperty, string>(x => x.Value));
}
