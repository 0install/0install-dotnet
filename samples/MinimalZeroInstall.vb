Imports NanoByte.Common.Tasks
Imports ZeroInstall.Model
Imports ZeroInstall.Services

Module MinimalZeroInstall
    Sub Main(ByVal args As String())
        Dim requirements = New Requirements(New FeedUri(args(0)))
        Dim services = New ServiceProvider(New CliTaskHandler())
        With services
            Dim selections = .Solver.Solve(requirements)
            For Each implementation In .SelectionsManager.GetUncachedImplementations(selections)
                .Fetcher.Fetch(implementation)
            Next
            .Executor.Start(selections)
        End With
    End Sub
End Module
