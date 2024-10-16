﻿using System.Threading.Tasks;

namespace InternalsViewer.UI.App.Activation;

public class DefaultActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    public DefaultActivationHandler()
    {
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        return false;
    }

    protected override async Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        await Task.CompletedTask;
    }
}
