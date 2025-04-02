// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Commands.Basic;

/// <summary>
/// View or change <see cref="Config"/>.
/// </summary>
public class Configure : CliCommand
{
    public const string Name = "config";
    public override string Description => Resources.DescriptionConfig;
    public override string Usage => "[NAME [VALUE|default]]";
    protected override int AdditionalArgsMax => 2;

    private bool _machineWide;
    private ConfigTab _tab;

    /// <inheritdoc/>
    public Configure(ICommandHandler handler)
        : base(handler)
    {
        Options.Add("m|machine", () => Resources.OptionMachine, _ => _machineWide = true);
        if (handler.IsGui)
            Options.Add("tab=", () => Resources.OptionConfigTab, (ConfigTab tab) => _tab = tab);
    }

    /// <inheritdoc/>
    public override ExitCode Execute()
    {
        var config = Load();

        switch (AdditionalArgs)
        {
            case []:
                ShowConfig(_tab);
                break;

            case [var key]:
                GetOptions(config, key);
                break;

            case [var key, var value]:
                if (_machineWide && WindowsUtils.IsWindows && !WindowsUtils.IsAdministrator)
                    throw new NotAdminException(Resources.MustBeAdminForMachineWide);

                SetOption(config, key, value);
                config.Save(_machineWide);
                break;
        }

        return ExitCode.OK;
    }

    private Config Load()
    {
        if (_machineWide)
        {
            var config = new Config();
            config.ReadFromFilesMachineWideOnly();
            return config;
        }

        return Config;
    }

    private void GetOptions(Config config, string key)
    {
        try
        {
            Handler.Output(key, config.GetOption(key));
        }
        #region Error handling
        catch (KeyNotFoundException)
        {
            throw new OptionException(string.Format(Resources.InvalidArgument, key), key);
        }
        #endregion
    }

    private static void SetOption(Config config, string key, string value)
    {
        try
        {
            if (value == "default") config.ResetOption(key);
            else config.SetOption(key, value);
        }
        #region Error handling
        catch (KeyNotFoundException)
        {
            throw new OptionException(string.Format(Resources.InvalidArgument, key), key);
        }
        catch (FormatException ex)
        {
            throw new OptionException(ex.Message, key);
        }
        #endregion
    }
}
