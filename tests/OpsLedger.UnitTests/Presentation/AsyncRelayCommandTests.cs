using FluentAssertions;
using OpsLedger.Presentation.Common.Commands;

namespace OpsLedger.UnitTests.Presentation;

public sealed class AsyncRelayCommandTests
{
    [Fact]
    public async Task Execute_disables_command_until_async_work_completes()
    {
        TaskCompletionSource workStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseWork = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Int32 changeNotifications = 0;
        AsyncRelayCommand command = new(async () =>
        {
            workStarted.SetResult();
            await releaseWork.Task;
        });
        command.CanExecuteChanged += (_, _) => changeNotifications++;

        command.Execute(null);
        await workStarted.Task;

        command.CanExecute(null).Should().BeFalse();

        releaseWork.SetResult();
        await WaitUntilAsync(() => command.CanExecute(null));

        command.CanExecute(null).Should().BeTrue();
        changeNotifications.Should().Be(2);
    }

    [Fact]
    public void Execute_does_not_run_when_can_execute_returns_false()
    {
        Int32 executions = 0;
        AsyncRelayCommand command = new(
            () =>
            {
                executions++;
                return Task.CompletedTask;
            },
            () => false);

        command.Execute(null);

        executions.Should().Be(0);
        command.CanExecute(null).Should().BeFalse();
    }

    private static async Task WaitUntilAsync(Func<Boolean> condition)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(2);

        while (!condition())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("The condition was not met before the timeout.");
            }

            await Task.Delay(20);
        }
    }
}
